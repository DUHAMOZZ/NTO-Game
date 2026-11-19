using System;
using UnityEngine;

namespace AnomalyDispatch.Dialogue
{
    public class DialogueRunner : MonoBehaviour, IService
    {
        private DialogueUI ui;

        public event Action<DialogueNode> OnConversationEnded;

        public DialogueConversation CurrentConversation { get; private set; }
        public DialogueNode CurrentNode { get; private set; }
        public bool IsRunning { get; private set; }

        public void Init()
        {
        }

        void Start()
        {
            ui = FindFirstObjectByType<DialogueUI>();
        }

        public void StartConversation(DialogueConversation conversation, string overrideStartNode = null)
        {
            if (conversation == null) { Debug.LogError("[Dialogue] Conversation is null"); return; }
            if (ui == null) { Debug.LogError("[Dialogue] UI is not found on the scene!"); return; }

            CurrentConversation = conversation;
            IsRunning = true;

            var startId = string.IsNullOrEmpty(overrideStartNode) ? conversation.startNodeId : overrideStartNode;
            var node = conversation.GetNode(startId);
            if (node == null)
            {
                Debug.LogError($"[Dialogue] Start node '{startId}' not found in {conversation.name}");
                EndConversation();
                return;
            }

            ui.Show();
            PresentNode(node);
        }

        private void PresentNode(DialogueNode node)
        {
            CurrentNode = node;

            if (node.onEnterEffects != null)
                foreach (var eff in node.onEnterEffects) eff.Apply(G.PlayerState);

            ui.ShowNode(node);

            if (node.isEnding)
            {
                ui.WaitForContinue(EndConversation);
                return;
            }

            if (node.HasChoices)
            {
                ui.ShowChoices(node.choices, G.PlayerState, OnChoiceSelected);
                return;
            }

            if (string.IsNullOrEmpty(node.nextNodeId))
            {
                ui.WaitForContinue(EndConversation);
                return;
            }

            ui.WaitForContinue(() => PresentNode(CurrentConversation.GetNode(node.nextNodeId)));
        }

        private void OnChoiceSelected(DialogueChoice choice)
        {
            if (choice == null) return;

            if (choice.effects != null)
                foreach (var eff in choice.effects) eff.Apply(G.PlayerState);

            var next = CurrentConversation.GetNode(choice.targetNodeId);
            if (next == null)
            {
                Debug.LogError($"[Dialogue] Node '{choice.targetNodeId}' not found");
                EndConversation();
                return;
            }
            PresentNode(next);
        }

        private void EndConversation()
        {
            IsRunning = false;
            ui.Hide();
            OnConversationEnded?.Invoke(CurrentNode);
        }
    }
}