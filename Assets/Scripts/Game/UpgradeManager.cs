using System.Collections.Generic;
using UnityEngine;

public enum UpgradeType
{
    Drill,
    MoveSpeed,
    IronOreCarry,
    HandcuffCarry,
    PrisonExpansion,
    HireWorker,
    HireOfficer
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

    [Header("Officer 고용")]
    public GameObject       officerPrefab;
    public Transform        officerSpawnPoint;
    public HandcuffPickupZone  officerPickupZone;
    public HandcuffSubmitRelay officerHandcuffSubmitZone;

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
            new UpgradeData { upgradeName = "Mining Upgrade",        upgradeType = UpgradeType.Drill,           baseCost = 50,  maxLevel = 5 },
            new UpgradeData { upgradeName = "Speed Upgrade",         upgradeType = UpgradeType.MoveSpeed,       baseCost = 40,  maxLevel = 5 },
            new UpgradeData { upgradeName = "Iron Capacity Upgrade", upgradeType = UpgradeType.IronOreCarry,    baseCost = 60,  maxLevel = 5 },
            new UpgradeData { upgradeName = "Handcuff Upgrade",      upgradeType = UpgradeType.HandcuffCarry,   baseCost = 60,  maxLevel = 5 },
            new UpgradeData { upgradeName = "PrisonCell Expand",     upgradeType = UpgradeType.PrisonExpansion, baseCost = 20,  maxLevel = 10 },
            new UpgradeData { upgradeName = "Hire Workers",  upgradeType = UpgradeType.HireWorker,  baseCost = 50,  maxLevel = 9999 },
            new UpgradeData { upgradeName = "Hire Officer",  upgradeType = UpgradeType.HireOfficer, baseCost = 100, maxLevel = 1 }
        };
    }

    public UpgradeData GetData(UpgradeType type) =>
        upgrades.Find(u => u.upgradeType == type);

    public int GetCost(UpgradeType type) => GetData(type)?.CurrentCost ?? 0;

    public bool IsMaxLevel(UpgradeType type)
    {
        // HireWorker: 감옥에 미고용 죄수가 없으면 MAX 표시
        if (type == UpgradeType.HireWorker)
            return PrisonCell.Instance == null || !PrisonCell.Instance.HasUnhiredPrisoners;

        return GetData(type)?.IsMaxLevel ?? true;
    }

    /// UpgradeZone에서 1원씩 납부 완료 후 호출 - 달러 차감 없이 업그레이드만 적용
    public void ApplyUpgrade(UpgradeType type)
    {
        // HireWorker: currentLevel 증가 없이 즉시 고용 처리
        if (type == UpgradeType.HireWorker)
        {
            PrisonCell.Instance?.HireWorkers(3);
            return;
        }

        // HireOfficer: Officer 프리팹 스폰 (1회 한정, currentLevel 증가로 maxLevel 도달)
        if (type == UpgradeType.HireOfficer)
        {
            UpgradeData officerData = GetData(UpgradeType.HireOfficer);
            if (officerData != null) officerData.currentLevel++;

            if (officerPrefab != null)
            {
                Vector3    spawnPos = officerSpawnPoint != null ? officerSpawnPoint.position : Vector3.zero;
                Quaternion spawnRot = officerSpawnPoint != null ? officerSpawnPoint.rotation : Quaternion.identity;

                GameObject obj     = Instantiate(officerPrefab, spawnPos, spawnRot);
                OfficerController officer = obj.GetComponent<OfficerController>();
                if (officer != null)
                {
                    officer.pickupZone         = officerPickupZone;
                    officer.handcuffSubmitZone = officerHandcuffSubmitZone;
                }
            }
            else
            {
                Debug.LogWarning("[UpgradeManager] officerPrefab이 설정되지 않았습니다.");
            }
            return;
        }

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

    /// 게임 리셋 — 모든 업그레이드 단계 초기화
    public void ResetUpgradeProgress()
    {
        foreach (UpgradeData u in upgrades)
            u.currentLevel = 0;
    }
}
