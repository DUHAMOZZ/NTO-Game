using UnityEngine;
using UnityEngine.UI;

public class CameraPoint : MonoBehaviour
{
    [Header("Данные камеры")]
    [SerializeField] private string _cameraId = "Camera_01";
    public string cameraName = "Камера №1";
    [Tooltip("Базовый снимок, который камера показывает при отсутствии активной улики")]
    public Sprite snapshotSprite;

    [Header("Элементы кнопки")]
    [Tooltip("Рамка/обводка, которая включается при выборе")]
    [SerializeField] private GameObject _outlineObject;
    [SerializeField] private Button _button;

    private RightMonitorMapController _controller;

    public string CameraId => string.IsNullOrEmpty(_cameraId) ? gameObject.name : _cameraId;

    public void Initialize(RightMonitorMapController controller)
    {
        _controller = controller;

        if (_button != null)
        {
            _button.onClick.RemoveListener(OnClick);
            _button.onClick.AddListener(OnClick);
        }

        SetSelected(false);
    }

    private void OnClick()
    {
        if (_controller != null)
        {
            _controller.SelectCamera(this);
        }
    }

    public void SetSelected(bool isSelected)
    {
        if (_outlineObject != null)
        {
            _outlineObject.SetActive(isSelected);
        }
    }
}