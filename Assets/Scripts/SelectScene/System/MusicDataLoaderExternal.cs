using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;
using VContainer;
using System.IO;
using System.Linq;
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
    [SerializeField] string chartFileNameEasy = "chart_easy.json";
    [SerializeField] string chartFileNameNormal = "chart_normal.json";
    [SerializeField] string chartFileNameHard = "chart_hard.json";
    [SerializeField] string chartFileNameMaster = "chart_master.json";

    [SerializeField] TMPro.TextMeshProUGUI kariTmp;

    private string dataPath;
    CancellationTokenSource cts;
    IMusicDataListSetter dataSetter;
    IMusicDataListGetter dataGetter;

    [Inject]
    public void Construct(IMusicDataListSetter dataSetter, IMusicDataListGetter dataGetter)
    {
        this.dataSetter = dataSetter;
        this.dataGetter = dataGetter;
    }

    public void LoadMusicDataList(Action onFinishedAction)
    {
        // 既に読み込まれている場合(他のシーンから来た時)は読み込み処理を飛ばす
        if(dataGetter.MusicDatasSorted != null && dataGetter.MusicDatasSorted.Count > 0) 
        {
            onFinishedAction.Invoke();
            return;
        }

        if (cts != null)
        {
            cts.Cancel();
            cts.Dispose();
        }
        cts = new CancellationTokenSource();

        LoadMusicDataListAsync(cts.Token, onFinishedAction).Forget();
    }

    private async UniTask LoadMusicDataListAsync(CancellationToken token, Action onFinishAction)
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
        List<UniTask<MusicData>> tasks = new List<UniTask<MusicData>>();
        MusicDataList musicDataList = new MusicDataList();

        // 各サブフォルダをループ
        foreach (string dir in subDirectories)
        {
            // 最後尾の「/」が「\」になっちゃうので置換
            string normalizedPath = dir.Replace("\\", "/");
            tasks.Add(LoadMusicData(normalizedPath, token));
        }

        // 全ての処理が終わるまで待ち
        MusicData[] results = await UniTask.WhenAll(tasks);

        foreach (var result in results)
        {
            if (result != null) { musicDataList.MusicDatas.Add(result); }
        }

        await LoadAudioDatasAsync(musicDataList, cts.Token);
        dataSetter.SetMusicList(musicDataList.MusicDatas);

        onFinishAction.Invoke();
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
            SetChartFileAsync(musicData, path, token)
        };

        // 全ての処理が終わるまで待ち
        bool[] results = await UniTask.WhenAll(tasks);

        // 一つでも処理が失敗したら返す
        if(!results.All(x => x)) { return null; }

        Debug.Log($"【System】{musicData.MusicName} ロード完了: {path}");
        return musicData;
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

        // ステージ
        if(info.StageType == null || !Enum.TryParse<StageType>(info.StageType, ignoreCase: true, out var stageType))
        { Debug.LogWarning($"【System】ステージ情報がありません: {path}"); }
        else
        { musicData.StageType = stageType; }

        // タイプ
        if (info.SymphonyType == null || !Enum.TryParse<SymphonyType>(info.SymphonyType, ignoreCase: true, out var symphonyType))
        { Debug.LogWarning($"【System】タイプ情報がありません: {path}"); }
        else
        { musicData.SymphonyType = symphonyType; }

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
            string clipPath = path + "/" + fileName;

            // フォルダパスの存在確認
            if (File.Exists(clipPath))
            {
                isFindPath = true;
                path = clipPath;
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
            string samplePath = path + "/" + fileName;

            // フォルダパスの存在確認
            if (File.Exists(samplePath))
            {
                isFindPath = true;
                path = samplePath;
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

    /// <summary>
    /// 譜面データ(パス)の取得
    /// </summary>
    /// <param name="musicData"></param>
    /// <param name="path"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    private async UniTask<bool> SetChartFileAsync(MusicData musicData, string path, CancellationToken token)
    {
        Debug.Log($"{musicData.MusicName} => {musicData.GetDifficulty(Difficulty.Easy)},{musicData.GetDifficulty(Difficulty.Normal)},{musicData.GetDifficulty(Difficulty.Hard)},{musicData.GetDifficulty(Difficulty.Master)}");

        // リストにあるファイルが存在するか確認
        string pathEasy = path + "/" + chartFileNameEasy;
        bool isExistEasy = musicData.GetDifficulty(Difficulty.Easy) >= 0;
        bool isExistNormal = musicData.GetDifficulty(Difficulty.Normal) >= 0;
        bool isExistHard = musicData.GetDifficulty(Difficulty.Hard) >= 0;
        bool isExistMaster = musicData.GetDifficulty(Difficulty.Master) >= 0;

        if (isExistEasy && File.Exists(pathEasy))
        {
            musicData.SetChartPath(Difficulty.Easy, pathEasy);
            Debug.Log($"【System】Easy譜面path読み込み: {musicData.MusicName}");
        }
        else
        {
            musicData.SetDifficulty(Difficulty.Easy, -1);
        }

        string pathNormal = path + "/" + chartFileNameNormal;
        if (isExistNormal && File.Exists(pathNormal))
        {
            musicData.SetChartPath(Difficulty.Normal, pathNormal);
            Debug.Log($"【System】Normal譜面path読み込み: {musicData.MusicName}");
        }
        else
        {
            musicData.SetDifficulty(Difficulty.Normal, -1);
        }

        string pathHard = path + "/" + chartFileNameHard;
        if (isExistHard && File.Exists(pathHard))
        {
            musicData.SetChartPath(Difficulty.Hard, pathHard);
            Debug.Log($"【System】Hard譜面path読み込み: {musicData.MusicName}");
        }
        else
        {
            musicData.SetDifficulty(Difficulty.Hard, -1);
        }

        string pathMaster = path + "/" + chartFileNameMaster;
        if (isExistMaster && File.Exists(pathMaster))
        {
            musicData.SetChartPath(Difficulty.Master, pathMaster);
            Debug.Log($"【System】Master譜面path読み込み: {musicData.MusicName}");
        }
        else
        {
            musicData.SetDifficulty(Difficulty.Master, -1);
        }

        // 無理やり
        await UniTask.Delay(1, cancellationToken: token);
        return true;
    }

    /// <summary>
    /// 楽曲のロードを非同期で行う
    /// </summary>
    /// <param name="onEndAction"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    private async UniTask LoadAudioDatasAsync(MusicDataList musicDataList, CancellationToken token)
    {
        foreach (var data in musicDataList.MusicDatas)
        {
            if (data.SampleClip.loadState == AudioDataLoadState.Loaded) { continue; }

            data.SampleClip.LoadAudioData();
            await UniTask.WaitUntil(() => data.SampleClip.loadState == AudioDataLoadState.Loaded, cancellationToken: token);
        }
    }

    private void OnDestroy()
    {
        if (cts != null)
        {
            cts.Cancel();
            cts.Dispose();
            cts = null;
        }
    }

    /// <summary>
    /// 楽曲名などの情報
    /// </summary>
    public class MusicInformation
    {
        public string MusicName { get; set; }
        public string ComposerName { get; set; }
        public string ChartDesigner { get; set; }
        public string SymphonyType { get; set; }
        public string StageType { get; set; }
        public int[] Difficulties { get; set; }
    }
}
