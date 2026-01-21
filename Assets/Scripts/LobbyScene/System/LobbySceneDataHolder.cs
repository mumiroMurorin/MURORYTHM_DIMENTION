using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using UniRx;

public class LobbySceneDataHolder : ILobbySceneDataGetter, ILobbySceneDataSetter
{
    // TutorialÉvÉåÉCÅH
    ReactiveProperty<bool> isPlayTutorial = new ReactiveProperty<bool>(false);
    IReadOnlyReactiveProperty<bool> ILobbySceneDataGetter.IsPlayTutorial => isPlayTutorial;
    void ILobbySceneDataSetter.SetPlayTutorial(bool isPlay)
    {
        isPlayTutorial.Value = isPlay;
    }
}

public interface ILobbySceneDataGetter
{
    IReadOnlyReactiveProperty<bool> IsPlayTutorial { get; }
}

public interface ILobbySceneDataSetter
{
    void SetPlayTutorial(bool isPlay);
}
