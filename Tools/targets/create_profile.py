"""Create a nonvalidated control profile from independently measured tag centers."""
import argparse,csv,json,math,pathlib
ROOT=pathlib.Path(__file__).resolve().parents[2]
PROJECT=ROOT/'Assets/SpatialBuild/Resources/DentalClinic/project.json'
OUTPUT=ROOT/'Assets/SpatialBuild/Resources/DentalClinic/control-profile.json'

def cross(a,b):return (a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0])
def norm(a):return math.sqrt(sum(x*x for x in a))

def create(input_csv,calibration_version,survey_basis):
 project=json.loads(PROJECT.read_text(encoding='utf8'))
 with open(input_csv,newline='',encoding='utf8') as f:rows=list(csv.DictReader(f))
 if len(rows)<3:raise ValueError('At least three measured tag centers are required')
 if not calibration_version or calibration_version=='UNCONFIGURED' or not survey_basis:raise ValueError('Declare a calibration version and measurement basis')
 controls=[];centers=[];ids=set()
 for r in rows:
  tag=int(r['marker_id']);size=float(r['marker_size_m']);p=[float(r[f'{axis}_m']) for axis in 'xyz']
  if tag in ids or tag<0 or not 0<size<2 or not all(map(math.isfinite,p)) or not r['datum_description'].strip():raise ValueError('Duplicate ID, invalid size/position, or missing datum description')
  ids.add(tag);centers.append(p)
  identity={'x':0,'y':0,'z':0,'w':1}
  controls.append({'nodeId':f'control-{tag}','datumDescription':r['datum_description'].strip(),'calibrationVersion':calibration_version,'family':'tagStandard41h12','markerId':tag,'markerSizeM':size,'projectFromDatum':{'position':dict(zip('xyz',p)),'rotation':identity},'datumFromMarker':{'position':{'x':0,'y':0,'z':0},'rotation':identity},'surveyed':True,'physicalValidationPassed':False,'orientationCalibrated':False})
 a=centers[0];area=max((norm(cross([b[i]-a[i] for i in range(3)],[c[i]-a[i] for i in range(3)])) for b in centers[1:] for c in centers[1:] if b!=c),default=0)
 if area<.01:raise ValueError('Measured controls are nearly collinear or too close')
 return {'schemaVersion':'spatialbuild.control.v1','projectId':project['projectId'],'projectRevision':project['revision'],'calibrationVersion':calibration_version,'surveyBasis':survey_basis,'notes':'Generated from measured centers. No independent accuracy trial or construction validity is claimed.','controls':controls}

if __name__=='__main__':
 parser=argparse.ArgumentParser();parser.add_argument('csv');parser.add_argument('--version',required=True);parser.add_argument('--basis',required=True);parser.add_argument('--output',default=str(OUTPUT));args=parser.parse_args()
 profile=create(args.csv,args.version,args.basis)
 pathlib.Path(args.output).write_text(json.dumps(profile,indent=2),encoding='utf8')
 print('Wrote',len(profile['controls']),'measured controls; independent physical validation remains false')
