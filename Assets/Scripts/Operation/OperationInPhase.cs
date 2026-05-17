using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using UnityEngine;

[CreateAssetMenu(fileName = "OperationInPhase", menuName = "ScriptableObject/OperationInPhase")]
public class OperationInPhase : ScriptableObject
{
    [Label("対応シーン")]
    [SerializeField] private SceneTag scene;

    #region Title

    [ShowIf("scene", SceneTag.TitleScene)]
    [Label("対応フェーズ")]
    [SerializeField] private PhaseStatusInTitleScene phaseStatusTitleScene;

    public bool CheckCondition(PhaseStatusInTitleScene phase)
    {
        return scene == SceneTag.TitleScene && phase == phaseStatusTitleScene;
    }

    #endregion
    #region Lobby

    [ShowIf("scene", SceneTag.LobbyScene)]
    [Label("対応フェーズ")]
    [SerializeField] private PhaseStatusInLobbyScene phaseStatusLobbyScene;

    public bool CheckCondition(PhaseStatusInLobbyScene phase)
    {
        return scene == SceneTag.LobbyScene && phase == phaseStatusLobbyScene;
    }

    #endregion
    #region Select

    [ShowIf("scene", SceneTag.SelectScene)]
    [Label("対応フェーズ")]
    [SerializeField] private PhaseStatusInSelectScene phaseStatusSelectScene;

    public bool CheckCondition(PhaseStatusInSelectScene phase)
    {
        return scene == SceneTag.SelectScene && phase == phaseStatusSelectScene;
    }

    #endregion
    #region RhythmGame

    [ShowIf("scene", SceneTag.RhythmGameScene)]
    [Label("対応フェーズ")]
    [SerializeField] private PhaseStatusInRhythmGame phaseStatusRhythmGameScene;

    public bool CheckCondition(PhaseStatusInRhythmGame phase)
    {
        return scene == SceneTag.RhythmGameScene && phase == phaseStatusRhythmGameScene;
    }

    #endregion
    #region Result

    [ShowIf("scene", SceneTag.ResultScene)]
    [Label("対応フェーズ")]
    [SerializeField] private PhaseStatusInResultScene phaseStatusResultScene;

    public bool CheckCondition(PhaseStatusInResultScene phase)
    {
        return scene == SceneTag.ResultScene && phase == phaseStatusResultScene;
    }

    #endregion
    #region GameOver

    [ShowIf("scene", SceneTag.GameOverScene)]
    [Label("対応フェーズ")]
    [SerializeField] private PhaseStatusInGameOverScene phaseStatusGameOverScene;

    public bool CheckCondition(PhaseStatusInGameOverScene phase)
    {
        return scene == SceneTag.GameOverScene && phase == phaseStatusGameOverScene;
    }

    #endregion

    [SerializeField] private float delaySeconds = 0.5f;
    [SerializeField] private OperationAssetGroup[] operationAssetGroups;

    public IEnumerable<OperationAssetGroup> AssetGroups => operationAssetGroups;

    public float DelaySeconds => delaySeconds;
}

[System.Serializable]
public class OperationAssetGroup
{
    [Label("タッチ後のクールタイム")]
    [SerializeField] private float coolTime = 0.2f;

    [SerializeField] private OperationAssetUnit[] operations;

    public IEnumerable<OperationAssetUnit> Operations => operations;

    public SliderCoolDownHandler SliderCoolDownHandler => new SliderCoolDownHandler(coolTime);
}

public enum OperationTag
{
    Title_WaitingForPlayerInput = 50,
    Lobby_SelectJapanese = 130,
    Lobby_SelectEnglish = 131,
    Lobby_PlayTutorial = 150,
    Lobby_SkipTutorial = 151,
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
    Result_ResultConfirm = 510,
    GameOver_Continue = 990,
    GameOver_FinishGame = 991,
}

public enum SceneTag
{
    TitleScene = 1,
    LobbyScene = 5,
    SelectScene = 10,
    TutorialScene = 15,
    RhythmGameScene = 20,
    ResultScene = 25,
    GameOverScene = 30,
}
