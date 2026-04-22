using UnityEngine;
using TMPro;

public class UpgradeZone : MonoBehaviour, IInteractable, IResettable
{
    [Header("업그레이드 타입")]
    public UpgradeType upgradeType;

    [Header("UI")]
    public TextMeshPro remainingCostText;
    public TextMeshPro upgradeNameText;
    public GameObject maxLevelIndicator;

    [Header("설정")]
    public float drainInterval = 0.05f;
    public bool  disableOnMax  = false;

    private float _drainTimer;
    private int _paid;
    private int _currentLevelCost;

    private void Awake() => ResetRegistry.Register(this);
    private void OnDestroy() => ResetRegistry.Unregister(this);

    public void ResetState() => ResetPaymentProgress();

    private void Start()
    {
        RefreshCost();
        RefreshUI();
    }

    private void Update()
    {
        // HireWorker는 감옥 상태가 실시간으로 바뀌므로 매 프레임 갱신
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
        // 감옥에 죄수가 없으면 공란, 전원 고용 완료면 MAX 표시
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

    public void ResetPaymentProgress()
    {
        _paid = 0;
        _drainTimer = 0f;
        gameObject.SetActive(true);
        RefreshCost();
        RefreshUI();
    }
}
