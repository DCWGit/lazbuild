"""Analyze independently observed physical checkpoints. No synthetic data accepted."""
import argparse,csv,json,math,statistics,pathlib

REQUIRED=('trial_id','utc','project_revision','calibration_version','software_commit','checkpoint_id','design_x_m','design_y_m','design_z_m','observed_x_m','observed_y_m','observed_z_m','instrument','operator','environment','repeat_index','measurement_type')

def evaluate(path):
 with open(path,newline='',encoding='utf8') as f:
  reader=csv.DictReader(f)
  missing=set(REQUIRED)-set(reader.fieldnames or [])
  if missing:raise ValueError('Missing columns: '+', '.join(sorted(missing)))
  rows=list(reader)
 if not rows:raise ValueError('No physical observations; no accuracy statistics can be calculated')
 if any(r['measurement_type']!='independent_physical' for r in rows):raise ValueError('Only independent_physical observations are accepted')
 if any(not r['instrument'] or not r['operator'] or not r['checkpoint_id'] for r in rows):raise ValueError('Instrument, operator and checkpoint are required')
 errors=[]
 for r in rows:
  design=[float(r[f'design_{axis}_m']) for axis in 'xyz']
  observed=[float(r[f'observed_{axis}_m']) for axis in 'xyz']
  if not all(map(math.isfinite,design+observed)):raise ValueError('Nonfinite observation')
  delta=[(b-a)*1000 for a,b in zip(design,observed)]
  errors.append({'checkpoint_id':r['checkpoint_id'],'repeat_index':r['repeat_index'],'dx_mm':delta[0],'dy_mm':delta[1],'dz_mm':delta[2],'horizontal_mm':math.hypot(*delta[:2]),'spatial_mm':math.sqrt(sum(d*d for d in delta))})
 mag=sorted(e['spatial_mm'] for e in errors);n=len(mag)
 result={'evidence':'INDEPENDENT_PHYSICAL','n':n,'trial_ids':sorted(set(r['trial_id'] for r in rows)),'project_revisions':sorted(set(r['project_revision'] for r in rows)),'calibration_versions':sorted(set(r['calibration_version'] for r in rows)),'software_commits':sorted(set(r['software_commit'] for r in rows)),'mean_mm':statistics.mean(mag),'median_mm':statistics.median(mag),'rmse_mm':math.sqrt(sum(x*x for x in mag)/n),'p95_mm':mag[max(0,math.ceil(.95*n)-1)],'maximum_mm':max(mag),'stddev_mm':statistics.stdev(mag) if n>1 else None,'observations':errors}
 return result

if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('csv');p.add_argument('--output');a=p.parse_args()
 result=evaluate(a.csv);text=json.dumps(result,indent=2)
 if a.output:pathlib.Path(a.output).write_text(text,encoding='utf8')
 else:print(text)
