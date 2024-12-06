using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;
using System.Linq;
using Deform;

public class PlayGame : MonoBehaviour
{
    //判定数の詳細
    public class Judge
    {
        public int sum = 0;
        public int general_num = 0;
        public int hold_num = 0;
        public int dynamic_num = 0;
        public int spaceHold_num = 0;
    }

    [Header("キネクト？")]
    [SerializeField] private bool isKinect;
    [Header("ノート速度(弄るならこっち)")]
    [SerializeField] private float note_speed;
    [Header("速度倍率")]
    [SerializeField] private float speed_magni = 10;
    [Header("ノートオフセット")]
    [SerializeField] private float note_offset;
    [Header("ホールドノートのメッシュ分割数")]
    [SerializeField] private int holdNoteDivision_num = 10;
    [Header("スペースノートの感度倍率")]
    [SerializeField] private float space_sensitivity;
    [Header("宙判定の間隔")]
    [SerializeField] private float spaceJudge_interval = 0.2f;
    [Header("判定表示場所")]
    [SerializeField] private Transform judge_transform;

    [SerializeField] private UICtrl uiCtrl;
    [SerializeField] private BendDeformer ground_bend;
    [SerializeField] private KinectInput kinect;
    [SerializeField] private SliderInput slider;
    [SerializeField] private GameObject note_obj;
    //[SerializeField] private GameObject marker_obj;

    [Header("通常ノートマテリアル(上からcenter,right,left,1mass)")]
    [SerializeField] private Material[] generalNote_mate;
    [Header("ホールドノートマテリアル(上からcenter,right,left,1mass)")]
    [SerializeField] private Material[] holdNote_mate;
    [Header("ホールドグラウンドマテリアル(上からdefault,touch,miss)")]
    [SerializeField] private Material[] holdNote_ground_mate;
    [Header("ダイナミックノート上オブジェクト(上からcenter,right,left,1mass)")]
    [SerializeField] private GameObject[] dynamicUpNote_obj;
    [Header("ダイナミックノート下オブジェクト(上からcenter,right,left,1mass)")]
    [SerializeField] private GameObject[] dynamicDownNote_obj;
    [Header("ダイナミックグラウンド右オブジェクト(上からcenter,right,left,1mass)")]
    [SerializeField] private GameObject[] dGroundRightNote_obj;
    [Header("ダイナミックグラウンド左オブジェクト(上からcenter,right,left,1mass)")]
    [SerializeField] private GameObject[] dGroundLeftNote_obj;
    [Header("通常判定 上からPerfect")]
    [SerializeField] private GameObject[] judge_obj;
    [Header("スペース判定 上からPerfect")]
    [SerializeField] private GameObject[] judge_space_obj;
    [Header("通常ノーツエフェクト(上からperfect,great,good)")]
    [SerializeField] private GameObject[] judgeEffects;
    [Header("キービーム")]
    [SerializeField] private GameObject[] keyBeam_obj;
    [Header("自身のLoadData")] 
    [SerializeField] private PlayGame playGame;

    const string GENERAL_NOTE = "g";
    const string HOLD_NOTE = "h";
    const string DYNAMIC_UP_NOTE = "d_up";
    const string DYNAMIC_DOWN_NOTE = "d_down";
    const string DYNAMIC_GROUND_RIGHT_NOTE = "d_gro_right";
    const string DYNAMIC_GROUND_LEFT_NOTE = "d_gro_left";

    //地面開始地点
    const float START_GROUND_Z = 183.25f;
    const float JUDGE_GROUND_Z = 0;
    const float END_GROUND_Z = -11.268f;

    //MeshCollider[] markerColliderArray;   //マーカーコライダー配列
    //Vector3[] markerPositionArray;        //マーカー座標配列
    List<Judge> judgesList;              //それぞれ判定の数[perfect,great,good,miss]
    List<NotesBlock> score_data;         //譜面データ

    //private int add_p_score;            //perfect判定毎に追加されるスコア
    private int combo;                //現在コンボ
    private int score;                //スコア
    private float note_generate_time; //ノート生成手前時間※
    private float game_time;          //経過時間
    private float speed;              //スピード
    private bool isPlaying;           //プレイ中？

    private List<Vector3> rightHand_list; //数フレーム前の右手の位置
    private List<Vector3> leftHand_list;  //数フレーム前の左手の位置

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
            GenerateNote();     //ノート生成
            if (isKinect) { UpdateSpaceJudge(); } //宙判定の更新
            InputKey();         //入力関係
            game_time += Time.fixedDeltaTime;    //あまりこの位置は好まれたものじゃないかも…(1フレーム目)
        }
    }

    //初期化 ※ノート事前読み込みは仮
    private void Init()
    {
        //親の削除と生成
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

        //リストの初期化
        //generatedNotesList = new List<GameObject>();
        //markerColliderArray = new MeshCollider[0];
        //markerPositionArray = new Vector3[0];
        score_data = new List<NotesBlock>();
        judgesList = new List<Judge>();
        
        rightHand_list = new List<Vector3>();
        leftHand_list = new List<Vector3>();
        for (int i = 0; i < 4; i++){ judgesList.Add(new Judge()); }

        //各値初期化
        isPlaying = false;
        combo = 0;
        score = 0;
        game_time = 0;
        
        notesData = new NotesData();

        //ノートの初期化
        generalNote_obj = InitNotes(generalNote_mate);
        holdNote_obj = InitNotes(holdNote_mate);
        //一旦ね※
        //dynamicNote_obj = InitNotes(dynamicNote_mate);
        note_original_pre.SetActive(false);

        //markerの設置
        //InstallMarker(18, true);
        //UpdateMarkerPositionList();
    }

    //マテリアルの張られたノーツを返す
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

    //時を動かす
    public void StartGame()
    {
        isPlaying = true;
    }

    /*
    //マーカーを設置(設置数)
    private void InstallMarker(int num, bool isDis)
    {       
        //エラー処理
        if (num < 1) {
            Debug.Log("マーカー数が少なすぎます: " + num);
            return;
        }

        float z;
        markerColliderArray = new MeshCollider[num]; //配列の初期化
        markerPositionArray = new Vector3[num];      //配列の初期化

        for (int n = 0; n < num; n++)
        {
            z = START_GROUND_Z + Mathf.Abs(START_GROUND_Z - END_GROUND_Z) / (num - 1) * n;   //分割したときのマーカーz座標

            //生成、名前、座標、角度、表示、親設定
            GameObject g = Instantiate(marker_obj);
            g.name = "Marker" + n;
            g.transform.position = new Vector3(10 * Mathf.Cos(-90f * Mathf.Deg2Rad), 0, z);
            g.SetActive(isDis);
            g.transform.SetParent(marker_pre.transform);

            markerColliderArray[n] = g.GetComponent<MeshCollider>();    //マーカーリストに追加
        }
    }

    //マーカー座標,角度リストを更新
    private void UpdateMarkerPositionList()
    {
        IEnumerable<Vector3> tmp = markerColliderArray.Select(x => x.bounds.center);
        markerPositionArray = tmp.ToArray();
    }
    */

    //譜面生成関数
    private void GenerateNote()
    {
        //譜面生成終了
        if (score_data.Count == 0) { return; }

        //DOPath使うならこの関数
        //note.transform.DOPath(markerPositionArray, time, PathType.Linear)
        //    .SetLookAt(0.001f).SetEase(Ease.Linear);
        //時間の計算
        //float time = Mathf.Abs(START_GROUND_Z - END_GROUND_Z) / speed;

        //データ上の時間を超過する限り繰り返す
        while (score_data.Count != 0 && IsReturnOverNextTime(score_data[0].time + note_offset))
        {
            //通常ノーツの生成
            foreach (GeneralNote g in score_data[0].general_list){
                g.obj.transform.position += ReturnLittleDistance(g.time);
                g.obj.SetActive(true);
            }

            //ホールドノーツの生成
            foreach (HoldNote h in score_data[0].hold_list){
                h.obj.transform.position += ReturnLittleDistance(h.time);
                h.obj.SetActive(true);
            }

            //ダイナミックノーツの生成
            foreach (DynamicNote d in score_data[0].dynamic_list)
            {
                d.obj.transform.position += ReturnLittleDistance(d.time);
                d.obj.SetActive(true);
            }

            score_data.RemoveAt(0);
        }
    }

    //ノートの事前生成
    public void GenerateNoteInAdvance()
    {
        //全てのノーツを事前生成
        for (int i = 0; i < score_data.Count; i++)
        {
            //通常ノーツの生成
            foreach (GeneralNote g in score_data[i].general_list)
            {
                GeneralNote new_g = OffsetGeneralNote(g);
                g.obj = InstantiateGeneralNote(new_g);
                g.obj.SetActive(false);
                //generatedNotesList.Add(note);
            }

            //ホールドノーツの生成
            foreach (HoldNote h in score_data[i].hold_list)
            {
                HoldNote new_h = OffsetHoldNote(h);
                if (new_h.isStart) { h.obj = ReturnHoldNote(new_h, null, 0); }
                h.obj.SetActive(false);
                //generatedNotesList.Add(note);
            }

            //ダイナミックノーツの生成
            foreach (DynamicNote d in score_data[i].dynamic_list)
            {
                DynamicNote new_d = OffsetDynamicNote(d);
                d.obj = InstantiateDynamicNote(new_d);
                d.obj.SetActive(false);
            }
        }
    }

    //----------------ノート生成関係-------------------

    //-----------通常ノート-------------

    //通常ノートを生成する
    private GameObject InstantiateGeneralNote(GeneralNote g)
    {
        GameObject pre = new GameObject("General_pre");
        GameObject obj = ReturnReSizeNote(g.judge_lane[1] - g.judge_lane[0] + 1, "g");    //1マスノートを組み合わせてオブジェクトを生成
        
        //ノート角度、座標、親の設定
        float deg = (g.judge_lane[0] + (g.judge_lane[1] - g.judge_lane[0]) / 2f - 7.5f) * 11.25f;
        obj.transform.eulerAngles = new Vector3(0, 0, deg);
        obj.transform.SetParent(pre.transform);
        pre.transform.position = new Vector3(0, 0, START_GROUND_Z);
        pre.transform.SetParent(note_pre.transform);
        pre.AddComponent<GeneralNoteObject>().Init(playGame, g, speed);  //GeneralNoteObjectを追加して初期化

        return pre;
    }

    //---------ホールドノート-----------

    //包括して生成されたホールドノートを返す
    private GameObject ReturnHoldNote(HoldNote h, GameObject p, float add_z)
    {
        //始点
        if (h.isStart)
        {
            //ホールドノートの祖の設定
            p = new GameObject("HoldNote");
            p.transform.position = new Vector3(0, 0, START_GROUND_Z);
            p.transform.SetParent(note_pre.transform);
            p.AddComponent<HoldNoteParentObject>().Init(speed);
            add_z = 0;
            //次のノーツへ
            ReturnHoldNote(h.next, p, add_z + (h.next.time - h.time) * speed);
            //始点生成
            GameObject obj = ReturnHoldEdge(h);
            obj.transform.SetParent(p.transform);
            obj.transform.localPosition = Vector3.zero;
            return p;
        }
        //終点
        else if (h.isGoal)
        {
            //終点生成
            GameObject obj = ReturnHoldEdge(h);
            obj.transform.SetParent(p.transform);
            obj.transform.localPosition = new Vector3(0, 0, add_z);
            //返す
            return p;
        }
        //グラウンド
        else
        {
            ReturnHoldNote(h.next, p, add_z + (h.next.time - h.time) * speed);
            GameObject obj = ReturnHoldGround(h, h.next);
            obj.transform.SetParent(p.transform);
            obj.transform.localPosition = new Vector3(0, 0, add_z);
            //次のノーツへ
            return p;
        }
    }

    //ホールドノートを生成する(終点と始点)
    private GameObject ReturnHoldEdge(HoldNote h)
    {
        //h = OffsetHoldNote(h);
        GameObject obj = CreateHoldEdge(h);

        //始点
        if (h.isStart) 
        {
            //メッシュの付属
            GameObject g = CreateHoldMesh(h, h.next);
            g.transform.position = obj.transform.position;
            g.transform.SetParent(obj.transform);
            obj.AddComponent<HoldNoteStart>().Init(playGame, notesData.ConvertHoldStarToGeneralNote(h), g, holdNote_ground_mate, speed);
        }
        //終点
        else { obj.AddComponent<HoldNoteEnd>().Init(playGame, h, speed); }

        return obj;
    }

    //ホールドノートオブジェクトを生成する(終点と始点)
    private GameObject CreateHoldEdge(HoldNote h)
    {
        GameObject pre = new GameObject("HoldEdge");
        GameObject obj = ReturnReSizeNote((int)h.judge_lane[1] - (int)h.judge_lane[0] + 1, "h");    //1マスノートを組み合わせてオブジェクトを生成

        //ノート角度、座標、親の設定
        float deg = (h.judge_lane[0] + (h.judge_lane[1] - h.judge_lane[0]) / 2f - 7.5f) * 11.25f;
        obj.transform.eulerAngles = new Vector3(0, 0, deg);
        obj.transform.SetParent(pre.transform);

        return pre;
    }

    //ホールドノートを生成する(グラウンド)
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

    //ホールドノートオブジェクトを生成する(メッシュ)
    private GameObject CreateHoldMesh(HoldNote former, HoldNote latter)
    {
        GameObject obj = new GameObject("mesh");
        MeshFilter meshFilter = obj.AddComponent<MeshFilter>();
        Mesh mesh = new Mesh();
        MeshRenderer meshRenderer = obj.AddComponent<MeshRenderer>();
        meshFilter.mesh = mesh;

        float h = speed * (latter.time - former.time);   //zの長さ
        //ノート端～端(judge_lane[0]～ [1])までに入る分割インデックス
        List<float> index_f = new List<float>();
        List<float> index_l = new List<float>();

        //横2辺の傾きを導出
        float a_0 = h / (latter.judge_lane[0] - former.judge_lane[0]);
        float a_1 = h / (latter.judge_lane[1] - former.judge_lane[1]);

        //分割インデックスリストに各メッシュ頂点を追加
        float first = former.judge_lane[0], end = former.judge_lane[1] + 1;
        if (a_0 < float.PositiveInfinity && a_0 < 0) //左辺の傾きが負の場合のみ変更
        {
            index_f.Add(former.judge_lane[0]);
            index_l.Add(former.judge_lane[0]);
            first = latter.judge_lane[0];
        }
        if (a_1 < float.PositiveInfinity && a_1 > 0) //右辺の傾きが正の場合のみ変更
        {
            index_f.Add(former.judge_lane[1] + 1);
            index_l.Add(former.judge_lane[1] + 1);
            end = latter.judge_lane[1] + 1;
        }
        index_f.AddRange(ReturnMeshPointList(first, end, holdNoteDivision_num)); //リストに代入

        first = latter.judge_lane[0];
        end = latter.judge_lane[1] + 1;
        if (a_0 < float.PositiveInfinity && a_0 > 0) //左辺の傾きが正の場合のみ変更
        {
            index_l.Add(latter.judge_lane[0]);
            index_f.Add(latter.judge_lane[0]);
            first = former.judge_lane[0];
        }
        if (a_1 < float.PositiveInfinity && a_1 < 0) //右辺の傾きが負の場合のみ変更
        {
            index_l.Add(latter.judge_lane[1] + 1);
            index_f.Add(latter.judge_lane[1] + 1);
            end = former.judge_lane[1] + 1;
        }

        index_l.AddRange(ReturnMeshPointList(first, end, holdNoteDivision_num)); //リストに代入

        index_f.Sort();                         //ソート
        index_l.Sort();                         //ソート
        index_f = index_f.Distinct().ToList();  //重複要素削除
        index_l = index_l.Distinct().ToList();  //重複要素削除
        if (a_0 < float.PositiveInfinity && a_0 != 0) { index_l.Remove(first); }//傾きが0でない場合、最初の値を削除
        if (a_1 < float.PositiveInfinity && a_1 != 0) { index_f.Remove(end); }  //傾きが0でない場合、最後の値を削除

        //点の座標を導出(奥行の部分をかんがえる)
        List<Vector3> vertices_f = new List<Vector3>();   //前ノート頂点座標
        List<Vector3> vertices_l = new List<Vector3>();   //後ノート頂点座標

        foreach (float f in index_f)
        {
            float deg = (f - 16) * 11.25f * Mathf.Deg2Rad;
            float z = 0;
            if (f < former.judge_lane[0])   //斜め左辺の場合※
            { z = a_0 * (f - latter.judge_lane[0]) + h; }
            else if (f > former.judge_lane[1] + 1)  //斜め右辺の場合※
            { z = a_1 * (f - former.judge_lane[1] - 1); }
            vertices_f.Add(new Vector3(10 * Mathf.Cos(deg), 10 * Mathf.Sin(deg), z));
        }

        foreach (float f in index_l)
        {
            float deg = (f - 16) * 11.25f * Mathf.Deg2Rad;
            float z = speed * (latter.time - former.time);
            if (f < latter.judge_lane[0])   //斜め左辺の場合※
            { z = a_0 * (f - former.judge_lane[0]); }
            else if (f > latter.judge_lane[1] + 1)  //斜め右辺の場合※ 
            { z = a_1 * (f - latter.judge_lane[1] - 1) + h; }
            vertices_l.Add(new Vector3(10 * Mathf.Cos(deg), 10 * Mathf.Sin(deg), z));
        }

        //メッシュ生成番号の割り振り
        List<int> triangles = new List<int>();  //メッシュ生成番号
        vertices_f.AddRange(vertices_l);        //結合
        int num = 0, harf = (vertices_f.Count - 1) / 2;
        if (vertices_f.Count % 2 != 0 && a_0 >= float.PositiveInfinity) { harf = (vertices_f.Count - 1) / 2 - 1; }

        //メッシュを計算
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

        //右辺の傾きのみ0(無限)のとき、最後のメッシュを追加
        if (a_0 >= float.PositiveInfinity && a_1 < float.PositiveInfinity)
        {
            triangles.Add(num + 1 + harf);
            triangles.Add(num + 2 + harf);
            triangles.Add(num);
        }

        //メッシュの点と面を設定して再計算
        mesh.vertices = vertices_f.ToArray(); //代入
        mesh.triangles = triangles.ToArray(); //代入
        mesh.RecalculateNormals();

        //Debug.Log("頂点list: " + string.Join(",", vertices_f.Select(n => n.ToString())));
        //Debug.Log("メッシュlist: " + string.Join(",", triangles.Select(n => n.ToString())));

        //Deformの設定
        Deformable d = obj.AddComponent<Deformable>();
        d.AddDeformer(ground_bend);

        //親とかいつもの設定
        GameObject pre = new GameObject("HoldGround");
        obj.transform.SetParent(pre.transform);

        return pre;
    }

    //---------ダイナミックノート----------

    //ダイナミックノートを生成する
    private GameObject InstantiateDynamicNote(DynamicNote d)
    {
        //d = OffsetDynamicNote(d);
        GameObject pre = new GameObject("Dynamic_pre");
        GameObject obj = ReturnReSizeNote(d.judge_lane[1] - d.judge_lane[0] + 1, d.kind);    //1マスノートを組み合わせてオブジェクトを生成

        //ノート角度、座標、親の設定
        float deg = (d.judge_lane[0] + (d.judge_lane[1] - d.judge_lane[0]) / 2f - 7.5f) * 11.25f;
        obj.transform.eulerAngles = new Vector3(0, 0, deg);
        obj.transform.SetParent(pre.transform);
        pre.transform.position = new Vector3(0, 0, START_GROUND_Z);
        pre.transform.SetParent(note_pre.transform);
        pre.AddComponent<DynamicNoteObject>().Init(playGame, d, speed);  //GeneralNoteObjectを追加して初期化

        return pre;
    }

    //------------その他-------------

    //引数の長さのノートを返す
    private GameObject ReturnReSizeNote(int size, string k)
    {
        string name;
        GameObject[] notes;

        //名前と複製ノートを決定
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
                Debug.Log("この種類のノートはありません: " + k);
                return null;
        }

        Vector3 pos, rot;
        GameObject pre = new GameObject(name);   //まとめ役のオブジェクト生成
        for(int i = 0; i < size; i++)
        {   
            //ポジションと角度の計算
            pos = new Vector3(10 * Mathf.Cos((((size - 1) / 2f - i) * 11.25f - 90f) * Mathf.Deg2Rad), 10 * Mathf.Sin((((size - 1) / 2f - i) * 11.25f - 90f) * Mathf.Deg2Rad), 0);
            rot = new Vector3(0, 0, ((size - 1) / 2f - i) * 11.25f);

            //1マスノートの時
            if (size == 1) { Instantiate(notes[3], pos, Quaternion.Euler(rot), pre.transform); }
            //ノート左端の時
            else if (i == 0) { Instantiate(notes[2], pos, Quaternion.Euler(rot), pre.transform); }
            //ノート右端の時
            else if (i == size - 1) { Instantiate(notes[1], pos, Quaternion.Euler(rot), pre.transform); }
            //ノート中の時
            else { Instantiate(notes[0], pos, Quaternion.Euler(rot), pre.transform); }
        }

        //Deformの設定
        foreach(Transform t in pre.transform)
        {
            Deformable d = t.GetComponentInChildren<Deformable>();
            d.AddDeformer(ground_bend);
        }

        return pre;
    }

    //範囲内のメッシュ頂点リストを返す(endに+1することを忘れないように)
    private List<float> ReturnMeshPointList(float first, float end, int div_num)
    {
        if(div_num == 0)
        {
            Debug.LogError("メッシュ分割数が0です");
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

    //通常ノートのオフセットの設定
    private GeneralNote OffsetGeneralNote(GeneralNote g)
    {
        g.judge_time = Array.ConvertAll(g.judge_time, i => i + note_offset);
        g.time += note_offset;
        return g;
    }

    //ホールドノートのオフセットの設定(重ねて次のホールドノートのオフセも弄っているので注意)
    private HoldNote OffsetHoldNote(HoldNote h)
    {
        if (h.isJudge) { h.judge_time = Array.ConvertAll(h.judge_time, i => i + note_offset); }
        h.time += note_offset;
        if (h.next != null) { h.next = OffsetHoldNote(h.next); }
        return h;
    }

    //ダイナミックノートのオフセットの設定
    private DynamicNote OffsetDynamicNote(DynamicNote d)
    {
        d.judge_time = Array.ConvertAll(d.judge_time, i => i + note_offset);
        d.time += note_offset;
        return d;
    }

    //----------------入力・判定関係-------------------

    //初期スライダー設定
    public void SetSliderOption(RootOption r)
    {
        slider.SetRootOption(r);
    }

    //初期KINECT設定
    public void SetKinectOption(RootOption r)
    {
        isKinect = r.isUseKinect;
        kinect.SetRootOption(r);
    }

    //初期SpaceSensitivity設定
    public void SetSpaceSensitivityOption(RootOption r)
    {
        space_sensitivity = r.space_sensitivity;
    }

    //初期スピード、オフセ設定
    public void SetMusicGameOption(float s, float offset)
    {
        note_speed = s;
        note_offset = offset * Time.fixedDeltaTime;
        speed = note_speed * speed_magni;
        //※一旦この値(見直せ)
        //note_generate_time = Mathf.Abs(START_GROUND_Z + Mathf.Abs(JUDGE_GROUND_Z - END_GROUND_Z) - END_GROUND_Z) / speed;
        note_generate_time = Mathf.Abs(START_GROUND_Z - JUDGE_GROUND_Z) / speed;
    }

    //キネクトがトラッキングしているかどうか返す
    public bool IsReturnKinectTracking()
    {
        return kinect.IsReturnTracking();
    }

    //ボタン入力関係
    private void InputKey()
    {
        //全ての入力を調べる
        for(int i = 0; i < 16; i++)
        {
            //キービーム
            DisKeyBeam(i, slider.IsReturnSliderTouching(i));
        }

        if (Input.GetKeyDown(KeyCode.Escape)) { SceneManager.LoadScene("MusicSelectScene"); }
    }

    //宙判定を更新する(FixedUpdate)
    private void UpdateSpaceJudge()
    {
        rightHand_list.Add(kinect.ReturnHandPos(true));
        leftHand_list.Add(kinect.ReturnHandPos(false));
        //Debug.Log($"{rightHand_list[rightHand_list.Count - 1]} , {leftHand_list[leftHand_list.Count - 1]}");

        if (game_time < spaceJudge_interval) { return; }

        rightHand_list.RemoveAt(0);
        leftHand_list.RemoveAt(0);
    }

    //宙アクションしたか返す
    public bool IsReturnSpaceAction(Vector3 judge_vec)
    {
        judge_vec *= space_sensitivity; //倍率
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

    //各ノートから判定を受け取る
    public void JudgementNote(GameObject obj, int judge, string kind)
    {
        //generatedNotesList.Remove(g);   //生成ノートリストから削除
        //スペースノートだった時
        if (kind.Contains("space")) {
            InstantiateSpaceJudge(judge);  //判定を表示
        }
        else{
            InstantiateGeneralJudge(judge);  //判定を表示
            if (judgeEffects[judge]) 
            {
                GameObject j = Instantiate(judgeEffects[judge], obj.transform.position, Quaternion.identity, effect_pre.transform);
                j.transform.position = new Vector3(obj.transform.position.x, 0, JUDGE_GROUND_Z);
                j.transform.eulerAngles = obj.transform.GetChild(0).transform.eulerAngles;
            }
        }

        //コンボ加算
        if(judge != 3){ combo++; }
        else { combo = 0; }
        uiCtrl.AddCombo(combo);

        //判定数を加算
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

    //通常ノート判定オブジェクトの表示
    public void InstantiateGeneralJudge(int judge)
    {
         Instantiate(judge_obj[judge], judge_transform.position, Quaternion.identity, judge_pre.transform);
    }

    //宙ノート判定オブジェクトの表示
    public void InstantiateSpaceJudge(int judge)
    {
        Instantiate(judge_space_obj[judge], judge_transform.position, Quaternion.identity, judge_pre.transform);
    }

    //キービームの表示非表示
    public void DisKeyBeam(int num, bool isActive)
    {
        keyBeam_obj[num].SetActive(isActive);
    }

    //---------------時間関係---------------

    //現在時間が譜面データの次のデータのTimeを超えたかどうか返す
    private bool IsReturnOverNextTime(float t)
    {
        if (t - note_generate_time <= game_time) { return true; }
        return false;
    }

    //過ぎ去った時間を取り戻すべく、ちょっとノートを進める
    private Vector3 ReturnLittleDistance(float time)
    {
        return -Vector3.forward * Mathf.Abs(game_time - time - note_generate_time) * speed * Time.fixedDeltaTime;
    }

    //現在時間を返す
    public float ReturnNowTime()
    {
        return game_time;
    }

    //---------------その他------------------

    //譜面データを受け取る
    public void SetGameData(List<NotesBlock> data)
    {
        score_data = data;
    }

    //スライドがタッチされた瞬間か返す
    public bool IsReturnSliderFirstTouch(int num)
    {
        return slider.IsReturnSliderFirstTouch(num);
    }

    //スライドがタッチされている最中か返す
    public bool IsReturnSliderTouching(int num)
    {
        return slider.IsReturnSliderTouching(num);
    }

    //最後のノートが処理されたか返す
    public bool IsReturnJudgeLastNote()
    {
        return note_pre.transform.childCount == 0 ? true : false;
    }

    //コンボ評価を返す
    public ComboRank ReturnComboRank()
    {
        if (judgesList[3].sum == 0)
        {
            //AP
            if(judgesList[2].sum == 0 && judgesList[1].sum == 0) { return ComboRank.AllPerfect; }
            //FC
            else { return ComboRank.FullCombo; }
        }
        //ミスがあるためトラコン
        else { return ComboRank.TrackComplete; }
    }

    //スコアからランクを返す
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

    //判定数を返す
    public int[] ReturnJudgeNums()
    { 
        //int[] tmp = new int[Enum.GetNames(typeof(TimingJudge)).Length] なんかエラー起きる
        int[] tmp = new int[4]
        {
            judgesList[(int)TimingJudge.perfect].sum,
            judgesList[(int)TimingJudge.great].sum,
            judgesList[(int)TimingJudge.good].sum,
            judgesList[(int)TimingJudge.miss].sum
        };
        return tmp;
    }

    //スコアを返す
    public int ReturnGameScore()
    {
        float score = 0;
        score = (1000000f / (judgesList[0].sum + judgesList[1].sum + judgesList[2].sum + judgesList[3].sum))
            * (judgesList[0].sum + judgesList[1].sum * 0.8f + judgesList[2].sum * 0.5f);
        return (int)score;
    }
}
