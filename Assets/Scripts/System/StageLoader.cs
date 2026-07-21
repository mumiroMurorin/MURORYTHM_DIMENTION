using UnityEngine;
using VContainer;

public class StageLoader : StageLoaderBase
{
    [Inject]
    public void Constructor(IMusicDataGetter musicDataGetter)
    {
        SetMusicDataGetter(musicDataGetter);
    }

    private void Start()
    {
        LoadSelectedStage();
    }

    protected override bool TryGetStageType(out StageType stageType)
    {
        stageType = default;

        if (MusicDataGetter == null || MusicDataGetter.Music == null || MusicDataGetter.Music.Value == null)
        {
            Debug.LogError("【System】MusicDataGetterからStageTypeを取得できません。");
            return false;
        }

        stageType = MusicDataGetter.Music.Value.StageType;
        return true;
    }
}