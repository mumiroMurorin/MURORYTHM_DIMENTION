using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UICtrl : MonoBehaviour
{
    [Header("コンボ表示")]
    [SerializeField] private GameObject combo_obj;
    [Header("体認識")]
    [SerializeField] private RecognitionBodyAnim recogBodyAnim;
    [Header("Comp")]
    [SerializeField] private AssessmentAnim assessment_comp;
    [Header("FC")]
    [SerializeField] private AssessmentAnim assessment_fc;
    [Header("AP")]
    [SerializeField] private AssessmentAnim assessment_ap;
    [Header("仮隠岐")]
    [SerializeField] private TextMeshProUGUI kari_tmp;


    [SerializeField] private FadeAnim fade;
    [SerializeField] private ReadyAnim readyAnim;
   
    private TextMeshPro combo_tmp;
    private Animator combo_anim;
    private MusicData data;

    private void Start()
    {
        
    }

    //トリガーと初期化
    public void FirstFunc(MusicData md)
    {
        Init();
        readyAnim.Init();
        recogBodyAnim.Init();
        SetInfoReady(md);
    }

    //初期化
    private void Init()
    {
        combo_tmp = combo_obj.GetComponent<TextMeshPro>();
        combo_anim = combo_obj.GetComponent<Animator>();
        AddCombo(0);
    }

    //コンボの表示
    public void AddCombo(int combo)
    {
        if (combo > 4)
        {
            combo_tmp.text = combo.ToString();
            combo_tmp.enabled = true;
            combo_anim.SetTrigger("combo");
        }
        else
        {
            combo_tmp.enabled = false;
        }
    }

    //---------------「Ready?」------------------

    //「Ready?」に情報セット
    private void SetInfoReady(MusicData md)
    {
        readyAnim.DataSet(md);
    }

    //「Ready？」動作開始
    public void ReadyAnimStart()
    {
        readyAnim.PlayAnimation();
    }

    //「Ready?」が終了したかどうか返す
    public bool IsReturnFinishReady()
    {
        return readyAnim.IsReturnFinishAnim();
    }

    //---------------「体認識中…」------------------

    //動作開始
    public void RecogAnimStart()
    {
        recogBodyAnim.PlayOpenAnimation();
    }

    //動作終了
    public void RecogAnimFinish()
    {
        recogBodyAnim.PlayCloseAnimation();
    }

    //終了したかどうか返す
    public bool IsReturnFinishOpenRecog()
    {
        return recogBodyAnim.IsReturnFinishOpenAnim();
    }

    //終了したかどうか返す
    public bool IsReturnFinishCloseRecog()
    {
        return recogBodyAnim.IsReturnFinishCloseAnim();
    }

    //--------------「FullCombo」--------------

    //「TrackComplete」動作開始
    public void TrackCompAnimStart()
    {
        assessment_comp.PlayAnimation();
    }

    //「FullCombo」動作開始
    public void FullComboAnimStart()
    {
        assessment_fc.PlayAnimation();
    }

    //「AllPerfect」動作開始
    public void AllPerfectAnimStart()
    {
        assessment_ap.PlayAnimation();
    }

    //評価アニメーションが終了したか返す
    public bool IsReturnFinishAssessment()
    {
        return assessment_comp.IsReturnFinishAnim() || assessment_fc.IsReturnFinishAnim() || assessment_ap.IsReturnFinishAnim();
    }

    //------------フェード------------

    //フェードイン
    public void FadeInStart()
    {
        fade.FadeIn();
    }

    //フェードアウト
    public void FadeOutStart()
    {
        fade.FadeOut();
    }
    
    //フェードインが終わったか返す
    public bool IsReturnFadeInFinish()
    {
        return fade.IsReturnFadeInFinish();
    }

    //フェードアウトが終わったか返す
    public bool IsReturnFadeOutFinish()
    {
        return fade.IsReturnFadeOutFinish();
    }

    //--------------------仮---------------------

    public void DebugLogBuildMode(string str)
    {
        kari_tmp.text = str;
    }
}
