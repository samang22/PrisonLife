using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EquipmentSlot
{
    public string slotName;
    public GameObject prefab;
}

public class CharacterEquipment : MonoBehaviour
{
    [Header("스켈레톤 루트 (Base_Mesh 하위 Root 본)")]
    public Transform skeletonRoot;

    [Header("초기 장착 메시 목록")]
    public List<EquipmentSlot> defaultEquipment = new List<EquipmentSlot>();

    // 슬롯 이름으로 현재 장착된 SMR을 관리
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

    public void Equip(string slotName, GameObject prefab)
    {
        Unequip(slotName);

        if (prefab == null) return;

        SkinnedMeshRenderer sourceSMR = prefab.GetComponentInChildren<SkinnedMeshRenderer>();
        if (sourceSMR == null)
        {
            Debug.LogWarning($"[CharacterEquipment] '{prefab.name}'에서 SkinnedMeshRenderer를 찾지 못했습니다.");
            return;
        }

        GameObject instance = new GameObject(slotName);
        instance.transform.SetParent(transform);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;

        SkinnedMeshRenderer newSMR = instance.AddComponent<SkinnedMeshRenderer>();
        CopySMRData(sourceSMR, newSMR);

        if (skeletonRoot != null)
            BoneRemapper.Remap(newSMR, skeletonRoot);
        else
            Debug.LogWarning("[CharacterEquipment] skeletonRoot가 설정되지 않았습니다.");

        _slots[slotName] = newSMR;
    }

    public void Unequip(string slotName)
    {
        if (_slots.TryGetValue(slotName, out SkinnedMeshRenderer existing))
        {
            if (existing != null)
                Destroy(existing.gameObject);
            _slots.Remove(slotName);
        }
    }

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
