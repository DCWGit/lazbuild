"""Pinned IFC -> normalized metadata + local float mesh buffers. No coordinate fitting."""
import json,pathlib,hashlib,struct,collections,concurrent.futures
import numpy as np
import ifcopenshell,ifcopenshell.geom,ifcopenshell.util.unit,ifcopenshell.util.element as util,ifcopenshell.util.placement
ROOT=pathlib.Path(__file__).resolve().parents[2]
OUT=ROOT/'Assets/SpatialBuild/Resources/DentalClinic'
def classify(e,source):
 if source=='arc':return 'ARCHITECTURAL','source-model'
 if source=='str':return 'STRUCTURAL','source-model'
 typ=util.get_type(e);groups=util.get_groups(e)
 evidence=' '.join([e.Name or '',typ.Name if typ and typ.Name else '',*(g.Name or '' for g in groups)]).lower()
 for discipline,words in [('FIRE_PROTECTION',['sprinkler','fire protection']),('ELECTRICAL',['conduit','cable','electrical','lighting','junction box']),('PLUMBING',['sanitary','domestic water','plumbing','lavatory','water closet']),('MECHANICAL',['duct','diffuser','air terminal','vav','fan','chiller','boiler'])]:
  if any(w in evidence for w in words):return discipline,'source-name rule: '+evidence[:180]
 return 'OTHER','insufficient discipline evidence'
def convert(source):
 path=ROOT/'SourceData/dental-clinic'/f'{source}.ifc';revision=hashlib.sha256(path.read_bytes()).hexdigest();m=ifcopenshell.open(str(path));units=ifcopenshell.util.unit.calculate_unit_scale(m)
 settings=ifcopenshell.geom.settings();settings.set(settings.USE_WORLD_COORDS,True)
 openings=m.by_type('IfcOpeningElement')
 iterator=ifcopenshell.geom.iterator(settings,m,1,**({'exclude':openings} if openings else {}))
 has_geometry=iterator.initialize()
 rows=[];blobs=[];part=0;file=None;bounds_min=np.full(3,np.inf);bounds_max=np.full(3,-np.inf);seen=set();max_error=0
 try:
  while has_geometry:
   shape=iterator.get();e=m.by_id(shape.id);vertices=np.array(shape.geometry.verts,dtype=np.float64).reshape(-1,3);indices=np.array(shape.geometry.faces,dtype=np.int32).reshape(-1,3)
   if len(vertices) and len(indices):
    if not np.isfinite(vertices).all():raise ValueError('Nonfinite IFC geometry')
    placement=ifcopenshell.util.placement.get_local_placement(e.ObjectPlacement) if e.ObjectPlacement else np.eye(4);placement[:3,3]*=units
    anchor=vertices.mean(axis=0);local=(vertices-anchor)[:,[0,2,1]].astype('<f4');tri=indices[:,[0,2,1]].astype('<i4')
    max_error=max(max_error,float(np.max(np.linalg.norm(local.astype(float)[:,[0,2,1]]+anchor-vertices,axis=1))))
    payload=local.tobytes()+tri.tobytes();trade,evidence=classify(e,source);typ=util.get_type(e)
    row=dict(id=source+':'+e.GlobalId,sourceGuid=e.GlobalId,name=e.Name or '',discipline=trade,disciplineEvidence=evidence,category=e.is_a(),type=typ.Name if typ else '',sourceRevision=revision,geometryAvailable=True,geometryReference='',byteOffset=0,vertexCount=len(vertices),indexCount=indices.size,anchor=anchor.tolist(),localTransform=placement.flatten().tolist(),projectTransform=placement.flatten().tolist(),metadata=json.dumps(util.get_psets(e),default=str),boundsMin=vertices.min(0).tolist(),boundsMax=vertices.max(0).tolist())
    rows.append(row);blobs.append((row,payload))
    bounds_min=np.minimum(bounds_min,vertices.min(0));bounds_max=np.maximum(bounds_max,vertices.max(0));seen.add(e.GlobalId)
   if not iterator.next():break
 finally:
  if file:file.close()
 expected=[e for e in m.by_type('IfcElement') if e.Representation and not e.is_a('IfcOpeningElement')]
 # Some valid IFC shapes are skipped by the geometry iterator. Attempt each
 # missing represented element directly before recording it as unavailable.
 for e in expected:
  if e.GlobalId in seen:continue
  try:shape=ifcopenshell.geom.create_shape(settings,e)
  except RuntimeError:continue
  vertices=np.array(shape.geometry.verts,dtype=np.float64).reshape(-1,3);indices=np.array(shape.geometry.faces,dtype=np.int32).reshape(-1,3)
  if not len(vertices) or not len(indices):continue
  if not np.isfinite(vertices).all():raise ValueError('Nonfinite fallback IFC geometry')
  placement=ifcopenshell.util.placement.get_local_placement(e.ObjectPlacement) if e.ObjectPlacement else np.eye(4);placement[:3,3]*=units
  anchor=vertices.mean(axis=0);local=(vertices-anchor)[:,[0,2,1]].astype('<f4');tri=indices[:,[0,2,1]].astype('<i4')
  max_error=max(max_error,float(np.max(np.linalg.norm(local.astype(float)[:,[0,2,1]]+anchor-vertices,axis=1))))
  payload=local.tobytes()+tri.tobytes();trade,evidence=classify(e,source);typ=util.get_type(e)
  row=dict(id=source+':'+e.GlobalId,sourceGuid=e.GlobalId,name=e.Name or '',discipline=trade,disciplineEvidence=evidence,category=e.is_a(),type=typ.Name if typ else '',sourceRevision=revision,geometryAvailable=True,geometryReference='',byteOffset=0,vertexCount=len(vertices),indexCount=indices.size,anchor=anchor.tolist(),localTransform=placement.flatten().tolist(),projectTransform=placement.flatten().tolist(),metadata=json.dumps(util.get_psets(e),default=str),boundsMin=vertices.min(0).tolist(),boundsMax=vertices.max(0).tolist())
  rows.append(row);blobs.append((row,payload));seen.add(e.GlobalId);bounds_min=np.minimum(bounds_min,vertices.min(0));bounds_max=np.maximum(bounds_max,vertices.max(0))
 # IfcOpenShell's parallel iterator yields nondeterministic order. Sort before
 # assigning byte offsets so repeated imports produce the same resource bytes.
 try:
  for row,payload in sorted(blobs,key=lambda x:x[0]['id']):
   if file is None or file.tell()+len(payload)>30_000_000:
    if file:file.close()
    part+=1;file=(OUT/f'{source}-{part:03d}.bytes').open('wb')
   row['geometryReference']=f'DentalClinic/{source}-{part:03d}'
   row['byteOffset']=file.tell();file.write(payload)
 finally:
  if file:file.close()
 missing=[e.GlobalId for e in expected if e.GlobalId not in seen]
 # Elements with no IFC representation remain in the catalog, without invented coordinates.
 for e in m.by_type('IfcElement'):
  if e.is_a('IfcOpeningElement') or e.GlobalId in seen:continue
  trade,evidence=classify(e,source);typ=util.get_type(e)
  rows.append(dict(id=source+':'+e.GlobalId,sourceGuid=e.GlobalId,name=e.Name or '',discipline=trade,disciplineEvidence=evidence,category=e.is_a(),type=typ.Name if typ else '',sourceRevision=revision,geometryAvailable=False,geometryReference='',byteOffset=0,vertexCount=0,indexCount=0,anchor=None,localTransform=None,projectTransform=None,metadata=json.dumps(util.get_psets(e),default=str),boundsMin=None,boundsMax=None))
 report=dict(file=path.name,sha256=revision,sourceUnitToMetre=units,schema=m.schema,elements=len(rows),renderableElements=len(seen),representedElements=len(expected),allElements=len(m.by_type('IfcElement')),missingGeometry=missing,boundsMin=bounds_min.tolist() if has_geometry else None,boundsMax=bounds_max.tolist() if has_geometry else None,maxFloatMeshErrorM=max_error,disciplines=dict(collections.Counter(e['discipline'] for e in rows)))
 rows.sort(key=lambda x:x['id'])
 print(json.dumps(report),flush=True);return rows,report
if __name__=='__main__':
 OUT.mkdir(parents=True,exist_ok=True)
 with concurrent.futures.ProcessPoolExecutor(max_workers=3) as pool: results=list(pool.map(convert,['arc','str','mep']))
 elements=sum((r[0] for r in results),[]);reports=[r[1] for r in results]
 origin=np.min([r['boundsMin'] for r in reports if r['boundsMin'] is not None],axis=0)
 frame=[1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1]
 revision_input=dict(sourceHashes=[r['sha256'] for r in reports],bimToProject=frame,projectOrigin=origin.tolist())
 revision=hashlib.sha256(json.dumps(revision_input,sort_keys=True,separators=(',',':')).encode()).hexdigest()
 manifest=dict(schemaVersion='spatialbuild.project.v2',projectId='dental-clinic',projectName='Dental Clinic',revision=revision,units='metres',coordinateSystem='IFC federated engineering XYZ Z-up; no surveyed project registration supplied',projectOrigin=origin.tolist(),bimToProject=frame,calibrationVersion='UNCONFIGURED',sources=reports,elements=elements)
 (OUT/'project.json').write_text(json.dumps(manifest,separators=(',',':'),allow_nan=False),encoding='utf-8')
 (ROOT/'Reports/ifc-import.json').write_text(json.dumps(dict(revision=revision,origin=origin.tolist(),sources=reports,elements=len(elements)),indent=2))
 print('COMPLETE',len(elements),revision,flush=True)
