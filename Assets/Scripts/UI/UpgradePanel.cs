using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 플레이어가 업그레이드 구역에 진입하면 자동 구매 시도
/// </summary>
public class UpgradePanel : MonoBehaviour, IInteractable
{
    [Header("업그레이드 타입")]
    public UpgradeType upgradeType;

    [Header("UI 참조")]
    public TextMeshProUGUI costText;
    public TextMeshProUGUI nameText;
    public Image iconImage;
    public GameObject maxLevelIndicator;

    [Header("구매 설정")]
    public float purchaseCooldown = 1f;

    private float _cooldownTimer;
    private bool _isMaxLevel;

    private void Start()
    {
        RefreshUI();
    }

    private void Update()
    {
        if (_cooldownTimer > 0f)
            _cooldownTimer -= Time.deltaTime;
    }

    public void OnInteract(PlayerController player)
    {
        if (_cooldownTimer > 0f) return;
        if (_isMaxLevel) return;

        bool purchased = UpgradeManager.Instance.TryPurchaseUpgrade(upgradeType);
        if (purchased)
        {
            _cooldownTimer = purchaseCooldown;
            RefreshUI();
        }
    }

    private void RefreshUI()
    {
        if (UpgradeManager.Instance == null) return;

        _isMaxLevel = UpgradeManager.Instance.IsMaxLevel(upgradeType);
        int cost = UpgradeManager.Instance.GetUpgradeCost(upgradeType);

        if (costText != null)
            costText.text = _isMaxLevel ? "MAX" : $"${cost}";

        if (maxLevelIndicator != null)
            maxLevelIndicator.SetActive(_isMaxLevel);
    }
}
