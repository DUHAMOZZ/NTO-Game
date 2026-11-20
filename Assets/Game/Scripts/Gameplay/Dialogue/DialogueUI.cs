using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyDispatch.Dialogue
{
    public class DialogueUI : MonoBehaviour
    {
        [SerializeField] private GameObject rootPanel;
        [SerializeField] private TMP_Text speakerText;
        [SerializeField] private TMP_Text bodyText;

        [SerializeField] private Transform choicesContainer;
        [SerializeField] private Button choiceButtonPrefab;

        [SerializeField] private float charsPerSecond = 45f;
        [SerializeField] private float delayBeforeChoices = 0.25f;

        private Coroutine _typeRoutine;
        private Coroutine _spawnChoicesRoutine;
        private Action _onContinue;
        private bool _isTyping;
        private bool _waitingForClick;
        private bool _waitingForChoice;
        private int _ignoreInputFrame = -1;
        private readonly List<GameObject> _spawnedChoices = new();

        private void Awake()
        {
            if (G.DialogueRunner != null)
            {
                G.DialogueRunner.RegisterUI(this);
            }

            if (rootPanel != null && (G.DialogueRunner == null || !G.DialogueRunner.IsRunning))
            {
                rootPanel.SetActive(false);
            }
        }

        public void Show() { if (rootPanel) rootPanel.SetActive(true); }
        public void Hide()
        {
            if (_typeRoutine != null) { StopCoroutine(_typeRoutine); _typeRoutine = null; }
            if (_spawnChoicesRoutine != null) { StopCoroutine(_spawnChoicesRoutine); _spawnChoicesRoutine = null; }
            _isTyping = false;
            _waitingForClick = false;
            _waitingForChoice = false;
            ClearChoices();
            if (rootPanel) rootPanel.SetActive(false);
        }

        public void ShowNode(DialogueNode node)
        {
            if (_spawnChoicesRoutine != null) { StopCoroutine(_spawnChoicesRoutine); _spawnChoicesRoutine = null; }
            ClearChoices();
            _waitingForClick = false;
            _waitingForChoice = false;
            _ignoreInputFrame = Time.frameCount;

            if (speakerText) speakerText.text = node.speaker;

            if (_typeRoutine != null) StopCoroutine(_typeRoutine);
            _typeRoutine = StartCoroutine(TypewriterRoutine(node.text));
        }

        public void WaitForContinue(Action onContinue)
        {
            _onContinue = onContinue;
            _waitingForClick = true;
        }

        public void ShowChoices(List<DialogueChoice> choices, PlayerState state, Action<DialogueChoice> onPick)
        {
            if (choicesContainer == null || choiceButtonPrefab == null)
            {
                Debug.LogError("[DialogueUI] is not configured.");
                return;
            }

            _waitingForChoice = true;
            if (_spawnChoicesRoutine != null) StopCoroutine(_spawnChoicesRoutine);
            _spawnChoicesRoutine = StartCoroutine(SpawnChoicesRoutine(choices, state, onPick));
        }

        private IEnumerator SpawnChoicesRoutine(List<DialogueChoice> choices, PlayerState state, Action<DialogueChoice> onPick)
        {
            while (_isTyping) yield return null;
            yield return new WaitForSeconds(delayBeforeChoices);

            foreach (var ch in choices)
            {
                bool available = AreConditionsMet(ch.conditions, state);
                if (!available && ch.hideIfUnavailable) continue;

                var btn = Instantiate(choiceButtonPrefab, choicesContainer);
                var label = btn.GetComponentInChildren<TMP_Text>();
                if (label) label.text = ch.text;
                btn.interactable = available;

                var captured = ch;
                btn.onClick.AddListener(() =>
                {
                    if (!_waitingForChoice) return;
                    _waitingForChoice = false;
                    _ignoreInputFrame = Time.frameCount;
                    ClearChoices();
                    onPick?.Invoke(captured);
                });

                _spawnedChoices.Add(btn.gameObject);
            }
            _spawnChoicesRoutine = null;
        }

        private static bool AreConditionsMet(List<FlagCondition> conditions, PlayerState state)
        {
            if (conditions == null) return true;
            foreach (var c in conditions)
                if (!c.Evaluate(state)) return false;
            return true;
        }

        private IEnumerator TypewriterRoutine(string text)
        {
            _isTyping = true;
            if (bodyText == null)
            {
                _isTyping = false;
                _typeRoutine = null;
                yield break;
            }

            bodyText.text = text ?? string.Empty;
            bodyText.maxVisibleCharacters = 0;
            bodyText.ForceMeshUpdate();

            int total = bodyText.textInfo.characterCount;
            int visible = Mathf.Min(1, total);
            bodyText.maxVisibleCharacters = visible;
            float acc = visible;

            while (visible < total)
            {
                acc += Time.deltaTime * charsPerSecond;
                int target = Mathf.Min(total, Mathf.FloorToInt(acc));
                if (target != visible) { visible = target; bodyText.maxVisibleCharacters = visible; }
                yield return null;
            }

            bodyText.maxVisibleCharacters = total;
            _isTyping = false;
            _typeRoutine = null;
        }

        private void SkipTypewriter()
        {
            if (_typeRoutine != null) StopCoroutine(_typeRoutine);
            if (bodyText != null) bodyText.maxVisibleCharacters = bodyText.textInfo.characterCount;
            _isTyping = false;
            _typeRoutine = null;
        }

        private void ClearChoices()
        {
            foreach (var go in _spawnedChoices) if (go) Destroy(go);
            _spawnedChoices.Clear();
        }

        private void Update()
        {
            if (Time.frameCount == _ignoreInputFrame) return;
            if (!WasClickedThisFrame() && !WasSpacePressed()) return;

            if (_isTyping) { SkipTypewriter(); return; }

            if (_waitingForClick && _onContinue != null)
            {
                var cb = _onContinue;
                _onContinue = null;
                _waitingForClick = false;
                cb();
            }
        }

        private static bool WasClickedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            var m = UnityEngine.InputSystem.Mouse.current;
            return m != null && m.leftButton.wasPressedThisFrame;
#else
            return Input.GetMouseButtonDown(0);
#endif
        }

        private static bool WasSpacePressed()
        {
#if ENABLE_INPUT_SYSTEM
            var k = UnityEngine.InputSystem.Keyboard.current;
            return k != null && k.spaceKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Space);
#endif
        }
    }
}