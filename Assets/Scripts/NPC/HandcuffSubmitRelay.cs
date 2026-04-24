using UnityEngine;

/// PrisonerQueue의 수갑 납품 구역
/// - 플레이어가 이 구역에 진입하면 PrisonerQueue.OnInteract 호출
/// - 납품된 수갑을 이 구역에 시각적으로 쌓음
[RequireComponent(typeof(BoxCollider))]
public class HandcuffSubmitRelay : MonoBehaviour, IInteractable
{
    [Header("위임 대상")]
    public PrisonerQueue queue;

    [Header("수갑 시각화")]
    public Transform handcuffStackRoot;
    public GameObject handcuffVisualPrefab;
    public float stackSpacing = 0.08f;
    public int maxVisualCount = 20;

    private void Awake()
    {
        GetComponent<BoxCollider>().isTrigger = true;
    }

    private void Start()
    {
        if (queue == null)
        {
            queue = FindObjectOfType<PrisonerQueue>();
            if (queue != null)
                Debug.Log($"[HandcuffSubmitRelay] PrisonerQueue 자동 탐색 성공: {queue.gameObject.name}");
            else
                Debug.LogError($"[HandcuffSubmitRelay] 씬에서 PrisonerQueue를 찾을 수 없습니다.");
        }
    }

    public void OnInteract(PlayerController player)
    {
        if (queue == null)
        {
            Debug.LogError("[HandcuffSubmitRelay] queue가 null입니다. 납품 불가.");
            return;
        }
        queue.OnInteract(player);
    }

    /// PrisonerQueue에서 수갑 수가 바뀔 때 호출
    public void RefreshVisual(int count)
    {
        if (handcuffStackRoot == null || handcuffVisualPrefab == null) return;

        foreach (Transform child in handcuffStackRoot)
            Destroy(child.gameObject);

        int visualCount = Mathf.Min(count, maxVisualCount);
        for (int i = 0; i < visualCount; i++)
        {
            Vector3 offset = Vector3.up * i * stackSpacing;
            Instantiate(handcuffVisualPrefab,
                        handcuffStackRoot.position + offset,
                        handcuffStackRoot.rotation,
                        handcuffStackRoot);
        }
    }
}
