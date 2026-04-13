using UnityEngine;

/// <summary>
/// 수갑 픽업 구역 - 제작대에서 변환된 수갑이 쌓이는 곳
/// 플레이어가 진입하면 1개씩 가슴에 추가
/// </summary>
public class HandcuffPickupZone : MonoBehaviour, IInteractable
{
    [Header("설정")]
    public float pickupInterval = 0.2f;

    [Header("스택 시각화")]
    public Transform stackRoot;
    public GameObject handcuffVisualPrefab;
    public float stackSpacing = 0.08f;

    public int StoredCount { get; private set; }

    private float _pickupTimer;

    public void AddHandcuff(int amount = 1)
    {
        StoredCount += amount;
        RefreshVisual();
    }

    // OfficerController에서 호출 - 실제 가져간 수량 반환
    public int TakeHandcuff(int amount = 1)
    {
        int taken = Mathf.Min(amount, StoredCount);
        StoredCount -= taken;
        RefreshVisual();
        return taken;
    }

    /// <summary>게임 리셋 — 적재 수갑 제거</summary>
    public void ClearStored()
    {
        StoredCount = 0;
        _pickupTimer = 0f;
        RefreshVisual();
    }

    public void OnInteract(PlayerController player)
    {
        if (StoredCount <= 0) return;
        if (!player.CanPickupHandcuff()) return;

        _pickupTimer += Time.deltaTime;
        if (_pickupTimer < pickupInterval) return;
        _pickupTimer = 0f;

        StoredCount--;
        player.AddHandcuff(1);
        RefreshVisual();
    }

    private void RefreshVisual()
    {
        if (stackRoot == null || handcuffVisualPrefab == null) return;

        foreach (Transform child in stackRoot)
            Destroy(child.gameObject);

        for (int i = 0; i < StoredCount; i++)
        {
            Vector3 offset = Vector3.up * i * stackSpacing;
            Instantiate(handcuffVisualPrefab,
                        stackRoot.position + offset,
                        stackRoot.rotation,
                        stackRoot);
        }
    }
}
