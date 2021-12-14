using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct CameraBufferSettings
{
    public bool allowHDR;
    public bool copyDepth;
    public bool copyDepthReflection;
    public bool copyColor;
    public bool copyColorReflection;
    // 渲染缩放的大小
    [Range(0.1f,2.0f)]
    public float renderScale;
}

