using System;
using System.Collections.Generic;
using AnomalyDispatch.Dialogue;
using UnityEngine;

public enum DispatchActionType
{
    SuccessEndScenario,
    AdvanceToStep2,
    FailEndScenario,
    NonTerminalMistake
}

[Serializable]
public class SquadDispatchRule
{
    public SquadConfig squad;
    public DispatchActionType actionType = DispatchActionType.FailEndScenario;
    public int rewardCurrency = 0;
    public int sanityDelta = 0;
    [TextArea(2, 4)] public string radioReport;
}

[CreateAssetMenu(fileName = "Scenario_New", menuName = "DeltaTime/Scenario Config")]
public class ScenarioConfig : ScriptableObject
{
    [Header("Идентификация и звонок")]
    public string scenarioId = "scenario_01";
    public string scenarioTitle = "Сценарий 1. Тень";
    public DialogueConversation dialogue;

    [Tooltip("Краткая сводка звонка, которая остаётся на левом мониторе после окончания разговора")]
    [TextArea(2, 4)] public string callerNotesSummary;

    [Header("Улики на правом мониторе")]
    [Tooltip("ID здания-цели (должен совпадать с BuildingTarget.BuildingId)")]
    public string targetBuildingId = "Building_01";

    [Tooltip("ID камеры, на которой видна аномалия/улика (совпадает с CameraPoint.CameraId)")]
    public string clueCameraId = "Camera_01";

    [Tooltip("Опциональный спрайт снимка для этого сценария (если пусто — используется спрайт самой камеры)")]
    public Sprite clueSnapshot;

    [TextArea(2, 4)] public string clueDescription;

    [Header("Повторный снимок (например, для Эхо через 10 сек)")]
    public float secondSnapshotDelay = 0f;
    public Sprite secondClueSnapshot;
    [TextArea(2, 4)] public string secondClueDescription;

    [Header("Таймер (0 = без ограничения времени)")]
    public float timeLimitSeconds = 0f;
    public int timeoutSanityDelta = -8;
    [TextArea(2, 3)] public string timeoutReport = "Время вышло. Помощь не успела.";
    [TextArea(2, 3)] public string step2TimeoutReport = "Первый этаж зачищен, но раненому никто не помог.";

    [Header("Правила отрядов: Шаг 1")]
    public List<SquadDispatchRule> step1Rules = new List<SquadDispatchRule>();

    [Header("Правила отрядов: Шаг 2 (для многоэтапных вызовов)")]
    public List<SquadDispatchRule> step2Rules = new List<SquadDispatchRule>();

    [Header("Ошибка выбора здания на карте")]
    public bool wrongBuildingEndsScenario = true;
    public int wrongBuildingSanityDelta = -2;
    [TextArea(2, 3)] public string wrongBuildingReport = "Ошибка координации: отряд выехал по ложному адресу!";

    [Header("Игнорирование звонка (опционально, например Эхо)")]
    public string ignoreEndingId = "";
    public int ignoreSanityDelta = 1;
    public float recallDelaySeconds = 2.5f;

    public SquadDispatchRule FindRule(int step, SquadConfig squad)
    {
        if (squad == null) return null;
        List<SquadDispatchRule> list = (step == 2) ? step2Rules : step1Rules;
        if (list == null) return null;

        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] != null && list[i].squad == squad)
            {
                return list[i];
            }
        }
        return null;
    }
}