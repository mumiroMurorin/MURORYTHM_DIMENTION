using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackGround_View : MonoBehaviour
{
    [SerializeField] MeshRenderer backGroundRenderer;

    public void OnSetMusicData(MusicData musicData)
    {
        if(musicData == null) { return; }

        backGroundRenderer.material.mainTexture = musicData.ThemeSprite.texture;
    }
}
