using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FadeAnim : MonoBehaviour
{
    [Header("フェードアニメーション")]
    [SerializeField] private Animator fade_anim;

    private bool isFadeInfinish; 
    private bool isFadeOutfinish;

    private void Start()
    {
        gameObject.SetActive(true);
    }

    //フェードイン開始
    public void FadeIn()
    {
        gameObject.SetActive(true);
        fade_anim.SetTrigger("in");
    }

    //フェードアウト開始
    public void FadeOut()
    {
        gameObject.SetActive(true);
        fade_anim.SetTrigger("out");
    }

    //フェードイン、アウトの終了
    public void FinishFade(int i)
    {
        if (i == 0) { isFadeInfinish = true; }
        else { 
            isFadeOutfinish = true;
            //gameObject.SetActive(false);
        }
    }

    //フェードインが終了したかどうかかえす
    public bool IsReturnFadeInFinish()
    {
        return isFadeInfinish;
    }

    //フェードアウトが終了したかどうかかえす
    public bool IsReturnFadeOutFinish()
    {
        return isFadeOutfinish;
    }
}
