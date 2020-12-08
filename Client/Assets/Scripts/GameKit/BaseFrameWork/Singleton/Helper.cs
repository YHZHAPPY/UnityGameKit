
public static class Helper
{
    public static void Log(string message)
    {
#if !HIDE_LOG
        UnityEngine.Debug.Log(message);
#endif
    }

    public static void LogError(string message)
    {
#if !HIDE_LOG
        UnityEngine.Debug.LogError(message);
#endif
    }

    /// <remarks>日志输出和一些通用功能</remarks>
    public static void LogWarning(string message)
    {
#if !HIDE_LOG
        UnityEngine.Debug.LogWarning(message);
#endif
    }
}