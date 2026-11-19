using AnomalyDispatch.Dialogue;
using UnityEngine;

public class ConversationHandler : MonoBehaviour, IService
{
    private DialogueRunner runner;
    private DialogueConversation[] dialogueConversations;

    public void Init()
    {
        runner = G.DialogueRunner;
        dialogueConversations = G.CMS.GetAllDialogueConversations();
    }

    private void Start()
    {
        StartRandomConversation();
    }

    private void StartRandomConversation()
    {
        if (dialogueConversations == null || dialogueConversations.Length == 0)
        {
            Debug.LogWarning("[ConversationHandler] No dialogue");
            return;
        }

        int randomIndex = Random.Range(0, dialogueConversations.Length);
        runner.StartConversation(dialogueConversations[randomIndex]);
    }
}