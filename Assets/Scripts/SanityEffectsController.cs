using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SanityEffectsController : MonoBehaviour
{
    [Header("Источник данных")]
    [SerializeField] private GameShiftController _shiftController;

    [Header("URP Post-Processing (Global Volume)")]
    [SerializeField] private Volume _globalVolume;
    [SerializeField] private float _minVignette = 0.18f;
    [SerializeField] private float _maxVignette = 0.60f;
    [SerializeField] private float _minFilmGrain = 0.08f;
    [SerializeField] private float _maxFilmGrain = 0.88f;
    [SerializeField] private float _minChromaticAberration = 0.0f;
    [SerializeField] private float _maxChromaticAberration = 0.75f;
    [SerializeField] private float _effectSmoothSpeed = 3.5f;

    [Header("Мерцание лампы при сильном стрессе")]
    [SerializeField] private Light _deskLampLight;
    [SerializeField] private float _flickerConditionThreshold = 40f;
    [SerializeField] private float _minPauseBetweenFlickers = 1.2f;
    [SerializeField] private float _maxPauseBetweenFlickers = 5.5f;

    [Header("Аудио-эффекты (опционально)")]
    [SerializeField] private AudioSource _heartbeatAudio;
    [SerializeField] private float _heartbeatStartThreshold = 75f;
    [SerializeField] private float _heartbeatMaxVolume = 0.80f;

    [SerializeField] private AudioSource _whisperAudio;
    [SerializeField] private float _whisperStartThreshold = 55f;
    [SerializeField] private float _whisperMaxVolume = 0.65f;

    private Vignette _vignette;
    private FilmGrain _filmGrain;
    private ChromaticAberration _chromaticAberration;

    private float _targetConditionPercent = 80f;
    private float _smoothedConditionPercent = 80f;
    private float _baseLampIntensity = 1f;

    private bool _isFlickerBurst;
    private float _nextFlickerEventTime;
    private float _nextFlickerStepTime;

    private void Awake()
    {
        if (_deskLampLight != null)
        {
            _baseLampIntensity = _deskLampLight.intensity;
        }

        SetupVolumeOverrides();
    }

    private void OnEnable()
    {
        if (_shiftController != null)
        {
            _shiftController.OnEffectiveConditionChanged += SetSanityPercent;
            _targetConditionPercent = _shiftController.CurrentEffectiveConditionPercent;
            _smoothedConditionPercent = _targetConditionPercent;
        }
    }

    private void OnDisable()
    {
        if (_shiftController != null)
        {
            _shiftController.OnEffectiveConditionChanged -= SetSanityPercent;
        }
    }

    private void Start()
    {
        if (_shiftController != null)
        {
            _targetConditionPercent = _shiftController.CurrentEffectiveConditionPercent;
            _smoothedConditionPercent = _targetConditionPercent;
        }

        ApplyPostProcessing(_smoothedConditionPercent);
        ApplyAudio(_smoothedConditionPercent);
        ScheduleNextFlickerBurst();
    }

    public void SetSanityPercent(float conditionPercent)
    {
        _targetConditionPercent = Mathf.Clamp(conditionPercent, 0f, 100f);
    }

    private void Update()
    {
        _smoothedConditionPercent = Mathf.Lerp(_smoothedConditionPercent, _targetConditionPercent, Time.deltaTime * _effectSmoothSpeed);

        ApplyPostProcessing(_smoothedConditionPercent);
        UpdateLampFlicker(_smoothedConditionPercent);
        ApplyAudio(_smoothedConditionPercent);
    }

    private void SetupVolumeOverrides()
    {
        if (_globalVolume == null || _globalVolume.profile == null) return;

        if (!_globalVolume.profile.TryGet(out _vignette))
        {
            _vignette = _globalVolume.profile.Add<Vignette>(false);
        }
        if (_vignette != null)
        {
            _vignette.intensity.overrideState = true;
        }

        if (!_globalVolume.profile.TryGet(out _filmGrain))
        {
            _filmGrain = _globalVolume.profile.Add<FilmGrain>(false);
        }
        if (_filmGrain != null)
        {
            _filmGrain.intensity.overrideState = true;
        }

        if (!_globalVolume.profile.TryGet(out _chromaticAberration))
        {
            _chromaticAberration = _globalVolume.profile.Add<ChromaticAberration>(false);
        }
        if (_chromaticAberration != null)
        {
            _chromaticAberration.intensity.overrideState = true;
        }
    }

    private void ApplyPostProcessing(float conditionPercent)
    {
        float stress = 1f - Mathf.Clamp01(conditionPercent / 100f);

        if (_vignette != null)
        {
            _vignette.intensity.value = Mathf.Lerp(_minVignette, _maxVignette, stress);
        }

        if (_filmGrain != null)
        {
            _filmGrain.intensity.value = Mathf.Lerp(_minFilmGrain, _maxFilmGrain, stress);
        }

        if (_chromaticAberration != null)
        {
            _chromaticAberration.intensity.value = Mathf.Lerp(_minChromaticAberration, _maxChromaticAberration, Mathf.Pow(stress, 1.3f));
        }
    }

    private void UpdateLampFlicker(float conditionPercent)
    {
        if (_deskLampLight == null) return;

        if (conditionPercent >= _flickerConditionThreshold)
        {
            _isFlickerBurst = false;
            if (!Mathf.Approximately(_deskLampLight.intensity, _baseLampIntensity))
            {
                _deskLampLight.intensity = Mathf.MoveTowards(_deskLampLight.intensity, _baseLampIntensity, Time.deltaTime * 20f);
            }
            return;
        }

        float now = Time.time;
        float severity = Mathf.InverseLerp(_flickerConditionThreshold, 0f, conditionPercent);

        if (!_isFlickerBurst)
        {
            if (now >= _nextFlickerEventTime)
            {
                _isFlickerBurst = true;
                float burstDuration = Random.Range(0.20f, 0.55f + 0.35f * severity);
                _nextFlickerEventTime = now + burstDuration;
                _nextFlickerStepTime = now;
            }
        }
        else
        {
            if (now >= _nextFlickerEventTime)
            {
                _isFlickerBurst = false;
                _deskLampLight.intensity = _baseLampIntensity;
                float pause = Mathf.Lerp(_maxPauseBetweenFlickers, _minPauseBetweenFlickers, severity);
                _nextFlickerEventTime = now + Random.Range(pause * 0.6f, pause * 1.2f);
                return;
            }

            if (now >= _nextFlickerStepTime)
            {
                float minFactor = Mathf.Lerp(0.35f, 0.02f, severity);
                _deskLampLight.intensity = _baseLampIntensity * Random.Range(minFactor, 0.90f);
                _nextFlickerStepTime = now + Random.Range(0.035f, 0.085f);
            }
        }
    }

    private void ScheduleNextFlickerBurst()
    {
        _isFlickerBurst = false;
        _nextFlickerEventTime = Time.time + Random.Range(_minPauseBetweenFlickers, _maxPauseBetweenFlickers);
    }

    private void ApplyAudio(float conditionPercent)
    {
        if (_heartbeatAudio != null)
        {
            float t = Mathf.InverseLerp(_heartbeatStartThreshold, 0f, conditionPercent);
            _heartbeatAudio.volume = t * _heartbeatMaxVolume;
            _heartbeatAudio.pitch = Mathf.Lerp(0.95f, 1.55f, t);
        }

        if (_whisperAudio != null)
        {
            float t = Mathf.InverseLerp(_whisperStartThreshold, 0f, conditionPercent);
            _whisperAudio.volume = t * _whisperMaxVolume;
        }
    }
}