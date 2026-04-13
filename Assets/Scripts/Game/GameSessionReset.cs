using UnityEngine;

/// <summary>
/// 진행 중 게임을 처음 상태로 되돌립니다. UI 버튼의 OnClick()에 연결하세요.
/// </summary>
public class GameSessionReset : MonoBehaviour
{
    [Header("플레이어")]
    [Tooltip("비우면 리셋 시점의 플레이어 위치를 유지합니다. 지정하면 그 Transform으로 이동합니다.")]
    public Transform playerSpawnPoint;

    /// <summary>
    /// UI Button → OnClick() 에서 호출
    /// </summary>
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

        foreach (OfficerController o in FindObjectsOfType<OfficerController>())
        {
            if (o != null) Destroy(o.gameObject);
        }

        UpgradeManager.Instance?.ResetUpgradeProgress();

        PrisonCell.Instance?.ResetToInitialState();

        foreach (PrisonerQueue q in FindObjectsOfType<PrisonerQueue>())
            q.ResetQueueState();

        foreach (HandcuffMachine hm in FindObjectsOfType<HandcuffMachine>())
            hm.ResetMachineState();

        foreach (HandcuffPickupZone h in FindObjectsOfType<HandcuffPickupZone>())
            h.ClearStored();

        foreach (DollarZone d in FindObjectsOfType<DollarZone>())
            d.ClearStored();

        foreach (RockController rock in FindObjectsOfType<RockController>())
            rock.ResetToAvailable();

        if (player != null)
            player.ResetRunState(pos, rot);

        foreach (UpgradeZone uz in FindObjectsOfType<UpgradeZone>())
            uz.ResetPaymentProgress();

        CurrencyManager.Instance?.ResetToStartingAmount();

        UIManager.Instance?.RefreshPrisonerCountDisplay();
        UIManager.Instance?.UpdateDollarsUI(player != null ? player.DollarCount : 0);
    }
}
