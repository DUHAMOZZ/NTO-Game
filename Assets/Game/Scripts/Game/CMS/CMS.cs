using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using AnomalyDispatch.Dialogue;

public class CMS : MonoBehaviour, IService
{
    // Data storage
    //private GameConfig gameConfig;

    private List<DialogueConversation> dialogueConversations = new List<DialogueConversation>();

    public event Action OnContentLoaded;
    public event Action OnContentReloaded;
    

    public void Init()
    {
        //очередь загрузки
        //LoadGameConfig
        LoadDialogueConversations();

        LoadAllContent();
    }
    
    public void LoadAllContent()
    {
        OnContentLoaded?.Invoke();
    }
    
    //загрузка
    // private void LoadGameConfig()
    //{
    //    gameConfig = Resources.Load<GameConfig>("GameConfig");
    //    }
    //}
    private void LoadDialogueConversations()
    {
        DialogueConversation[] dialogueConversations_ = Resources.LoadAll<DialogueConversation>("dialogueConversations");
        foreach (var dc in dialogueConversations_)
        {
            dialogueConversations.Add(dc);
        }
    }

    //получение
    //public GameConfig GetGameCongig()
    //{
        //return gameConfig;
    //}

    public DialogueConversation[] GetAllDialogueConversations()
    {
        if(dialogueConversations != null)
        {
            return dialogueConversations.ToArray();
        }
        return null;
    }

    
    public void ReloadContent()
    {
        LoadAllContent();
        OnContentReloaded?.Invoke();
    }
    
}