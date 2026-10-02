using UnityEngine;
using SpatialBuild.Construction;
namespace SpatialBuild.Quest {
 public sealed class HandPreviewControls : MonoBehaviour {
  public OVRHand rightHand;
  public ConstructionWorld world;
  public Transform eye,trackingSpace;
  readonly PinchGate pinch=new PinchGate();
  readonly FieldTrialSession trial=new FieldTrialSession();
  CurvedPreviewPanel panel;
  LineRenderer rayLine;
  GameObject cursor,checkMarker;
  Material rayMaterial,markerMaterial;
  TextMesh markerLabel;
  WorkerGuide guide;
  bool materials;
  bool active,moveMode,dragging;
  int tab;
  float clickUntil;
  Vector3 dragHand,dragWorld;
  public string Hint {get;private set;}="Raise your right hand";
  public void Suspend(){
   active=false;dragging=false;moveMode=false;pinch.Reset();trial.checkMode=false;
   if(guide)guide.Pause();
   if(panel){panel.Collapse();panel.gameObject.SetActive(false);}if(rayLine)rayLine.enabled=false;
   if(cursor)cursor.SetActive(false);if(checkMarker)checkMarker.SetActive(false);
  }
  void Create(){
   guide=gameObject.AddComponent<WorkerGuide>();guide.Initialize(world,eye);
   var go=new GameObject("Curved preview menu");go.transform.SetParent(transform,false);panel=go.AddComponent<CurvedPreviewPanel>();panel.Build();
   var line=new GameObject("Hand pointer");line.transform.SetParent(transform,false);rayLine=line.AddComponent<LineRenderer>();
   rayLine.positionCount=2;rayLine.startWidth=.003f;rayLine.endWidth=.002f;
   rayMaterial=new Material(Shader.Find("SpatialBuild/TransparentGuidance"));rayMaterial.color=Color.cyan;rayLine.sharedMaterial=rayMaterial;
   cursor=GameObject.CreatePrimitive(PrimitiveType.Sphere);cursor.name="Pinch feedback dot";cursor.transform.SetParent(transform,false);
   cursor.transform.localScale=Vector3.one*.014f;Destroy(cursor.GetComponent<Collider>());cursor.GetComponent<Renderer>().sharedMaterial=rayMaterial;
   checkMarker=GameObject.CreatePrimitive(PrimitiveType.Sphere);checkMarker.name="User check reference";Destroy(checkMarker.GetComponent<Collider>());
   checkMarker.transform.SetParent(world.transform,false);checkMarker.transform.localScale=Vector3.one*.06f;
   markerMaterial=new Material(Shader.Find("SpatialBuild/TransparentGuidance"));markerMaterial.color=new Color(1,.7f,.1f,1);
   checkMarker.GetComponent<Renderer>().sharedMaterial=markerMaterial;
   markerLabel=CurvedPreviewPanel.Text(checkMarker.transform,"",new Vector3(0,2,0),.07f);checkMarker.SetActive(false);
  }
  void PlacePanel(){
   // UI follows the complete view pose; ConstructionWorld remains independent.
   panel.transform.SetParent(eye,false);
   panel.transform.localPosition=Vector3.zero;
   panel.transform.localRotation=Quaternion.identity;
   panel.transform.localScale=Vector3.one;
   Physics.SyncTransforms();
  }
  public void Tick(HeadsetPreview preview){
   if(!panel)Create();if(!active){active=true;panel.gameObject.SetActive(true);PlacePanel();pinch.Reset();}
   // Keep collider targeting in sync with the moving head-relative panel.
   Physics.SyncTransforms();
   bool valid=rightHand&&rightHand.IsTracked&&rightHand.IsDataHighConfidence&&rightHand.IsPointerPoseValid&&!rightHand.IsSystemGestureInProgress&&rightHand.GetFingerConfidence(OVRHand.HandFinger.Index)==OVRHand.TrackingConfidence.High;
   bool pinching=valid&&rightHand.GetFingerIsPinching(OVRHand.HandFinger.Index);bool pressed=pinch.Pressed(valid,pinching);
   if(!valid){dragging=false;rayLine.enabled=false;cursor.SetActive(false);panel.Collapse();Hint="Point at MENU below to open controls";Present(preview,-1,false);return;}
   Ray ray=new Ray(rightHand.PointerPose.position,rightHand.PointerPose.forward);
   int hovered=panel.Hover(ray,Time.unscaledDeltaTime,out float distance);
   if(hovered<0&&Physics.Raycast(ray,out var modelHit,distance))distance=modelHit.distance;
   if(pressed)clickUntil=Time.unscaledTime+.18f;bool purple=pinching||Time.unscaledTime<clickUntil;
   rayMaterial.color=purple?new Color(.78f,.25f,1f,1):Color.cyan;
   rayLine.enabled=true;rayLine.SetPosition(0,ray.origin);rayLine.SetPosition(1,ray.GetPoint(distance));
   cursor.SetActive(true);cursor.transform.position=ray.GetPoint(distance);cursor.transform.localScale=Vector3.one*(purple?.023f:.014f);
   Hint=dragging?"MOVING - release to place":moveMode?"Move unlocked - pinch and drag away from menu":"Point + pinch | purple = pinching";
   if(pressed){if(hovered>=0){dragging=false;Execute(hovered,preview);}else if(moveMode){dragging=true;dragHand=rightHand.transform.position;dragWorld=world.transform.position;}else world.Select(ray);}
   if(!pinching&&dragging){dragging=false;trial.Event("preview_moved",world.transform);}
   if(dragging){Vector3 delta=rightHand.transform.position-dragHand;if(delta.magnitude>1.5f){dragging=false;pinch.Reset();}else world.transform.position=dragWorld+delta;}
   Present(preview,hovered,purple);
  }
  void ShowAll(){world.discipline=Discipline.All;world.taskOnly=false;world.focusedTaskId="";world.elevationFilter=false;world.layerMode=0;}
  void FocusTask(){ShowAll();world.focusedTaskId="H"+(184+trial.taskIndex);}
  void Execute(int action,HeadsetPreview preview){
   if(action==10){panel.ToggleWheel();return;}
   if(action<4){tab=action;moveMode=false;panel.OpenTab();return;}int a=action-4;
   switch(tab){
    case 2:
     guide.Pause();
     if(a==0)preview.Place(world,eye,trackingSpace,!preview.fullScale);
     if(a==1)moveMode=!moveMode;
     if(a==2)preview.Rotate(world,-15);if(a==3)preview.Rotate(world,15);
     if(a==4){preview.Place(world,eye,trackingSpace,preview.fullScale);PlacePanel();}
     if(a==5){preview.Place(world,eye,trackingSpace,false);ShowAll();moveMode=false;trial.checkMode=false;PlacePanel();}break;
    case 1:
     world.elevationFilter=false;
     if(a==0)world.layerMode=(world.layerMode+1)%3;
     if(a==1)world.elevationM=Mathf.Clamp(world.elevationM-.25f,0,10);
     if(a==2)world.elevationM=Mathf.Clamp(world.elevationM+.25f,0,10);
     if(a==3)world.onlyNext=!world.onlyNext;
     if(a==4)world.hideDone=!world.hideDone;
     if(a==5){ShowAll();world.onlyNext=false;}break;
    case 0:
     if(a==0){if(world.jobActive)guide.Pause();else{if(!preview.fullScale)preview.Place(world,eye,trackingSpace,true);guide.Begin();moveMode=false;trial.checkMode=false;}}
     if(a==1){guide.Done();trial.Event("worker_marked_done_not_verified",world.transform);}
     if(a==2){guide.Undo();trial.Event("worker_undo",world.transform);}
     if(a==3)world.job.Next();
     if(a==4)materials=!materials;
     if(a==5)world.onlyNext=!world.onlyNext;
     break;
    case 3:
     if(a==0){trial.checkMode=!trial.checkMode;moveMode=false;}if(a==1){trial.checkIndex=(trial.checkIndex+1)%4;trial.reportedOffsetMm=-1;}
     if(a==2)trial.reportedOffsetMm=Mathf.Max(0,trial.reportedOffsetMm-1);if(a==3)trial.reportedOffsetMm=Mathf.Min(1000,Mathf.Max(0,trial.reportedOffsetMm)+1);if(a==4)trial.reportedOffsetMm=Mathf.Min(1000,Mathf.Max(0,trial.reportedOffsetMm)+10);
     if(a==5)trial.Record(world.transform.localScale.x,world.transform);break;
   }
   trial.Event("preview_tab_"+tab+"_action_"+a,world.transform);Debug.Log("SpatialBuild UI action: tab "+tab+", button "+a);
  }
  void Present(HeadsetPreview preview,int hovered,bool purple){
   string[] actions;string description;
   switch(tab){
    case 2:actions=new[]{preview.fullScale?"MINIATURE\n1:10":"FULL SIZE\n1:1",moveMode?"LOCK\nplacement":"MOVE\npinch + drag","ROTATE\n15 degrees left","ROTATE\n15 degrees right","PLACE\nin front","RESET\nminiature"};
     description="1  Choose size    2  Move    3  Lock\n"+(preview.fullScale?"Floor is approximate. Not survey aligned.":"Miniature preview. Not survey aligned.")+"\n"+Hint;break;
    case 1:actions=new[]{"LAYERS\n"+(world.layerMode==0?"all":world.layerMode==1?"one band":"all below"),"LOWER\n0.25 m","HIGHER\n0.25 m",world.onlyNext?"SHOW\nremaining":"ONLY\nnext piece",world.hideDone?"SHOW\ndone items":"HIDE\ndone items","RESET\nlayer view"};
     description="Height: "+world.elevationM.ToString("F2")+" m | band +/- "+world.halfRangeM.ToString("F2")+" m\nGold = next piece | blue = remaining\nDone items are your report, not a scan.";break;
    case 0:actions=new[]{world.jobActive?"PAUSE\njob guide":"START\ndemo job","DONE\nnext piece","UNDO\nlast done","SKIP\nfor now",materials?"SHOW\ninstruction":"MATERIALS\nremaining",world.onlyNext?"SHOW\nremaining":"ONLY\nnext piece"};
     description=materials?"MATERIALS STILL NEEDED\n"+world.job.Materials():Short(guide.Summary,58)+"\n"+world.job.done.Count+" / "+world.job.definition.steps.Length+" marked done (by you)\n"+(world.jobActive?guide.Message:"Start opens a full-size demo. Not registered.");break;
    default:actions=new[]{trial.checkMode?"HIDE\nreference":"SHOW\nreference","NEXT POINT\nP"+((trial.checkIndex+1)%4),"OFFSET\n-1 mm","OFFSET\n+1 mm","OFFSET\n+10 mm","SAVE\nuser report"};
     description="P"+trial.checkIndex+" | Your measured offset: "+(trial.reportedOffsetMm<0?"not entered":trial.reportedOffsetMm.ToString("F0")+" mm")+"\nMeasure against a real mark at full scale.\n"+Short(trial.LastMessage,64);break;
   }
   var labels=new string[10]{"JOB","LAYERS","PLACE","CHECK",actions[0],actions[1],actions[2],actions[3],actions[4],actions[5]};panel.Present(labels,tab,hovered,purple,description);
   bool show=trial.checkMode&&preview.fullScale&&world.displayEnabled;checkMarker.SetActive(show);
   if(show){checkMarker.transform.localPosition=FieldTrialSession.CheckPoint(trial.checkIndex);markerLabel.text="P"+trial.checkIndex+"\nUNREGISTERED";markerLabel.transform.rotation=Quaternion.LookRotation(markerLabel.transform.position-eye.position,Vector3.up);}
  }
  static string Short(string value,int length){if(string.IsNullOrEmpty(value))return "";return value.Length>length?value.Substring(0,length)+"...":value;}
  void OnDestroy(){if(rayMaterial)Destroy(rayMaterial);if(markerMaterial)Destroy(markerMaterial);}
 }
}
