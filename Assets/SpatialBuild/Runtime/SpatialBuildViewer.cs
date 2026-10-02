using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SpatialBuild {
    [Serializable] public class ModelBundle {
        public string schemaVersion, projectId, projectFrame, modelRevision, registrationId, physicalAccuracy;
        public float[] bimToProject, unityBasis;
        public ElementData[] elements;
    }
    [Serializable] public class ElementData {
        public string id, guid, name, ifcType, material, system, trade, category, description, sizeLabel;
        public string sourceFrame, modelRevision, installationStatus;
        public float toleranceMm;
        public float[] positionUnity, designPositionProject, vertices, dimensionsM;
        public int[] triangles;
    }
    [Serializable] public class RegistrationBundle {
        public string sourceFrame, targetFrame, sessionId, calibrationVersion;
        public float[] projectToQuestCanonical; // ROW MAJOR. Right handed Z-up on both sides.
    }
    [Serializable] public class MonitorFrame {
        public string state, session_id, calibration_version;
        public bool synthetic;
    }
    public class ElementIdentity : MonoBehaviour { public ElementData data; }

    public class SpatialBuildViewer : MonoBehaviour {
        public TextAsset modelFile;
        public Camera eye;
        [Tooltip("Inspection preview is amber and never approved for layout.")]
        public bool inspectionPreview = true;
        public bool allowSyntheticReplay = false;
        public float staleAfterSeconds = .5f;
        public string activeSession = "UNREGISTERED";
        public string activeCalibration = "UNREGISTERED";
        public string confidenceState = "UNKNOWN";
        public string status = "INSPECTION ONLY — NOT REGISTERED / DO NOT USE FOR LAYOUT";
        public Transform modelRoot;
        public ModelBundle Bundle { get; private set; }
        private readonly List<MeshRenderer> renderers = new List<MeshRenderer>();
        private TextMesh tooltip, banner;
        private float lastFrameTime = float.NegativeInfinity;
        private string filter = "ALL";
        private ElementIdentity selected;
        private bool expanded;
        private bool registrationValid;

        void Start() {
            if (!eye) eye = Camera.main;
            if (!modelFile) modelFile = Resources.Load<TextAsset>("spatialbuild-model");
            if (!modelFile) { status = "MODEL MISSING"; return; }
            Bundle = JsonUtility.FromJson<ModelBundle>(modelFile.text);
            if (Bundle == null || Bundle.schemaVersion != "spatialbuild.unity.v1" || Bundle.elements == null) {
                status = "UNSUPPORTED MODEL SCHEMA"; return;
            }
            if (!modelRoot) { modelRoot = new GameObject("ProjectModelRoot").transform; modelRoot.SetParent(transform, false); }
            Shader shader = Shader.Find("SpatialBuild/TransparentGuidance");
            if (!shader) { status = "GUIDANCE SHADER MISSING"; return; }
            foreach (var e in Bundle.elements) {
                if (e.vertices == null || e.vertices.Length % 3 != 0 || e.triangles == null || e.positionUnity == null || e.positionUnity.Length != 3)
                    throw new InvalidOperationException("Invalid exported mesh");
                var go = new GameObject(e.name); go.transform.SetParent(modelRoot, false);
                go.transform.localPosition = V(e.positionUnity);
                var vertices = new Vector3[e.vertices.Length / 3];
                for (int i = 0; i < vertices.Length; i++) vertices[i] = new Vector3(e.vertices[3*i], e.vertices[3*i+1], e.vertices[3*i+2]);
                var mesh = new Mesh { name = e.name, indexFormat = IndexFormat.UInt32 };
                mesh.vertices = vertices; mesh.triangles = e.triangles; mesh.RecalculateNormals(); mesh.RecalculateBounds();
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = new Material(shader); renderers.Add(mr);
                go.AddComponent<MeshCollider>().sharedMesh = mesh;
                go.AddComponent<ElementIdentity>().data = e;
            }
            tooltip = MakeText("Element tooltip", .012f); tooltip.gameObject.SetActive(false);
            banner = MakeText("Confidence warning", .016f);
            RefreshAppearance();
        }
        public static Vector3 V(float[] a) { return new Vector3(a[0], a[1], a[2]); }
        public static Matrix4x4 Basis() {
            Matrix4x4 c = Matrix4x4.identity; c[1,1]=0;c[2,2]=0;c[1,2]=1;c[2,1]=1;return c;
        }
        public static Matrix4x4 ReadRigid(float[] a) {
            if (a == null || a.Length != 16) throw new ArgumentException("16 row-major values required");
            Matrix4x4 m = Matrix4x4.zero;
            for(int i=0;i<16;i++) { if(float.IsNaN(a[i]) || float.IsInfinity(a[i])) throw new ArgumentException("Nonfinite transform"); m[i/4,i%4]=a[i]; }
            if (Mathf.Abs(m[3,0])+Mathf.Abs(m[3,1])+Mathf.Abs(m[3,2])+Mathf.Abs(m[3,3]-1)>1e-5f) throw new ArgumentException("Bad homogeneous row");
            for(int i=0;i<3;i++) for(int j=0;j<3;j++) {
                float dot=Vector3.Dot(m.GetColumn(i),m.GetColumn(j));
                if(Mathf.Abs(dot-(i==j?1:0))>1e-4f) throw new ArgumentException("Scale/shear forbidden");
            }
            if(Mathf.Abs(m.determinant-1)>1e-4f) throw new ArgumentException("Reflection forbidden");
            return m;
        }
        public void ApplyRegistrationJson(string json) {
            // Explicit operator-controlled registration; never estimate alignment from graphics.
            var r=JsonUtility.FromJson<RegistrationBundle>(json);
            if(Bundle==null || r.sourceFrame!=Bundle.projectFrame || string.IsNullOrEmpty(r.sessionId) || r.targetFrame!="quest:"+r.sessionId || string.IsNullOrEmpty(r.calibrationVersion))
                throw new ArgumentException("Registration frame/session mismatch");
            var c=Basis();var u=c*ReadRigid(r.projectToQuestCanonical)*c.transpose;
            modelRoot.SetPositionAndRotation(u.GetColumn(3),Quaternion.LookRotation(u.GetColumn(2),u.GetColumn(1)));
            activeSession=r.sessionId;activeCalibration=r.calibrationVersion;registrationValid=true;
            Invalidate("New registration: independent accuracy validation still required",false);
            Debug.Log("SpatialBuild transform applied: "+json);
        }
        public void ApplyMonitorJson(string json) {
            var f=JsonUtility.FromJson<MonitorFrame>(json);
            if(!registrationValid || f.session_id!=activeSession || f.calibration_version!=activeCalibration) { Invalidate("Stale session/calibration",true);return; }
            lastFrameTime=Time.realtimeSinceStartup;
            if(f.state=="RED") { confidenceState="RED";status="REGISTRATION INVALID — DO NOT BUILD"; }
            else if(f.state=="GREEN" && f.synthetic && allowSyntheticReplay) {confidenceState="GREEN";status="SIMULATED GREEN — NOT PHYSICAL VALIDATION";}
            else {confidenceState="UNKNOWN";status="POSITIONING UNAVAILABLE — DO NOT USE FOR LAYOUT";}
            RefreshAppearance();
        }
        public void Invalidate(string reason,bool resetRegistration=true) {
            if(resetRegistration) registrationValid=false;
            confidenceState="UNKNOWN";lastFrameTime=float.NegativeInfinity;status=reason+" / DO NOT USE FOR LAYOUT";
            RefreshAppearance();
        }
        public void SetCategory(string category) {filter=category;RefreshAppearance();}
        public void SetInspectionPreview(bool enabled) {inspectionPreview=enabled;RefreshAppearance();}
        void RefreshAppearance() {
            bool show=confidenceState!="UNKNOWN" || inspectionPreview;
            Color color=confidenceState=="RED"?new Color(1,.15f,.12f,.45f):confidenceState=="GREEN"?new Color(.15f,1,.45f,.45f):new Color(1,.65f,.1f,.25f);
            foreach(var mr in renderers) {
                bool visible=show && (filter=="ALL" || mr.GetComponent<ElementIdentity>().data.category==filter);
                mr.enabled=visible;mr.GetComponent<Collider>().enabled=visible;mr.sharedMaterial.color=color;
            }
            if(tooltip && (!show || (selected && filter!="ALL" && selected.data.category!=filter))) tooltip.gameObject.SetActive(false);
        }
        public void SelectRay(Ray ray) {
            if(!Physics.Raycast(ray,out RaycastHit hit,30f)) return;
            var id=hit.collider.GetComponent<ElementIdentity>();if(!id)return;
            expanded=id==selected?!expanded:false;selected=id;
            var e=id.data;
            tooltip.text=e.name+" · "+e.description+"\n"+e.material+"\n"+e.system+" · "+e.sizeLabel+
                "\nElevation reference: "+e.designPositionProject[2].ToString("F3")+" m"+
                "\nTolerance: "+e.toleranceMm+" mm radial · "+e.installationStatus+
                "\n"+status;
            if(expanded) tooltip.text+="\nIFC: "+e.guid+"\nRevision: "+e.modelRevision+"\nDrawing/detail/RFI: NOT PROVIDED";
            tooltip.transform.position=hit.point+Vector3.up*.15f;tooltip.gameObject.SetActive(true);
        }
        TextMesh MakeText(string name,float scale) {
            var g=new GameObject(name);var t=g.AddComponent<TextMesh>();t.fontSize=40;t.characterSize=scale;t.anchor=TextAnchor.MiddleCenter;t.alignment=TextAlignment.Center;t.color=Color.white;
            var font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.font=font;g.GetComponent<MeshRenderer>().sharedMaterial=font.material;return t;
        }
        void LateUpdate() {
            if(!eye)eye=Camera.main;
            if(confidenceState!="UNKNOWN" && Time.realtimeSinceStartup-lastFrameTime>Mathf.Max(.05f,staleAfterSeconds)) Invalidate("Reference observations expired",false);
            if(!eye)return;
            if(banner) {banner.text=status+(confidenceState=="UNKNOWN" && inspectionPreview?"\nAMBER INSPECTION PREVIEW ONLY":"");banner.transform.position=eye.transform.position+eye.transform.forward*1.5f+Vector3.up*.3f;banner.transform.rotation=Quaternion.LookRotation(banner.transform.position-eye.transform.position);}
            if(tooltip && tooltip.gameObject.activeSelf) tooltip.transform.rotation=Quaternion.LookRotation(tooltip.transform.position-eye.transform.position);
            #if ENABLE_LEGACY_INPUT_MANAGER
            if(Input.GetMouseButtonDown(0)) SelectRay(eye.ScreenPointToRay(Input.mousePosition));
            #endif
        }
        void OnApplicationPause(bool paused) {if(paused)Invalidate("Application paused / tracking session must be renewed");}
        void OnApplicationFocus(bool focused) {if(!focused)Invalidate("Application lost focus / tracking session must be renewed");}
        void OnDestroy() {foreach(var mr in renderers)if(mr){Destroy(mr.sharedMaterial);Destroy(mr.GetComponent<MeshFilter>().sharedMesh);}if(tooltip)Destroy(tooltip.gameObject);if(banner)Destroy(banner.gameObject);}
    }
}
