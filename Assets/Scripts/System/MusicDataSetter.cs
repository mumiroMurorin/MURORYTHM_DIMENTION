using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

public class MusicDataSetter : MonoBehaviour
{
    IMusicDataSetter musicDataSetter;

    public IMusicDataSetter DataSetter { get { return musicDataSetter; } }

    [Inject]
    public void Coustructor(IMusicDataSetter musicDataSetter)
    {
        this.musicDataSetter = musicDataSetter;
    }
}
