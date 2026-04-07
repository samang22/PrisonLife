using UnityEngine;
using TMPro;

/// <summary>
/// 수갑 제조기 - 일정 주기로 수갑을 생산하고 스택에 쌓음
/// </summary>
public class HandcuffMachine : MonoBehaviour
{
    [Header("생산 설정")]
    public float productionInterval = 2f;
    public int maxHandcuffStack = 10;
    public GameObject handcuffPrefab;
    public Transform stackSpawnPoint;

    [Header("UI")]
    public TextMeshPro stackCountText;

    private float _productionTimer;
    private int _currentStack;

    private void Update()
    {
        if (_currentStack >= maxHandcuffStack) return;

        _productionTimer += Time.deltaTime;
        if (_productionTimer >= productionInterval)
        {
            ProduceHandcuff();
            _productionTimer = 0f;
        }
    }

    private void ProduceHandcuff()
    {
        _currentStack++;

        if (handcuffPrefab != null && stackSpawnPoint != null)
        {
            Vector3 spawnPos = stackSpawnPoint.position + Vector3.up * (_currentStack - 1) * 0.1f;
            Instantiate(handcuffPrefab, spawnPos, Quaternion.identity, stackSpawnPoint);
        }

        UpdateStackText();
    }

    public bool TakeHandcuff()
    {
        if (_currentStack <= 0) return false;
        _currentStack--;
        UpdateStackText();
        return true;
    }

    private void UpdateStackText()
    {
        if (stackCountText != null)
            stackCountText.text = $"{_currentStack}/{maxHandcuffStack}";
    }
}
