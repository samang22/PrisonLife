using UnityEngine;
using TMPro;

/// <summary>
/// 업그레이드 구매 구역 - 플레이어가 머물면 달러가 1원씩 차감되고
/// 요구 비용이 모두 채워지면 업그레이드 적용
/// </summary>
public class UpgradeZone : MonoBehaviour, IInteractable
{
    [Header("업그레이드 타입")]
    public UpgradeType upgradeType;

    [Header("UI")]
    public TextMeshPro remainingCostText;
    public TextMeshPro upgradeNameText;
    public GameObject maxLevelIndicator;

    [Header("설정")]
    public float drainInterval  = 0.05f;
    public bool  disableOnMax   = false;   // true면 MAX 도달 시 이 GameObject 비활성화

    private float _drainTimer;
    private int _paid;
    private int _currentLevelCost;

    private void Start()
    {
        RefreshCost();
        RefreshUI();
    }

    private void Update()
    {
        // HireWorker는 감옥 상태가 실시간으로 바뀌므로 매 프레임 UI 갱신
        if (upgradeType == UpgradeType.HireWorker)
            RefreshUI();
    }

    public void OnInteract(PlayerController player)
    {
        if (UpgradeManager.Instance == null) return;
        if (UpgradeManager.Instance.IsMaxLevel(upgradeType)) return;

        _drainTimer += Time.deltaTime;
        if (_drainTimer < drainInterval) return;
        _drainTimer = 0f;

        if (!player.TakeDollar(1)) return;

        _paid++;
        RefreshUI();

        if (_paid >= _currentLevelCost)
        {
            UpgradeManager.Instance.ApplyUpgrade(upgradeType);
            _paid = 0;
            RefreshCost();
            RefreshUI();
        }
    }

    private void RefreshCost()
    {
        _currentLevelCost = UpgradeManager.Instance != null
            ? UpgradeManager.Instance.GetCost(upgradeType)
            : 0;
    }

    private void RefreshUI()
    {
        // HireWorker: 감옥에 죄수가 아예 없으면 비용/MAX 표시 모두 공란
        if (upgradeType == UpgradeType.HireWorker)
        {
            bool noPrisoners = PrisonCell.Instance == null || PrisonCell.Instance.CurrentCount == 0;
            bool allHired    = !noPrisoners && (PrisonCell.Instance == null || !PrisonCell.Instance.HasUnhiredPrisoners);

            if (remainingCostText != null)
            {
                if (noPrisoners)
                    remainingCostText.text = "";
                else if (allHired)
                    remainingCostText.text = "MAX";
                else
                    remainingCostText.text = $"${Mathf.Max(0, _currentLevelCost - _paid)}";
            }

            if (upgradeNameText != null)
            {
                UpgradeData data = UpgradeManager.Instance?.GetData(upgradeType);
                if (data != null) upgradeNameText.text = data.upgradeName;
            }

            if (maxLevelIndicator != null)
                maxLevelIndicator.SetActive(allHired);

            return;
        }

        bool isMax = UpgradeManager.Instance?.IsMaxLevel(upgradeType) ?? false;
        int remaining = Mathf.Max(0, _currentLevelCost - _paid);

        if (remainingCostText != null)
            remainingCostText.text = isMax ? "MAX" : $"${remaining}";

        if (upgradeNameText != null)
        {
            UpgradeData data = UpgradeManager.Instance?.GetData(upgradeType);
            if (data != null)
                upgradeNameText.text = data.upgradeName;
        }

        if (maxLevelIndicator != null)
            maxLevelIndicator.SetActive(isMax);

        if (isMax && disableOnMax)
            gameObject.SetActive(false);
    }

    // 플레이어가 구역을 벗어나면 진행 초기화 없이 유지 (재진입 시 이어서 납부)
}
