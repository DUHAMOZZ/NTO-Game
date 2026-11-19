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