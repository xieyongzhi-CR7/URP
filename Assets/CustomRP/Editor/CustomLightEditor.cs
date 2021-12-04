
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

[CanEditMultipleObjects]
[CustomEditorForRenderPipeline(typeof(Light),typeof(CustomRenderPineAsset))]
public class CustomLightEditor : LightEditor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        //DrawRenderingLayerMask();
        RenderingLayerMaskDrawer.Draw(settings.renderingLayerMask,renderingLayerMaskLabel);
        if (!settings.lightType.hasMultipleDifferentValues && (LightType)settings.lightType.enumValueIndex == LightType.Spot)
        {
            settings.DrawInnerAndOuterSpotAngle();
            
        }
        settings.ApplyModifiedProperties();
        var light = target as Light;
        if (light.cullingMask != -1)
        {
            EditorGUILayout.HelpBox("Culling Mask only affects shadows.",MessageType.Warning);
        }
    }
    
    private static GUIContent renderingLayerMaskLabel =
        new GUIContent("Rendering Layer Mask", "Functional version of above property");


    void DrawRenderingLayerMask()
    {
        SerializedProperty property = settings.renderingLayerMask;
        EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
        EditorGUI.BeginChangeCheck();
        int mask = property.intValue;
        if (mask == int.MaxValue)
        {
            mask = -1;
        }
        mask = EditorGUILayout.MaskField(renderingLayerMaskLabel, mask,
            GraphicsSettings.currentRenderPipeline.renderingLayerMaskNames);
        if (EditorGUI.EndChangeCheck())
        {
            property.intValue = mask == -1 ? int.MaxValue : mask;
        }
        EditorGUI.showMixedValue = false;
    }
}
