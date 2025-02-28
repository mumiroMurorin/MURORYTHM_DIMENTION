using System.Collections;
using System.IO;
using UnityEngine;
using TMPro;

public class EditorCtrl : MonoBehaviour 
{
    [SerializeField] private EditorAudioCtrl audioCtrl;
    [SerializeField] private EditorUICtrl uiCtrl;
    [SerializeField] private EditorCursol editorCursol;
    [Header("�t�B�[���h")]
    [SerializeField] private GameObject field_obj;
    [Header("�y���e")]
    [SerializeField] private GameObject scoreField_obj;
    [Header("�y���[�e")]
    [SerializeField] private GameObject scoreEdge_obj;
    [Header("���ߐ�")]
    [SerializeField] private GameObject barLine_obj;
    [Header("���ߔԍ�")]
    [SerializeField] private GameObject barLineNum_obj;
    [Header("�ʏ�m�[�g")]
    [SerializeField] private GameObject generalNote_obj;
    [Header("MAX�g�嗦")]
    [SerializeField] private float max_magni = 2000;
    [Header("MIN�g�嗦")]
    [SerializeField] private float min_magni = 100;

    private GameObject barLine_par;
    private GameObject barNum_par;
    private GameObject arrangeNote_obj;

    const float FIELD_OFFSET = 90;
    const string GENERAL_NOTE = "g";

    private bool isPlayScore;
    private bool isPlayMusic;
    private bool isArrangementNote;
    private int offset;
    private int main_note = 4;
    private float fieldLength_per_sec = 2;   //1�b���Ƃɐi�ދ���
    private float main_bpm;                  //���C��BPM
    private float music_fulltime;            //�����̎���
    private float score_speed;               //���ʂ̃X�s�[�h
    private float score_start_ratio;         //���ʃX�^�[�g�n�_�̕���
    private float score_start_z;             //���ʃX�^�[�g�n�_��y���W
    private float offset_z;                  //�I�t�Z�b�g�ł����y���W
    private float score_length;              //���ʂ̒���

    //�����萔
    int[] note_list = new int[11]
    {
        4,8,12,16,24,32,48,64,96,128,192
    };

    private void Start()
    {

    }

    private void FixedUpdate()
    {
        //���ʂ̃X�N���[����
        if (isPlayScore)
        {
            AdvanceScore();
        }
    }

    private void Update()
    { 
        //�m�[�g�̔z�u
        if (isArrangementNote)
        {
            UpdateNoteArrangement();
        }
    }

    //------------�t�B�[���h�n------------

    //�t�B�[���h�̍X�V
    public void UpdateField()
    {
        if(music_fulltime == 0 || main_bpm == 0) { return; }
        UpdateVariable();
        RemoveBarLine();
        AddBarLine(0, main_bpm, main_note);
        RegisterOffset(offset);
        uiCtrl.ChangeFieldSize(music_fulltime * fieldLength_per_sec * 1000);
    }

    //�X�^�[�g�A�G���h�̕����A���W�A���ʃX�s�[�h�̍X�V(���X�^�[�g�n�_�Ȃ�0)
    private void UpdateVariable()
    {
        //score_start_ratio = uiCtrl.ReturnScoreFieldHeight() / (uiCtrl.ReturnFieldLength() - uiCtrl.ReturnScoreFieldHeight());
        score_start_z = 0;
        score_length = music_fulltime * fieldLength_per_sec;
        score_speed = score_length / (music_fulltime / Time.fixedDeltaTime);
    }

    //���ߐ�(����)�̒ǉ�
    public void AddBarLine(int line_num, float bpm, int note)
    {
        //1����4�������̂��ƁB(�܂�bpm256��4��������1���Ԃ�256�񍏂�)
        //1���߂̒�����,4/4���Ƃ����, (fieldLenght_per_sec * 60 * 4) / bpm        
        float bar_length = (fieldLength_per_sec * 60 * 4) / bpm / note;
        int i = 0;
        Vector3[] positions = new Vector3[]{
                new Vector3(-4.5f, 0, 0),               // �J�n�_
                new Vector3(4.5f, 0, 0),               // �I���_
        };

        //����(����)���̒ǉ�
        for (float f = score_start_z; f <= score_start_z + score_length; f += bar_length)
        {
            //LineRenderer�̐ݒ�
            GameObject obj = Instantiate(barLine_obj, barLine_par.transform);
            obj.transform.SetParent(barLine_par.transform);
            obj.transform.position = new Vector3(0, 0, f);
            obj.name = ("line " + i);
            obj.SetActive(true);
            LineRenderer l = obj.GetComponent<LineRenderer>();
            l.startWidth = l.endWidth = 0.03f;
            l.SetPositions(positions);
            l.useWorldSpace = false;

            //���ߐ��A���ߔԍ��̒ǉ�
            if (i++ % note == 0)
            {
                l.startWidth = l.endWidth = 0.08f;
                obj = Instantiate(barLineNum_obj, barNum_par.transform);
                obj.transform.localPosition = new Vector3(0, 0, f);
                obj.GetComponent<TextMeshPro>().text = ("#" + i / note);
                obj.SetActive(true);
            }
        }

        //Debug.Log($"time: {audioCtrl.ReturnAudioFullTime()} length: {uiCtrl.ReturnFieldLength()} bpm: {bpm} interval: {bar_length}");
    }

    //���ߐ��A�����A���ߔԍ��̍폜
    public void RemoveBarLine()
    {
        Destroy(barLine_par);
        Destroy(barNum_par);
        BarLineSet();
    }

    //����(ground)�̍Đ���
    private void AdvanceScore()
    {
        //���̍Đ�
        if (!isPlayMusic && IsReturnOverStartPoint())
        {
            //audioCtrl.MemoryAudioTime(ReturnMusicTime(uiCtrl.ReturnScrollBarRatio()));
            audioCtrl.AudioPlay();
            isPlayMusic = true;
        }

        if(audioCtrl.ReturnAudioNowTime() >= audioCtrl.ReturnAudioFullTime())
        {
            isPlayMusic = false;
            isPlayScore = false;
            PauseScore();
            return;
        }

        GroundChangePos(score_speed, true);
        uiCtrl.ChangeSliderRatio((-field_obj.transform.position.z - offset_z) / (score_start_z + score_length - offset_z), false);
    }

    //���߁A�����A���ߔԍ��̐e�ݒ�
    private void BarLineSet()
    {
        barLine_par = new GameObject("BarLine");
        barLine_par.transform.SetParent(scoreField_obj.transform);
        barNum_par = new GameObject("BarNumber");
        barNum_par.transform.SetParent(scoreEdge_obj.transform);
        barNum_par.transform.localPosition = new Vector3(-5, 0, 0);
    }

    //�O���E���h�̈ړ�
    private void GroundChangePos(float value, bool isAdd)
    {
        if (isAdd) { field_obj.transform.position -= new Vector3(0, 0, value); }
        else { field_obj.transform.position = new Vector3(0, 0, -value); }
    }

    //��������X�^�[�g�n�_���߂������Ԃ�
    private bool IsReturnOverStartPoint()
    {
        return score_start_z - offset_z >= field_obj.transform.position.z ? true : false; 
    }

    //-----------------�z�u�n-----------------

    //�m�[�g�z�u
    private void UpdateNoteArrangement()
    {
        // �J�[�\���ʒu���擾
        Vector3 mousePosition = Input.mousePosition;
        mousePosition.z = 30;
        Vector3 note_pos = ReturnNearestNotePos(mousePosition);
       
        //�z�u���[�h(�z�u�\�ȏꍇ)
        if (note_pos.y > -1) {
            NoteArrangeTmp(note_pos, arrangeNote_obj);  //�ꎞ�ݒu
            if (Input.GetMouseButtonDown(0)) {
                ArrangementNote(note_pos, arrangeNote_obj); //�ݒu
                return;
            }
        }
    }

    //�z�u���[�h�ƃm�[�g���[�h�̕ύX
    public void ChangeCursolMode(bool isArrange)
    {
        isArrangementNote = isArrange;
        arrangeNote_obj.SetActive(isArrange);
    }

    //�N���b�N������̃m�[�g��Ԃ�
    private GameObject ReturnNotePointed(Vector3 pos)
    {
        if(ReturnObjectClickedTag(pos) == "Note") { return ReturnGameObjectClicked(pos); }
        return null;
    }

    //�m�[�g��\��(�ꎞ�z�u)����
    private void NoteArrangeTmp(Vector3 pos, GameObject obj)
    {
        obj.transform.position = pos;
    }

    //�m�[�g��z�u����
    private void ArrangementNote(Vector3 pos, GameObject obj)
    {
        obj.transform.position = pos;
        EditorGeneralNote e = Instantiate(obj, scoreField_obj.transform).GetComponent<EditorGeneralNote>();
        e.Init();
        e.EnableCollider(true);
    }

    //�m�[�g�̐���
    private GameObject GenerateNote(string kind)
    {
        switch (kind)
        {
            case GENERAL_NOTE:
                return Instantiate(generalNote_obj);
        }
        return null;
    }

    //�J�[�\����̃I�u�W�F�N�g�̃^�O��Ԃ�
    private string ReturnObjectClickedTag(Vector3 mouse_pos)
    {
        Ray rayOrigin = Camera.main.ScreenPointToRay(mouse_pos);
        if (Physics.Raycast(rayOrigin, out RaycastHit hitInfo)) { return hitInfo.collider.tag; }
        return null;
    }

    //�N���b�N������̃I�u�W�F�N�g��Ԃ�
    private GameObject ReturnGameObjectClicked(Vector3 mouse_pos)
    {
        Ray rayOrigin = Camera.main.ScreenPointToRay(mouse_pos);
        if (Physics.Raycast(rayOrigin, out RaycastHit hitInfo)) { return hitInfo.collider.gameObject; }
        return null;
    }

    //-----------------����n-----------------

    //�Đ�
    public void PlayScore()
    {
        isPlayScore = true;
        uiCtrl.SetScrollBarEnable(false);
    }

    //�ꎞ��~
    public void PauseScore()
    {
        audioCtrl.AudioStop(true);
        uiCtrl.SetScrollBarEnable(true);
        isPlayScore = false;
        isPlayMusic = false;
    }

    //��~
    public void StopScore()
    {
        audioCtrl.AudioStop(false);
        audioCtrl.MemoryAudioTime(0);
        uiCtrl.ChangeSliderRatio(score_start_ratio, false);
        uiCtrl.SetScrollBarEnable(true);
        GroundChangePos(score_start_z + offset_z, false);
        isPlayScore = false;
        isPlayMusic = false;
    }

    //�O���E���h������̏c���쁦
    public void GroundVerticalScroll(float value)
    {
        value = Mathf.Min(value, 1);
        if(value >= score_start_ratio)
        {
            //audioCtrl.MemoryAudioTime(ReturnMusicTime(value));
        }
        else if(value < score_start_ratio)
        {
            audioCtrl.MemoryAudioTime(0);
        }

        GroundChangePos(value * score_length + offset_z, false);
    }

    //�O���E���h�̊g�嗦���쁦
    public void GroundMagnificationChange(float f)
    {
        fieldLength_per_sec = Mathf.Clamp01(f) * (max_magni - min_magni) + min_magni;
        //uiCtrl.ChangeFieldSize(music_fulltime * fieldLength_per_sec);
    }

    //�m�[�g�̑I��
    public void ChoiceNote(string kind)
    {
        isArrangementNote = true;
        arrangeNote_obj = GenerateNote(GENERAL_NOTE);
        arrangeNote_obj.GetComponent<EditorGeneralNote>().Init();
        arrangeNote_obj.GetComponent<EditorGeneralNote>().EnableCollider(false);
        arrangeNote_obj.transform.SetParent(scoreField_obj.transform);
    }

    //-----------------�o�^�n-----------------

    //�����̓o�^
    public void RegisterAudioClip(AudioClip c, string name)
    {
        audioCtrl.SetAudioClip(c);
        uiCtrl.RegisterMusicName(name);
        music_fulltime = audioCtrl.ReturnAudioFullTime();
        UpdateField();
    }

    //���C��BPM�̓o�^
    public void RegisterMainBPM(float f)
    {
        main_bpm = f;
        UpdateField();
    }

    //���C�������̓o�^
    public void RegisterNote(int i)
    {
        main_note = note_list[i];
        RemoveBarLine();
        AddBarLine(0, main_bpm, main_note);
    }

    //�I�t�Z�b�g�̓o�^��
    public void RegisterOffset(int i)
    {
        offset = i;
        offset_z = offset * Time.fixedDeltaTime * fieldLength_per_sec;
        GroundChangePos(score_start_z + offset_z, false);
    }

    //---------------���^�[���n---------------
    
    //�Đ������Ԃ�
    public bool ReturnIsPlayScore()
    {
        return isPlayScore;
    }

    //�}�E�X�̈ʒu�Ɉ�ԋ߂��m�[�g�z�u�ꏊ��Ԃ�
    public Vector3 ReturnNearestNotePos(Vector3 mouse_pos)
    {
        //���[��
        Vector3 target = Camera.main.ScreenToWorldPoint(mouse_pos);
        Vector3 pos = new Vector3
            ((int)(Mathf.Clamp(target.x + 4f, 0f, 7.75f) / 0.5f) * 0.5f - 3.75f, target.y, 0);

        //����
        if (ReturnObjectClickedTag(mouse_pos) == "BarLine")
        {
            return pos + new Vector3(0, 0, ReturnGameObjectClicked(mouse_pos).transform.position.z);
        }

        return new Vector3(0, -100, 0);
    }

    /*
    //�Ԃ�
    private float ReturnMusicTime(float ratio)
    {
        float y = ratio * (uiCtrl.ReturnFieldLength() - uiCtrl.ReturnScoreFieldHeight()) - score_start_z;
        return (y / (uiCtrl.ReturnFieldLength() - uiCtrl.ReturnScoreFieldHeight() * 2)) * audioCtrl.ReturnAudioFullTime();
    }*/

    //-----------------���̑�------------------

    //�G�N�X�|�[�g
    public void ExportScore()
    {

    }

    //�J�[�\���̕ύX
    public void ChangeCursol(int num)
    {
        editorCursol.ChangeSprite(num);
    }
}