using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicTopicAnimation : MonoBehaviour
{
    [SerializeField] private SelectMusic selectMusic;

    //�ړ��̏I��
    public void FinishMoveTopic(int i)
    {
        selectMusic.FinishMoveTopic(i == 1 ? true : false);
    }

    //���̑��A�j���[�V�����̏I��
    public void FinishAnim()
    {
        selectMusic.FinishAnimEvent();
    }
}
