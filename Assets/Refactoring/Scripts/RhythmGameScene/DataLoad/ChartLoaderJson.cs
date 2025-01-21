using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Refactoring
{
    //public class ChartLoaderJson : MonoBehaviour, IChartLoader
    //{
    //    [SerializeField] TextAsset jsonFile;

    //    public async UniTask<ChartData> LoadChartData(TextAsset textAsset)
    //    {
    //        // ぬるぽ
    //        if (jsonFile == null || textAsset == null)
    //        {
    //            Debug.LogError("【System】CSVファイルが参照されていません。");
    //            return null;
    //        }

    //        // JSONファイルをC#クラスに変換
    //        // = JsonUtility.FromJson<MusicData>(textAsset != null ? textAsset.text : jsonFile.text);


    //    }

    //}
}
