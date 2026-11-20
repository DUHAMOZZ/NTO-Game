using UnityEngine;
using UnityEngine.UI;

public class BuildingTarget : MonoBehaviour
{
    [Header("Данные здания")]
    [SerializeField] private string _buildingId = "Building_01";
    public string buildingName = "Заброшенный завод";
    public string sectorCode = "Сектор Б-3";

    [Header("Компоненты выделения")]
    [SerializeField] private GameObject _outlineObject;
    [SerializeField] private Button _button;

    private RightMonitorMapController _controller;

    public string BuildingId => string.IsNullOrEmpty(_buildingId) ? gameObject.name : _buildingId;

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
            _controller.SelectBuilding(this);
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