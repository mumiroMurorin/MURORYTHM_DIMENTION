using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;
using System.Linq;
using Deform;

public class PlayGame : MonoBehaviour
{
    //���萔�̏ڍ�
    public class Judge
    {
        public int sum = 0;
        public int general_num = 0;
        public int hold_num = 0;
        public int dynamic_num = 0;
        public int spaceHold_num = 0;
    }

    [Header("�L�l�N�g�H")]
    [SerializeField] private bool isKinect;
    [Header("�m�[�g���x(�M��Ȃ炱����)")]
    [SerializeField] private float note_speed;
    [Header("���x�{��")]
    [SerializeField] private float speed_magni = 10;
    [Header("�m�[�g�I�t�Z�b�g")]
    [SerializeField] private float note_offset;
    [Header("�z�[���h�m�[�g�̃��b�V��������")]
    [SerializeField] private int holdNoteDivision_num = 10;
    [Header("�X�y�[�X�m�[�g�̊��x�{��")]
    [SerializeField] private float space_sensitivity;
    [Header("������̊Ԋu")]
    [SerializeField] private float spaceJudge_interval = 0.2f;
    [Header("����\���ꏊ")]
    [SerializeField] private Transform judge_transform;

    [SerializeField] private UICtrl uiCtrl;
    [SerializeField] private BendDeformer ground_bend;
    [SerializeField] private KinectInput kinect;
    [SerializeField] private SliderInput slider;
    [SerializeField] private GameObject note_obj;
    //[SerializeField] private GameObject marker_obj;

    [Header("�ʏ�m�[�g�}�e���A��(�ォ��center,right,left,1mass)")]
    [SerializeField] private Material[] generalNote_mate;
    [Header("�z�[���h�m�[�g�}�e���A��(�ォ��center,right,left,1mass)")]
    [SerializeField] private Material[] holdNote_mate;
    [Header("�z�[���h�O���E���h�}�e���A��(�ォ��default,touch,miss)")]
    [SerializeField] private Material[] holdNote_ground_mate;
    [Header("�_�C�i�~�b�N�m�[�g��I�u�W�F�N�g(�ォ��center,right,left,1mass)")]
    [SerializeField] private GameObject[] dynamicUpNote_obj;
    [Header("�_�C�i�~�b�N�m�[�g���I�u�W�F�N�g(�ォ��center,right,left,1mass)")]
    [SerializeField] private GameObject[] dynamicDownNote_obj;
    [Header("�_�C�i�~�b�N�O���E���h�E�I�u�W�F�N�g(�ォ��center,right,left,1mass)")]
    [SerializeField] private GameObject[] dGroundRightNote_obj;
    [Header("�_�C�i�~�b�N�O���E���h���I�u�W�F�N�g(�ォ��center,right,left,1mass)")]
    [SerializeField] private GameObject[] dGroundLeftNote_obj;
    [Header("�ʏ픻�� �ォ��Perfect")]
    [SerializeField] private GameObject[] judge_obj;
    [Header("�X�y�[�X���� �ォ��Perfect")]
    [SerializeField] private GameObject[] judge_space_obj;
    [Header("�ʏ�m�[�c�G�t�F�N�g(�ォ��perfect,great,good)")]
    [SerializeField] private GameObject[] judgeEffects;
    [Header("�L�[�r�[��")]
    [SerializeField] private GameObject[] keyBeam_obj;
    [Header("���g��LoadData")] 
    [SerializeField] private PlayGame playGame;

    const string GENERAL_NOTE = "g";
    const string HOLD_NOTE = "h";
    const string DYNAMIC_UP_NOTE = "d_up";
    const string DYNAMIC_DOWN_NOTE = "d_down";
    const string DYNAMIC_GROUND_RIGHT_NOTE = "d_gro_right";
    const string DYNAMIC_GROUND_LEFT_NOTE = "d_gro_left";

    //�n�ʊJ�n�n�_
    const float START_GROUND_Z = 183.25f;
    const float JUDGE_GROUND_Z = 0;
    const float END_GROUND_Z = -11.268f;

    //MeshCollider[] markerColliderArray;   //�}�[�J�[�R���C�_�[�z��
    //Vector3[] markerPositionArray;        //�}�[�J�[���W�z��
    List<Judge> judgesList;              //���ꂼ�ꔻ��̐�[perfect,great,good,miss]
    List<NotesBlock> score_data;         //���ʃf�[�^

    //private int add_p_score;            //perfect���薈�ɒǉ������X�R�A
    private int combo;                //���݃R���{
    private int score;                //�X�R�A
    private float note_generate_time; //�m�[�g������O���ԁ�
    private float game_time;          //�o�ߎ���
    private float speed;              //�X�s�[�h
    private bool isPlaying;           //�v���C���H

    private List<Vector3> rightHand_list; //���t���[���O�̉E��̈ʒu
    private List<Vector3> leftHand_list;  //���t���[���O�̍���̈ʒu

    private NotesData notesData;
    private GameObject effect_pre;
    private GameObject note_pre;
    private GameObject note_original_pre;
    private GameObject marker_pre;
    private GameObject judge_pre;
    private GameObject[] generalNote_obj;
    private GameObject[] holdNote_obj;
    //private GameObject[] dynamicNote_obj;

    void Start()
    {
        Init();
    }

    private void FixedUpdate()
    {
        if (isPlaying)
        {
            GenerateNote();     //�m�[�g����
            if (isKinect) { UpdateSpaceJudge(); } //������̍X�V
            InputKey();         //���͊֌W
            game_time += Time.fixedDeltaTime;    //���܂肱�̈ʒu�͍D�܂ꂽ���̂���Ȃ������c(1�t���[����)
        }
    }

    //������ ���m�[�g���O�ǂݍ��݂͉�
    private void Init()
    {
        //�e�̍폜�Ɛ���
        if (note_pre) { Destroy(note_pre.transform); }
        note_pre = new GameObject("Notes");
        if (note_original_pre) { Destroy(note_original_pre.transform); }
        note_original_pre = new GameObject("Notes_Original");
        if (marker_pre) { Destroy(marker_pre.transform); }
        marker_pre = new GameObject("Marker");
        if (effect_pre) { Destroy(effect_pre.transform); }
        effect_pre = new GameObject("Effects");
        if (judge_pre) { Destroy(judge_pre.transform); }
        judge_pre = new GameObject("Judges");

        //���X�g�̏�����
        //generatedNotesList = new List<GameObject>();
        //markerColliderArray = new MeshCollider[0];
        //markerPositionArray = new Vector3[0];
        score_data = new List<NotesBlock>();
        judgesList = new List<Judge>();
        
        rightHand_list = new List<Vector3>();
        leftHand_list = new List<Vector3>();
        for (int i = 0; i < 4; i++){ judgesList.Add(new Judge()); }

        //�e�l������
        isPlaying = false;
        combo = 0;
        score = 0;
        game_time = 0;
        
        notesData = new NotesData();

        //�m�[�g�̏�����
        generalNote_obj = InitNotes(generalNote_mate);
        holdNote_obj = InitNotes(holdNote_mate);
        //��U�ˁ�
        //dynamicNote_obj = InitNotes(dynamicNote_mate);
        note_original_pre.SetActive(false);

        //marker�̐ݒu
        //InstallMarker(18, true);
        //UpdateMarkerPositionList();
    }

    //�}�e���A���̒���ꂽ�m�[�c��Ԃ�
    private GameObject[] InitNotes(Material[] mate)
    {
        GameObject[] notes = new GameObject[mate.Length];
        for(int i = 0; i < mate.Length; i++)
        {
            notes[i] = Instantiate(note_obj, note_original_pre.transform);
            notes[i].GetComponentInChildren<Renderer>().material = mate[i];
        }
        return notes;
    }

    //���𓮂���
    public void StartGame()
    {
        isPlaying = true;
    }

    /*
    //�}�[�J�[��ݒu(�ݒu��)
    private void InstallMarker(int num, bool isDis)
    {       
        //�G���[����
        if (num < 1) {
            Debug.Log("�}�[�J�[�������Ȃ����܂�: " + num);
            return;
        }

        float z;
        markerColliderArray = new MeshCollider[num]; //�z��̏�����
        markerPositionArray = new Vector3[num];      //�z��̏�����

        for (int n = 0; n < num; n++)
        {
            z = START_GROUND_Z + Mathf.Abs(START_GROUND_Z - END_GROUND_Z) / (num - 1) * n;   //���������Ƃ��̃}�[�J�[z���W

            //�����A���O�A���W�A�p�x�A�\���A�e�ݒ�
            GameObject g = Instantiate(marker_obj);
            g.name = "Marker" + n;
            g.transform.position = new Vector3(10 * Mathf.Cos(-90f * Mathf.Deg2Rad), 0, z);
            g.SetActive(isDis);
            g.transform.SetParent(marker_pre.transform);

            markerColliderArray[n] = g.GetComponent<MeshCollider>();    //�}�[�J�[���X�g�ɒǉ�
        }
    }

    //�}�[�J�[���W,�p�x���X�g���X�V
    private void UpdateMarkerPositionList()
    {
        IEnumerable<Vector3> tmp = markerColliderArray.Select(x => x.bounds.center);
        markerPositionArray = tmp.ToArray();
    }
    */

    //���ʐ����֐�
    private void GenerateNote()
    {
        //���ʐ����I��
        if (score_data.Count == 0) { return; }

        //DOPath�g���Ȃ炱�̊֐�
        //note.transform.DOPath(markerPositionArray, time, PathType.Linear)
        //    .SetLookAt(0.001f).SetEase(Ease.Linear);
        //���Ԃ̌v�Z
        //float time = Mathf.Abs(START_GROUND_Z - END_GROUND_Z) / speed;

        //�f�[�^��̎��Ԃ𒴉߂������J��Ԃ�
        while (score_data.Count != 0 && IsReturnOverNextTime(score_data[0].time + note_offset))
        {
            //�ʏ�m�[�c�̐���
            foreach (GeneralNote g in score_data[0].general_list){
                g.obj.transform.position += ReturnLittleDistance(g.time);
                g.obj.SetActive(true);
            }

            //�z�[���h�m�[�c�̐���
            foreach (HoldNote h in score_data[0].hold_list){
                h.obj.transform.position += ReturnLittleDistance(h.time);
                h.obj.SetActive(true);
            }

            //�_�C�i�~�b�N�m�[�c�̐���
            foreach (DynamicNote d in score_data[0].dynamic_list)
            {
                d.obj.transform.position += ReturnLittleDistance(d.time);
                d.obj.SetActive(true);
            }

            score_data.RemoveAt(0);
        }
    }

    //�m�[�g�̎��O����
    public void GenerateNoteInAdvance()
    {
        //�S�Ẵm�[�c�����O����
        for (int i = 0; i < score_data.Count; i++)
        {
            //�ʏ�m�[�c�̐���
            foreach (GeneralNote g in score_data[i].general_list)
            {
                GeneralNote new_g = OffsetGeneralNote(g);
                g.obj = InstantiateGeneralNote(new_g);
                g.obj.SetActive(false);
                //generatedNotesList.Add(note);
            }

            //�z�[���h�m�[�c�̐���
            foreach (HoldNote h in score_data[i].hold_list)
            {
                HoldNote new_h = OffsetHoldNote(h);
                if (new_h.isStart) { h.obj = ReturnHoldNote(new_h, null, 0); }
                h.obj.SetActive(false);
                //generatedNotesList.Add(note);
            }

            //�_�C�i�~�b�N�m�[�c�̐���
            foreach (DynamicNote d in score_data[i].dynamic_list)
            {
                DynamicNote new_d = OffsetDynamicNote(d);
                d.obj = InstantiateDynamicNote(new_d);
                d.obj.SetActive(false);
            }
        }
    }

    //----------------�m�[�g�����֌W-------------------

    //-----------�ʏ�m�[�g-------------

    //�ʏ�m�[�g�𐶐�����
    private GameObject InstantiateGeneralNote(GeneralNote g)
    {
        GameObject pre = new GameObject("General_pre");
        GameObject obj = ReturnReSizeNote(g.judge_lane[1] - g.judge_lane[0] + 1, "g");    //1�}�X�m�[�g��g�ݍ��킹�ăI�u�W�F�N�g�𐶐�
        
        //�m�[�g�p�x�A���W�A�e�̐ݒ�
        float deg = (g.judge_lane[0] + (g.judge_lane[1] - g.judge_lane[0]) / 2f - 7.5f) * 11.25f;
        obj.transform.eulerAngles = new Vector3(0, 0, deg);
        obj.transform.SetParent(pre.transform);
        pre.transform.position = new Vector3(0, 0, START_GROUND_Z);
        pre.transform.SetParent(note_pre.transform);
        pre.AddComponent<GeneralNoteObject>().Init(playGame, g, speed);  //GeneralNoteObject��ǉ����ď�����

        return pre;
    }

    //---------�z�[���h�m�[�g-----------

    //����Đ������ꂽ�z�[���h�m�[�g��Ԃ�
    private GameObject ReturnHoldNote(HoldNote h, GameObject p, float add_z)
    {
        //�n�_
        if (h.isStart)
        {
            //�z�[���h�m�[�g�̑c�̐ݒ�
            p = new GameObject("HoldNote");
            p.transform.position = new Vector3(0, 0, START_GROUND_Z);
            p.transform.SetParent(note_pre.transform);
            p.AddComponent<HoldNoteParentObject>().Init(speed);
            add_z = 0;
            //���̃m�[�c��
            ReturnHoldNote(h.next, p, add_z + (h.next.time - h.time) * speed);
            //�n�_����
            GameObject obj = ReturnHoldEdge(h);
            obj.transform.SetParent(p.transform);
            obj.transform.localPosition = Vector3.zero;
            return p;
        }
        //�I�_
        else if (h.isGoal)
        {
            //�I�_����
            GameObject obj = ReturnHoldEdge(h);
            obj.transform.SetParent(p.transform);
            obj.transform.localPosition = new Vector3(0, 0, add_z);
            //�Ԃ�
            return p;
        }
        //�O���E���h
        else
        {
            ReturnHoldNote(h.next, p, add_z + (h.next.time - h.time) * speed);
            GameObject obj = ReturnHoldGround(h, h.next);
            obj.transform.SetParent(p.transform);
            obj.transform.localPosition = new Vector3(0, 0, add_z);
            //���̃m�[�c��
            return p;
        }
    }

    //�z�[���h�m�[�g�𐶐�����(�I�_�Ǝn�_)
    private GameObject ReturnHoldEdge(HoldNote h)
    {
        //h = OffsetHoldNote(h);
        GameObject obj = CreateHoldEdge(h);

        //�n�_
        if (h.isStart) 
        {
            //���b�V���̕t��
            GameObject g = CreateHoldMesh(h, h.next);
            g.transform.position = obj.transform.position;
            g.transform.SetParent(obj.transform);
            obj.AddComponent<HoldNoteStart>().Init(playGame, notesData.ConvertHoldStarToGeneralNote(h), g, holdNote_ground_mate, speed);
        }
        //�I�_
        else { obj.AddComponent<HoldNoteEnd>().Init(playGame, h, speed); }

        return obj;
    }

    //�z�[���h�m�[�g�I�u�W�F�N�g�𐶐�����(�I�_�Ǝn�_)
    private GameObject CreateHoldEdge(HoldNote h)
    {
        GameObject pre = new GameObject("HoldEdge");
        GameObject obj = ReturnReSizeNote((int)h.judge_lane[1] - (int)h.judge_lane[0] + 1, "h");    //1�}�X�m�[�g��g�ݍ��킹�ăI�u�W�F�N�g�𐶐�

        //�m�[�g�p�x�A���W�A�e�̐ݒ�
        float deg = (h.judge_lane[0] + (h.judge_lane[1] - h.judge_lane[0]) / 2f - 7.5f) * 11.25f;
        obj.transform.eulerAngles = new Vector3(0, 0, deg);
        obj.transform.SetParent(pre.transform);

        return pre;
    }

    //�z�[���h�m�[�g�𐶐�����(�O���E���h)
    private GameObject ReturnHoldGround(HoldNote former, HoldNote latter)
    {
        //former = OffsetHoldNote(former);
        //former = OffsetHoldNote(latter);
        GameObject obj = CreateHoldMesh(former, latter);
        //obj.transform.position = new Vector3(0, 0, START_GROUND_Z);
        //obj.transform.SetParent(note_pre.transform);
        obj.AddComponent<HoldNoteGround>().Init(playGame, former, speed, holdNote_ground_mate);
        return obj;
    }

    //�z�[���h�m�[�g�I�u�W�F�N�g�𐶐�����(���b�V��)
    private GameObject CreateHoldMesh(HoldNote former, HoldNote latter)
    {
        GameObject obj = new GameObject("mesh");
        MeshFilter meshFilter = obj.AddComponent<MeshFilter>();
        Mesh mesh = new Mesh();
        MeshRenderer meshRenderer = obj.AddComponent<MeshRenderer>();
        meshFilter.mesh = mesh;

        float h = speed * (latter.time - former.time);   //z�̒���
        //�m�[�g�[�`�[(judge_lane[0]�` [1])�܂łɓ��镪���C���f�b�N�X
        List<float> index_f = new List<float>();
        List<float> index_l = new List<float>();

        //��2�ӂ̌X���𓱏o
        float a_0 = h / (latter.judge_lane[0] - former.judge_lane[0]);
        float a_1 = h / (latter.judge_lane[1] - former.judge_lane[1]);

        //�����C���f�b�N�X���X�g�Ɋe���b�V�����_��ǉ�
        float first = former.judge_lane[0], end = former.judge_lane[1] + 1;
        if (a_0 < float.PositiveInfinity && a_0 < 0) //���ӂ̌X�������̏ꍇ�̂ݕύX
        {
            index_f.Add(former.judge_lane[0]);
            index_l.Add(former.judge_lane[0]);
            first = latter.judge_lane[0];
        }
        if (a_1 < float.PositiveInfinity && a_1 > 0) //�E�ӂ̌X�������̏ꍇ�̂ݕύX
        {
            index_f.Add(former.judge_lane[1] + 1);
            index_l.Add(former.judge_lane[1] + 1);
            end = latter.judge_lane[1] + 1;
        }
        index_f.AddRange(ReturnMeshPointList(first, end, holdNoteDivision_num)); //���X�g�ɑ��

        first = latter.judge_lane[0];
        end = latter.judge_lane[1] + 1;
        if (a_0 < float.PositiveInfinity && a_0 > 0) //���ӂ̌X�������̏ꍇ�̂ݕύX
        {
            index_l.Add(latter.judge_lane[0]);
            index_f.Add(latter.judge_lane[0]);
            first = former.judge_lane[0];
        }
        if (a_1 < float.PositiveInfinity && a_1 < 0) //�E�ӂ̌X�������̏ꍇ�̂ݕύX
        {
            index_l.Add(latter.judge_lane[1] + 1);
            index_f.Add(latter.judge_lane[1] + 1);
            end = former.judge_lane[1] + 1;
        }

        index_l.AddRange(ReturnMeshPointList(first, end, holdNoteDivision_num)); //���X�g�ɑ��

        index_f.Sort();                         //�\�[�g
        index_l.Sort();                         //�\�[�g
        index_f = index_f.Distinct().ToList();  //�d���v�f�폜
        index_l = index_l.Distinct().ToList();  //�d���v�f�폜
        if (a_0 < float.PositiveInfinity && a_0 != 0) { index_l.Remove(first); }//�X����0�łȂ��ꍇ�A�ŏ��̒l���폜
        if (a_1 < float.PositiveInfinity && a_1 != 0) { index_f.Remove(end); }  //�X����0�łȂ��ꍇ�A�Ō�̒l���폜

        //�_�̍��W�𓱏o(���s�̕��������񂪂���)
        List<Vector3> vertices_f = new List<Vector3>();   //�O�m�[�g���_���W
        List<Vector3> vertices_l = new List<Vector3>();   //��m�[�g���_���W

        foreach (float f in index_f)
        {
            float deg = (f - 16) * 11.25f * Mathf.Deg2Rad;
            float z = 0;
            if (f < former.judge_lane[0])   //�΂ߍ��ӂ̏ꍇ��
            { z = a_0 * (f - latter.judge_lane[0]) + h; }
            else if (f > former.judge_lane[1] + 1)  //�΂߉E�ӂ̏ꍇ��
            { z = a_1 * (f - former.judge_lane[1] - 1); }
            vertices_f.Add(new Vector3(10 * Mathf.Cos(deg), 10 * Mathf.Sin(deg), z));
        }

        foreach (float f in index_l)
        {
            float deg = (f - 16) * 11.25f * Mathf.Deg2Rad;
            float z = speed * (latter.time - former.time);
            if (f < latter.judge_lane[0])   //�΂ߍ��ӂ̏ꍇ��
            { z = a_0 * (f - former.judge_lane[0]); }
            else if (f > latter.judge_lane[1] + 1)  //�΂߉E�ӂ̏ꍇ�� 
            { z = a_1 * (f - latter.judge_lane[1] - 1) + h; }
            vertices_l.Add(new Vector3(10 * Mathf.Cos(deg), 10 * Mathf.Sin(deg), z));
        }

        //���b�V�������ԍ��̊���U��
        List<int> triangles = new List<int>();  //���b�V�������ԍ�
        vertices_f.AddRange(vertices_l);        //����
        int num = 0, harf = (vertices_f.Count - 1) / 2;
        if (vertices_f.Count % 2 != 0 && a_0 >= float.PositiveInfinity) { harf = (vertices_f.Count - 1) / 2 - 1; }

        //���b�V�����v�Z
        while (num + 1 <= harf) 
        {
            triangles.Add(num + 1);
            triangles.Add(num);
            triangles.Add(num + 1 + harf);

            if (num + 2 + harf >= vertices_f.Count) { break; }
            triangles.Add(num + 1 + harf);
            triangles.Add(num + 2 + harf);
            triangles.Add(++num);
        }

        //�E�ӂ̌X���̂�0(����)�̂Ƃ��A�Ō�̃��b�V����ǉ�
        if (a_0 >= float.PositiveInfinity && a_1 < float.PositiveInfinity)
        {
            triangles.Add(num + 1 + harf);
            triangles.Add(num + 2 + harf);
            triangles.Add(num);
        }

        //���b�V���̓_�Ɩʂ�ݒ肵�čČv�Z
        mesh.vertices = vertices_f.ToArray(); //���
        mesh.triangles = triangles.ToArray(); //���
        mesh.RecalculateNormals();

        //Debug.Log("���_list: " + string.Join(",", vertices_f.Select(n => n.ToString())));
        //Debug.Log("���b�V��list: " + string.Join(",", triangles.Select(n => n.ToString())));

        //Deform�̐ݒ�
        Deformable d = obj.AddComponent<Deformable>();
        d.AddDeformer(ground_bend);

        //�e�Ƃ������̐ݒ�
        GameObject pre = new GameObject("HoldGround");
        obj.transform.SetParent(pre.transform);

        return pre;
    }

    //---------�_�C�i�~�b�N�m�[�g----------

    //�_�C�i�~�b�N�m�[�g�𐶐�����
    private GameObject InstantiateDynamicNote(DynamicNote d)
    {
        //d = OffsetDynamicNote(d);
        GameObject pre = new GameObject("Dynamic_pre");
        GameObject obj = ReturnReSizeNote(d.judge_lane[1] - d.judge_lane[0] + 1, d.kind);    //1�}�X�m�[�g��g�ݍ��킹�ăI�u�W�F�N�g�𐶐�

        //�m�[�g�p�x�A���W�A�e�̐ݒ�
        float deg = (d.judge_lane[0] + (d.judge_lane[1] - d.judge_lane[0]) / 2f - 7.5f) * 11.25f;
        obj.transform.eulerAngles = new Vector3(0, 0, deg);
        obj.transform.SetParent(pre.transform);
        pre.transform.position = new Vector3(0, 0, START_GROUND_Z);
        pre.transform.SetParent(note_pre.transform);
        pre.AddComponent<DynamicNoteObject>().Init(playGame, d, speed);  //GeneralNoteObject��ǉ����ď�����

        return pre;
    }

    //------------���̑�-------------

    //�����̒����̃m�[�g��Ԃ�
    private GameObject ReturnReSizeNote(int size, string k)
    {
        string name;
        GameObject[] notes;

        //���O�ƕ����m�[�g������
        switch (k)
        {
            case GENERAL_NOTE:
                name = "GeneralNote";
                notes = generalNote_obj;
                break;
            case HOLD_NOTE:
                name = "HoldNote";
                notes = holdNote_obj;
                break;
            case DYNAMIC_UP_NOTE:
                name = "DynamicUpNote";
                notes = dynamicUpNote_obj;
                break;
            case DYNAMIC_DOWN_NOTE:
                name = "DynamicDownNote";
                notes = dynamicDownNote_obj;
                break;
            case DYNAMIC_GROUND_RIGHT_NOTE:
                name = "DynamicGroundRightNote";
                notes = dGroundRightNote_obj;
                break;
            case DYNAMIC_GROUND_LEFT_NOTE:
                name = "DynamicGroundLeftNote";
                notes = dGroundLeftNote_obj;
                break;
            default:
                Debug.Log("���̎�ނ̃m�[�g�͂���܂���: " + k);
                return null;
        }

        Vector3 pos, rot;
        GameObject pre = new GameObject(name);   //�܂Ƃߖ��̃I�u�W�F�N�g����
        for(int i = 0; i < size; i++)
        {   
            //�|�W�V�����Ɗp�x�̌v�Z
            pos = new Vector3(10 * Mathf.Cos((((size - 1) / 2f - i) * 11.25f - 90f) * Mathf.Deg2Rad), 10 * Mathf.Sin((((size - 1) / 2f - i) * 11.25f - 90f) * Mathf.Deg2Rad), 0);
            rot = new Vector3(0, 0, ((size - 1) / 2f - i) * 11.25f);

            //1�}�X�m�[�g�̎�
            if (size == 1) { Instantiate(notes[3], pos, Quaternion.Euler(rot), pre.transform); }
            //�m�[�g���[�̎�
            else if (i == 0) { Instantiate(notes[2], pos, Quaternion.Euler(rot), pre.transform); }
            //�m�[�g�E�[�̎�
            else if (i == size - 1) { Instantiate(notes[1], pos, Quaternion.Euler(rot), pre.transform); }
            //�m�[�g���̎�
            else { Instantiate(notes[0], pos, Quaternion.Euler(rot), pre.transform); }
        }

        //Deform�̐ݒ�
        foreach(Transform t in pre.transform)
        {
            Deformable d = t.GetComponentInChildren<Deformable>();
            d.AddDeformer(ground_bend);
        }

        return pre;
    }

    //�͈͓��̃��b�V�����_���X�g��Ԃ�(end��+1���邱�Ƃ�Y��Ȃ��悤��)
    private List<float> ReturnMeshPointList(float first, float end, int div_num)
    {
        if(div_num == 0)
        {
            Debug.LogError("���b�V����������0�ł�");
            return null;
        }

        List<float> list = new List<float>();
        float f = 0;
        while (f < end)
        {
            if (first < f) { list.Add(f); }
            f += 1f / div_num;
        }
        list.Add(first);
        list.Add(end);
        list.Sort();
        return list;
    }

    //�ʏ�m�[�g�̃I�t�Z�b�g�̐ݒ�
    private GeneralNote OffsetGeneralNote(GeneralNote g)
    {
        g.judge_time = Array.ConvertAll(g.judge_time, i => i + note_offset);
        g.time += note_offset;
        return g;
    }

    //�z�[���h�m�[�g�̃I�t�Z�b�g�̐ݒ�(�d�˂Ď��̃z�[���h�m�[�g�̃I�t�Z���M���Ă���̂Œ���)
    private HoldNote OffsetHoldNote(HoldNote h)
    {
        if (h.isJudge) { h.judge_time = Array.ConvertAll(h.judge_time, i => i + note_offset); }
        h.time += note_offset;
        if (h.next != null) { h.next = OffsetHoldNote(h.next); }
        return h;
    }

    //�_�C�i�~�b�N�m�[�g�̃I�t�Z�b�g�̐ݒ�
    private DynamicNote OffsetDynamicNote(DynamicNote d)
    {
        d.judge_time = Array.ConvertAll(d.judge_time, i => i + note_offset);
        d.time += note_offset;
        return d;
    }

    //----------------���́E����֌W-------------------

    //�����X���C�_�[�ݒ�
    public void SetSliderOption(RootOption r)
    {
        slider.SetRootOption(r);
    }

    //����KINECT�ݒ�
    public void SetKinectOption(RootOption r)
    {
        isKinect = r.isUseKinect;
        kinect.SetRootOption(r);
    }

    //����SpaceSensitivity�ݒ�
    public void SetSpaceSensitivityOption(RootOption r)
    {
        space_sensitivity = r.space_sensitivity;
    }

    //�����X�s�[�h�A�I�t�Z�ݒ�
    public void SetMusicGameOption(float s, float offset)
    {
        note_speed = s;
        note_offset = offset * Time.fixedDeltaTime;
        speed = note_speed * speed_magni;
        //����U���̒l(������)
        //note_generate_time = Mathf.Abs(START_GROUND_Z + Mathf.Abs(JUDGE_GROUND_Z - END_GROUND_Z) - END_GROUND_Z) / speed;
        note_generate_time = Mathf.Abs(START_GROUND_Z - JUDGE_GROUND_Z) / speed;
    }

    //�L�l�N�g���g���b�L���O���Ă��邩�ǂ����Ԃ�
    public bool IsReturnKinectTracking()
    {
        return kinect.IsReturnTracking();
    }

    //�{�^�����͊֌W
    private void InputKey()
    {
        //�S�Ă̓��͂𒲂ׂ�
        for(int i = 0; i < 16; i++)
        {
            //�L�[�r�[��
            DisKeyBeam(i, slider.IsReturnSliderTouching(i));
        }

        if (Input.GetKeyDown(KeyCode.Escape)) { SceneManager.LoadScene("MusicSelectScene"); }
    }

    //��������X�V����(FixedUpdate)
    private void UpdateSpaceJudge()
    {
        rightHand_list.Add(kinect.ReturnHandPos(true));
        leftHand_list.Add(kinect.ReturnHandPos(false));
        //Debug.Log($"{rightHand_list[rightHand_list.Count - 1]} , {leftHand_list[leftHand_list.Count - 1]}");

        if (game_time < spaceJudge_interval) { return; }

        rightHand_list.RemoveAt(0);
        leftHand_list.RemoveAt(0);
    }

    //���A�N�V�����������Ԃ�
    public bool IsReturnSpaceAction(Vector3 judge_vec)
    {
        judge_vec *= space_sensitivity; //�{��
        for (int i = 0; i < rightHand_list.Count; i++)
        {
            Vector3 vec = kinect.ReturnHandPos(true) - rightHand_list[i];
            //Debug.Log($"right: {judge_vec} , {vec}");
            //Debug.Log($"{i} , {rightHand_list[i]}  right: {vec.x}");
            if (judge_vec.x > 0 && vec.x > judge_vec.x) { return true; }
            else if (judge_vec.x < 0 && vec.x < judge_vec.x) { return true; }
            if (judge_vec.y > 0 && vec.y > judge_vec.y) { return true; }
            else if (judge_vec.y < 0 && vec.y < judge_vec.y) { return true; }
            if (judge_vec.z > 0 && vec.z > judge_vec.z) { return true; }
            else if (judge_vec.z < 0 && vec.z < judge_vec.z) { return true; }

            vec = kinect.ReturnHandPos(false) - leftHand_list[i];
            //Debug.Log($"left: {judge_vec} , {vec}");
            if (judge_vec.x > 0 && vec.x > judge_vec.x) { return true; }
            else if (judge_vec.x < 0 && vec.x < judge_vec.x) { return true; }
            if (judge_vec.y > 0 && vec.y > judge_vec.y) { return true; }
            else if (judge_vec.y < 0 && vec.y < judge_vec.y) { return true; }
            if (judge_vec.z > 0 && vec.z > judge_vec.z) { return true; }
            else if (judge_vec.z < 0 && vec.z < judge_vec.z) { return true; }
        }

        return false;
    }

    //�e�m�[�g���画����󂯎��
    public void JudgementNote(GameObject obj, int judge, string kind)
    {
        //generatedNotesList.Remove(g);   //�����m�[�g���X�g����폜
        //�X�y�[�X�m�[�g��������
        if (kind.Contains("space")) {
            InstantiateSpaceJudge(judge);  //�����\��
        }
        else{
            InstantiateGeneralJudge(judge);  //�����\��
            if (judgeEffects[judge]) 
            {
                GameObject j = Instantiate(judgeEffects[judge], obj.transform.position, Quaternion.identity, effect_pre.transform);
                j.transform.position = new Vector3(obj.transform.position.x, 0, JUDGE_GROUND_Z);
                j.transform.eulerAngles = obj.transform.GetChild(0).transform.eulerAngles;
            }
        }

        //�R���{���Z
        if(judge != 3){ combo++; }
        else { combo = 0; }
        uiCtrl.AddCombo(combo);

        //���萔�����Z
        judgesList[judge].sum++;
        switch (kind)
        {
            case "general":
                judgesList[judge].general_num++;
                break;
            case "hold":
                judgesList[judge].hold_num++;
                break;
            case "dynamic_space":
                judgesList[judge].dynamic_num++;
                break;
            case "spaceHold":
                judgesList[judge].spaceHold_num++;
                break;
        }
    }

    //�ʏ�m�[�g����I�u�W�F�N�g�̕\��
    public void InstantiateGeneralJudge(int judge)
    {
         Instantiate(judge_obj[judge], judge_transform.position, Quaternion.identity, judge_pre.transform);
    }

    //���m�[�g����I�u�W�F�N�g�̕\��
    public void InstantiateSpaceJudge(int judge)
    {
        Instantiate(judge_space_obj[judge], judge_transform.position, Quaternion.identity, judge_pre.transform);
    }

    //�L�[�r�[���̕\����\��
    public void DisKeyBeam(int num, bool isActive)
    {
        keyBeam_obj[num].SetActive(isActive);
    }

    //---------------���Ԋ֌W---------------

    //���ݎ��Ԃ����ʃf�[�^�̎��̃f�[�^��Time�𒴂������ǂ����Ԃ�
    private bool IsReturnOverNextTime(float t)
    {
        if (t - note_generate_time <= game_time) { return true; }
        return false;
    }

    //�߂����������Ԃ����߂��ׂ��A������ƃm�[�g��i�߂�
    private Vector3 ReturnLittleDistance(float time)
    {
        return -Vector3.forward * Mathf.Abs(game_time - time - note_generate_time) * speed * Time.fixedDeltaTime;
    }

    //���ݎ��Ԃ�Ԃ�
    public float ReturnNowTime()
    {
        return game_time;
    }

    //---------------���̑�------------------

    //���ʃf�[�^���󂯎��
    public void SetGameData(List<NotesBlock> data)
    {
        score_data = data;
    }

    //�X���C�h���^�b�`���ꂽ�u�Ԃ��Ԃ�
    public bool IsReturnSliderFirstTouch(int num)
    {
        return slider.IsReturnSliderFirstTouch(num);
    }

    //�X���C�h���^�b�`����Ă���Œ����Ԃ�
    public bool IsReturnSliderTouching(int num)
    {
        return slider.IsReturnSliderTouching(num);
    }

    //�Ō�̃m�[�g���������ꂽ���Ԃ�
    public bool IsReturnJudgeLastNote()
    {
        return note_pre.transform.childCount == 0 ? true : false;
    }

    //�R���{�]����Ԃ�
    public ComboRank ReturnComboRank()
    {
        if (judgesList[3].sum == 0)
        {
            //AP
            if(judgesList[2].sum == 0 && judgesList[1].sum == 0) { return ComboRank.AllPerfect; }
            //FC
            else { return ComboRank.FullCombo; }
        }
        //�~�X�����邽�߃g���R��
        else { return ComboRank.TrackComplete; }
    }

    //�X�R�A���烉���N��Ԃ�
    public ScoreRank ReturnScoreRank(int score)
    {
        if (score == 1000000) { return ScoreRank.MAX; }
        else if (score > 990000) { return ScoreRank.S_plus; }
        else if (score > 975000) { return ScoreRank.S; }
        else if (score > 950000) { return ScoreRank.A_plus; }
        else if (score > 925000) { return ScoreRank.A; }
        else if (score > 900000) { return ScoreRank.B; }
        else if (score > 800000) { return ScoreRank.C; }
        else if (score > 500000) { return ScoreRank.D; }
        else { return ScoreRank.E; }
    }

    //���萔��Ԃ�
    public int[] ReturnJudgeNums()
    { 
        //int[] tmp = new int[Enum.GetNames(typeof(TimingJudge)).Length] �Ȃ񂩃G���[�N����
        int[] tmp = new int[4]
        {
            judgesList[(int)TimingJudge.perfect].sum,
            judgesList[(int)TimingJudge.great].sum,
            judgesList[(int)TimingJudge.good].sum,
            judgesList[(int)TimingJudge.miss].sum
        };
        return tmp;
    }

    //�X�R�A��Ԃ�
    public int ReturnGameScore()
    {
        float score = 0;
        score = (1000000f / (judgesList[0].sum + judgesList[1].sum + judgesList[2].sum + judgesList[3].sum))
            * (judgesList[0].sum + judgesList[1].sum * 0.8f + judgesList[2].sum * 0.5f);
        return (int)score;
    }
}
