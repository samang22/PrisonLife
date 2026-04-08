using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EquipmentSlot
{
    public string slotName;
    public GameObject prefab;
}

/// <summary>
/// 슬롯 기반 모듈식 캐릭터 메시 교체 시스템
/// prefab에서 SkinnedMeshRenderer를 자동 추출하여 바인딩
/// </summary>
public class CharacterEquipment : MonoBehaviour
{
    [Header("스켈레톤 루트 (Base_Mesh 하위 Root 본)")]
    public Transform skeletonRoot;

    [Header("초기 장착 메시 목록")]
    public List<EquipmentSlot> defaultEquipment = new List<EquipmentSlot>();

    // 슬롯 이름 → 현재 장착된 SkinnedMeshRenderer
    private readonly Dictionary<string, SkinnedMeshRenderer> _slots
        = new Dictionary<string, SkinnedMeshRenderer>();

    private void Start()
    {
        foreach (EquipmentSlot slot in defaultEquipment)
        {
            if (!string.IsNullOrEmpty(slot.slotName) && slot.prefab != null)
                Equip(slot.slotName, slot.prefab);
        }
    }

    /// <summary>
    /// 슬롯에 새 메시 장착. prefab에서 SMR을 자동 추출하여 바인딩.
    /// </summary>
    public void Equip(string slotName, GameObject prefab)
    {
        Unequip(slotName);

        if (prefab == null) return;

        // 프리팹에서 SMR 추출
        SkinnedMeshRenderer sourceSMR = prefab.GetComponentInChildren<SkinnedMeshRenderer>();
        if (sourceSMR == null)
        {
            Debug.LogWarning($"[CharacterEquipment] '{prefab.name}'에서 SkinnedMeshRenderer를 찾지 못했습니다.");
            return;
        }

        // 새 슬롯 오브젝트 생성
        GameObject instance = new GameObject(slotName);
        instance.transform.SetParent(transform);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;

        // SMR 데이터 복사
        SkinnedMeshRenderer newSMR = instance.AddComponent<SkinnedMeshRenderer>();
        CopySMRData(sourceSMR, newSMR);

        // 스켈레톤에 본 배열 바인딩
        if (skeletonRoot != null)
            BoneRemapper.Remap(newSMR, skeletonRoot);
        else
            Debug.LogWarning("[CharacterEquipment] skeletonRoot가 설정되지 않았습니다.");

        _slots[slotName] = newSMR;
    }

    /// <summary>
    /// 슬롯 비우기
    /// </summary>
    public void Unequip(string slotName)
    {
        if (_slots.TryGetValue(slotName, out SkinnedMeshRenderer existing))
        {
            if (existing != null)
                Destroy(existing.gameObject);
            _slots.Remove(slotName);
        }
    }

    /// <summary>
    /// 현재 슬롯의 SMR 반환 (없으면 null)
    /// </summary>
    public SkinnedMeshRenderer GetSlot(string slotName)
    {
        _slots.TryGetValue(slotName, out SkinnedMeshRenderer smr);
        return smr;
    }

    private static void CopySMRData(SkinnedMeshRenderer src, SkinnedMeshRenderer dst)
    {
        dst.sharedMesh = src.sharedMesh;
        dst.sharedMaterials = src.sharedMaterials;
        dst.bones = src.bones;
        dst.rootBone = src.rootBone;
        dst.localBounds = src.localBounds;
        dst.updateWhenOffscreen = true;
    }
}
