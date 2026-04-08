using UnityEngine;

/// <summary>
/// 항상 카메라를 향하도록 회전 (월드 스페이스 텍스트/아이콘에 부착)
/// </summary>
public class Billboard : MonoBehaviour
{
    private Camera _camera;

    private void Start()
    {
        _camera = Camera.main;
    }

    private void LateUpdate()
    {
        if (_camera == null) return;
        transform.rotation = _camera.transform.rotation;
    }
}
