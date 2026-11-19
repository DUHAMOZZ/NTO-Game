using System;
using AnomalyDispatch.Dialogue;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameShiftController : MonoBehaviour
{
    private enum ShiftPhase
    {
        Idle,
        WaitingInitialCall,
        InDialogue,
        WaitingRecall,
        ActionPhase,
        BetweenScenarios,
        BankruptPanic,
        ShiftCompleted
    }

    [Header("Конфигурация смены")]
    [SerializeField] private ShiftConfig _shiftConfig;

    [Header("Контроллеры сцены")]
    [SerializeField] private LeftMonitorShopController _leftMonitor;
    [SerializeField] private RightMonitorMapController _rightMonitor;
    [SerializeField] private DialogueUI _dialogueUI;

    [Header("UI, скрываемый во время звонка (кнопки поворота + PulseMonitorHUD)")]
    [SerializeField] private GameObject[] _hideDuringDialogue;

    [Header("Экран завершения / Гейм-овер (опционально на Canvas)")]
    [SerializeField] private GameObject _endScreenPanel;
    [SerializeField] private TextMeshProUGUI _endTitleText;
    [SerializeField] private TextMeshProUGUI _endSummaryText;
    [SerializeField] private Button _restartButton;
    [SerializeField] private float _panicBeforeGameOverSeconds = 3.5f;

    public event Action<float> OnSanityChanged;
    public event Action<float> OnEffectiveConditionChanged;

    public float CurrentSanityPercent
    {
        get
        {
            if (_shiftConfig == null || _shiftConfig.maxSanity <= 0f) return 100f;
            return Mathf.Clamp((_sanity / _shiftConfig.maxSanity) * 100f, 0f, 100f);
        }
    }

    public float CurrentEffectiveConditionPercent => _lastEffectiveCondition;

    private ShiftPhase _phase = ShiftPhase.Idle;
    private int _scenarioIndex;
    private int _currentStep = 1;
    private int _currency;
    private float _sanity;
    private int _resolvedSuccessCount;

    private float _phaseTimer;
    private bool _isScenarioTimerRunning;
    private float _scenarioTimeRemaining;
    private int _lastDisplayedTimerSec = -1;

    private float _shockStressBonus;
    private float _lastEffectiveCondition = 80f;

    private void Awake()
    {
        if (G.ConversationHandler != null)
        {
            G.ConversationHandler.ManagedExternally = true;
        }

        if (_dialogueUI != null && G.DialogueRunner != null)
        {
            G.DialogueRunner.RegisterUI(_dialogueUI);
        }

        if (_shiftConfig != null)
        {
            _sanity = Mathf.Clamp(_shiftConfig.startingSanity, 0f, _shiftConfig.maxSanity);
            _currency = _shiftConfig.startingCurrency;
        }

        if (_endScreenPanel != null)
        {
            _endScreenPanel.SetActive(false);
        }

        if (_restartButton != null)
        {
            _restartButton.onClick.RemoveListener(RestartShift);
            _restartButton.onClick.AddListener(RestartShift);
        }
    }

    private void OnEnable()
    {
        if (G.DialogueRunner != null)
        {
            G.DialogueRunner.OnConversationEnded += HandleConversationEnded;
        }
    }

    private void OnDisable()
    {
        if (G.DialogueRunner != null)
        {
            G.DialogueRunner.OnConversationEnded -= HandleConversationEnded;
        }
    }

    private void Start()
    {
        if (_shiftConfig == null || _shiftConfig.scenarios == null || _shiftConfig.scenarios.Count == 0)
        {
            Debug.LogError("[GameShiftController] ShiftConfig не назначен или не содержит сценариев!");
            return;
        }

        if (G.PlayerState != null)
        {
            G.PlayerState.ResetAll();
        }

        _currency = _shiftConfig.startingCurrency;
        _sanity = Mathf.Clamp(_shiftConfig.startingSanity, 0f, _shiftConfig.maxSanity);
        _scenarioIndex = 0;
        _resolvedSuccessCount = 0;
        _shockStressBonus = 0f;

        SyncPlayerState();
        OnSanityChanged?.Invoke(CurrentSanityPercent);
        RefreshEffectiveCondition(true);

        if (_leftMonitor != null)
        {
            _leftMonitor.InitializeState(_currency);
            _leftMonitor.SetDispatchEnabled(false);
            _leftMonitor.SetReportText("Смена началась. Ожидайте поступления звонка...");
        }

        if (_shiftConfig.delayBeforeFirstCall <= 0f)
        {
            StartScenario(_scenarioIndex);
        }
        else
        {
            SetDialogueHiddenObjectsVisible(true);
            _phase = ShiftPhase.WaitingInitialCall;
            _phaseTimer = _shiftConfig.delayBeforeFirstCall;
        }
    }

    private void Update()
    {
        if (_shockStressBonus > 0f)
        {
            _shockStressBonus = Mathf.MoveTowards(_shockStressBonus, 0f, Time.deltaTime * 3.5f);
        }

        switch (_phase)
        {
            case ShiftPhase.WaitingInitialCall:
                _phaseTimer -= Time.deltaTime;
                if (_phaseTimer <= 0f)
                {
                    StartScenario(_scenarioIndex);
                }
                break;

            case ShiftPhase.WaitingRecall:
                _phaseTimer -= Time.deltaTime;
                if (_phaseTimer <= 0f)
                {
                    StartCurrentDialogue();
                }
                break;

            case ShiftPhase.ActionPhase:
                if (_isScenarioTimerRunning)
                {
                    _scenarioTimeRemaining -= Time.deltaTime;
                    if (_scenarioTimeRemaining <= 0f)
                    {
                        _scenarioTimeRemaining = 0f;
                        _isScenarioTimerRunning = false;
                        UpdateTimerUI(0);
                        HandleScenarioTimeout();
                    }
                    else
                    {
                        int ceilSec = Mathf.CeilToInt(_scenarioTimeRemaining);
                        if (ceilSec != _lastDisplayedTimerSec)
                        {
                            UpdateTimerUI(ceilSec);
                        }
                    }
                }
                break;

            case ShiftPhase.BetweenScenarios:
                _phaseTimer -= Time.deltaTime;
                if (_phaseTimer <= 0f)
                {
                    int nextIndex = _scenarioIndex + 1;
                    if (nextIndex < _shiftConfig.scenarios.Count)
                    {
                        StartScenario(nextIndex);
                    }
                    else
                    {
                        FinishShift();
                    }
                }
                break;

            case ShiftPhase.BankruptPanic:
                _phaseTimer -= Time.deltaTime;
                if (_phaseTimer <= 0f)
                {
                    ShowBankruptGameOver();
                }
                break;
        }

        RefreshEffectiveCondition(false);
    }

    private ScenarioConfig CurrentScenario
    {
        get
        {
            if (_shiftConfig == null || _shiftConfig.scenarios == null) return null;
            if (_scenarioIndex < 0 || _scenarioIndex >= _shiftConfig.scenarios.Count) return null;
            return _shiftConfig.scenarios[_scenarioIndex];
        }
    }

    private void StartScenario(int index)
    {
        _scenarioIndex = index;
        _currentStep = 1;
        _isScenarioTimerRunning = false;
        _lastDisplayedTimerSec = -1;

        ScenarioConfig scenario = CurrentScenario;
        if (scenario == null)
        {
            FinishShift();
            return;
        }

        if (_rightMonitor != null)
        {
            _rightMonitor.ClearForNewScenario();
            _rightMonitor.SetScenarioClue(scenario);
        }

        if (_leftMonitor != null)
        {
            _leftMonitor.SetDispatchEnabled(false);
            _leftMonitor.UpdateTimerDisplay(-1);
            _leftMonitor.SetReportText($"Входящий вызов: {scenario.scenarioTitle}");
        }

        StartCurrentDialogue();
    }

    private void StartCurrentDialogue()
    {
        ScenarioConfig scenario = CurrentScenario;
        if (scenario == null || scenario.dialogue == null || G.DialogueRunner == null)
        {
            EnterActionPhase();
            return;
        }

        _phase = ShiftPhase.InDialogue;
        SetDialogueHiddenObjectsVisible(false);
        G.DialogueRunner.StartConversation(scenario.dialogue);
    }

    private void HandleConversationEnded(DialogueNode lastNode)
    {
        if (_phase != ShiftPhase.InDialogue) return;

        SetDialogueHiddenObjectsVisible(true);

        ScenarioConfig scenario = CurrentScenario;
        if (scenario != null &&
            !string.IsNullOrEmpty(scenario.ignoreEndingId) &&
            lastNode != null &&
            string.Equals(lastNode.endingId, scenario.ignoreEndingId, StringComparison.Ordinal))
        {
            ModifySanity(scenario.ignoreSanityDelta);
            if (_leftMonitor != null)
            {
                _leftMonitor.SetReportText("Вы положили трубку. Рассудок +1. Через мгновение телефон звонит снова...");
            }

            _phase = ShiftPhase.WaitingRecall;
            _phaseTimer = Mathf.Max(0.5f, scenario.recallDelaySeconds);
            return;
        }

        EnterActionPhase();
    }

    private void EnterActionPhase()
    {
        ScenarioConfig scenario = CurrentScenario;
        if (scenario == null) return;

        _phase = ShiftPhase.ActionPhase;
        SetDialogueHiddenObjectsVisible(true);

        if (_leftMonitor != null)
        {
            _leftMonitor.SetDispatchEnabled(true);
            _leftMonitor.SetReportText(string.IsNullOrEmpty(scenario.callerNotesSummary)
                ? "Изучите камеры на правом мониторе и отправьте подходящий отряд."
                : scenario.callerNotesSummary);
        }

        if (scenario.timeLimitSeconds > 0f)
        {
            _scenarioTimeRemaining = scenario.timeLimitSeconds;
            _isScenarioTimerRunning = true;
            UpdateTimerUI(Mathf.CeilToInt(_scenarioTimeRemaining));
        }
        else
        {
            _isScenarioTimerRunning = false;
            UpdateTimerUI(-1);
        }

        RefreshEffectiveCondition(true);
    }

    public void HandleDispatchRequest(SquadConfig squad, BuildingTarget target)
    {
        if (_phase != ShiftPhase.ActionPhase || squad == null || target == null) return;
        if (_currency < squad.cost) return;

        ScenarioConfig scenario = CurrentScenario;
        if (scenario == null) return;

        ModifyCurrency(-squad.cost);

        bool isLastScenario = (_scenarioIndex >= _shiftConfig.scenarios.Count - 1);

        bool isCorrectBuilding = string.Equals(target.BuildingId, scenario.targetBuildingId, StringComparison.OrdinalIgnoreCase);
        if (!isCorrectBuilding)
        {
            ModifySanity(scenario.wrongBuildingSanityDelta);
            AddShockSpike(25f);

            if (_currency <= 0)
            {
                TriggerBankruptPanic(scenario.wrongBuildingReport);
                return;
            }

            if (_leftMonitor != null)
            {
                _leftMonitor.SetReportText(scenario.wrongBuildingReport);
            }

            if (scenario.wrongBuildingEndsScenario)
            {
                EndCurrentScenario();
            }
            return;
        }

        SquadDispatchRule rule = scenario.FindRule(_currentStep, squad);
        if (rule == null)
        {
            if (_currency <= 0)
            {
                TriggerBankruptPanic($"Отряд {squad.squadName} направлен на объект, результата нет.");
                return;
            }

            if (_leftMonitor != null)
            {
                _leftMonitor.SetReportText($"Отряд {squad.squadName} направлен на объект, результата нет.");
            }
            EndCurrentScenario();
            return;
        }

        if (rule.rewardCurrency != 0) ModifyCurrency(rule.rewardCurrency);
        if (rule.sanityDelta != 0) ModifySanity(rule.sanityDelta);
        if (rule.sanityDelta < 0) AddShockSpike(Mathf.Clamp(-rule.sanityDelta * 4f, 15f, 40f));

        if (_leftMonitor != null && !string.IsNullOrEmpty(rule.radioReport))
        {
            _leftMonitor.SetReportText($"[РАЦИЯ]: {rule.radioReport}");
        }

        switch (rule.actionType)
        {
            case DispatchActionType.SuccessEndScenario:
                _resolvedSuccessCount++;
                if (_currency <= 0 && !isLastScenario)
                {
                    TriggerBankruptPanic(rule.radioReport);
                    return;
                }
                EndCurrentScenario();
                break;

            case DispatchActionType.AdvanceToStep2:
                _currentStep = 2;
                if (_currency <= 0)
                {
                    ModifySanity(scenario.timeoutSanityDelta);
                    TriggerBankruptPanic($"{rule.radioReport} Не осталось крышечек на второй отряд!");
                }
                break;

            case DispatchActionType.FailEndScenario:
                if (_currency <= 0)
                {
                    TriggerBankruptPanic(rule.radioReport);
                    return;
                }
                EndCurrentScenario();
                break;

            case DispatchActionType.NonTerminalMistake:
                if (_currency <= 0)
                {
                    ModifySanity(scenario.timeoutSanityDelta);
                    TriggerBankruptPanic(rule.radioReport);
                }
                break;
        }
    }

    private void HandleScenarioTimeout()
    {
        ScenarioConfig scenario = CurrentScenario;
        if (scenario == null) return;

        ModifySanity(scenario.timeoutSanityDelta);
        AddShockSpike(35f);

        string report = (_currentStep == 2 && !string.IsNullOrEmpty(scenario.step2TimeoutReport))
            ? scenario.step2TimeoutReport
            : scenario.timeoutReport;

        if (_currency <= 0)
        {
            TriggerBankruptPanic(report);
            return;
        }

        if (_leftMonitor != null)
        {
            _leftMonitor.SetReportText($"[РАЦИЯ]: {report}");
        }

        EndCurrentScenario();
    }

    private void TriggerBankruptPanic(string lastReport)
    {
        _isScenarioTimerRunning = false;
        UpdateTimerUI(-1);
        SetDialogueHiddenObjectsVisible(true);

        if (_leftMonitor != null)
        {
            _leftMonitor.SetDispatchEnabled(false);
            _leftMonitor.SetReportText(
                $"[РАЦИЯ]: {lastReport}\n\n" +
                "КРЫШЕЧЕК БОЛЬШЕ НЕТ. ОТРЯДЫ НЕДОСТУПНЫ. ОПЕРАТОР В БЕЗЫСХОДНОСТИ...");
        }

        _phase = ShiftPhase.BankruptPanic;
        _phaseTimer = _panicBeforeGameOverSeconds;
        RefreshEffectiveCondition(true);
    }

    private void ShowBankruptGameOver()
    {
        _phase = ShiftPhase.ShiftCompleted;
        _isScenarioTimerRunning = false;
        UpdateTimerUI(-1);
        SetDialogueHiddenObjectsVisible(true);

        if (_leftMonitor != null)
        {
            _leftMonitor.SetDispatchEnabled(false);
        }

        string title = "БЕЗЫСХОДНОСТЬ — СМЕНА ПРОВАЛЕНА";
        string summary =
            $"Запасы крышечек полностью исчерпаны.\n" +
            $"Отправлять отряды больше не на что.\n\n" +
            $"Успешно решено вызовов: {_resolvedSuccessCount} из {_shiftConfig.scenarios.Count}\n" +
            $"Рассудок: {Mathf.RoundToInt(_sanity)}/{Mathf.RoundToInt(_shiftConfig.maxSanity)}";

        ShowEndScreen(title, summary);
    }

    private void EndCurrentScenario()
    {
        _isScenarioTimerRunning = false;
        UpdateTimerUI(-1);

        if (_leftMonitor != null)
        {
            _leftMonitor.SetDispatchEnabled(false);
        }

        _phase = ShiftPhase.BetweenScenarios;
        _phaseTimer = _shiftConfig.delayBetweenScenarios;
        RefreshEffectiveCondition(true);
    }

    private void FinishShift()
    {
        _phase = ShiftPhase.ShiftCompleted;
        _isScenarioTimerRunning = false;
        UpdateTimerUI(-1);
        SetDialogueHiddenObjectsVisible(true);

        if (_leftMonitor != null)
        {
            _leftMonitor.SetDispatchEnabled(false);
        }

        string grade = _resolvedSuccessCount == _shiftConfig.scenarios.Count
            ? "ИДЕАЛЬНАЯ СМЕНА"
            : (_resolvedSuccessCount >= 2 ? "СМЕНА ЗАВЕРШЕНА" : "ТЯЖЁЛАЯ НОЧЬ");

        string summary =
            $"Решено вызовов: {_resolvedSuccessCount} из {_shiftConfig.scenarios.Count}\n" +
            $"Остаток крышечек: {_currency}\n" +
            $"Итоговый рассудок: {Mathf.RoundToInt(_sanity)}/{Mathf.RoundToInt(_shiftConfig.maxSanity)}";

        ShowEndScreen(grade, summary);
    }

    private void ShowEndScreen(string title, string summary)
    {
        if (_leftMonitor != null)
        {
            _leftMonitor.SetReportText($"{title}\n{summary}");
        }

        if (_endScreenPanel != null)
        {
            _endScreenPanel.SetActive(true);
            if (_endTitleText != null) _endTitleText.text = title;
            if (_endSummaryText != null) _endSummaryText.text = summary;
        }
    }

    public void RestartShift()
    {
        if (G.DialogueRunner != null && G.DialogueRunner.IsRunning)
        {
            G.DialogueRunner.AbortConversation();
        }
        if (G.PlayerState != null)
        {
            G.PlayerState.ResetAll();
        }
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void SetDialogueHiddenObjectsVisible(bool isVisible)
    {
        if (_hideDuringDialogue == null) return;
        for (int i = 0; i < _hideDuringDialogue.Length; i++)
        {
            if (_hideDuringDialogue[i] != null && _hideDuringDialogue[i].activeSelf != isVisible)
            {
                _hideDuringDialogue[i].SetActive(isVisible);
            }
        }
    }

    private void ModifyCurrency(int delta)
    {
        _currency = Mathf.Max(0, _currency + delta);
        SyncPlayerState();
        if (_leftMonitor != null)
        {
            _leftMonitor.UpdateCurrencyDisplay(_currency);
        }
    }

    private void ModifySanity(int delta)
    {
        _sanity = Mathf.Clamp(_sanity + delta, 0f, _shiftConfig.maxSanity);
        SyncPlayerState();
        OnSanityChanged?.Invoke(CurrentSanityPercent);
        RefreshEffectiveCondition(true);
    }

    private void AddShockSpike(float amount)
    {
        _shockStressBonus = Mathf.Clamp(_shockStressBonus + amount, 0f, 55f);
        RefreshEffectiveCondition(true);
    }

    private void RefreshEffectiveCondition(bool forceNotify)
    {
        float effective = CalculateEffectiveConditionPercent();
        if (forceNotify || Mathf.Abs(effective - _lastEffectiveCondition) >= 0.25f)
        {
            _lastEffectiveCondition = effective;
            OnEffectiveConditionChanged?.Invoke(_lastEffectiveCondition);
        }
    }

    private float CalculateEffectiveConditionPercent()
    {
        if (_phase == ShiftPhase.BankruptPanic || (_currency <= 0 && _phase != ShiftPhase.ShiftCompleted))
        {
            return 0f;
        }

        if (_phase == ShiftPhase.ShiftCompleted && _currency <= 0 && _resolvedSuccessCount < _shiftConfig.scenarios.Count)
        {
            return 0f;
        }

        float stress = 100f - CurrentSanityPercent;

        if (_phase != ShiftPhase.ShiftCompleted)
        {
            if (_currency == 3) stress += 10f;
            else if (_currency == 2) stress += 24f;
            else if (_currency == 1) stress += 42f;
        }

        if (_phase == ShiftPhase.ActionPhase && _isScenarioTimerRunning && _scenarioTimeRemaining < 45f)
        {
            float urgency = 1f - Mathf.Clamp01(_scenarioTimeRemaining / 45f);
            stress += urgency * 36f;
        }

        stress += _shockStressBonus;

        return Mathf.Clamp(100f - stress, 0f, 100f);
    }

    private void UpdateTimerUI(int wholeSeconds)
    {
        _lastDisplayedTimerSec = wholeSeconds;
        if (_leftMonitor != null)
        {
            _leftMonitor.UpdateTimerDisplay(wholeSeconds);
        }
    }

    private void SyncPlayerState()
    {
        if (G.PlayerState != null)
        {
            G.PlayerState.SetNumber("caps", _currency);
            G.PlayerState.SetNumber("sanity", _sanity);
        }
    }

#if UNITY_EDITOR
    public string DebugStatusSummary =>
        $"Фаза: {_phase} | Сценарий: {(CurrentScenario != null ? CurrentScenario.scenarioTitle : "Нет")} | Шаг: {_currentStep} | Крышечки: {_currency} | Рассудок: {_sanity} | Эфф. состояние: {_lastEffectiveCondition:0}";

    public void DebugJumpToScenario(int index)
    {
        if (_shiftConfig == null || _shiftConfig.scenarios == null) return;
        if (index < 0 || index >= _shiftConfig.scenarios.Count) return;

        if (_endScreenPanel != null) _endScreenPanel.SetActive(false);

        if (G.DialogueRunner != null && G.DialogueRunner.IsRunning)
        {
            G.DialogueRunner.AbortConversation();
        }

        StartScenario(index);
    }

    public void DebugSkipDialogue()
    {
        if (_phase != ShiftPhase.InDialogue) return;

        if (G.DialogueRunner != null && G.DialogueRunner.IsRunning)
        {
            G.DialogueRunner.AbortConversation();
        }

        EnterActionPhase();
    }

    public void DebugAddCurrency(int amount)
    {
        ModifyCurrency(amount);
        RefreshEffectiveCondition(true);
    }

    public void DebugSetSanity(float sanityValue)
    {
        _sanity = Mathf.Clamp(sanityValue, 0f, _shiftConfig.maxSanity);
        SyncPlayerState();
        OnSanityChanged?.Invoke(CurrentSanityPercent);
        RefreshEffectiveCondition(true);
    }

    public void DebugSetTimerRemaining(float seconds)
    {
        if (_phase == ShiftPhase.ActionPhase && _isScenarioTimerRunning)
        {
            _scenarioTimeRemaining = Mathf.Max(0.1f, seconds);
            UpdateTimerUI(Mathf.CeilToInt(_scenarioTimeRemaining));
            RefreshEffectiveCondition(true);
        }
    }
#endif
}