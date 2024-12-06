using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UICtrl : MonoBehaviour
{
    [Header("�R���{�\��")]
    [SerializeField] private GameObject combo_obj;
    [Header("�̔F��")]
    [SerializeField] private RecognitionBodyAnim recogBodyAnim;
    [Header("Comp")]
    [SerializeField] private AssessmentAnim assessment_comp;
    [Header("FC")]
    [SerializeField] private AssessmentAnim assessment_fc;
    [Header("AP")]
    [SerializeField] private AssessmentAnim assessment_ap;
    [Header("���B��")]
    [SerializeField] private TextMeshProUGUI kari_tmp;


    [SerializeField] private FadeAnim fade;
    [SerializeField] private ReadyAnim readyAnim;
   
    private TextMeshPro combo_tmp;
    private Animator combo_anim;
    private MusicData data;

    private void Start()
    {
        
    }

    //�g���K�[�Ə�����
    public void FirstFunc(MusicData md)
    {
        Init();
        readyAnim.Init();
        recogBodyAnim.Init();
        SetInfoReady(md);
    }

    //������
    private void Init()
    {
        combo_tmp = combo_obj.GetComponent<TextMeshPro>();
        combo_anim = combo_obj.GetComponent<Animator>();
        AddCombo(0);
    }

    //�R���{�̕\��
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

    //---------------�uReady?�v------------------

    //�uReady?�v�ɏ��Z�b�g
    private void SetInfoReady(MusicData md)
    {
        readyAnim.DataSet(md);
    }

    //�uReady�H�v����J�n
    public void ReadyAnimStart()
    {
        readyAnim.PlayAnimation();
    }

    //�uReady?�v���I���������ǂ����Ԃ�
    public bool IsReturnFinishReady()
    {
        return readyAnim.IsReturnFinishAnim();
    }

    //---------------�u�̔F�����c�v------------------

    //����J�n
    public void RecogAnimStart()
    {
        recogBodyAnim.PlayOpenAnimation();
    }

    //����I��
    public void RecogAnimFinish()
    {
        recogBodyAnim.PlayCloseAnimation();
    }

    //�I���������ǂ����Ԃ�
    public bool IsReturnFinishOpenRecog()
    {
        return recogBodyAnim.IsReturnFinishOpenAnim();
    }

    //�I���������ǂ����Ԃ�
    public bool IsReturnFinishCloseRecog()
    {
        return recogBodyAnim.IsReturnFinishCloseAnim();
    }

    //--------------�uFullCombo�v--------------

    //�uTrackComplete�v����J�n
    public void TrackCompAnimStart()
    {
        assessment_comp.PlayAnimation();
    }

    //�uFullCombo�v����J�n
    public void FullComboAnimStart()
    {
        assessment_fc.PlayAnimation();
    }

    //�uAllPerfect�v����J�n
    public void AllPerfectAnimStart()
    {
        assessment_ap.PlayAnimation();
    }

    //�]���A�j���[�V�������I���������Ԃ�
    public bool IsReturnFinishAssessment()
    {
        return assessment_comp.IsReturnFinishAnim() || assessment_fc.IsReturnFinishAnim() || assessment_ap.IsReturnFinishAnim();
    }

    //------------�t�F�[�h------------

    //�t�F�[�h�C��
    public void FadeInStart()
    {
        fade.FadeIn();
    }

    //�t�F�[�h�A�E�g
    public void FadeOutStart()
    {
        fade.FadeOut();
    }
    
    //�t�F�[�h�C�����I��������Ԃ�
    public bool IsReturnFadeInFinish()
    {
        return fade.IsReturnFadeInFinish();
    }

    //�t�F�[�h�A�E�g���I��������Ԃ�
    public bool IsReturnFadeOutFinish()
    {
        return fade.IsReturnFadeOutFinish();
    }

    //--------------------��---------------------

    public void DebugLogBuildMode(string str)
    {
        kari_tmp.text = str;
    }
}
