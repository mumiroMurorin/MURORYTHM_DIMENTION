using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// シーン遷移の二重実行を防ぐ。
/// 1つのシーンにいる間は、最初に始まったシーン遷移だけを通し、それ以降の遷移の要求は無視する。
/// 次のシーンが読み込まれたら、また遷移を受け付ける。
/// </summary>
public static class SceneTransitionGate
{
    static bool isTransitioning;
    static string transitioningSceneName;

    /// <summary>
    /// シーン遷移が始まっているか
    /// </summary>
    public static bool IsTransitioning => isTransitioning;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        isTransitioning = false;
        transitioningSceneName = null;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    /// <summary>
    /// シーン遷移を始めてよいか確かめる。
    /// 既に別の遷移が始まっていれば false を返すので、呼び出し側は何もせずに戻ること。
    /// </summary>
    public static bool TryBegin(string nextSceneName)
    {
        if (isTransitioning)
        {
            Debug.LogWarning($"【Transition】{transitioningSceneName} への遷移中のため、{nextSceneName} への遷移を無視しました");
            return false;
        }

        isTransitioning = true;
        transitioningSceneName = nextSceneName;
        return true;
    }

    /// <summary>
    /// シーンの読み込みを始められなかったときに呼び、遷移をまた受け付けるようにする
    /// </summary>
    public static void Abort()
    {
        isTransitioning = false;
        transitioningSceneName = null;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single) { return; }

        isTransitioning = false;
        transitioningSceneName = null;
    }
}
