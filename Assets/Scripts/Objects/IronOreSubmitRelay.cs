using UnityEngine;

/// <summary>
/// HandcuffMachine의 철광석 납품 구역
/// - 플레이어가 이 구역에 진입하면 HandcuffMachine.OnInteract 호출
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class IronOreSubmitRelay : MonoBehaviour, IInteractable
{
    [Header("위임 대상")]
    public HandcuffMachine machine;

    private void Awake()
    {
        GetComponent<BoxCollider>().isTrigger = true;
    }

    private void Start()
    {
        // HandcuffMachine이 바인딩되지 않은 경우 씬에서 자동 탐색
        if (machine == null)
        {
            machine = FindObjectOfType<HandcuffMachine>();
            if (machine != null)
                Debug.Log($"[IronOreSubmitRelay] HandcuffMachine 자동 탐색 성공: {machine.gameObject.name}");
            else
                Debug.LogError($"[IronOreSubmitRelay] 씬에서 HandcuffMachine을 찾을 수 없습니다.");
        }
        else
        {
            Debug.Log($"[IronOreSubmitRelay] HandcuffMachine 바인딩 확인: {machine.gameObject.name}");
        }
    }

    public void OnInteract(PlayerController player)
    {
        if (machine == null)
        {
            Debug.LogError("[IronOreSubmitRelay] machine이 null입니다. 납품 불가.");
            return;
        }
        Debug.Log($"[IronOreSubmitRelay] OnInteract 호출 - 플레이어 IronOre: {player.IronOreCount}");
        machine.OnInteract(player);
    }
}
