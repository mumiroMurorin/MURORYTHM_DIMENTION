using UnityEngine;

public abstract class StageLoaderBase : MonoBehaviour
{
    [SerializeField] Transform stageParent;
    [SerializeField] StageTypeToPrefabDatabase stageTypeToPrefabDatabase;

    protected IMusicDataGetter MusicDataGetter { get; private set; }

    protected void SetMusicDataGetter(IMusicDataGetter musicDataGetter)
    {
        MusicDataGetter = musicDataGetter;
    }

    protected void LoadSelectedStage()
    {
        if (!TryGetStageType(out StageType stageType)) { return; }

        LoadStage(stageType);
    }

    protected abstract bool TryGetStageType(out StageType stageType);

    protected void LoadStage(StageType stageType)
    {
        var obj = InstantiateStagePrefab(stageType);
        if (obj == null) { return; }

        obj.transform.SetParent(stageParent);

        if (!obj.TryGetComponent(out IStageController stageController))
        {
            Debug.LogWarning($"【System】ステージオブジェクトにIStageControllerがアタッチされていません: {obj.name}");
            return;
        }

        if (MusicDataGetter == null)
        {
            Debug.LogWarning("【System】MusicDataGetterが設定されていないため、StageControllerを初期化できません。");
            return;
        }

        stageController.Initialize(MusicDataGetter);
    }

    private GameObject InstantiateStagePrefab(StageType stageType)
    {
        if (stageTypeToPrefabDatabase == null)
        {
            Debug.LogError("【System】StageTypeToPrefabDatabaseが設定されていません。");
            return null;
        }

        GameObject prefab = stageTypeToPrefabDatabase.GetPrefab(stageType);
        if (prefab != null) { return Instantiate(prefab, Vector3.zero, Quaternion.identity); }

        Debug.LogError($"【System】該当するStageが見つかりません: {stageType}");
        return null;
    }
}