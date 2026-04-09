using UnityEngine;

[CreateAssetMenu(fileName = "PlayerStats", menuName = "PrisonLife/PlayerStats")]
public class PlayerStats : ScriptableObject
{
    [Header("기본값 (게임 시작 시 이 값으로 초기화)")]
    public float defaultMoveSpeed = 5f;
    public float defaultMiningInterval = 0.5f;
    public float defaultMiningRange = 1.5f;
    public int defaultDrillLevel = 0;
    public int defaultMaxIronOreCarry = 5;
    public int defaultMaxHandcuffCarry = 5;

    [Header("현재 런타임 값 (자동 초기화됨)")]
    [HideInInspector] public float moveSpeed;
    [HideInInspector] public float miningInterval;
    [HideInInspector] public float miningRange;
    [HideInInspector] public int drillLevel;
    [HideInInspector] public int maxIronOreCarry;
    [HideInInspector] public int maxHandcuffCarry;

    // 단계별 실제 채굴 반경: 1·2단계는 1/3, 3단계는 full
    public float EffectiveMiningRange => drillLevel >= 2 ? miningRange : miningRange / 3f;

    /// <summary>
    /// 게임 시작 시 PlayerController.Awake()에서 호출 - 기본값으로 초기화
    /// </summary>
    public void RuntimeReset()
    {
        moveSpeed        = defaultMoveSpeed;
        miningInterval   = defaultMiningInterval;
        miningRange      = defaultMiningRange;
        drillLevel       = defaultDrillLevel;
        maxIronOreCarry  = defaultMaxIronOreCarry;
        maxHandcuffCarry = defaultMaxHandcuffCarry;
    }

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
