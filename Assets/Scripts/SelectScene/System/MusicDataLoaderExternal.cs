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
using Newtonsoft.Json.Linq;

public class MusicDataLoaderExternal : MonoBehaviour, IMusicDataListLoader
{
    [Header("�y�ȃf�[�^�̃p�X")]
    [Tooltip("�f�[�^�������Ă�t�H���_��")]
    [SerializeField] string dataFolderName;
    [Tooltip("�y�ȃf�[�^�t�@�C����")]
    [SerializeField] string musicInformationFileName = "information.json";
    [Tooltip("�y�ȃW���P�b�g�t�@�C����")]
    [SerializeField] string[] musicJacketFileNames = new string[] { "jacket.png", "jacket.jpg", "jacket.jpeg" };
    [Tooltip("�y�ȃe�[�}�摜�t�@�C����")]
    [SerializeField] string[] musicThemeImageFileNames = new string[] { "theme.png", "theme.jpg", "theme.jpeg" };
    [Tooltip("�y�ȃI�[�f�B�I�t�@�C����")]
    [SerializeField] string[] musicClipFileNames = new string[] { "clip.wav", "clip.mp3", "clip.ogg" };
    [Tooltip("�y�ȃT���v���I�[�f�B�I�t�@�C����")]
    [SerializeField] string[] sampleClipFileNames = new string[] { "sample.wav", "sample.mp3", "sample.ogg" };
    [Header("�t�H�[���o�b�N")]
    [Tooltip("�T���v���I�[�f�B�I�t�@�C�������݂��Ȃ��ꍇ�Ɏg�p���鉹��")]
    [SerializeField] AudioClip fallbackSampleClip;
    [Tooltip("�W���P�b�g�摜�����݂��Ȃ��ꍇ�Ɏg�p����摜")]
    [SerializeField] Sprite fallbackJacketSprite;
    [Tooltip("�e�[�}�摜�����݂��Ȃ��ꍇ�Ɏg�p����摜")]
    [SerializeField] Sprite fallbackThemeSprite;
    [Tooltip("���ʃf�[�^��")]
    [SerializeField] string chartFileNameEasy = "chart_easy.json";
    [SerializeField] string chartFileNameNormal = "chart_normal.json";
    [SerializeField] string chartFileNameHard = "chart_hard.json";
    [SerializeField] string chartFileNameMaster = "chart_master.json";

    string dataPath;
    bool isLoaded;
    CancellationTokenSource cts;
    IMusicDataListSetter dataSetter;
    IMusicDataListGetter dataGetter;

    [Inject]
    public void Construct(IMusicDataListSetter dataSetter, IMusicDataListGetter dataGetter)
    {
        this.dataSetter = dataSetter;
        this.dataGetter = dataGetter;
    }

    public bool CheckLoadedMusicDatas()
    {
        return dataGetter.MusicDatasSorted != null && dataGetter.MusicDatasSorted.Count > 0;
    }

    public void LoadMusicDataList(Action onFinishedAction)
    {
        // ���ɓǂݍ��܂�Ă���ꍇ(���̃V�[�����痈����)�͓ǂݍ��ݏ������΂�
        //if(dataGetter.MusicDatasSorted != null && dataGetter.MusicDatasSorted.Count > 0) 
        //{
        //    onFinishedAction.Invoke();
        //    return;
        // }

        cts?.CancelAndDispose();
        cts = new CancellationTokenSource();

        LoadMusicDataListAsync(cts.Token, onFinishedAction).Forget();
    }

    private async UniTask LoadMusicDataListAsync(CancellationToken token, Action onFinishAction)
    {
        dataPath = Application.dataPath + "/" + dataFolderName;

        // �t�H���_�p�X�̑��݊m�F
        if (!Directory.Exists(dataPath))
        {
            Debug.LogWarning("�ySystem�z�w�肳�ꂽ�t�H���_�����݂��܂���: " + dataPath);
            return;
        }

        // �T�u�t�H���_�̃p�X�ꗗ��擾���Ĉ�����o��
        string[] subDirectories = Directory.GetDirectories(dataPath);
        List<UniTask<MusicData>> tasks = new List<UniTask<MusicData>>();
        MusicDataList musicDataList = new MusicDataList();

        // �e�T�u�t�H���_����[�v
        foreach (string dir in subDirectories)
        {
            // �Ō���́u/�v���u\�v�ɂȂ����Ⴄ�̂Œu��
            string normalizedPath = dir.Replace("\\", "/");
            tasks.Add(LoadMusicData(normalizedPath, token));
        }

        // �S�Ă̏������I���܂ő҂�
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
    /// �f�B���N�g������MusicData�𐶐����ĕԂ�
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    private async UniTask<MusicData> LoadMusicData(string path, CancellationToken token)
    {
        Debug.Log("�ySystem�zMusicData���[�h�J�n:" + path);

        MusicData musicData = new MusicData();
        // �y�ȏ��̃Z�b�g
        if (!SetMusicInformation(musicData, path)) { return null; }

        // �y�ȃf�[�^�̃Z�b�g
        List<UniTask<bool>> tasks = new List<UniTask<bool>>
        {
            SetMusicClipFileAsync(musicData, path, token),
            SetSampleClipFileAsync(musicData, path, token),
            SetJacketFileAsync(musicData, path, token),
            SetThemeImageFileAsync(musicData, path, token),
            SetChartFileAsync(musicData, path, token)
        };

        // �S�Ă̏������I���܂ő҂�
        bool[] results = await UniTask.WhenAll(tasks);

        // clip�͕K�{�A����ȊO�̓t�H�[���o�b�N�œǂݍ��߂邽�߁A�����Ŏ��s�����ꍇ�̂ݏ��O����
        if(!results.All(x => x)) { return null; }

        // �L�^�̓ǂݍ���
        MusicRecordPersistence.LoadAndApply(musicData);

        Debug.Log($"�ySystem�z{musicData.MusicName} ���[�h����: {path}");
        return musicData;
    }

    /// <summary>
    /// �y�ȏ��̎擾�A�Z�b�g
    /// </summary>
    /// <param name="musicData"></param>
    /// <param name="path"></param>
    /// <returns></returns>
    private bool SetMusicInformation(MusicData musicData, string path)
    {
        path = path + "/" + musicInformationFileName;

        // �t�@�C���̑��݊m�F
        if (!File.Exists(path))
        {
            Debug.LogWarning("�ySystem�z�w�肳�ꂽ�t�H���_�����݂��܂���: " + path);
            return false;
        }

        // �y�ȏ��f�[�^�̕ϊ�
        if(!JsonLoader.TryLoadFromJsonFile(path, out MusicInformation info))
        {
            Debug.LogWarning("�ySystem�z�y�ȏ��t�@�C���̕ϊ��Ɏ��s���܂���: " + path);
            return false;
        }

        // �f�[�^�Z�b�g
        musicData.MusicName = RemoveLineBreakSequences(info.MusicName);
        musicData.ComposerName = RemoveLineBreakSequences(info.ComposerName);
        musicData.OtherCreator = SanitizeCreatorNames(info.OtherCreator);
        musicData.ChartDesigners = SanitizeChartDesigners(info.ChartDesigner);
        musicData.SetDifficulty(info.Difficulties);

        // �X�e�[�W
        if(info.StageType == null || !Enum.TryParse<StageType>(info.StageType, ignoreCase: true, out var stageType))
        { Debug.LogWarning($"�ySystem�z�X�e�[�W��񂪂���܂���: {path}"); }
        else
        { musicData.StageType = stageType; }

        // �^�C�v
        if (info.SymphonyType == null || !Enum.TryParse<SymphonyType>(info.SymphonyType, ignoreCase: true, out var symphonyType))
        { Debug.LogWarning($"�ySystem�z�^�C�v��񂪂���܂���: {path}"); }
        else
        { musicData.SymphonyType = symphonyType; }

        return true;
    }

    private string RemoveLineBreakSequences(string text)
    {
        if (string.IsNullOrEmpty(text)) { return text; }

        return text
            .Replace("\\r\\n", "")
            .Replace("\\n", "")
            .Replace("\\r", "")
            .Replace("\r\n", "")
            .Replace("\n", "")
            .Replace("\r", "");
    }

    private string[] SanitizeCreatorNames(string[] names)
    {
        if (names == null) { return new string[0]; }

        for (int i = 0; i < names.Length; i++)
        {
            names[i] = RemoveLineBreakSequences(names[i]);
        }

        return names;
    }

    private string[] SanitizeChartDesigners(JToken chartDesignerToken)
    {
        string[] chartDesigners = new string[4];
        if (chartDesignerToken == null || chartDesignerToken.Type != JTokenType.Array)
        {
            return chartDesigners;
        }

        var chartDesignerArray = (JArray)chartDesignerToken;
        for (int i = 0; i < chartDesigners.Length && i < chartDesignerArray.Count; i++)
        {
            if (chartDesignerArray[i] == null || chartDesignerArray[i].Type == JTokenType.Null) { continue; }

            chartDesigners[i] = RemoveLineBreakSequences(chartDesignerArray[i].ToString());
        }

        return chartDesigners;
    }
    /// <summary>
    /// �ȃf�[�^�̎擾�A�Z�b�g(�񓯊�)
    /// </summary>
    /// <param name="musicData"></param>
    /// <param name="path"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    private async UniTask<bool> SetMusicClipFileAsync(MusicData musicData, string path, CancellationToken token)
    {
        // ���X�g�ɂ���t�@�C�������݂��邩�m�F
        bool isFindPath = false;
        foreach(var fileName in musicClipFileNames)
        {
            string clipPath = path + "/" + fileName;

            // �t�H���_�p�X�̑��݊m�F
            if (File.Exists(clipPath))
            {
                isFindPath = true;
                path = clipPath;
                break;
            }
        }

        // ������Ȃ������ꍇ�͊y�ȃf�[�^�Ƃ��ēǂݍ��܂Ȃ�
        if (!isFindPath)
        {
            Debug.LogWarning("�ySystem�z�y�ȃI�[�f�B�I�t�@�C�������݂��Ȃ����ߊy�ȓǂݍ��݂�X�L�b�v���܂�: " + path);
            return false;
        }

        // �y�Ȃ̓ǂݍ���
        musicData.MusicClip = await AudioFileSelector.LoadAudioClip(path, token);

        return true;
    }

    /// <summary>
    /// �y�ȃT���v���̎擾�A�Z�b�g(�񓯊�)
    /// </summary>
    /// <param name="musicData"></param>
    /// <param name="path"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    private async UniTask<bool> SetSampleClipFileAsync(MusicData musicData, string path, CancellationToken token)
    {
        // ���X�g�ɂ���t�@�C�������݂��邩�m�F
        bool isFindPath = false;
        foreach (var fileName in sampleClipFileNames)
        {
            string samplePath = path + "/" + fileName;

            // �t�H���_�p�X�̑��݊m�F
            if (File.Exists(samplePath))
            {
                isFindPath = true;
                path = samplePath;
                break;
            }
        }

        // ������Ȃ������ꍇ�̓t�H�[���o�b�N��g�p����
        if (!isFindPath)
        {
            Debug.LogWarning("�ySystem�z�y�ȃT���v���I�[�f�B�I�t�@�C�������݂��Ȃ����߃t�H�[���o�b�N��g�p���܂�: " + path);
            musicData.SampleClip = fallbackSampleClip;
            return true;
        }

        // �y�Ȃ̓ǂݍ���
        musicData.SampleClip = await AudioFileSelector.LoadAudioClip(path, token);

        return true;
    }

    /// <summary>
    /// �y�ȃW���P�b�g�̎擾�A�Z�b�g(�񓯊�)
    /// </summary>
    /// <param name="musicData"></param>
    /// <param name="path"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    private async UniTask<bool> SetJacketFileAsync(MusicData musicData, string path, CancellationToken token)
    {
        if (!TryFindFilePath(path, musicJacketFileNames, out var jacketPath))
        {
            Debug.LogWarning("�ySystem�z�y�ȃW���P�b�g�摜�����݂��Ȃ����߃t�H�[���o�b�N��g�p���܂�: " + path);
            musicData.MusicSprite = fallbackJacketSprite;
            return true;
        }

        musicData.MusicSprite = await ImageLoader.LoadSpriteFromPathAsync(jacketPath, token);
        return true;
    }

    /// <summary>
    /// �y�ȃe�[�}�̎擾�A�Z�b�g(�񓯊�)
    /// </summary>
    /// <param name="musicData"></param>
    /// <param name="path"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    private async UniTask<bool> SetThemeImageFileAsync(MusicData musicData, string path, CancellationToken token)
    {
        if (!TryFindFilePath(path, musicThemeImageFileNames, out var themeImagePath))
        {
            Debug.LogWarning("�ySystem�z�y�ȃe�[�}�摜�����݂��Ȃ����߃t�H�[���o�b�N��g�p���܂�: " + path);
            musicData.ThemeSprite = fallbackThemeSprite;
            return true;
        }

        musicData.ThemeSprite = await ImageLoader.LoadSpriteFromPathAsync(themeImagePath, token);
        return true;
    }

    private bool TryFindFilePath(string directoryPath, string[] fileNames, out string filePath)
    {
        foreach (var fileName in fileNames)
        {
            string candidatePath = directoryPath + "/" + fileName;

            if (File.Exists(candidatePath))
            {
                filePath = candidatePath;
                return true;
            }
        }

        filePath = null;
        return false;
    }

    /// <summary>
    /// ���ʃf�[�^(�p�X)�̎擾
    /// </summary>
    /// <param name="musicData"></param>
    /// <param name="path"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    private async UniTask<bool> SetChartFileAsync(MusicData musicData, string path, CancellationToken token)
    {
        // ���X�g�ɂ���t�@�C�������݂��邩�m�F
        string pathEasy = path + "/" + chartFileNameEasy;
        bool isExistEasy = musicData.GetDifficulty(Difficulty.Easy) >= 0;
        bool isExistNormal = musicData.GetDifficulty(Difficulty.Normal) >= 0;
        bool isExistHard = musicData.GetDifficulty(Difficulty.Hard) >= 0;
        bool isExistMaster = musicData.GetDifficulty(Difficulty.Master) >= 0;

        if (isExistEasy && File.Exists(pathEasy))
        {
            musicData.SetChartPath(Difficulty.Easy, pathEasy);
            Debug.Log($"�ySystem�zEasy����path�ǂݍ���: {musicData.MusicName}");
        }
        else
        {
            musicData.SetDifficulty(Difficulty.Easy, -1);
        }

        string pathNormal = path + "/" + chartFileNameNormal;
        if (isExistNormal && File.Exists(pathNormal))
        {
            musicData.SetChartPath(Difficulty.Normal, pathNormal);
            Debug.Log($"�ySystem�zNormal����path�ǂݍ���: {musicData.MusicName}");
        }
        else
        {
            musicData.SetDifficulty(Difficulty.Normal, -1);
        }

        string pathHard = path + "/" + chartFileNameHard;
        if (isExistHard && File.Exists(pathHard))
        {
            musicData.SetChartPath(Difficulty.Hard, pathHard);
            Debug.Log($"�ySystem�zHard����path�ǂݍ���: {musicData.MusicName}");
        }
        else
        {
            musicData.SetDifficulty(Difficulty.Hard, -1);
        }

        string pathMaster = path + "/" + chartFileNameMaster;
        if (isExistMaster && File.Exists(pathMaster))
        {
            musicData.SetChartPath(Difficulty.Master, pathMaster);
            Debug.Log($"�ySystem�zMaster����path�ǂݍ���: {musicData.MusicName}");
        }
        else
        {
            musicData.SetDifficulty(Difficulty.Master, -1);
        }

        // �������
        await UniTask.Delay(1, cancellationToken: token);
        return true;
    }

    /// <summary>
    /// �y�Ȃ̃��[�h��񓯊��ōs��
    /// </summary>
    /// <param name="onEndAction"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    private async UniTask LoadAudioDatasAsync(MusicDataList musicDataList, CancellationToken token)
    {
        foreach (var data in musicDataList.MusicDatas)
        {
            if (data.SampleClip == null)
            {
                Debug.LogWarning($"�ySystem�zSampleClip���ݒ肳��Ă��Ȃ����ߎ��O���[�h��X�L�b�v���܂�: {data.MusicName}");
                continue;
            }

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
    /// �y�Ȗ��Ȃǂ̏��
    /// </summary>
    public class MusicInformation
    {
        public string MusicName { get; set; }
        public string ComposerName { get; set; }
        public string[] OtherCreator { get; set; }
        public JToken ChartDesigner { get; set; }
        public string SymphonyType { get; set; }
        public string StageType { get; set; }
        public int[] Difficulties { get; set; }
    }
}
