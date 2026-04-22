using UnityEngine;

public class HandcuffPickupZone : MonoBehaviour, IInteractable, IResettable
{
    [Header("설정")]
    public float pickupInterval = 0.2f;

    [Header("스택 시각화")]
    public Transform stackRoot;
    public GameObject handcuffVisualPrefab;
    public float stackSpacing = 0.08f;

    public int StoredCount { get; private set; }

    private float _pickupTimer;

    private void Awake() => ResetRegistry.Register(this);
    private void OnDestroy() => ResetRegistry.Unregister(this);

    public void ResetState() => ClearStored();

    public void AddHandcuff(int amount = 1)
    {
        StoredCount += amount;
        RefreshVisual();
    }

    public int TakeHandcuff(int amount = 1)
    {
        int taken = Mathf.Min(amount, StoredCount);
        StoredCount -= taken;
        RefreshVisual();
        return taken;
    }

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

        int current = stackRoot.childCount;

        for (int i = StoredCount; i < current; i++)
            stackRoot.GetChild(i).gameObject.SetActive(false);

        for (int i = 0; i < StoredCount; i++)
        {
            GameObject obj;
            if (i < current)
            {
                obj = stackRoot.GetChild(i).gameObject;
                obj.SetActive(true);
            }
            else
            {
                obj = Instantiate(handcuffVisualPrefab, stackRoot);
            }
            obj.transform.localPosition = Vector3.up * i * stackSpacing;
            obj.transform.localRotation = Quaternion.identity;
        }
    }
}
