using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ShiftConfig", menuName = "DeltaTime/Shift Config")]
public class ShiftConfig : ScriptableObject
{
    [Header("Стартовые параметры смены")]
    public int startingCurrency = 6;
    [Range(0f, 100f)] public float startingSanity = 50f;
    public float maxSanity = 100f;

    [Header("Тайминги переходов")]
    public float delayBeforeFirstCall = 0.5f;
    public float delayBetweenScenarios = 4.0f;

    [Header("Цепочка сценариев смены")]
    public List<ScenarioConfig> scenarios = new List<ScenarioConfig>();
}