using UnityEngine;

// UI Button의 OnClick()에 연결해서 사용
public class GameSessionReset : MonoBehaviour
{
    [Header("플레이어")]
    [Tooltip("비우면 리셋 시점의 플레이어 위치를 유지합니다. 지정하면 그 Transform으로 이동합니다.")]
    public Transform playerSpawnPoint;

    public void ResetGame()
    {
        PlayerController player = FindObjectOfType<PlayerController>();

        Vector3    pos = player != null ? player.transform.position : Vector3.zero;
        Quaternion rot = player != null ? player.transform.rotation : Quaternion.identity;
        if (playerSpawnPoint != null)
        {
            pos = playerSpawnPoint.position;
            rot = playerSpawnPoint.rotation;
        }

        // Officer는 동적 생성 오브젝트라 레지스트리 대신 직접 제거
        foreach (OfficerController o in FindObjectsOfType<OfficerController>())
        {
            if (o != null) Destroy(o.gameObject);
        }

        UpgradeManager.Instance?.ResetUpgradeProgress();
        PrisonCell.Instance?.ResetToInitialState();

        // IResettable 등록 오브젝트 일괄 리셋
        ResetRegistry.ResetAll();

        if (player != null)
            player.ResetRunState(pos, rot);

        CurrencyManager.Instance?.ResetToStartingAmount();

        UIManager.Instance?.RefreshPrisonerCountDisplay();
        UIManager.Instance?.UpdateDollarsUI(player != null ? player.DollarCount : 0);
    }
}
