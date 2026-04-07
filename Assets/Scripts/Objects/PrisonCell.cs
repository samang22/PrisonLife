using UnityEngine;
using TMPro;

/// <summary>
/// 감옥 수용 구역 - 죄수가 수감될 때 UI 업데이트
/// </summary>
public class PrisonCell : MonoBehaviour
{
    [Header("설정")]
    public int cellCapacity = 5;
    public TextMeshPro cellCountText;

    private int _currentCount;

    private void Start()
    {
        UpdateCellText();

        if (GameManager.Instance != null)
            GameManager.Instance.onPrisonerCountChanged.AddListener(OnGlobalCountChanged);
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.onPrisonerCountChanged.RemoveListener(OnGlobalCountChanged);
    }

    private void OnGlobalCountChanged(int current, int max)
    {
        UpdateCellText();
    }

    private void UpdateCellText()
    {
        if (cellCountText != null)
            cellCountText.text = $"{GameManager.Instance?.CurrentPrisonerCount ?? 0}/{GameManager.Instance?.maxPrisonerCapacity ?? 20}";
    }
}
