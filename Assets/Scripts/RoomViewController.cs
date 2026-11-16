using System.Collections;
using UnityEngine;

public class RoomViewController : MonoBehaviour
{
    [Header("Настройки углов")]
    [Tooltip("Список углов по оси Y (например: 0 - стол, 90 - правый монитор, 180 - дверь сзади, -90 - левый монитор)")]
    [SerializeField] private float[] _targetYawAngles = new float[] { -45f, 0f, 45f, 180f };

    [Tooltip("Стартовый индекс ракурса")]
    [SerializeField] private int _currentIndex = 1; // По умолчанию смотрим в центр (0 градусов)

    [Header("Настройки плавности")]
    [Tooltip("Время поворота в секундах")]
    [SerializeField] private float _rotationDuration = 0.35f;

    [Tooltip("Кривая плавности движения (разгон и торможение кресла)")]
    [SerializeField] private AnimationCurve _smoothCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Tooltip("Зацикливать ли поворот по кругу на 360 градусов")]
    [SerializeField] private bool _isCyclic = true;

    private Coroutine _rotationCoroutine;

    private void Start()
    {
        // Устанавливаем стартовый угол без анимации при старте игры
        if (_targetYawAngles != null && _targetYawAngles.Length > 0)
        {
            _currentIndex = Mathf.Clamp(_currentIndex, 0, _targetYawAngles.Length - 1);
            transform.rotation = Quaternion.Euler(0f, _targetYawAngles[_currentIndex], 0f);
        }
    }

    /// <summary>
    /// Поворот на один шаг влево (вешать на кнопку UI Влево)
    /// </summary>
    public void RotateLeft()
    {
        ChangeViewIndex(-1);
    }

    /// <summary>
    /// Поворот на один шаг вправо (вешать на кнопку UI Вправо)
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
            // Бесшовный переход по кругу (с последнего на первый и наоборот)
            if (newIndex >= _targetYawAngles.Length) newIndex = 0;
            else if (newIndex < 0) newIndex = _targetYawAngles.Length - 1;
        }
        else
        {
            // Упираемся в границы без кругового вращения
            newIndex = Mathf.Clamp(newIndex, 0, _targetYawAngles.Length - 1);
            if (newIndex == _currentIndex) return; // Уже на краю
        }

        _currentIndex = newIndex;
        RotateToAngle(_targetYawAngles[_currentIndex]);
    }

    private void RotateToAngle(float targetAngleY)
    {
        // Если уже крутимся — прерываем прошлый поворот, чтобы подхватить новый клик
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

            // Quaternion.Slerp сам выбирает кратчайший путь поворота сферы
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, curveValue);

            yield return null;
        }

        transform.rotation = targetRotation;
        _rotationCoroutine = null;
    }
}