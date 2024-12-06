using System.Collections;
using System.IO;
using UnityEngine;
using TMPro;

public class EditorCtrl : MonoBehaviour 
{
    [SerializeField] private EditorAudioCtrl audioCtrl;
    [SerializeField] private EditorUICtrl uiCtrl;
    [SerializeField] private EditorCursol editorCursol;
    [Header("フィールド")]
    [SerializeField] private GameObject field_obj;
    [Header("楽譜親")]
    [SerializeField] private GameObject scoreField_obj;
    [Header("楽譜端親")]
    [SerializeField] private GameObject scoreEdge_obj;
    [Header("小節線")]
    [SerializeField] private GameObject barLine_obj;
    [Header("小節番号")]
    [SerializeField] private GameObject barLineNum_obj;
    [Header("通常ノート")]
    [SerializeField] private GameObject generalNote_obj;
    [Header("MAX拡大率")]
    [SerializeField] private float max_magni = 2000;
    [Header("MIN拡大率")]
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
    private float fieldLength_per_sec = 2;   //1秒ごとに進む距離
    private float main_bpm;                  //メインBPM
    private float music_fulltime;            //音声の時間
    private float score_speed;               //譜面のスピード
    private float score_start_ratio;         //譜面スタート地点の歩合
    private float score_start_z;             //譜面スタート地点のy座標
    private float offset_z;                  //オフセットでずれるy座標
    private float score_length;              //譜面の長さ

    //音符定数
    int[] note_list = new int[11]
    {
        4,8,12,16,24,32,48,64,96,128,192
    };

    private void Start()
    {

    }

    private void FixedUpdate()
    {
        //譜面のスクロール他
        if (isPlayScore)
        {
            AdvanceScore();
        }
    }

    private void Update()
    { 
        //ノートの配置
        if (isArrangementNote)
        {
            UpdateNoteArrangement();
        }
    }

    //------------フィールド系------------

    //フィールドの更新
    public void UpdateField()
    {
        if(music_fulltime == 0 || main_bpm == 0) { return; }
        UpdateVariable();
        RemoveBarLine();
        AddBarLine(0, main_bpm, main_note);
        RegisterOffset(offset);
        uiCtrl.ChangeFieldSize(music_fulltime * fieldLength_per_sec * 1000);
    }

    //スタート、エンドの歩合、座標、譜面スピードの更新(※スタート地点など0)
    private void UpdateVariable()
    {
        //score_start_ratio = uiCtrl.ReturnScoreFieldHeight() / (uiCtrl.ReturnFieldLength() - uiCtrl.ReturnScoreFieldHeight());
        score_start_z = 0;
        score_length = music_fulltime * fieldLength_per_sec;
        score_speed = score_length / (music_fulltime / Time.fixedDeltaTime);
    }

    //小節線(分線)の追加
    public void AddBarLine(int line_num, float bpm, int note)
    {
        //1拍は4分音符のこと。(つまりbpm256は4分音符を1分間に256回刻む)
        //1小節の長さは,4/4だとすると, (fieldLenght_per_sec * 60 * 4) / bpm        
        float bar_length = (fieldLength_per_sec * 60 * 4) / bpm / note;
        int i = 0;
        Vector3[] positions = new Vector3[]{
                new Vector3(-4.5f, 0, 0),               // 開始点
                new Vector3(4.5f, 0, 0),               // 終了点
        };

        //小節(音符)線の追加
        for (float f = score_start_z; f <= score_start_z + score_length; f += bar_length)
        {
            //LineRendererの設定
            GameObject obj = Instantiate(barLine_obj, barLine_par.transform);
            obj.transform.SetParent(barLine_par.transform);
            obj.transform.position = new Vector3(0, 0, f);
            obj.name = ("line " + i);
            obj.SetActive(true);
            LineRenderer l = obj.GetComponent<LineRenderer>();
            l.startWidth = l.endWidth = 0.03f;
            l.SetPositions(positions);
            l.useWorldSpace = false;

            //小節線、小節番号の追加
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

    //小節線、分線、小節番号の削除
    public void RemoveBarLine()
    {
        Destroy(barLine_par);
        Destroy(barNum_par);
        BarLineSet();
    }

    //譜面(ground)の再生時
    private void AdvanceScore()
    {
        //音の再生
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

    //小節、分線、小節番号の親設定
    private void BarLineSet()
    {
        barLine_par = new GameObject("BarLine");
        barLine_par.transform.SetParent(scoreField_obj.transform);
        barNum_par = new GameObject("BarNumber");
        barNum_par.transform.SetParent(scoreEdge_obj.transform);
        barNum_par.transform.localPosition = new Vector3(-5, 0, 0);
    }

    //グラウンドの移動
    private void GroundChangePos(float value, bool isAdd)
    {
        if (isAdd) { field_obj.transform.position -= new Vector3(0, 0, value); }
        else { field_obj.transform.position = new Vector3(0, 0, -value); }
    }

    //判定線がスタート地点を過ぎたか返す
    private bool IsReturnOverStartPoint()
    {
        return score_start_z - offset_z >= field_obj.transform.position.z ? true : false; 
    }

    //-----------------配置系-----------------

    //ノート配置
    private void UpdateNoteArrangement()
    {
        // カーソル位置を取得
        Vector3 mousePosition = Input.mousePosition;
        mousePosition.z = 30;
        Vector3 note_pos = ReturnNearestNotePos(mousePosition);
       
        //配置モード(配置可能な場合)
        if (note_pos.y > -1) {
            NoteArrangeTmp(note_pos, arrangeNote_obj);  //一時設置
            if (Input.GetMouseButtonDown(0)) {
                ArrangementNote(note_pos, arrangeNote_obj); //設置
                return;
            }
        }
    }

    //配置モードとノートモードの変更
    public void ChangeCursolMode(bool isArrange)
    {
        isArrangementNote = isArrange;
        arrangeNote_obj.SetActive(isArrange);
    }

    //クリックした先のノートを返す
    private GameObject ReturnNotePointed(Vector3 pos)
    {
        if(ReturnObjectClickedTag(pos) == "Note") { return ReturnGameObjectClicked(pos); }
        return null;
    }

    //ノートを表示(一時配置)する
    private void NoteArrangeTmp(Vector3 pos, GameObject obj)
    {
        obj.transform.position = pos;
    }

    //ノートを配置する
    private void ArrangementNote(Vector3 pos, GameObject obj)
    {
        obj.transform.position = pos;
        EditorGeneralNote e = Instantiate(obj, scoreField_obj.transform).GetComponent<EditorGeneralNote>();
        e.Init();
        e.EnableCollider(true);
    }

    //ノートの生成
    private GameObject GenerateNote(string kind)
    {
        switch (kind)
        {
            case GENERAL_NOTE:
                return Instantiate(generalNote_obj);
        }
        return null;
    }

    //カーソル先のオブジェクトのタグを返す
    private string ReturnObjectClickedTag(Vector3 mouse_pos)
    {
        Ray rayOrigin = Camera.main.ScreenPointToRay(mouse_pos);
        if (Physics.Raycast(rayOrigin, out RaycastHit hitInfo)) { return hitInfo.collider.tag; }
        return null;
    }

    //クリックした先のオブジェクトを返す
    private GameObject ReturnGameObjectClicked(Vector3 mouse_pos)
    {
        Ray rayOrigin = Camera.main.ScreenPointToRay(mouse_pos);
        if (Physics.Raycast(rayOrigin, out RaycastHit hitInfo)) { return hitInfo.collider.gameObject; }
        return null;
    }

    //-----------------操作系-----------------

    //再生
    public void PlayScore()
    {
        isPlayScore = true;
        uiCtrl.SetScrollBarEnable(false);
    }

    //一時停止
    public void PauseScore()
    {
        audioCtrl.AudioStop(true);
        uiCtrl.SetScrollBarEnable(true);
        isPlayScore = false;
        isPlayMusic = false;
    }

    //停止
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

    //グラウンド側からの縦操作※
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

    //グラウンドの拡大率操作※
    public void GroundMagnificationChange(float f)
    {
        fieldLength_per_sec = Mathf.Clamp01(f) * (max_magni - min_magni) + min_magni;
        //uiCtrl.ChangeFieldSize(music_fulltime * fieldLength_per_sec);
    }

    //ノートの選択
    public void ChoiceNote(string kind)
    {
        isArrangementNote = true;
        arrangeNote_obj = GenerateNote(GENERAL_NOTE);
        arrangeNote_obj.GetComponent<EditorGeneralNote>().Init();
        arrangeNote_obj.GetComponent<EditorGeneralNote>().EnableCollider(false);
        arrangeNote_obj.transform.SetParent(scoreField_obj.transform);
    }

    //-----------------登録系-----------------

    //音声の登録
    public void RegisterAudioClip(AudioClip c, string name)
    {
        audioCtrl.SetAudioClip(c);
        uiCtrl.RegisterMusicName(name);
        music_fulltime = audioCtrl.ReturnAudioFullTime();
        UpdateField();
    }

    //メインBPMの登録
    public void RegisterMainBPM(float f)
    {
        main_bpm = f;
        UpdateField();
    }

    //メイン音符の登録
    public void RegisterNote(int i)
    {
        main_note = note_list[i];
        RemoveBarLine();
        AddBarLine(0, main_bpm, main_note);
    }

    //オフセットの登録※
    public void RegisterOffset(int i)
    {
        offset = i;
        offset_z = offset * Time.fixedDeltaTime * fieldLength_per_sec;
        GroundChangePos(score_start_z + offset_z, false);
    }

    //---------------リターン系---------------
    
    //再生中か返す
    public bool ReturnIsPlayScore()
    {
        return isPlayScore;
    }

    //マウスの位置に一番近いノート配置場所を返す
    public Vector3 ReturnNearestNotePos(Vector3 mouse_pos)
    {
        //レーン
        Vector3 target = Camera.main.ScreenToWorldPoint(mouse_pos);
        Vector3 pos = new Vector3
            ((int)(Mathf.Clamp(target.x + 4f, 0f, 7.75f) / 0.5f) * 0.5f - 3.75f, target.y, 0);

        //分線
        if (ReturnObjectClickedTag(mouse_pos) == "BarLine")
        {
            return pos + new Vector3(0, 0, ReturnGameObjectClicked(mouse_pos).transform.position.z);
        }

        return new Vector3(0, -100, 0);
    }

    /*
    //返す
    private float ReturnMusicTime(float ratio)
    {
        float y = ratio * (uiCtrl.ReturnFieldLength() - uiCtrl.ReturnScoreFieldHeight()) - score_start_z;
        return (y / (uiCtrl.ReturnFieldLength() - uiCtrl.ReturnScoreFieldHeight() * 2)) * audioCtrl.ReturnAudioFullTime();
    }*/

    //-----------------その他------------------

    //エクスポート
    public void ExportScore()
    {

    }

    //カーソルの変更
    public void ChangeCursol(int num)
    {
        editorCursol.ChangeSprite(num);
    }
}