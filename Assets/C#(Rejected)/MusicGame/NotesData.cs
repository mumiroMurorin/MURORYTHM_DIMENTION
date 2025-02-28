using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeneralNote
{
    public float time;
    public float[] judge_time;  //[6]
    public int[] judge_lane;    //[2]
    public GameObject obj;
}

public class HoldNote
{
    public float time;
    public bool isStart;
    public bool isGoal;
    public bool isJudge;
    public float[] judge_time;    //[6]
    public float[] judge_lane;    //[2]
    public GameObject obj;
    public HoldNote next;
}

public class DynamicNote
{
    public string kind;
    public float time;
    public float[] judge_time;  //[6]
    public int[] judge_lane;    //[2]
    public Vector3 judge_vector;
    public GameObject obj;
}

public class NotesBlock
{
    public float time;
    public List<GeneralNote> general_list;
    public List<HoldNote> hold_list;
    public List<DynamicNote> dynamic_list;
}

public class NotesData
{
    List<NotesBlock> score;   //���ʃf�[�^

    //������
    public void Init()
    {
        score = new List<NotesBlock>();
    }

    //���ʃf�[�^�ɒʏ�m�[�g��ǉ�
    public bool AddGeneralNote(GeneralNote g)
    {
        //���ɕ��ʃf�[�^�ɓ���time�̃f�[�^������ꍇ�A�}��
        int index = ReturnExistTimeIndex(g.time);
        if (index != -1){ score[index].general_list.Add(g); }
        else    //��L�𖞂����Ȃ������ꍇ�A�V���ɂ���time��NotesBlock�𐶐����ĕ��ʃf�[�^�ɒǉ�
        {
            NotesBlock notesBlock = ReturnVoidNotesBlock();
            notesBlock.time = g.time;
            notesBlock.general_list = new List<GeneralNote> { g };
            score.Add(notesBlock);
        }

        //�m�[�g���肪�d�������Ƃ��̏���
        if(score.Count >= 2) {
            List<GeneralNote> formers;
            for (int i = score.Count - 2; i >= 0; i--)
            {
                //���Ԃ��߂�����break
                if (g.judge_time[0] > score[i].time) { break; }
                //�ʏ�m�[�g������������continue
                if (score[i].general_list.Count == 0) { continue; }
                //���Ԃ��C�����ꂽ��break
                formers = score[i].general_list;
                if (AdjustmentNoteJudgeTime(ref formers, ref g)) { break; }
            }
        }

        return true;
    }

    //���ʃf�[�^�̒ʏ�m�[�g�̔�������C������B�C�������ꍇ��true�A�����łȂ��ꍇfalse��Ԃ�
    private bool AdjustmentNoteJudgeTime(ref List<GeneralNote> formers, ref GeneralNote latter)
    {
        GeneralNote former;
        for (int i = 0; i < formers.Count; i++)
        {
            former = formers[i];
            //���Ԃ�����Ă��� && ���胉�C��������Ă��邩���`�F�b�N
            if (IsReturnJudgeTimeLapping(former, latter) && IsReturnJudgeLaneOverLapping(former, latter))
            {
                former.judge_time[5] = former.time + (latter.time - former.time) / 2f;   //�O�m�[�g�̌�딻������炷
                if (former.judge_time[5] < former.judge_time[4])
                {     //�O�m�[�g�̌��good����������great���肪�x�������Ƃ�����
                    former.judge_time[4] = former.judge_time[5];
                }
                if (former.judge_time[4] < former.judge_time[3])
                {      //�O�m�[�g�̌��great����������perfect���肪�x�������Ƃ�����
                    former.judge_time[3] = former.judge_time[4];
                }

                latter.judge_time[0] = former.time + (latter.time - former.time) / 2f; //��m�[�c�̑O��������炷
                if (former.judge_time[0] > former.judge_time[1])
                {   //��m�[�g�̑Ogood��������Ogreat���肪���������Ƃ�����
                    former.judge_time[1] = former.judge_time[0];
                }
                if (former.judge_time[1] > former.judge_time[2])
                {   //��m�[�g�̑Ogreat��������Operfect���肪���������Ƃ�����
                    former.judge_time[2] = former.judge_time[1];
                }
                return true;
            }
        }
        
        return false;
    }

    //���莞�Ԃ�����Ă��邩�Ԃ�
    private bool IsReturnJudgeTimeLapping(GeneralNote former, GeneralNote latter)
    {
        if(former.judge_time[5] > latter.judge_time[0]) { return true; }
        return false;
    }

    //���胉�C��������Ă��邩�ǂ����Ԃ�
    private bool IsReturnJudgeLaneOverLapping(GeneralNote former, GeneralNote latter)
    {
        if (former.judge_lane[0] <= latter.judge_lane[1] && former.judge_lane[1] >= latter.judge_lane[0]) { return true; }
        return false;
    }

    //���ʃf�[�^�Ƀz�[���h�m�[�g��ǉ�
    public bool AddHoldNote(HoldNote h)
    {
        //���ɕ��ʃf�[�^�ɓ���time�̃f�[�^������ꍇ�A�}��
        int index = ReturnExistTimeIndex(h.time);
        if (index != -1) { score[index].hold_list.Add(h); }
        else    //��L�𖞂����Ȃ������ꍇ�A�V���ɂ���time��NotesBlock�𐶐����ĕ��ʃf�[�^�ɒǉ�
        {
            NotesBlock notesBlock = ReturnVoidNotesBlock();
            notesBlock.time = h.time;
            notesBlock.hold_list = new List<HoldNote> { h };
            score.Add(notesBlock);
        }

        return true;
    }

    //���ʃf�[�^�ɒʏ�m�[�g��ǉ�
    public bool AddDynamicNote(DynamicNote d)
    {
        //���ɕ��ʃf�[�^�ɓ���time�̃f�[�^������ꍇ�A�}��
        int index = ReturnExistTimeIndex(d.time);
        if (index != -1) { score[index].dynamic_list.Add(d); }
        else    //��L�𖞂����Ȃ������ꍇ�A�V���ɂ���time��NotesBlock�𐶐����ĕ��ʃf�[�^�ɒǉ�
        {
            NotesBlock notesBlock = ReturnVoidNotesBlock();
            notesBlock.time = d.time;
            notesBlock.dynamic_list = new List<DynamicNote> { d };
            score.Add(notesBlock);
        }

        return true;
    }

    //���������ꂽNotesBlock��Ԃ�
    private NotesBlock ReturnVoidNotesBlock()
    {
        return new NotesBlock 
        {
            time = 0, 
            general_list = new List<GeneralNote>(),
            hold_list = new List<HoldNote>(), 
            dynamic_list = new List<DynamicNote>() 
        };
    }

    //������time��NotesBlock�����ʃf�[�^�ɑ��݂����ꍇ�A����Index��Ԃ�
    private int ReturnExistTimeIndex(float time)
    {
        for (int i = 0; i < score.Count; i++){
            if (score[i].time == time) { return i; }
        }

        return -1;
    }

    //�z�[���h�m�[�g�n�_��ʏ�m�[�g�ɕϊ�����
    public GeneralNote ConvertHoldStarToGeneralNote(HoldNote h)
    {
        if (!h.isStart) //�G���[
        {
            Debug.LogError("�n�_�ł͂���܂���");
            return null;
        }

        GeneralNote g = new GeneralNote();
        g.judge_lane = Array.ConvertAll(h.judge_lane, f => (int)f); //���背�[��

        g.judge_time = h.judge_time; //���莞��
        g.time = h.time;             //����
        return g;
    }

    //���ʃf�[�^��Ԃ�
    public List<NotesBlock> ReturnScoreData()
    {
        return score;
    }

}
