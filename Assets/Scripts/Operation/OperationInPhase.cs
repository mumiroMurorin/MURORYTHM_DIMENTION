using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using UnityEngine;

[CreateAssetMenu(fileName = "OperationInPhase", menuName = "ScriptableObject/OperationInPhase")]
public class OperationInPhase : ScriptableObject
{
    [Label("蟇ｾ蠢懊す繝ｼ繝ｳ")]
    [SerializeField] private SceneTag scene;

    #region Title

    [ShowIf("scene", SceneTag.TitleScene)]
    [Label("蟇ｾ蠢懊ヵ繧ｧ繝ｼ繧ｺ")]
    [SerializeField] private PhaseStatusInTitleScene phaseStatusTitleScene;

    public bool CheckCondition(PhaseStatusInTitleScene phase)
    {
        return scene == SceneTag.TitleScene && phase == phaseStatusTitleScene;
    }

    #endregion
    #region Lobby

    [ShowIf("scene", SceneTag.LobbyScene)]
    [Label("蟇ｾ蠢懊ヵ繧ｧ繝ｼ繧ｺ")]
    [SerializeField] private PhaseStatusInLobbyScene phaseStatusLobbyScene;

    public bool CheckCondition(PhaseStatusInLobbyScene phase)
    {
        return scene == SceneTag.LobbyScene && phase == phaseStatusLobbyScene;
    }

    #endregion
    #region Select

    [ShowIf("scene", SceneTag.SelectScene)]
    [Label("蟇ｾ蠢懊ヵ繧ｧ繝ｼ繧ｺ")]
    [SerializeField] private PhaseStatusInSelectScene phaseStatusSelectScene;

    public bool CheckCondition(PhaseStatusInSelectScene phase)
    {
        return scene == SceneTag.SelectScene && phase == phaseStatusSelectScene;
    }

    #endregion
    #region RhythmGame

    [ShowIf("scene", SceneTag.RhythmGameScene)]
    [Label("蟇ｾ蠢懊ヵ繧ｧ繝ｼ繧ｺ")]
    [SerializeField] private PhaseStatusInRhythmGame phaseStatusRhythmGameScene;

    public bool CheckCondition(PhaseStatusInRhythmGame phase)
    {
        return scene == SceneTag.RhythmGameScene && phase == phaseStatusRhythmGameScene;
    }

    #endregion
    #region Result

    [ShowIf("scene", SceneTag.ResultScene)]
    [Label("蟇ｾ蠢懊ヵ繧ｧ繝ｼ繧ｺ")]
    [SerializeField] private PhaseStatusInResultScene phaseStatusResultScene;

    public bool CheckCondition(PhaseStatusInResultScene phase)
    {
        return scene == SceneTag.ResultScene && phase == phaseStatusResultScene;
    }

    #endregion
    #region GameOver

    [ShowIf("scene", SceneTag.GameOverScene)]
    [Label("蟇ｾ蠢懊ヵ繧ｧ繝ｼ繧ｺ")]
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
    [Label("繧ｿ繝・メ蠕後・繧ｯ繝ｼ繝ｫ繧ｿ繧､繝")]
    [SerializeField] private float coolTime = 0.2f;

    [SerializeField] private OperationAssetUnit[] operations;

    public IEnumerable<OperationAssetUnit> Operations => operations;

    public SliderCoolDownHandler SliderCoolDownHandler => new SliderCoolDownHandler(coolTime);
}


