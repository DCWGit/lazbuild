import csv,json,math,pathlib,sys,tempfile,unittest
ROOT=pathlib.Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'Tools/targets'))
sys.path.insert(0,str(ROOT/'Tools/measurements'))
from create_profile import create
from analyze import evaluate,REQUIRED

class PipelineChecks(unittest.TestCase):
 def test_control_profile_never_self_validates(self):
  with tempfile.TemporaryDirectory() as tmp:
   path=pathlib.Path(tmp)/'centers.csv'
   with path.open('w',newline='') as f:
    writer=csv.DictWriter(f,fieldnames=['marker_id','marker_size_m','x_m','y_m','z_m','datum_description']);writer.writeheader()
    for marker,x,y in [(0,0,0),(1,1,0),(2,0,1)]:writer.writerow(dict(marker_id=marker,marker_size_m=.063,x_m=x,y_m=y,z_m=0,datum_description='measured center'))
   result=create(path,'test-revision','synthetic unit test fixture')
   project=json.loads((ROOT/'Assets/SpatialBuild/Resources/DentalClinic/project.json').read_text())
   self.assertEqual(result['projectRevision'],project['revision'])
   self.assertTrue(all(c['surveyed'] and not c['physicalValidationPassed'] for c in result['controls']))
   self.assertEqual(len(result['controls']),3)
 def test_physical_measurement_schema_and_stats(self):
  with tempfile.TemporaryDirectory() as tmp:
   path=pathlib.Path(tmp)/'checkpoints.csv'
   with path.open('w',newline='') as f:
    writer=csv.DictWriter(f,fieldnames=REQUIRED);writer.writeheader()
    for i,error in enumerate([0,.01,.02]):
     row=dict.fromkeys(REQUIRED,'test');row.update(checkpoint_id=str(i),repeat_index=str(i),design_x_m='0',design_y_m='0',design_z_m='0',observed_x_m=str(error),observed_y_m='0',observed_z_m='0',measurement_type='independent_physical',instrument='test instrument',operator='test operator');writer.writerow(row)
   result=evaluate(path)
   self.assertEqual(result['n'],3);self.assertAlmostEqual(result['mean_mm'],10)
   self.assertAlmostEqual(result['rmse_mm'],math.sqrt(500/3));self.assertAlmostEqual(result['p95_mm'],20)
   contents=path.read_text().replace('independent_physical','simulated',1);path.write_text(contents)
   with self.assertRaises(ValueError):evaluate(path)

if __name__=='__main__':unittest.main()
