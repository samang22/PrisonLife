using UnityEngine;

/// <summary>
/// 철광석 광맥 - 플레이어가 접근하면 자동 채굴, 철광석을 등에 쌓아줌
/// </summary>
public class RockController : MonoBehaviour, IInteractable
{
    [Header("채굴 설정")]
    public float miningInterval = 1f;   // 채굴 주기 (초)
    public bool respawn = true;
    public float respawnTime = 15f;

    [Header("이펙트")]
    public GameObject mineParticlePrefab;

    private bool _isDepleted;
    private float _respawnTimer;
    private float _miningTimer;
    private MeshRenderer _meshRenderer;
    private Collider _collider;

    private void Awake()
    {
        _meshRenderer = GetComponentInChildren<MeshRenderer>();
        _collider = GetComponent<Collider>();
    }

    private void Update()
    {
        if (_isDepleted && respawn)
        {
            _respawnTimer -= Time.deltaTime;
            if (_respawnTimer <= 0f)
                Revive();
        }
    }

    public void OnInteract(PlayerController player)
    {
        if (_isDepleted) return;
        if (!player.CanPickupIronOre()) return;

        _miningTimer += Time.deltaTime;

        // 플레이어 stats의 miningInterval 사용 (업그레이드 반영)
        float interval = player.stats != null ? player.stats.miningInterval : miningInterval;

        if (_miningTimer >= interval)
        {
            _miningTimer = 0f;
            player.AddIronOre(1);

            if (mineParticlePrefab != null)
                Instantiate(mineParticlePrefab, transform.position, Quaternion.identity);

            // 채굴 횟수 제한 (5회 채굴 후 고갈)
            _hitCount++;
            if (_hitCount >= maxHits)
                Deplete();
        }
    }

    [Header("고갈 설정")]
    public int maxHits = 5;
    private int _hitCount;

    private void Deplete()
    {
        _isDepleted = true;
        _respawnTimer = respawnTime;
        _hitCount = 0;

        if (_meshRenderer != null) _meshRenderer.enabled = false;
        if (_collider != null) _collider.enabled = false;
    }

    private void Revive()
    {
        _isDepleted = false;

        if (_meshRenderer != null) _meshRenderer.enabled = true;
        if (_collider != null) _collider.enabled = true;
    }
}
