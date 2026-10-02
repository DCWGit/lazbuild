"""Validate normalized IFC references and provenance before Unity packaging."""
import collections,hashlib,json,pathlib,struct
ROOT=pathlib.Path(__file__).resolve().parents[2]
data=json.loads((ROOT/'Assets/SpatialBuild/Resources/DentalClinic/project.json').read_text())
assert data['schemaVersion']=='spatialbuild.project.v2' and data['units']=='metres'
assert len(data['projectOrigin'])==3 and len(data['bimToProject'])==16
assert len(data['elements'])==len({x['id'] for x in data['elements']})
assert all(x['sourceGuid'] and x['sourceRevision'] and x['metadata'] is not None for x in data['elements'])
source_hashes={}
for source in data['sources']:
 p=ROOT/'SourceData/dental-clinic'/source['file']
 digest=hashlib.sha256(p.read_bytes()).hexdigest()
 assert digest==source['sha256'],p
 source_hashes[source['file'][:3]]=digest
revision_input=dict(sourceHashes=[s['sha256'] for s in data['sources']],bimToProject=data['bimToProject'],projectOrigin=data['projectOrigin'])
assert data['revision']==hashlib.sha256(json.dumps(revision_input,sort_keys=True,separators=(',',':')).encode()).hexdigest()
buffers={};counts=collections.Counter()
for element in data['elements']:
 prefix=element['id'].split(':')[0]
 assert element['sourceRevision']==source_hashes[prefix]
 if not element['geometryAvailable']:
  assert not element['geometryReference'] and element['vertexCount']==0 and element['anchor'] is None
  continue
 reference=element['geometryReference']
 if reference not in buffers:buffers[reference]=(ROOT/'Assets/SpatialBuild/Resources'/f'{reference}.bytes').read_bytes()
 blob=buffers[reference];start=element['byteOffset'];v=element['vertexCount'];i=element['indexCount'];end=start+12*v+4*i
 assert 0<=start<end<=len(blob) and i%3==0 and len(element['anchor'])==3
 indices=struct.unpack_from('<'+str(i)+'i',blob,start+12*v)
 assert all(0<=j<v for j in indices),element['id']
 counts[prefix]+=1
assert counts['arc']>0 and counts['str']>0 and counts['mep']==0
print('PASS',len(data['elements']),'unique IFC elements,',sum(counts.values()),'renderable meshes, source hashes and binary ranges')
print('MEP source has no geometry; metadata retained without invented coordinates')
