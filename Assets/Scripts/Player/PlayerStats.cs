using UnityEngine;

[CreateAssetMenu(fileName = "PlayerStats", menuName = "PrisonLife/PlayerStats")]
public class PlayerStats : ScriptableObject
{
    [Header("이동")]
    public float moveSpeed = 5f;

    [Header("채굴")]
    public float miningDamagePerSecond = 10f;
    public int drillLevel = 0;

    [Header("운반")]
    public int maxCarryCount = 5;
    public int currentCarryCount = 0;

    public void UpgradeDrill()
    {
        drillLevel++;
        miningDamagePerSecond += 10f;
    }

    public void UpgradeMoveSpeed()
    {
        moveSpeed += 1f;
    }

    public void UpgradeCarryCapacity()
    {
        maxCarryCount += 5;
    }

    public bool CanCarry()
    {
        return currentCarryCount < maxCarryCount;
    }
}
