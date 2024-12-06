using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class EditorUICtrl : MonoBehaviour
{
    [SerializeField] private EditorCtrl editorCtrl;

    [Header("���[�h���(object)")]
    [SerializeField] private GameObject load_obj;
    [Header("�Đ��{�^���摜")]
    [SerializeField] private Texture play_texture;
    [Header("�ꎞ��~�{�^���摜")]
    [SerializeField] private Texture pause_texture;
    [Header("�X�R�A�t�B�[���h�̑�{")]
    [SerializeField] private GameObject scoreField_obj;
    [Header("�O���E���h�̃X�N���[���o�[")]
    [SerializeField] private Scrollbar ground_scrollbar;
    [Header("���C���t�B�[���h")]
    [SerializeField] private GameObject field_obj;
    [Header("�����t�@�C���̖��O(�ҏW���)")]
    [SerializeField] private TextMeshProUGUI[] music_name_tmp;

    private RectTransform field_rect;
    private ScrollRect scrollRect;

    private float add_height;
    private string music_name;

    void Start()
    {
        
        field_rect = field_obj.GetComponent<RectTransform>();
        scrollRect = scoreField_obj.GetComponent<ScrollRect>();
    }

    void Update()
    {
        
    }

    //-----------------�t�B�[���h�֘A-----------------

   
    //�t�B�[���h�̑傫����ς���
    public void ChangeFieldSize(float size)
    {
        field_rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);
    }

    //�X�N���[���o�[�̈ʒu��ς���
    public bool ChangeSliderRatio(float value, bool isAdd)
    {
        if (isAdd) 
        {
            if (ground_scrollbar.value + value > 1 || ground_scrollbar.value + value < 0) 
            { 
                Debug.LogWarning("���E�I�I�I"); 
                return false; 
            }
            ground_scrollbar.value += value;
        }
        else 
        {
            if (value > 1 || value < 0) { Debug.LogError("0�`1�܂łł��I"); return false; }
            ground_scrollbar.value = value;
        }
        return true;
    }

    //�X�N���[���������H
    public void SetScrollBarEnable(bool b)
    {
        scrollRect.vertical = b;
        ground_scrollbar.interactable = b;
    }

    //�X�N���[���o�[�̈ʒu(����)��Ԃ�
    public float ReturnScrollBarRatio()
    {
        return ground_scrollbar.value;
    }

    //-----------------�{�^���Ƃ��֘A-----------------

    //�������̓o�^
    public void RegisterMusicName(string str)
    {
        music_name = str;
        foreach (TextMeshProUGUI t in music_name_tmp)
        {
            t.text = str;
        }
    }

    //�Đ��E�ꎞ��~�{�^��
    public void PushPlayButton(RawImage ima)
    {
        if (!editorCtrl.ReturnIsPlayScore())
        {
            editorCtrl.PlayScore();
            ima.texture = pause_texture;
        }
        else
        {
            editorCtrl.PauseScore();
            ima.texture = play_texture;
        }
    }

    //��~�{�^��
    public void PushStopButton(RawImage playButton_ima)
    {
        editorCtrl.StopScore();
        playButton_ima.texture = play_texture;
    }

    //�ҏW�J�n�{�^��
    public void PushEditStartButton()
    {
        load_obj.SetActive(false);
    }

    //���C��BPM
    public void InputBPMBox(TMP_InputField tmp)
    {
        float f;
        if(float.TryParse(tmp.text,out f))
        {
            editorCtrl.RegisterMainBPM(f);
        }
    }

    //���C������
    public void RegisterNote(TMP_Dropdown d)
    {
        editorCtrl.RegisterNote(d.value);
    }

    //���C���I�t�Z�b�g
    public void InputOffsetBox(TMP_InputField tmp)
    {
        int i;
        if (int.TryParse(tmp.text, out i))
        {
            editorCtrl.RegisterOffset(i);
        }
    }

    //�O���E���h�̃X�N���[��
    public void ScrollGround(Scrollbar s)
    {
        if (!editorCtrl.ReturnIsPlayScore()) { editorCtrl.GroundVerticalScroll(s.value); }
    }

    //�O���E���h�̊g�嗦
    public void MagnificationGround(Slider s)
    {
        editorCtrl.GroundMagnificationChange(s.value);
    }

    //�m�[�g�I���{�^��
    public void PushNoteSelectButton(string kind) 
    {
        editorCtrl.ChoiceNote(kind);
    }

    //�G�N�X�|�[�g�{�^��
    public void PushExportButton()
    {
        editorCtrl.ExportScore();
    }

}