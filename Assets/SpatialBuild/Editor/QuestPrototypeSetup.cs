#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using SpatialBuild.Positioning;
using SpatialBuild.Construction;
using SpatialBuild.Quest;
using SpatialBuild.Bim;
using SpatialBuild.Coordinates;
using SpatialBuild.Localization;

namespace SpatialBuild {
    public static class QuestPrototypeSetup {
        const string ScenePath="Assets/SpatialBuild/Scenes/SpatialBuildQuest.unity";
        [MenuItem("SpatialBuild/Open Quest prototype")]
        public static void Open(){if(EditorApplication.isPlaying)return;if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;Prepare();EditorSceneManager.OpenScene(ScenePath);}
        public static void Prepare(){
            Verify();
            VerifyInterface();
            VerifyJob();
            VerifyImportedProject();
            if(!File.Exists(ScenePath)){
                var previous=SceneManager.GetActiveScene();
                if(string.IsNullOrEmpty(previous.path)&&Application.isBatchMode)EditorSceneManager.SaveScene(previous,"Assets/ValidationEmpty.unity");
                var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
                try {
                    var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.meta.xr.sdk.core/Prefabs/OVRCameraRig.prefab");
                    if(!prefab)throw new Exception("Meta camera rig prefab unavailable");
                    var rigGo=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);var rig=rigGo.GetComponent<OVRCameraRig>();var manager=rigGo.GetComponent<OVRManager>();
                    manager.isInsightPassthroughEnabled=true;manager.trackingOriginType=OVRManager.TrackingOrigin.Stage;
                    var passthrough=new GameObject("Passthrough");passthrough.AddComponent<OVRPassthroughLayer>();
                    foreach(var camera in rigGo.GetComponentsInChildren<Camera>(true)){camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;camera.nearClipPlane=.05f;camera.farClipPlane=50;}
                    rig.centerEyeAnchor.tag="MainCamera";RenderSettings.skybox=null;
                    var world=new GameObject("ConstructionWorld").AddComponent<ConstructionWorld>();world.displayEnabled=false;
                    var controller=new GameObject("Manual project registration").AddComponent<QuestRegistrationController>();controller.world=world;controller.trackingSpace=rig.trackingSpace;controller.eye=rig.centerEyeAnchor;
                    if(!EditorSceneManager.SaveScene(scene,ScenePath))throw new Exception("Quest scene save failed");
                }finally{EditorSceneManager.CloseScene(scene,true);if(previous.IsValid())SceneManager.SetActiveScene(previous);}
            }
            PrepareHands();
            var graphics=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);var shaders=graphics.FindProperty("m_AlwaysIncludedShaders");
            var shader=Shader.Find("SpatialBuild/TransparentGuidance");if(!shader)throw new Exception("Guidance shader missing");bool has=false;
            for(int i=0;i<shaders.arraySize;i++)has|=shaders.GetArrayElementAtIndex(i).objectReferenceValue==shader;
            if(!has){shaders.InsertArrayElementAtIndex(shaders.arraySize);shaders.GetArrayElementAtIndex(shaders.arraySize-1).objectReferenceValue=shader;graphics.ApplyModifiedProperties();}
            AssetDatabase.SaveAssets();File.WriteAllText("spatialbuild-quest-ready.txt","Quest scene prepared; registration math verified. No physical validation.\n"+DateTime.UtcNow.ToString("O"));
        }
        static void PrepareHands(){
            var config=OVRProjectConfig.CachedProjectConfig;
            config.handTrackingSupport=OVRProjectConfig.HandTrackingSupport.ControllersAndHands;
            OVRProjectConfig.CommitProjectConfig(config);
            var previous=SceneManager.GetActiveScene();
            var scene=SceneManager.GetSceneByPath(ScenePath);bool opened=!scene.isLoaded;
            if(opened)scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
            try {
                OVRCameraRig rig=null;QuestRegistrationController registration=null;
                foreach(var root in scene.GetRootGameObjects()){
                    if(!rig)rig=root.GetComponentInChildren<OVRCameraRig>(true);
                    if(!registration)registration=root.GetComponentInChildren<QuestRegistrationController>(true);
                }
                if(!rig||!registration)throw new Exception("Quest rig or registration missing");
                var right=ConfigureHand(rig.rightHandAnchor,rig.trackingSpace,OVRHand.Hand.HandRight);
                ConfigureHand(rig.leftHandAnchor,rig.trackingSpace,OVRHand.Hand.HandLeft);
                var controls=registration.GetComponent<HandPreviewControls>();if(!controls)controls=registration.gameObject.AddComponent<HandPreviewControls>();
                controls.world=registration.world;controls.eye=rig.centerEyeAnchor;controls.trackingSpace=rig.trackingSpace;controls.rightHand=right;
                registration.handControls=controls;
                var field=registration.GetComponent<FieldProjectController>();if(!field)field=registration.gameObject.AddComponent<FieldProjectController>();
                field.developerDemo=registration;field.eye=rig.centerEyeAnchor;field.trackingSpace=rig.trackingSpace;field.rightHand=right;
                controls.field=field;
                if(!EditorSceneManager.SaveScene(scene,ScenePath))throw new Exception("Hand scene save failed");
            }finally {if(opened)EditorSceneManager.CloseScene(scene,true);if(previous.IsValid())SceneManager.SetActiveScene(previous);}
        }
        static OVRHand ConfigureHand(Transform anchor,Transform trackingSpace,OVRHand.Hand type){
            var hand=anchor.GetComponentInChildren<OVRHand>(true);
            if(!hand){var go=new GameObject("SpatialBuild hand input");go.transform.SetParent(anchor,false);hand=go.AddComponent<OVRHand>();}
            var serialized=new SerializedObject(hand);serialized.FindProperty("HandType").intValue=(int)type;
            serialized.FindProperty("_pointerPoseRoot").objectReferenceValue=trackingSpace;serialized.ApplyModifiedPropertiesWithoutUndo();return hand;
        }
        [MenuItem("SpatialBuild/Verify manual registration")]
        public static void Verify(){
            var p=new[]{Vector3.zero,new Vector3(2,0,0),new Vector3(0,0,2)};var r=Quaternion.Euler(8,37,-4);var t=new Vector3(3,1,-5);var q=new Vector3[3];for(int i=0;i<3;i++)q[i]=r*p[i]+t;
            Require(ManualRegistration.TrySolve(p,q,.001f,out var fit,out var rms,out var reason),reason);
            Require(Vector3.Distance(t,fit.position)<.00001f&&Quaternion.Angle(r,fit.rotation)<.01f,"Rigid recovery");
            Require(Vector3.Distance(fit.position+fit.rotation*new Vector3(2,0,2),t+r*new Vector3(2,0,2))<.00001f,"Independent fourth point");
            var line=new[]{Vector3.zero,Vector3.right,Vector3.right*2};Require(!ManualRegistration.TrySolve(line,line,.01f,out _,out _,out _),"Reject collinear controls");
            q[1]+=Vector3.right*.5f;Require(!ManualRegistration.TrySolve(p,q,.03f,out _,out _,out _),"Reject inconsistent controls");
            q[0]=new Vector3(float.NaN,0,0);Require(!ManualRegistration.TrySolve(p,q,.03f,out _,out _,out _),"Reject nonfinite controls");
            var testObject=new GameObject("Registration verification"){hideFlags=HideFlags.HideAndDontSave};
            var worldObject=new GameObject("Verification world"){hideFlags=HideFlags.HideAndDontSave};
            try {
                var world=worldObject.AddComponent<ConstructionWorld>();world.displayEnabled=false;
                var controller=testObject.AddComponent<QuestRegistrationController>();controller.world=world;
                for(int i=0;i<3;i++)Require(controller.CaptureProbe(r*p[i]+t),"Capture control "+i);
                Require(controller.aligned&&world.displayEnabled,"Alignment enables inspection");
                Require(controller.CaptureProbe(r*new Vector3(2,0,2)+t),"Held-out controller check");
                Require(!controller.CaptureProbe(r*new Vector3(2,0,2)+t+Vector3.up*.2f),"Bad independent check rejected");
                Require(!controller.aligned&&!world.displayEnabled,"Bad check hides geometry");
                for(int i=0;i<3;i++)controller.CaptureProbe(r*p[i]+t);
                controller.Invalidate("Synthetic tracking loss");Require(!controller.aligned&&!world.displayEnabled,"Tracking invalidation clears alignment");
                Require(!controller.CaptureProbe(new Vector3(float.NaN,0,0)),"Nonfinite probe rejected immediately");
                var preview=new HeadsetPreview();
                testObject.transform.SetPositionAndRotation(new Vector3(2,1.6f,3),Quaternion.Euler(0,31,0));
                preview.Place(world,testObject.transform,worldObject.transform,false);
                Require(preview.placed&&!preview.fullScale&&Mathf.Abs(world.transform.localScale.x-.1f)<.0001f,"Miniature scale");
                Vector3 centre=world.transform.TransformPoint(new Vector3(5,0,4));preview.Rotate(world);
                Require(Vector3.Distance(centre,world.transform.TransformPoint(new Vector3(5,0,4)))<.0001f,"Rotation preserves preview centre");
                preview.Place(world,testObject.transform,testObject.transform,true);
                Require(preview.fullScale&&world.transform.localScale==Vector3.one,"Full scale preview");
                preview.Reset(world);Require(!preview.placed&&!world.displayEnabled&&world.transform.localScale==Vector3.one,"Preview reset hides and restores scale");
                var gate=new PinchGate();
                Require(!gate.Pressed(true,true),"No action on first tracked held pinch");
                gate.Pressed(true,false);Require(gate.Pressed(true,true),"Released then pinched activates");
                Require(!gate.Pressed(true,true),"Held pinch does not repeat");
                gate.Pressed(false,false);Require(!gate.Pressed(true,true),"Tracking recovery requires release");
            }finally{UnityEngine.Object.DestroyImmediate(testObject);UnityEngine.Object.DestroyImmediate(worldObject);}
            File.WriteAllText("spatialbuild-registration-verification.txt","PASS: rigid pose recovery, held-out fourth point, collinearity rejection, inconsistent-control rejection, nonfinite rejection, capture sequence, independent-check pass/fail, invalidation hides geometry, nonfinite probe rejected immediately. Synthetic controls only; no headset events exercised.");
            File.WriteAllText("spatialbuild-hand-verification.txt","PASS: miniature/full-scale placement, rotation centre invariance, reset hides geometry and restores scale, pinch release-to-arm, held-pinch suppression and tracking-recovery suppression. Physical hand interaction still requires headset validation.");
        }
        static void Require(bool condition,string reason){if(!condition)throw new Exception("Registration verification failed: "+reason);}
        public static void VerifyImportedProject(){
            var asset=Resources.Load<TextAsset>("DentalClinic/project");Require(asset!=null,"Imported IFC manifest available");
            var project=JsonUtility.FromJson<ProjectManifest>(asset.text);
            Require(project!=null&&project.schemaVersion=="spatialbuild.project.v2"&&project.units=="metres","Normalized IFC manifest schema");
            Require(project.elements!=null&&project.elements.Length==19988,"Full GUID-linked IFC catalog");
            int renderable=0,mep=0;foreach(var element in project.elements){if(element.geometryAvailable)renderable++;if(element.id.StartsWith("mep:")){mep++;Require(!element.geometryAvailable,"MEP geometry must not be fabricated");}}
            Require(renderable==3933&&mep==16012,"IFC geometry counts");
            var matrix=MatrixFrames.FromRowMajor(project.bimToProject);Require((matrix.Apply(new DVec(1,2,3))-new DVec(1,2,3)).Norm<1e-12,"BIM/project transform");
            var profileAsset=Resources.Load<TextAsset>("DentalClinic/control-profile");Require(profileAsset!=null,"Control profile available");
            var profile=JsonUtility.FromJson<ControlProfile>(profileAsset.text);
            Require(profile.projectRevision==project.revision&&profile.controls!=null&&profile.controls.Length==0,"Field starts without fabricated survey controls");
            File.WriteAllText("spatialbuild-ifc-verification.txt","PASS: 19988 IFC elements, 3933 arc/str meshes, 16012 MEP metadata-only entries, profile revision match, no configured physical control. No physical accuracy evidence.");
        }
        public static void VerifyJob(){
            var asset=Resources.Load<TextAsset>("demo-pipe-job");Require(asset!=null,"Demo job asset exists");
            var job=new ConstructionJob(JsonUtility.FromJson<JobDefinition>(asset.text));
            Require(job.definition.steps.Length==6&&job.Current.targetId=="H184","Job starts at first anchor");
            Require(job.Materials().Contains("5 Demo hanger anchor")&&job.Materials().Contains("6 2 inch copper pipe"),"Initial material quantities");
            job.Next();Require(job.Current.targetId=="H185"&&job.done.Count==0,"Skip is not completion");
            job.MarkDone();Require(job.done.Count==1&&job.Current.targetId=="H186","Done advances and counts once");
            Require(job.Materials().Contains("4 Demo hanger anchor"),"Done reduces remaining material count");
            Require(job.Undo()&&job.Current.targetId=="H185"&&job.done.Count==0,"Undo restores material and target");
            for(int i=0;i<6;i++)Require(job.MarkDone(),"Complete step "+i);
            Require(job.Current==null&&job.done.Count==6&&!job.MarkDone(),"Complete job cannot overcount");
            Require(job.Undo()&&job.Current!=null&&job.done.Count==5,"Undo works after job complete");
            File.WriteAllText("spatialbuild-job-verification.txt","PASS: six-step job loading, material quantities, skip does not complete, completion advances, undo restores target/materials, completion bounds. Completion is user-reported; no construction validation.");
        }
        public static void VerifyInterface(){
            var go=new GameObject("UI verification"){hideFlags=HideFlags.HideAndDontSave};
            var cameraGo=new GameObject("UI verification camera"){hideFlags=HideFlags.HideAndDontSave};
            RenderTexture target=null;Texture2D capture=null;var previous=RenderTexture.active;
            try {
                var panel=go.AddComponent<CurvedPreviewPanel>();panel.Build();
                Require(!panel.buttons[0].gameObject.activeInHierarchy&&panel.buttons[10].gameObject.activeInHierarchy,"Collapsed menu shows only wheel");
                panel.ShowForVerification();
                panel.Present(new[]{"JOB","LAYERS","PLACE","CHECK","START\ndemo job","DONE\nnext piece","UNDO\nlast done","SKIP\nfor now","MATERIALS\nremaining","ONLY\nnext piece"},0,5,true,"H184 | HANGER ANCHOR HERE\n0 / 6 marked done (by you)\nStart opens a full-size demo. Not registered.");
                Physics.SyncTransforms();
                foreach(var button in panel.buttons){
                    var p=button.transform.position;
                    Require(Mathf.Abs(Mathf.Atan2(p.x,p.z)*Mathf.Rad2Deg)<20&&Mathf.Abs(Mathf.Atan2(p.y,p.z)*Mathf.Rad2Deg)<25,"Menu targets remain in central lower view");
                    Require(button.Raycast(new Ray(Vector3.zero,p.normalized),out _,2),"Menu button can be targeted");
                }
                var trial=new FieldTrialSession();Require(!trial.Record(1,go.transform),"No check before reference shown");
                trial.checkMode=true;Require(!trial.Record(.1f,go.transform),"No miniature measurement record");
                Require(!trial.Record(1,go.transform),"No implicit zero measurement record");
                Require(Vector3.Distance(FieldTrialSession.CheckPoint(0),FieldTrialSession.CheckPoint(3))>2.8f,"Independent diagonal checkpoint");
                var camera=cameraGo.AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.08f,.10f,.14f);camera.fieldOfView=68;
                target=new RenderTexture(1500,1000,24);camera.targetTexture=target;camera.Render();RenderTexture.active=target;
                capture=new Texture2D(1500,1000,TextureFormat.RGB24,false);capture.ReadPixels(new Rect(0,0,1500,1000),0,0);capture.Apply();
                Directory.CreateDirectory("Builds");File.WriteAllBytes("Builds/ui-preview.png",capture.EncodeToPNG());
                File.WriteAllText("spatialbuild-ui-verification.txt","PASS: collapsed wheel, eleven button hit targets within central/lower view, reject hidden-reference/miniature/unentered check records, independent checkpoint geometry. Rendered Builds/ui-preview.png; headset comfort still needs user validation.");
            }finally{RenderTexture.active=previous;if(target)UnityEngine.Object.DestroyImmediate(target);if(capture)UnityEngine.Object.DestroyImmediate(capture);UnityEngine.Object.DestroyImmediate(cameraGo);UnityEngine.Object.DestroyImmediate(go);}
        }
        [MenuItem("SpatialBuild/Build Quest APK")]
        public static void Build(){
            Prepare();
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android,"com.spatialbuild.prototype");PlayerSettings.productName="SpatialBuild Prototype";
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android,ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel32;
            PlayerSettings.Android.targetSdkVersion=AndroidSdkVersions.AndroidApiLevel34;
            Directory.CreateDirectory("Builds");
            BuildReport report;
            SessionState.SetBool("SpatialBuild.PrototypeBuild",true);
            try {report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName="Builds/SpatialBuildQuest.apk",target=BuildTarget.Android,options=BuildOptions.Development});}
            finally{SessionState.SetBool("SpatialBuild.PrototypeBuild",false);}
            File.WriteAllText("spatialbuild-android-build.txt",report.summary.result+"\nErrors: "+report.summary.totalErrors+"\nWarnings: "+report.summary.totalWarnings+"\nBytes: "+report.summary.totalSize+"\n"+DateTime.UtcNow.ToString("O"));
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Android build failed; see log");
        }
    }
}
#endif
