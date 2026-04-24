using UnityEngine;

/// HandcuffMachine의 철광석 납품 구역
/// - 플레이어가 이 구역에 진입하면 HandcuffMachine.OnInteract 호출
/// - 납품된 철광석을 이 구역 StackRoot에 시각적으로 쌓음
[RequireComponent(typeof(BoxCollider))]
public class IronOreSubmitRelay : MonoBehaviour, IInteractable
{
    [Header("위임 대상")]
    public HandcuffMachine machine;

    [Header("철광석 시각화")]
    public Transform stackRoot;
    public GameObject ironOreVisualPrefab;
    public float stackSpacing = 0.1f;
    public int maxVisualCount = 20;

    private void Awake()
    {
        GetComponent<BoxCollider>().isTrigger = true;
    }

    private void Start()
    {
        if (machine == null)
        {
            machine = FindObjectOfType<HandcuffMachine>();
            if (machine != null)
                Debug.Log($"[IronOreSubmitRelay] HandcuffMachine 자동 탐색 성공: {machine.gameObject.name}");
            else
                Debug.LogError($"[IronOreSubmitRelay] 씬에서 HandcuffMachine을 찾을 수 없습니다.");
        }
    }

    public void OnInteract(PlayerController player)
    {
        if (machine == null)
        {
            Debug.LogError("[IronOreSubmitRelay] machine이 null입니다. 납품 불가.");
            return;
        }
        machine.OnInteract(player);
    }

    /// HandcuffMachine에서 플레이어 납품 수가 바뀔 때 호출
    public void RefreshVisual(int count)
    {
        if (stackRoot == null || ironOreVisualPrefab == null) return;

        foreach (Transform child in stackRoot)
            Destroy(child.gameObject);

        int visualCount = Mathf.Min(count, maxVisualCount);
        for (int i = 0; i < visualCount; i++)
        {
            Vector3 offset = Vector3.up * i * stackSpacing;
            Instantiate(ironOreVisualPrefab,
                        stackRoot.position + offset,
                        stackRoot.rotation,
                        stackRoot);
        }
    }
}
