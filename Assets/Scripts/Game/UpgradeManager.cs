using System.Collections.Generic;
using UnityEngine;

public enum UpgradeType
{
    Drill,
    MoveSpeed,
    IronOreCarry,
    HandcuffCarry,
    PrisonExpansion
}

[System.Serializable]
public class UpgradeData
{
    public string upgradeName;
    public UpgradeType upgradeType;
    public int baseCost;
    public int maxLevel;
    public int currentLevel;

    public int CurrentCost => Mathf.RoundToInt(baseCost * Mathf.Pow(1.5f, currentLevel));
    public bool IsMaxLevel => currentLevel >= maxLevel;
}

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }

    [Header("업그레이드 목록")]
    public List<UpgradeData> upgrades = new List<UpgradeData>();

    private PlayerController _player;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        InitDefaultUpgrades();
    }

    private void Start()
    {
        _player = FindObjectOfType<PlayerController>();
    }

    private void InitDefaultUpgrades()
    {
        if (upgrades.Count > 0) return;

        upgrades = new List<UpgradeData>
        {
            new UpgradeData { upgradeName = "드릴 업그레이드",     upgradeType = UpgradeType.Drill,          baseCost = 50, maxLevel = 5 },
            new UpgradeData { upgradeName = "이동속도 업그레이드", upgradeType = UpgradeType.MoveSpeed,      baseCost = 40, maxLevel = 5 },
            new UpgradeData { upgradeName = "운반량 업그레이드",   upgradeType = UpgradeType.IronOreCarry,   baseCost = 60, maxLevel = 5 },
            new UpgradeData { upgradeName = "수갑 소지 업그레이드",upgradeType = UpgradeType.HandcuffCarry,  baseCost = 60, maxLevel = 5 },
            new UpgradeData { upgradeName = "감옥 확장",           upgradeType = UpgradeType.PrisonExpansion,baseCost = 100, maxLevel = 10 }
        };
    }

    public UpgradeData GetData(UpgradeType type) =>
        upgrades.Find(u => u.upgradeType == type);

    public int GetCost(UpgradeType type) => GetData(type)?.CurrentCost ?? 0;

    public bool IsMaxLevel(UpgradeType type) => GetData(type)?.IsMaxLevel ?? true;

    /// <summary>
    /// UpgradeZone에서 1원씩 납부 완료 후 호출 - 달러 차감 없이 업그레이드만 적용
    /// </summary>
    public void ApplyUpgrade(UpgradeType type)
    {
        UpgradeData data = GetData(type);
        if (data == null || data.IsMaxLevel) return;

        data.currentLevel++;

        if (_player == null) _player = FindObjectOfType<PlayerController>();
        if (_player?.stats == null) return;

        switch (type)
        {
            case UpgradeType.Drill:
                _player.stats.UpgradeDrill();
                _player.RefreshTool();
                break;
            case UpgradeType.MoveSpeed:
                _player.stats.UpgradeMoveSpeed();
                break;
            case UpgradeType.IronOreCarry:
                _player.stats.UpgradeIronOreCarry();
                break;
            case UpgradeType.HandcuffCarry:
                _player.stats.UpgradeHandcuffCarry();
                break;
            case UpgradeType.PrisonExpansion:
                PrisonCell.Instance?.Expand();
                break;
        }
    }
}
