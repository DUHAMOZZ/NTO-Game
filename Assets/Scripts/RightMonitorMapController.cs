using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RightMonitorMapController : MonoBehaviour
{
    [Header("Точки интерактивной карты")]
    [SerializeField] private CameraPoint[] _cameraPoints;
    [SerializeField] private BuildingTarget[] _buildingTargets;

    [Header("Левая часть: панель снимка")]
    [Tooltip("CanvasGroup кнопки снимка для управления прозрачностью")]
    [SerializeField] private CanvasGroup _snapshotBtnCanvasGroup;
    [SerializeField] private Button _takeSnapshotButton;
    [SerializeField] private Image _snapshotDisplayImage;
    [SerializeField] private GameObject _noSignalPlaceholder;
    [SerializeField] private Image _screenFlashImage;
    [SerializeField] private TextMeshProUGUI _snapshotDescriptionText;

    [Header("Приближение снимка (опционально)")]
    [SerializeField] private Button _zoomToggleButton;
    [SerializeField] private TextMeshProUGUI _zoomButtonText;
    [SerializeField] private float _zoomedScale = 2f;

    [Header("Информационные статусы (UI TextMeshPro)")]
    [SerializeField] private TextMeshProUGUI _cameraStatusText;
    [SerializeField] private TextMeshProUGUI _targetBuildingText;

    public CameraPoint CurrentCamera { get; private set; }
    public BuildingTarget CurrentBuilding { get; private set; }

    public event Action<BuildingTarget> OnBuildingSelected;
    public event Action<CameraPoint> OnCameraSelected;

    private string _activeClueCameraId;
    private Sprite _activeClueSprite;
    private string _activeClueDescription;
    private float _secondSnapshotDelay;
    private Sprite _secondClueSprite;
    private string _secondClueDescription;

    private float _firstClueSnapTime = -1f;
    private bool _hasSnappedClueOnce;
    private bool _isZoomed;
    private Coroutine _flashCoroutine;

    private void Awake()
    {
        if (_cameraPoints != null)
        {
            for (int i = 0; i < _cameraPoints.Length; i++)
            {
                if (_cameraPoints[i] != null) _cameraPoints[i].Initialize(this);
            }
        }

        if (_buildingTargets != null)
        {
            for (int i = 0; i < _buildingTargets.Length; i++)
            {
                if (_buildingTargets[i] != null) _buildingTargets[i].Initialize(this);
            }
        }

        if (_takeSnapshotButton != null)
        {
            _takeSnapshotButton.onClick.RemoveListener(OnTakeSnapshotClicked);
            _takeSnapshotButton.onClick.AddListener(OnTakeSnapshotClicked);
        }

        if (_zoomToggleButton != null)
        {
            _zoomToggleButton.onClick.RemoveListener(ToggleZoom);
            _zoomToggleButton.onClick.AddListener(ToggleZoom);
        }

        ClearForNewScenario();
    }

    public void SetScenarioClue(ScenarioConfig scenario)
    {
        _firstClueSnapTime = -1f;
        _hasSnappedClueOnce = false;

        if (scenario == null)
        {
            _activeClueCameraId = null;
            _activeClueSprite = null;
            _activeClueDescription = null;
            _secondSnapshotDelay = 0f;
            _secondClueSprite = null;
            _secondClueDescription = null;
            return;
        }

        _activeClueCameraId = scenario.clueCameraId;
        _activeClueSprite = scenario.clueSnapshot;
        _activeClueDescription = scenario.clueDescription;
        _secondSnapshotDelay = scenario.secondSnapshotDelay;
        _secondClueSprite = scenario.secondClueSnapshot;
        _secondClueDescription = scenario.secondClueDescription;
    }

    public void ClearForNewScenario()
    {
        CurrentCamera = null;
        CurrentBuilding = null;
        _firstClueSnapTime = -1f;
        _hasSnappedClueOnce = false;

        if (_cameraPoints != null)
        {
            for (int i = 0; i < _cameraPoints.Length; i++)
            {
                if (_cameraPoints[i] != null) _cameraPoints[i].SetSelected(false);
            }
        }

        if (_buildingTargets != null)
        {
            for (int i = 0; i < _buildingTargets.Length; i++)
            {
                if (_buildingTargets[i] != null) _buildingTargets[i].SetSelected(false);
            }
        }

        SetSnapshotButtonActive(false);
        SetZoomState(false);

        if (_snapshotDisplayImage != null) _snapshotDisplayImage.sprite = null;
        if (_noSignalPlaceholder != null) _noSignalPlaceholder.SetActive(true);
        if (_screenFlashImage != null) _screenFlashImage.gameObject.SetActive(false);
        if (_snapshotDescriptionText != null) _snapshotDescriptionText.text = "Выберите камеру на карте и сделайте снимок.";

        UpdateStatusText();
        OnBuildingSelected?.Invoke(null);
    }

    public void SelectCamera(CameraPoint selectedCam)
    {
        CurrentCamera = selectedCam;

        if (_cameraPoints != null)
        {
            for (int i = 0; i < _cameraPoints.Length; i++)
            {
                if (_cameraPoints[i] != null)
                {
                    _cameraPoints[i].SetSelected(_cameraPoints[i] == selectedCam);
                }
            }
        }

        SetSnapshotButtonActive(selectedCam != null);
        UpdateStatusText();
        OnCameraSelected?.Invoke(selectedCam);
    }

    public void SelectBuilding(BuildingTarget selectedBuilding)
    {
        CurrentBuilding = selectedBuilding;

        if (_buildingTargets != null)
        {
            for (int i = 0; i < _buildingTargets.Length; i++)
            {
                if (_buildingTargets[i] != null)
                {
                    _buildingTargets[i].SetSelected(_buildingTargets[i] == selectedBuilding);
                }
            }
        }

        UpdateStatusText();
        OnBuildingSelected?.Invoke(selectedBuilding);
    }

    private void OnTakeSnapshotClicked()
    {
        if (CurrentCamera == null) return;

        bool isClueCamera = !string.IsNullOrEmpty(_activeClueCameraId) &&
                            string.Equals(CurrentCamera.CameraId, _activeClueCameraId, StringComparison.OrdinalIgnoreCase);

        Sprite chosenSprite = CurrentCamera.snapshotSprite;
        string descriptionText = "На участке всё спокойно. Подозрительных объектов не обнаружено.";

        if (isClueCamera)
        {
            if (_activeClueSprite != null) chosenSprite = _activeClueSprite;
            descriptionText = _activeClueDescription;

            if (_secondSnapshotDelay > 0f && !string.IsNullOrEmpty(_secondClueDescription))
            {
                if (!_hasSnappedClueOnce)
                {
                    _hasSnappedClueOnce = true;
                    _firstClueSnapTime = Time.time;
                }
                else
                {
                    float elapsedSinceFirst = Time.time - _firstClueSnapTime;
                    if (elapsedSinceFirst >= _secondSnapshotDelay)
                    {
                        if (_secondClueSprite != null) chosenSprite = _secondClueSprite;
                        descriptionText = _secondClueDescription;
                    }
                    else
                    {
                        int left = Mathf.CeilToInt(_secondSnapshotDelay - elapsedSinceFirst);
                        descriptionText = $"{_activeClueDescription}\n(Повторите снимок через {left} сек. для проверки движения)";
                    }
                }
            }
        }

        if (_snapshotDisplayImage != null && chosenSprite != null)
        {
            _snapshotDisplayImage.sprite = chosenSprite;
        }

        if (_snapshotDescriptionText != null)
        {
            _snapshotDescriptionText.text = descriptionText;
        }

        if (_noSignalPlaceholder != null)
        {
            _noSignalPlaceholder.SetActive(false);
        }

        SetZoomState(false);

        if (_screenFlashImage != null)
        {
            if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);
            _flashCoroutine = StartCoroutine(FlashRoutine());
        }
    }

    private void ToggleZoom()
    {
        if (_snapshotDisplayImage == null || _snapshotDisplayImage.sprite == null) return;
        SetZoomState(!_isZoomed);
    }

    private void SetZoomState(bool zoomed)
    {
        _isZoomed = zoomed;
        if (_snapshotDisplayImage != null)
        {
            float scale = zoomed ? _zoomedScale : 1f;
            _snapshotDisplayImage.rectTransform.localScale = new Vector3(scale, scale, 1f);
        }
        if (_zoomButtonText != null)
        {
            _zoomButtonText.text = zoomed ? "Масштаб: 2x" : "Масштаб: 1x";
        }
    }

    private IEnumerator FlashRoutine()
    {
        _screenFlashImage.gameObject.SetActive(true);
        _screenFlashImage.color = new Color(1f, 1f, 1f, 0.75f);

        const float duration = 0.12f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(0.75f, 0f, elapsed / duration);
            _screenFlashImage.color = new Color(1f, 1f, 1f, alpha);
            yield return null;
        }

        _screenFlashImage.gameObject.SetActive(false);
        _flashCoroutine = null;
    }

    private void SetSnapshotButtonActive(bool isActive)
    {
        if (_snapshotBtnCanvasGroup != null)
        {
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
                ? $"Камера: {CurrentCamera.cameraName}"
                : "Камера: выберите на карте";
        }

        if (_targetBuildingText != null)
        {
            _targetBuildingText.text = CurrentBuilding != null
                ? $"Цель: {CurrentBuilding.buildingName} [{CurrentBuilding.sectorCode}]"
                : "Цель отправки: не выбрана";
        }
    }
}