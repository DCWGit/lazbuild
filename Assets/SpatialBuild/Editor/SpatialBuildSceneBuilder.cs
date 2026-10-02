#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace SpatialBuild {
    public static class SpatialBuildSceneBuilder {
        [MenuItem("SpatialBuild/Add model to current scene")]
        public static void AddToScene() {
            if(Object.FindFirstObjectByType<SpatialBuildViewer>()) {Debug.LogWarning("A SpatialBuild viewer already exists.");return;}
            var go=new GameObject("SpatialBuild");Undo.RegisterCreatedObjectUndo(go,"Add SpatialBuild model");
            var viewer=go.AddComponent<SpatialBuildViewer>();viewer.modelFile=Resources.Load<TextAsset>("spatialbuild-model");viewer.inspectionPreview=true;
            Selection.activeObject=go;EditorSceneManager.MarkSceneDirty(go.scene);
            Debug.Log("Add Meta Camera Rig and Passthrough Building Blocks. Assign center-eye camera. Inspector preview remains untrusted.");
        }
        [MenuItem("SpatialBuild/Create desktop inspection scene")]
        public static void CreateInspectionScene() {
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var cameraGo=new GameObject("Inspection camera");cameraGo.tag="MainCamera";
            var camera=cameraGo.AddComponent<Camera>();cameraGo.transform.position=new Vector3(3,1.6f,-2);cameraGo.transform.LookAt(new Vector3(3,2.4f,2));camera.nearClipPlane=.05f;
            AddToScene();Debug.Log("Desktop inspection only. This is not a Quest/AR scene or accuracy test.");
        }
    }
}
#endif
