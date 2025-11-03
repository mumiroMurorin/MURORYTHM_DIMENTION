using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StageController : MonoBehaviour, IStageController
{
    [SerializeField] CharacterSpawner characterSpawner;

    [Header("タイトルオブジェクト設定")]
    [SerializeField] FlyingTextSettings titleSettings;
    [SerializeField] OutlineSettings titleOutline;
    [SerializeField] Transform titleParent;

    [Header("難易度オブジェクト設定")]
    [SerializeField] FlyingTextSettings difficultySettings;
    [SerializeField] OutlineSettings difficultyOutline;
    [SerializeField] Transform difficultyParent;

    public void Initialize(IMusicDataGetter musicDataGetter)
    {
        // タイトルオブジェクトのスポーン
        var titleObj = characterSpawner.SpawnCharacter(musicDataGetter.Music.Value.MusicName, titleSettings);
        titleObj.transform.SetParent(titleParent);
        titleObj.transform.localPosition = Vector3.zero;
        titleObj.transform.localEulerAngles = Vector3.zero;
        titleOutline.ApplyOutline(titleObj);

        // 難易度オブジェクトのスポーン
        var diffObj = characterSpawner.SpawnCharacter(musicDataGetter.Difficulty.Value.ToString().ToUpper(), difficultySettings);
        diffObj.transform.SetParent(difficultyParent);
        diffObj.transform.localPosition = Vector3.zero;
        diffObj.transform.localEulerAngles = Vector3.zero;
        difficultyOutline.ApplyOutline(diffObj);

    }
}

public interface IStageController
{
    void Initialize(IMusicDataGetter musicDataGetter);
}
