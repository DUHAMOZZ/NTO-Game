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