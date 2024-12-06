using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AssessmentAnim : MonoBehaviour
{
    //[Header("アニメーション")]
    //[SerializeField] private Animator anim;

    bool isFinishAnim;

    private void Start()
    {
        
    }

    //アニメーションの再生
    public void PlayAnimation()
    {
        gameObject.SetActive(true);
    }

    //アニメーションが終了したか返す
    public bool IsReturnFinishAnim()
    {
        return isFinishAnim;
    }

    //アニメーション終了時の呼び出しイベント
    public void FinishAnimation()
    {
        isFinishAnim = true;
    }
}
