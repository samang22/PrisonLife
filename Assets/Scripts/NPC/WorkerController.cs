using System.Collections;
using UnityEngine;

/// <summary>
/// 수감된 죄수 - 일정 주기로 철광석 프리팹을 생성하여 HandcuffMachine 스택 위치로 날려 보냄
/// </summary>
public class WorkerController : MonoBehaviour
{
    [Header("생산 설정")]
    public float productionInterval = 1.5f;

    [Header("연결")]
    public HandcuffMachine targetMachine;

    [Header("프리팹")]
    public GameObject ironOrePrefab;

    [Header("이동 설정")]
    public float flySpeed = 5f;
    public float arcHeight = 1.5f;

    private float _timer;

    private void Update()
    {
        if (targetMachine == null || ironOrePrefab == null) return;

        _timer += Time.deltaTime;
        if (_timer >= productionInterval)
        {
            _timer = 0f;
            ProduceResource();
        }
    }

    private void ProduceResource()
    {
        if (targetMachine.QueueCount >= targetMachine.maxQueue) return;

        Vector3 spawnPos = transform.position + Vector3.up * 1f;
        GameObject ore = Instantiate(ironOrePrefab, spawnPos, Quaternion.identity);
        StartCoroutine(FlyToMachine(ore));
    }

    private IEnumerator FlyToMachine(GameObject ore)
    {
        if (targetMachine.StackRoot == null)
        {
            Destroy(ore);
            yield break;
        }

        Vector3 start = ore.transform.position;
        Vector3 end = targetMachine.StackRoot.position;
        float distance = Vector3.Distance(start, end);
        float duration = Mathf.Max(0.1f, distance / flySpeed);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (ore == null) yield break;
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            Vector3 linear = Vector3.Lerp(start, end, t);
            ore.transform.position = linear + Vector3.up * (Mathf.Sin(t * Mathf.PI) * arcHeight);
            yield return null;
        }

        if (ore == null) yield break;
        targetMachine.ReceiveIronOre(ore);
    }
}
