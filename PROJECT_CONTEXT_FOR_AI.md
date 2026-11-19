# ПОЛНЫЙ КОНТЕКСТ ПРОЕКТА ДЛЯ НЕЙРОСЕТИ (DeltaTime Studio)
Дата формирования: 19.11.2026 17:50:21
Версия Unity: 6000.3.28f1
Активная сцена: 0
======================================================================

## 1. ИСХОДНЫЙ КОД C# СКРИПТОВ (Assets/Scripts)
Ниже представлены все пользовательские скрипты игровой логики:

### Файл: `BuildingTarget.cs` (Assets/Scripts/BuildingTarget.cs)
```csharp
using UnityEngine;
using UnityEngine.UI;

public class BuildingTarget : MonoBehaviour
{
    [Header("������ �������")]
    public string buildingName = "����������� �����";
    public string sectorCode = "������ �-3";

    [Header("���������� ���������")]
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
```

### Файл: `CameraPoint.cs` (Assets/Scripts/CameraPoint.cs)
```csharp
using UnityEngine;
using UnityEngine.UI;

public class CameraPoint : MonoBehaviour
{
    [Header("������ ������")]
    public string cameraName = "������ �4: ���� � �����������";
    [Tooltip("����������, ������� ������� ��� ������")]
    public Sprite snapshotSprite;

    [Header("�������� �������")]
    [Tooltip("�����/�������, ������� ���������� ��� ������ (�������� ������)")]
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
```

### Файл: `LeftMonitorShopController.cs` (Assets/Scripts/LeftMonitorShopController.cs)
```csharp
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LeftMonitorShopController : MonoBehaviour
{
    [SerializeField] private RightMonitorMapController _rightMapMonitor;
    [SerializeField] private SquadConfig[] _squadConfigs; // ���� ������� 4 �������
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

        if (!hasSquad) _dispatchBtnText.text = "�������� �����";
        else if (!hasTarget) _dispatchBtnText.text = "�������� ������ ������";
        else _dispatchBtnText.text = $"��������� [{_selectedSquad.squadName}]";
    }

    private void SelectSquad(SquadConfig cfg)
    {
        _selectedSquad = cfg;
        _reportLogText.text = $"������: {cfg.squadName} (����: {cfg.cost})";
    }

    private void OnDispatchClicked()
    {
        _currency -= _selectedSquad.cost;
        _reportLogText.text = $"����� {_selectedSquad.squadName} ��������� �� ������!";
        _selectedSquad = null;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (_currencyText != null) _currencyText.text = $"������: {_currency}";
    }
}
```

### Файл: `RightMonitorMapController.cs` (Assets/Scripts/RightMonitorMapController.cs)
```csharp
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RightMonitorMapController : MonoBehaviour
{
    [Header("������ ������������� �����")]
    [SerializeField] private CameraPoint[] _cameraPoints;
    [SerializeField] private BuildingTarget[] _buildingTargets;

    [Header("����� �����: ������ ������")]
    [Tooltip("CanvasGroup ������ ������ ��� ����������� ������������")]
    [SerializeField] private CanvasGroup _snapshotBtnCanvasGroup;
    [SerializeField] private Button _takeSnapshotButton;
    [SerializeField] private Image _snapshotDisplayImage;
    [SerializeField] private GameObject _noSignalPlaceholder; // ������ �����/������ �� ������� ����
    [SerializeField] private Image _screenFlashImage;        // ����� ������� �������

    [Header("�������������� ������� (UI TextMeshPro)")]
    [SerializeField] private TextMeshProUGUI _cameraStatusText;
    [SerializeField] private TextMeshProUGUI _targetBuildingText;

    // �������� ��� ������ (����� �� ��� ��������� ����� ��� �������� ������ � ������ ��������)
    public CameraPoint CurrentCamera { get; private set; }
    public BuildingTarget CurrentBuilding { get; private set; }

    private void Awake()
    {
        // �������������� ������
        if (_cameraPoints != null)
        {
            foreach (var cam in _cameraPoints)
            {
                if (cam != null) cam.Initialize(this);
            }
        }

        // �������������� ������
        if (_buildingTargets != null)
        {
            foreach (var bld in _buildingTargets)
            {
                if (bld != null) bld.Initialize(this);
            }
        }

        if (_takeSnapshotButton != null)
        {
            _takeSnapshotButton.onClick.AddListener(OnTakeSnapshotClicked);
        }

        // �� ��������� ������ �� ������� -> ������ ��������� (0.25f) � ���������
        SetSnapshotButtonActive(false);

        if (_noSignalPlaceholder != null) _noSignalPlaceholder.SetActive(true);
        if (_screenFlashImage != null) _screenFlashImage.gameObject.SetActive(false);

        UpdateStatusText();
    }

    public void SelectCamera(CameraPoint selectedCam)
    {
        CurrentCamera = selectedCam;

        foreach (var cam in _cameraPoints)
        {
            if (cam != null) cam.SetSelected(cam == selectedCam);
        }

        // ������� ������ � ������ ������ ���������� ����� � ������������!
        SetSnapshotButtonActive(true);
        UpdateStatusText();
    }

    public void SelectBuilding(BuildingTarget selectedBuilding)
    {
        CurrentBuilding = selectedBuilding;

        foreach (var bld in _buildingTargets)
        {
            if (bld != null) bld.SetSelected(bld == selectedBuilding);
        }

        UpdateStatusText();
    }

    private void OnTakeSnapshotClicked()
    {
        if (CurrentCamera == null || CurrentCamera.snapshotSprite == null) return;

        // ���������� ������
        _snapshotDisplayImage.sprite = CurrentCamera.snapshotSprite;

        if (_noSignalPlaceholder != null)
        {
            _noSignalPlaceholder.SetActive(false);
        }

        // ������ ���������� ������� �������
        if (_screenFlashImage != null)
        {
            StartCoroutine(FlashRoutine());
        }
    }

    private IEnumerator FlashRoutine()
    {
        _screenFlashImage.gameObject.SetActive(true);
        Color startColor = new Color(1f, 1f, 1f, 0.75f);
        _screenFlashImage.color = startColor;

        float duration = 0.12f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(0.75f, 0f, elapsed / duration);
            _screenFlashImage.color = new Color(1f, 1f, 1f, alpha);
            yield return null;
        }

        _screenFlashImage.gameObject.SetActive(false);
    }

    private void SetSnapshotButtonActive(bool isActive)
    {
        if (_snapshotBtnCanvasGroup != null)
        {
            // ���������� � �������������� (0.25f) ��� ������������ (1.0f)
            _snapshotBtnCanvasGroup.alpha = isActive ? 1.0f : 0.25f;
            _snapshotBtnCanvasGroup.interactable = isActive;
            _snapshotBtnCanvasGroup.blocksRaycasts = isActive;
        }
    }

    private void UpdateStatusText()
    {
        if (_cameraStatusText != null)
        {
            _cameraStatusText.text = CurrentCamera != null
                ? $"������: {CurrentCamera.cameraName}"
                : "������: �������� ������";
        }

        if (_targetBuildingText != null)
        {
            _targetBuildingText.text = CurrentBuilding != null
                ? $"����: {CurrentBuilding.buildingName} [{CurrentBuilding.sectorCode}]"
                : "���� ��������: �� �������";
        }
    }
}
```

### Файл: `RoomViewController.cs` (Assets/Scripts/RoomViewController.cs)
```csharp
using System.Collections;
using UnityEngine;

public class RoomViewController : MonoBehaviour
{
    [Header("��������� �����")]
    [Tooltip("������ ����� �� ��� Y (��������: 0 - ����, 90 - ������ �������, 180 - ����� �����, -90 - ����� �������)")]
    [SerializeField] private float[] _targetYawAngles = new float[] { -45f, 0f, 45f, 180f };

    [Tooltip("��������� ������ �������")]
    [SerializeField] private int _currentIndex = 1; // �� ��������� ������� � ����� (0 ��������)

    [Header("��������� ���������")]
    [Tooltip("����� �������� � ��������")]
    [SerializeField] private float _rotationDuration = 0.35f;

    [Tooltip("������ ��������� �������� (������ � ���������� ������)")]
    [SerializeField] private AnimationCurve _smoothCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Tooltip("����������� �� ������� �� ����� �� 360 ��������")]
    [SerializeField] private bool _isCyclic = true;

    private Coroutine _rotationCoroutine;

    private void Start()
    {
        // ������������� ��������� ���� ��� �������� ��� ������ ����
        if (_targetYawAngles != null && _targetYawAngles.Length > 0)
        {
            _currentIndex = Mathf.Clamp(_currentIndex, 0, _targetYawAngles.Length - 1);
            transform.rotation = Quaternion.Euler(0f, _targetYawAngles[_currentIndex], 0f);
        }
    }

    /// <summary>
    /// ������� �� ���� ��� ����� (������ �� ������ UI �����)
    /// </summary>
    public void RotateLeft()
    {
        ChangeViewIndex(-1);
    }

    /// <summary>
    /// ������� �� ���� ��� ������ (������ �� ������ UI ������)
    /// </summary>
    public void RotateRight()
    {
        ChangeViewIndex(1);
    }

    private void ChangeViewIndex(int direction)
    {
        if (_targetYawAngles == null || _targetYawAngles.Length == 0) return;

        int newIndex = _currentIndex + direction;

        if (_isCyclic)
        {
            // ��������� ������� �� ����� (� ���������� �� ������ � ��������)
            if (newIndex >= _targetYawAngles.Length) newIndex = 0;
            else if (newIndex < 0) newIndex = _targetYawAngles.Length - 1;
        }
        else
        {
            // ��������� � ������� ��� ��������� ��������
            newIndex = Mathf.Clamp(newIndex, 0, _targetYawAngles.Length - 1);
            if (newIndex == _currentIndex) return; // ��� �� ����
        }

        _currentIndex = newIndex;
        RotateToAngle(_targetYawAngles[_currentIndex]);
    }

    private void RotateToAngle(float targetAngleY)
    {
        // ���� ��� �������� � ��������� ������� �������, ����� ���������� ����� ����
        if (_rotationCoroutine != null)
        {
            StopCoroutine(_rotationCoroutine);
        }

        _rotationCoroutine = StartCoroutine(SmoothRotateRoutine(targetAngleY));
    }

    private IEnumerator SmoothRotateRoutine(float targetYaw)
    {
        Quaternion startRotation = transform.rotation;
        Quaternion targetRotation = Quaternion.Euler(0f, targetYaw, 0f);

        float elapsed = 0f;

        while (elapsed < _rotationDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / _rotationDuration);
            float curveValue = _smoothCurve.Evaluate(progress);

            // Quaternion.Slerp ��� �������� ���������� ���� �������� �����
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, curveValue);

            yield return null;
        }

        transform.rotation = targetRotation;
        _rotationCoroutine = null;
    }
}
```

### Файл: `SquadConfig.cs` (Assets/Scripts/SquadConfig.cs)
```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "NewSquad", menuName = "DeltaTime/Squad Config")]
public class SquadConfig : ScriptableObject 
{
    public string squadName = "���";
    public int cost = 1;
    [TextArea] public string description = "������� �����";
}
```

### Файл: `G.cs` (Assets/Game/Scripts/Game/G.cs)
```csharp
using AnomalyDispatch.Dialogue;
using UnityEngine;

public static class G
{
    // каждый менеджер должен быть с интерфейсом IServise
    // тут объявлять менеджеры
    //public static GameMain GameMain;

    public static CMS CMS;
    public static PlayerState PlayerState;
    public static DialogueRunner DialogueRunner;
    public static ConversationHandler ConversationHandler;
}

[DefaultExecutionOrder(-9999)]
public static class Bootstrapper
{
    private static bool _initialized = false;
    private static GameObject serviceHolder;

    // запускается перед созданием сцены
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void OnBeforeSceneLoad()
    {
        if(_initialized) return;

        serviceHolder = new GameObject("===Services===");
        Object.DontDestroyOnLoad(serviceHolder);

        // тут порядок создания
        //G.GameMain = CreateSimpleService<GameMain>();

        G.CMS = CreateSimpleService<CMS>();
        G.PlayerState = CreateSimpleService<PlayerState>();
        G.DialogueRunner = CreateSimpleService<DialogueRunner>();
        G.ConversationHandler = CreateSimpleService<ConversationHandler>();

        //можно еще так, когда сцена загружается
        //G.SceneLoader.OnLoadAction = () =>
        //{
        //    G.GameMain.Setup();
        //};

        

        _initialized = true;

    }

    private static T CreateSimpleService<T>() where T : Component, IService
    {
        GameObject g = new GameObject(typeof(T).ToString());

        g.transform.parent = serviceHolder.transform;
        T t = g.AddComponent<T>();
        t.Init();
        return g.GetComponent<T>();
    }
    
}

public interface IService
{
    public void Init();
}
```

### Файл: `PlayerState.cs` (Assets/Game/Scripts/Game/PlayerState.cs)
```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace AnomalyDispatch.Dialogue
{
    public class PlayerState : MonoBehaviour, IService
    {

        private readonly Dictionary<string, float> _flags = new();

        public event Action<string, float> OnFlagChanged;
        public event Action OnStateReset;

        public void Init()
        {
            
        }
        public float GetNumber(string key)
            => !string.IsNullOrEmpty(key) && _flags.TryGetValue(key, out var v) ? v : 0f;

        public bool HasFlag(string key) => GetNumber(key) >= 1f;

        public void SetNumber(string key, float value)
        {
            if (string.IsNullOrEmpty(key)) return;
            if (float.IsNaN(value) || float.IsInfinity(value)) return; // Защита состояния
            
            var prev = GetNumber(key);
            if (Mathf.Approximately(prev, value)) return;
            _flags[key] = value;
            OnFlagChanged?.Invoke(key, value);
        }

        public void SetFlag(string key, bool value) => SetNumber(key, value ? 1f : 0f);

        public void ResetAll()
        {
            _flags.Clear();
            OnStateReset?.Invoke();
        }

        public List<FlagEntry> ExportFlags()
        {
            var list = new List<FlagEntry>(_flags.Count);
            foreach (var kv in _flags) list.Add(new FlagEntry { key = kv.Key, value = kv.Value });
            return list;
        }

        public void ImportFlags(List<FlagEntry> entries)
        {
            _flags.Clear();
            if (entries == null) return;
            foreach (var e in entries) _flags[e.key] = e.value;
        }
    }

    [Serializable]
    public class FlagEntry
    {
        public string key;
        public float value;
    }
}
```

### Файл: `CMS.cs` (Assets/Game/Scripts/Game/CMS/CMS.cs)
```csharp
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using AnomalyDispatch.Dialogue;

public class CMS : MonoBehaviour, IService
{
    // Data storage
    //private GameConfig gameConfig;

    private List<DialogueConversation> dialogueConversations = new List<DialogueConversation>();

    public event Action OnContentLoaded;
    public event Action OnContentReloaded;
    

    public void Init()
    {
        //очередь загрузки
        //LoadGameConfig
        LoadDialogueConversations();

        LoadAllContent();
    }
    
    public void LoadAllContent()
    {
        OnContentLoaded?.Invoke();
    }
    
    //загрузка
    // private void LoadGameConfig()
    //{
    //    gameConfig = Resources.Load<GameConfig>("GameConfig");
    //    }
    //}
    private void LoadDialogueConversations()
    {
        DialogueConversation[] dialogueConversations_ = Resources.LoadAll<DialogueConversation>("dialogueConversations");
        foreach (var dc in dialogueConversations_)
        {
            dialogueConversations.Add(dc);
        }
    }

    //получение
    //public GameConfig GetGameCongig()
    //{
        //return gameConfig;
    //}

    public DialogueConversation[] GetAllDialogueConversations()
    {
        if(dialogueConversations != null)
        {
            return dialogueConversations.ToArray();
        }
        return null;
    }

    
    public void ReloadContent()
    {
        LoadAllContent();
        OnContentReloaded?.Invoke();
    }
    
}
```

### Файл: `ConversationHandler.cs` (Assets/Game/Scripts/Gameplay/Dialogue/ConversationHandler.cs)
```csharp
using AnomalyDispatch.Dialogue;
using UnityEngine;

public class ConversationHandler : MonoBehaviour, IService
{
    private DialogueRunner runner;
    private DialogueConversation[] dialogueConversations;

    public void Init()
    {
        runner = G.DialogueRunner;
        dialogueConversations = G.CMS.GetAllDialogueConversations();
    }

    private void Start()
    {
        StartRandomConversation();
    }

    private void StartRandomConversation()
    {
        if (dialogueConversations == null || dialogueConversations.Length == 0)
        {
            Debug.LogWarning("[ConversationHandler] No dialogue");
            return;
        }

        int randomIndex = Random.Range(0, dialogueConversations.Length);
        runner.StartConversation(dialogueConversations[randomIndex]);
    }
}
```

### Файл: `DialogueConversation.cs` (Assets/Game/Scripts/Gameplay/Dialogue/DialogueConversation.cs)
```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace AnomalyDispatch.Dialogue
{
    [CreateAssetMenu(menuName = "Anomaly Dispatch/Dialogue Conversation", fileName = "NewConversation")]
    public class DialogueConversation : ScriptableObject
    {
        public string conversationId = "conv_01";
        public string startNodeId = "start";
        public List<DialogueNode> nodes = new();

        private Dictionary<string, DialogueNode> _lookup;

        public DialogueNode GetNode(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (_lookup == null) BuildLookup();
            return _lookup.TryGetValue(id, out var node) ? node : null;
        }

        private void BuildLookup()
        {
            _lookup = new Dictionary<string, DialogueNode>();
            foreach (var n in nodes)
                if (n != null && !string.IsNullOrEmpty(n.id)) _lookup[n.id] = n;
        }

        private void OnValidate() => _lookup = null;
    }

    [Serializable]
    public class DialogueNode
    {
        public string id = "start";
        public string speaker = "Dispatcher";
        [TextArea(3, 8)] public string text;

        [Header("effects of entering a node")]
        public List<FlagEffect> onEnterEffects = new();

        [Header("Choices (if empty, proceed via nextNodeId)")]
        public List<DialogueChoice> choices = new();

        [Tooltip("Where to go when there are no elections? A void + no choices = the end of the dialogue.")]
        public string nextNodeId;

        [Header("Final")]
        public bool isEnding;
        public string endingId;

        public bool HasChoices => choices != null && choices.Count > 0;
    }

    [Serializable]
    public class DialogueChoice
    {
        [TextArea(1, 3)] public string text;
        public string targetNodeId;

        [Header("Display conditions (all must be met)")]
        public List<FlagCondition> conditions = new();

        [Header("The effects of Choice")]
        public List<FlagEffect> effects = new();

        [Tooltip("Hide the option if the condition is not met. Otherwise, show it in gray.")]
        public bool hideIfUnavailable = true;
    }

    [Serializable]
    public class FlagCondition
    {
        public string flagKey;
        public ComparisonType comparison = ComparisonType.IsTrue;
        public float value;

        public bool Evaluate(PlayerState state)
        {
            if (state == null) return true;
            float v = state.GetNumber(flagKey);
            return comparison switch
            {
                ComparisonType.IsTrue        => v >= 1f,
                ComparisonType.IsFalse       => v < 1f,
                ComparisonType.Equals        => Mathf.Approximately(v, value),
                ComparisonType.NotEquals     => !Mathf.Approximately(v, value),
                ComparisonType.Greater       => v > value,
                ComparisonType.Less          => v < value,
                ComparisonType.GreaterOrEqual => v >= value,
                ComparisonType.LessOrEqual   => v <= value,
                _ => true
            };
        }
    }

    public enum ComparisonType
    {
        IsTrue, IsFalse, Equals, NotEquals, Greater, Less, GreaterOrEqual, LessOrEqual
    }

    [Serializable]
    public class FlagEffect
    {
        public string flagKey;
        public EffectType type = EffectType.SetTrue;
        public float value;

        public void Apply(PlayerState state)
        {
            if (state == null || string.IsNullOrEmpty(flagKey)) return;
            switch (type)
            {
                case EffectType.SetTrue:  state.SetNumber(flagKey, 1f); break;
                case EffectType.SetFalse: state.SetNumber(flagKey, 0f); break;
                case EffectType.Set:      state.SetNumber(flagKey, value); break;
                case EffectType.Add:      state.SetNumber(flagKey, state.GetNumber(flagKey) + value); break;
                case EffectType.Subtract: state.SetNumber(flagKey, state.GetNumber(flagKey) - value); break;
            }
        }
    }

    public enum EffectType { SetTrue, SetFalse, Set, Add, Subtract }
}
```

### Файл: `DialogueRunner.cs` (Assets/Game/Scripts/Gameplay/Dialogue/DialogueRunner.cs)
```csharp
using System;
using UnityEngine;

namespace AnomalyDispatch.Dialogue
{
    public class DialogueRunner : MonoBehaviour, IService
    {
        private DialogueUI ui;

        public event Action<DialogueNode> OnConversationEnded;

        public DialogueConversation CurrentConversation { get; private set; }
        public DialogueNode CurrentNode { get; private set; }
        public bool IsRunning { get; private set; }

        public void Init()
        {
        }

        void Start()
        {
            ui = FindFirstObjectByType<DialogueUI>();
        }

        public void StartConversation(DialogueConversation conversation, string overrideStartNode = null)
        {
            if (conversation == null) { Debug.LogError("[Dialogue] Conversation is null"); return; }
            if (ui == null) { Debug.LogError("[Dialogue] UI is not found on the scene!"); return; }

            CurrentConversation = conversation;
            IsRunning = true;

            var startId = string.IsNullOrEmpty(overrideStartNode) ? conversation.startNodeId : overrideStartNode;
            var node = conversation.GetNode(startId);
            if (node == null)
            {
                Debug.LogError($"[Dialogue] Start node '{startId}' not found in {conversation.name}");
                EndConversation();
                return;
            }

            ui.Show();
            PresentNode(node);
        }

        private void PresentNode(DialogueNode node)
        {
            CurrentNode = node;

            if (node.onEnterEffects != null)
                foreach (var eff in node.onEnterEffects) eff.Apply(G.PlayerState);

            ui.ShowNode(node);

            if (node.isEnding)
            {
                ui.WaitForContinue(EndConversation);
                return;
            }

            if (node.HasChoices)
            {
                ui.ShowChoices(node.choices, G.PlayerState, OnChoiceSelected);
                return;
            }

            if (string.IsNullOrEmpty(node.nextNodeId))
            {
                ui.WaitForContinue(EndConversation);
                return;
            }

            ui.WaitForContinue(() => PresentNode(CurrentConversation.GetNode(node.nextNodeId)));
        }

        private void OnChoiceSelected(DialogueChoice choice)
        {
            if (choice == null) return;

            if (choice.effects != null)
                foreach (var eff in choice.effects) eff.Apply(G.PlayerState);

            var next = CurrentConversation.GetNode(choice.targetNodeId);
            if (next == null)
            {
                Debug.LogError($"[Dialogue] Node '{choice.targetNodeId}' not found");
                EndConversation();
                return;
            }
            PresentNode(next);
        }

        private void EndConversation()
        {
            IsRunning = false;
            ui.Hide();
            OnConversationEnded?.Invoke(CurrentNode);
        }
    }
}
```

### Файл: `DialogueUI.cs` (Assets/Game/Scripts/Gameplay/Dialogue/DialogueUI.cs)
```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyDispatch.Dialogue
{
    public class DialogueUI : MonoBehaviour
    {
        [SerializeField] private GameObject rootPanel;
        [SerializeField] private TMP_Text speakerText;
        [SerializeField] private TMP_Text bodyText;

        [SerializeField] private Transform choicesContainer;
        [SerializeField] private Button choiceButtonPrefab;

        [SerializeField] private float charsPerSecond = 45f;
        [SerializeField] private float delayBeforeChoices = 0.25f;

        private Coroutine _typeRoutine;
        private Action _onContinue;
        private bool _isTyping;
        private bool _waitingForClick;
        private bool _waitingForChoice;
        private readonly List<GameObject> _spawnedChoices = new();

        public void Show()  { if (rootPanel) rootPanel.SetActive(true); }
        public void Hide()  { if (rootPanel) rootPanel.SetActive(false); ClearChoices(); }

        public void ShowNode(DialogueNode node)
        {
            ClearChoices();
            _waitingForClick = false;
            _waitingForChoice = false;

            if (speakerText) speakerText.text = node.speaker;

            if (_typeRoutine != null) StopCoroutine(_typeRoutine);
            _typeRoutine = StartCoroutine(TypewriterRoutine(node.text));
        }

        public void WaitForContinue(Action onContinue)
        {
            _onContinue = onContinue;
            _waitingForClick = true;
        }

        public void ShowChoices(List<DialogueChoice> choices, PlayerState state, Action<DialogueChoice> onPick)
        {
            if (choicesContainer == null || choiceButtonPrefab == null)
            {
                Debug.LogError("[DialogueUI] is not configured.");
                return;
            }
        
            _waitingForChoice = true;
            StartCoroutine(SpawnChoicesRoutine(choices, state, onPick));
        }

        private IEnumerator SpawnChoicesRoutine(List<DialogueChoice> choices, PlayerState state, Action<DialogueChoice> onPick)
        {
            while (_isTyping) yield return null;
            yield return new WaitForSeconds(delayBeforeChoices);

            foreach (var ch in choices)
            {
                bool available = AreConditionsMet(ch.conditions, state);
                if (!available && ch.hideIfUnavailable) continue;

                var btn = Instantiate(choiceButtonPrefab, choicesContainer);
                var label = btn.GetComponentInChildren<TMP_Text>();
                if (label) label.text = ch.text;
                btn.interactable = available;

                var captured = ch;
                btn.onClick.AddListener(() =>
                {
                    if (!_waitingForChoice) return;
                    _waitingForChoice = false;
                    ClearChoices();
                    onPick?.Invoke(captured);
                });

                _spawnedChoices.Add(btn.gameObject);
            }
        }

        private static bool AreConditionsMet(List<FlagCondition> conditions, PlayerState state)
        {
            if (conditions == null) return true;
            foreach (var c in conditions)
                if (!c.Evaluate(state)) return false;
            return true;
        }

        private IEnumerator TypewriterRoutine(string text)
        {
            _isTyping = true;
            bodyText.text = text;
            bodyText.maxVisibleCharacters = 0;
            bodyText.ForceMeshUpdate();

            int total = bodyText.textInfo.characterCount;
            int visible = 0;
            float acc = 0f;

            while (visible < total)
            {
                acc += Time.deltaTime * charsPerSecond;
                int target = Mathf.Min(total, Mathf.FloorToInt(acc));
                if (target != visible) { visible = target; bodyText.maxVisibleCharacters = visible; }
                yield return null;
            }

            bodyText.maxVisibleCharacters = total;
            _isTyping = false;
            _typeRoutine = null;
        }

        private void SkipTypewriter()
        {
            if (_typeRoutine != null) StopCoroutine(_typeRoutine);
            bodyText.maxVisibleCharacters = bodyText.textInfo.characterCount;
            _isTyping = false;
            _typeRoutine = null;
        }

        private void ClearChoices()
        {
            foreach (var go in _spawnedChoices) if (go) Destroy(go);
            _spawnedChoices.Clear();
        }

        private void Update()
        {
            if (!WasClickedThisFrame() && !WasSpacePressed()) return;

            if (_isTyping) { SkipTypewriter(); return; }

            if (_waitingForClick && _onContinue != null)
            {
                var cb = _onContinue;
                _onContinue = null;
                _waitingForClick = false;
                cb();
            }
        }

        // Поддержка и старого Input Manager, и нового Input System
        private static bool WasClickedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            var m = UnityEngine.InputSystem.Mouse.current;
            return m != null && m.leftButton.wasPressedThisFrame;
#else
            return Input.GetMouseButtonDown(0);
#endif
        }

        private static bool WasSpacePressed()
        {
#if ENABLE_INPUT_SYSTEM
            var k = UnityEngine.InputSystem.Keyboard.current;
            return k != null && k.spaceKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Space);
#endif
        }
    }
}
```

======================================================================

## 2. КОНФИГУРАЦИОННЫЕ ДАННЫЕ (ScriptableObjects)
Все созданные ассеты-конфигурации и их параметры:

### Конфиг: `Squad_GBR` [Тип: SquadConfig] (`Assets/Configs/Squad_GBR.asset`)
  - **Squad Name** (`squadName`): "ГБР"
  - **Cost** (`cost`): 1
  - **Description** (`description`): "Быстрый вызов"

### Конфиг: `Squad_Heavy` [Тип: SquadConfig] (`Assets/Configs/Squad_Heavy.asset`)
  - **Squad Name** (`squadName`): "Тяжелый отряд"
  - **Cost** (`cost`): 5
  - **Description** (`description`): "Быстрый вызов"

### Конфиг: `Squad_Mage` [Тип: SquadConfig] (`Assets/Configs/Squad_Mage.asset`)
  - **Squad Name** (`squadName`): "Знахари"
  - **Cost** (`cost`): 3
  - **Description** (`description`): "Быстрый вызов"

### Конфиг: `Squad_Medic` [Тип: SquadConfig] (`Assets/Configs/Squad_Medic.asset`)
  - **Squad Name** (`squadName`): "Скорая"
  - **Cost** (`cost`): 2
  - **Description** (`description`): "Быстрый вызов"

### Конфиг: `NewConversation` [Тип: DialogueConversation] (`Assets/Game/Resources/dialogueConversations/NewConversation.asset`)
  - **Conversation Id** (`conversationId`): "conv_01"
  - **Start Node Id** (`startNodeId`): "start"
  - **Nodes** (`nodes`): ...

### Конфиг: `InputSystem_Actions` [Тип: InputActionAsset] (`Assets/InputSystem_Actions.inputactions`)
  - **Action Maps** (`m_ActionMaps`): ...
  - **Control Schemes** (`m_ControlSchemes`): ...
  - **Is Project Wide** (`m_IsProjectWide`): False

### Конфиг: `LiberationSans SDF - Fallback` [Тип: TMP_FontAsset] (`Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset`)
  - **Version** (`m_Version`): "1.1.0"
  - **Face Info** (`m_FaceInfo`): ...
  - **Material** (`m_Material`): LiberationSans SDF Material
  - **Source Font File GUID** (`m_SourceFontFileGUID`): "e3265ab4bf004d28a9537516768c1c75"
  - **Creation Settings** (`m_CreationSettings`): ...
  - **Source Font File** (`m_SourceFontFile`): LiberationSans
  - **Source Font File Path** (`m_SourceFontFilePath`): ""
  - **Atlas Population Mode** (`m_AtlasPopulationMode`): Dynamic
  - **Internal Dynamic OS** (`InternalDynamicOS`): False
  - **Glyph Table** (`m_GlyphTable`): ...
  - **Character Table** (`m_CharacterTable`): ...
  - **Atlas Textures** (`m_AtlasTextures`): ...
  - **Atlas Texture Index** (`m_AtlasTextureIndex`): 1
  - **Is Multi Atlas Textures Enabled** (`m_IsMultiAtlasTexturesEnabled`): True
  - **Get Font Features** (`m_GetFontFeatures`): True
  - **Clear Dynamic Data On Build** (`m_ClearDynamicDataOnBuild`): True
  - **Atlas Width** (`m_AtlasWidth`): 512
  - **Atlas Height** (`m_AtlasHeight`): 512
  - **Atlas Padding** (`m_AtlasPadding`): 9
  - **Atlas Render Mode** (`m_AtlasRenderMode`): SDFAA_HINTED
  - **Used Glyph Rects** (`m_UsedGlyphRects`): ...
  - **Free Glyph Rects** (`m_FreeGlyphRects`): ...
  - **Font Feature Table** (`m_FontFeatureTable`): ...
  - **Should Reimport Font Features** (`m_ShouldReimportFontFeatures`): False
  - **Fallback Font Asset Table** (`m_FallbackFontAssetTable`): ...
  - **Font Weight Table** (`m_FontWeightTable`): ...
  - **Font Weights** (`fontWeights`): ...
  - **Normal Style** (`normalStyle`): 0,00
  - **Normal Spacing Offset** (`normalSpacingOffset`): 0,00
  - **Bold Style** (`boldStyle`): 0,75
  - **Bold Spacing** (`boldSpacing`): 7,00
  - **Italic Style** (`italicStyle`): 35
  - **Tab Size** (`tabSize`): 10
  - **Font Info** (`m_fontInfo`): ...
  - **Glyph Info List** (`m_glyphInfoList`): ...
  - **Kerning Table** (`m_KerningTable`): ...
  - **Fallback Font Assets** (`fallbackFontAssets`): ...
  - **Atlas** (`atlas`): [NULL]

### Конфиг: `LiberationSans SDF` [Тип: TMP_FontAsset] (`Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset`)
  - **Version** (`m_Version`): "1.1.0"
  - **Face Info** (`m_FaceInfo`): ...
  - **Material** (`m_Material`): LiberationSans SDF Material
  - **Source Font File GUID** (`m_SourceFontFileGUID`): "e3265ab4bf004d28a9537516768c1c75"
  - **Creation Settings** (`m_CreationSettings`): ...
  - **Source Font File** (`m_SourceFontFile`): [NULL]
  - **Source Font File Path** (`m_SourceFontFilePath`): ""
  - **Atlas Population Mode** (`m_AtlasPopulationMode`): Static
  - **Internal Dynamic OS** (`InternalDynamicOS`): False
  - **Glyph Table** (`m_GlyphTable`): ...
  - **Character Table** (`m_CharacterTable`): ...
  - **Atlas Textures** (`m_AtlasTextures`): ...
  - **Atlas Texture Index** (`m_AtlasTextureIndex`): 0
  - **Is Multi Atlas Textures Enabled** (`m_IsMultiAtlasTexturesEnabled`): False
  - **Get Font Features** (`m_GetFontFeatures`): True
  - **Clear Dynamic Data On Build** (`m_ClearDynamicDataOnBuild`): False
  - **Atlas Width** (`m_AtlasWidth`): 1024
  - **Atlas Height** (`m_AtlasHeight`): 1024
  - **Atlas Padding** (`m_AtlasPadding`): 9
  - **Atlas Render Mode** (`m_AtlasRenderMode`): SDFAA_HINTED
  - **Used Glyph Rects** (`m_UsedGlyphRects`): ...
  - **Free Glyph Rects** (`m_FreeGlyphRects`): ...
  - **Font Feature Table** (`m_FontFeatureTable`): ...
  - **Should Reimport Font Features** (`m_ShouldReimportFontFeatures`): False
  - **Fallback Font Asset Table** (`m_FallbackFontAssetTable`): ...
  - **Font Weight Table** (`m_FontWeightTable`): ...
  - **Font Weights** (`fontWeights`): ...
  - **Normal Style** (`normalStyle`): 0,00
  - **Normal Spacing Offset** (`normalSpacingOffset`): 0,00
  - **Bold Style** (`boldStyle`): 0,75
  - **Bold Spacing** (`boldSpacing`): 7,00
  - **Italic Style** (`italicStyle`): 35
  - **Tab Size** (`tabSize`): 10
  - **Font Info** (`m_fontInfo`): ...
  - **Glyph Info List** (`m_glyphInfoList`): ...
  - **Kerning Table** (`m_KerningTable`): ...
  - **Fallback Font Assets** (`fallbackFontAssets`): ...
  - **Atlas** (`atlas`): [NULL]

### Конфиг: `EmojiOne` [Тип: TMP_SpriteAsset] (`Assets/TextMesh Pro/Resources/Sprite Assets/EmojiOne.asset`)
  - **Version** (`m_Version`): "1.1.0"
  - **Face Info** (`m_FaceInfo`): ...
  - **Material** (`m_Material`): TextMeshPro/Sprite
  - **Sprite Sheet** (`spriteSheet`): EmojiOne
  - **Sprite Character Table** (`m_SpriteCharacterTable`): ...
  - **Glyph Table** (`m_GlyphTable`): ...
  - **Sprite Info List** (`spriteInfoList`): ...
  - **Fallback Sprite Assets** (`fallbackSpriteAssets`): ...

### Конфиг: `Default Style Sheet` [Тип: TMP_StyleSheet] (`Assets/TextMesh Pro/Resources/Style Sheets/Default Style Sheet.asset`)
  - **Style List** (`m_StyleList`): ...

### Конфиг: `TMP Settings` [Тип: TMP_Settings] (`Assets/TextMesh Pro/Resources/TMP Settings.asset`)
  - **Asset Version** (`assetVersion`): "2"
  - **Text Wrapping Mode** (`m_TextWrappingMode`): Normal
  - **Enable Kerning** (`m_enableKerning`): True
  - **Active Font Features** (`m_ActiveFontFeatures`): ...
  - **Enable Extra Padding** (`m_enableExtraPadding`): False
  - **Enable Tint All Sprites** (`m_enableTintAllSprites`): False
  - **Enable Parse Escape Characters** (`m_enableParseEscapeCharacters`): True
  - **Enable Raycast Target** (`m_EnableRaycastTarget`): True
  - **Get Font Features At Runtime** (`m_GetFontFeaturesAtRuntime`): True
  - **Missing Glyph Character** (`m_missingGlyphCharacter`): 0
  - **Clear Dynamic Data On Build** (`m_ClearDynamicDataOnBuild`): True
  - **Warnings Disabled** (`m_warningsDisabled`): False
  - **Default Font Asset** (`m_defaultFontAsset`): LiberationSans SDF
  - **Default Font Asset Path** (`m_defaultFontAssetPath`): "Fonts & Materials/"
  - **Default Font Size** (`m_defaultFontSize`): 36,00
  - **Default Auto Size Min Ratio** (`m_defaultAutoSizeMinRatio`): 0,50
  - **Default Auto Size Max Ratio** (`m_defaultAutoSizeMaxRatio`): 2,00
  - **Default Text Mesh Pro Text Container Size** (`m_defaultTextMeshProTextContainerSize`): ...
  - **Default Text Mesh Pro UI Text Container Size** (`m_defaultTextMeshProUITextContainerSize`): ...
  - **Auto Size Text Container** (`m_autoSizeTextContainer`): False
  - **Is Text Object Scale Static** (`m_IsTextObjectScaleStatic`): False
  - **Fallback Font Assets** (`m_fallbackFontAssets`): ...
  - **Match Material Preset** (`m_matchMaterialPreset`): True
  - **Hide Sub Text Objects** (`m_HideSubTextObjects`): False
  - **Default Sprite Asset** (`m_defaultSpriteAsset`): EmojiOne
  - **Default Sprite Asset Path** (`m_defaultSpriteAssetPath`): "Sprite Assets/"
  - **Enable Emoji Support** (`m_enableEmojiSupport`): True
  - **Missing Character Sprite Unicode** (`m_MissingCharacterSpriteUnicode`): 0
  - **Emoji Fallback Text Assets** (`m_EmojiFallbackTextAssets`): ...
  - **Default Color Gradient Presets Path** (`m_defaultColorGradientPresetsPath`): "Color Gradient Presets/"
  - **Default Style Sheet** (`m_defaultStyleSheet`): Default Style Sheet
  - **Style Sheets Resource Path** (`m_StyleSheetsResourcePath`): ""
  - **Leading Characters** (`m_leadingCharacters`): LineBreaking Leading Characters
  - **Following Characters** (`m_followingCharacters`): LineBreaking Following Characters
  - **Use Modern Hangul Line Breaking Rules** (`m_UseModernHangulLineBreakingRules`): False

======================================================================

## 3. ИЕРАРХИЯ СЦЕНЫ И ПРИВЯЗКИ В ИНСПЕКТОРЕ
Структура объектов сцены '0' со всеми связями:

? **Directional Light** [Disabled] (Layer: Default)
   • Component: `Light`
      ?? Type = Directional
      ?? Intensity = 0,00
      ?? Range = 10,00
      ?? Spot Angle = 30,00
      ?? Inner Spot Angle = 21,80
      ?? Cookie: **[NULL / НЕ НАЗНАЧЕНО]**
      ?? Draw Halo = False
      ?? Flare: **[NULL / НЕ НАЗНАЧЕНО]**
      ?? Render Mode = Auto
      ?? Rendering Layer Mask = 1
      ?? Lightmapping = Realtime
      ?? Light Shadow Caster Mode = 0
      ?? Bounce Intensity = 1,00
      ?? Color Temperature = 5000,00
      ?? Use Color Temperature = True
      ?? Use Bounding Sphere Override = False
      ?? Use View Frustum For Shadow Caster Cull = True
      ?? Force Visible = False
      ?? Shadow Radius = 0,00
      ?? Shadow Angle = 0,00
      ?? Light Unit = 1
      ?? Lux At Distance = 1,00
      ?? Enable Spot Reflector = True
   • Component: `UniversalAdditionalLightData`
      ?? Use Pipeline Settings = True
      ?? Additional Lights Shadow Resolution Tier = 2
      ?? Custom Shadow Layers = False
      ?? Soft Shadow Quality = Low
      ?? Version = Rendering Layers Mask
      ?? Light Layer Mask = Light Layer Default
      ?? Shadow Layer Mask = Light Layer Default
      ?? Rendering Layers = 1
      ?? Shadow Rendering Layers = 1
? **Global Volume** [Active] (Layer: Default)
   • Component: `Volume`
      ?? Is Global = True
      ?? Priority = 0,00
      ?? Blend Distance = 0,00
      ?? Weight = 1,00
      ?? Shared Profile: `SampleSceneProfile` (VolumeProfile)
? **room** [Active] (Layer: Default)
   ??? **blockout** [Active] (Layer: Default)
      ??? **floor** [Active] (Layer: Default)
         • Component: `MeshFilter`
            ?? Mesh: `Cube` (Mesh)
         • Component: `MeshRenderer`
            ?? Cast Shadows = On
            ?? Receive Shadows = True
            ?? Dynamic Occludee = True
            ?? Static Shadow Caster = False
            ?? Motion Vectors = Per Object Motion
            ?? Light Probe Usage = 1
            ?? Reflection Probe Usage = 1
            ?? Ray Tracing Mode = 2
            ?? Ray Trace Procedural = False
            ?? Ray Tracing Accel Struct Build Flags Override = False
            ?? Ray Tracing Accel Struct Build Flags = 1
            ?? Small Mesh Culling = True
            ?? Force Mesh Lod = -1
            ?? Mesh Lod Selection Bias = 0,00
            ?? Rendering Layer Mask = 1
            ?? Renderer Priority = 0
            ?? Probe Anchor: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Light Probe Volume Override: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Scale In Lightmap = 1,00
            ?? Receive GI = 1
            ?? Preserve U Vs = False
            ?? Ignore Normals For Chart Detection = False
            ?? Important GI = False
            ?? Stitch Lightmap Seams = True
            ?? Minimum Chart Size = 4
            ?? Auto UV Max Distance = 0,50
            ?? Auto UV Max Angle = 89,00
            ?? Lightmap Parameters: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Mask Interaction = None
         • Component: `BoxCollider`
            ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Layer Override Priority = 0
            ?? Is Trigger = False
            ?? Provides Contacts = False
      ??? **ceiling** [Active] (Layer: Default)
         • Component: `MeshFilter`
            ?? Mesh: `Cube` (Mesh)
         • Component: `MeshRenderer`
            ?? Cast Shadows = On
            ?? Receive Shadows = True
            ?? Dynamic Occludee = True
            ?? Static Shadow Caster = False
            ?? Motion Vectors = Per Object Motion
            ?? Light Probe Usage = 1
            ?? Reflection Probe Usage = 1
            ?? Ray Tracing Mode = 2
            ?? Ray Trace Procedural = False
            ?? Ray Tracing Accel Struct Build Flags Override = False
            ?? Ray Tracing Accel Struct Build Flags = 1
            ?? Small Mesh Culling = True
            ?? Force Mesh Lod = -1
            ?? Mesh Lod Selection Bias = 0,00
            ?? Rendering Layer Mask = 1
            ?? Renderer Priority = 0
            ?? Probe Anchor: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Light Probe Volume Override: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Scale In Lightmap = 1,00
            ?? Receive GI = 1
            ?? Preserve U Vs = False
            ?? Ignore Normals For Chart Detection = False
            ?? Important GI = False
            ?? Stitch Lightmap Seams = True
            ?? Minimum Chart Size = 4
            ?? Auto UV Max Distance = 0,50
            ?? Auto UV Max Angle = 89,00
            ?? Lightmap Parameters: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Mask Interaction = None
         • Component: `BoxCollider`
            ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Layer Override Priority = 0
            ?? Is Trigger = False
            ?? Provides Contacts = False
      ??? **wall 1** [Active] (Layer: Default)
         • Component: `MeshFilter`
            ?? Mesh: `Cube` (Mesh)
         • Component: `MeshRenderer`
            ?? Cast Shadows = On
            ?? Receive Shadows = True
            ?? Dynamic Occludee = True
            ?? Static Shadow Caster = False
            ?? Motion Vectors = Per Object Motion
            ?? Light Probe Usage = 1
            ?? Reflection Probe Usage = 1
            ?? Ray Tracing Mode = 2
            ?? Ray Trace Procedural = False
            ?? Ray Tracing Accel Struct Build Flags Override = False
            ?? Ray Tracing Accel Struct Build Flags = 1
            ?? Small Mesh Culling = True
            ?? Force Mesh Lod = -1
            ?? Mesh Lod Selection Bias = 0,00
            ?? Rendering Layer Mask = 1
            ?? Renderer Priority = 0
            ?? Probe Anchor: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Light Probe Volume Override: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Scale In Lightmap = 1,00
            ?? Receive GI = 1
            ?? Preserve U Vs = False
            ?? Ignore Normals For Chart Detection = False
            ?? Important GI = False
            ?? Stitch Lightmap Seams = True
            ?? Minimum Chart Size = 4
            ?? Auto UV Max Distance = 0,50
            ?? Auto UV Max Angle = 89,00
            ?? Lightmap Parameters: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Mask Interaction = None
         • Component: `BoxCollider`
            ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Layer Override Priority = 0
            ?? Is Trigger = False
            ?? Provides Contacts = False
      ??? **wall 2** [Active] (Layer: Default)
         • Component: `MeshFilter`
            ?? Mesh: `Cube` (Mesh)
         • Component: `MeshRenderer`
            ?? Cast Shadows = On
            ?? Receive Shadows = True
            ?? Dynamic Occludee = True
            ?? Static Shadow Caster = False
            ?? Motion Vectors = Per Object Motion
            ?? Light Probe Usage = 1
            ?? Reflection Probe Usage = 1
            ?? Ray Tracing Mode = 2
            ?? Ray Trace Procedural = False
            ?? Ray Tracing Accel Struct Build Flags Override = False
            ?? Ray Tracing Accel Struct Build Flags = 1
            ?? Small Mesh Culling = True
            ?? Force Mesh Lod = -1
            ?? Mesh Lod Selection Bias = 0,00
            ?? Rendering Layer Mask = 1
            ?? Renderer Priority = 0
            ?? Probe Anchor: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Light Probe Volume Override: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Scale In Lightmap = 1,00
            ?? Receive GI = 1
            ?? Preserve U Vs = False
            ?? Ignore Normals For Chart Detection = False
            ?? Important GI = False
            ?? Stitch Lightmap Seams = True
            ?? Minimum Chart Size = 4
            ?? Auto UV Max Distance = 0,50
            ?? Auto UV Max Angle = 89,00
            ?? Lightmap Parameters: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Mask Interaction = None
         • Component: `BoxCollider`
            ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Layer Override Priority = 0
            ?? Is Trigger = False
            ?? Provides Contacts = False
      ??? **wall 3** [Active] (Layer: Default)
         • Component: `MeshFilter`
            ?? Mesh: `Cube` (Mesh)
         • Component: `MeshRenderer`
            ?? Cast Shadows = On
            ?? Receive Shadows = True
            ?? Dynamic Occludee = True
            ?? Static Shadow Caster = False
            ?? Motion Vectors = Per Object Motion
            ?? Light Probe Usage = 1
            ?? Reflection Probe Usage = 1
            ?? Ray Tracing Mode = 2
            ?? Ray Trace Procedural = False
            ?? Ray Tracing Accel Struct Build Flags Override = False
            ?? Ray Tracing Accel Struct Build Flags = 1
            ?? Small Mesh Culling = True
            ?? Force Mesh Lod = -1
            ?? Mesh Lod Selection Bias = 0,00
            ?? Rendering Layer Mask = 1
            ?? Renderer Priority = 0
            ?? Probe Anchor: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Light Probe Volume Override: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Scale In Lightmap = 1,00
            ?? Receive GI = 1
            ?? Preserve U Vs = False
            ?? Ignore Normals For Chart Detection = False
            ?? Important GI = False
            ?? Stitch Lightmap Seams = True
            ?? Minimum Chart Size = 4
            ?? Auto UV Max Distance = 0,50
            ?? Auto UV Max Angle = 89,00
            ?? Lightmap Parameters: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Mask Interaction = None
         • Component: `BoxCollider`
            ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Layer Override Priority = 0
            ?? Is Trigger = False
            ?? Provides Contacts = False
      ??? **wall 4** [Active] (Layer: Default)
         • Component: `MeshFilter`
            ?? Mesh: `Cube` (Mesh)
         • Component: `MeshRenderer`
            ?? Cast Shadows = On
            ?? Receive Shadows = True
            ?? Dynamic Occludee = True
            ?? Static Shadow Caster = False
            ?? Motion Vectors = Per Object Motion
            ?? Light Probe Usage = 1
            ?? Reflection Probe Usage = 1
            ?? Ray Tracing Mode = 2
            ?? Ray Trace Procedural = False
            ?? Ray Tracing Accel Struct Build Flags Override = False
            ?? Ray Tracing Accel Struct Build Flags = 1
            ?? Small Mesh Culling = True
            ?? Force Mesh Lod = -1
            ?? Mesh Lod Selection Bias = 0,00
            ?? Rendering Layer Mask = 1
            ?? Renderer Priority = 0
            ?? Probe Anchor: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Light Probe Volume Override: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Scale In Lightmap = 1,00
            ?? Receive GI = 1
            ?? Preserve U Vs = False
            ?? Ignore Normals For Chart Detection = False
            ?? Important GI = False
            ?? Stitch Lightmap Seams = True
            ?? Minimum Chart Size = 4
            ?? Auto UV Max Distance = 0,50
            ?? Auto UV Max Angle = 89,00
            ?? Lightmap Parameters: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Mask Interaction = None
         • Component: `BoxCollider`
            ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Layer Override Priority = 0
            ?? Is Trigger = False
            ?? Provides Contacts = False
      ??? **Point Light 1** [Active] (Layer: Default)
         • Component: `Light`
            ?? Type = Point
            ?? Intensity = 10,00
            ?? Range = 20,65
            ?? Spot Angle = 30,00
            ?? Inner Spot Angle = 21,80
            ?? Cookie: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Draw Halo = False
            ?? Flare: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Render Mode = Auto
            ?? Rendering Layer Mask = 1
            ?? Lightmapping = Realtime
            ?? Light Shadow Caster Mode = 0
            ?? Bounce Intensity = 1,00
            ?? Color Temperature = 6570,00
            ?? Use Color Temperature = False
            ?? Use Bounding Sphere Override = False
            ?? Use View Frustum For Shadow Caster Cull = True
            ?? Force Visible = False
            ?? Shadow Radius = 0,00
            ?? Shadow Angle = 0,00
            ?? Light Unit = 1
            ?? Lux At Distance = 1,00
            ?? Enable Spot Reflector = True
         • Component: `UniversalAdditionalLightData`
            ?? Use Pipeline Settings = True
            ?? Additional Lights Shadow Resolution Tier = 2
            ?? Custom Shadow Layers = False
            ?? Soft Shadow Quality = Use settings from Render Pipeline Asset
            ?? Version = Rendering Layers Mask
            ?? Light Layer Mask = Light Layer Default
            ?? Shadow Layer Mask = Light Layer Default
            ?? Rendering Layers = 1
            ?? Shadow Rendering Layers = 1
      ??? **Point Light 2** [Active] (Layer: Default)
         • Component: `Light`
            ?? Type = Point
            ?? Intensity = 5,00
            ?? Range = 20,65
            ?? Spot Angle = 30,00
            ?? Inner Spot Angle = 21,80
            ?? Cookie: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Draw Halo = False
            ?? Flare: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Render Mode = Auto
            ?? Rendering Layer Mask = 1
            ?? Lightmapping = Realtime
            ?? Light Shadow Caster Mode = 0
            ?? Bounce Intensity = 1,00
            ?? Color Temperature = 6570,00
            ?? Use Color Temperature = False
            ?? Use Bounding Sphere Override = False
            ?? Use View Frustum For Shadow Caster Cull = True
            ?? Force Visible = False
            ?? Shadow Radius = 0,00
            ?? Shadow Angle = 0,00
            ?? Light Unit = 1
            ?? Lux At Distance = 1,00
            ?? Enable Spot Reflector = True
         • Component: `UniversalAdditionalLightData`
            ?? Use Pipeline Settings = True
            ?? Additional Lights Shadow Resolution Tier = 2
            ?? Custom Shadow Layers = False
            ?? Soft Shadow Quality = Use settings from Render Pipeline Asset
            ?? Version = Rendering Layers Mask
            ?? Light Layer Mask = Light Layer Default
            ?? Shadow Layer Mask = Light Layer Default
            ?? Rendering Layers = 1
            ?? Shadow Rendering Layers = 1
   ??? **table** [Active] (Layer: Default)
      ??? **table leg 1** [Active] (Layer: Default)
         • Component: `MeshFilter`
            ?? Mesh: `Cube` (Mesh)
         • Component: `MeshRenderer`
            ?? Cast Shadows = On
            ?? Receive Shadows = True
            ?? Dynamic Occludee = True
            ?? Static Shadow Caster = False
            ?? Motion Vectors = Per Object Motion
            ?? Light Probe Usage = 1
            ?? Reflection Probe Usage = 1
            ?? Ray Tracing Mode = 2
            ?? Ray Trace Procedural = False
            ?? Ray Tracing Accel Struct Build Flags Override = False
            ?? Ray Tracing Accel Struct Build Flags = 1
            ?? Small Mesh Culling = True
            ?? Force Mesh Lod = -1
            ?? Mesh Lod Selection Bias = 0,00
            ?? Rendering Layer Mask = 1
            ?? Renderer Priority = 0
            ?? Probe Anchor: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Light Probe Volume Override: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Scale In Lightmap = 1,00
            ?? Receive GI = 1
            ?? Preserve U Vs = False
            ?? Ignore Normals For Chart Detection = False
            ?? Important GI = False
            ?? Stitch Lightmap Seams = True
            ?? Minimum Chart Size = 4
            ?? Auto UV Max Distance = 0,50
            ?? Auto UV Max Angle = 89,00
            ?? Lightmap Parameters: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Mask Interaction = None
         • Component: `BoxCollider`
            ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Layer Override Priority = 0
            ?? Is Trigger = False
            ?? Provides Contacts = False
      ??? **table leg 1** [Active] (Layer: Default)
         • Component: `MeshFilter`
            ?? Mesh: `Cube` (Mesh)
         • Component: `MeshRenderer`
            ?? Cast Shadows = On
            ?? Receive Shadows = True
            ?? Dynamic Occludee = True
            ?? Static Shadow Caster = False
            ?? Motion Vectors = Per Object Motion
            ?? Light Probe Usage = 1
            ?? Reflection Probe Usage = 1
            ?? Ray Tracing Mode = 2
            ?? Ray Trace Procedural = False
            ?? Ray Tracing Accel Struct Build Flags Override = False
            ?? Ray Tracing Accel Struct Build Flags = 1
            ?? Small Mesh Culling = True
            ?? Force Mesh Lod = -1
            ?? Mesh Lod Selection Bias = 0,00
            ?? Rendering Layer Mask = 1
            ?? Renderer Priority = 0
            ?? Probe Anchor: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Light Probe Volume Override: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Scale In Lightmap = 1,00
            ?? Receive GI = 1
            ?? Preserve U Vs = False
            ?? Ignore Normals For Chart Detection = False
            ?? Important GI = False
            ?? Stitch Lightmap Seams = True
            ?? Minimum Chart Size = 4
            ?? Auto UV Max Distance = 0,50
            ?? Auto UV Max Angle = 89,00
            ?? Lightmap Parameters: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Mask Interaction = None
         • Component: `BoxCollider`
            ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Layer Override Priority = 0
            ?? Is Trigger = False
            ?? Provides Contacts = False
      ??? **table leg 1** [Active] (Layer: Default)
         • Component: `MeshFilter`
            ?? Mesh: `Cube` (Mesh)
         • Component: `MeshRenderer`
            ?? Cast Shadows = On
            ?? Receive Shadows = True
            ?? Dynamic Occludee = True
            ?? Static Shadow Caster = False
            ?? Motion Vectors = Per Object Motion
            ?? Light Probe Usage = 1
            ?? Reflection Probe Usage = 1
            ?? Ray Tracing Mode = 2
            ?? Ray Trace Procedural = False
            ?? Ray Tracing Accel Struct Build Flags Override = False
            ?? Ray Tracing Accel Struct Build Flags = 1
            ?? Small Mesh Culling = True
            ?? Force Mesh Lod = -1
            ?? Mesh Lod Selection Bias = 0,00
            ?? Rendering Layer Mask = 1
            ?? Renderer Priority = 0
            ?? Probe Anchor: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Light Probe Volume Override: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Scale In Lightmap = 1,00
            ?? Receive GI = 1
            ?? Preserve U Vs = False
            ?? Ignore Normals For Chart Detection = False
            ?? Important GI = False
            ?? Stitch Lightmap Seams = True
            ?? Minimum Chart Size = 4
            ?? Auto UV Max Distance = 0,50
            ?? Auto UV Max Angle = 89,00
            ?? Lightmap Parameters: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Mask Interaction = None
         • Component: `BoxCollider`
            ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Layer Override Priority = 0
            ?? Is Trigger = False
            ?? Provides Contacts = False
      ??? **table leg 1** [Active] (Layer: Default)
         • Component: `MeshFilter`
            ?? Mesh: `Cube` (Mesh)
         • Component: `MeshRenderer`
            ?? Cast Shadows = On
            ?? Receive Shadows = True
            ?? Dynamic Occludee = True
            ?? Static Shadow Caster = False
            ?? Motion Vectors = Per Object Motion
            ?? Light Probe Usage = 1
            ?? Reflection Probe Usage = 1
            ?? Ray Tracing Mode = 2
            ?? Ray Trace Procedural = False
            ?? Ray Tracing Accel Struct Build Flags Override = False
            ?? Ray Tracing Accel Struct Build Flags = 1
            ?? Small Mesh Culling = True
            ?? Force Mesh Lod = -1
            ?? Mesh Lod Selection Bias = 0,00
            ?? Rendering Layer Mask = 1
            ?? Renderer Priority = 0
            ?? Probe Anchor: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Light Probe Volume Override: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Scale In Lightmap = 1,00
            ?? Receive GI = 1
            ?? Preserve U Vs = False
            ?? Ignore Normals For Chart Detection = False
            ?? Important GI = False
            ?? Stitch Lightmap Seams = True
            ?? Minimum Chart Size = 4
            ?? Auto UV Max Distance = 0,50
            ?? Auto UV Max Angle = 89,00
            ?? Lightmap Parameters: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Mask Interaction = None
         • Component: `BoxCollider`
            ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Layer Override Priority = 0
            ?? Is Trigger = False
            ?? Provides Contacts = False
      ??? **tabletop ** [Active] (Layer: Default)
         • Component: `MeshFilter`
            ?? Mesh: `Cube` (Mesh)
         • Component: `MeshRenderer`
            ?? Cast Shadows = On
            ?? Receive Shadows = True
            ?? Dynamic Occludee = True
            ?? Static Shadow Caster = False
            ?? Motion Vectors = Per Object Motion
            ?? Light Probe Usage = 1
            ?? Reflection Probe Usage = 1
            ?? Ray Tracing Mode = 2
            ?? Ray Trace Procedural = False
            ?? Ray Tracing Accel Struct Build Flags Override = False
            ?? Ray Tracing Accel Struct Build Flags = 1
            ?? Small Mesh Culling = True
            ?? Force Mesh Lod = -1
            ?? Mesh Lod Selection Bias = 0,00
            ?? Rendering Layer Mask = 1
            ?? Renderer Priority = 0
            ?? Probe Anchor: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Light Probe Volume Override: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Scale In Lightmap = 1,00
            ?? Receive GI = 1
            ?? Preserve U Vs = False
            ?? Ignore Normals For Chart Detection = False
            ?? Important GI = False
            ?? Stitch Lightmap Seams = True
            ?? Minimum Chart Size = 4
            ?? Auto UV Max Distance = 0,50
            ?? Auto UV Max Angle = 89,00
            ?? Lightmap Parameters: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Mask Interaction = None
         • Component: `BoxCollider`
            ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Layer Override Priority = 0
            ?? Is Trigger = False
            ?? Provides Contacts = False
   ??? **TV Right** [Active] (Layer: Default)
   ??? **telek1** [Active] (Layer: Default)
      ??? **UI_MONITOR_LEFT** [Active] (Layer: Default)
         ??? **Canvas_LeftMonitor** [Active] (Layer: UI)
            • Component: `Canvas`
               ?? Render Mode = World Space
               ?? Camera: `Main Camera` (Camera)
               ?? Plane Distance = 100,00
               ?? Pixel Perfect = False
               ?? Receives Events = True
               ?? Override Sorting = False
               ?? Override Pixel Perfect = False
               ?? Sorting Bucket Normalized Size = 0,00
               ?? Vertex Color Always Gamma Space = False
               ?? Additional Shader Channels Flag = -1
               ?? Update Rect Transform For Standalone = Enabled
            • Component: `CanvasScaler`
               ?? Ui Scale Mode = Constant Pixel Size
               ?? Reference Pixels Per Unit = 100,00
               ?? Scale Factor = 1,00
               ?? Screen Match Mode = Match Width Or Height
               ?? Match Width Or Height = 0,00
               ?? Physical Unit = Points
               ?? Fallback Screen DPI = 96,00
               ?? Default Sprite DPI = 96,00
               ?? Dynamic Pixels Per Unit = 1,00
               ?? Preset Info Is World = True
            • Component: `GraphicRaycaster`
               ?? Ignore Reversed Graphics = True
               ?? Blocking Objects = None
            • Component: `LeftMonitorShopController`
               ?? Right Map Monitor: `Canvas_RightMonitor` (RightMonitorMapController)
               ?? Currency = 150
               ?? Sanity Slider: `Slider` (Slider)
               ?? Currency Text: `Text_Currency` (TextMeshProUGUI)
               ?? Dispatch Button: `Btn_DispatchSquad` (Button)
               ?? Dispatch Btn Text: `Text (TMP)` (TextMeshProUGUI)
               ?? Report Log Text: `Text_ReportLog` (TextMeshProUGUI)
            ??? **Panel_Currency** [Active] (Layer: UI)
               • Component: `Image`
                  ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
                  ?? Raycast Target = True
                  ?? Maskable = True
                  ?? Sprite: `Background` (Sprite)
                  ?? Type = Sliced
                  ?? Preserve Aspect = False
                  ?? Fill Center = True
                  ?? Fill Method = Radial 360
                  ?? Fill Amount = 1,00
                  ?? Fill Clockwise = True
                  ?? Fill Origin = 0
                  ?? Use Sprite Mesh = False
                  ?? Pixels Per Unit Multiplier = 1,00
               ??? **Image** [Active] (Layer: UI)
                  • Component: `Image`
                     ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
                     ?? Raycast Target = True
                     ?? Maskable = True
                     ?? Sprite: **[NULL / НЕ НАЗНАЧЕНО]**
                     ?? Type = Simple
                     ?? Preserve Aspect = False
                     ?? Fill Center = True
                     ?? Fill Method = Radial 360
                     ?? Fill Amount = 1,00
                     ?? Fill Clockwise = True
                     ?? Fill Origin = 0
                     ?? Use Sprite Mesh = False
                     ?? Pixels Per Unit Multiplier = 1,00
               ??? **Text_Currency** [Active] (Layer: UI)
                  • Component: `TextMeshProUGUI`
                     ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
                     ?? Raycast Target = True
                     ?? Maskable = True
                     ?? Text = "БЮДЖЕТ: 150"
                     ?? Is Right To Left = False
                     ?? Font Asset: `LiberationSans SDF` (TMP_FontAsset)
                     ?? Shared Material: `LiberationSans SDF Material` (Material)
                     ?? Font Material: **[NULL / НЕ НАЗНАЧЕНО]**
                     ?? Enable Vertex Gradient = False
                     ?? Color Mode = Four Corners Gradient
                     ?? Font Color Gradient Preset: **[NULL / НЕ НАЗНАЧЕНО]**
                     ?? Sprite Asset: **[NULL / НЕ НАЗНАЧЕНО]**
                     ?? Tint All Sprites = False
                     ?? Style Sheet: **[NULL / НЕ НАЗНАЧЕНО]**
                     ?? Text Style Hash Code = -1183493901
                     ?? Override Html Colors = False
                     ?? Font Size = 60,00
                     ?? Font Size Base = 60,00
                     ?? Font Weight = Regular
                     ?? Enable Auto Sizing = False
                     ?? Font Size Min = 18,00
                     ?? Font Size Max = 72,00
                     ?? Font Style = Normal
                     ?? Horizontal Alignment = Center
                     ?? Vertical Alignment = Middle
                     ?? Text Alignment = Converted
                     ?? Character Spacing = 0,00
                     ?? Character Horizontal Scale = 1,00
                     ?? Word Spacing = 0,00
                     ?? Line Spacing = 0,00
                     ?? Line Spacing Max = 0,00
                     ?? Paragraph Spacing = 0,00
                     ?? Char Width Max Adj = 0,00
                     ?? Text Wrapping Mode = Normal
                     ?? Word Wrapping Ratios = 0,40
                     ?? Overflow Mode = Overflow
                     ?? Linked Text Component: **[NULL / НЕ НАЗНАЧЕНО]**
                     ?? Parent Linked Component: **[NULL / НЕ НАЗНАЧЕНО]**
                     ?? Enable Kerning = False
                     ?? Enable Extra Padding = False
                     ?? Check Padding Required = False
                     ?? Is Rich Text = True
                     ?? Emoji Fallback Support = True
                     ?? Parse Ctrl Characters = True
                     ?? Is Orthographic = True
                     ?? Is Culling Enabled = False
                     ?? Horizontal Mapping = Character
                     ?? Vertical Mapping = Character
                     ?? Uv Line Offset = 0,00
                     ?? Geometry Sorting Order = Normal
                     ?? Is Text Object Scale Static = False
                     ?? Vertex Buffer Auto Size Reduction = False
                     ?? Use Max Visible Descender = True
                     ?? Page To Display = 1
                     ?? Is Using Legacy Animation Component = False
                     ?? Is Volumetric Text = False
                     ?? Has Font Asset Changed = False
                     ?? Base Material: **[NULL / НЕ НАЗНАЧЕНО]**
                  ??? **TMP SubMeshUI [LiberationSans SDF Material + LiberationSans SDF Atlas]** [Active] (Layer: UI)
                     • Component: `LayoutElement`
                        ?? Ignore Layout = True
                        ?? Min Width = -1,00
                        ?? Min Height = -1,00
                        ?? Preferred Width = -1,00
                        ?? Preferred Height = -1,00
                        ?? Flexible Width = -1,00
                        ?? Flexible Height = -1,00
                        ?? Layout Priority = 1
                     • Component: `TMP_SubMeshUI`
                        ?? Material: `LiberationSans SDF Material + LiberationSans SDF Atlas` (Material)
                        ?? Raycast Target = True
                        ?? Maskable = True
                        ?? Font Asset: `LiberationSans SDF - Fallback` (TMP_FontAsset)
                        ?? Sprite Asset: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Shared Material: `LiberationSans SDF Material + LiberationSans SDF Atlas` (Material)
                        ?? Is Default Material = False
                        ?? Padding = 1,25
                        ?? Text Component: `Text_Currency` (TextMeshProUGUI)
                        ?? Material Reference Index = 1
            ??? **Slider** [Active] (Layer: UI)
               • Component: `Slider`
                  ?? Transition = Color Tint
                  ?? Interactable = True
                  ?? Target Graphic: **[NULL / НЕ НАЗНАЧЕНО]**
                  ?? Fill Rect: `Fill` (RectTransform)
                  ?? Handle Rect: **[NULL / НЕ НАЗНАЧЕНО]**
                  ?? Direction = Left To Right
                  ?? Min Value = 0,00
                  ?? Max Value = 100,00
                  ?? Whole Numbers = False
                  ?? Value = 80,00
               ??? **Background** [Active] (Layer: UI)
                  • Component: `Image`
                     ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
                     ?? Raycast Target = True
                     ?? Maskable = True
                     ?? Sprite: `Background` (Sprite)
                     ?? Type = Sliced
                     ?? Preserve Aspect = False
                     ?? Fill Center = True
                     ?? Fill Method = Radial 360
                     ?? Fill Amount = 1,00
                     ?? Fill Clockwise = True
                     ?? Fill Origin = 0
                     ?? Use Sprite Mesh = False
                     ?? Pixels Per Unit Multiplier = 1,00
               ??? **Fill Area** [Active] (Layer: UI)
                  ??? **Fill** [Active] (Layer: UI)
                     • Component: `Image`
                        ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Raycast Target = True
                        ?? Maskable = True
                        ?? Sprite: `UISprite` (Sprite)
                        ?? Type = Sliced
                        ?? Preserve Aspect = False
                        ?? Fill Center = True
                        ?? Fill Method = Radial 360
                        ?? Fill Amount = 1,00
                        ?? Fill Clockwise = True
                        ?? Fill Origin = 0
                        ?? Use Sprite Mesh = False
                        ?? Pixels Per Unit Multiplier = 1,00
               ??? **Text (TMP)** [Active] (Layer: UI)
                  • Component: `TextMeshProUGUI`
                     ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
                     ?? Raycast Target = True
                     ?? Maskable = True
                     ?? Text = "ШКАЛА РАССУДКА"
                     ?? Is Right To Left = False
                     ?? Font Asset: `LiberationSans SDF` (TMP_FontAsset)
                     ?? Shared Material: `LiberationSans SDF Material` (Material)
                     ?? Font Material: **[NULL / НЕ НАЗНАЧЕНО]**
                     ?? Enable Vertex Gradient = False
                     ?? Color Mode = Four Corners Gradient
                     ?? Font Color Gradient Preset: **[NULL / НЕ НАЗНАЧЕНО]**
                     ?? Sprite Asset: **[NULL / НЕ НАЗНАЧЕНО]**
                     ?? Tint All Sprites = False
                     ?? Style Sheet: **[NULL / НЕ НАЗНАЧЕНО]**
                     ?? Text Style Hash Code = -1183493901
                     ?? Override Html Colors = False
                     ?? Font Size = 50,00
                     ?? Font Size Base = 50,00
                     ?? Font Weight = Regular
                     ?? Enable Auto Sizing = False
                     ?? Font Size Min = 18,00
                     ?? Font Size Max = 72,00
                     ?? Font Style = Normal
                     ?? Horizontal Alignment = Center
                     ?? Vertical Alignment = Middle
                     ?? Text Alignment = Converted
                     ?? Character Spacing = 0,00
                     ?? Character Horizontal Scale = 1,00
                     ?? Word Spacing = 0,00
                     ?? Line Spacing = 0,00
                     ?? Line Spacing Max = 0,00
                     ?? Paragraph Spacing = 0,00
                     ?? Char Width Max Adj = 0,00
                     ?? Text Wrapping Mode = Normal
                     ?? Word Wrapping Ratios = 0,40
                     ?? Overflow Mode = Overflow
                     ?? Linked Text Component: **[NULL / НЕ НАЗНАЧЕНО]**
                     ?? Parent Linked Component: **[NULL / НЕ НАЗНАЧЕНО]**
                     ?? Enable Kerning = False
                     ?? Enable Extra Padding = False
                     ?? Check Padding Required = False
                     ?? Is Rich Text = True
                     ?? Emoji Fallback Support = True
                     ?? Parse Ctrl Characters = True
                     ?? Is Orthographic = True
                     ?? Is Culling Enabled = False
                     ?? Horizontal Mapping = Character
                     ?? Vertical Mapping = Character
                     ?? Uv Line Offset = 0,00
                     ?? Geometry Sorting Order = Normal
                     ?? Is Text Object Scale Static = False
                     ?? Vertex Buffer Auto Size Reduction = False
                     ?? Use Max Visible Descender = True
                     ?? Page To Display = 1
                     ?? Is Using Legacy Animation Component = False
                     ?? Is Volumetric Text = False
                     ?? Has Font Asset Changed = False
                     ?? Base Material: **[NULL / НЕ НАЗНАЧЕНО]**
                  ??? **TMP SubMeshUI [LiberationSans SDF Material + LiberationSans SDF Atlas]** [Active] (Layer: UI)
                     • Component: `LayoutElement`
                        ?? Ignore Layout = True
                        ?? Min Width = -1,00
                        ?? Min Height = -1,00
                        ?? Preferred Width = -1,00
                        ?? Preferred Height = -1,00
                        ?? Flexible Width = -1,00
                        ?? Flexible Height = -1,00
                        ?? Layout Priority = 1
                     • Component: `TMP_SubMeshUI`
                        ?? Material: `LiberationSans SDF Material + LiberationSans SDF Atlas` (Material)
                        ?? Raycast Target = True
                        ?? Maskable = True
                        ?? Font Asset: `LiberationSans SDF - Fallback` (TMP_FontAsset)
                        ?? Sprite Asset: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Shared Material: `LiberationSans SDF Material + LiberationSans SDF Atlas` (Material)
                        ?? Is Default Material = False
                        ?? Padding = 1,25
                        ?? Text Component: `Text (TMP)` (TextMeshProUGUI)
                        ?? Material Reference Index = 1
            ??? **Btn_GBR** [Active] (Layer: UI)
               • Component: `Image`
                  ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
                  ?? Raycast Target = True
                  ?? Maskable = True
                  ?? Sprite: `UISprite` (Sprite)
                  ?? Type = Sliced
                  ?? Preserve Aspect = False
                  ?? Fill Center = True
                  ?? Fill Method = Radial 360
                  ?? Fill Amount = 1,00
                  ?? Fill Clockwise = True
                  ?? Fill Origin = 0
                  ?? Use Sprite Mesh = False
                  ?? Pixels Per Unit Multiplier = 1,00
               • Component: `Button`
                  ?? Transition = Color Tint
                  ?? Interactable = True
                  ?? Target Graphic: `Btn_GBR` (Image)
               ??? **Text (TMP)** [Active] (Layer: UI)
                  • Component: `TextMeshProUGUI`
                     ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
                     ?? Raycast Target = True
                     ?? Maskable = True
                     ?? Text = "ГБР - 50"
                     ?? Is Right To Left = False
                     ?? Font Asset: `LiberationSans SDF` (TMP_FontAsset)
                     ?? Shared Material: `LiberationSans SDF Material` (Material)
                     ?? Font Material: **[NULL / НЕ НАЗНАЧЕНО]**
                     ?? Enable Vertex Gradient = False
                     ?? Color Mode = Four Corners Gradient
                     ?? Font Color Gradient Preset: **[NULL / НЕ НАЗНАЧЕНО]**
                     ?? Sprite Asset: **[NULL / НЕ НАЗНАЧЕНО]**
                     ?? Tint All Sprites = False
                     ?? Style Sheet: **[NULL / НЕ НАЗНАЧЕНО]**
                     ?? Text Style Hash Code = -1183493901
                     ?? Override Html Colors = False
                     ?? Font Size = 70,00
                     ?? Font Size Base = 70,00
                     ?? Font Weight = Regular
                     ?? Enable Auto Sizing = False
                     ?? Font Size Min = 18,00
                     ?? Font Size Max = 72,00
                     ?? Font Style = Normal
                     ?? Horizontal Alignment = Center
                     ?? Vertical Alignment = Middle
                     ?? Text Alignment = Converted
                     ?? Character Spacing = 0,00
                     ?? Character Horizontal Scale = 1,00
                     ?? Word Spacing = 0,00
                     ?? Line Spacing = 0,00
                     ?? Line Spacing Max = 0,00
                     ?? Paragraph Spacing = 0,00
                     ?? Char Width Max Adj = 0,00
                     ?? Text Wrapping Mode = Normal
                     ?? Word Wrapping Ratios = 0,40
                     ?? Overflow Mode = Overflow
                     ?? Linked Text Component: **[NULL / НЕ НАЗНАЧЕНО]**
                     ?? Parent Linked Component: **[NULL / НЕ НАЗНАЧЕНО]**
                     ?? Enable Kerning = False
                     ?? Enable Extra Padding = False
                     ?? Check Padding Required = False
                     ?? Is Rich Text = True
                     ?? Emoji Fallback Support = True
                     ?? Parse Ctrl Characters = True
                     ?? Is Orthographic = True
                     ?? Is Culling Enabled = False
                     ?? Horizontal Mapping = Character
                     ?? Vertical Mapping = Character
                     ?? Uv Line Offset = 0,00
                     ?? Geometry Sorting Order = Normal
                     ?? Is Text Object Scale Static = False
                     ?? Vertex Buffer Auto Size Reduction = False
                     ?? Use Max Visible Descender = True
                     ?? Page To Display = 1
                     ?? Is Using Legacy Animation Component = False
                     ?? Is Volumetric Text = False
                     ?? Has Font Asset Changed = False
                     ?? Base Material: **[NULL / НЕ НАЗНАЧЕНО]**
                  ??? **TMP SubMeshUI [LiberationSans SDF Material + LiberationSans SDF Atlas]** [Active] (Layer: UI)
                     • Component: `LayoutElement`
                        ?? Ignore Layout = True
                        ?? Min Width = -1,00
                        ?? Min Height = -1,00
                        ?? Preferred Width = -1,00
                        ?? Preferred Height = -1,00
                        ?? Flexible Width = -1,00
                        ?? Flexible Height = -1,00
                        ?? Layout Priority = 1
                     • Component: `TMP_SubMeshUI`
                        ?? Material: `LiberationSans SDF Material + LiberationSans SDF Atlas` (Material)
                        ?? Raycast Target = True
                        ?? Maskable = True
                        ?? Font Asset: `LiberationSans SDF - Fallback` (TMP_FontAsset)
                        ?? Sprite Asset: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Shared Material: `LiberationSans SDF Material + LiberationSans SDF Atlas` (Material)
                        ?? Is Default Material = False
                        ?? Padding = 1,25
                        ?? Text Component: `Text (TMP)` (TextMeshProUGUI)
                        ?? Material Reference Index = 1
            ??? **Btn_Spetsnaz** [Active] (Layer: UI)
               • Component: `Image`
                  ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
                  ?? Raycast Target = True
                  ?? Maskable = True
                  ?? Sprite: `UISprite` (Sprite)
                  ?? Type = Sliced
                  ?? Preserve Aspect = False
                  ?? Fill Center = True
                  ?? Fill Method = Radial 360
                  ?? Fill Amount = 1,00
                  ?? Fill Clockwise = True
                  ?? Fill Origin = 0
                  ?? Use Sprite Mesh = False
                  ?? Pixels Per Unit Multiplier = 1,00
               • Component: `Button`
                  ?? Transition = Color Tint
                  ?? Interactable = True
                  ?? Target Graphic: `Btn_Spetsnaz` (Image)
               ??? **Text (TMP)** [Active] (Layer: UI)
                  • Component: `TextMeshProUGUI`
                     ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
                     ?? Raycast Target = True
                     ?? Maskable = True
                     ?? Text = "Спецназ - 100"
                     ?? Is Right To Left = False
                     ?? Font Asset: `LiberationSans SDF` (TMP_FontAsset)
                     ?? Shared Material: `LiberationSans SDF Material` (Material)
                     ?? Font Material: **[NULL / НЕ НАЗНАЧЕНО]**
                     ?? Enable Vertex Gradient = False
                     ?? Color Mode = Four Corners Gradient
                     ?? Font Color Gradient Preset: **[NULL / НЕ НАЗНАЧЕНО]**
                     ?? Sprite Asset: **[NULL / НЕ НАЗНАЧЕНО]**
                     ?? Tint All Sprites = False
                     ?? Style Sheet: **[NULL / НЕ НАЗНАЧЕНО]**
                     ?? Text Style Hash Code = -1183493901
                     ?? Override Html Colors = False
                     ?? Font Size = 70,00
                     ?? Font Size Base = 70,00
                     ?? Font Weight = Regular
                     ?? Enable Auto Sizing = False
                     ?? Font Size Min = 18,00
                     ?? Font Size Max = 72,00
                     ?? Font Style = Normal
                     ?? Horizontal Alignment = Center
                     ?? Vertical Alignment = Middle
                     ?? Text Alignment = Converted
                     ?? Character Spacing = 0,00
                     ?? Character Horizontal Scale = 1,00
                     ?? Word Spacing = 0,00
                     ?? Line Spacing = 0,00
                     ?? Line Spacing Max = 0,00
                     ?? Paragraph Spacing = 0,00
                     ?? Char Width Max Adj = 0,00
                     ?? Text Wrapping Mode = Normal
                     ?? Word Wrapping Ratios = 0,40
                     ?? Overflow Mode = Overflow
                     ?? Linked Text Component: **[NULL / НЕ НАЗНАЧЕНО]**
                     ?? Parent Linked Component: **[NULL / НЕ НАЗНАЧЕНО]**
                     ?? Enable Kerning = False
                     ?? Enable Extra Padding = False
                     ?? Check Padding Required = False
                     ?? Is Rich Text = True
                     ?? Emoji Fallback Support = True
                     ?? Parse Ctrl Characters = True
                     ?? Is Orthographic = True
                     ?? Is Culling Enabled = False
                     ?? Horizontal Mapping = Character
                     ?? Vertical Mapping = Character
                     ?? Uv Line Offset = 0,00
                     ?? Geometry Sorting Order = Normal
                     ?? Is Text Object Scale Static = False
                     ?? Vertex Buffer Auto Size Reduction = False
                     ?? Use Max Visible Descender = True
                     ?? Page To Display = 1
                     ?? Is Using Legacy Animation Component = False
                     ?? Is Volumetric Text = False
                     ?? Has Font Asset Changed = False
                     ?? Base Material: **[NULL / НЕ НАЗНАЧЕНО]**
                  ??? **TMP SubMeshUI [LiberationSans SDF Material + LiberationSans SDF Atlas]** [Active] (Layer: UI)
                     • Component: `LayoutElement`
                        ?? Ignore Layout = True
                        ?? Min Width = -1,00
                        ?? Min Height = -1,00
                        ?? Preferred Width = -1,00
                        ?? Preferred Height = -1,00
                        ?? Flexible Width = -1,00
                        ?? Flexible Height = -1,00
                        ?? Layout Priority = 1
                     • Component: `TMP_SubMeshUI`
                        ?? Material: `LiberationSans SDF Material + LiberationSans SDF Atlas` (Material)
                        ?? Raycast Target = True
                        ?? Maskable = True
                        ?? Font Asset: `LiberationSans SDF - Fallback` (TMP_FontAsset)
                        ?? Sprite Asset: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Shared Material: `LiberationSans SDF Material + LiberationSans SDF Atlas` (Material)
                        ?? Is Default Material = False
                        ?? Padding = 1,25
                        ?? Text Component: `Text (TMP)` (TextMeshProUGUI)
                        ?? Material Reference Index = 1
            ??? **Btn_Biohazard** [Active] (Layer: UI)
               • Component: `Image`
                  ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
                  ?? Raycast Target = True
                  ?? Maskable = True
                  ?? Sprite: `UISprite` (Sprite)
                  ?? Type = Sliced
                  ?? Preserve Aspect = False
                  ?? Fill Center = True
                  ?? Fill Method = Radial 360
                  ?? Fill Amount = 1,00
                  ?? Fill Clockwise = True
                  ?? Fill Origin = 0
                  ?? Use Sprite Mesh = False
                  ?? Pixels Per Unit Multiplier = 1,00
               • Component: `Button`
                  ?? Transition = Color Tint
                  ?? Interactable = True
                  ?? Target Graphic: `Btn_Biohazard` (Image)
               ??? **Text (TMP)** [Active] (Layer: UI)
                  • Component: `TextMeshProUGUI`
                     ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
                     ?? Raycast Target = True
                     ?? Maskable = True
                     ?? Text = "Биозащита -150"
                     ?? Is Right To Left = False
                     ?? Font Asset: `LiberationSans SDF` (TMP_FontAsset)
                     ?? Shared Material: `LiberationSans SDF Material` (Material)
                     ?? Font Material: **[NULL / НЕ НАЗНАЧЕНО]**
                     ?? Enable Vertex Gradient = False
                     ?? Color Mode = Four Corners Gradient
                     ?? Font Color Gradient Preset: **[NULL / НЕ НАЗНАЧЕНО]**
                     ?? Sprite Asset: **[NULL / НЕ НАЗНАЧЕНО]**
                     ?? Tint All Sprites = False
                     ?? Style Sheet: **[NULL / НЕ НАЗНАЧЕНО]**
                     ?? Text Style Hash Code = -1183493901
                     ?? Override Html Colors = False
                     ?? Font Size = 70,00
                     ?? Font Size Base = 70,00
                     ?? Font Weight = Regular
                     ?? Enable Auto Sizing = False
                     ?? Font Size Min = 18,00
                     ?? Font Size Max = 72,00
                     ?? Font Style = Normal
                     ?? Horizontal Alignment = Center
                     ?? Vertical Alignment = Middle
                     ?? Text Alignment = Converted
                     ?? Character Spacing = 0,00
                     ?? Character Horizontal Scale = 1,00
                     ?? Word Spacing = 0,00
                     ?? Line Spacing = 0,00
                     ?? Line Spacing Max = 0,00
                     ?? Paragraph Spacing = 0,00
                     ?? Char Width Max Adj = 0,00
                     ?? Text Wrapping Mode = Normal
                     ?? Word Wrapping Ratios = 0,40
                     ?? Overflow Mode = Overflow
                     ?? Linked Text Component: **[NULL / НЕ НАЗНАЧЕНО]**
                     ?? Parent Linked Component: **[NULL / НЕ НАЗНАЧЕНО]**
                     ?? Enable Kerning = False
                     ?? Enable Extra Padding = False
                     ?? Check Padding Required = False
                     ?? Is Rich Text = True
                     ?? Emoji Fallback Support = True
                     ?? Parse Ctrl Characters = True
                     ?? Is Orthographic = True
                     ?? Is Culling Enabled = False
                     ?? Horizontal Mapping = Character
                     ?? Vertical Mapping = Character
                     ?? Uv Line Offset = 0,00
                     ?? Geometry Sorting Order = Normal
                     ?? Is Text Object Scale Static = False
                     ?? Vertex Buffer Auto Size Reduction = False
                     ?? Use Max Visible Descender = True
                     ?? Page To Display = 1
                     ?? Is Using Legacy Animation Component = False
                     ?? Is Volumetric Text = False
                     ?? Has Font Asset Changed = False
                     ?? Base Material: **[NULL / НЕ НАЗНАЧЕНО]**
                  ??? **TMP SubMeshUI [LiberationSans SDF Material + LiberationSans SDF Atlas]** [Active] (Layer: UI)
                     • Component: `LayoutElement`
                        ?? Ignore Layout = True
                        ?? Min Width = -1,00
                        ?? Min Height = -1,00
                        ?? Preferred Width = -1,00
                        ?? Preferred Height = -1,00
                        ?? Flexible Width = -1,00
                        ?? Flexible Height = -1,00
                        ?? Layout Priority = 1
                     • Component: `TMP_SubMeshUI`
                        ?? Material: `LiberationSans SDF Material + LiberationSans SDF Atlas` (Material)
                        ?? Raycast Target = True
                        ?? Maskable = True
                        ?? Font Asset: `LiberationSans SDF - Fallback` (TMP_FontAsset)
                        ?? Sprite Asset: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Shared Material: `LiberationSans SDF Material + LiberationSans SDF Atlas` (Material)
                        ?? Is Default Material = False
                        ?? Padding = 1,25
                        ?? Text Component: `Text (TMP)` (TextMeshProUGUI)
                        ?? Material Reference Index = 1
            ??? **Image_ReportLog** [Active] (Layer: UI)
               • Component: `Image`
                  ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
                  ?? Raycast Target = True
                  ?? Maskable = True
                  ?? Sprite: **[NULL / НЕ НАЗНАЧЕНО]**
                  ?? Type = Simple
                  ?? Preserve Aspect = False
                  ?? Fill Center = True
                  ?? Fill Method = Radial 360
                  ?? Fill Amount = 1,00
                  ?? Fill Clockwise = True
                  ?? Fill Origin = 0
                  ?? Use Sprite Mesh = False
                  ?? Pixels Per Unit Multiplier = 1,00
               ??? **Text_ReportLog** [Active] (Layer: UI)
                  • Component: `TextMeshProUGUI`
                     ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
                     ?? Raycast Target = True
                     ?? Maskable = True
                     ?? Text = "Отчет"
                     ?? Is Right To Left = False
                     ?? Font Asset: `LiberationSans SDF` (TMP_FontAsset)
                     ?? Shared Material: `LiberationSans SDF Material` (Material)
                     ?? Font Material: **[NULL / НЕ НАЗНАЧЕНО]**
                     ?? Enable Vertex Gradient = False
                     ?? Color Mode = Four Corners Gradient
                     ?? Font Color Gradient Preset: **[NULL / НЕ НАЗНАЧЕНО]**
                     ?? Sprite Asset: **[NULL / НЕ НАЗНАЧЕНО]**
                     ?? Tint All Sprites = False
                     ?? Style Sheet: **[NULL / НЕ НАЗНАЧЕНО]**
                     ?? Text Style Hash Code = -1183493901
                     ?? Override Html Colors = False
                     ?? Font Size = 72,00
                     ?? Font Size Base = 70,00
                     ?? Font Weight = Regular
                     ?? Enable Auto Sizing = True
                     ?? Font Size Min = 18,00
                     ?? Font Size Max = 72,00
                     ?? Font Style = Normal
                     ?? Horizontal Alignment = Center
                     ?? Vertical Alignment = Middle
                     ?? Text Alignment = Converted
                     ?? Character Spacing = 0,00
                     ?? Character Horizontal Scale = 1,00
                     ?? Word Spacing = 0,00
                     ?? Line Spacing = 0,00
                     ?? Line Spacing Max = 0,00
                     ?? Paragraph Spacing = 0,00
                     ?? Char Width Max Adj = 0,00
                     ?? Text Wrapping Mode = Normal
                     ?? Word Wrapping Ratios = 0,40
                     ?? Overflow Mode = Overflow
                     ?? Linked Text Component: **[NULL / НЕ НАЗНАЧЕНО]**
                     ?? Parent Linked Component: **[NULL / НЕ НАЗНАЧЕНО]**
                     ?? Enable Kerning = False
                     ?? Enable Extra Padding = False
                     ?? Check Padding Required = False
                     ?? Is Rich Text = True
                     ?? Emoji Fallback Support = True
                     ?? Parse Ctrl Characters = True
                     ?? Is Orthographic = True
                     ?? Is Culling Enabled = False
                     ?? Horizontal Mapping = Character
                     ?? Vertical Mapping = Character
                     ?? Uv Line Offset = 0,00
                     ?? Geometry Sorting Order = Normal
                     ?? Is Text Object Scale Static = False
                     ?? Vertex Buffer Auto Size Reduction = False
                     ?? Use Max Visible Descender = True
                     ?? Page To Display = 1
                     ?? Is Using Legacy Animation Component = False
                     ?? Is Volumetric Text = False
                     ?? Has Font Asset Changed = False
                     ?? Base Material: **[NULL / НЕ НАЗНАЧЕНО]**
                  ??? **TMP SubMeshUI [LiberationSans SDF Material + LiberationSans SDF Atlas]** [Active] (Layer: UI)
                     • Component: `LayoutElement`
                        ?? Ignore Layout = True
                        ?? Min Width = -1,00
                        ?? Min Height = -1,00
                        ?? Preferred Width = -1,00
                        ?? Preferred Height = -1,00
                        ?? Flexible Width = -1,00
                        ?? Flexible Height = -1,00
                        ?? Layout Priority = 1
                     • Component: `TMP_SubMeshUI`
                        ?? Material: `LiberationSans SDF Material + LiberationSans SDF Atlas` (Material)
                        ?? Raycast Target = True
                        ?? Maskable = True
                        ?? Font Asset: `LiberationSans SDF - Fallback` (TMP_FontAsset)
                        ?? Sprite Asset: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Shared Material: `LiberationSans SDF Material + LiberationSans SDF Atlas` (Material)
                        ?? Is Default Material = False
                        ?? Padding = 1,25
                        ?? Text Component: `Text_ReportLog` (TextMeshProUGUI)
                        ?? Material Reference Index = 1
            ??? **Btn_DispatchSquad** [Active] (Layer: UI)
               • Component: `Image`
                  ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
                  ?? Raycast Target = True
                  ?? Maskable = True
                  ?? Sprite: `UISprite` (Sprite)
                  ?? Type = Sliced
                  ?? Preserve Aspect = False
                  ?? Fill Center = True
                  ?? Fill Method = Radial 360
                  ?? Fill Amount = 1,00
                  ?? Fill Clockwise = True
                  ?? Fill Origin = 0
                  ?? Use Sprite Mesh = False
                  ?? Pixels Per Unit Multiplier = 1,00
               • Component: `Button`
                  ?? Transition = Color Tint
                  ?? Interactable = True
                  ?? Target Graphic: `Btn_DispatchSquad` (Image)
               ??? **Text (TMP)** [Active] (Layer: UI)
                  • Component: `TextMeshProUGUI`
                     ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
                     ?? Raycast Target = True
                     ?? Maskable = True
                     ?? Text = "Отправить"
                     ?? Is Right To Left = False
                     ?? Font Asset: `LiberationSans SDF` (TMP_FontAsset)
                     ?? Shared Material: `LiberationSans SDF Material` (Material)
                     ?? Font Material: **[NULL / НЕ НАЗНАЧЕНО]**
                     ?? Enable Vertex Gradient = False
                     ?? Color Mode = Four Corners Gradient
                     ?? Font Color Gradient Preset: **[NULL / НЕ НАЗНАЧЕНО]**
                     ?? Sprite Asset: **[NULL / НЕ НАЗНАЧЕНО]**
                     ?? Tint All Sprites = False
                     ?? Style Sheet: **[NULL / НЕ НАЗНАЧЕНО]**
                     ?? Text Style Hash Code = -1183493901
                     ?? Override Html Colors = False
                     ?? Font Size = 72,00
                     ?? Font Size Base = 70,00
                     ?? Font Weight = Regular
                     ?? Enable Auto Sizing = True
                     ?? Font Size Min = 18,00
                     ?? Font Size Max = 72,00
                     ?? Font Style = Normal
                     ?? Horizontal Alignment = Center
                     ?? Vertical Alignment = Middle
                     ?? Text Alignment = Converted
                     ?? Character Spacing = 0,00
                     ?? Character Horizontal Scale = 1,00
                     ?? Word Spacing = 0,00
                     ?? Line Spacing = 0,00
                     ?? Line Spacing Max = 0,00
                     ?? Paragraph Spacing = 0,00
                     ?? Char Width Max Adj = 0,00
                     ?? Text Wrapping Mode = Normal
                     ?? Word Wrapping Ratios = 0,40
                     ?? Overflow Mode = Overflow
                     ?? Linked Text Component: **[NULL / НЕ НАЗНАЧЕНО]**
                     ?? Parent Linked Component: **[NULL / НЕ НАЗНАЧЕНО]**
                     ?? Enable Kerning = False
                     ?? Enable Extra Padding = False
                     ?? Check Padding Required = False
                     ?? Is Rich Text = True
                     ?? Emoji Fallback Support = True
                     ?? Parse Ctrl Characters = True
                     ?? Is Orthographic = True
                     ?? Is Culling Enabled = False
                     ?? Horizontal Mapping = Character
                     ?? Vertical Mapping = Character
                     ?? Uv Line Offset = 0,00
                     ?? Geometry Sorting Order = Normal
                     ?? Is Text Object Scale Static = False
                     ?? Vertex Buffer Auto Size Reduction = False
                     ?? Use Max Visible Descender = True
                     ?? Page To Display = 1
                     ?? Is Using Legacy Animation Component = False
                     ?? Is Volumetric Text = False
                     ?? Has Font Asset Changed = False
                     ?? Base Material: **[NULL / НЕ НАЗНАЧЕНО]**
                  ??? **TMP SubMeshUI [LiberationSans SDF Material + LiberationSans SDF Atlas]** [Active] (Layer: UI)
                     • Component: `LayoutElement`
                        ?? Ignore Layout = True
                        ?? Min Width = -1,00
                        ?? Min Height = -1,00
                        ?? Preferred Width = -1,00
                        ?? Preferred Height = -1,00
                        ?? Flexible Width = -1,00
                        ?? Flexible Height = -1,00
                        ?? Layout Priority = 1
                     • Component: `TMP_SubMeshUI`
                        ?? Material: `LiberationSans SDF Material + LiberationSans SDF Atlas` (Material)
                        ?? Raycast Target = True
                        ?? Maskable = True
                        ?? Font Asset: `LiberationSans SDF - Fallback` (TMP_FontAsset)
                        ?? Sprite Asset: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Shared Material: `LiberationSans SDF Material + LiberationSans SDF Atlas` (Material)
                        ?? Is Default Material = False
                        ?? Padding = 1,25
                        ?? Text Component: `Text (TMP)` (TextMeshProUGUI)
                        ?? Material Reference Index = 1
      ??? **ekran** [Active] (Layer: Default)
         • Component: `MeshFilter`
            ?? Mesh: `ekran` (Mesh)
         • Component: `MeshRenderer`
            ?? Cast Shadows = On
            ?? Receive Shadows = True
            ?? Dynamic Occludee = True
            ?? Static Shadow Caster = False
            ?? Motion Vectors = Per Object Motion
            ?? Light Probe Usage = 1
            ?? Reflection Probe Usage = 1
            ?? Ray Tracing Mode = 2
            ?? Ray Trace Procedural = False
            ?? Ray Tracing Accel Struct Build Flags Override = False
            ?? Ray Tracing Accel Struct Build Flags = 1
            ?? Small Mesh Culling = True
            ?? Force Mesh Lod = -1
            ?? Mesh Lod Selection Bias = 0,00
            ?? Rendering Layer Mask = 1
            ?? Renderer Priority = 0
            ?? Probe Anchor: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Light Probe Volume Override: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Scale In Lightmap = 1,00
            ?? Receive GI = 1
            ?? Preserve U Vs = False
            ?? Ignore Normals For Chart Detection = False
            ?? Important GI = False
            ?? Stitch Lightmap Seams = True
            ?? Minimum Chart Size = 4
            ?? Auto UV Max Distance = 0,50
            ?? Auto UV Max Angle = 89,00
            ?? Lightmap Parameters: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Mask Interaction = None
      ??? **knopka1** [Active] (Layer: Default)
         • Component: `MeshFilter`
            ?? Mesh: `knopka1` (Mesh)
         • Component: `MeshRenderer`
            ?? Cast Shadows = On
            ?? Receive Shadows = True
            ?? Dynamic Occludee = True
            ?? Static Shadow Caster = False
            ?? Motion Vectors = Per Object Motion
            ?? Light Probe Usage = 1
            ?? Reflection Probe Usage = 1
            ?? Ray Tracing Mode = 2
            ?? Ray Trace Procedural = False
            ?? Ray Tracing Accel Struct Build Flags Override = False
            ?? Ray Tracing Accel Struct Build Flags = 1
            ?? Small Mesh Culling = True
            ?? Force Mesh Lod = -1
            ?? Mesh Lod Selection Bias = 0,00
            ?? Rendering Layer Mask = 1
            ?? Renderer Priority = 0
            ?? Probe Anchor: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Light Probe Volume Override: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Scale In Lightmap = 1,00
            ?? Receive GI = 1
            ?? Preserve U Vs = False
            ?? Ignore Normals For Chart Detection = False
            ?? Important GI = False
            ?? Stitch Lightmap Seams = True
            ?? Minimum Chart Size = 4
            ?? Auto UV Max Distance = 0,50
            ?? Auto UV Max Angle = 89,00
            ?? Lightmap Parameters: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Mask Interaction = None
      ??? **knopka2** [Active] (Layer: Default)
         • Component: `MeshFilter`
            ?? Mesh: `knopka2` (Mesh)
         • Component: `MeshRenderer`
            ?? Cast Shadows = On
            ?? Receive Shadows = True
            ?? Dynamic Occludee = True
            ?? Static Shadow Caster = False
            ?? Motion Vectors = Per Object Motion
            ?? Light Probe Usage = 1
            ?? Reflection Probe Usage = 1
            ?? Ray Tracing Mode = 2
            ?? Ray Trace Procedural = False
            ?? Ray Tracing Accel Struct Build Flags Override = False
            ?? Ray Tracing Accel Struct Build Flags = 1
            ?? Small Mesh Culling = True
            ?? Force Mesh Lod = -1
            ?? Mesh Lod Selection Bias = 0,00
            ?? Rendering Layer Mask = 1
            ?? Renderer Priority = 0
            ?? Probe Anchor: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Light Probe Volume Override: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Scale In Lightmap = 1,00
            ?? Receive GI = 1
            ?? Preserve U Vs = False
            ?? Ignore Normals For Chart Detection = False
            ?? Important GI = False
            ?? Stitch Lightmap Seams = True
            ?? Minimum Chart Size = 4
            ?? Auto UV Max Distance = 0,50
            ?? Auto UV Max Angle = 89,00
            ?? Lightmap Parameters: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Mask Interaction = None
      ??? **knopka3** [Active] (Layer: Default)
         • Component: `MeshFilter`
            ?? Mesh: `knopka3` (Mesh)
         • Component: `MeshRenderer`
            ?? Cast Shadows = On
            ?? Receive Shadows = True
            ?? Dynamic Occludee = True
            ?? Static Shadow Caster = False
            ?? Motion Vectors = Per Object Motion
            ?? Light Probe Usage = 1
            ?? Reflection Probe Usage = 1
            ?? Ray Tracing Mode = 2
            ?? Ray Trace Procedural = False
            ?? Ray Tracing Accel Struct Build Flags Override = False
            ?? Ray Tracing Accel Struct Build Flags = 1
            ?? Small Mesh Culling = True
            ?? Force Mesh Lod = -1
            ?? Mesh Lod Selection Bias = 0,00
            ?? Rendering Layer Mask = 1
            ?? Renderer Priority = 0
            ?? Probe Anchor: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Light Probe Volume Override: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Scale In Lightmap = 1,00
            ?? Receive GI = 1
            ?? Preserve U Vs = False
            ?? Ignore Normals For Chart Detection = False
            ?? Important GI = False
            ?? Stitch Lightmap Seams = True
            ?? Minimum Chart Size = 4
            ?? Auto UV Max Distance = 0,50
            ?? Auto UV Max Angle = 89,00
            ?? Lightmap Parameters: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Mask Interaction = None
      ??? **knopka4** [Active] (Layer: Default)
         • Component: `MeshFilter`
            ?? Mesh: `knopka4` (Mesh)
         • Component: `MeshRenderer`
            ?? Cast Shadows = On
            ?? Receive Shadows = True
            ?? Dynamic Occludee = True
            ?? Static Shadow Caster = False
            ?? Motion Vectors = Per Object Motion
            ?? Light Probe Usage = 1
            ?? Reflection Probe Usage = 1
            ?? Ray Tracing Mode = 2
            ?? Ray Trace Procedural = False
            ?? Ray Tracing Accel Struct Build Flags Override = False
            ?? Ray Tracing Accel Struct Build Flags = 1
            ?? Small Mesh Culling = True
            ?? Force Mesh Lod = -1
            ?? Mesh Lod Selection Bias = 0,00
            ?? Rendering Layer Mask = 1
            ?? Renderer Priority = 0
            ?? Probe Anchor: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Light Probe Volume Override: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Scale In Lightmap = 1,00
            ?? Receive GI = 1
            ?? Preserve U Vs = False
            ?? Ignore Normals For Chart Detection = False
            ?? Important GI = False
            ?? Stitch Lightmap Seams = True
            ?? Minimum Chart Size = 4
            ?? Auto UV Max Distance = 0,50
            ?? Auto UV Max Angle = 89,00
            ?? Lightmap Parameters: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Mask Interaction = None
      ??? **noga1** [Active] (Layer: Default)
         • Component: `MeshFilter`
            ?? Mesh: `noga1` (Mesh)
         • Component: `MeshRenderer`
            ?? Cast Shadows = On
            ?? Receive Shadows = True
            ?? Dynamic Occludee = True
            ?? Static Shadow Caster = False
            ?? Motion Vectors = Per Object Motion
            ?? Light Probe Usage = 1
            ?? Reflection Probe Usage = 1
            ?? Ray Tracing Mode = 2
            ?? Ray Trace Procedural = False
            ?? Ray Tracing Accel Struct Build Flags Override = False
            ?? Ray Tracing Accel Struct Build Flags = 1
            ?? Small Mesh Culling = True
            ?? Force Mesh Lod = -1
            ?? Mesh Lod Selection Bias = 0,00
            ?? Rendering Layer Mask = 1
            ?? Renderer Priority = 0
            ?? Probe Anchor: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Light Probe Volume Override: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Scale In Lightmap = 1,00
            ?? Receive GI = 1
            ?? Preserve U Vs = False
            ?? Ignore Normals For Chart Detection = False
            ?? Important GI = False
            ?? Stitch Lightmap Seams = True
            ?? Minimum Chart Size = 4
            ?? Auto UV Max Distance = 0,50
            ?? Auto UV Max Angle = 89,00
            ?? Lightmap Parameters: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Mask Interaction = None
      ??? **noga2** [Active] (Layer: Default)
         • Component: `MeshFilter`
            ?? Mesh: `noga2` (Mesh)
         • Component: `MeshRenderer`
            ?? Cast Shadows = On
            ?? Receive Shadows = True
            ?? Dynamic Occludee = True
            ?? Static Shadow Caster = False
            ?? Motion Vectors = Per Object Motion
            ?? Light Probe Usage = 1
            ?? Reflection Probe Usage = 1
            ?? Ray Tracing Mode = 2
            ?? Ray Trace Procedural = False
            ?? Ray Tracing Accel Struct Build Flags Override = False
            ?? Ray Tracing Accel Struct Build Flags = 1
            ?? Small Mesh Culling = True
            ?? Force Mesh Lod = -1
            ?? Mesh Lod Selection Bias = 0,00
            ?? Rendering Layer Mask = 1
            ?? Renderer Priority = 0
            ?? Probe Anchor: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Light Probe Volume Override: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Scale In Lightmap = 1,00
            ?? Receive GI = 1
            ?? Preserve U Vs = False
            ?? Ignore Normals For Chart Detection = False
            ?? Important GI = False
            ?? Stitch Lightmap Seams = True
            ?? Minimum Chart Size = 4
            ?? Auto UV Max Distance = 0,50
            ?? Auto UV Max Angle = 89,00
            ?? Lightmap Parameters: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Mask Interaction = None
      ??? **noga3** [Active] (Layer: Default)
         • Component: `MeshFilter`
            ?? Mesh: `noga3` (Mesh)
         • Component: `MeshRenderer`
            ?? Cast Shadows = On
            ?? Receive Shadows = True
            ?? Dynamic Occludee = True
            ?? Static Shadow Caster = False
            ?? Motion Vectors = Per Object Motion
            ?? Light Probe Usage = 1
            ?? Reflection Probe Usage = 1
            ?? Ray Tracing Mode = 2
            ?? Ray Trace Procedural = False
            ?? Ray Tracing Accel Struct Build Flags Override = False
            ?? Ray Tracing Accel Struct Build Flags = 1
            ?? Small Mesh Culling = True
            ?? Force Mesh Lod = -1
            ?? Mesh Lod Selection Bias = 0,00
            ?? Rendering Layer Mask = 1
            ?? Renderer Priority = 0
            ?? Probe Anchor: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Light Probe Volume Override: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Scale In Lightmap = 1,00
            ?? Receive GI = 1
            ?? Preserve U Vs = False
            ?? Ignore Normals For Chart Detection = False
            ?? Important GI = False
            ?? Stitch Lightmap Seams = True
            ?? Minimum Chart Size = 4
            ?? Auto UV Max Distance = 0,50
            ?? Auto UV Max Angle = 89,00
            ?? Lightmap Parameters: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Mask Interaction = None
      ??? **noga4** [Active] (Layer: Default)
         • Component: `MeshFilter`
            ?? Mesh: `noga4` (Mesh)
         • Component: `MeshRenderer`
            ?? Cast Shadows = On
            ?? Receive Shadows = True
            ?? Dynamic Occludee = True
            ?? Static Shadow Caster = False
            ?? Motion Vectors = Per Object Motion
            ?? Light Probe Usage = 1
            ?? Reflection Probe Usage = 1
            ?? Ray Tracing Mode = 2
            ?? Ray Trace Procedural = False
            ?? Ray Tracing Accel Struct Build Flags Override = False
            ?? Ray Tracing Accel Struct Build Flags = 1
            ?? Small Mesh Culling = True
            ?? Force Mesh Lod = -1
            ?? Mesh Lod Selection Bias = 0,00
            ?? Rendering Layer Mask = 1
            ?? Renderer Priority = 0
            ?? Probe Anchor: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Light Probe Volume Override: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Scale In Lightmap = 1,00
            ?? Receive GI = 1
            ?? Preserve U Vs = False
            ?? Ignore Normals For Chart Detection = False
            ?? Important GI = False
            ?? Stitch Lightmap Seams = True
            ?? Minimum Chart Size = 4
            ?? Auto UV Max Distance = 0,50
            ?? Auto UV Max Angle = 89,00
            ?? Lightmap Parameters: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Mask Interaction = None
      ??? **ruchka** [Active] (Layer: Default)
         • Component: `MeshFilter`
            ?? Mesh: `ruchka` (Mesh)
         • Component: `MeshRenderer`
            ?? Cast Shadows = On
            ?? Receive Shadows = True
            ?? Dynamic Occludee = True
            ?? Static Shadow Caster = False
            ?? Motion Vectors = Per Object Motion
            ?? Light Probe Usage = 1
            ?? Reflection Probe Usage = 1
            ?? Ray Tracing Mode = 2
            ?? Ray Trace Procedural = False
            ?? Ray Tracing Accel Struct Build Flags Override = False
            ?? Ray Tracing Accel Struct Build Flags = 1
            ?? Small Mesh Culling = True
            ?? Force Mesh Lod = -1
            ?? Mesh Lod Selection Bias = 0,00
            ?? Rendering Layer Mask = 1
            ?? Renderer Priority = 0
            ?? Probe Anchor: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Light Probe Volume Override: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Scale In Lightmap = 1,00
            ?? Receive GI = 1
            ?? Preserve U Vs = False
            ?? Ignore Normals For Chart Detection = False
            ?? Important GI = False
            ?? Stitch Lightmap Seams = True
            ?? Minimum Chart Size = 4
            ?? Auto UV Max Distance = 0,50
            ?? Auto UV Max Angle = 89,00
            ?? Lightmap Parameters: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Mask Interaction = None
      ??? **telik** [Active] (Layer: Default)
         • Component: `MeshFilter`
            ?? Mesh: `telik` (Mesh)
         • Component: `MeshRenderer`
            ?? Cast Shadows = On
            ?? Receive Shadows = True
            ?? Dynamic Occludee = True
            ?? Static Shadow Caster = False
            ?? Motion Vectors = Per Object Motion
            ?? Light Probe Usage = 1
            ?? Reflection Probe Usage = 1
            ?? Ray Tracing Mode = 2
            ?? Ray Trace Procedural = False
            ?? Ray Tracing Accel Struct Build Flags Override = False
            ?? Ray Tracing Accel Struct Build Flags = 1
            ?? Small Mesh Culling = True
            ?? Force Mesh Lod = -1
            ?? Mesh Lod Selection Bias = 0,00
            ?? Rendering Layer Mask = 1
            ?? Renderer Priority = 0
            ?? Probe Anchor: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Light Probe Volume Override: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Scale In Lightmap = 1,00
            ?? Receive GI = 1
            ?? Preserve U Vs = False
            ?? Ignore Normals For Chart Detection = False
            ?? Important GI = False
            ?? Stitch Lightmap Seams = True
            ?? Minimum Chart Size = 4
            ?? Auto UV Max Distance = 0,50
            ?? Auto UV Max Angle = 89,00
            ?? Lightmap Parameters: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Mask Interaction = None
      ??? **Cube** [Active] (Layer: Default)
         • Component: `MeshFilter`
            ?? Mesh: `Cube` (Mesh)
         • Component: `MeshRenderer`
            ?? Cast Shadows = On
            ?? Receive Shadows = True
            ?? Dynamic Occludee = True
            ?? Static Shadow Caster = False
            ?? Motion Vectors = Per Object Motion
            ?? Light Probe Usage = 1
            ?? Reflection Probe Usage = 1
            ?? Ray Tracing Mode = 2
            ?? Ray Trace Procedural = False
            ?? Ray Tracing Accel Struct Build Flags Override = False
            ?? Ray Tracing Accel Struct Build Flags = 1
            ?? Small Mesh Culling = True
            ?? Force Mesh Lod = -1
            ?? Mesh Lod Selection Bias = 0,00
            ?? Rendering Layer Mask = 1
            ?? Renderer Priority = 0
            ?? Probe Anchor: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Light Probe Volume Override: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Scale In Lightmap = 1,00
            ?? Receive GI = 1
            ?? Preserve U Vs = False
            ?? Ignore Normals For Chart Detection = False
            ?? Important GI = False
            ?? Stitch Lightmap Seams = True
            ?? Minimum Chart Size = 4
            ?? Auto UV Max Distance = 0,50
            ?? Auto UV Max Angle = 89,00
            ?? Lightmap Parameters: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Mask Interaction = None
         • Component: `BoxCollider`
            ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Layer Override Priority = 0
            ?? Is Trigger = False
            ?? Provides Contacts = False
   ??? **telek2** [Active] (Layer: Default)
      ??? **Cube** [Active] (Layer: Default)
         • Component: `MeshFilter`
            ?? Mesh: `Cube` (Mesh)
         • Component: `MeshRenderer`
            ?? Cast Shadows = On
            ?? Receive Shadows = True
            ?? Dynamic Occludee = True
            ?? Static Shadow Caster = False
            ?? Motion Vectors = Per Object Motion
            ?? Light Probe Usage = 1
            ?? Reflection Probe Usage = 1
            ?? Ray Tracing Mode = 2
            ?? Ray Trace Procedural = False
            ?? Ray Tracing Accel Struct Build Flags Override = False
            ?? Ray Tracing Accel Struct Build Flags = 1
            ?? Small Mesh Culling = True
            ?? Force Mesh Lod = -1
            ?? Mesh Lod Selection Bias = 0,00
            ?? Rendering Layer Mask = 1
            ?? Renderer Priority = 0
            ?? Probe Anchor: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Light Probe Volume Override: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Scale In Lightmap = 1,00
            ?? Receive GI = 1
            ?? Preserve U Vs = False
            ?? Ignore Normals For Chart Detection = False
            ?? Important GI = False
            ?? Stitch Lightmap Seams = True
            ?? Minimum Chart Size = 4
            ?? Auto UV Max Distance = 0,50
            ?? Auto UV Max Angle = 89,00
            ?? Lightmap Parameters: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Mask Interaction = None
         • Component: `BoxCollider`
            ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Layer Override Priority = 0
            ?? Is Trigger = False
            ?? Provides Contacts = False
      ??? **UI_MONITOR_RIGHT** [Active] (Layer: Default)
         ??? **Canvas_RightMonitor** [Active] (Layer: UI)
            • Component: `Canvas`
               ?? Render Mode = World Space
               ?? Camera: `Main Camera` (Camera)
               ?? Plane Distance = 100,00
               ?? Pixel Perfect = False
               ?? Receives Events = True
               ?? Override Sorting = False
               ?? Override Pixel Perfect = False
               ?? Sorting Bucket Normalized Size = 0,00
               ?? Vertex Color Always Gamma Space = False
               ?? Additional Shader Channels Flag = -1
               ?? Update Rect Transform For Standalone = Enabled
            • Component: `CanvasScaler`
               ?? Ui Scale Mode = Constant Pixel Size
               ?? Reference Pixels Per Unit = 100,00
               ?? Scale Factor = 1,00
               ?? Screen Match Mode = Match Width Or Height
               ?? Match Width Or Height = 0,00
               ?? Physical Unit = Points
               ?? Fallback Screen DPI = 96,00
               ?? Default Sprite DPI = 96,00
               ?? Dynamic Pixels Per Unit = 1,00
               ?? Preset Info Is World = True
            • Component: `GraphicRaycaster`
               ?? Ignore Reversed Graphics = True
               ?? Blocking Objects = None
            • Component: `RightMonitorMapController`
               ?? Snapshot Btn Canvas Group: `Btn_TakeSnapshot  ` (CanvasGroup)
               ?? Take Snapshot Button: `Btn_TakeSnapshot  ` (Button)
               ?? Snapshot Display Image: `SnapshotDisplay` (Image)
               ?? No Signal Placeholder: **[NULL / НЕ НАЗНАЧЕНО]**
               ?? Screen Flash Image: `FlashImage` (Image)
               ?? Camera Status Text: **[NULL / НЕ НАЗНАЧЕНО]**
               ?? Target Building Text: **[NULL / НЕ НАЗНАЧЕНО]**
            ??? **FlashImage** [Active] (Layer: UI)
               • Component: `Image`
                  ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
                  ?? Raycast Target = True
                  ?? Maskable = True
                  ?? Sprite: **[NULL / НЕ НАЗНАЧЕНО]**
                  ?? Type = Simple
                  ?? Preserve Aspect = False
                  ?? Fill Center = True
                  ?? Fill Method = Radial 360
                  ?? Fill Amount = 1,00
                  ?? Fill Clockwise = True
                  ?? Fill Origin = 0
                  ?? Use Sprite Mesh = False
                  ?? Pixels Per Unit Multiplier = 1,00
            ??? **SnapshotDisplay** [Active] (Layer: UI)
               • Component: `Image`
                  ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
                  ?? Raycast Target = True
                  ?? Maskable = True
                  ?? Sprite: `Background` (Sprite)
                  ?? Type = Sliced
                  ?? Preserve Aspect = False
                  ?? Fill Center = True
                  ?? Fill Method = Radial 360
                  ?? Fill Amount = 1,00
                  ?? Fill Clockwise = True
                  ?? Fill Origin = 0
                  ?? Use Sprite Mesh = False
                  ?? Pixels Per Unit Multiplier = 1,00
            ??? **Btn_TakeSnapshot  ** [Active] (Layer: UI)
               • Component: `Image`
                  ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
                  ?? Raycast Target = True
                  ?? Maskable = True
                  ?? Sprite: `UISprite` (Sprite)
                  ?? Type = Sliced
                  ?? Preserve Aspect = False
                  ?? Fill Center = True
                  ?? Fill Method = Radial 360
                  ?? Fill Amount = 1,00
                  ?? Fill Clockwise = True
                  ?? Fill Origin = 0
                  ?? Use Sprite Mesh = False
                  ?? Pixels Per Unit Multiplier = 1,00
               • Component: `Button`
                  ?? Transition = Color Tint
                  ?? Interactable = True
                  ?? Target Graphic: `Btn_TakeSnapshot  ` (Image)
               • Component: `CanvasGroup`
                  ?? Alpha = 1,00
                  ?? Interactable = True
                  ?? Blocks Raycasts = True
                  ?? Ignore Parent Groups = False
               ??? **Text (TMP)** [Active] (Layer: UI)
                  • Component: `TextMeshProUGUI`
                     ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
                     ?? Raycast Target = True
                     ?? Maskable = True
                     ?? Text = "Сделать снимок"
                     ?? Is Right To Left = False
                     ?? Font Asset: `LiberationSans SDF` (TMP_FontAsset)
                     ?? Shared Material: `LiberationSans SDF Material` (Material)
                     ?? Font Material: **[NULL / НЕ НАЗНАЧЕНО]**
                     ?? Enable Vertex Gradient = False
                     ?? Color Mode = Four Corners Gradient
                     ?? Font Color Gradient Preset: **[NULL / НЕ НАЗНАЧЕНО]**
                     ?? Sprite Asset: **[NULL / НЕ НАЗНАЧЕНО]**
                     ?? Tint All Sprites = False
                     ?? Style Sheet: **[NULL / НЕ НАЗНАЧЕНО]**
                     ?? Text Style Hash Code = -1183493901
                     ?? Override Html Colors = False
                     ?? Font Size = 50,00
                     ?? Font Size Base = 50,00
                     ?? Font Weight = Regular
                     ?? Enable Auto Sizing = False
                     ?? Font Size Min = 18,00
                     ?? Font Size Max = 72,00
                     ?? Font Style = Normal
                     ?? Horizontal Alignment = Center
                     ?? Vertical Alignment = Middle
                     ?? Text Alignment = Converted
                     ?? Character Spacing = 0,00
                     ?? Character Horizontal Scale = 1,00
                     ?? Word Spacing = 0,00
                     ?? Line Spacing = 0,00
                     ?? Line Spacing Max = 0,00
                     ?? Paragraph Spacing = 0,00
                     ?? Char Width Max Adj = 0,00
                     ?? Text Wrapping Mode = Normal
                     ?? Word Wrapping Ratios = 0,40
                     ?? Overflow Mode = Overflow
                     ?? Linked Text Component: **[NULL / НЕ НАЗНАЧЕНО]**
                     ?? Parent Linked Component: **[NULL / НЕ НАЗНАЧЕНО]**
                     ?? Enable Kerning = False
                     ?? Enable Extra Padding = False
                     ?? Check Padding Required = False
                     ?? Is Rich Text = True
                     ?? Emoji Fallback Support = True
                     ?? Parse Ctrl Characters = True
                     ?? Is Orthographic = True
                     ?? Is Culling Enabled = False
                     ?? Horizontal Mapping = Character
                     ?? Vertical Mapping = Character
                     ?? Uv Line Offset = 0,00
                     ?? Geometry Sorting Order = Normal
                     ?? Is Text Object Scale Static = False
                     ?? Vertex Buffer Auto Size Reduction = False
                     ?? Use Max Visible Descender = True
                     ?? Page To Display = 1
                     ?? Is Using Legacy Animation Component = False
                     ?? Is Volumetric Text = False
                     ?? Has Font Asset Changed = False
                     ?? Base Material: **[NULL / НЕ НАЗНАЧЕНО]**
                  ??? **TMP SubMeshUI [LiberationSans SDF Material + LiberationSans SDF Atlas]** [Active] (Layer: UI)
                     • Component: `LayoutElement`
                        ?? Ignore Layout = True
                        ?? Min Width = -1,00
                        ?? Min Height = -1,00
                        ?? Preferred Width = -1,00
                        ?? Preferred Height = -1,00
                        ?? Flexible Width = -1,00
                        ?? Flexible Height = -1,00
                        ?? Layout Priority = 1
                     • Component: `TMP_SubMeshUI`
                        ?? Material: `LiberationSans SDF Material + LiberationSans SDF Atlas` (Material)
                        ?? Raycast Target = True
                        ?? Maskable = True
                        ?? Font Asset: `LiberationSans SDF - Fallback` (TMP_FontAsset)
                        ?? Sprite Asset: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Shared Material: `LiberationSans SDF Material + LiberationSans SDF Atlas` (Material)
                        ?? Is Default Material = False
                        ?? Padding = 1,25
                        ?? Text Component: `Text (TMP)` (TextMeshProUGUI)
                        ?? Material Reference Index = 1
            ??? **Panel_CityMap** [Active] (Layer: UI)
               • Component: `Image`
                  ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
                  ?? Raycast Target = False
                  ?? Maskable = True
                  ?? Sprite: `Background` (Sprite)
                  ?? Type = Sliced
                  ?? Preserve Aspect = False
                  ?? Fill Center = True
                  ?? Fill Method = Radial 360
                  ?? Fill Amount = 1,00
                  ?? Fill Clockwise = True
                  ?? Fill Origin = 0
                  ?? Use Sprite Mesh = False
                  ?? Pixels Per Unit Multiplier = 1,00
               ??? **Camera_01** [Active] (Layer: UI)
                  • Component: `CameraPoint`
                     ?? Camera Name = "Камера №1"
                     ?? Snapshot Sprite: `8fb30f6602c76049c23b90c03d305bca_0` (Sprite)
                     ?? Outline Object: `OutlineImage  ` (GameObject)
                     ?? Button: `IconImage     ` (Button)
                  ??? **OutlineImage  ** [Disabled] (Layer: UI)
                     • Component: `Image`
                        ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Raycast Target = True
                        ?? Maskable = True
                        ?? Sprite: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Type = Simple
                        ?? Preserve Aspect = False
                        ?? Fill Center = True
                        ?? Fill Method = Radial 360
                        ?? Fill Amount = 1,00
                        ?? Fill Clockwise = True
                        ?? Fill Origin = 0
                        ?? Use Sprite Mesh = False
                        ?? Pixels Per Unit Multiplier = 1,00
                  ??? **IconImage     ** [Active] (Layer: UI)
                     • Component: `Image`
                        ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Raycast Target = True
                        ?? Maskable = True
                        ?? Sprite: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Type = Simple
                        ?? Preserve Aspect = False
                        ?? Fill Center = True
                        ?? Fill Method = Radial 360
                        ?? Fill Amount = 1,00
                        ?? Fill Clockwise = True
                        ?? Fill Origin = 0
                        ?? Use Sprite Mesh = False
                        ?? Pixels Per Unit Multiplier = 1,00
                     • Component: `Button`
                        ?? Transition = Color Tint
                        ?? Interactable = True
                        ?? Target Graphic: `IconImage     ` (Image)
                  ??? **Text** [Active] (Layer: UI)
                     • Component: `TextMeshProUGUI`
                        ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Raycast Target = False
                        ?? Maskable = True
                        ?? Text = "Камера№1"
                        ?? Is Right To Left = False
                        ?? Font Asset: `LiberationSans SDF` (TMP_FontAsset)
                        ?? Shared Material: `LiberationSans SDF Material` (Material)
                        ?? Font Material: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Enable Vertex Gradient = False
                        ?? Color Mode = Four Corners Gradient
                        ?? Font Color Gradient Preset: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Sprite Asset: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Tint All Sprites = False
                        ?? Style Sheet: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Text Style Hash Code = -1183493901
                        ?? Override Html Colors = False
                        ?? Font Size = 24,00
                        ?? Font Size Base = 24,00
                        ?? Font Weight = Regular
                        ?? Enable Auto Sizing = False
                        ?? Font Size Min = 18,00
                        ?? Font Size Max = 72,00
                        ?? Font Style = Normal
                        ?? Horizontal Alignment = Center
                        ?? Vertical Alignment = Middle
                        ?? Text Alignment = Converted
                        ?? Character Spacing = 0,00
                        ?? Character Horizontal Scale = 1,00
                        ?? Word Spacing = 0,00
                        ?? Line Spacing = 0,00
                        ?? Line Spacing Max = 0,00
                        ?? Paragraph Spacing = 0,00
                        ?? Char Width Max Adj = 0,00
                        ?? Text Wrapping Mode = Normal
                        ?? Word Wrapping Ratios = 0,40
                        ?? Overflow Mode = Overflow
                        ?? Linked Text Component: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Parent Linked Component: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Enable Kerning = False
                        ?? Enable Extra Padding = False
                        ?? Check Padding Required = False
                        ?? Is Rich Text = True
                        ?? Emoji Fallback Support = True
                        ?? Parse Ctrl Characters = True
                        ?? Is Orthographic = True
                        ?? Is Culling Enabled = False
                        ?? Horizontal Mapping = Character
                        ?? Vertical Mapping = Character
                        ?? Uv Line Offset = 0,00
                        ?? Geometry Sorting Order = Normal
                        ?? Is Text Object Scale Static = False
                        ?? Vertex Buffer Auto Size Reduction = False
                        ?? Use Max Visible Descender = True
                        ?? Page To Display = 1
                        ?? Is Using Legacy Animation Component = False
                        ?? Is Volumetric Text = False
                        ?? Has Font Asset Changed = False
                        ?? Base Material: **[NULL / НЕ НАЗНАЧЕНО]**
                     ??? **TMP SubMeshUI [LiberationSans SDF Material + LiberationSans SDF Atlas]** [Active] (Layer: UI)
                        • Component: `LayoutElement`
                           ?? Ignore Layout = True
                           ?? Min Width = -1,00
                           ?? Min Height = -1,00
                           ?? Preferred Width = -1,00
                           ?? Preferred Height = -1,00
                           ?? Flexible Width = -1,00
                           ?? Flexible Height = -1,00
                           ?? Layout Priority = 1
                        • Component: `TMP_SubMeshUI`
                           ?? Material: `LiberationSans SDF Material + LiberationSans SDF Atlas` (Material)
                           ?? Raycast Target = False
                           ?? Maskable = True
                           ?? Font Asset: `LiberationSans SDF - Fallback` (TMP_FontAsset)
                           ?? Sprite Asset: **[NULL / НЕ НАЗНАЧЕНО]**
                           ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
                           ?? Shared Material: `LiberationSans SDF Material + LiberationSans SDF Atlas` (Material)
                           ?? Is Default Material = False
                           ?? Padding = 1,25
                           ?? Text Component: `Text` (TextMeshProUGUI)
                           ?? Material Reference Index = 1
               ??? **Camera_02** [Active] (Layer: UI)
                  • Component: `Image`
                     ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
                     ?? Raycast Target = True
                     ?? Maskable = True
                     ?? Sprite: `UISprite` (Sprite)
                     ?? Type = Sliced
                     ?? Preserve Aspect = False
                     ?? Fill Center = True
                     ?? Fill Method = Radial 360
                     ?? Fill Amount = 1,00
                     ?? Fill Clockwise = True
                     ?? Fill Origin = 0
                     ?? Use Sprite Mesh = False
                     ?? Pixels Per Unit Multiplier = 1,00
                  • Component: `CameraPoint`
                     ?? Camera Name = "Камера №2"
                     ?? Snapshot Sprite: `1487837888_0` (Sprite)
                     ?? Outline Object: `OutlineImage  ` (GameObject)
                     ?? Button: `IconImage     ` (Button)
                  ??? **OutlineImage  ** [Disabled] (Layer: UI)
                     • Component: `Image`
                        ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Raycast Target = True
                        ?? Maskable = True
                        ?? Sprite: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Type = Simple
                        ?? Preserve Aspect = False
                        ?? Fill Center = True
                        ?? Fill Method = Radial 360
                        ?? Fill Amount = 1,00
                        ?? Fill Clockwise = True
                        ?? Fill Origin = 0
                        ?? Use Sprite Mesh = False
                        ?? Pixels Per Unit Multiplier = 1,00
                  ??? **IconImage     ** [Active] (Layer: UI)
                     • Component: `Image`
                        ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Raycast Target = True
                        ?? Maskable = True
                        ?? Sprite: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Type = Simple
                        ?? Preserve Aspect = False
                        ?? Fill Center = True
                        ?? Fill Method = Radial 360
                        ?? Fill Amount = 1,00
                        ?? Fill Clockwise = True
                        ?? Fill Origin = 0
                        ?? Use Sprite Mesh = False
                        ?? Pixels Per Unit Multiplier = 1,00
                     • Component: `Button`
                        ?? Transition = Color Tint
                        ?? Interactable = True
                        ?? Target Graphic: `IconImage     ` (Image)
                  ??? **Text** [Active] (Layer: UI)
                     • Component: `TextMeshProUGUI`
                        ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Raycast Target = False
                        ?? Maskable = True
                        ?? Text = "Камера№2"
                        ?? Is Right To Left = False
                        ?? Font Asset: `LiberationSans SDF` (TMP_FontAsset)
                        ?? Shared Material: `LiberationSans SDF Material` (Material)
                        ?? Font Material: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Enable Vertex Gradient = False
                        ?? Color Mode = Four Corners Gradient
                        ?? Font Color Gradient Preset: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Sprite Asset: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Tint All Sprites = False
                        ?? Style Sheet: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Text Style Hash Code = -1183493901
                        ?? Override Html Colors = False
                        ?? Font Size = 24,00
                        ?? Font Size Base = 24,00
                        ?? Font Weight = Regular
                        ?? Enable Auto Sizing = False
                        ?? Font Size Min = 18,00
                        ?? Font Size Max = 72,00
                        ?? Font Style = Normal
                        ?? Horizontal Alignment = Center
                        ?? Vertical Alignment = Middle
                        ?? Text Alignment = Converted
                        ?? Character Spacing = 0,00
                        ?? Character Horizontal Scale = 1,00
                        ?? Word Spacing = 0,00
                        ?? Line Spacing = 0,00
                        ?? Line Spacing Max = 0,00
                        ?? Paragraph Spacing = 0,00
                        ?? Char Width Max Adj = 0,00
                        ?? Text Wrapping Mode = Normal
                        ?? Word Wrapping Ratios = 0,40
                        ?? Overflow Mode = Overflow
                        ?? Linked Text Component: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Parent Linked Component: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Enable Kerning = False
                        ?? Enable Extra Padding = False
                        ?? Check Padding Required = False
                        ?? Is Rich Text = True
                        ?? Emoji Fallback Support = True
                        ?? Parse Ctrl Characters = True
                        ?? Is Orthographic = True
                        ?? Is Culling Enabled = False
                        ?? Horizontal Mapping = Character
                        ?? Vertical Mapping = Character
                        ?? Uv Line Offset = 0,00
                        ?? Geometry Sorting Order = Normal
                        ?? Is Text Object Scale Static = False
                        ?? Vertex Buffer Auto Size Reduction = False
                        ?? Use Max Visible Descender = True
                        ?? Page To Display = 1
                        ?? Is Using Legacy Animation Component = False
                        ?? Is Volumetric Text = False
                        ?? Has Font Asset Changed = False
                        ?? Base Material: **[NULL / НЕ НАЗНАЧЕНО]**
                     ??? **TMP SubMeshUI [LiberationSans SDF Material + LiberationSans SDF Atlas]** [Active] (Layer: UI)
                        • Component: `LayoutElement`
                           ?? Ignore Layout = True
                           ?? Min Width = -1,00
                           ?? Min Height = -1,00
                           ?? Preferred Width = -1,00
                           ?? Preferred Height = -1,00
                           ?? Flexible Width = -1,00
                           ?? Flexible Height = -1,00
                           ?? Layout Priority = 1
                        • Component: `TMP_SubMeshUI`
                           ?? Material: `LiberationSans SDF Material + LiberationSans SDF Atlas` (Material)
                           ?? Raycast Target = False
                           ?? Maskable = True
                           ?? Font Asset: `LiberationSans SDF - Fallback` (TMP_FontAsset)
                           ?? Sprite Asset: **[NULL / НЕ НАЗНАЧЕНО]**
                           ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
                           ?? Shared Material: `LiberationSans SDF Material + LiberationSans SDF Atlas` (Material)
                           ?? Is Default Material = False
                           ?? Padding = 1,25
                           ?? Text Component: `Text` (TextMeshProUGUI)
                           ?? Material Reference Index = 1
               ??? **Camera_03** [Active] (Layer: UI)
                  • Component: `Image`
                     ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
                     ?? Raycast Target = True
                     ?? Maskable = True
                     ?? Sprite: `UISprite` (Sprite)
                     ?? Type = Sliced
                     ?? Preserve Aspect = False
                     ?? Fill Center = True
                     ?? Fill Method = Radial 360
                     ?? Fill Amount = 1,00
                     ?? Fill Clockwise = True
                     ?? Fill Origin = 0
                     ?? Use Sprite Mesh = False
                     ?? Pixels Per Unit Multiplier = 1,00
                  • Component: `CameraPoint`
                     ?? Camera Name = "Камера №3"
                     ?? Snapshot Sprite: `images-_5__0` (Sprite)
                     ?? Outline Object: `OutlineImage  ` (GameObject)
                     ?? Button: `IconImage     ` (Button)
                  ??? **OutlineImage  ** [Disabled] (Layer: UI)
                     • Component: `Image`
                        ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Raycast Target = True
                        ?? Maskable = True
                        ?? Sprite: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Type = Simple
                        ?? Preserve Aspect = False
                        ?? Fill Center = True
                        ?? Fill Method = Radial 360
                        ?? Fill Amount = 1,00
                        ?? Fill Clockwise = True
                        ?? Fill Origin = 0
                        ?? Use Sprite Mesh = False
                        ?? Pixels Per Unit Multiplier = 1,00
                  ??? **IconImage     ** [Active] (Layer: UI)
                     • Component: `Image`
                        ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Raycast Target = True
                        ?? Maskable = True
                        ?? Sprite: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Type = Simple
                        ?? Preserve Aspect = False
                        ?? Fill Center = True
                        ?? Fill Method = Radial 360
                        ?? Fill Amount = 1,00
                        ?? Fill Clockwise = True
                        ?? Fill Origin = 0
                        ?? Use Sprite Mesh = False
                        ?? Pixels Per Unit Multiplier = 1,00
                     • Component: `Button`
                        ?? Transition = Color Tint
                        ?? Interactable = True
                        ?? Target Graphic: `IconImage     ` (Image)
                  ??? **Text** [Active] (Layer: UI)
                     • Component: `TextMeshProUGUI`
                        ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Raycast Target = False
                        ?? Maskable = True
                        ?? Text = "Камера№3"
                        ?? Is Right To Left = False
                        ?? Font Asset: `LiberationSans SDF` (TMP_FontAsset)
                        ?? Shared Material: `LiberationSans SDF Material` (Material)
                        ?? Font Material: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Enable Vertex Gradient = False
                        ?? Color Mode = Four Corners Gradient
                        ?? Font Color Gradient Preset: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Sprite Asset: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Tint All Sprites = False
                        ?? Style Sheet: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Text Style Hash Code = -1183493901
                        ?? Override Html Colors = False
                        ?? Font Size = 24,00
                        ?? Font Size Base = 24,00
                        ?? Font Weight = Regular
                        ?? Enable Auto Sizing = False
                        ?? Font Size Min = 18,00
                        ?? Font Size Max = 72,00
                        ?? Font Style = Normal
                        ?? Horizontal Alignment = Center
                        ?? Vertical Alignment = Middle
                        ?? Text Alignment = Converted
                        ?? Character Spacing = 0,00
                        ?? Character Horizontal Scale = 1,00
                        ?? Word Spacing = 0,00
                        ?? Line Spacing = 0,00
                        ?? Line Spacing Max = 0,00
                        ?? Paragraph Spacing = 0,00
                        ?? Char Width Max Adj = 0,00
                        ?? Text Wrapping Mode = Normal
                        ?? Word Wrapping Ratios = 0,40
                        ?? Overflow Mode = Overflow
                        ?? Linked Text Component: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Parent Linked Component: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Enable Kerning = False
                        ?? Enable Extra Padding = False
                        ?? Check Padding Required = False
                        ?? Is Rich Text = True
                        ?? Emoji Fallback Support = True
                        ?? Parse Ctrl Characters = True
                        ?? Is Orthographic = True
                        ?? Is Culling Enabled = False
                        ?? Horizontal Mapping = Character
                        ?? Vertical Mapping = Character
                        ?? Uv Line Offset = 0,00
                        ?? Geometry Sorting Order = Normal
                        ?? Is Text Object Scale Static = False
                        ?? Vertex Buffer Auto Size Reduction = False
                        ?? Use Max Visible Descender = True
                        ?? Page To Display = 1
                        ?? Is Using Legacy Animation Component = False
                        ?? Is Volumetric Text = False
                        ?? Has Font Asset Changed = False
                        ?? Base Material: **[NULL / НЕ НАЗНАЧЕНО]**
                     ??? **TMP SubMeshUI [LiberationSans SDF Material + LiberationSans SDF Atlas]** [Active] (Layer: UI)
                        • Component: `LayoutElement`
                           ?? Ignore Layout = True
                           ?? Min Width = -1,00
                           ?? Min Height = -1,00
                           ?? Preferred Width = -1,00
                           ?? Preferred Height = -1,00
                           ?? Flexible Width = -1,00
                           ?? Flexible Height = -1,00
                           ?? Layout Priority = 1
                        • Component: `TMP_SubMeshUI`
                           ?? Material: `LiberationSans SDF Material + LiberationSans SDF Atlas` (Material)
                           ?? Raycast Target = False
                           ?? Maskable = True
                           ?? Font Asset: `LiberationSans SDF - Fallback` (TMP_FontAsset)
                           ?? Sprite Asset: **[NULL / НЕ НАЗНАЧЕНО]**
                           ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
                           ?? Shared Material: `LiberationSans SDF Material + LiberationSans SDF Atlas` (Material)
                           ?? Is Default Material = False
                           ?? Padding = 1,25
                           ?? Text Component: `Text` (TextMeshProUGUI)
                           ?? Material Reference Index = 1
               ??? **Camera_04** [Active] (Layer: UI)
                  • Component: `Image`
                     ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
                     ?? Raycast Target = True
                     ?? Maskable = True
                     ?? Sprite: `UISprite` (Sprite)
                     ?? Type = Sliced
                     ?? Preserve Aspect = False
                     ?? Fill Center = True
                     ?? Fill Method = Radial 360
                     ?? Fill Amount = 1,00
                     ?? Fill Clockwise = True
                     ?? Fill Origin = 0
                     ?? Use Sprite Mesh = False
                     ?? Pixels Per Unit Multiplier = 1,00
                  • Component: `CameraPoint`
                     ?? Camera Name = "Камера №4"
                     ?? Snapshot Sprite: `images-_7__0` (Sprite)
                     ?? Outline Object: `OutlineImage  ` (GameObject)
                     ?? Button: `IconImage     ` (Button)
                  ??? **OutlineImage  ** [Disabled] (Layer: UI)
                     • Component: `Image`
                        ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Raycast Target = True
                        ?? Maskable = True
                        ?? Sprite: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Type = Simple
                        ?? Preserve Aspect = False
                        ?? Fill Center = True
                        ?? Fill Method = Radial 360
                        ?? Fill Amount = 1,00
                        ?? Fill Clockwise = True
                        ?? Fill Origin = 0
                        ?? Use Sprite Mesh = False
                        ?? Pixels Per Unit Multiplier = 1,00
                  ??? **IconImage     ** [Active] (Layer: UI)
                     • Component: `Image`
                        ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Raycast Target = True
                        ?? Maskable = True
                        ?? Sprite: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Type = Simple
                        ?? Preserve Aspect = False
                        ?? Fill Center = True
                        ?? Fill Method = Radial 360
                        ?? Fill Amount = 1,00
                        ?? Fill Clockwise = True
                        ?? Fill Origin = 0
                        ?? Use Sprite Mesh = False
                        ?? Pixels Per Unit Multiplier = 1,00
                     • Component: `Button`
                        ?? Transition = Color Tint
                        ?? Interactable = True
                        ?? Target Graphic: `IconImage     ` (Image)
                  ??? **Text** [Active] (Layer: UI)
                     • Component: `TextMeshProUGUI`
                        ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Raycast Target = False
                        ?? Maskable = True
                        ?? Text = "Камера№4"
                        ?? Is Right To Left = False
                        ?? Font Asset: `LiberationSans SDF` (TMP_FontAsset)
                        ?? Shared Material: `LiberationSans SDF Material` (Material)
                        ?? Font Material: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Enable Vertex Gradient = False
                        ?? Color Mode = Four Corners Gradient
                        ?? Font Color Gradient Preset: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Sprite Asset: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Tint All Sprites = False
                        ?? Style Sheet: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Text Style Hash Code = -1183493901
                        ?? Override Html Colors = False
                        ?? Font Size = 24,00
                        ?? Font Size Base = 24,00
                        ?? Font Weight = Regular
                        ?? Enable Auto Sizing = False
                        ?? Font Size Min = 18,00
                        ?? Font Size Max = 72,00
                        ?? Font Style = Normal
                        ?? Horizontal Alignment = Center
                        ?? Vertical Alignment = Middle
                        ?? Text Alignment = Converted
                        ?? Character Spacing = 0,00
                        ?? Character Horizontal Scale = 1,00
                        ?? Word Spacing = 0,00
                        ?? Line Spacing = 0,00
                        ?? Line Spacing Max = 0,00
                        ?? Paragraph Spacing = 0,00
                        ?? Char Width Max Adj = 0,00
                        ?? Text Wrapping Mode = Normal
                        ?? Word Wrapping Ratios = 0,40
                        ?? Overflow Mode = Overflow
                        ?? Linked Text Component: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Parent Linked Component: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Enable Kerning = False
                        ?? Enable Extra Padding = False
                        ?? Check Padding Required = False
                        ?? Is Rich Text = True
                        ?? Emoji Fallback Support = True
                        ?? Parse Ctrl Characters = True
                        ?? Is Orthographic = True
                        ?? Is Culling Enabled = False
                        ?? Horizontal Mapping = Character
                        ?? Vertical Mapping = Character
                        ?? Uv Line Offset = 0,00
                        ?? Geometry Sorting Order = Normal
                        ?? Is Text Object Scale Static = False
                        ?? Vertex Buffer Auto Size Reduction = False
                        ?? Use Max Visible Descender = True
                        ?? Page To Display = 1
                        ?? Is Using Legacy Animation Component = False
                        ?? Is Volumetric Text = False
                        ?? Has Font Asset Changed = False
                        ?? Base Material: **[NULL / НЕ НАЗНАЧЕНО]**
                     ??? **TMP SubMeshUI [LiberationSans SDF Material + LiberationSans SDF Atlas]** [Active] (Layer: UI)
                        • Component: `LayoutElement`
                           ?? Ignore Layout = True
                           ?? Min Width = -1,00
                           ?? Min Height = -1,00
                           ?? Preferred Width = -1,00
                           ?? Preferred Height = -1,00
                           ?? Flexible Width = -1,00
                           ?? Flexible Height = -1,00
                           ?? Layout Priority = 1
                        • Component: `TMP_SubMeshUI`
                           ?? Material: `LiberationSans SDF Material + LiberationSans SDF Atlas` (Material)
                           ?? Raycast Target = False
                           ?? Maskable = True
                           ?? Font Asset: `LiberationSans SDF - Fallback` (TMP_FontAsset)
                           ?? Sprite Asset: **[NULL / НЕ НАЗНАЧЕНО]**
                           ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
                           ?? Shared Material: `LiberationSans SDF Material + LiberationSans SDF Atlas` (Material)
                           ?? Is Default Material = False
                           ?? Padding = 1,25
                           ?? Text Component: `Text` (TextMeshProUGUI)
                           ?? Material Reference Index = 1
               ??? **Building_01** [Active] (Layer: UI)
                  • Component: `BuildingTarget`
                     ?? Building Name = "Здание №1"
                     ?? Sector Code = "Сектор Б-3"
                     ?? Outline Object: `Outline Object` (GameObject)
                     ?? Button: `BuildingImage` (Button)
                  ??? **Outline Object** [Disabled] (Layer: UI)
                     • Component: `Image`
                        ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Raycast Target = True
                        ?? Maskable = True
                        ?? Sprite: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Type = Simple
                        ?? Preserve Aspect = False
                        ?? Fill Center = True
                        ?? Fill Method = Radial 360
                        ?? Fill Amount = 1,00
                        ?? Fill Clockwise = True
                        ?? Fill Origin = 0
                        ?? Use Sprite Mesh = False
                        ?? Pixels Per Unit Multiplier = 1,00
                  ??? **BuildingImage** [Active] (Layer: UI)
                     • Component: `Image`
                        ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Raycast Target = True
                        ?? Maskable = True
                        ?? Sprite: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Type = Simple
                        ?? Preserve Aspect = False
                        ?? Fill Center = True
                        ?? Fill Method = Radial 360
                        ?? Fill Amount = 1,00
                        ?? Fill Clockwise = True
                        ?? Fill Origin = 0
                        ?? Use Sprite Mesh = False
                        ?? Pixels Per Unit Multiplier = 1,00
                     • Component: `Button`
                        ?? Transition = Color Tint
                        ?? Interactable = True
                        ?? Target Graphic: `BuildingImage` (Image)
                  ??? **Text (TMP)** [Active] (Layer: UI)
                     • Component: `TextMeshProUGUI`
                        ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Raycast Target = False
                        ?? Maskable = True
                        ?? Text = "Здание №1"
                        ?? Is Right To Left = False
                        ?? Font Asset: `LiberationSans SDF` (TMP_FontAsset)
                        ?? Shared Material: `LiberationSans SDF Material` (Material)
                        ?? Font Material: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Enable Vertex Gradient = False
                        ?? Color Mode = Four Corners Gradient
                        ?? Font Color Gradient Preset: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Sprite Asset: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Tint All Sprites = False
                        ?? Style Sheet: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Text Style Hash Code = -1183493901
                        ?? Override Html Colors = False
                        ?? Font Size = 36,00
                        ?? Font Size Base = 36,00
                        ?? Font Weight = Regular
                        ?? Enable Auto Sizing = False
                        ?? Font Size Min = 18,00
                        ?? Font Size Max = 72,00
                        ?? Font Style = Normal
                        ?? Horizontal Alignment = Center
                        ?? Vertical Alignment = Middle
                        ?? Text Alignment = Converted
                        ?? Character Spacing = 0,00
                        ?? Character Horizontal Scale = 1,00
                        ?? Word Spacing = 0,00
                        ?? Line Spacing = 0,00
                        ?? Line Spacing Max = 0,00
                        ?? Paragraph Spacing = 0,00
                        ?? Char Width Max Adj = 0,00
                        ?? Text Wrapping Mode = Normal
                        ?? Word Wrapping Ratios = 0,40
                        ?? Overflow Mode = Overflow
                        ?? Linked Text Component: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Parent Linked Component: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Enable Kerning = False
                        ?? Enable Extra Padding = False
                        ?? Check Padding Required = False
                        ?? Is Rich Text = True
                        ?? Emoji Fallback Support = True
                        ?? Parse Ctrl Characters = True
                        ?? Is Orthographic = True
                        ?? Is Culling Enabled = False
                        ?? Horizontal Mapping = Character
                        ?? Vertical Mapping = Character
                        ?? Uv Line Offset = 0,00
                        ?? Geometry Sorting Order = Normal
                        ?? Is Text Object Scale Static = False
                        ?? Vertex Buffer Auto Size Reduction = False
                        ?? Use Max Visible Descender = True
                        ?? Page To Display = 1
                        ?? Is Using Legacy Animation Component = False
                        ?? Is Volumetric Text = False
                        ?? Has Font Asset Changed = False
                        ?? Base Material: **[NULL / НЕ НАЗНАЧЕНО]**
                     ??? **TMP SubMeshUI [LiberationSans SDF Material + LiberationSans SDF Atlas]** [Active] (Layer: UI)
                        • Component: `LayoutElement`
                           ?? Ignore Layout = True
                           ?? Min Width = -1,00
                           ?? Min Height = -1,00
                           ?? Preferred Width = -1,00
                           ?? Preferred Height = -1,00
                           ?? Flexible Width = -1,00
                           ?? Flexible Height = -1,00
                           ?? Layout Priority = 1
                        • Component: `TMP_SubMeshUI`
                           ?? Material: `LiberationSans SDF Material + LiberationSans SDF Atlas` (Material)
                           ?? Raycast Target = False
                           ?? Maskable = True
                           ?? Font Asset: `LiberationSans SDF - Fallback` (TMP_FontAsset)
                           ?? Sprite Asset: **[NULL / НЕ НАЗНАЧЕНО]**
                           ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
                           ?? Shared Material: `LiberationSans SDF Material + LiberationSans SDF Atlas` (Material)
                           ?? Is Default Material = False
                           ?? Padding = 1,25
                           ?? Text Component: `Text (TMP)` (TextMeshProUGUI)
                           ?? Material Reference Index = 1
               ??? **Building_02** [Active] (Layer: UI)
                  • Component: `BuildingTarget`
                     ?? Building Name = "Здание №2"
                     ?? Sector Code = "Сектор Д-6"
                     ?? Outline Object: `Outline Object` (GameObject)
                     ?? Button: `BuildingImage` (Button)
                  ??? **Outline Object** [Disabled] (Layer: UI)
                     • Component: `Image`
                        ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Raycast Target = True
                        ?? Maskable = True
                        ?? Sprite: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Type = Simple
                        ?? Preserve Aspect = False
                        ?? Fill Center = True
                        ?? Fill Method = Radial 360
                        ?? Fill Amount = 1,00
                        ?? Fill Clockwise = True
                        ?? Fill Origin = 0
                        ?? Use Sprite Mesh = False
                        ?? Pixels Per Unit Multiplier = 1,00
                  ??? **BuildingImage** [Active] (Layer: UI)
                     • Component: `Image`
                        ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Raycast Target = True
                        ?? Maskable = True
                        ?? Sprite: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Type = Simple
                        ?? Preserve Aspect = False
                        ?? Fill Center = True
                        ?? Fill Method = Radial 360
                        ?? Fill Amount = 1,00
                        ?? Fill Clockwise = True
                        ?? Fill Origin = 0
                        ?? Use Sprite Mesh = False
                        ?? Pixels Per Unit Multiplier = 1,00
                     • Component: `Button`
                        ?? Transition = Color Tint
                        ?? Interactable = True
                        ?? Target Graphic: `BuildingImage` (Image)
                  ??? **Text (TMP)** [Active] (Layer: UI)
                     • Component: `TextMeshProUGUI`
                        ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Raycast Target = False
                        ?? Maskable = True
                        ?? Text = "Здание №2"
                        ?? Is Right To Left = False
                        ?? Font Asset: `LiberationSans SDF` (TMP_FontAsset)
                        ?? Shared Material: `LiberationSans SDF Material` (Material)
                        ?? Font Material: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Enable Vertex Gradient = False
                        ?? Color Mode = Four Corners Gradient
                        ?? Font Color Gradient Preset: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Sprite Asset: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Tint All Sprites = False
                        ?? Style Sheet: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Text Style Hash Code = -1183493901
                        ?? Override Html Colors = False
                        ?? Font Size = 36,00
                        ?? Font Size Base = 36,00
                        ?? Font Weight = Regular
                        ?? Enable Auto Sizing = False
                        ?? Font Size Min = 18,00
                        ?? Font Size Max = 72,00
                        ?? Font Style = Normal
                        ?? Horizontal Alignment = Center
                        ?? Vertical Alignment = Middle
                        ?? Text Alignment = Converted
                        ?? Character Spacing = 0,00
                        ?? Character Horizontal Scale = 1,00
                        ?? Word Spacing = 0,00
                        ?? Line Spacing = 0,00
                        ?? Line Spacing Max = 0,00
                        ?? Paragraph Spacing = 0,00
                        ?? Char Width Max Adj = 0,00
                        ?? Text Wrapping Mode = Normal
                        ?? Word Wrapping Ratios = 0,40
                        ?? Overflow Mode = Overflow
                        ?? Linked Text Component: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Parent Linked Component: **[NULL / НЕ НАЗНАЧЕНО]**
                        ?? Enable Kerning = False
                        ?? Enable Extra Padding = False
                        ?? Check Padding Required = False
                        ?? Is Rich Text = True
                        ?? Emoji Fallback Support = True
                        ?? Parse Ctrl Characters = True
                        ?? Is Orthographic = True
                        ?? Is Culling Enabled = False
                        ?? Horizontal Mapping = Character
                        ?? Vertical Mapping = Character
                        ?? Uv Line Offset = 0,00
                        ?? Geometry Sorting Order = Normal
                        ?? Is Text Object Scale Static = False
                        ?? Vertex Buffer Auto Size Reduction = False
                        ?? Use Max Visible Descender = True
                        ?? Page To Display = 1
                        ?? Is Using Legacy Animation Component = False
                        ?? Is Volumetric Text = False
                        ?? Has Font Asset Changed = False
                        ?? Base Material: **[NULL / НЕ НАЗНАЧЕНО]**
                     ??? **TMP SubMeshUI [LiberationSans SDF Material + LiberationSans SDF Atlas]** [Active] (Layer: UI)
                        • Component: `LayoutElement`
                           ?? Ignore Layout = True
                           ?? Min Width = -1,00
                           ?? Min Height = -1,00
                           ?? Preferred Width = -1,00
                           ?? Preferred Height = -1,00
                           ?? Flexible Width = -1,00
                           ?? Flexible Height = -1,00
                           ?? Layout Priority = 1
                        • Component: `TMP_SubMeshUI`
                           ?? Material: `LiberationSans SDF Material + LiberationSans SDF Atlas` (Material)
                           ?? Raycast Target = False
                           ?? Maskable = True
                           ?? Font Asset: `LiberationSans SDF - Fallback` (TMP_FontAsset)
                           ?? Sprite Asset: **[NULL / НЕ НАЗНАЧЕНО]**
                           ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
                           ?? Shared Material: `LiberationSans SDF Material + LiberationSans SDF Atlas` (Material)
                           ?? Is Default Material = False
                           ?? Padding = 1,25
                           ?? Text Component: `Text (TMP)` (TextMeshProUGUI)
                           ?? Material Reference Index = 1
      ??? **ekran** [Active] (Layer: Default)
         • Component: `MeshFilter`
            ?? Mesh: `ekran` (Mesh)
         • Component: `MeshRenderer`
            ?? Cast Shadows = On
            ?? Receive Shadows = True
            ?? Dynamic Occludee = True
            ?? Static Shadow Caster = False
            ?? Motion Vectors = Per Object Motion
            ?? Light Probe Usage = 1
            ?? Reflection Probe Usage = 1
            ?? Ray Tracing Mode = 2
            ?? Ray Trace Procedural = False
            ?? Ray Tracing Accel Struct Build Flags Override = False
            ?? Ray Tracing Accel Struct Build Flags = 1
            ?? Small Mesh Culling = True
            ?? Force Mesh Lod = -1
            ?? Mesh Lod Selection Bias = 0,00
            ?? Rendering Layer Mask = 1
            ?? Renderer Priority = 0
            ?? Probe Anchor: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Light Probe Volume Override: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Scale In Lightmap = 1,00
            ?? Receive GI = 1
            ?? Preserve U Vs = False
            ?? Ignore Normals For Chart Detection = False
            ?? Important GI = False
            ?? Stitch Lightmap Seams = True
            ?? Minimum Chart Size = 4
            ?? Auto UV Max Distance = 0,50
            ?? Auto UV Max Angle = 89,00
            ?? Lightmap Parameters: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Mask Interaction = None
      ??? **knopka1** [Active] (Layer: Default)
         • Component: `MeshFilter`
            ?? Mesh: `knopka1` (Mesh)
         • Component: `MeshRenderer`
            ?? Cast Shadows = On
            ?? Receive Shadows = True
            ?? Dynamic Occludee = True
            ?? Static Shadow Caster = False
            ?? Motion Vectors = Per Object Motion
            ?? Light Probe Usage = 1
            ?? Reflection Probe Usage = 1
            ?? Ray Tracing Mode = 2
            ?? Ray Trace Procedural = False
            ?? Ray Tracing Accel Struct Build Flags Override = False
            ?? Ray Tracing Accel Struct Build Flags = 1
            ?? Small Mesh Culling = True
            ?? Force Mesh Lod = -1
            ?? Mesh Lod Selection Bias = 0,00
            ?? Rendering Layer Mask = 1
            ?? Renderer Priority = 0
            ?? Probe Anchor: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Light Probe Volume Override: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Scale In Lightmap = 1,00
            ?? Receive GI = 1
            ?? Preserve U Vs = False
            ?? Ignore Normals For Chart Detection = False
            ?? Important GI = False
            ?? Stitch Lightmap Seams = True
            ?? Minimum Chart Size = 4
            ?? Auto UV Max Distance = 0,50
            ?? Auto UV Max Angle = 89,00
            ?? Lightmap Parameters: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Mask Interaction = None
      ??? **knopka2** [Active] (Layer: Default)
         • Component: `MeshFilter`
            ?? Mesh: `knopka2` (Mesh)
         • Component: `MeshRenderer`
            ?? Cast Shadows = On
            ?? Receive Shadows = True
            ?? Dynamic Occludee = True
            ?? Static Shadow Caster = False
            ?? Motion Vectors = Per Object Motion
            ?? Light Probe Usage = 1
            ?? Reflection Probe Usage = 1
            ?? Ray Tracing Mode = 2
            ?? Ray Trace Procedural = False
            ?? Ray Tracing Accel Struct Build Flags Override = False
            ?? Ray Tracing Accel Struct Build Flags = 1
            ?? Small Mesh Culling = True
            ?? Force Mesh Lod = -1
            ?? Mesh Lod Selection Bias = 0,00
            ?? Rendering Layer Mask = 1
            ?? Renderer Priority = 0
            ?? Probe Anchor: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Light Probe Volume Override: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Scale In Lightmap = 1,00
            ?? Receive GI = 1
            ?? Preserve U Vs = False
            ?? Ignore Normals For Chart Detection = False
            ?? Important GI = False
            ?? Stitch Lightmap Seams = True
            ?? Minimum Chart Size = 4
            ?? Auto UV Max Distance = 0,50
            ?? Auto UV Max Angle = 89,00
            ?? Lightmap Parameters: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Mask Interaction = None
      ??? **knopka3** [Active] (Layer: Default)
         • Component: `MeshFilter`
            ?? Mesh: `knopka3` (Mesh)
         • Component: `MeshRenderer`
            ?? Cast Shadows = On
            ?? Receive Shadows = True
            ?? Dynamic Occludee = True
            ?? Static Shadow Caster = False
            ?? Motion Vectors = Per Object Motion
            ?? Light Probe Usage = 1
            ?? Reflection Probe Usage = 1
            ?? Ray Tracing Mode = 2
            ?? Ray Trace Procedural = False
            ?? Ray Tracing Accel Struct Build Flags Override = False
            ?? Ray Tracing Accel Struct Build Flags = 1
            ?? Small Mesh Culling = True
            ?? Force Mesh Lod = -1
            ?? Mesh Lod Selection Bias = 0,00
            ?? Rendering Layer Mask = 1
            ?? Renderer Priority = 0
            ?? Probe Anchor: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Light Probe Volume Override: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Scale In Lightmap = 1,00
            ?? Receive GI = 1
            ?? Preserve U Vs = False
            ?? Ignore Normals For Chart Detection = False
            ?? Important GI = False
            ?? Stitch Lightmap Seams = True
            ?? Minimum Chart Size = 4
            ?? Auto UV Max Distance = 0,50
            ?? Auto UV Max Angle = 89,00
            ?? Lightmap Parameters: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Mask Interaction = None
      ??? **knopka4** [Active] (Layer: Default)
         • Component: `MeshFilter`
            ?? Mesh: `knopka4` (Mesh)
         • Component: `MeshRenderer`
            ?? Cast Shadows = On
            ?? Receive Shadows = True
            ?? Dynamic Occludee = True
            ?? Static Shadow Caster = False
            ?? Motion Vectors = Per Object Motion
            ?? Light Probe Usage = 1
            ?? Reflection Probe Usage = 1
            ?? Ray Tracing Mode = 2
            ?? Ray Trace Procedural = False
            ?? Ray Tracing Accel Struct Build Flags Override = False
            ?? Ray Tracing Accel Struct Build Flags = 1
            ?? Small Mesh Culling = True
            ?? Force Mesh Lod = -1
            ?? Mesh Lod Selection Bias = 0,00
            ?? Rendering Layer Mask = 1
            ?? Renderer Priority = 0
            ?? Probe Anchor: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Light Probe Volume Override: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Scale In Lightmap = 1,00
            ?? Receive GI = 1
            ?? Preserve U Vs = False
            ?? Ignore Normals For Chart Detection = False
            ?? Important GI = False
            ?? Stitch Lightmap Seams = True
            ?? Minimum Chart Size = 4
            ?? Auto UV Max Distance = 0,50
            ?? Auto UV Max Angle = 89,00
            ?? Lightmap Parameters: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Mask Interaction = None
      ??? **noga1** [Active] (Layer: Default)
         • Component: `MeshFilter`
            ?? Mesh: `noga1` (Mesh)
         • Component: `MeshRenderer`
            ?? Cast Shadows = On
            ?? Receive Shadows = True
            ?? Dynamic Occludee = True
            ?? Static Shadow Caster = False
            ?? Motion Vectors = Per Object Motion
            ?? Light Probe Usage = 1
            ?? Reflection Probe Usage = 1
            ?? Ray Tracing Mode = 2
            ?? Ray Trace Procedural = False
            ?? Ray Tracing Accel Struct Build Flags Override = False
            ?? Ray Tracing Accel Struct Build Flags = 1
            ?? Small Mesh Culling = True
            ?? Force Mesh Lod = -1
            ?? Mesh Lod Selection Bias = 0,00
            ?? Rendering Layer Mask = 1
            ?? Renderer Priority = 0
            ?? Probe Anchor: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Light Probe Volume Override: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Scale In Lightmap = 1,00
            ?? Receive GI = 1
            ?? Preserve U Vs = False
            ?? Ignore Normals For Chart Detection = False
            ?? Important GI = False
            ?? Stitch Lightmap Seams = True
            ?? Minimum Chart Size = 4
            ?? Auto UV Max Distance = 0,50
            ?? Auto UV Max Angle = 89,00
            ?? Lightmap Parameters: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Mask Interaction = None
      ??? **noga2** [Active] (Layer: Default)
         • Component: `MeshFilter`
            ?? Mesh: `noga2` (Mesh)
         • Component: `MeshRenderer`
            ?? Cast Shadows = On
            ?? Receive Shadows = True
            ?? Dynamic Occludee = True
            ?? Static Shadow Caster = False
            ?? Motion Vectors = Per Object Motion
            ?? Light Probe Usage = 1
            ?? Reflection Probe Usage = 1
            ?? Ray Tracing Mode = 2
            ?? Ray Trace Procedural = False
            ?? Ray Tracing Accel Struct Build Flags Override = False
            ?? Ray Tracing Accel Struct Build Flags = 1
            ?? Small Mesh Culling = True
            ?? Force Mesh Lod = -1
            ?? Mesh Lod Selection Bias = 0,00
            ?? Rendering Layer Mask = 1
            ?? Renderer Priority = 0
            ?? Probe Anchor: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Light Probe Volume Override: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Scale In Lightmap = 1,00
            ?? Receive GI = 1
            ?? Preserve U Vs = False
            ?? Ignore Normals For Chart Detection = False
            ?? Important GI = False
            ?? Stitch Lightmap Seams = True
            ?? Minimum Chart Size = 4
            ?? Auto UV Max Distance = 0,50
            ?? Auto UV Max Angle = 89,00
            ?? Lightmap Parameters: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Mask Interaction = None
      ??? **noga3** [Active] (Layer: Default)
         • Component: `MeshFilter`
            ?? Mesh: `noga3` (Mesh)
         • Component: `MeshRenderer`
            ?? Cast Shadows = On
            ?? Receive Shadows = True
            ?? Dynamic Occludee = True
            ?? Static Shadow Caster = False
            ?? Motion Vectors = Per Object Motion
            ?? Light Probe Usage = 1
            ?? Reflection Probe Usage = 1
            ?? Ray Tracing Mode = 2
            ?? Ray Trace Procedural = False
            ?? Ray Tracing Accel Struct Build Flags Override = False
            ?? Ray Tracing Accel Struct Build Flags = 1
            ?? Small Mesh Culling = True
            ?? Force Mesh Lod = -1
            ?? Mesh Lod Selection Bias = 0,00
            ?? Rendering Layer Mask = 1
            ?? Renderer Priority = 0
            ?? Probe Anchor: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Light Probe Volume Override: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Scale In Lightmap = 1,00
            ?? Receive GI = 1
            ?? Preserve U Vs = False
            ?? Ignore Normals For Chart Detection = False
            ?? Important GI = False
            ?? Stitch Lightmap Seams = True
            ?? Minimum Chart Size = 4
            ?? Auto UV Max Distance = 0,50
            ?? Auto UV Max Angle = 89,00
            ?? Lightmap Parameters: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Mask Interaction = None
      ??? **noga4** [Active] (Layer: Default)
         • Component: `MeshFilter`
            ?? Mesh: `noga4` (Mesh)
         • Component: `MeshRenderer`
            ?? Cast Shadows = On
            ?? Receive Shadows = True
            ?? Dynamic Occludee = True
            ?? Static Shadow Caster = False
            ?? Motion Vectors = Per Object Motion
            ?? Light Probe Usage = 1
            ?? Reflection Probe Usage = 1
            ?? Ray Tracing Mode = 2
            ?? Ray Trace Procedural = False
            ?? Ray Tracing Accel Struct Build Flags Override = False
            ?? Ray Tracing Accel Struct Build Flags = 1
            ?? Small Mesh Culling = True
            ?? Force Mesh Lod = -1
            ?? Mesh Lod Selection Bias = 0,00
            ?? Rendering Layer Mask = 1
            ?? Renderer Priority = 0
            ?? Probe Anchor: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Light Probe Volume Override: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Scale In Lightmap = 1,00
            ?? Receive GI = 1
            ?? Preserve U Vs = False
            ?? Ignore Normals For Chart Detection = False
            ?? Important GI = False
            ?? Stitch Lightmap Seams = True
            ?? Minimum Chart Size = 4
            ?? Auto UV Max Distance = 0,50
            ?? Auto UV Max Angle = 89,00
            ?? Lightmap Parameters: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Mask Interaction = None
      ??? **ruchka** [Active] (Layer: Default)
         • Component: `MeshFilter`
            ?? Mesh: `ruchka` (Mesh)
         • Component: `MeshRenderer`
            ?? Cast Shadows = On
            ?? Receive Shadows = True
            ?? Dynamic Occludee = True
            ?? Static Shadow Caster = False
            ?? Motion Vectors = Per Object Motion
            ?? Light Probe Usage = 1
            ?? Reflection Probe Usage = 1
            ?? Ray Tracing Mode = 2
            ?? Ray Trace Procedural = False
            ?? Ray Tracing Accel Struct Build Flags Override = False
            ?? Ray Tracing Accel Struct Build Flags = 1
            ?? Small Mesh Culling = True
            ?? Force Mesh Lod = -1
            ?? Mesh Lod Selection Bias = 0,00
            ?? Rendering Layer Mask = 1
            ?? Renderer Priority = 0
            ?? Probe Anchor: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Light Probe Volume Override: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Scale In Lightmap = 1,00
            ?? Receive GI = 1
            ?? Preserve U Vs = False
            ?? Ignore Normals For Chart Detection = False
            ?? Important GI = False
            ?? Stitch Lightmap Seams = True
            ?? Minimum Chart Size = 4
            ?? Auto UV Max Distance = 0,50
            ?? Auto UV Max Angle = 89,00
            ?? Lightmap Parameters: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Mask Interaction = None
      ??? **telik** [Active] (Layer: Default)
         • Component: `MeshFilter`
            ?? Mesh: `telik` (Mesh)
         • Component: `MeshRenderer`
            ?? Cast Shadows = On
            ?? Receive Shadows = True
            ?? Dynamic Occludee = True
            ?? Static Shadow Caster = False
            ?? Motion Vectors = Per Object Motion
            ?? Light Probe Usage = 1
            ?? Reflection Probe Usage = 1
            ?? Ray Tracing Mode = 2
            ?? Ray Trace Procedural = False
            ?? Ray Tracing Accel Struct Build Flags Override = False
            ?? Ray Tracing Accel Struct Build Flags = 1
            ?? Small Mesh Culling = True
            ?? Force Mesh Lod = -1
            ?? Mesh Lod Selection Bias = 0,00
            ?? Rendering Layer Mask = 1
            ?? Renderer Priority = 0
            ?? Probe Anchor: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Light Probe Volume Override: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Scale In Lightmap = 1,00
            ?? Receive GI = 1
            ?? Preserve U Vs = False
            ?? Ignore Normals For Chart Detection = False
            ?? Important GI = False
            ?? Stitch Lightmap Seams = True
            ?? Minimum Chart Size = 4
            ?? Auto UV Max Distance = 0,50
            ?? Auto UV Max Angle = 89,00
            ?? Lightmap Parameters: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Mask Interaction = None
   ??? **Door** [Active] (Layer: Default)
      • Component: `MeshFilter`
         ?? Mesh: `Cube` (Mesh)
      • Component: `MeshRenderer`
         ?? Cast Shadows = On
         ?? Receive Shadows = True
         ?? Dynamic Occludee = True
         ?? Static Shadow Caster = False
         ?? Motion Vectors = Per Object Motion
         ?? Light Probe Usage = 1
         ?? Reflection Probe Usage = 1
         ?? Ray Tracing Mode = 2
         ?? Ray Trace Procedural = False
         ?? Ray Tracing Accel Struct Build Flags Override = False
         ?? Ray Tracing Accel Struct Build Flags = 1
         ?? Small Mesh Culling = True
         ?? Force Mesh Lod = -1
         ?? Mesh Lod Selection Bias = 0,00
         ?? Rendering Layer Mask = 1
         ?? Renderer Priority = 0
         ?? Probe Anchor: **[NULL / НЕ НАЗНАЧЕНО]**
         ?? Light Probe Volume Override: **[NULL / НЕ НАЗНАЧЕНО]**
         ?? Scale In Lightmap = 1,00
         ?? Receive GI = 1
         ?? Preserve U Vs = False
         ?? Ignore Normals For Chart Detection = False
         ?? Important GI = False
         ?? Stitch Lightmap Seams = True
         ?? Minimum Chart Size = 4
         ?? Auto UV Max Distance = 0,50
         ?? Auto UV Max Angle = 89,00
         ?? Lightmap Parameters: **[NULL / НЕ НАЗНАЧЕНО]**
         ?? Mask Interaction = None
      • Component: `BoxCollider`
         ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
         ?? Layer Override Priority = 0
         ?? Is Trigger = False
         ?? Provides Contacts = False
      ??? **Cube** [Active] (Layer: Default)
         • Component: `MeshFilter`
            ?? Mesh: `Cube` (Mesh)
         • Component: `MeshRenderer`
            ?? Cast Shadows = On
            ?? Receive Shadows = True
            ?? Dynamic Occludee = True
            ?? Static Shadow Caster = False
            ?? Motion Vectors = Per Object Motion
            ?? Light Probe Usage = 1
            ?? Reflection Probe Usage = 1
            ?? Ray Tracing Mode = 2
            ?? Ray Trace Procedural = False
            ?? Ray Tracing Accel Struct Build Flags Override = False
            ?? Ray Tracing Accel Struct Build Flags = 1
            ?? Small Mesh Culling = True
            ?? Force Mesh Lod = -1
            ?? Mesh Lod Selection Bias = 0,00
            ?? Rendering Layer Mask = 1
            ?? Renderer Priority = 0
            ?? Probe Anchor: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Light Probe Volume Override: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Scale In Lightmap = 1,00
            ?? Receive GI = 1
            ?? Preserve U Vs = False
            ?? Ignore Normals For Chart Detection = False
            ?? Important GI = False
            ?? Stitch Lightmap Seams = True
            ?? Minimum Chart Size = 4
            ?? Auto UV Max Distance = 0,50
            ?? Auto UV Max Angle = 89,00
            ?? Lightmap Parameters: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Mask Interaction = None
         • Component: `BoxCollider`
            ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Layer Override Priority = 0
            ?? Is Trigger = False
            ?? Provides Contacts = False
   ??? **CameraPivot** [Active] (Layer: Default)
      • Component: `RoomViewController`
         ?? Current Index = 1
         ?? Rotation Duration = 0,35
         ?? Is Cyclic = True
      ??? **Main Camera** [Active] (Layer: Default)
         • Component: `Camera`
            ?? Clear Flags = Skybox
            ?? Projection Matrix Mode = 1
            ?? Gate Fit Mode = 2
            ?? FOV Axis Mode = 0
            ?? Iso = 200
            ?? Shutter Speed = 0,01
            ?? Aperture = 16,00
            ?? Focus Distance = 10,00
            ?? Focal Length = 50,00
            ?? Blade Count = 5
            ?? Barrel Clipping = 0,25
            ?? Anamorphism = 0,00
            ?? Near clip plane = 0,30
            ?? Far clip plane = 1000,00
            ?? Field of view = 60,00
            ?? Orthographic = False
            ?? Orthographic size = 5,00
            ?? Depth = -1,00
            ?? Rendering Path = Use Graphics Settings
            ?? Target Texture: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Target Display = 0
            ?? Target Eye = 3
            ?? HDR = True
            ?? Allow MSAA = True
            ?? Allow Dynamic Resolution = False
            ?? Force Into RT = False
            ?? Occlusion Culling = True
            ?? Stereo Convergence = 10,00
            ?? Stereo Separation = 0,02
         • Component: `AudioListener`
         • Component: `UniversalAdditionalCameraData`
            ?? Render Shadows = True
            ?? Requires Depth Texture Option = Use settings from Render Pipeline Asset
            ?? Requires Opaque Texture Option = Use settings from Render Pipeline Asset
            ?? Camera Type = Base
            ?? Renderer Index = -1
            ?? Volume Trigger: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Volume Framework Update Mode Option = Use Pipeline Settings
            ?? Render Post Processing = True
            ?? Antialiasing = No Anti-aliasing
            ?? Antialiasing Quality = High
            ?? Stop Na N = False
            ?? Dithering = False
            ?? Clear Depth = True
            ?? Allow XR Rendering = True
            ?? Allow HDR Output = True
            ?? Use Screen Coord Override = False
            ?? Requires Depth Texture = False
            ?? Requires Color Texture = False
            ?? Version = Depth And Opaque Texture Options
? **Canvas** [Active] (Layer: UI)
   • Component: `Canvas`
      ?? Render Mode = Screen Space - Overlay
      ?? Camera: **[NULL / НЕ НАЗНАЧЕНО]**
      ?? Plane Distance = 100,00
      ?? Pixel Perfect = False
      ?? Receives Events = True
      ?? Override Sorting = False
      ?? Override Pixel Perfect = False
      ?? Sorting Bucket Normalized Size = 0,00
      ?? Vertex Color Always Gamma Space = False
      ?? Additional Shader Channels Flag = -1
      ?? Update Rect Transform For Standalone = Enabled
   • Component: `CanvasScaler`
      ?? Ui Scale Mode = Scale With Screen Size
      ?? Reference Pixels Per Unit = 100,00
      ?? Scale Factor = 1,00
      ?? Screen Match Mode = Match Width Or Height
      ?? Match Width Or Height = 0,50
      ?? Physical Unit = Points
      ?? Fallback Screen DPI = 96,00
      ?? Default Sprite DPI = 96,00
      ?? Dynamic Pixels Per Unit = 1,00
      ?? Preset Info Is World = False
   • Component: `GraphicRaycaster`
      ?? Ignore Reversed Graphics = True
      ?? Blocking Objects = None
   ??? **Dialogue** [Active] (Layer: UI)
      • Component: `DialogueUI`
         ?? Root Panel: `Dialogue` (GameObject)
         ?? Speaker Text: `Text (TMP)` (TextMeshProUGUI)
         ?? Body Text: `Text (TMP) (1)` (TextMeshProUGUI)
         ?? Choices Container: `Panel` (RectTransform)
         ?? Choice Button Prefab: `Button` (Button)
         ?? Chars Per Second = 45,00
         ?? Delay Before Choices = 0,25
      ??? **Panel** [Active] (Layer: UI)
         • Component: `Image`
            ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Raycast Target = True
            ?? Maskable = True
            ?? Sprite: `Background` (Sprite)
            ?? Type = Sliced
            ?? Preserve Aspect = False
            ?? Fill Center = True
            ?? Fill Method = Radial 360
            ?? Fill Amount = 1,00
            ?? Fill Clockwise = True
            ?? Fill Origin = 0
            ?? Use Sprite Mesh = False
            ?? Pixels Per Unit Multiplier = 1,00
         ??? **Text (TMP)** [Active] (Layer: UI)
            • Component: `TextMeshProUGUI`
               ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
               ?? Raycast Target = True
               ?? Maskable = True
               ?? Text = "New Text"
               ?? Is Right To Left = False
               ?? Font Asset: `LiberationSans SDF` (TMP_FontAsset)
               ?? Shared Material: `LiberationSans SDF Material` (Material)
               ?? Font Material: **[NULL / НЕ НАЗНАЧЕНО]**
               ?? Enable Vertex Gradient = False
               ?? Color Mode = Four Corners Gradient
               ?? Font Color Gradient Preset: **[NULL / НЕ НАЗНАЧЕНО]**
               ?? Sprite Asset: **[NULL / НЕ НАЗНАЧЕНО]**
               ?? Tint All Sprites = False
               ?? Style Sheet: **[NULL / НЕ НАЗНАЧЕНО]**
               ?? Text Style Hash Code = -1183493901
               ?? Override Html Colors = False
               ?? Font Size = 44,30
               ?? Font Size Base = 44,30
               ?? Font Weight = Regular
               ?? Enable Auto Sizing = False
               ?? Font Size Min = 18,00
               ?? Font Size Max = 72,00
               ?? Font Style = Normal
               ?? Horizontal Alignment = Center
               ?? Vertical Alignment = Middle
               ?? Text Alignment = Converted
               ?? Character Spacing = 0,00
               ?? Character Horizontal Scale = 1,00
               ?? Word Spacing = 0,00
               ?? Line Spacing = 0,00
               ?? Line Spacing Max = 0,00
               ?? Paragraph Spacing = 0,00
               ?? Char Width Max Adj = 0,00
               ?? Text Wrapping Mode = Normal
               ?? Word Wrapping Ratios = 0,40
               ?? Overflow Mode = Overflow
               ?? Linked Text Component: **[NULL / НЕ НАЗНАЧЕНО]**
               ?? Parent Linked Component: **[NULL / НЕ НАЗНАЧЕНО]**
               ?? Enable Kerning = False
               ?? Enable Extra Padding = False
               ?? Check Padding Required = False
               ?? Is Rich Text = True
               ?? Emoji Fallback Support = True
               ?? Parse Ctrl Characters = True
               ?? Is Orthographic = True
               ?? Is Culling Enabled = False
               ?? Horizontal Mapping = Character
               ?? Vertical Mapping = Character
               ?? Uv Line Offset = 0,00
               ?? Geometry Sorting Order = Normal
               ?? Is Text Object Scale Static = False
               ?? Vertex Buffer Auto Size Reduction = False
               ?? Use Max Visible Descender = True
               ?? Page To Display = 1
               ?? Is Using Legacy Animation Component = False
               ?? Is Volumetric Text = False
               ?? Has Font Asset Changed = False
               ?? Base Material: **[NULL / НЕ НАЗНАЧЕНО]**
         ??? **Text (TMP) (1)** [Active] (Layer: UI)
            • Component: `TextMeshProUGUI`
               ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
               ?? Raycast Target = True
               ?? Maskable = True
               ?? Text = "New Text"
               ?? Is Right To Left = False
               ?? Font Asset: `LiberationSans SDF` (TMP_FontAsset)
               ?? Shared Material: `LiberationSans SDF Material` (Material)
               ?? Font Material: **[NULL / НЕ НАЗНАЧЕНО]**
               ?? Enable Vertex Gradient = False
               ?? Color Mode = Four Corners Gradient
               ?? Font Color Gradient Preset: **[NULL / НЕ НАЗНАЧЕНО]**
               ?? Sprite Asset: **[NULL / НЕ НАЗНАЧЕНО]**
               ?? Tint All Sprites = False
               ?? Style Sheet: **[NULL / НЕ НАЗНАЧЕНО]**
               ?? Text Style Hash Code = -1183493901
               ?? Override Html Colors = False
               ?? Font Size = 36,63
               ?? Font Size Base = 36,63
               ?? Font Weight = Regular
               ?? Enable Auto Sizing = False
               ?? Font Size Min = 18,00
               ?? Font Size Max = 72,00
               ?? Font Style = Normal
               ?? Horizontal Alignment = Center
               ?? Vertical Alignment = Middle
               ?? Text Alignment = Converted
               ?? Character Spacing = 0,00
               ?? Character Horizontal Scale = 1,00
               ?? Word Spacing = 0,00
               ?? Line Spacing = 0,00
               ?? Line Spacing Max = 0,00
               ?? Paragraph Spacing = 0,00
               ?? Char Width Max Adj = 0,00
               ?? Text Wrapping Mode = Normal
               ?? Word Wrapping Ratios = 0,40
               ?? Overflow Mode = Overflow
               ?? Linked Text Component: **[NULL / НЕ НАЗНАЧЕНО]**
               ?? Parent Linked Component: **[NULL / НЕ НАЗНАЧЕНО]**
               ?? Enable Kerning = False
               ?? Enable Extra Padding = False
               ?? Check Padding Required = False
               ?? Is Rich Text = True
               ?? Emoji Fallback Support = True
               ?? Parse Ctrl Characters = True
               ?? Is Orthographic = True
               ?? Is Culling Enabled = False
               ?? Horizontal Mapping = Character
               ?? Vertical Mapping = Character
               ?? Uv Line Offset = 0,00
               ?? Geometry Sorting Order = Normal
               ?? Is Text Object Scale Static = False
               ?? Vertex Buffer Auto Size Reduction = False
               ?? Use Max Visible Descender = True
               ?? Page To Display = 1
               ?? Is Using Legacy Animation Component = False
               ?? Is Volumetric Text = False
               ?? Has Font Asset Changed = False
               ?? Base Material: **[NULL / НЕ НАЗНАЧЕНО]**
      ??? **Panel** [Active] (Layer: UI)
         • Component: `Image`
            ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Raycast Target = True
            ?? Maskable = True
            ?? Sprite: `Background` (Sprite)
            ?? Type = Sliced
            ?? Preserve Aspect = False
            ?? Fill Center = True
            ?? Fill Method = Radial 360
            ?? Fill Amount = 1,00
            ?? Fill Clockwise = True
            ?? Fill Origin = 0
            ?? Use Sprite Mesh = False
            ?? Pixels Per Unit Multiplier = 1,00
         • Component: `VerticalLayoutGroup`
            ?? Child Alignment = Middle Center
            ?? Spacing = 0,00
            ?? Child Force Expand Width = True
            ?? Child Force Expand Height = True
            ?? Child Control Width = False
            ?? Child Control Height = False
            ?? Child Scale Width = False
            ?? Child Scale Height = False
            ?? Reverse Arrangement = False
         • Component: `ContentSizeFitter`
            ?? Horizontal Fit = Unconstrained
            ?? Vertical Fit = Unconstrained
   ??? **RotateLeftButton** [Active] (Layer: UI)
      • Component: `Image`
         ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
         ?? Raycast Target = True
         ?? Maskable = True
         ?? Sprite: `UISprite` (Sprite)
         ?? Type = Sliced
         ?? Preserve Aspect = False
         ?? Fill Center = True
         ?? Fill Method = Radial 360
         ?? Fill Amount = 1,00
         ?? Fill Clockwise = True
         ?? Fill Origin = 0
         ?? Use Sprite Mesh = False
         ?? Pixels Per Unit Multiplier = 1,00
      • Component: `Button`
         ?? Transition = Color Tint
         ?? Interactable = True
         ?? Target Graphic: `RotateLeftButton` (Image)
      ??? **Text (TMP)** [Active] (Layer: UI)
         • Component: `TextMeshProUGUI`
            ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Raycast Target = True
            ?? Maskable = True
            ?? Text = "Left"
            ?? Is Right To Left = False
            ?? Font Asset: `LiberationSans SDF` (TMP_FontAsset)
            ?? Shared Material: `LiberationSans SDF Material` (Material)
            ?? Font Material: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Enable Vertex Gradient = False
            ?? Color Mode = Four Corners Gradient
            ?? Font Color Gradient Preset: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Sprite Asset: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Tint All Sprites = False
            ?? Style Sheet: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Text Style Hash Code = -1183493901
            ?? Override Html Colors = False
            ?? Font Size = 50,00
            ?? Font Size Base = 50,00
            ?? Font Weight = Regular
            ?? Enable Auto Sizing = False
            ?? Font Size Min = 0,00
            ?? Font Size Max = 0,00
            ?? Font Style = Normal
            ?? Horizontal Alignment = Center
            ?? Vertical Alignment = Middle
            ?? Text Alignment = Converted
            ?? Character Spacing = 0,00
            ?? Character Horizontal Scale = 1,00
            ?? Word Spacing = 0,00
            ?? Line Spacing = 0,00
            ?? Line Spacing Max = 0,00
            ?? Paragraph Spacing = 0,00
            ?? Char Width Max Adj = 0,00
            ?? Text Wrapping Mode = No Wrap
            ?? Word Wrapping Ratios = 0,40
            ?? Overflow Mode = Overflow
            ?? Linked Text Component: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Parent Linked Component: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Enable Kerning = False
            ?? Enable Extra Padding = False
            ?? Check Padding Required = False
            ?? Is Rich Text = True
            ?? Emoji Fallback Support = True
            ?? Parse Ctrl Characters = True
            ?? Is Orthographic = True
            ?? Is Culling Enabled = False
            ?? Horizontal Mapping = Character
            ?? Vertical Mapping = Character
            ?? Uv Line Offset = 0,00
            ?? Geometry Sorting Order = Normal
            ?? Is Text Object Scale Static = False
            ?? Vertex Buffer Auto Size Reduction = False
            ?? Use Max Visible Descender = True
            ?? Page To Display = 1
            ?? Is Using Legacy Animation Component = False
            ?? Is Volumetric Text = False
            ?? Has Font Asset Changed = False
            ?? Base Material: **[NULL / НЕ НАЗНАЧЕНО]**
   ??? **RotateRightButton** [Active] (Layer: UI)
      • Component: `Image`
         ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
         ?? Raycast Target = True
         ?? Maskable = True
         ?? Sprite: `UISprite` (Sprite)
         ?? Type = Sliced
         ?? Preserve Aspect = False
         ?? Fill Center = True
         ?? Fill Method = Radial 360
         ?? Fill Amount = 1,00
         ?? Fill Clockwise = True
         ?? Fill Origin = 0
         ?? Use Sprite Mesh = False
         ?? Pixels Per Unit Multiplier = 1,00
      • Component: `Button`
         ?? Transition = Color Tint
         ?? Interactable = True
         ?? Target Graphic: `RotateRightButton` (Image)
      ??? **Text (TMP)** [Active] (Layer: UI)
         • Component: `TextMeshProUGUI`
            ?? Material: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Raycast Target = True
            ?? Maskable = True
            ?? Text = "Right"
            ?? Is Right To Left = False
            ?? Font Asset: `LiberationSans SDF` (TMP_FontAsset)
            ?? Shared Material: `LiberationSans SDF Material` (Material)
            ?? Font Material: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Enable Vertex Gradient = False
            ?? Color Mode = Four Corners Gradient
            ?? Font Color Gradient Preset: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Sprite Asset: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Tint All Sprites = False
            ?? Style Sheet: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Text Style Hash Code = -1183493901
            ?? Override Html Colors = False
            ?? Font Size = 50,00
            ?? Font Size Base = 50,00
            ?? Font Weight = Regular
            ?? Enable Auto Sizing = False
            ?? Font Size Min = 0,00
            ?? Font Size Max = 0,00
            ?? Font Style = Normal
            ?? Horizontal Alignment = Center
            ?? Vertical Alignment = Middle
            ?? Text Alignment = Converted
            ?? Character Spacing = 0,00
            ?? Character Horizontal Scale = 1,00
            ?? Word Spacing = 0,00
            ?? Line Spacing = 0,00
            ?? Line Spacing Max = 0,00
            ?? Paragraph Spacing = 0,00
            ?? Char Width Max Adj = 0,00
            ?? Text Wrapping Mode = No Wrap
            ?? Word Wrapping Ratios = 0,40
            ?? Overflow Mode = Overflow
            ?? Linked Text Component: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Parent Linked Component: **[NULL / НЕ НАЗНАЧЕНО]**
            ?? Enable Kerning = False
            ?? Enable Extra Padding = False
            ?? Check Padding Required = False
            ?? Is Rich Text = True
            ?? Emoji Fallback Support = True
            ?? Parse Ctrl Characters = True
            ?? Is Orthographic = True
            ?? Is Culling Enabled = False
            ?? Horizontal Mapping = Character
            ?? Vertical Mapping = Character
            ?? Uv Line Offset = 0,00
            ?? Geometry Sorting Order = Normal
            ?? Is Text Object Scale Static = False
            ?? Vertex Buffer Auto Size Reduction = False
            ?? Use Max Visible Descender = True
            ?? Page To Display = 1
            ?? Is Using Legacy Animation Component = False
            ?? Is Volumetric Text = False
            ?? Has Font Asset Changed = False
            ?? Base Material: **[NULL / НЕ НАЗНАЧЕНО]**
? **EventSystem** [Active] (Layer: Default)
   • Component: `EventSystem`
      ?? First Selected: **[NULL / НЕ НАЗНАЧЕНО]**
      ?? Send Navigation Events = True
      ?? Drag Threshold = 10
   • Component: `InputSystemUIInputModule`
      ?? Send Pointer Hover To Parent = True
      ?? Move Repeat Delay = 0,50
      ?? Move Repeat Rate = 0,10
      ?? XR Tracking Origin: **[NULL / НЕ НАЗНАЧЕНО]**
      ?? Deselect On Background Click = True
      ?? Pointer Behavior = Single Mouse Or Pen But Multi Touch And Track
      ?? Scroll Delta Per Tick = 6,00

======================================================================
## 4. СВОДКА ДИАГНОСТИКИ
• Экспортировано C# скриптов: 13
• Экспортировано ScriptableObject конфигов: 11
• Всего объектов на сцене: 125
• Ошибок Missing Script: 0
• Пустых ссылок [NULL]: 361
======================================================================
