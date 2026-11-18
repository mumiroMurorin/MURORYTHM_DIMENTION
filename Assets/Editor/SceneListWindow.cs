using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// プロジェクト内のシーンを一覧で表示し、クリックでそのシーンを開くウィンドウ。
/// メニューの Tools > シーン一覧 から開く。タブとして好きな場所にドッキングできる。
/// <para>
/// ・左クリック: そのシーンを開く (変更があれば保存するか確認する)
/// ・右クリック: 追加で開く / Project ウィンドウで表示
/// </para>
/// </summary>
public sealed class SceneListWindow : EditorWindow
{
    const string MENU_PATH = "Tools/シーン一覧";
    const string WINDOW_TITLE = "シーン一覧";
    const float ROW_HEIGHT = 22f;

    // 外部アセット (サンプルシーンなど) のフォルダ。「外部アセット」をオンにしたときだけ一覧に出す
    static readonly string[] ExternalAssetFolders =
    {
        "Assets/Imports/",
        "Assets/Samples/",
        "Assets/Plugins/",
        "Assets/NaughtyAttributes/",
        "Assets/StandaloneFileBrowser/",
        "Assets/YughuesFreeGroundMaterials/",
        "Assets/TextMesh Pro/",
        "Assets/Deform/",
    };

    class SceneEntry
    {
        public string Path;
        public string Name;
        public bool IsInBuildSettings;
        public bool IsEnabledInBuild;
    }

    readonly List<SceneEntry> buildScenes = new List<SceneEntry>();
    readonly List<SceneEntry> projectScenes = new List<SceneEntry>();
    readonly List<SceneEntry> externalScenes = new List<SceneEntry>();

    string searchText = "";
    bool showExternalScenes;
    Vector2 scrollPosition;

    GUIStyle sceneButtonStyle;
    GUIStyle activeSceneButtonStyle;
    Texture sceneIcon;

    // プロジェクトごとに設定を分ける
    string ShowExternalPrefKey => "SceneListWindow.ShowExternalScenes." + Application.dataPath;

    [MenuItem(MENU_PATH)]
    static void Open()
    {
        SceneListWindow window = GetWindow<SceneListWindow>();
        window.minSize = new Vector2(180f, 120f);
        window.Show();
    }

    private void OnEnable()
    {
        sceneIcon = EditorGUIUtility.IconContent("SceneAsset Icon").image;
        titleContent = new GUIContent(WINDOW_TITLE, sceneIcon);
        showExternalScenes = EditorPrefs.GetBool(ShowExternalPrefKey, false);

        EditorBuildSettings.sceneListChanged += OnBuildSceneListChanged;
        EditorSceneManager.activeSceneChangedInEditMode += OnActiveSceneChanged;

        RefreshSceneList();
    }

    private void OnDisable()
    {
        EditorBuildSettings.sceneListChanged -= OnBuildSceneListChanged;
        EditorSceneManager.activeSceneChangedInEditMode -= OnActiveSceneChanged;
    }

    // シーンの追加・削除・名前変更に追従する
    private void OnProjectChange()
    {
        RefreshSceneList();
        Repaint();
    }

    private void OnFocus()
    {
        RefreshSceneList();
    }

    private void OnBuildSceneListChanged()
    {
        RefreshSceneList();
        Repaint();
    }

    private void OnActiveSceneChanged(Scene previous, Scene next)
    {
        Repaint();
    }

    private void RefreshSceneList()
    {
        buildScenes.Clear();
        projectScenes.Clear();
        externalScenes.Clear();

        // ビルド設定に入っているシーン (ビルド設定の順 = ゲームの流れの順)
        HashSet<string> buildScenePaths = new HashSet<string>();
        foreach (EditorBuildSettingsScene buildScene in EditorBuildSettings.scenes)
        {
            if (string.IsNullOrEmpty(buildScene.path) || !File.Exists(buildScene.path)) { continue; }

            buildScenePaths.Add(buildScene.path);
            buildScenes.Add(CreateEntry(buildScene.path, true, buildScene.enabled));
        }

        // それ以外のシーン (自分で作ったものと、外部アセットのものに分ける)
        foreach (string guid in AssetDatabase.FindAssets("t:SceneAsset", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (buildScenePaths.Contains(path)) { continue; }

            SceneEntry entry = CreateEntry(path, false, false);
            if (IsExternalAsset(path)) { externalScenes.Add(entry); }
            else { projectScenes.Add(entry); }
        }

        projectScenes.Sort(CompareByPath);
        externalScenes.Sort(CompareByPath);
    }

    private static SceneEntry CreateEntry(string path, bool isInBuildSettings, bool isEnabledInBuild)
    {
        return new SceneEntry
        {
            Path = path,
            Name = System.IO.Path.GetFileNameWithoutExtension(path),
            IsInBuildSettings = isInBuildSettings,
            IsEnabledInBuild = isEnabledInBuild,
        };
    }

    private static bool IsExternalAsset(string path)
    {
        return ExternalAssetFolders.Any(folder => path.StartsWith(folder, StringComparison.Ordinal));
    }

    private static int CompareByPath(SceneEntry a, SceneEntry b)
    {
        return string.Compare(a.Path, b.Path, StringComparison.OrdinalIgnoreCase);
    }

    private void OnGUI()
    {
        EnsureStyles();
        DrawToolbar();

        bool isPlaying = EditorApplication.isPlayingOrWillChangePlaymode;
        if (isPlaying)
        {
            EditorGUILayout.HelpBox("再生中はシーンを切り替えられません。再生を止めてから選んでください。", MessageType.Info);
        }

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        using (new EditorGUI.DisabledScope(isPlaying))
        {
            string activeScenePath = SceneManager.GetActiveScene().path;

            int shownCount = 0;
            shownCount += DrawSection("ビルド設定のシーン", buildScenes, activeScenePath);
            shownCount += DrawSection("その他のシーン", projectScenes, activeScenePath);
            if (showExternalScenes)
            {
                shownCount += DrawSection("外部アセットのシーン", externalScenes, activeScenePath);
            }

            if (shownCount == 0)
            {
                EditorGUILayout.Space(8f);
                EditorGUILayout.LabelField("該当するシーンがありません", EditorStyles.centeredGreyMiniLabel);
            }
        }
        EditorGUILayout.EndScrollView();
    }

    private void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            searchText = GUILayout.TextField(searchText, EditorStyles.toolbarSearchField, GUILayout.MinWidth(60f));

            bool showExternal = GUILayout.Toggle(
                showExternalScenes,
                new GUIContent("外部アセット", "外部アセット (Imports フォルダなど) のサンプルシーンも表示する"),
                EditorStyles.toolbarButton,
                GUILayout.Width(80f));
            if (showExternal != showExternalScenes)
            {
                showExternalScenes = showExternal;
                EditorPrefs.SetBool(ShowExternalPrefKey, showExternal);
            }

            if (GUILayout.Button(new GUIContent("更新", "シーン一覧を読み直す"), EditorStyles.toolbarButton, GUILayout.Width(40f)))
            {
                RefreshSceneList();
            }
        }
    }

    /// <summary>
    /// 見出しとシーンの行を描く。戻り値は表示した行の数。
    /// </summary>
    private int DrawSection(string title, List<SceneEntry> entries, string activeScenePath)
    {
        List<SceneEntry> visibleEntries = entries.Where(MatchesSearch).ToList();
        if (visibleEntries.Count == 0) { return 0; }

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField($"{title} ({visibleEntries.Count})", EditorStyles.boldLabel);

        foreach (SceneEntry entry in visibleEntries)
        {
            DrawSceneRow(entry, entry.Path == activeScenePath);
        }

        return visibleEntries.Count;
    }

    private bool MatchesSearch(SceneEntry entry)
    {
        if (string.IsNullOrEmpty(searchText)) { return true; }

        return entry.Name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0
            || entry.Path.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void DrawSceneRow(SceneEntry entry, bool isActive)
    {
        string label = entry.Name;
        if (entry.IsInBuildSettings && !entry.IsEnabledInBuild) { label += "  (ビルドでは無効)"; }
        if (isActive) { label += "  (開いています)"; }

        GUIContent content = new GUIContent(label, sceneIcon, entry.Path);
        GUIStyle style = isActive ? activeSceneButtonStyle : sceneButtonStyle;
        Rect rect = GUILayoutUtility.GetRect(content, style, GUILayout.ExpandWidth(true), GUILayout.Height(ROW_HEIGHT));

        // 右クリックはメニューを出す (ボタンより先に処理する)
        Event current = Event.current;
        if (current.type == EventType.ContextClick && rect.Contains(current.mousePosition))
        {
            ShowContextMenu(entry);
            current.Use();
            return;
        }

        if (GUI.Button(rect, content, style))
        {
            OpenScene(entry.Path, OpenSceneMode.Single);
        }
    }

    private void ShowContextMenu(SceneEntry entry)
    {
        GenericMenu menu = new GenericMenu();
        menu.AddItem(new GUIContent("開く"), false, () => OpenScene(entry.Path, OpenSceneMode.Single));
        menu.AddItem(new GUIContent("追加で開く (今のシーンを閉じない)"), false, () => OpenScene(entry.Path, OpenSceneMode.Additive));
        menu.AddSeparator("");
        menu.AddItem(new GUIContent("Project ウィンドウで表示"), false, () =>
        {
            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(entry.Path);
            if (sceneAsset == null) { return; }

            Selection.activeObject = sceneAsset;
            EditorGUIUtility.PingObject(sceneAsset);
        });
        menu.ShowAsContext();
    }

    private static void OpenScene(string path, OpenSceneMode mode)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) { return; }
        if (!File.Exists(path))
        {
            Debug.LogWarning($"[シーン一覧] シーンが見つかりません: {path}");
            return;
        }

        // 今のシーンを閉じるときは、変更を保存するか確認する (キャンセルされたら開かない)
        if (mode == OpenSceneMode.Single && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) { return; }

        EditorSceneManager.OpenScene(path, mode);
    }

    private void EnsureStyles()
    {
        if (sceneButtonStyle != null) { return; }

        sceneButtonStyle = new GUIStyle(GUI.skin.button)
        {
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(6, 6, 2, 2),
            imagePosition = ImagePosition.ImageLeft,
        };

        activeSceneButtonStyle = new GUIStyle(sceneButtonStyle)
        {
            fontStyle = FontStyle.Bold,
        };
    }
}
