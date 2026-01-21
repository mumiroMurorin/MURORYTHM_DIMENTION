using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

public class LobbySceneDataController : MonoBehaviour
{
    ILobbySceneDataGetter dataGetter;
    ILobbySceneDataSetter dataSetter;

    public ILobbySceneDataGetter DataGetter { get { return dataGetter; } }
    public ILobbySceneDataSetter DataSetter { get { return dataSetter; } }

    [Inject]
    public void Constructor(ILobbySceneDataGetter dataGetter, ILobbySceneDataSetter dataSetter)
    {
        this.dataGetter = dataGetter;
        this.dataSetter = dataSetter;
    }
}
