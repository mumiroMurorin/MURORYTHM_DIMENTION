using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using System.Linq;

[CreateAssetMenu(fileName = "OperationInPhase", menuName = "ScriptableObject/OperationInPhase")]
public class OperationInPhase : ScriptableObject
{
    [Label("対応シーン")]
    [SerializeField] SceneTag scene;

    #region =================== Title ===================

    [ShowIf("scene", SceneTag.TitleScene)] [Label("対応フェーズ")]
    [SerializeField] PhaseStatusInTitleScene phaseStatusTitleScene;

    public bool CheckCondition(PhaseStatusInTitleScene phase)
    {
        return scene == SceneTag.TitleScene && phase == phaseStatusTitleScene;
    }

    #endregion
    #region =================== Lobby ===================

    [ShowIf("scene", SceneTag.LobbyScene)] [Label("対応フェーズ")]
    [SerializeField] PhaseStatusInLobbyScene phaseStatusLobbyScene;

    public bool CheckCondition(PhaseStatusInLobbyScene phase)
    {
        return scene == SceneTag.LobbyScene && phase == phaseStatusLobbyScene;
    }

    #endregion
    #region =================== Select ===================

    [ShowIf("scene", SceneTag.SelectScene)] [Label("対応フェーズ")]
    [SerializeField] PhaseStatusInSelectScene phaseStatusSelectScene;

    public bool CheckCondition(PhaseStatusInSelectScene phase)
    {
        return scene == SceneTag.SelectScene && phase == phaseStatusSelectScene;
    }
    
    #endregion
    #region =================== RhythmGame ===================

    [ShowIf("scene", SceneTag.RhythmGameScene)] [Label("対応フェーズ")]
    [SerializeField] PhaseStatusInRhythmGame phaseStatusRhythmGameScene;

    public bool CheckCondition(PhaseStatusInRhythmGame phase)
    {
        return scene == SceneTag.SelectScene && phase == phaseStatusRhythmGameScene;
    }

    #endregion
    #region =================== Result ===================

    [ShowIf("scene", SceneTag.ResultScene)] [Label("対応フェーズ")]
    [SerializeField] PhaseStatusInResultScene phaseStatusResultScene;

    public bool CheckCondition(PhaseStatusInResultScene phase)
    {
        return scene == SceneTag.ResultScene && phase == phaseStatusResultScene;
    }

    #endregion


    [SerializeField] float delaySeconds = 0.5f;
    [SerializeField] OperationAssetGroup[] operationAssetGroups;

    public IEnumerable<OperationAssetGroup> AssetGroups { get { return operationAssetGroups; } }

    public float DelaySeconds { get { return delaySeconds; } }
}

[System.Serializable]
public class OperationAssetGroup
{
    [Label("タッチ後のクールタイム")]
    [SerializeField] float coolTime = 0.2f;
    [SerializeField] OperationAsset[] operations;

    public IEnumerable<OperationAsset> Operations { get { return operations; } }

    public SliderCoolDownHandler SliderCoolDownHandler { get { return new SliderCoolDownHandler(coolTime); } }
}

[System.Serializable]
public class OperationAsset
{
    [Label("操作タグ")]
    [SerializeField] OperationTag tag;

    [Label("対応するスライダー番号")]
    [MinValue(0), MaxValue(15)] [SerializeField] int leftEdge;
    [MinValue(0), MaxValue(15)] [SerializeField] int rightEdge;

    [Label("テキスト、スライダーの色")]
    [SerializeField] Color themeColor = Color.red;

    [Label("対応テキスト")]
    [SerializeField] string text;

    
    public OperationTag Tag { get { return tag; } }

    public int[] SliderIndices { get { return Enumerable.Range(leftEdge, rightEdge - leftEdge + 1).ToArray(); } }

    public Color ThemeColor { get { return themeColor; } }

    public string Text { get { return text; } }
}

public enum OperationTag
{
    // Lobby...100～
    Lobby_PlayTutorial = 150,
    Lobby_SkipTutorial = 151,

    // Select...200～
    Select_SelectMusic = 210,
    Select_MoveRight = 220,
    Select_MoveLeft = 221,
    Select_UpDifficulty = 230,
    Select_DownDifficulty = 231,

    Select_Detail_StartMusic = 250,
    Select_Detail_UnStartableMusic = 251,
    Select_Detail_BackSelectMusic = 260,
    Select_Detail_OpenOption = 270,

    Select_Option_BackMusicDetail = 300,
    Select_Option_MoveRight = 310,
    Select_Option_MoveLeft = 311,
    Select_Option_PlusValue = 320,
    Select_Option_MinusValue = 321,

    // Result...500～
    Result_ResultConfirm = 510,
}

public enum SceneTag
{
    TitleScene = 1,
    LobbyScene = 5,
    SelectScene = 10,
    TutorialScene = 15,
    RhythmGameScene = 20,
    ResultScene = 25,
}