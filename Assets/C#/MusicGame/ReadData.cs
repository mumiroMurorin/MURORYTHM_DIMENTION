using System.Collections;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.IO;
using UnityEngine;

public class ReadData : MonoBehaviour
{
    [Header("�ʏ�m�[�gGood�̔��蕝")] public float general_judge_time_good;
    [Header("�ʏ�m�[�gGreat�̔��蕝")] public float general_judge_time_great;
    [Header("�ʏ�m�[�gPerfect�̔��蕝")] public float general_judge_time_perfect;
    [Header("Hold�m�[�gGood�̔��蕝")] public float hold_judge_time_good;
    [Header("Hold�m�[�gGreat�̔��蕝")] public float hold_judge_time_great;
    [Header("Hold�m�[�gPerfect�̔��蕝")] public float hold_judge_time_perfect;
    [Header("Dynamic�m�[�gGood�̔��蕝")] public float dynamic_judge_time_good;
    [Header("Dynamic�m�[�gGreat�̔��蕝")] public float dynamic_judge_time_great;
    [Header("Dynamic�m�[�gPerfect�̔��蕝")] public float dynamic_judge_time_perfect;

    const string GENERAL_NOTE = "g";
    const string HOLD_NOTE = "h";
    const string DYNAMIC_UP_NOTE = "d_up";
    const string DYNAMIC_DOWN_NOTE = "d_down";
    const string DYNAMIC_GROUND_RIGHT_NOTE = "d_gro_right";
    const string DYNAMIC_GROUND_LEFT_NOTE = "d_gro_left";

    private int NOTE_TIME_COLUMN;
    private int NOTE_KIND_COLUMN;
    private int NOTE_LANE_COLUMN;
    private int NOTE_JUDGE_COLUMN;
    private int NOTE_VECTOR_COLUMN;

    List<string[]> csvDatas = new List<string[]>(); // CSV�̒��g�����郊�X�g;
    List<HoldNote> holdNotesList;     //�z�[���h�m�[�g���ꎞ�ۑ����郊�X�g

    private bool isComplete;
    private NotesData notesData;

    //�ǂݍ��ރf�[�^�̏��������ƃg���K�[
    public void FirstFunc(TextAsset chart_file)
    {
        Init();
        csvDatas = ConvertTextAssetToStringList(chart_file);
        SetDataColumn();//���̗񂪉���\���Ă��邩�ݒ�
        CSVDataToGameData();    //���ʃf�[�^��}��
        isComplete = true;
    }

    //������(���ʂ�ǂݍ��ޖ��ɍs��)
    private void Init()
    {
        notesData = new NotesData();
        csvDatas = new List<string[]>();
        holdNotesList = new List<HoldNote>();

        notesData.Init();
    }

    //��s���ǂݍ���ŕ��ʃf�[�^�ɑ}��
    private void CSVDataToGameData()
    {
        string[] str;
        for (int i = 1; i < csvDatas.Count; i++)
        {
            str = csvDatas[i];
            if (str[0] == "" || str[0] == null) { break; }

            switch (ReturnKindOfNote(str))
            {
                case GENERAL_NOTE:
                    GeneralNote g = ConvertGeneralNote(str);
                    notesData.AddGeneralNote(g);
                    break;
                case HOLD_NOTE:
                    HoldNote h = ConvertHoldNote(str);
                    if (h.isStart) { notesData.AddHoldNote(h); }
                    break;
                case DYNAMIC_UP_NOTE:
                case DYNAMIC_DOWN_NOTE:
                case DYNAMIC_GROUND_RIGHT_NOTE:
                case DYNAMIC_GROUND_LEFT_NOTE:
                    DynamicNote d = ConvertDynamicNote(str);
                    notesData.AddDynamicNote(d);
                    break;
            }
        }

        //DebugAllNotesData(GENERAL_NOTE);
        EnableIsGoal();     //�z�[���h�m�[�g�ɏI�_�����t�^
    }

    //�^����ꂽ������̃m�[�c�̎�ނ�Ԃ�
    private string ReturnKindOfNote(string[] str)
    {
        switch (str[NOTE_KIND_COLUMN])
        {
            case GENERAL_NOTE: return GENERAL_NOTE;
            case DYNAMIC_UP_NOTE: return DYNAMIC_UP_NOTE;
            case DYNAMIC_DOWN_NOTE: return DYNAMIC_DOWN_NOTE;
            case DYNAMIC_GROUND_RIGHT_NOTE: return DYNAMIC_GROUND_RIGHT_NOTE;
            case DYNAMIC_GROUND_LEFT_NOTE: return DYNAMIC_GROUND_LEFT_NOTE;
            default:
                if(str[NOTE_KIND_COLUMN][0] == HOLD_NOTE[0]) { return HOLD_NOTE; }
                else { Debug.LogError("�m��Ȃ��m�[�g�̎�ނł�: " + str[NOTE_KIND_COLUMN]); }
                break;
        }
        
        return null;
    }

    //�^����ꂽ�������GeneralNote�ɕϊ�
    private GeneralNote ConvertGeneralNote(string[] str)
    {
        GeneralNote g = new GeneralNote();

        //time��float�ɕϊ�
        if(!float.TryParse(str[NOTE_TIME_COLUMN], out g.time)) { 
            Debug.LogError("time����float�ɕϊ��s�ȕ����񂪂���܂���: " + str[NOTE_TIME_COLUMN]); 
            return null;
        }

        g.judge_time = new float[6]
        {
            g.time - general_judge_time_good,      //�OGood����
            g.time - general_judge_time_great,     //�OGrat����
            g.time - general_judge_time_perfect,   //�OPerfect����
            g.time + general_judge_time_perfect,   //��Perfect����
            g.time + general_judge_time_great,     //��Grat����
            g.time + general_judge_time_good,      //��Good����
        };

        //Lane��int[]�ɕϊ�
        string[] s = str[NOTE_LANE_COLUMN].Split('t');  //�u0t3�v���u0�v�Ɓu3�v�ɕ�����
        g.judge_lane = new int[2];
        for (int i = 0; i < 2; i++){
            if (!int.TryParse(s[i], out g.judge_lane[i]))
            {
                Debug.LogError("lane����int�ɕϊ��s�ȕ����񂪂���܂���: " + str[NOTE_LANE_COLUMN]);
                return null;
            }
        }
        return g;
    }

    //�^����ꂽ�������HoldNote�ɕϊ�
    private HoldNote ConvertHoldNote(string[] str)
    {
        HoldNote h = new HoldNote();

        //time��float�ɕϊ�
        if (!float.TryParse(str[NOTE_TIME_COLUMN], out h.time))
        {
            Debug.LogError("time����float�ɕϊ��s�ȕ����񂪂���܂���: " + str[NOTE_TIME_COLUMN]);
            return null;
        }

        //[KIND]���A"h"�Ɛ����ɕ�����
        int index;
        string[] s = str[NOTE_KIND_COLUMN].Split('_');
        if (!int.TryParse(s[1], out index))
        {
            Debug.LogError("kind����int�ɕϊ��s�ȕ����񂪂���܂���: " + str[NOTE_KIND_COLUMN]);
            return null;
        }
        //�����O�m�[�g�̐V�K�o�^�̓C���f�b�N�X+1�Ȃ̂ŁA
        //index��holdNoteList�̃C���f�b�N�X+1�𒴂����Ƃ��G���[
        if (index > holdNotesList.Count)
        {
            Debug.LogError("�V�K�o�^����HoldNote�͏��holdNotesList�̃C���f�b�N�X��+1�Ŗ�����΂Ȃ�܂���: " + str[NOTE_KIND_COLUMN]);
            return null;
        }
        //�����̃z�[���h�m�[�g�̎��m�[�g�ł���ꍇ�A
        //holdNoteList�����ւ��A�擪�̃z�[���h�m�[�g��ύX
        if(index < holdNotesList.Count) 
        {
            holdNotesList[index].next = h;
            holdNotesList[index] = h;
            //����
            if (str[NOTE_JUDGE_COLUMN] == "TRUE")
            {
                h.isJudge = true;
                h.judge_time = new float[6] {
                    h.time - hold_judge_time_good,      //Good����
                    h.time - hold_judge_time_great,     //Great����
                    h.time - hold_judge_time_perfect,   //Perfect����
                    h.time + hold_judge_time_perfect,   //Perfect����
                    h.time + hold_judge_time_great,     //Great����
                    h.time + hold_judge_time_good,      //Good����
                };
            }
        }
        //�V�����z�[���h�m�[�g�ł���ꍇ�AholdNoteList�ɐV�K�o�^����
        else if(index == holdNotesList.Count)
        {
            holdNotesList.Add(h);
            h.isStart = true;
            h.isJudge = true;
            //����
            h.judge_time = new float[6] {
                    h.time - general_judge_time_good,      //Good����
                    h.time - general_judge_time_great,     //Great����
                    h.time - general_judge_time_perfect,   //Perfect����
                    h.time + general_judge_time_perfect,   //Perfect����
                    h.time + general_judge_time_great,     //Great����
                    h.time + general_judge_time_good,      //Good����
            };
        }

        //Lane��int[]�ɕϊ�
        s = str[NOTE_LANE_COLUMN].Split('t');  //�u0t3�v���u0�v�Ɓu3�v�ɕ�����
        h.judge_lane = new float[2];
        for (int i = 0; i < 2; i++)
        {
            if (!float.TryParse(s[i], out h.judge_lane[i]))
            {
                Debug.LogError("lane����float�ɕϊ��s�ȕ����񂪂���܂���: " + str[NOTE_LANE_COLUMN]);
                return null;
            }
        }

        return h;
    }

    //�^����ꂽ�������DynamicNote�ɕϊ�
    private DynamicNote ConvertDynamicNote(string[] str)
    {
        DynamicNote d = new DynamicNote();

        //time��float�ɕϊ�
        if (!float.TryParse(str[NOTE_TIME_COLUMN], out d.time))
        {
            Debug.LogError("time����float�ɕϊ��s�ȕ����񂪂���܂���: " + str[NOTE_TIME_COLUMN]);
            return null;
        }

        d.judge_time = new float[6]
        {
            d.time - dynamic_judge_time_good,      //�OGood����
            d.time - dynamic_judge_time_great,     //�OGrat����
            d.time - dynamic_judge_time_perfect,   //�OPerfect����
            d.time + dynamic_judge_time_perfect,   //��Perfect����
            d.time + dynamic_judge_time_great,     //��Grat����
            d.time + dynamic_judge_time_good,      //��Good����
        };

        //Lane��int[]�ɕϊ�
        string[] s = str[NOTE_LANE_COLUMN].Split('t');  //�u0t3�v���u0�v�Ɓu3�v�ɕ�����
        d.judge_lane = new int[2];
        for (int i = 0; i < 2; i++)
        {
            if (!int.TryParse(s[i], out d.judge_lane[i]))
            {
                Debug.LogError("lane����int�ɕϊ��s�ȕ����񂪂���܂���: " + str[NOTE_LANE_COLUMN]);
                return null;
            }
        }

        //��ޕʂɕ�����
        d.kind = str[NOTE_KIND_COLUMN];

        //VECTOR��vector3�ɕϊ�
        //d.judge_vector = StringToVector3(str[NOTE_VECTOR_COLUMN]);
        d.judge_vector = PositionToVector3(d.judge_lane, d.kind);

        return d;
    }

    //holdNotesList�Ɏc���Ă���m�[�c���I�_����ɂ���
    private void EnableIsGoal()
    {
        for(int i = 0; i < holdNotesList.Count; i++)
        {
            holdNotesList[i].isGoal = true;
        }
        holdNotesList.Clear();
    }

    //���̗񂪉���\���Ă��邩�ݒ�
    private void SetDataColumn()
    {
        for (int i = 0; i < csvDatas[0].Length; i++)
        {
            switch (csvDatas[0][i]) {
                case "[TIME]":
                    NOTE_TIME_COLUMN = i;
                    break;
                case "[KIND]":
                    NOTE_KIND_COLUMN = i;
                    break;
                case "[LANE]":
                    NOTE_LANE_COLUMN = i;
                    break;
                case "[JUDGE]":
                    NOTE_JUDGE_COLUMN = i;
                    break;
                case "[VECTOR]":
                    NOTE_VECTOR_COLUMN = i;
                    break;
                default:
                    Debug.LogError("�m��Ȃ������񂪓����Ă���: " + csvDatas[0][i]);
                    break;
            }
        }
    }

    //---------------���̑�------------------

    //�����������ǂ����Ԃ�
    public bool isReturnReady()
    {
        return isComplete;
    }

    //�o���オ�������ʃf�[�^��Ԃ�
    public List<NotesBlock> ReturnGameData()
    {
        return notesData.ReturnScoreData();
    }

    //string�u(0:0:0)�v��Vector3�ɕϊ�
    public static Vector3 StringToVector3(string input)
    {
        //�����񂩂�ǂݎ��
        var elements = input.Trim('(', ')').Split(':'); // �O��Ɋۊ��ʂ�����΍폜���A�J���}�ŕ���
        var result = Vector3.zero;
        var elementCount = Mathf.Min(elements.Length, 3); // ���[�v�񐔂�elements�̐��ȉ�����3�ȉ��ɂ���

        for (var i = 0; i < elementCount; i++)
        {
            float value;
            value = float.Parse(elements[i]);
            //float.TryParse(elements[i], out value); // �ϊ��Ɏ��s�����Ƃ��ɗ�O���o������]�܂�����΁AParse���g���̂������ł��傤
            result[i] = value;
        }
        return result;
    }

    //�m�[�c���W����Vector3�𐶐�
    public static Vector3 PositionToVector3(int[] lane, string kind)
    {
        //�����񂩂�ǂݎ��
        var result = Vector3.zero;
        float center = (lane[1] + lane[0]) / 2.0f;
        switch (kind)
        {
            case DYNAMIC_UP_NOTE:
                if(center <= 7.5f) { result = new Vector3(-0.2f, 0.2f, 0); }
                else { result = new Vector3(0.2f, 0.2f, 0); }
                break;
            case DYNAMIC_GROUND_RIGHT_NOTE:
                if (center <= 7.5f) { result = new Vector3(0.2f, -0.2f, 0); }
                else { result = new Vector3(0.2f, 0.2f, 0); }
                break;
            case DYNAMIC_GROUND_LEFT_NOTE:
                if (center <= 7.5f) { result = new Vector3(-0.2f, 0.2f, 0); }
                else { result = new Vector3(-0.2f, -0.2f, 0); }
                break;
        }

        return result;
    }

    //���������Փx��Ԃ�
    private string ReturnDifficulityName(int num)
    {
        switch (num)
        {
            case 0:
                return "initiate";
            case 1:
                return "fanatic";
            case 2:
                return "skyclad";
            case 3:
                return "dream";
            default:
                Debug.LogError("0�`3�ȊO�̐�����������:" + num);
                return "";
        }
    }


    //--------------�f�o�b�O�p---------------

    //[�f�o�b�O�p]�S�Ẵm�[�g�f�[�^��\��
    private void DebugAllNotesData(string s)
    {
        foreach (NotesBlock n in notesData.ReturnScoreData())
        {
            switch (s)
            {
                case GENERAL_NOTE:
                    if (n.general_list.Count == 0) { continue; }
                    foreach (GeneralNote g in n.general_list) { DebugGeneralNoteData(g); }
                    break;
                case HOLD_NOTE:
                    if (n.hold_list.Count == 0) { continue; }
                    foreach (HoldNote h in n.hold_list) { DebugHoldNoteData(h); }
                    break;
                case DYNAMIC_UP_NOTE:
                case DYNAMIC_DOWN_NOTE:
                case DYNAMIC_GROUND_RIGHT_NOTE:
                case DYNAMIC_GROUND_LEFT_NOTE:
                    if (n.dynamic_list.Count == 0) { continue; }
                    foreach (DynamicNote d in n.dynamic_list) { DebugDynamicNoteData(d); }
                    break;
            }

        }
    }

    //[�f�o�b�O�p]�����̒ʏ�m�[�g�f�[�^��\��
    private void DebugGeneralNoteData(GeneralNote g)
    {
        Debug.Log("[time: " + g.time + "]  [judge" + g.judge_time[0] + " ~ " + g.judge_time[5] + "]  [lane:" + g.judge_lane[0] + " ~ " + g.judge_lane[1] + "]");
    }

    //[�f�o�b�O�p]�����̃z�[���h�m�[�g�f�[�^��\��
    private void DebugHoldNoteData(HoldNote h)
    {
        Debug.Log("[time: " + h.time + "]  [lane:" + h.judge_lane[0] + " ~ " + h.judge_lane[1] + "]" +
            "] [isStart: " + h.isStart + "] [isGoal:" + h.isGoal + "] [isJudge:" + h.isJudge + "] [next: " + h.next + "]");
    }

    //[�f�o�b�O�p]�����̃_�C�i�~�b�N�m�[�g�f�[�^��\��
    private void DebugDynamicNoteData(DynamicNote d)
    {
        Debug.Log("[time: " + d.time + "]  [judge" + d.judge_time[0] + " ~ " + d.judge_time[5] 
            + "]  [lane:" + d.judge_lane[0] + " ~ " + d.judge_lane[1] + "] [vector " + d.judge_vector + "]");
    }

    /// <summary>
    /// CSV�f�[�^(textAsset)��List<string>�ɕϊ�
    /// </summary>
    /// <param name="text"></param>
    /// <param name="cancellation_token"></param>
    /// <returns></returns>
    private List<string[]> ConvertTextAssetToStringList(TextAsset text)
    {
        List<string[]> list = new List<string[]>();
        StringReader reader = new StringReader(text.text);
        while (reader.Peek() != -1)
        {
            string line = reader.ReadLine();
            list.Add(line.Split(','));
        }

        return list;
    }
}