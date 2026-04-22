using System.Collections.Generic;
using UnityEngine;

// skeletonRoot 하위 본 이름을 기준으로 SMR의 bones 배열을 교체
// 서로 다른 프리팹의 메시를 같은 캐릭터 스켈레톤에 붙일 때 사용
public static class BoneRemapper
{
    public static void Remap(SkinnedMeshRenderer target, Transform skeletonRoot)
    {
        Dictionary<string, Transform> boneMap = BuildBoneMap(skeletonRoot);

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
