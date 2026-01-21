using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

public class MusicDataGetter : MonoBehaviour
{
    IMusicDataGetter musicDataGetter;

    public IMusicDataGetter DataGetter { get { return musicDataGetter; } }

    [Inject]
    public void Coustructor(IMusicDataGetter musicDataGetter)
    {
        this.musicDataGetter = musicDataGetter;
    }
}
