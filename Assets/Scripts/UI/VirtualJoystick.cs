using UnityEngine;
using UnityEngine.EventSystems;

public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("설정")]
    public float handleRange = 1f;
    public float deadZone = 0.1f;

    [Header("참조")]
    public RectTransform background;
    public RectTransform handle;

    public Vector2 Direction { get; private set; }

    private Canvas _canvas;
    private Camera _camera;
    private Vector2 _inputVector;

    private void Start()
    {
        _canvas = GetComponentInParent<Canvas>();
        _camera = _canvas.renderMode == RenderMode.ScreenSpaceCamera ? _canvas.worldCamera : null;
        SetHandlePosition(Vector2.zero);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            background,
            eventData.position,
            _camera,
            out Vector2 localPoint
        );

        Vector2 pivotOffset = background.pivot * background.rect.size;
        Vector2 delta = localPoint + pivotOffset;

        float bgRadius = background.rect.width * 0.5f;
        _inputVector = delta.magnitude > bgRadius ? delta.normalized : delta / bgRadius;

        SetHandlePosition(_inputVector);
        Direction = _inputVector.magnitude < deadZone ? Vector2.zero : _inputVector;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _inputVector = Vector2.zero;
        Direction = Vector2.zero;
        SetHandlePosition(Vector2.zero);
    }

    private void SetHandlePosition(Vector2 input)
    {
        if (handle == null || background == null) return;
        float radius = background.rect.width * 0.5f * handleRange;
        handle.anchoredPosition = input * radius;
    }
}
