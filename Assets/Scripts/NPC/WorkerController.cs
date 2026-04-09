using System.Collections;
using UnityEngine;

/// <summary>
/// 고용된 수감 죄수 - Deposit을 찾아 이동 → 채굴 → 광석을 IronOreSubmitZone으로 전송
/// 채굴 속도: 1단계(0.5s)의 0.5배 → 1.0s마다 1회
/// </summary>
public class WorkerController : MonoBehaviour
{
    [Header("채굴 설정")]
    public float miningInterval  = 1.0f;
    public float moveSpeed       = 2.5f;
    public float miningStopRange = 1.8f;   // Deposit에 이 거리 이내면 채굴 시작

    [Header("연결")]
    public IronOreSubmitRelay targetSubmitZone;

    [Header("프리팹")]
    public GameObject ironOrePrefab;

    [Header("이동 연출")]
    public float flySpeed  = 5f;
    public float arcHeight = 1.5f;

    [Header("애니메이션 (선택)")]
    public string speedParam  = "Vert";
    public string miningParam = "";        // 채굴 애니메이션 트리거 이름 (없으면 공란)

    // ── 상태 ──
    private enum State { Idle, Moving, Mining }
    private State _state = State.Idle;

    private RockController _targetDeposit;
    private float          _miningTimer;
    private Animator       _animator;

    private static readonly float DepositSearchInterval = 0.5f;
    private float _searchTimer;

    private void OnEnable()
    {
        _animator = GetComponentInParent<Animator>();
        _state    = State.Idle;
        _targetDeposit = null;
        _miningTimer   = 0f;
        _searchTimer   = 0f;
    }

    private void Update()
    {
        if (targetSubmitZone == null || ironOrePrefab == null) return;

        switch (_state)
        {
            case State.Idle:   UpdateIdle();   break;
            case State.Moving: UpdateMoving(); break;
            case State.Mining: UpdateMining(); break;
        }
    }

    // ── Idle: 주기적으로 가장 가까운 활성 Deposit 탐색 ──
    private void UpdateIdle()
    {
        SetAnimSpeed(0f);

        _searchTimer += Time.deltaTime;
        if (_searchTimer < DepositSearchInterval) return;
        _searchTimer = 0f;

        _targetDeposit = FindNearestDeposit();
        if (_targetDeposit != null)
            _state = State.Moving;
    }

    // ── Moving: Deposit을 향해 이동 (Y축 고정) ──
    private void UpdateMoving()
    {
        if (_targetDeposit == null || !_targetDeposit.IsAvailable)
        {
            _state = State.Idle;
            return;
        }

        // Y를 무시한 수평 거리로만 판단
        Vector3 myPos     = transform.position;
        Vector3 targetPos = _targetDeposit.transform.position;
        Vector3 flatDiff  = new Vector3(targetPos.x - myPos.x, 0f, targetPos.z - myPos.z);
        float   dist      = flatDiff.magnitude;

        if (dist <= miningStopRange)
        {
            FaceTarget(targetPos);
            SetAnimSpeed(0f);
            _miningTimer = 0f;
            _state = State.Mining;
            return;
        }

        // 수평으로만 이동, Y 위치 유지
        Vector3 dir    = flatDiff.normalized;
        Vector3 newPos = myPos + dir * moveSpeed * Time.deltaTime;
        newPos.y = myPos.y;                  // Y 고정
        transform.position = newPos;
        FaceTarget(targetPos);
        SetAnimSpeed(1f);
    }

    // ── Mining: 일정 주기로 채굴 ──
    private void UpdateMining()
    {
        if (_targetDeposit == null || !_targetDeposit.IsAvailable)
        {
            _state = State.Idle;
            return;
        }

        // Deposit이 너무 멀어졌으면 다시 이동 (수평 거리만 체크)
        Vector3 flatDiff = _targetDeposit.transform.position - transform.position;
        flatDiff.y = 0f;
        float dist = flatDiff.magnitude;
        if (dist > miningStopRange * 1.5f)
        {
            _state = State.Moving;
            return;
        }

        FaceTarget(_targetDeposit.transform.position);

        if (!string.IsNullOrEmpty(miningParam) && _animator != null)
            _animator.SetTrigger(miningParam);

        _miningTimer += Time.deltaTime;
        if (_miningTimer < miningInterval) return;
        _miningTimer = 0f;

        // HandcuffMachine 큐가 가득 차면 채굴 대기
        HandcuffMachine machine = targetSubmitZone?.machine;
        if (machine == null || machine.QueueCount >= machine.maxQueue) return;

        if (_targetDeposit.MineByWorker())
        {
            Vector3    spawnPos = transform.position + Vector3.up * 1f;
            GameObject ore      = Instantiate(ironOrePrefab, spawnPos, Quaternion.identity);
            StartCoroutine(FlyToSubmitZone(ore));
        }

        // 채굴 후 Deposit이 고갈됐으면 다음 Deposit 탐색
        if (!_targetDeposit.IsAvailable)
            _state = State.Idle;
    }

    // ── 가장 가까운 활성 Deposit 탐색 ──
    private RockController FindNearestDeposit()
    {
        RockController[] all      = FindObjectsOfType<RockController>();
        RockController   nearest  = null;
        float            minDist  = float.MaxValue;

        foreach (RockController dep in all)
        {
            if (!dep.IsAvailable) continue;
            float d = Vector3.Distance(transform.position, dep.transform.position);
            if (d < minDist) { minDist = d; nearest = dep; }
        }
        return nearest;
    }

    // ── 광석 오브젝트를 IronOreSubmitZone으로 날려 보냄 ──
    private IEnumerator FlyToSubmitZone(GameObject ore)
    {
        Transform stackRoot = targetSubmitZone?.stackRoot;
        if (stackRoot == null) { Destroy(ore); yield break; }

        Vector3 start    = ore.transform.position;
        Vector3 end      = stackRoot.position;
        float   duration = Mathf.Max(0.1f, Vector3.Distance(start, end) / flySpeed);
        float   elapsed  = 0f;

        while (elapsed < duration)
        {
            if (ore == null) yield break;
            elapsed += Time.deltaTime;
            float   t      = Mathf.Clamp01(elapsed / duration);
            Vector3 linear = Vector3.Lerp(start, end, t);
            ore.transform.position = linear + Vector3.up * (Mathf.Sin(t * Mathf.PI) * arcHeight);
            yield return null;
        }

        if (ore == null) yield break;
        targetSubmitZone?.machine?.ReceiveIronOre(ore);
    }

    private void FaceTarget(Vector3 target)
    {
        Vector3 dir = target - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;
        transform.rotation = Quaternion.Slerp(transform.rotation,
                                              Quaternion.LookRotation(dir),
                                              10f * Time.deltaTime);
    }

    private void SetAnimSpeed(float speed)
    {
        if (_animator != null && !string.IsNullOrEmpty(speedParam))
            _animator.SetFloat(speedParam, speed);
    }
}
