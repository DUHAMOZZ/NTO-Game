using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LeftMonitorShopController : MonoBehaviour
{
    [Header("Связь с правым монитором")]
    [SerializeField] private RightMonitorMapController _rightMapMonitor;

    [Header("Экономика и Рассудок")]
    [SerializeField] private int _currency = 150; // Стартовые очки/валюта
    [SerializeField] private Slider _sanitySlider; // Шкала рассудка (0-100)
    [SerializeField] private TextMeshProUGUI _currencyText;

    [Header("Кнопки покупки отрядов (Магазин)")]
    [SerializeField] private Button _btnBuyGBR;       // Дешевый, быстрый (цена: 50)
    [SerializeField] private Button _btnBuySpetsnaz;  // Тяжелый (цена: 100)
    [SerializeField] private Button _btnBuyBiohazard; // Дорогой спецотряд (цена: 150)

    [Header("Кнопка отправки")]
    [SerializeField] private Button _dispatchButton;
    [SerializeField] private TextMeshProUGUI _dispatchBtnText;

    [Header("Окно отчета / Результат")]
    [SerializeField] private TextMeshProUGUI _reportLogText;

    // Выбранный отряд для покупки
    private string _selectedSquadName = null;
    private int _selectedSquadCost = 0;

    private void Start()
    {
        // Привязываем кнопки магазина
        _btnBuyGBR.onClick.AddListener(() => SelectSquad("ГБР (Быстрый)", 50));
        _btnBuySpetsnaz.onClick.AddListener(() => SelectSquad("Тяжелый Спецназ", 100));
        _btnBuyBiohazard.onClick.AddListener(() => SelectSquad("Группа Биозащиты", 150));

        _dispatchButton.onClick.AddListener(OnDispatchClicked);

        UpdateUI();
    }

    private void Update()
    {
        // Проверяем: если и на левом мониторе выбран отряд, и на правом выбрано здание — кнопка отправки активна
        bool hasBuilding = _rightMapMonitor != null && _rightMapMonitor.CurrentBuilding != null;
        bool hasSquad = !string.IsNullOrEmpty(_selectedSquadName);

        _dispatchButton.interactable = hasBuilding && hasSquad && (_currency >= _selectedSquadCost);

        if (!hasSquad)
        {
            _dispatchBtnText.text = "ВЫБЕРИТЕ ОТРЯД В МАГАЗИНЕ";
        }
        else if (!hasBuilding)
        {
            _dispatchBtnText.text = "ВЫБЕРИТЕ ЗДАНИЕ НА КАРТЕ СПРАВА";
        }
        else
        {
            _dispatchBtnText.text = $"ОТПРАВИТЬ {_selectedSquadName} В [{_rightMapMonitor.CurrentBuilding.buildingName}]";
        }
    }

    private void SelectSquad(string squadName, int cost)
    {
        _selectedSquadName = squadName;
        _selectedSquadCost = cost;
        _reportLogText.text = $"Выбран для заказа: {squadName}. Стоимость: {cost} квот.";
    }

    private void OnDispatchClicked()
    {
        if (_currency < _selectedSquadCost) return;

        // Списываем валюту
        _currency -= _selectedSquadCost;

        string targetBuilding = _rightMapMonitor.CurrentBuilding.buildingName;

        // Простая симуляция последствий для прототипа:
        // Если отправили Спецназ — угроза устранена, рассудок на месте, получаем награду
        if (_selectedSquadName.Contains("Спецназ"))
        {
            _currency += 120; // Награда за успешную операцию
            _reportLogText.text = $"ОТЧЕТ: Отряд {_selectedSquadName} зачистил здание {targetBuilding}! Получена премия +120.";
        }
        else
        {
            // Не тот отряд -> провал, рассудок падает!
            _sanitySlider.value -= 25f;
            _reportLogText.text = $"ОТЧЕТ: Отряд {_selectedSquadName} уничтожен в {targetBuilding}! Аномалия прорвалась! Рассудок снижен!";
        }

        // Сбрасываем выбор
        _selectedSquadName = null;
        _selectedSquadCost = 0;

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (_currencyText != null)
            _currencyText.text = $"БЮДЖЕТ: {_currency}";
    }
}