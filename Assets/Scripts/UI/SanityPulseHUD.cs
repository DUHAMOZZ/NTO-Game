using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SanityPulseHUD : MonoBehaviour
{
    private enum StressZone
    {
        None = -1,
        Calm = 0,
        Tense = 1,
        Panic = 2,
        Doom = 3
    }

    [Header("Èñòî÷íèê äàííûõ")]
    [SerializeField] private GameShiftController _shiftController;

    [Header("Ýëåìåíòû HUD")]
    [SerializeField] private RectTransform _hudRoot;
    [SerializeField] private RectTransform _heartIconRect;
    [SerializeField] private Graphic _heartIconGraphic;
    [SerializeField] private ECGLineGraphic _ecgLine;
    [SerializeField] private TextMeshProUGUI _bpmText;
    [SerializeField] private TextMeshProUGUI _stateLabelText;
    [SerializeField] private TextMeshProUGUI _sanityPercentText;

    [Header("Ïîðîãè è öâåòà çîí")]
    [SerializeField] private float _highSanityThreshold = 70f;
    [SerializeField] private float _lowSanityThreshold = 35f;
    [SerializeField] private Color _calmColor = new Color(0.24f, 1.0f, 0.48f, 1f);
    [SerializeField] private Color _tenseColor = new Color(1.0f, 0.72f, 0.15f, 1f);
    [SerializeField] private Color _panicColor = new Color(1.0f, 0.18f, 0.18f, 1f);

    [Header("Äèàïàçîí ïóëüñà (BPM)")]
    [SerializeField] private float _calmBpmMin = 60f;
    [SerializeField] private float _calmBpmMax = 70f;
    [SerializeField] private float _tenseBpmMin = 85f;
    [SerializeField] private float _tenseBpmMax = 115f;
    [SerializeField] private float _panicBpmMin = 130f;
    [SerializeField] private float _panicBpmMax = 178f;

    [Header("Àíèìàöèÿ áèåíèÿ è äðîæàíèÿ")]
    [SerializeField] private float _iconBaseScale = 1f;
    [SerializeField] private float _iconBeatPeakScale = 1.26f;
    [SerializeField] private float _panicShakeAmplitude = 3.2f;
    [SerializeField] private float _valueSmoothSpeed = 4.5f;

    private float _targetConditionPercent = 80f;
    private float _smoothedCondition = 80f;
    private float _displaySanityPercent = 80f;
    private float _currentBpm = 65f;
    private float _beatPhase;
    private float _ecgScrollPhase;

    private int _lastDisplayedBpm = -1;
    private int _lastDisplayedSanityInt = -1;
    private StressZone _lastZone = StressZone.None;
    private Vector2 _baseAnchoredPosition;
    private bool _basePositionCaptured;

    private void Awake()
    {
        CaptureBasePosition();
    }

    private void OnEnable()
    {
        CaptureBasePosition();

        if (_shiftController != null)
        {
            _shiftController.OnEffectiveConditionChanged += SetConditionPercent;
            _shiftController.OnSanityChanged += SetSanityReadoutPercent;

            _targetConditionPercent = _shiftController.CurrentEffectiveConditionPercent;
            _smoothedCondition = _targetConditionPercent;
            _displaySanityPercent = _shiftController.CurrentSanityPercent;
        }

        UpdateVisualsImmediate(_smoothedCondition);
    }

    private void OnDisable()
    {
        if (_shiftController != null)
        {
            _shiftController.OnEffectiveConditionChanged -= SetConditionPercent;
            _shiftController.OnSanityChanged -= SetSanityReadoutPercent;
        }
    }

    private void CaptureBasePosition()
    {
        if (!_basePositionCaptured && _hudRoot != null)
        {
            _baseAnchoredPosition = _hudRoot.anchoredPosition;
            _basePositionCaptured = true;
        }
    }

    public void SetSanityPercent(float sanityPercent)
    {
        SetConditionPercent(sanityPercent);
        SetSanityReadoutPercent(sanityPercent);
    }

    public void SetConditionPercent(float conditionPercent)
    {
        _targetConditionPercent = Mathf.Clamp(conditionPercent, 0f, 100f);
    }

    public void SetSanityReadoutPercent(float sanityPercent)
    {
        _displaySanityPercent = Mathf.Clamp(sanityPercent, 0f, 100f);
    }

    private void Update()
    {
        _smoothedCondition = Mathf.Lerp(_smoothedCondition, _targetConditionPercent, Time.deltaTime * _valueSmoothSpeed);
        UpdateVisualsImmediate(_smoothedCondition);
    }

    private void UpdateVisualsImmediate(float condition)
    {
        StressZone zone = GetZone(condition);
        Color zoneColor = GetZoneColor(zone);
        float targetBpm = CalculateTargetBpm(condition, zone);
        _currentBpm = Mathf.Lerp(_currentBpm, targetBpm, Time.deltaTime * _valueSmoothSpeed);

        float beatsPerSecond = _currentBpm / 60f;
        _beatPhase += Time.deltaTime * beatsPerSecond;
        if (_beatPhase >= 1f) _beatPhase -= Mathf.Floor(_beatPhase);

        _ecgScrollPhase += Time.deltaTime * beatsPerSecond;
        if (_ecgScrollPhase >= 1000f) _ecgScrollPhase -= 1000f;

        if (_heartIconRect != null)
        {
            float pulseCurve = EvaluateHeartbeatScale(_beatPhase);
            float scale = Mathf.Lerp(_iconBaseScale, _iconBeatPeakScale, pulseCurve);
            _heartIconRect.localScale = new Vector3(scale, scale, 1f);
        }

        if (_heartIconGraphic != null && _heartIconGraphic.color != zoneColor)
        {
            _heartIconGraphic.color = zoneColor;
        }

        if (_ecgLine != null)
        {
            float jitter = (zone == StressZone.Panic || zone == StressZone.Doom)
                ? Mathf.InverseLerp(_lowSanityThreshold, 0f, condition)
                : 0f;
            _ecgLine.UpdateWave(_ecgScrollPhase, jitter, zoneColor);
        }

        int roundedBpm = Mathf.RoundToInt(_currentBpm);
        if (_bpmText != null)
        {
            if (roundedBpm != _lastDisplayedBpm)
            {
                _lastDisplayedBpm = roundedBpm;
                _bpmText.SetText("{0} BPM", roundedBpm);
            }
            if (_bpmText.color != zoneColor)
            {
                _bpmText.color = zoneColor;
            }
        }

        if (_sanityPercentText != null)
        {
            int roundedSanity = Mathf.RoundToInt(_displaySanityPercent);
            if (roundedSanity != _lastDisplayedSanityInt)
            {
                _lastDisplayedSanityInt = roundedSanity;
                _sanityPercentText.SetText("{0}%", roundedSanity);
            }
            if (_sanityPercentText.color != zoneColor)
            {
                _sanityPercentText.color = zoneColor;
            }
        }

        if (zone != _lastZone)
        {
            _lastZone = zone;
            if (_stateLabelText != null)
            {
                _stateLabelText.text = zone switch
                {
                    StressZone.Calm => "ÍÎÐÌÀ",
                    StressZone.Tense => "ÒÐÅÂÎÃÀ",
                    StressZone.Panic => "ÊÐÈÒÈ×ÅÑÊÈÉ ÑÒÐÅÑÑ",
                    StressZone.Doom => "ÁÅÇÛÑÕÎÄÍÎÑÒÜ",
                    _ => string.Empty
                };
                _stateLabelText.color = zoneColor;
            }
        }

        if (_hudRoot != null)
        {
            if (zone == StressZone.Panic || zone == StressZone.Doom)
            {
                float panicStrength = Mathf.InverseLerp(_lowSanityThreshold, 0f, condition);
                float amp = _panicShakeAmplitude * (0.4f + 0.6f * panicStrength);
                float ox = (Mathf.PerlinNoise(Time.time * 34f, 0f) - 0.5f) * 2f * amp;
                float oy = (Mathf.PerlinNoise(0f, Time.time * 34f) - 0.5f) * 2f * amp;
                _hudRoot.anchoredPosition = _baseAnchoredPosition + new Vector2(ox, oy);
            }
            else if (_hudRoot.anchoredPosition != _baseAnchoredPosition)
            {
                _hudRoot.anchoredPosition = _baseAnchoredPosition;
            }
        }
    }

    private StressZone GetZone(float condition)
    {
        if (condition <= 1f) return StressZone.Doom;
        if (condition >= _highSanityThreshold) return StressZone.Calm;
        if (condition >= _lowSanityThreshold) return StressZone.Tense;
        return StressZone.Panic;
    }

    private Color GetZoneColor(StressZone zone)
    {
        return zone switch
        {
            StressZone.Calm => _calmColor,
            StressZone.Tense => _tenseColor,
            _ => _panicColor
        };
    }

    private float CalculateTargetBpm(float condition, StressZone zone)
    {
        switch (zone)
        {
            case StressZone.Calm:
                {
                    float t = Mathf.InverseLerp(100f, _highSanityThreshold, condition);
                    return Mathf.Lerp(_calmBpmMin, _calmBpmMax, t);
                }
            case StressZone.Tense:
                {
                    float t = Mathf.InverseLerp(_highSanityThreshold, _lowSanityThreshold, condition);
                    return Mathf.Lerp(_tenseBpmMin, _tenseBpmMax, t);
                }
            case StressZone.Panic:
                {
                    float t = Mathf.InverseLerp(_lowSanityThreshold, 1f, condition);
                    return Mathf.Lerp(_panicBpmMin, _panicBpmMax, t);
                }
            default:
                return _panicBpmMax;
        }
    }

    private static float EvaluateHeartbeatScale(float phase)
    {
        if (phase < 0.14f)
        {
            return Mathf.Sin((phase / 0.14f) * Mathf.PI);
        }
        if (phase >= 0.20f && phase < 0.34f)
        {
            return Mathf.Sin(((phase - 0.20f) / 0.14f) * Mathf.PI) * 0.65f;
        }
        return 0f;
    }
}