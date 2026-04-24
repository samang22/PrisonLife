#if UNITY_EDITOR
using UnityEditor;

/// 에디터가 로드될 때 URP_Renderer에 RockFieldInstancedRenderFeature 가 없으면 붙입니다.
/// 그래야 RockFieldInstancedSrpState + SRP Pass 로만 GPU 암석이 그려질 수 있습니다.
[InitializeOnLoad]
static class RockFieldUrpFeatureAutoRegister
{
    static RockFieldUrpFeatureAutoRegister()
    {
        EditorApplication.delayCall += () => PrisonLifeRockFieldMenu.TryEnsureRockFieldFeatureOnUrp();
    }
}
#endif
