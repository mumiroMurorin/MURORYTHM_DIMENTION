using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicTopicAnimation : MonoBehaviour
{
    [SerializeField] private SelectMusic selectMusic;

    //移動の終了
    public void FinishMoveTopic(int i)
    {
        selectMusic.FinishMoveTopic(i == 1 ? true : false);
    }

    //その他アニメーションの終了
    public void FinishAnim()
    {
        selectMusic.FinishAnimEvent();
    }
}
