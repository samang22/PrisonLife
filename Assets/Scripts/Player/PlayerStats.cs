using UnityEngine;

[CreateAssetMenu(fileName = "PlayerStats", menuName = "PrisonLife/PlayerStats")]
public class PlayerStats : ScriptableObject
{
    [Header("이동")]
    public float moveSpeed = 5f;

    [Header("채굴")]
    public float miningInterval = 1f;
    public int drillLevel = 0;

    [Header("철광석 운반 (등)")]
    public int maxIronOreCarry = 5;

    [Header("수갑 소지 (가슴)")]
    public int maxHandcuffCarry = 5;

    public void UpgradeDrill()
    {
        drillLevel++;
        miningInterval = Mathf.Max(0.2f, miningInterval - 0.1f);
    }

    public void UpgradeMoveSpeed()
    {
        moveSpeed += 1f;
    }

    public void UpgradeIronOreCarry()
    {
        maxIronOreCarry += 3;
    }

    public void UpgradeHandcuffCarry()
    {
        maxHandcuffCarry += 3;
    }
}
