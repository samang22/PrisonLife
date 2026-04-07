using UnityEngine;

/// <summary>
/// 수감된 죄수 - 감옥 안에서 자동으로 철광석을 생산
/// 속도는 플레이어 초기 채굴 속도보다 조금 느린 수준
/// </summary>
public class WorkerController : MonoBehaviour
{
    [Header("생산 설정")]
    public float productionInterval = 1.5f;  // 플레이어 기본 1초보다 느린 1.5초

    [Header("연결")]
    public HandcuffPickupZone outputZone;    // 생산한 자원을 보낼 구역 (선택)

    private float _timer;

    private void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= productionInterval)
        {
            _timer = 0f;
            ProduceResource();
        }
    }

    private void ProduceResource()
    {
        // 수감된 죄수가 자동으로 철광석 1개 생산 → 제작대 인근 구역에 공급
        // outputZone이 없으면 CurrencyManager로 직접 소량 지급 (설계에 따라 조정)
        if (outputZone != null)
            outputZone.AddHandcuff(0); // 필요 시 철광석 공급 로직으로 교체
        else
            CurrencyManager.Instance?.AddDollars(1);
    }
}
