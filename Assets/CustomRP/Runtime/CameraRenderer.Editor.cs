using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using  UnityEngine.Rendering;
using UnityEngine.Rendering.VirtualTexturing;

public partial class CameraRenderer
{
    partial void PrepareBuffer();
    partial void PrepareForSceneWindow();
    partial void DrawUnsupportedShaders();
    partial void DrawGizmos();
    partial void DrawGizmosBeforeFX();
    partial void DrawGizmosAfterFX();
   #if UNITY_EDITOR
    private static ShaderTagId[] legacyShaderTagIds = { new ShaderTagId("Always"),new ShaderTagId("ForwardBase"),  };
    private static Material errorMaterial;
    partial void DrawUnsupportedShaders()
    {

        if (errorMaterial == null)
        {
         //   errorMaterial = new Material(Shader.Find("Hidder/InternalErrorShader"));
            errorMaterial = new Material(Shader.Find("UI/Default"));
        }
        var drawingSettings = new DrawingSettings(legacyShaderTagIds[0], new SortingSettings(camera)) {overrideMaterial = errorMaterial};

        for (int i = 0; i < legacyShaderTagIds.Length; i++)
        {
            drawingSettings.SetShaderPassName(i,legacyShaderTagIds[i]);
        }

        var filteringSettings = FilteringSettings.defaultValue;
        context.DrawRenderers(cullingResults,ref drawingSettings,ref filteringSettings);
    }

    partial void DrawGizmos()
    {
        if (Handles.ShouldRenderGizmos())
        {
            
            context.DrawGizmos(camera,GizmoSubset.PreImageEffects);
            context.DrawGizmos(camera,GizmoSubset.PostImageEffects);
        }
    }


    partial void PrepareForSceneWindow()
    {
        if (camera.cameraType == CameraType.SceneView)
        {
            // 如果切换到Scene视图，调用此方法完成绘制
            ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
            // scene 窗口下  禁用 缩放
            useScaledRendering = false;
        }
    }

    
    partial void DrawGizmosBeforeFX()
    {
        if (Handles.ShouldRenderGizmos())
        {
            if (useIntermediateBuffer)
            {
                Draw(depthAttachmentId,BuiltinRenderTextureType.CameraTarget,true);
                ExecuteCommandBuffer();
            }
            context.DrawGizmos(camera,GizmoSubset.PreImageEffects);
        }
    }
    partial void DrawGizmosAfterFX()
    {
        context.DrawGizmos(camera,GizmoSubset.PostImageEffects);
    }

#endif
    
#if UNITY_EDITOR
    private string SampleName { get; set; }
    partial void PrepareBuffer()
    {
        Profiler.BeginSample("Editor Only");
        buffer.name = SampleName = camera.name;
        Profiler.EndSample();
    }
#else
    const string SampleName = bufferName;
#endif
    

}
