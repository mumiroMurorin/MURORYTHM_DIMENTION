using UniRx;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class InteractNoteEffectControllerInOption : InteractNoteEffectControllerBinder
{
    [SerializeField] bool useMusicDataListGetter;

    IMusicDataListGetter musicDataListGetter;

    [Inject]
    public void ConstructMusicDataListGetter(
    IMusicDataListGetter musicDataListGetter)
    {
        this.musicDataListGetter = musicDataListGetter;
    }

    protected override void Start()
    {
    }

    public void InitializeAfterLoadData()
    {
        Initialize();
    }

    protected override MusicData GetCurrentMusicData()
    {
        if (useMusicDataListGetter)
        {
            return musicDataListGetter?.CurrentMusicData?.Value;
        }

        return base.GetCurrentMusicData();
    }
}
