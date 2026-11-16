using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LeftMonitorShopController : MonoBehaviour
{
    [SerializeField] private RightMonitorMapController _rightMapMonitor;
    [SerializeField] private SquadConfig[] _squadConfigs; // Сюда закинем 4 конфига
    [SerializeField] private Button[] _squadButtons;

    [SerializeField] private int _currency = 15;
    [SerializeField] private Slider _sanitySlider;
    [SerializeField] private TextMeshProUGUI _currencyText;

    [SerializeField] private Button _dispatchButton;
    [SerializeField] private TextMeshProUGUI _dispatchBtnText;
    [SerializeField] private TextMeshProUGUI _reportLogText;

    private SquadConfig _selectedSquad;

    private void Start()
    {
        for (int i = 0; i < _squadButtons.Length; i++)
        {
            if (i < _squadConfigs.Length && _squadConfigs[i] != null)
            {
                SquadConfig cfg = _squadConfigs[i];
                TextMeshProUGUI txt = _squadButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null) txt.text = $"{cfg.squadName} [{cfg.cost}]";

                int index = i;
                _squadButtons[i].onClick.RemoveAllListeners();
                _squadButtons[i].onClick.AddListener(() => SelectSquad(_squadConfigs[index]));
            }
        }
        _dispatchButton.onClick.AddListener(OnDispatchClicked);
        UpdateUI();
    }

    private void Update()
    {
        bool hasTarget = _rightMapMonitor != null && _rightMapMonitor.CurrentBuilding != null;
        bool hasSquad = _selectedSquad != null;
        _dispatchButton.interactable = hasTarget && hasSquad && (_currency >= _selectedSquad.cost);

        if (!hasSquad) _dispatchBtnText.text = "ВЫБЕРИТЕ ОТРЯД";
        else if (!hasTarget) _dispatchBtnText.text = "ВЫБЕРИТЕ ЗДАНИЕ СПРАВА";
        else _dispatchBtnText.text = $"ОТПРАВИТЬ [{_selectedSquad.squadName}]";
    }

    private void SelectSquad(SquadConfig cfg)
    {
        _selectedSquad = cfg;
        _reportLogText.text = $"Выбран: {cfg.squadName} (Цена: {cfg.cost})";
    }

    private void OnDispatchClicked()
    {
        _currency -= _selectedSquad.cost;
        _reportLogText.text = $"Отряд {_selectedSquad.squadName} отправлен на объект!";
        _selectedSquad = null;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (_currencyText != null) _currencyText.text = $"БЮДЖЕТ: {_currency}";
    }
}