using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RecognitionBodyAnim : MonoBehaviour
{
    [Header("認識アニメーション")]
    [SerializeField] private Animator recognition_anim;

    bool isFinishOpenAnim;
    bool isFinishCloseAnim;

    private void Start()
    {
        //this.gameObject.SetActive(false);   
    }

    public void Init() 
    {
        this.gameObject.SetActive(false);
    }

    //オープンアニメーションの再生
    public void PlayOpenAnimation()
    {
        gameObject.SetActive(true);
        recognition_anim.SetTrigger("start");
    }

    //終了アニメーションの再生
    public void PlayCloseAnimation()
    {
        gameObject.SetActive(true);
        recognition_anim.SetTrigger("finish");
    }

    //アニメーションが終了したか返す
    public bool IsReturnFinishOpenAnim()
    {
        return isFinishOpenAnim;
    }

    //アニメーションが終了したか返す
    public bool IsReturnFinishCloseAnim()
    {
        return isFinishCloseAnim;
    }

    //アニメーション終了時の呼び出しイベント
    public void FinishOpenAnimation()
    {
        isFinishOpenAnim = true;
    }

    //アニメーション終了時の呼び出しイベント
    public void FinishCloseAnimation()
    {
        isFinishCloseAnim = true;
    }
}
