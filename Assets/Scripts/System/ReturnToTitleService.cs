using System;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;

/// <summary>
/// タイトル画面へ戻す処理。シーンに置く必要はなく、起動時に自動で作られてシーンをまたいで動く。
/// ・放置: ロビー・選曲・リザルト・コンティニュー画面で、一定時間 (5分) 操作が無ければタイトルへ戻す
/// ・デバッグ: Ctrl + H で、どのシーンからでもタイトルへ戻す
/// </summary>
public sealed class ReturnToTitleService : MonoBehaviour
{
    const string TITLE_SCENE_NAME = "TitleScene";

    // 放置とみなすまでの時間 (秒)
    const float IDLE_SECONDS_TO_TITLE = 300f;

    // タイトルへ戻るデバッグキー (Ctrl と一緒に押す)。
    // スライダーのデバッグ用キー (1〜8, Q, W, E, R, T, Y, U, I) と重ならないキーにしている
    const KeyCode DEBUG_RETURN_KEY = KeyCode.H;

    // 放置したらタイトルへ戻すシーン (リズムゲームとチュートリアルは自動で終わるので含めない)
    static readonly string[] IdleReturnSceneNames =
    {
        "LobbyScene",
        "MusicSelectScene",
        "ResultScene",
        "GameOverScene",
    };

    // デバッグキーでタイトルへ戻れるシーン (譜面エディタは含めない)
    static readonly string[] DebugReturnSceneNames =
    {
        "LobbyScene",
        "TutorialScene",
        "MusicSelectScene",
        "RhythmGameScene",
        "ResultScene",
        "GameOverScene",
        "RootScene",
    };

    float lastInputTime;
    IDisposable sliderSubscription;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void CreateInstance()
    {
        // タイトルシーンを読み込めないビルド (譜面エディタ版) では何もしない
        if (!Application.CanStreamedLevelBeLoaded(TITLE_SCENE_NAME)) { return; }

        GameObject obj = new GameObject(nameof(ReturnToTitleService));
        DontDestroyOnLoad(obj);
        obj.AddComponent<ReturnToTitleService>();
    }

    private void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        ResetIdleTimer();
    }

    private void Start()
    {
        TryBindSliderInput();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        sliderSubscription?.Dispose();
        sliderSubscription = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResetIdleTimer();
        TryBindSliderInput();
    }

    private void Update()
    {
        // キーボード・マウスの操作も「操作あり」とみなす
        if (Input.anyKeyDown) { ResetIdleTimer(); }

        string sceneName = SceneManager.GetActiveScene().name;

        if (IsDebugReturnKeyDown() && Contains(DebugReturnSceneNames, sceneName))
        {
            ReturnToTitle("デバッグキー (Ctrl + H) が押された");
            return;
        }

        if (Contains(IdleReturnSceneNames, sceneName) && Time.unscaledTime - lastInputTime >= IDLE_SECONDS_TO_TITLE)
        {
            ReturnToTitle($"{IDLE_SECONDS_TO_TITLE:0} 秒間操作が無かった");
        }
    }

    private static bool IsDebugReturnKeyDown()
    {
        bool isControlPressed = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        return isControlPressed && Input.GetKeyDown(DEBUG_RETURN_KEY);
    }

    private void ReturnToTitle(string reason)
    {
        // 遷移できなかったときに毎フレーム呼ばないよう、先にタイマーを戻す
        ResetIdleTimer();

        // 既に別のシーン遷移が始まっていれば何もしない
        if (!SceneTransitionGate.TryBegin(TITLE_SCENE_NAME)) { return; }

        Debug.Log($"【Transition】{reason}ため、タイトルへ戻ります");

        AsyncOperation operation = SceneManager.LoadSceneAsync(TITLE_SCENE_NAME);
        if (operation == null) { SceneTransitionGate.Abort(); }
    }

    private void ResetIdleTimer()
    {
        lastInputTime = Time.unscaledTime;
    }

    /// <summary>
    /// コントローラー (スライダー) のタッチも「操作あり」とみなすため、入力を購読する
    /// </summary>
    private void TryBindSliderInput()
    {
        if (sliderSubscription != null) { return; }

        RootLifetimeScope rootScope = FindObjectOfType<RootLifetimeScope>();
        if (rootScope == null || rootScope.Container == null) { return; }

        try
        {
            ISliderInputGetter sliderInputGetter = rootScope.Container.Resolve<ISliderInputGetter>();
            sliderSubscription = sliderInputGetter.OnSliderTouchDown.Subscribe(_ => ResetIdleTimer());
        }
        catch (Exception e)
        {
            Debug.LogWarning($"【System】スライダーの入力を取得できないため、放置の判定はキーボード・マウスのみで行います: {e.Message}");
        }
    }

    private static bool Contains(string[] sceneNames, string sceneName)
    {
        return Array.IndexOf(sceneNames, sceneName) >= 0;
    }
}
