using System;
using UnityEngine;
using SpatialBuild.Construction;
namespace SpatialBuild.Quest {
 public sealed class WorkerGuide : MonoBehaviour {
  public ConstructionWorld world;public Transform eye;
  TextMesh instruction;LineRenderer target;
  Material material;
  public string Message {get;private set;}="Demo only - position not verified";
  public void Initialize(ConstructionWorld model,Transform head){
   world=model;eye=head;
   var asset=Resources.Load<TextAsset>("demo-pipe-job");world.job=new ConstructionJob(JsonUtility.FromJson<JobDefinition>(asset.text));
   try{world.job.Load();}catch(Exception e){Debug.LogWarning("Job progress load: "+e.Message);Message="Could not restore progress";}
   instruction=CurvedPreviewPanel.Text(transform,"",Vector3.zero,.008f);
   var go=new GameObject("Active construction target");go.transform.SetParent(transform,false);target=go.AddComponent<LineRenderer>();
   target.positionCount=4;target.startWidth=.008f;target.endWidth=.008f;material=new Material(Shader.Find("SpatialBuild/TransparentGuidance"));material.color=new Color(1,.75f,.1f,1);target.sharedMaterial=material;
  }
  public void Begin(){world.jobActive=true;world.discipline=Discipline.All;world.focusedTaskId="";world.taskOnly=false;world.elevationFilter=false;world.layerMode=0;world.onlyNext=false;}
  public void Done(){if(world.jobActive&&world.job.MarkDone())Save();}
  public void Undo(){if(world.job.Undo())Save();}
  void Save(){try{world.job.Save();Message="Progress saved - self-reported";}catch(Exception e){Message="Progress NOT saved";Debug.LogWarning(e.Message);}}
  public void Pause(){world.jobActive=false;}
  public string Summary=>world.job.Current==null?"All steps marked done - not verified":world.job.Current.targetId+" | "+world.job.Current.title;
  void LateUpdate(){
   if(!world||world.job==null)return;
   var step=world.job.Current;var element=step==null?null:world.FindElement(step.targetId);
   bool visible=world.displayEnabled&&world.jobActive&&element&&element.GetComponent<Renderer>().enabled;
   instruction.gameObject.SetActive(visible);target.enabled=visible;if(!visible)return;
   Vector3 p=element.transform.position;float scale=world.transform.lossyScale.x;
   instruction.transform.position=p+Vector3.up*(.22f*scale);instruction.transform.localScale=Vector3.one*Mathf.Max(.35f,scale);
   instruction.transform.rotation=Quaternion.LookRotation(instruction.transform.position-eye.position,Vector3.up);
   instruction.text=step.targetId+"  "+step.title+"\n"+step.quantity.ToString("0.##")+" "+step.unit+" | "+step.material+"\nDEMO - POSITION NOT VERIFIED";
   if(step.targetId=="P-284"){
    target.SetPosition(0,world.transform.TransformPoint(new Vector3(2,2.9f,3)));target.SetPosition(1,world.transform.TransformPoint(new Vector3(8,2.9f,3)));target.SetPosition(2,target.GetPosition(1));target.SetPosition(3,target.GetPosition(1));
   }else{Vector3 x=world.transform.right*.12f*scale,z=world.transform.forward*.12f*scale;target.SetPosition(0,p-x);target.SetPosition(1,p+x);target.SetPosition(2,p);target.SetPosition(3,p+z);}
   target.startWidth=target.endWidth=Mathf.Max(.002f,.008f*scale);
  }
  void OnDestroy(){if(material)Destroy(material);}
 }
}
