
using UnityEngine;
/// <remarks>继承自Mono的泛型单例</remarks>
public abstract class MonoSingleton<T> : MonoBehaviour, IManager where T : UnityEngine.Component
{
    private static T instance;

    public static T Instance
    {
        get
        {
            if (instance == null)
            {

                GameObject obj = new GameObject("[" + typeof(T).ToString() + "]");
                instance = obj.AddComponent<T>();
                GameObject.DontDestroyOnLoad(obj);
                if (instance != null)
                {
                    Helper.Log("Singleton init result:[" + typeof(T).ToString() + "]");
                }
                else
                {
                    Helper.LogError("Instance is null : " + typeof(T).ToString());
                }
            }
            return instance;
        }
    }

    public virtual bool Init()
    {
        return true;
    }

    public T GetInstance()
    {
        return Instance;
    }
}