using UnityEngine;
using UnityEngine.UI;

public class BuildingTarget : MonoBehaviour
{
    [Header("Данные объекта")]
    public string buildingName = "Заброшенный склад";
    public string sectorCode = "Сектор Б-3";

    [Header("Визуальное выделение")]
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
        _controller.SelectBuilding(this);
    }

    public void SetSelected(bool isSelected)
    {
        if (_outlineObject != null)
        {
            _outlineObject.SetActive(isSelected);
        }
    }
}