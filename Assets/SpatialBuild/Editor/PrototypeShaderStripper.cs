#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;

namespace SpatialBuild {
    // Only the explicitly scoped SpatialBuild build excludes unused ML shader variants.
    // Sample scenes retain their normal shader compilation when built separately.
    public sealed class PrototypeShaderStripper : IPreprocessShaders {
        public int callbackOrder=>10000;
        public void OnProcessShader(Shader shader,ShaderSnippetData snippet,IList<ShaderCompilerData> variants){
            if(SessionState.GetBool("SpatialBuild.PrototypeBuild",false)&&shader.name.StartsWith("Hidden/Sentis/"))variants.Clear();
        }
    }
}
#endif
