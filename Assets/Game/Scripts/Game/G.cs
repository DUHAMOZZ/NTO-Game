using UnityEngine;

public static class G
{
    // каждый менеджер должен быть с интерфейсом IServise
    // тут объявлять менеджеры
    //public static GameMain GameMain;
}

[DefaultExecutionOrder(-9999)]
public static class Bootstrapper
{
    private static bool _initialized = false;
    private static GameObject serviceHolder;

    // запускается перед созданием сцены
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void OnBeforeSceneLoad()
    {
        if(_initialized) return;

        serviceHolder = new GameObject("===Services===");
        Object.DontDestroyOnLoad(serviceHolder);

        // тут порядок создания
        //G.GameMain = CreateSimpleService<GameMain>();

        //можно еще так, когда сцена загружается
        //G.SceneLoader.OnLoadAction = () =>
        //{
        //    G.GameMain.Setup();
        //};

        

        _initialized = true;

    }

    private static T CreateSimpleService<T>() where T : Component, IService
    {
        GameObject g = new GameObject(typeof(T).ToString());

        g.transform.parent = serviceHolder.transform;
        T t = g.AddComponent<T>();
        t.Init();
        return g.GetComponent<T>();
    }
    
}

public interface IService
{
    public void Init();
}
