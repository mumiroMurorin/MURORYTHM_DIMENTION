using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using UniRx;

public class GameOverSceneDataHolder : IGameOverSceneDataGetter, IGameOverSceneDataSetter
{
    // コンティニュー？
    ReactiveProperty<bool> isContinue = new ReactiveProperty<bool>(false);
    IReadOnlyReactiveProperty<bool> IGameOverSceneDataGetter.IsContinue => isContinue;
    void IGameOverSceneDataSetter.SetContinue(bool isPlay)
    {
        isContinue.Value = isPlay;
    }
}

public interface IGameOverSceneDataGetter
{
    IReadOnlyReactiveProperty<bool> IsContinue { get; }
}

public interface IGameOverSceneDataSetter
{
    void SetContinue(bool isPlay);
}
