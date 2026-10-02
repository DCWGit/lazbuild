using System.Collections.Generic;
using UnityEngine;
using SpatialBuild.Positioning;

namespace SpatialBuild.Construction {
    public enum Discipline { All, Structural, Civil, Mechanical, Electrical, Plumbing }
    public sealed class ConstructionElement : MonoBehaviour {
        public string elementId,material,system,instruction;
        public Discipline discipline;
        public bool taskTarget;
    }
    public sealed class ConstructionWorld : MonoBehaviour {
        public PositioningProvider provider;
        public Discipline discipline=Discipline.All;
        public bool elevationFilter,taskOnly;
        public bool displayEnabled=true;
        public string focusedTaskId="";
        public ConstructionJob job;
        public bool jobActive,onlyNext,hideDone=true;
        public int layerMode; // 0 all, 1 band, 2 below
        public ConstructionElement FindElement(string id){return elements.Find(e=>e.elementId==id);}
        public float elevationM=2.9f,halfRangeM=.3f;
        public string selectedInfo="Look at an object and select to inspect it.";
        readonly List<ConstructionElement> elements=new List<ConstructionElement>();
        Material amber,red,ghost,current;
        void Start(){
            amber=Material(new Color(1,.65f,.15f,.4f));red=Material(new Color(1,.1f,.1f,.4f));
            ghost=Material(new Color(.15f,.7f,1,.18f));current=Material(new Color(1,.75f,.1f,.85f));
            Add("SLAB-01",Discipline.Civil,new Vector3(5,-.12f,4),new Vector3(10,.2f,8),"Concrete","Foundation",false);
            Add("COL-01",Discipline.Structural,new Vector3(1,1.5f,1),new Vector3(.3f,3,.3f),"Steel","Structure",false);
            Add("COL-02",Discipline.Structural,new Vector3(8,1.5f,1),new Vector3(.3f,3,.3f),"Steel","Structure",false);
            Add("BEAM-01",Discipline.Structural,new Vector3(4.5f,3,1),new Vector3(7,.3f,.25f),"Steel","Structure",false);
            Add("WALL-01",Discipline.Structural,new Vector3(5,1.25f,7),new Vector3(8,2.5f,.12f),"Example wall","Partition",false);
            Add("P-284",Discipline.Plumbing,new Vector3(5,2.9f,3),new Vector3(6,.05f,.05f),"Copper, nominal 2 inch (example)","Domestic water",false);
            Add("DUCT-01",Discipline.Mechanical,new Vector3(5,2.5f,5),new Vector3(6,.35f,.5f),"Sheet metal","Supply air",false);
            Add("CONDUIT-01",Discipline.Electrical,new Vector3(5,2.7f,6),new Vector3(6,.025f,.025f),"Example conduit","Power",false);
            for(int i=0;i<5;i++)Add("H"+(184+i),Discipline.Mechanical,new Vector3(2+i*1.5f,3.2f,3),new Vector3(.09f,.09f,.09f),"Example hanger anchor","Pipe supports",true);
        }
        Material Material(Color c){var m=new Material(Shader.Find("SpatialBuild/TransparentGuidance"));m.color=c;return m;}
        void Add(string id,Discipline d,Vector3 p,Vector3 size,string material,string system,bool task){
            var parent=transform.Find(d.ToString());if(!parent){parent=new GameObject(d.ToString()).transform;parent.SetParent(transform,false);}
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=id;go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.localScale=size;
            go.GetComponent<Renderer>().sharedMaterial=amber;
            var e=go.AddComponent<ConstructionElement>();e.elementId=id;e.discipline=d;e.material=material;e.system=system;e.taskTarget=task;e.instruction=task?"DRILL HERE (SIMULATED)":"DESIGN LOCATION (SIMULATED)";elements.Add(e);
        }
        void Update(){
            bool fault=provider&&provider.Current.status==PositionStatus.Fault;
            foreach(var e in elements){var r=e.GetComponent<Renderer>();
                // All geometry is in the project frame. Y in Unity is project elevation Z.
                var center=transform.InverseTransformPoint(e.transform.position);var height=e.transform.localScale.y/2;
                bool show=displayEnabled&&(discipline==Discipline.All||e.discipline==discipline)&&(!taskOnly||e.taskTarget)&&
                    (string.IsNullOrEmpty(focusedTaskId)||e.elementId==focusedTaskId)&&
                    (!elevationFilter||center.y+height>=elevationM-halfRangeM&&center.y-height<=elevationM+halfRangeM);
                if(layerMode==1)show&=center.y+height>=elevationM-halfRangeM&&center.y-height<=elevationM+halfRangeM;
                if(layerMode==2)show&=center.y-height<=elevationM;
                bool active=jobActive&&job!=null&&job.Current!=null&&job.Current.targetId==e.elementId;
                if(jobActive&&job!=null)show&=job.Contains(e.elementId)&&(!hideDone||!job.done.Contains(e.elementId))&&(!onlyNext||active);
                r.enabled=show;e.GetComponent<Collider>().enabled=show;r.sharedMaterial=fault?red:jobActive?(active?current:ghost):amber;
            }
        }
        public void Select(Camera camera){
            if(!camera)return;
            Select(camera.ViewportPointToRay(new Vector3(.5f,.5f)));
        }
        public void Select(Ray ray){
            if(Physics.Raycast(ray,out var hit,100)){
                var e=hit.collider.GetComponent<ConstructionElement>();if(!e){selectedInfo="No construction element selected";return;}
                selectedInfo=e.elementId+" | "+e.discipline+"\n"+e.material+"\n"+e.system+"\nElevation: "+transform.InverseTransformPoint(e.transform.position).y.ToString("F3")+" m\n"+e.instruction;
            }else selectedInfo="No element at the center crosshair";
        }
        void OnDestroy(){if(amber)Destroy(amber);if(red)Destroy(red);if(ghost)Destroy(ghost);if(current)Destroy(current);}
    }
}
