using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;

public class CMS : MonoBehaviour, IService
{
    // Data storage
    //private GameConfig gameConfig;

    public event Action OnContentLoaded;
    public event Action OnContentReloaded;
    

    public void Init()
    {
        //очередь загрузки
        //LoadGameConfig

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

    //получение
    //public GameConfig GetGameCongig()
    //{
        //return gameConfig;
    //}

    
    public void ReloadContent()
    {
        LoadAllContent();
        OnContentReloaded?.Invoke();
    }
    
}