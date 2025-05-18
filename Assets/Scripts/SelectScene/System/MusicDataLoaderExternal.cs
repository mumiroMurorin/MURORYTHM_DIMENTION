using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using System.IO;

public class MusicDataLoaderExternal : MonoBehaviour, IMusicDataListLoader
{
    [SerializeField] string dataFolderName;
    [SerializeField] TMPro.TextMeshProUGUI kariTmp;

    private string dataPath;

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
        if (Directory.Exists(dataPath))
        {
            // サブフォルダのパス一覧を取得して一つずつ取り出す
            string[] subDirectories = Directory.GetDirectories(dataPath);

            // 各サブフォルダをループ
            foreach (string dir in subDirectories)
            {
                LoadMusicData(dir);
            }
        }
        else
        {
            Debug.LogWarning("【System】指定されたフォルダが存在しません: " + dataPath);
            return;
        }
    }

    /// <summary>
    /// ディレクトリからMusicDataを生成して返す
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    private MusicData LoadMusicData(string path)
    {
         Debug.Log("【System】MusicDataロード開始:" + path);
         
    }

    private bool SetMusicInformation(ref MusicData musicData, string path)
    {

    }

    void IMusicDataListLoader.LoadAudioDatas(Action onEndAction)
    {

    }
}
