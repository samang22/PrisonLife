using UnityEngine;
using TMPro;

public class DollarZone : MonoBehaviour, IInteractable, IResettable
{
    [Header("UI")]
    public TextMeshPro stackCountText;

    [Header("시각화")]
    public Transform dollarStackRoot;
    public GameObject dollarVisualPrefab;
    public float stackSpacing = 0.06f;
    public int maxVisualCount = 20;

    public int StoredDollars { get; private set; }

    private void Awake() => ResetRegistry.Register(this);
    private void OnDestroy() => ResetRegistry.Unregister(this);

    public void ResetState() => ClearStored();

    public void ClearStored()
    {
        StoredDollars = 0;
        RefreshUI();
        RefreshVisual();
    }

    public void AddDollars(int amount)
    {
        StoredDollars += amount;
        RefreshUI();
        RefreshVisual();
    }

    public void OnInteract(PlayerController player)
    {
        if (StoredDollars <= 0) return;

        StoredDollars--;
        player.AddDollar(1);
        RefreshUI();
        RefreshVisual();
    }

    private void RefreshUI()
    {
        if (stackCountText != null)
            stackCountText.text = StoredDollars > 0 ? $"${StoredDollars}" : "";
    }

    private void RefreshVisual()
    {
        if (dollarStackRoot == null || dollarVisualPrefab == null) return;

        int visualCount = Mathf.Min(StoredDollars, maxVisualCount);
        int current     = dollarStackRoot.childCount;

        for (int i = visualCount; i < current; i++)
            dollarStackRoot.GetChild(i).gameObject.SetActive(false);

        for (int i = 0; i < visualCount; i++)
        {
            GameObject obj;
            if (i < current)
            {
                obj = dollarStackRoot.GetChild(i).gameObject;
                obj.SetActive(true);
            }
            else
            {
                obj = Instantiate(dollarVisualPrefab, dollarStackRoot);
            }
            obj.transform.localPosition = Vector3.up * i * stackSpacing;
            obj.transform.localRotation = Quaternion.identity;
        }
    }
}
