#if UNITY_EDITOR
using UnityEditor;

/// 에디터가 로드될 때 URP_Renderer에 RockFieldInstancedRenderFeature 가 없으면 붙입니다.
/// (RenderFeature 는 Missing script 방지용 no-op. 실제 그리기는 <see cref="Graphics.DrawMeshInstanced"/>)
[InitializeOnLoad]
static class RockFieldUrpFeatureAutoRegister
{
    static RockFieldUrpFeatureAutoRegister()
    {
        EditorApplication.delayCall += () => PrisonLifeRockFieldMenu.TryEnsureRockFieldFeatureOnUrp();
    }
}
#endif
