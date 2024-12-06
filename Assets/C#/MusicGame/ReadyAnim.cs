using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ReadyAnim : MonoBehaviour
{
    [Header("「Ready?」アニメーション")]
    [SerializeField] private Animator ready_anim;

    [Header("曲名")]
    [SerializeField] private TextMeshProUGUI musicName_tmp;

    [Header("コンポーザー名")]
    [SerializeField] private TextMeshProUGUI composerName_tmp;

    bool isFinishAnim;

    private void Start()
    {
        //gameObject.SetActive(false);
    }

    public void Init()
    {
        this.gameObject.SetActive(false);
    }

    //データのセット
    public void DataSet(MusicData md)
    {
        musicName_tmp.text = md.MusicName;
        composerName_tmp.text = md.ComposerName;
    }

    //アニメーションの再生
    public void PlayAnimation()
    {
        gameObject.SetActive(true);
        ready_anim.SetTrigger("start");
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

    //殺す
    public void Kill()
    {
        Destroy(this.gameObject);
    }
}
