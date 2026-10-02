#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpatialBuild {
    public static class SpatialBuildProjectSetup {
        public static void PrepareDesktop() {
            SpatialBuildSceneBuilder.CreateInspectionScene();
            if (!AssetDatabase.IsValidFolder("Assets/SpatialBuild/Scenes"))
                AssetDatabase.CreateFolder("Assets/SpatialBuild", "Scenes");
            const string path = "Assets/SpatialBuild/Scenes/SpatialBuildInspection.unity";
            if (!EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), path))
                throw new System.Exception("Could not save inspection scene");
            var shader = Shader.Find("SpatialBuild/TransparentGuidance");
            if (!shader) throw new System.Exception("Guidance shader missing");
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
            var shaders = settings.FindProperty("m_AlwaysIncludedShaders");
            bool present = false;
            for (int i = 0; i < shaders.arraySize; i++)
                present |= shaders.GetArrayElementAtIndex(i).objectReferenceValue == shader;
            if (!present) {
                shaders.InsertArrayElementAtIndex(shaders.arraySize);
                shaders.GetArrayElementAtIndex(shaders.arraySize - 1).objectReferenceValue = shader;
                settings.ApplyModifiedProperties();
            }
            PlayerSettings.productName = "SpatialBuild";
            AssetDatabase.SaveAssets();
            Debug.Log("SPATIALBUILD_SETUP_COMPLETE: " + path);
        }
    }
}
#endif
