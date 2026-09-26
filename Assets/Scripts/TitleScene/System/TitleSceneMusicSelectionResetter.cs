using VContainer.Unity;

public sealed class TitleSceneMusicSelectionResetter : IStartable
{
    private readonly IMusicDataSetter musicDataSetter;
    private readonly IMusicDataListGetter musicDataListGetter;
    private readonly IMusicDataListSetter musicDataListSetter;

    public TitleSceneMusicSelectionResetter(
        IMusicDataSetter musicDataSetter,
        IMusicDataListGetter musicDataListGetter,
        IMusicDataListSetter musicDataListSetter)
    {
        this.musicDataSetter = musicDataSetter;
        this.musicDataListGetter = musicDataListGetter;
        this.musicDataListSetter = musicDataListSetter;
    }

    public void Start()
    {
        // リザルトなどが参照していたプレイ対象を解除する
        musicDataSetter.SetMusicData(null);
        musicDataSetter.SetDifficulty(Difficulty.Easy);

        // セレクト画面側の選択状態を先頭の楽曲・Easyへ戻す
        musicDataListSetter.SetDifficulty(Difficulty.Easy);

        if (musicDataListGetter.MusicDatasSorted.Count > 0)
        {
            musicDataListSetter.SetMusicIndex(0);
        }
    }
}
