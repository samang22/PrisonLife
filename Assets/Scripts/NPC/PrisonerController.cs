using UnityEngine;
using UnityEngine.AI;

public class PrisonerController : MonoBehaviour
{
    public enum PrisonerState { Wandering, Fleeing, BeingArrested, Imprisoned }

    [Header("설정")]
    public float wanderRadius = 8f;
    public float fleeSpeed = 4f;
    public float arrestDistance = 1.5f;
    public float arrestTime = 1.5f;
    public int rewardDollars = 20;

    [Header("참조")]
    public Animator animator;

    public PrisonerState CurrentState { get; private set; } = PrisonerState.Wandering;

    private NavMeshAgent _agent;
    private Transform _playerTransform;
    private float _arrestTimer;
    private float _wanderTimer;
    private Vector3 _spawnPosition;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _spawnPosition = transform.position;
    }

    private void Start()
    {
        _playerTransform = FindObjectOfType<PlayerController>()?.transform;
    }

    private void Update()
    {
        if (CurrentState == PrisonerState.Imprisoned) return;

        float distToPlayer = _playerTransform != null
            ? Vector3.Distance(transform.position, _playerTransform.position)
            : float.MaxValue;

        switch (CurrentState)
        {
            case PrisonerState.Wandering:
                HandleWandering(distToPlayer);
                break;
            case PrisonerState.Fleeing:
                HandleFleeing(distToPlayer);
                break;
            case PrisonerState.BeingArrested:
                HandleBeingArrested();
                break;
        }

        if (animator != null)
            animator.SetFloat(SpeedHash, _agent.velocity.magnitude);
    }

    private void HandleWandering(float distToPlayer)
    {
        if (distToPlayer < 6f)
        {
            SetState(PrisonerState.Fleeing);
            return;
        }

        _wanderTimer -= Time.deltaTime;
        if (_wanderTimer <= 0f)
        {
            Vector3 randomDir = Random.insideUnitSphere * wanderRadius;
            randomDir += _spawnPosition;
            if (NavMesh.SamplePosition(randomDir, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
                _agent.SetDestination(hit.position);
            _wanderTimer = Random.Range(2f, 5f);
        }
    }

    private void HandleFleeing(float distToPlayer)
    {
        if (distToPlayer > 10f)
        {
            SetState(PrisonerState.Wandering);
            return;
        }

        if (distToPlayer <= arrestDistance)
        {
            SetState(PrisonerState.BeingArrested);
            return;
        }

        // 플레이어 반대 방향으로 도망
        Vector3 fleeDir = (transform.position - _playerTransform.position).normalized;
        Vector3 fleeTarget = transform.position + fleeDir * 5f;
        if (NavMesh.SamplePosition(fleeTarget, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            _agent.SetDestination(hit.position);
    }

    private void HandleBeingArrested()
    {
        _agent.ResetPath();
        _agent.velocity = Vector3.zero;
        transform.LookAt(_playerTransform);

        _arrestTimer += Time.deltaTime;
        if (_arrestTimer >= arrestTime)
        {
            Arrest();
        }
    }

    private void Arrest()
    {
        if (GameManager.Instance.TryImprisonPrisoner())
        {
            CurrencyManager.Instance.AddDollars(rewardDollars);
            SetState(PrisonerState.Imprisoned);
            gameObject.SetActive(false);
        }
        else
        {
            _arrestTimer = 0f;
            SetState(PrisonerState.Wandering);
        }
    }

    private void SetState(PrisonerState newState)
    {
        CurrentState = newState;
        _arrestTimer = 0f;

        _agent.speed = newState == PrisonerState.Fleeing ? fleeSpeed : 2.5f;

        if (newState == PrisonerState.BeingArrested)
            _agent.ResetPath();
    }
}
