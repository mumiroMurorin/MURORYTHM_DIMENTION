using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UniRx;

public class LobbySceneDataHolder : ILobbySceneDataGetter, ILobbySceneDataSetter
{
    public static event Action<GameLanguage> SelectedLanguageChanged;

    // Tutorial selection.
    ReactiveProperty<bool> isPlayTutorial = new ReactiveProperty<bool>(false);
    IReadOnlyReactiveProperty<bool> ILobbySceneDataGetter.IsPlayTutorial => isPlayTutorial;
    void ILobbySceneDataSetter.SetPlayTutorial(bool isPlay)
    {
        isPlayTutorial.Value = isPlay;
    }

    ReactiveProperty<GameLanguage> selectedLanguage;
    IReadOnlyReactiveProperty<GameLanguage> ILobbySceneDataGetter.SelectedLanguage => selectedLanguage;

    public LobbySceneDataHolder()
    {
        selectedLanguage = new ReactiveProperty<GameLanguage>(GameLanguage.Japanese);
    }

    void ILobbySceneDataSetter.SetSelectedLanguage(GameLanguage language)
    {
        selectedLanguage.Value = language;
        SelectedLanguageChanged?.Invoke(language);
    }
}

public interface ILobbySceneDataGetter
{
    IReadOnlyReactiveProperty<bool> IsPlayTutorial { get; }

    IReadOnlyReactiveProperty<GameLanguage> SelectedLanguage { get; }
}

public interface ILobbySceneDataSetter
{
    void SetPlayTutorial(bool isPlay);

    void SetSelectedLanguage(GameLanguage language);
}
