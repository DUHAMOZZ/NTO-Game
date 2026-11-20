using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class SquadButtonSlot
{
    public SquadConfig config;
    public Button button;
    public TextMeshProUGUI labelText;
    public GameObject selectedHighlight;
}

public class LeftMonitorShopController : MonoBehaviour
{
    [Header("¬ÌÂ¯ÌËÂ ÒÒ˚ÎÍË")]
    [SerializeField] private RightMonitorMapController _rightMapMonitor;
    [SerializeField] private GameShiftController _shiftController;

    [Header("—ÎÓÚ˚ ÓÚˇ‰Ó‚ (ÒÂÚÍ‡ 2x2)")]
    [SerializeField] private SquadButtonSlot[] _squadSlots;

    [Header("HUD ÎÂ‚Ó„Ó ÏÓÌËÚÓ‡")]
    [SerializeField] private TextMeshProUGUI _currencyText;
    [SerializeField] private TextMeshProUGUI _timerText;
    [SerializeField] private GameObject _timerContainer;

    [Header("ŒÚÔ‡‚Í‡ Ë ‡‰ËÓÓÚ˜∏Ú")]
    [SerializeField] private Button _dispatchButton;
    [SerializeField] private TextMeshProUGUI _dispatchBtnText;
    [SerializeField] private TextMeshProUGUI _reportLogText;

    private SquadConfig _selectedSquad;
    private int _currentCurrency;
    private bool _dispatchAllowed;
    private BuildingTarget _cachedTarget;

    private void Awake()
    {
        SetupSquadButtons();

        if (_dispatchButton != null)
        {
            _dispatchButton.onClick.RemoveListener(OnDispatchClicked);
            _dispatchButton.onClick.AddListener(OnDispatchClicked);
        }

        if (_rightMapMonitor != null)
        {
            _rightMapMonitor.OnBuildingSelected += HandleBuildingSelected;
        }
    }

    private void OnDestroy()
    {
        if (_rightMapMonitor != null)
        {
            _rightMapMonitor.OnBuildingSelected -= HandleBuildingSelected;
        }
    }

    private void SetupSquadButtons()
    {
        if (_squadSlots == null) return;

        for (int i = 0; i < _squadSlots.Length; i++)
        {
            SquadButtonSlot slot = _squadSlots[i];
            if (slot == null || slot.config == null || slot.button == null) continue;

            if (slot.labelText != null)
            {
                slot.labelText.text = $"{slot.config.squadName} [{slot.config.cost}]";
            }

            if (slot.selectedHighlight != null)
            {
                slot.selectedHighlight.SetActive(false);
            }

            SquadConfig capturedCfg = slot.config;
            slot.button.onClick.RemoveAllListeners();
            slot.button.onClick.AddListener(() => SelectSquad(capturedCfg));
        }
    }

    public void InitializeState(int startingCurrency)
    {
        _currentCurrency = startingCurrency;
        UpdateCurrencyDisplay(startingCurrency);
        UpdateTimerDisplay(-1);
        ClearSelectedSquad();
    }

    public void SetDispatchEnabled(bool isEnabled)
    {
        _dispatchAllowed = isEnabled;
        if (!isEnabled)
        {
            ClearSelectedSquad();
        }
        RefreshDispatchButtonState();
    }

    public void UpdateCurrencyDisplay(int newCurrency)
    {
        _currentCurrency = newCurrency;
        if (_currencyText != null)
        {
            _currencyText.text = $" –€ÿ≈◊ »: {_currentCurrency}";
        }
        RefreshDispatchButtonState();
    }

    public void UpdateTimerDisplay(int remainingSeconds)
    {
        bool show = remainingSeconds >= 0;
        if (_timerContainer != null)
        {
            _timerContainer.SetActive(show);
        }

        if (_timerText != null)
        {
            _timerText.text = show ? $"¬–≈Ãﬂ: {remainingSeconds} Ò" : string.Empty;
        }
    }

    public void SetReportText(string text)
    {
        if (_reportLogText != null)
        {
            _reportLogText.text = text;
        }
    }

    public void ClearSelectedSquad()
    {
        _selectedSquad = null;
        UpdateSlotHighlights();
        RefreshDispatchButtonState();
    }

    private void HandleBuildingSelected(BuildingTarget target)
    {
        _cachedTarget = target;
        RefreshDispatchButtonState();
    }

    private void SelectSquad(SquadConfig cfg)
    {
        if (!_dispatchAllowed) return;

        _selectedSquad = cfg;
        UpdateSlotHighlights();
        RefreshDispatchButtonState();
    }

    private void UpdateSlotHighlights()
    {
        if (_squadSlots == null) return;

        for (int i = 0; i < _squadSlots.Length; i++)
        {
            SquadButtonSlot slot = _squadSlots[i];
            if (slot != null && slot.selectedHighlight != null)
            {
                slot.selectedHighlight.SetActive(_selectedSquad != null && slot.config == _selectedSquad);
            }
        }
    }

    private void RefreshDispatchButtonState()
    {
        if (_dispatchButton == null || _dispatchBtnText == null) return;

        if (!_dispatchAllowed)
        {
            _dispatchButton.interactable = false;
            _dispatchBtnText.text = "Œ∆»ƒ¿Õ»≈ ¬€«Œ¬¿";
            return;
        }

        if (_rightMapMonitor != null)
        {
            _cachedTarget = _rightMapMonitor.CurrentBuilding;
        }

        bool hasTarget = _cachedTarget != null;
        bool hasSquad = _selectedSquad != null;
        bool enoughMoney = hasSquad && (_currentCurrency >= _selectedSquad.cost);

        _dispatchButton.interactable = hasTarget && hasSquad && enoughMoney;

        if (!hasSquad)
        {
            _dispatchBtnText.text = "¬€¡≈–»“≈ Œ“–ﬂƒ";
        }
        else if (!hasTarget)
        {
            _dispatchBtnText.text = "¬€¡≈–»“≈ «ƒ¿Õ»≈ Õ¿  ¿–“≈";
        }
        else if (!enoughMoney)
        {
            _dispatchBtnText.text = $"Õ≈ ’¬¿“¿≈“  –€ÿ≈◊≈  ({_selectedSquad.cost})";
        }
        else
        {
            _dispatchBtnText.text = $"Œ“œ–¿¬»“‹ [{_selectedSquad.squadName}]";
        }
    }

    private void OnDispatchClicked()
    {
        if (!_dispatchAllowed || _selectedSquad == null || _rightMapMonitor == null || _rightMapMonitor.CurrentBuilding == null)
        {
            return;
        }

        SquadConfig dispatchedSquad = _selectedSquad;
        BuildingTarget targetBuilding = _rightMapMonitor.CurrentBuilding;
        ClearSelectedSquad();

        if (_shiftController != null)
        {
            _shiftController.HandleDispatchRequest(dispatchedSquad, targetBuilding);
        }
    }
}