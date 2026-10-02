#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using SpatialBuild.Positioning;
using SpatialBuild.Construction;
using SpatialBuild.Diagnostics;

namespace SpatialBuild {
    [InitializeOnLoad] public static class PositioningLabSetup {
        const string ScenePath="Assets/SpatialBuild/Scenes/SpatialBuildPositioningLab.unity";
        static PositioningLabSetup(){EditorApplication.delayCall+=PrepareRequested;}
        static void PrepareRequested(){
            if(!File.Exists("spatialbuild-lab.request"))return;
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating){EditorApplication.delayCall+=PrepareRequested;return;}
            try {RunVerification();CreateScene(false);File.Delete("spatialbuild-lab.request");File.WriteAllText("spatialbuild-lab-ready.txt","COMPILED AND SCENE CREATED\n"+DateTime.UtcNow.ToString("O"));}
            catch(Exception e){File.WriteAllText("spatialbuild-lab-error.txt",e.ToString());Debug.LogException(e);}
        }
        [MenuItem("SpatialBuild/Open positioning lab")]
        public static void OpenLab(){
            if(EditorApplication.isPlaying){Debug.LogWarning("Stop Play mode before opening the lab.");return;}
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            if(!File.Exists(ScenePath))CreateScene(false);
            EditorSceneManager.OpenScene(ScenePath);
        }
        [MenuItem("SpatialBuild/Verify positioning math")]
        public static void RunVerification(){string report=NetworkVerification.Run();File.WriteAllText("spatialbuild-network-verification.txt",report);Debug.Log(report);}
        public static void PrepareBatch(){
            RunVerification();
            if(string.IsNullOrEmpty(SceneManager.GetActiveScene().path)){
                if(!Application.isBatchMode)throw new Exception("Save your current scene before preparing the lab.");
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),"Assets/ValidationEmpty.unity");
            }
            CreateScene(false);
        }
        public static void CreateScene(bool overwrite){
            if(File.Exists(ScenePath)&&!overwrite)return;
            var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
            try {
                if(!AssetDatabase.IsValidFolder("Assets/SpatialBuild/Scenes"))AssetDatabase.CreateFolder("Assets/SpatialBuild","Scenes");
                var camera=new GameObject("Lab camera").AddComponent<Camera>();camera.tag="MainCamera";camera.nearClipPlane=.05f;camera.farClipPlane=100;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.025f,.035f,.055f);
                camera.transform.position=new Vector3(-6,8,-5);camera.transform.LookAt(new Vector3(5,1,4));
                var provider=new GameObject("Simulated positioning provider").AddComponent<SimulatedPositioningProvider>();
                var world=new GameObject("ConstructionWorld").AddComponent<ConstructionWorld>();world.provider=provider;
                var diagnostics=new GameObject("Development diagnostics").AddComponent<NetworkDiagnostics>();diagnostics.simulator=provider;diagnostics.world=world;diagnostics.viewCamera=camera;
                if(!EditorSceneManager.SaveScene(scene,ScenePath))throw new Exception("Could not save lab scene");
            }finally{EditorSceneManager.CloseScene(scene,true);if(previous.IsValid())SceneManager.SetActiveScene(previous);}
            AssetDatabase.SaveAssets();Debug.Log("SPATIALBUILD_LAB_CREATED: "+ScenePath);
        }
    }
}
#endif
