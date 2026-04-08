using UnityEngine;

/// <summary>
/// 철광석 광맥 - 플레이어가 접근하면 자동 채굴, 철광석을 등에 쌓아줌
/// </summary>
public class RockController : MonoBehaviour
{
    [Header("채굴 설정")]
    public bool respawn = true;
    public float respawnTime = 15f;

    [Header("이펙트")]
    public GameObject mineParticlePrefab;

    private bool _isDepleted;
    private float _respawnTimer;
    private Renderer[] _renderers;
    private Collider[] _colliders;

    private void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>();
        _colliders = GetComponentsInChildren<Collider>();
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

    // PlayerController.HandleMining()에서 호출
    public void Mine(PlayerController player)
    {
        if (_isDepleted) return;

        player.AddIronOre(1);

        if (mineParticlePrefab != null)
            Instantiate(mineParticlePrefab, transform.position, Quaternion.identity);

        Deplete();
    }

    private void Deplete()
    {
        _isDepleted = true;
        _respawnTimer = respawnTime;

        Debug.Log($"[RockController] 고갈 - {gameObject.name} / {respawnTime}초 후 재생성");

        foreach (Renderer r in _renderers) r.enabled = false;
        foreach (Collider c in _colliders) c.enabled = false;
    }

    private void Revive()
    {
        _isDepleted = false;

        Debug.Log($"[RockController] 재생성 - {gameObject.name}");

        foreach (Renderer r in _renderers) r.enabled = true;
        foreach (Collider c in _colliders) c.enabled = true;
    }
}
