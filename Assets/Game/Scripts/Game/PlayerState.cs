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