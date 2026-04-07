using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 안전모를 쓴 작업 죄수 - 지정된 바위를 자동으로 채굴
/// </summary>
public class WorkerController : MonoBehaviour
{
    public enum WorkerState { MovingToRock, Mining, Idle }

    [Header("설정")]
    public float miningDamage = 5f;
    public float miningInterval = 0.5f;

    [Header("참조")]
    public Animator animator;

    private NavMeshAgent _agent;
    private RockController _targetRock;
    private WorkerState _state = WorkerState.Idle;
    private float _miningTimer;

    private static readonly int MiningHash = Animator.StringToHash("IsMining");
    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        switch (_state)
        {
            case WorkerState.Idle:
                FindNearestRock();
                break;
            case WorkerState.MovingToRock:
                CheckArrivalAtRock();
                break;
            case WorkerState.Mining:
                HandleMining();
                break;
        }

        if (animator != null)
        {
            animator.SetFloat(SpeedHash, _agent.velocity.magnitude);
            animator.SetBool(MiningHash, _state == WorkerState.Mining);
        }
    }

    private void FindNearestRock()
    {
        RockController[] rocks = FindObjectsOfType<RockController>();
        float closestDist = float.MaxValue;
        RockController closest = null;

        foreach (var rock in rocks)
        {
            float dist = Vector3.Distance(transform.position, rock.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = rock;
            }
        }

        if (closest != null)
        {
            _targetRock = closest;
            _agent.SetDestination(_targetRock.transform.position);
            _state = WorkerState.MovingToRock;
        }
    }

    private void CheckArrivalAtRock()
    {
        if (_targetRock == null)
        {
            _state = WorkerState.Idle;
            return;
        }

        if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance + 0.5f)
        {
            _agent.ResetPath();
            _state = WorkerState.Mining;
        }
    }

    private void HandleMining()
    {
        if (_targetRock == null)
        {
            _state = WorkerState.Idle;
            return;
        }

        _miningTimer += Time.deltaTime;
        if (_miningTimer >= miningInterval)
        {
            _targetRock.TakeDamage(miningDamage);
            _miningTimer = 0f;
        }
    }
}
