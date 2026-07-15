using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StageController : MonoBehaviour, IStageController
{
    [SerializeField] CharacterSpawner characterSpawner;
    [SerializeField] SymphonyTypePresentationDatabase symphonyTypePresentationDatabase;

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
        string difString = GetDifficultyText(musicDataGetter);

        var diffObj = characterSpawner.SpawnCharacter(difString, difficultySettings);
        diffObj.transform.SetParent(difficultyParent);
        diffObj.transform.localPosition = Vector3.zero;
        diffObj.transform.localEulerAngles = Vector3.zero;
        difficultyOutline.ApplyOutline(diffObj);
    }

    private string GetDifficultyText(IMusicDataGetter musicDataGetter)
    {
        if (musicDataGetter.Difficulty.Value != Difficulty.Master)
        {
            return musicDataGetter.Difficulty.Value.ToString().ToUpper();
        }

        SymphonyType symphonyType = musicDataGetter.Music.Value.SymphonyType;
        string masterDifficultyText = symphonyTypePresentationDatabase?.GetMasterDifficultyText(symphonyType);
        if (!string.IsNullOrEmpty(masterDifficultyText))
        {
            return masterDifficultyText;
        }

        Debug.LogWarning($"[StageController] Master difficulty text is not set: {symphonyType}");
        return Difficulty.Master.ToString().ToUpper();
    }
}

public interface IStageController
{
    void Initialize(IMusicDataGetter musicDataGetter);
}
