using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;
using VContainer;
using System.IO;
using JsonUtil;

public class MusicDataLoaderExternal : MonoBehaviour, IMusicDataListLoader
{
    [Header("楽曲データのパス")]
    [Tooltip("データが入ってるフォルダ名")]
    [SerializeField] string dataFolderName;
    [Tooltip("楽曲データファイル名")]
    [SerializeField] string musicInformationFileName = "information.json";
    [Tooltip("楽曲ジャケットファイル名")]
    [SerializeField] string musicJacketFileName = "jacket.png";
    [Tooltip("楽曲テーマ画像ファイル名")]
    [SerializeField] string musicThemeImageFileName = "theme.png";
    [Tooltip("楽曲オーディオファイル名")]
    [SerializeField] string[] musicClipFileNames = new string[] { "clip.wav", "clip.mp3", "clip.ogg" };
    [Tooltip("楽曲サンプルオーディオファイル名")]
    [SerializeField] string[] sampleClipFileNames = new string[] { "sample.wav", "sample.mp3", "sample.ogg" };
    [Tooltip("譜面データ名")]
    [SerializeField] string[] chartFileNames = new string[] { "chart_initiate.json", "chart_fanatic.json", "chart_skyclad.json", "chart_dream.json" };

    [SerializeField] TMPro.TextMeshProUGUI kariTmp;

    private string dataPath;
    CancellationTokenSource cts;
    ISelectSceneDataSetter selectSceneDataSetter;

    [Inject]
    public void Construct(ISelectSceneDataSetter selectSceneDataSetter)
    {
        this.selectSceneDataSetter = selectSceneDataSetter;
    }

    void Start()
    {
        LoadMusicDataList();
    }

    public void LoadMusicDataList()
    {
        dataPath = Application.dataPath + "/" + dataFolderName;

        // フォルダパスの存在確認
        if (!Directory.Exists(dataPath))
        {
            Debug.LogWarning("【System】指定されたフォルダが存在しません: " + dataPath);
            return;
        }

        // サブフォルダのパス一覧を取得して一つずつ取り出す
        string[] subDirectories = Directory.GetDirectories(dataPath);
        MusicDataList musicDataList = new MusicDataList();

        if(cts != null)
        {
            cts.Cancel();
            cts.Dispose();
        }
        cts = new CancellationTokenSource();

        // 各サブフォルダをループ
        foreach (string dir in subDirectories)
        {
            // 最後尾の「/」が「\」になっちゃうので置換
            string normalizedPath = dir.Replace("\\", "/");

            //MusicData musicData = await LoadMusicData(normalizedPath, cts.Token);
            //if (musicData != null) { musicDataList.MusicDatas.Add(musicData); }
        }
    }

    /// <summary>
    /// ディレクトリからMusicDataを生成して返す
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    private async UniTask<MusicData> LoadMusicData(string path, CancellationToken token)
    {
        Debug.Log("【System】MusicDataロード開始:" + path);

        MusicData musicData = new MusicData();
        // 楽曲情報のセット
        if (!SetMusicInformation(musicData, path)) { return null; }

        // 楽曲データのセット
        List<UniTask<bool>> tasks = new List<UniTask<bool>>
        {
            SetMusicClipFileAsync(musicData, path, token),
            SetSampleClipFileAsync(musicData, path, token),
            SetJacketFileAsync(musicData, path, token),
            SetThemeImageFileAsync(musicData, path, token),
        };

        // 全ての処理が終わるまで待ち
        bool[] results = await UniTask.WhenAll(tasks);

        return null;
    }

    /// <summary>
    /// 楽曲情報の取得、セット
    /// </summary>
    /// <param name="musicData"></param>
    /// <param name="path"></param>
    /// <returns></returns>
    private bool SetMusicInformation(MusicData musicData, string path)
    {
        path = path + "/" + musicInformationFileName;

        // ファイルの存在確認
        if (!File.Exists(path))
        {
            Debug.LogWarning("【System】指定されたフォルダが存在しません: " + path);
            return false;
        }

        // 楽曲情報データの変換
        if(!JsonLoader.TryLoadFromJsonFile(path, out MusicInformation info))
        {
            Debug.LogWarning("【System】楽曲情報ファイルの変換に失敗しました: " + path);
            return false;
        }

        // データセット
        musicData.MusicName = info.MusicName;
        musicData.ComposerName = info.ComposerName;
        musicData.SetDifficulty(info.Difficulties);

        return true;
    }

    /// <summary>
    /// 曲データの取得、セット(非同期)
    /// </summary>
    /// <param name="musicData"></param>
    /// <param name="path"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    private async UniTask<bool> SetMusicClipFileAsync(MusicData musicData, string path, CancellationToken token)
    {
        // リストにあるファイルが存在するか確認
        bool isFindPath = false;
        foreach(var fileName in musicClipFileNames)
        {
            path = path + "/" + fileName;

            // フォルダパスの存在確認
            if (File.Exists(path))
            {
                isFindPath = true;
                break;
            }
        }

        // 見つからなかった場合
        if (!isFindPath)
        {
            Debug.LogWarning("【System】指定されたフォルダが存在しません: " + path);
            return false;
        }

        // 楽曲の読み込み
        musicData.MusicClip = await AudioFileSelector.LoadAudioClip(path, token);

        return true;
    }

    /// <summary>
    /// 楽曲サンプルの取得、セット(非同期)
    /// </summary>
    /// <param name="musicData"></param>
    /// <param name="path"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    private async UniTask<bool> SetSampleClipFileAsync(MusicData musicData, string path, CancellationToken token)
    {
        // リストにあるファイルが存在するか確認
        bool isFindPath = false;
        foreach (var fileName in sampleClipFileNames)
        {
            path = path + "/" + fileName;

            // フォルダパスの存在確認
            if (File.Exists(path))
            {
                isFindPath = true;
                break;
            }
        }

        // 見つからなかった場合
        if (!isFindPath)
        {
            Debug.LogWarning("【System】指定されたフォルダが存在しません: " + path);
            return false;
        }

        // 楽曲の読み込み
        musicData.SampleClip = await AudioFileSelector.LoadAudioClip(path, token);

        return true;
    }

    /// <summary>
    /// 楽曲ジャケットの取得、セット(非同期)
    /// </summary>
    /// <param name="musicData"></param>
    /// <param name="path"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    private async UniTask<bool> SetJacketFileAsync(MusicData musicData, string path, CancellationToken token)
    {
        path = path + "/" + musicJacketFileName;

        // ファイルの存在確認
        if (!File.Exists(path))
        {
            Debug.LogWarning("【System】指定されたフォルダが存在しません: " + path);
            return false;
        }

        musicData.MusicSprite = await ImageLoader.LoadSpriteFromPathAsync(path, token);
        return true;
    }

    /// <summary>
    /// 楽曲テーマの取得、セット(非同期)
    /// </summary>
    /// <param name="musicData"></param>
    /// <param name="path"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    private async UniTask<bool> SetThemeImageFileAsync(MusicData musicData, string path, CancellationToken token)
    {
        path = path + "/" + musicThemeImageFileName;

        // ファイルの存在確認
        if (!File.Exists(path))
        {
            Debug.LogWarning("【System】指定されたフォルダが存在しません: " + path);
            return false;
        }

        musicData.ThemeSprite = await ImageLoader.LoadSpriteFromPathAsync(path, token);
        return true;
    }

    //private async UniTask<bool> SetChartFileAsync(MusicData musicData, string path, CancellationToken token)
    //{
    //    // リストにあるファイルが存在するか確認
    //    bool isFindPath = false;
    //    foreach (var fileName in chartFileNames)
    //    {
    //        path = path + "/" + fileName;

    //        // フォルダパスの存在確認
    //        if (File.Exists(path))
    //        {
    //            isFindPath = true;
    //            break;
    //        }
    //    }

    //    // 見つからなかった場合
    //    if (!isFindPath)
    //    {
    //        Debug.LogWarning("【System】指定されたフォルダが存在しません: " + path);
    //        return false;
    //    }
    //}

    void IMusicDataListLoader.LoadAudioDatas(Action onEndAction)
    {

    }

    /// <summary>
    /// 楽曲名などの情報
    /// </summary>
    public class MusicInformation
    {
        public string MusicName { get; set; }
        public string ComposerName { get; set; }
        public string ChartDesigner { get; set; }
        public int[] Difficulties { get; set; }
    }
}
