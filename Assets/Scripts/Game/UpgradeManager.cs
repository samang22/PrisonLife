using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class UpgradeData
{
    public string upgradeName;
    public int cost;
    public int maxLevel;
    public int currentLevel;
    public UpgradeType upgradeType;
}

public enum UpgradeType
{
    Drill,
    MoveSpeed,
    CarryCapacity
}

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }

    [Header("업그레이드 목록")]
    public List<UpgradeData> upgrades = new List<UpgradeData>
    {
        new UpgradeData { upgradeName = "드릴 업그레이드", cost = 50, maxLevel = 5, currentLevel = 0, upgradeType = UpgradeType.Drill },
        new UpgradeData { upgradeName = "이동속도 업그레이드", cost = 50, maxLevel = 5, currentLevel = 0, upgradeType = UpgradeType.MoveSpeed },
        new UpgradeData { upgradeName = "운반량 업그레이드", cost = 75, maxLevel = 3, currentLevel = 0, upgradeType = UpgradeType.CarryCapacity }
    };

    private PlayerStats _playerStats;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        _playerStats = FindObjectOfType<PlayerController>()?.stats;
    }

    public bool TryPurchaseUpgrade(UpgradeType type)
    {
        UpgradeData data = upgrades.Find(u => u.upgradeType == type);
        if (data == null) return false;
        if (data.currentLevel >= data.maxLevel) return false;
        if (!CurrencyManager.Instance.TrySpendDollars(data.cost)) return false;

        data.currentLevel++;
        data.cost = Mathf.RoundToInt(data.cost * 1.5f);

        ApplyUpgrade(data.upgradeType);
        return true;
    }

    private void ApplyUpgrade(UpgradeType type)
    {
        if (_playerStats == null) return;

        switch (type)
        {
            case UpgradeType.Drill:
                _playerStats.UpgradeDrill();
                break;
            case UpgradeType.MoveSpeed:
                _playerStats.UpgradeMoveSpeed();
                break;
            case UpgradeType.CarryCapacity:
                _playerStats.UpgradeCarryCapacity();
                break;
        }
    }

    public int GetUpgradeCost(UpgradeType type)
    {
        UpgradeData data = upgrades.Find(u => u.upgradeType == type);
        return data?.cost ?? 0;
    }

    public bool IsMaxLevel(UpgradeType type)
    {
        UpgradeData data = upgrades.Find(u => u.upgradeType == type);
        return data == null || data.currentLevel >= data.maxLevel;
    }
}
