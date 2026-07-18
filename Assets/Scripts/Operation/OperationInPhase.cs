using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using UnityEngine;

[CreateAssetMenu(fileName = "OperationInPhase", menuName = "ScriptableObject/OperationInPhase")]
public class OperationInPhase : ScriptableObject
{
    [Label("対象シーン")]
    [SerializeField] private SceneTag scene;

    #region Title

    [ShowIf("scene", SceneTag.TitleScene)]
    [Label("対象フェーズ")]
    [SerializeField] private PhaseStatusInTitleScene phaseStatusTitleScene;

    public bool CheckCondition(PhaseStatusInTitleScene phase)
    {
        return scene == SceneTag.TitleScene && phase == phaseStatusTitleScene;
    }

    #endregion
    #region Lobby

    [ShowIf("scene", SceneTag.LobbyScene)]
    [Label("対象フェーズ")]
    [SerializeField] private PhaseStatusInLobbyScene phaseStatusLobbyScene;

    public bool CheckCondition(PhaseStatusInLobbyScene phase)
    {
        return scene == SceneTag.LobbyScene && phase == phaseStatusLobbyScene;
    }

    #endregion
    #region Select

    [ShowIf("scene", SceneTag.SelectScene)]
    [Label("対象フェーズ")]
    [SerializeField] private PhaseStatusInSelectScene phaseStatusSelectScene;

    public bool CheckCondition(PhaseStatusInSelectScene phase)
    {
        return scene == SceneTag.SelectScene && phase == phaseStatusSelectScene;
    }

    #endregion
    #region Tutorial

    [ShowIf("scene", SceneTag.TutorialScene)]
    [Label("対象フェーズ")]
    [SerializeField] private PhaseStatusInTutorialScene phaseStatusTutorialScene;

    public bool CheckCondition(PhaseStatusInTutorialScene phase)
    {
        return scene == SceneTag.TutorialScene && phase == phaseStatusTutorialScene;
    }

    #endregion
    #region RhythmGame

    [ShowIf("scene", SceneTag.RhythmGameScene)]
    [Label("対象フェーズ")]
    [SerializeField] private PhaseStatusInRhythmGame phaseStatusRhythmGameScene;

    public bool CheckCondition(PhaseStatusInRhythmGame phase)
    {
        return scene == SceneTag.RhythmGameScene && phase == phaseStatusRhythmGameScene;
    }

    #endregion
    #region Result

    [ShowIf("scene", SceneTag.ResultScene)]
    [Label("対象フェーズ")]
    [SerializeField] private PhaseStatusInResultScene phaseStatusResultScene;

    public bool CheckCondition(PhaseStatusInResultScene phase)
    {
        return scene == SceneTag.ResultScene && phase == phaseStatusResultScene;
    }

    #endregion
    #region GameOver

    [ShowIf("scene", SceneTag.GameOverScene)]
    [Label("対象フェーズ")]
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


