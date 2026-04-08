using UnityEngine;
using TMPro;

/// <summary>
/// 달러 누적 구역 - 죄수 체포 시 달러가 쌓임
/// 플레이어가 구역에 진입하면 1개씩 수거
/// </summary>
public class DollarZone : MonoBehaviour, IInteractable
{
    [Header("UI")]
    public TextMeshPro stackCountText;

    [Header("시각화")]
    public Transform dollarStackRoot;
    public GameObject dollarVisualPrefab;
    public float stackSpacing = 0.06f;
    public int maxVisualCount = 20;

    public int StoredDollars { get; private set; }

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

        foreach (Transform child in dollarStackRoot)
            Destroy(child.gameObject);

        int visualCount = Mathf.Min(StoredDollars, maxVisualCount);
        for (int i = 0; i < visualCount; i++)
        {
            Vector3 offset = Vector3.up * i * stackSpacing;
            Instantiate(dollarVisualPrefab,
                        dollarStackRoot.position + offset,
                        dollarStackRoot.rotation,
                        dollarStackRoot);
        }
    }
}
