using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 메시의 Bones 배열을 skeletonRoot의 동명 본으로 자동 바인딩
/// ithappy 모듈식 캐릭터처럼 본 이름이 공유되는 경우에 사용
/// </summary>
public static class BoneRemapper
{
    /// <summary>
    /// target SMR의 bones를 skeletonRoot 하위의 동명 Transform으로 교체
    /// </summary>
    public static void Remap(SkinnedMeshRenderer target, Transform skeletonRoot)
    {
        // 스켈레톤의 모든 본을 이름 기준으로 딕셔너리 구성
        Dictionary<string, Transform> boneMap = BuildBoneMap(skeletonRoot);

        // Bones 배열 교체
        Transform[] originalBones = target.bones;
        Transform[] newBones = new Transform[originalBones.Length];

        for (int i = 0; i < originalBones.Length; i++)
        {
            if (originalBones[i] == null) continue;

            if (boneMap.TryGetValue(originalBones[i].name, out Transform mapped))
                newBones[i] = mapped;
            else
                Debug.LogWarning($"[BoneRemapper] 본을 찾지 못함: {originalBones[i].name}");
        }

        target.bones = newBones;

        // Root Bone 교체
        if (target.rootBone != null &&
            boneMap.TryGetValue(target.rootBone.name, out Transform mappedRoot))
        {
            target.rootBone = mappedRoot;
        }
    }

    private static Dictionary<string, Transform> BuildBoneMap(Transform root)
    {
        var map = new Dictionary<string, Transform>();
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if (!map.ContainsKey(t.name))
                map[t.name] = t;
        }
        return map;
    }
}
