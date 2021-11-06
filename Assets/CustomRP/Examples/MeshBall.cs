using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SocialPlatforms;
using Random = UnityEngine.Random;

public class MeshBall : MonoBehaviour
{
    private static int baseColorId = Shader.PropertyToID("_BaseColor");
    private static int metallicId = Shader.PropertyToID("_Metallic");
    private static int smoothnessId = Shader.PropertyToID("_Smoothness");
    private static int emissionColorId = Shader.PropertyToID("_EmissionColor");
    [SerializeField]
    private Mesh mesh = default;

    [SerializeField]
    private Material material = default;
    
    float[] metallic = new float[1023];
    float[] smoothness = new float[1023];
    Matrix4x4[] matrices = new Matrix4x4[1023];
    Vector4[] baseColors = new Vector4[1023];
    
    private MaterialPropertyBlock block;


    private void Awake()
    {
        for (int i = 0;  i< matrices.Length; i++)
        {
            // 前2个scene使用
            matrices[i] = Matrix4x4.TRS(Random.insideUnitSphere * 10f,Quaternion.Euler(Random.value*360f,Random.value*360f,Random.value*360f), Vector3.one *Random.Range(0.5f,1.5f));
            baseColors[i] = new Vector4(Random.value,Random.value,Random.value,Random.Range(0.5f,1.0f));
            // 第3个scene使用
            metallic[i] = Random.value < 0.25 ? 1f : 0f;
            smoothness[i] = Random.Range(0.05f, 0.95f);
        }
    }



    // Update is called once per frame
    void Update()
    {
        if (block==null)
        {
            block = new MaterialPropertyBlock();
            block.SetVectorArray(baseColorId,baseColors);
            block.SetVectorArray(emissionColorId,baseColors);
            block.SetFloatArray(smoothnessId,smoothness);
            block.SetFloatArray(metallicId,metallic);
            
            var positions = new Vector3[1023];
            for (int i = 0; i < matrices.Length; i++)
            {
                positions[i] = matrices[i].GetColumn(3);
            }
            //如果环境中不存在烘焙的 LightProbe, unity 会使用ambient Probe 写进lightProbeArray中，用（1，1，1，1）写入occlusionProbesArray    存在和烘焙了光照探针， 
            //如果场景中存在烘焙的光照探针数据  ， 则使用烘焙的探针进行计算 
            
            
            //  使用DrawMeshInstanced接口，lightProbe是可以使用的， 阴影是不能用烘焙的，
            var lightProbes = new SphericalHarmonicsL2[1023];
            var occlusionProbes = new Vector4[1023];
             LightProbes.CalculateInterpolatedLightAndOcclusionProbes(positions,lightProbes,occlusionProbes);
            block.CopySHCoefficientArraysFrom(lightProbes);
            block.CopyProbeOcclusionArrayFrom(occlusionProbes);
        }
        
        Graphics.DrawMeshInstanced(mesh,0,material,matrices,1023,block,ShadowCastingMode.On,true,0,null,LightProbeUsage.CustomProvided);
        //Graphics.DrawMeshInstanced(mesh,0,material,matrices,1023,block);
    }
}
