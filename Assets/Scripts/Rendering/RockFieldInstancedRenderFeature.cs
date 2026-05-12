using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Indirect+듀얼패스였을 때 피처 이름 유지용. 패스 비어 있음.
[DisallowMultipleRendererFeature("RockFieldInstancedRenderFeature")]
public class RockFieldInstancedRenderFeature : ScriptableRendererFeature
{
    public override void Create() { }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) { }

    protected override void Dispose(bool disposing) { }
}
