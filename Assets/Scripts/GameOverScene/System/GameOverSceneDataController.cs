using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

public class GameOverSceneDataController : MonoBehaviour
{
    IGameOverSceneDataGetter dataGetter;
    IGameOverSceneDataSetter dataSetter;

    public IGameOverSceneDataGetter DataGetter { get { return dataGetter; } }
    public IGameOverSceneDataSetter DataSetter { get { return dataSetter; } }

    [Inject]
    public void Constructor(IGameOverSceneDataGetter dataGetter, IGameOverSceneDataSetter dataSetter)
    {
        this.dataGetter = dataGetter;
        this.dataSetter = dataSetter;
    }
}
