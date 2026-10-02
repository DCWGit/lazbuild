using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SpatialBuild.Coordinates;
using UnityEngine;

namespace SpatialBuild.Bim {
 // Mesh vertices are already relative to their IFC element anchor; the root is
 // ProjectOrigin-relative and is the only object moved by localization.
 public sealed class BimModelView : MonoBehaviour {
  public ProjectManifest Project {get;private set;}
  public RigidD ProjectFromBim {get;private set;}=RigidD.Identity;
  public Transform ModelRoot {get;private set;}
  public string Filter {get;private set;}="ALL";
  public bool ElevationEnabled {get;private set;}
  public double ElevationM {get;private set;}
  public double HalfRangeM {get;private set;}=.5;
  public string SelectedInfo {get;private set;}="";
  public bool Visible {get;private set;}
  readonly List<Entry> entries=new List<Entry>();
  readonly Dictionary<string,byte[]> buffers=new Dictionary<string,byte[]>();
  Material validMaterial,invalidMaterial,previewMaterial;
  Material currentMaterial;
  Coroutine loading;
  struct Entry {public SpatialElement element;public GameObject gameObject;public Mesh mesh;}
  public bool Load(string path){
   var asset=Resources.Load<TextAsset>(path);if(!asset){Debug.LogError("SpatialBuild: project asset missing: "+path);return false;}
   Unload();Project=JsonUtility.FromJson<ProjectManifest>(asset.text);
   if(Project==null||Project.schemaVersion!="spatialbuild.project.v2"||Project.units!="metres"||Project.projectOrigin==null||Project.projectOrigin.Length!=3||Project.elements==null){Debug.LogError("SpatialBuild: project manifest invalid");Project=null;return false;}
   try{ProjectFromBim=MatrixFrames.FromRowMajor(Project.bimToProject);}catch(Exception e){Debug.LogError("SpatialBuild: invalid BIM to project frame: "+e.Message);Project=null;return false;}
   var go=new GameObject("IFC Project - "+Project.projectId);ModelRoot=go.transform;ModelRoot.SetParent(transform,false);
   validMaterial=MaterialFor(new Color(.12f,.95f,.4f,.42f));invalidMaterial=MaterialFor(new Color(1,.11f,.08f,.55f));previewMaterial=MaterialFor(new Color(.35f,.62f,1,.24f));currentMaterial=invalidMaterial;
   loading=StartCoroutine(CreateMeshes());return true;
  }
  static Material MaterialFor(Color color){var m=new Material(Shader.Find("SpatialBuild/TransparentGuidance"));m.color=color;return m;}
  IEnumerator CreateMeshes(){
   int count=0;
   foreach(var element in Project.elements){
    if(!element.geometryAvailable||element.anchor==null||element.anchor.Length!=3)continue;
    if(!buffers.TryGetValue(element.geometryReference,out var bytes)){
     var asset=Resources.Load<TextAsset>(element.geometryReference);if(!asset){Debug.LogError("Missing IFC mesh bytes: "+element.geometryReference);continue;}
     bytes=asset.bytes;buffers.Add(element.geometryReference,bytes);
    }
    int vertexBytes=checked(element.vertexCount*12),indexBytes=checked(element.indexCount*4);
    if(element.byteOffset<0||element.byteOffset+vertexBytes+indexBytes>bytes.Length||element.vertexCount>1000000||element.indexCount>3000000){Debug.LogError("Invalid IFC mesh bounds "+element.id);continue;}
    var verts=new Vector3[element.vertexCount];var tris=new int[element.indexCount];
    for(int i=0;i<verts.Length;i++){int o=element.byteOffset+i*12;verts[i]=new Vector3(BitConverter.ToSingle(bytes,o),BitConverter.ToSingle(bytes,o+4),BitConverter.ToSingle(bytes,o+8));}
    for(int i=0;i<tris.Length;i++)tris[i]=BitConverter.ToInt32(bytes,element.byteOffset+vertexBytes+i*4);
    var mesh=new Mesh {indexFormat=verts.Length>65535?UnityEngine.Rendering.IndexFormat.UInt32:UnityEngine.Rendering.IndexFormat.UInt16};mesh.vertices=verts;mesh.triangles=tris;mesh.RecalculateBounds();
    var go=new GameObject(element.id);go.transform.SetParent(ModelRoot,false);
    var anchor=ProjectFromBim.Apply(new DVec(element.anchor[0],element.anchor[1],element.anchor[2]));
    go.transform.localPosition=UnityFrames.ToUnity(anchor-new DVec(Project.projectOrigin[0],Project.projectOrigin[1],Project.projectOrigin[2]));
    go.transform.localRotation=UnityFrames.ToUnity(ProjectFromBim.rotation);
    go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=currentMaterial;
    entries.Add(new Entry{element=element,gameObject=go,mesh=mesh});go.SetActive(Show(element));
    if(++count%64==0)yield return null;
   }
   loading=null;Debug.Log("SpatialBuild: IFC renderable meshes loaded: "+count+" / "+Project.elements.Length);
  }
  public void SetVisible(bool value){if(Visible==value)return;Visible=value;RefreshVisibility();}
  bool Show(SpatialElement element)=>Visible&&(Filter=="ALL"||element.discipline==Filter)&&(!ElevationEnabled||element.boundsMin==null||element.boundsMax==null||element.boundsMin[2]<=ElevationM+HalfRangeM&&element.boundsMax[2]>=ElevationM-HalfRangeM);
  void RefreshVisibility(){foreach(var entry in entries)entry.gameObject.SetActive(Show(entry.element));}
  public void SetFilter(string discipline){if(Filter==discipline)return;Filter=discipline;RefreshVisibility();}
  public void SetElevation(bool enabled,double centre,double halfRange){ElevationEnabled=enabled;ElevationM=centre;HalfRangeM=Math.Max(0,halfRange);RefreshVisibility();}
  public void SetTrust(bool valid,bool preview){var mat=valid?validMaterial:preview?previewMaterial:invalidMaterial;if(currentMaterial==mat)return;currentMaterial=mat;foreach(var entry in entries)entry.gameObject.GetComponent<MeshRenderer>().sharedMaterial=mat;}
  public void SetProjectPose(RigidD questFromProject){if(Project==null||!ModelRoot)return;var origin=new DVec(Project.projectOrigin[0],Project.projectOrigin[1],Project.projectOrigin[2]);UnityFrames.Apply(ModelRoot,questFromProject*new RigidD(origin,DQuat.Identity));}
  public string Select(Ray ray){
   // Selection is deliberately bounded; colliders are created only after a user asks.
   float best=float.PositiveInfinity;SpatialElement picked=null;
   foreach(var entry in entries){if(!entry.gameObject.activeInHierarchy)continue;
    var bounds=entry.gameObject.GetComponent<MeshRenderer>().bounds;
    if(bounds.IntersectRay(ray,out var distance)&&distance<best){best=distance;picked=entry.element;}
   }
   SelectedInfo=picked==null?"No IFC element in view":picked.discipline+" | "+picked.name+"\n"+picked.category+" | "+picked.sourceGuid;
   return SelectedInfo;
  }
  public void Unload(){
   if(loading!=null){StopCoroutine(loading);loading=null;}
   foreach(var entry in entries)if(entry.mesh)Destroy(entry.mesh);entries.Clear();buffers.Clear();
   if(ModelRoot)Destroy(ModelRoot.gameObject);ModelRoot=null;Project=null;
   foreach(var material in new[]{validMaterial,invalidMaterial,previewMaterial})if(material)Destroy(material);
  }
  void OnDestroy(){Unload();}
 }
}
