using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.Scripting.APIUpdating;


using UnityEngine.Experimental.GlobalIllumination;
using  UnityEngine.Rendering;
using Lightmapping = UnityEngine.Experimental.GlobalIllumination.Lightmapping;
using LightType = UnityEngine.LightType;

public partial class CustomRenderPipeLine
{
    partial void InitializeForEditor();

    partial void DisposeForEditor();

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        DisposeForEditor();
        renderer.Dipose();
    }

#if UNITY_EDITOR
    partial void InitializeForEditor()
    {
       Lightmapping.SetDelegate(lightsDelegate);
    }

    partial void DisposeForEditor()
    {
        //base.Dispose(disposing);
        Lightmapping.ResetDelegate();
    }


    private static Lightmapping.RequestLightsDelegate lightsDelegate = (Light[] lights, NativeArray<LightDataGI> output) =>
    {
        var lightData = new LightDataGI();
        for (int i = 0; i < lights.Length; i++)
        {
            Light light = lights[i];
            switch (light.type)
            {
                case LightType.Directional:
                    var directionalLight = new DirectionalLight();
                    LightmapperUtils.Extract(light, ref directionalLight);
                    lightData.Init(ref directionalLight);
                    break;
                
                case LightType.Spot:
                    var spotLight = new SpotLight();
                    LightmapperUtils.Extract(light, ref spotLight);
                    lightData.Init(ref spotLight);
                    break;
                case LightType.Point:
                    var pointLight = new PointLight();
                    LightmapperUtils.Extract(light, ref pointLight);
                    lightData.Init(ref pointLight);
                    break;
                case LightType.Area:
                    var rectangleLight = new RectangleLight();
                    LightmapperUtils.Extract(light, ref rectangleLight);
                    rectangleLight.mode = LightMode.Baked;
                    lightData.Init(ref rectangleLight);
                    break;
                default:
                    lightData.InitNoBake(light.GetInstanceID());
                    break;
            }

            output[i] = lightData;
        }
    };
#endif
}
