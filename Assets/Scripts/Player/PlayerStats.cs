using UnityEngine;

[CreateAssetMenu(fileName = "PlayerStats", menuName = "PrisonLife/PlayerStats")]
public class PlayerStats : ScriptableObject
{
    [Header("이동")]
    public float moveSpeed = 5f;

    [Header("채굴")]
    public float miningInterval = 0.5f;  // 1단계 채굴 주기
    public float miningRange = 1.5f;     // 3단계 OverlapSphere 반경
    public int drillLevel = 0;           // 0=1단계 / 1=2단계 / 2=3단계

    // 단계별 실제 채굴 반경: 1·2단계는 1/3, 3단계는 full
    public float EffectiveMiningRange => drillLevel >= 2 ? miningRange : miningRange / 3f;

    [Header("철광석 운반 (등)")]
    public int maxIronOreCarry = 5;

    [Header("수갑 소지 (가슴)")]
    public int maxHandcuffCarry = 5;

    public void UpgradeDrill()
    {
        drillLevel = Mathf.Min(drillLevel + 1, 2);
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
