using UnityEngine;
using UnityEngine.UI;

public class CameraPoint : MonoBehaviour
{
    [Header("Данные камеры")]
    public string cameraName = "Камера №4: Роща у перекрестка";
    [Tooltip("Фотография, которую покажет эта камера")]
    public Sprite snapshotSprite;

    [Header("Элементы объекта")]
    [Tooltip("Рамка/обводка, которая включается при выборе (дочерний объект)")]
    [SerializeField] private GameObject _outlineObject;
    [SerializeField] private Button _button;

    private RightMonitorMapController _controller;

    public void Initialize(RightMonitorMapController controller)
    {
        _controller = controller;

        if (_button == null) _button = GetComponent<Button>();
        if (_button != null)
        {
            _button.onClick.AddListener(OnClick);
        }

        SetSelected(false);
    }

    private void OnClick()
    {
        _controller.SelectCamera(this);
    }

    public void SetSelected(bool isSelected)
    {
        if (_outlineObject != null)
        {
            _outlineObject.SetActive(isSelected);
        }
    }
}