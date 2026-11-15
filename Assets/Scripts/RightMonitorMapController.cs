using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RightMonitorMapController : MonoBehaviour
{
    [Header("Списки интерактивных точек")]
    [SerializeField] private CameraPoint[] _cameraPoints;
    [SerializeField] private BuildingTarget[] _buildingTargets;

    [Header("Левая часть: Панель снимка")]
    [Tooltip("CanvasGroup кнопки снимка для регулировки прозрачности")]
    [SerializeField] private CanvasGroup _snapshotBtnCanvasGroup;
    [SerializeField] private Button _takeSnapshotButton;
    [SerializeField] private Image _snapshotDisplayImage;
    [SerializeField] private GameObject _noSignalPlaceholder; // Черный экран/помехи до первого фото
    [SerializeField] private Image _screenFlashImage;        // Белая вспышка затвора

    [Header("Информационные надписи (UI TextMeshPro)")]
    [SerializeField] private TextMeshProUGUI _cameraStatusText;
    [SerializeField] private TextMeshProUGUI _targetBuildingText;

    // Свойства для Андрея (чтобы он мог прочитать выбор при отправке отряда с левого монитора)
    public CameraPoint CurrentCamera { get; private set; }
    public BuildingTarget CurrentBuilding { get; private set; }

    private void Awake()
    {
        // Инициализируем камеры
        if (_cameraPoints != null)
        {
            foreach (var cam in _cameraPoints)
            {
                if (cam != null) cam.Initialize(this);
            }
        }

        // Инициализируем здания
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

        // По умолчанию камера не выбрана -> кнопка прозрачна (0.25f) и отключена
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

        // Выбрали камеру — кнопка снимка становится яркой и кликабельной!
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

        // Показываем снимок
        _snapshotDisplayImage.sprite = CurrentCamera.snapshotSprite;

        if (_noSignalPlaceholder != null)
        {
            _noSignalPlaceholder.SetActive(false);
        }

        // Эффект аналоговой вспышки затвора
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
            // Прозрачная и некликабельная (0.25f) или непрозрачная (1.0f)
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
                ? $"СИГНАЛ: {CurrentCamera.cameraName}"
                : "СИГНАЛ: ВЫБЕРИТЕ КАМЕРУ";
        }

        if (_targetBuildingText != null)
        {
            _targetBuildingText.text = CurrentBuilding != null
                ? $"ЦЕЛЬ: {CurrentBuilding.buildingName} [{CurrentBuilding.sectorCode}]"
                : "ЦЕЛЬ ОТПРАВКИ: НЕ ВЫБРАНА";
        }
    }
}