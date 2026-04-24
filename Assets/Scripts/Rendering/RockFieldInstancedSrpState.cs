using UnityEngine;

/// 매 프레임 <see cref="RockFieldInstancedRenderer.LateUpdate"/>가 채우고,
/// <see cref="RockFieldInstancedRenderFeature"/>가 URP Pass에서 읽습니다.
public static class RockFieldInstancedSrpState
{
    public static bool Ready { get; private set; }

    public struct Data
    {
        public Mesh mesh;
        public int submeshIndex;
        public Material material;
        public MaterialPropertyBlock propertyBlock;
        public GraphicsBuffer argsBuffer;
        public int argsOffset;
        public Bounds bounds;
        public Camera drawCamera;
    }

    public static Data Current;

    public static void ClearFrame() => Ready = false;

    public static void PrepareDraw(
        Mesh mesh, int submeshIndex, Material material, MaterialPropertyBlock mpb, GraphicsBuffer args, int argsOffset, Bounds bounds, Camera drawCamera)
    {
        Current = new Data
        {
            mesh = mesh,
            submeshIndex = submeshIndex,
            material = material,
            propertyBlock = mpb,
            argsBuffer = args,
            argsOffset = argsOffset,
            bounds = bounds,
            drawCamera = drawCamera
        };
        Ready = true;
    }
}
