using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using SpatialBuild.Positioning;
using SpatialBuild.Construction;

namespace SpatialBuild.Diagnostics {
    public sealed class NetworkDiagnostics : MonoBehaviour {
        public SimulatedPositioningProvider simulator;
        public ConstructionWorld world;
        public Camera viewCamera;
        public float errorMagnification=100;
        readonly List<GameObject> visuals=new List<GameObject>();
        Material cyan,magenta,beam;
        int lastSample=-1;float orbit=220;Vector2 scroll;
        string exportMessage="";
        public static Vector3 UnityPoint(Point3 p)=>new Vector3((float)p.x,(float)p.z,(float)p.y);
        void Start(){cyan=Make(Color.cyan);magenta=Make(Color.magenta);beam=Make(new Color(.2f,.65f,1));PositionCamera();}
        Material Make(Color c){var m=new Material(Shader.Find("Unlit/Color"));m.color=c;return m;}
        void PositionCamera(){if(!viewCamera)return;float a=orbit*Mathf.Deg2Rad;viewCamera.transform.position=new Vector3(5+14*Mathf.Cos(a),8,4+14*Mathf.Sin(a));viewCamera.transform.LookAt(new Vector3(5,1,4));}
        void Update(){if(simulator.Sample!=lastSample){lastSample=simulator.Sample;DrawNetwork();}}
        void DrawNetwork(){foreach(var v in visuals)if(v)Destroy(v);visuals.Clear();
            var nodes=simulator.Truth;if(nodes==null)return;
            for(int i=0;i<nodes.Length;i++){
                var actual=UnityPoint(nodes[i]-nodes[0]);Sphere(actual,cyan,.13f);
                Label(((char)('A'+i)).ToString(),actual+Vector3.up*.3f);
                if(simulator.Solution!=null&&simulator.Solution.valid){var estimated=UnityPoint(simulator.Solution.positions[i]);var exaggerated=actual+(estimated-actual)*errorMagnification;Sphere(exaggerated,magenta,.07f);Line(actual,exaggerated,magenta,.025f);}
                for(int j=i+1;j<nodes.Length;j++)if(!(simulator.occludeC&&(i==2||j==2)))Line(actual,UnityPoint(nodes[j]-nodes[0]),beam,.012f);
            }
        }
        void Sphere(Vector3 p,Material m,float radius){var g=GameObject.CreatePrimitive(PrimitiveType.Sphere);g.transform.SetParent(transform,false);g.transform.position=p;g.transform.localScale=Vector3.one*radius;Destroy(g.GetComponent<Collider>());g.GetComponent<Renderer>().sharedMaterial=m;visuals.Add(g);}
        void Line(Vector3 a,Vector3 b,Material m,float width){var g=new GameObject("Diagnostic line");g.transform.SetParent(transform,false);var l=g.AddComponent<LineRenderer>();l.sharedMaterial=m;l.positionCount=2;l.SetPositions(new[]{a,b});l.startWidth=l.endWidth=width;visuals.Add(g);}
        void Label(string text,Vector3 p){var g=new GameObject("Node label");g.transform.SetParent(transform,false);g.transform.position=p;g.transform.rotation=viewCamera.transform.rotation;var t=g.AddComponent<TextMesh>();t.text=text;t.characterSize=.15f;t.fontSize=32;t.anchor=TextAnchor.MiddleCenter;t.color=Color.cyan;visuals.Add(g);}
        void OnGUI(){
            GUI.Label(new Rect(Screen.width/2-5,Screen.height/2-10,20,20),"+");
            GUILayout.BeginArea(new Rect(12,12,430,Mathf.Max(200,Screen.height-24)),GUI.skin.box);
            scroll=GUILayout.BeginScrollView(scroll);
            GUILayout.Label("SPATIALBUILD | POSITIONING LAB");GUILayout.Label("SIMULATED ONLY - NOT PHYSICAL LAYOUT");
            GUILayout.Label("A fixed at origin; station orientations assumed known.\nUnity Y = project Z (elevation). All distances in metres.");
            var s=simulator.Current;GUILayout.Label(s.status+": "+s.message);
            GUILayout.Label("Optical sequence: "+simulator.Stage+" (timed illustration)");
            GUILayout.BeginHorizontal();if(GUILayout.Button(simulator.scanRunning?"Pause scanning":"Resume scanning"))simulator.scanRunning=!simulator.scanRunning;
            if(GUILayout.Button("Measure once"))simulator.Capture();GUILayout.EndHorizontal();
            GUILayout.Label("Independent Gaussian noise: 1 sigma, not +/- limits");
            simulator.noise.rangeMm=Slider("Range mm",simulator.noise.rangeMm,0,10);
            simulator.noise.angleDeg=Slider("Angle degrees",simulator.noise.angleDeg,0,.05);
            simulator.noise.levelDeg=Slider("Level degrees",simulator.noise.levelDeg,0,.02);
            simulator.noise.encoderDeg=Slider("Encoder degrees",simulator.noise.encoderDeg,0,.02);
            simulator.noise.centeringMm=Slider("Receiver mm",simulator.noise.centeringMm,0,2);
            simulator.noise.persistentRangeBiasMm=Slider("Range bias mm",simulator.noise.persistentRangeBiasMm,-10,10);
            simulator.noise.persistentYawBiasDeg=Slider("Shared yaw bias degrees",simulator.noise.persistentYawBiasDeg,-.1,.1);
            GUILayout.Label("Level/encoder/centering are independent angular equivalents.\nShared yaw bias is NOT covered by formal uncertainty.");
            GUILayout.BeginHorizontal();if(GUILayout.Button("Zero noise")){simulator.noise=new NoiseSettings{rangeMm=0,angleDeg=0,levelDeg=0,encoderDeg=0,centeringMm=0};simulator.Capture();}
            if(GUILayout.Button("Default noise")){simulator.noise=new NoiseSettings();simulator.Capture();}GUILayout.EndHorizontal();
            bool fourth=GUILayout.Toggle(simulator.fourthNode,"Four nodes (redundancy comparison)");if(fourth!=simulator.fourthNode){simulator.fourthNode=fourth;simulator.ResetExperiment();}
            bool hidden=GUILayout.Toggle(simulator.occludeC,"Occlude all measurements to/from C");if(hidden!=simulator.occludeC){simulator.occludeC=hidden;simulator.Capture();}
            GUILayout.BeginHorizontal();if(GUILayout.Button("Freeze baseline"))simulator.Calibrate();if(GUILayout.Button("Move C 50 mm"))simulator.MoveC();if(GUILayout.Button("Restore C"))simulator.RestoreC();GUILayout.EndHorizontal();
            GUILayout.Label("Baseline: "+(simulator.Monitor.HasBaseline?"frozen":"not set")+" | Displacement "+simulator.Monitor.displacementMm.ToString("F2")+" mm");
            GUILayout.Label("Alarm threshold: "+simulator.movementThresholdMm+" mm. Latches until explicit baseline reset.\nNo automatic node attribution or world realignment.");
            var sol=simulator.Solution;
            if(sol!=null&&sol.valid){
                GUILayout.Label("Sample "+simulator.Sample+" | Max truth error "+simulator.TruthErrorMm.ToString("F3")+" mm");
                GUILayout.Label("Weighted residual RMS "+sol.weightedRms.ToString("F3")+" | DOF "+sol.degreesOfFreedom+" | iterations "+sol.iterations);
                for(int i=0;i<sol.positions.Length;i++){var p=sol.positions[i];GUILayout.Label(((char)('A'+i))+": ("+p.x.ToString("F4")+", "+p.y.ToString("F4")+", "+p.z.ToString("F4")+") | RMS sigma "+sol.positionSigmaMm[i].ToString("F3")+" mm");}
                GUILayout.Label("Sigma = conditional linearized 3D RMS. Not 95% coverage.\nExcludes survey datum, shared biases, headset and display.");
            }
            GUILayout.Label("Cyan: truth. Magenta: error exaggerated "+errorMagnification+"x.\nBlue links: ideal lines, not simulated beam optics.");
            if(simulator.Measurements.Count>0){var o=simulator.Measurements[0];GUILayout.Label("A > B: "+o.distance.ToString("F4")+" m | az "+(o.azimuth*180/Math.PI).ToString("F4")+" deg | el "+(o.elevation*180/Math.PI).ToString("F4")+" deg");}
            if(GUILayout.Button("Export current measurements CSV"))Export();GUILayout.Label(exportMessage);
            GUILayout.Space(8);GUILayout.Label("CONSTRUCTION VIEW (independent of solved node graphics)");
            world.discipline=(Discipline)GUILayout.SelectionGrid((int)world.discipline,Enum.GetNames(typeof(Discipline)),3);
            world.taskOnly=GUILayout.Toggle(world.taskOnly,"Task: hanger locations H184-H188 only");
            world.elevationFilter=GUILayout.Toggle(world.elevationFilter,"Elevation slice");
            world.elevationM=(float)Slider("Elevation m",world.elevationM,0,4);world.halfRangeM=(float)Slider("Half range m",world.halfRangeM,.05,2);
            GUILayout.BeginHorizontal();if(GUILayout.Button("Orbit left")){orbit-=20;PositionCamera();DrawNetwork();}if(GUILayout.Button("Orbit right")){orbit+=20;PositionCamera();DrawNetwork();}if(GUILayout.Button("Select at crosshair"))world.Select(viewCamera);GUILayout.EndHorizontal();
            GUILayout.Label(world.selectedInfo);GUILayout.EndScrollView();GUILayout.EndArea();
        }
        double Slider(string label,double value,double min,double max){GUILayout.Label(label+": "+value.ToString("F4"));return GUILayout.HorizontalSlider((float)value,(float)min,(float)max);}
        void Export(){try{
            string folder=Path.Combine(Application.persistentDataPath,"Experiments");Directory.CreateDirectory(folder);
            string path=Path.Combine(folder,"simulation-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff")+".csv");
            var b=new StringBuilder("synthetic,session,sample,seed,from,to,range_m,az_rad,el_rad,range_sigma_m,angle_sigma_rad,range_bias_mm,yaw_bias_deg,max_truth_error_mm,solver_valid,weighted_rms\n");
            var captured=simulator.CapturedNoise;
            foreach(var o in simulator.Measurements)b.AppendLine(string.Join(",",new[]{"true",simulator.Current.session,(simulator.Sample-1).ToString(),captured.seed.ToString(),o.from.ToString(),o.to.ToString(),F(o.distance),F(o.azimuth),F(o.elevation),F(o.rangeSigma),F(o.angleSigma),F(captured.persistentRangeBiasMm),F(captured.persistentYawBiasDeg),F(simulator.TruthErrorMm),simulator.Solution.valid.ToString(),F(simulator.Solution.weightedRms)}));
            File.WriteAllText(path,b.ToString());
            var nodes=new StringBuilder("synthetic,node,true_x_m,true_y_m,true_z_m,solved_x_m,solved_y_m,solved_z_m,error_mm,conditional_rms_sigma_mm\n");
            for(int i=0;i<simulator.Truth.Length;i++){
                var t=simulator.Truth[i]-simulator.Truth[0];string prefix="true,"+i+","+F(t.x)+","+F(t.y)+","+F(t.z);
                if(simulator.Solution.valid){var p=simulator.Solution.positions[i];nodes.AppendLine(prefix+","+F(p.x)+","+F(p.y)+","+F(p.z)+","+F((p-t).Length*1000)+","+F(simulator.Solution.positionSigmaMm[i]));}
                else nodes.AppendLine(prefix+",,,,,");
            }
            File.WriteAllText(Path.ChangeExtension(path,"nodes.csv"),nodes.ToString());
            File.WriteAllText(Path.ChangeExtension(path,"settings.json"),JsonUtility.ToJson(captured,true));
            exportMessage="Saved measurements, nodes and settings: "+path;
        }catch(Exception e){exportMessage="Export failed: "+e.Message;}}
        static string F(double x)=>x.ToString("R",CultureInfo.InvariantCulture);
        void OnDestroy(){foreach(var g in visuals)if(g)Destroy(g);if(cyan)Destroy(cyan);if(magenta)Destroy(magenta);if(beam)Destroy(beam);}
    }
}
