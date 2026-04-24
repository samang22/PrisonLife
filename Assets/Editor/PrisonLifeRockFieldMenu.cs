// Editor 어셈블리(Assets/Editor)는 런타임 스크립트 타입을 직접 참조하지 못하는 설정이 있을 수 있어
// RockFieldInstancedRenderFeature 이름만으로 Assembly.GetType 을 씁니다. (GetTypes() 예외 루프는 사용하지 않음)
#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public static class PrisonLifeRockFieldMenu
{
    const string kRendererPath = "Assets/URP_Renderer.asset";
    const string kTypeShort = "RockFieldInstancedRenderFeature";

    /// URP_Renderer에 RockFieldInstancedRenderFeature 가 없으면 추가합니다(멱등).
    /// InitializeOnLoad에서 자동 호출 — 수동 Add Renderer Feature가 필요 없어집니다.
    public static bool TryEnsureRockFieldFeatureOnUrp()
    {
        var featureType = FindRockFieldFeatureType();
        if (featureType == null)
        {
            Debug.LogWarning("[PrisonLife] RockFieldInstancedRenderFeature.cs 를 찾지 못해 URP에 기능을 붙이지 못했습니다. Console 컴파일 에러를 확인하세요.");
            return false;
        }

        if (!typeof(ScriptableRendererFeature).IsAssignableFrom(featureType))
            return false;

        var data = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(kRendererPath);
        if (data == null)
        {
            Debug.LogWarning("[PrisonLife] " + kRendererPath + " 를 찾을 수 없어 Rock Render Feature 를 등록하지 못했습니다.");
            return false;
        }

        for (int i = 0; i < data.rendererFeatures.Count; i++)
        {
            var f = data.rendererFeatures[i];
            if (f != null && f.GetType() == featureType)
                return true;
        }

        var feature = ScriptableObject.CreateInstance(featureType) as ScriptableRendererFeature;
        if (feature == null) return false;

        feature.name = kTypeShort;
        AssetDatabase.AddObjectToAsset(feature, data);
        data.rendererFeatures.Add(feature);
        typeof(ScriptableRendererData)
            .GetMethod("ValidateRendererFeatures", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.Invoke(data, null);

        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[PrisonLife] URP_Renderer에 RockFieldInstancedRenderFeature 를 자동 등록했습니다. (GPU 인스턴스 경로에 필요)");
        return true;
    }

    [MenuItem("PrisonLife/Add Rock Field Instanced to URP", false, 0)]
    [MenuItem("Tools/PrisonLife/Add Rock Field Instanced to URP", false, 0)]
    [MenuItem("Assets/PrisonLife/Add Rock Field Instanced to URP", false, 0)]
    static void AddRockFieldFeature()
    {
        if (TryEnsureRockFieldFeatureOnUrp())
        {
            var data = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(kRendererPath);
            if (data != null)
            {
                Selection.activeObject = data;
                for (int i = 0; i < data.rendererFeatures.Count; i++)
                {
                    if (data.rendererFeatures[i] != null && data.rendererFeatures[i].name == kTypeShort)
                    {
                        EditorGUIUtility.PingObject(data.rendererFeatures[i]);
                        break;
                    }
                }
            }
        }
        else
        {
            EditorUtility.DisplayDialog(
                kTypeShort, "URP에 RockFieldInstancedRenderFeature 를 추가하지 못했습니다.\nConsole 경고/에러를 확인하세요.",
                "OK");
        }
    }

    /// 1) AssetDatabase + MonoScript.GetClass: 어셈블리 이름/로드와 무관하게, 컴파일된 스크립트의 Type을 씀.
    /// 2) 실패 시 Assembly.GetType 보조.
    static Type FindRockFieldFeatureType()
    {
        // 경로/이름에 "RockFieldInstancedRenderFeature" 가 들어간 MonoScript 찾기
        foreach (var guid in AssetDatabase.FindAssets("RockFieldInstancedRenderFeature t:MonoScript"))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path) || !path.EndsWith("RockFieldInstancedRenderFeature.cs", StringComparison.OrdinalIgnoreCase))
                continue;
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
            if (script == null) continue;
            var t = script.GetClass();
            if (t != null) return t;
        }

        // t:필터가 환경에 따라 비어 있을 수 있어 넓게 검색
        foreach (var guid in AssetDatabase.FindAssets("RockFieldInstancedRenderFeature"))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith("RockFieldInstancedRenderFeature.cs", StringComparison.OrdinalIgnoreCase)) continue;
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
            if (script == null) continue;
            var t = script.GetClass();
            if (t != null) return t;
        }

        try
        {
            var t = Type.GetType(kTypeShort + ", Assembly-CSharp", false, false);
            if (t != null) return t;
        }
        catch { }

        foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (a.IsDynamic) continue;
            try
            {
                var t = a.GetType(kTypeShort, false, false);
                if (t != null) return t;
            }
            catch { }
        }

        return null;
    }
}
#endif
