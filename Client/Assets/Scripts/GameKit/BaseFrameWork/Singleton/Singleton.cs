/// <remarks>普通单例泛型</remarks>
public abstract class Singleton<T> : IManager where T : new()
{
    private static T instance;

    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new T();
            }
            return instance;
        }
    }

    public virtual bool Init()
    {
        return true;
    }
}