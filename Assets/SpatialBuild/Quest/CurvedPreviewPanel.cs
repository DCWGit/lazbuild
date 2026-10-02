using UnityEngine;
namespace SpatialBuild.Quest {
 public sealed class CurvedPreviewPanel : MonoBehaviour {
  public const float Radius=1.15f;
  public readonly BoxCollider[] buttons=new BoxCollider[11];
  readonly TextMesh[] labels=new TextMesh[10];
  readonly Renderer[] backgrounds=new Renderer[10];
  readonly Transform[] roots=new Transform[10];
  readonly bool[] available=new bool[10];
  Material normal,hover,pressed,selected;
  GameObject information;
  TextMesh details;
  Renderer hubRenderer;
  bool expanded,actions,closedUntilExit;
  float animation,lastHover;
  static readonly Vector3 Hub=new Vector3(0,-.43f,Radius);
  public static Vector3 Arc(float degrees,float y){float a=degrees*Mathf.Deg2Rad;return new Vector3(Mathf.Sin(a)*Radius,y,Mathf.Cos(a)*Radius);}
  public void Build(){
   normal=Mat(new Color(.025f,.055f,.09f,.97f));hover=Mat(new Color(.02f,.28f,.36f,1));pressed=Mat(new Color(.40f,.08f,.65f,1));selected=Mat(new Color(.10f,.20f,.30f,1));
   for(int i=0;i<10;i++){available[i]=true;
    var root=new GameObject("Menu card "+i);root.transform.SetParent(transform,false);roots[i]=root.transform;
    var box=GameObject.CreatePrimitive(PrimitiveType.Cube);box.transform.SetParent(root.transform,false);
    box.transform.localScale=new Vector3(i<4?.17f:.235f,i<4?.085f:.105f,.012f);
    backgrounds[i]=box.GetComponent<Renderer>();backgrounds[i].sharedMaterial=normal;buttons[i]=box.GetComponent<BoxCollider>();
    labels[i]=Text(root.transform,"",new Vector3(0,0,-.009f),i<4?.0038f:.0039f);
   }
   information=new GameObject("Selected tab instructions");information.transform.SetParent(transform,false);
   var backing=GameObject.CreatePrimitive(PrimitiveType.Cube);backing.transform.SetParent(information.transform,false);
   backing.transform.localPosition=new Vector3(0,.125f,Radius+.01f);backing.transform.localScale=new Vector3(.76f,.15f,.012f);backing.GetComponent<Renderer>().sharedMaterial=normal;Remove(backing.GetComponent<Collider>());
   details=Text(information.transform,"",new Vector3(0,.125f,Radius),.0034f);
   var wheel=new GameObject("Bottom menu wheel");wheel.transform.SetParent(transform,false);wheel.transform.localPosition=Hub;
   buttons[10]=wheel.AddComponent<BoxCollider>();buttons[10].size=new Vector3(.16f,.16f,.04f);
   var disc=GameObject.CreatePrimitive(PrimitiveType.Cylinder);disc.transform.SetParent(wheel.transform,false);disc.transform.localRotation=Quaternion.Euler(90,0,0);disc.transform.localScale=new Vector3(.14f,.008f,.14f);Remove(disc.GetComponent<Collider>());
   hubRenderer=disc.GetComponent<Renderer>();hubRenderer.sharedMaterial=selected;
   Text(wheel.transform,"MENU",new Vector3(0,0,-.014f),.0035f);
   Layout();
  }
  public static TextMesh Text(Transform parent,string value,Vector3 position,float size){
   var go=new GameObject("Readable label");go.transform.SetParent(parent,false);go.transform.localPosition=position;
   var text=go.AddComponent<TextMesh>();text.text=value;text.fontSize=64;text.characterSize=size;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;
   var font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.font=font;go.GetComponent<Renderer>().sharedMaterial=font.material;return text;
  }
  static Material Mat(Color c){var m=new Material(Shader.Find("SpatialBuild/TransparentGuidance"));m.color=c;return m;}
  static void Remove(Object o){if(Application.isPlaying)Destroy(o);else DestroyImmediate(o);}
  void Layout(){
   float t=animation*animation*(3-2*animation);
   for(int i=0;i<10;i++){
    bool show=available[i]&&(i<4?animation>.01f:actions&&animation>.95f);
    roots[i].gameObject.SetActive(show);
    if(i<4){float x=(i-1.5f)*.19f;Vector3 destination=new Vector3(x,-.29f-Mathf.Abs(x)*.08f,Radius);
     roots[i].localPosition=Vector3.Lerp(Hub,destination,t);roots[i].localRotation=Quaternion.Euler(0,0,Mathf.Lerp(0,-(i-1.5f)*7,t));
    }else{roots[i].localPosition=new Vector3(((i-4)%3-1)*.25f,-.005f-((i-4)/3)*.12f,Radius);roots[i].localRotation=Quaternion.identity;}
    buttons[i].enabled=show&&animation>.95f;
   }
   information.SetActive(actions&&animation>.95f);
  }
  public int Hover(Ray ray,float dt,out float distance){
   int hitIndex=Hit(ray,out distance);
   if(hitIndex!=10)closedUntilExit=false;
   if(hitIndex==10&&!closedUntilExit)expanded=true;
   if(hitIndex>=0)lastHover=Time.unscaledTime;
   if(expanded&&Time.unscaledTime-lastHover>2.5f){expanded=false;actions=false;}
   animation=Mathf.MoveTowards(animation,expanded?1:0,dt*6);Layout();Physics.SyncTransforms();
   return Hit(ray,out distance);
  }
  int Hit(Ray ray,out float distance){int index=-1;distance=4;
   for(int i=0;i<buttons.Length;i++)if(buttons[i].enabled&&buttons[i].gameObject.activeInHierarchy&&buttons[i].Raycast(ray,out var hit,distance)){index=i;distance=hit.distance;}return index;
  }
  public void OpenTab(){expanded=true;actions=true;lastHover=Time.unscaledTime;}
  public void ToggleWheel(){if(expanded){Collapse();closedUntilExit=true;}else{expanded=true;lastHover=Time.unscaledTime;}}
  public void Collapse(){expanded=false;actions=false;animation=0;Layout();}
  public void ShowForVerification(){expanded=true;actions=true;animation=1;Layout();}
  public void Present(string[] text,int tab,int hovered,bool pinching,string description){
   for(int i=0;i<10;i++){available[i]=!string.IsNullOrEmpty(text[i]);labels[i].text=text[i];backgrounds[i].sharedMaterial=i==hovered?(pinching?pressed:hover):(i==tab?selected:normal);}
   Layout();
   hubRenderer.sharedMaterial=hovered==10?(pinching?pressed:hover):selected;details.text=description;
  }
  void OnDestroy(){foreach(var m in new[]{normal,hover,pressed,selected})if(m)Remove(m);}
 }
}
