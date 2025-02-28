using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class EditorUICtrl : MonoBehaviour
{
    [SerializeField] private EditorCtrl editorCtrl;

    [Header("ロード画面(object)")]
    [SerializeField] private GameObject load_obj;
    [Header("再生ボタン画像")]
    [SerializeField] private Texture play_texture;
    [Header("一時停止ボタン画像")]
    [SerializeField] private Texture pause_texture;
    [Header("スコアフィールドの大本")]
    [SerializeField] private GameObject scoreField_obj;
    [Header("グラウンドのスクロールバー")]
    [SerializeField] private Scrollbar ground_scrollbar;
    [Header("メインフィールド")]
    [SerializeField] private GameObject field_obj;
    [Header("音声ファイルの名前(編集画面)")]
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

    //-----------------フィールド関連-----------------

   
    //フィールドの大きさを変える
    public void ChangeFieldSize(float size)
    {
        field_rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);
    }

    //スクロールバーの位置を変える
    public bool ChangeSliderRatio(float value, bool isAdd)
    {
        if (isAdd) 
        {
            if (ground_scrollbar.value + value > 1 || ground_scrollbar.value + value < 0) 
            { 
                Debug.LogWarning("限界！！！"); 
                return false; 
            }
            ground_scrollbar.value += value;
        }
        else 
        {
            if (value > 1 || value < 0) { Debug.LogError("0～1まででよろ！"); return false; }
            ground_scrollbar.value = value;
        }
        return true;
    }

    //スクロールおさわり？
    public void SetScrollBarEnable(bool b)
    {
        scrollRect.vertical = b;
        ground_scrollbar.interactable = b;
    }

    //スクロールバーの位置(歩合)を返す
    public float ReturnScrollBarRatio()
    {
        return ground_scrollbar.value;
    }

    //-----------------ボタンとか関連-----------------

    //音声名の登録
    public void RegisterMusicName(string str)
    {
        music_name = str;
        foreach (TextMeshProUGUI t in music_name_tmp)
        {
            t.text = str;
        }
    }

    //再生・一時停止ボタン
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

    //停止ボタン
    public void PushStopButton(RawImage playButton_ima)
    {
        editorCtrl.StopScore();
        playButton_ima.texture = play_texture;
    }

    //編集開始ボタン
    public void PushEditStartButton()
    {
        load_obj.SetActive(false);
    }

    //メインBPM
    public void InputBPMBox(TMP_InputField tmp)
    {
        float f;
        if(float.TryParse(tmp.text,out f))
        {
            editorCtrl.RegisterMainBPM(f);
        }
    }

    //メイン音符
    public void RegisterNote(TMP_Dropdown d)
    {
        editorCtrl.RegisterNote(d.value);
    }

    //メインオフセット
    public void InputOffsetBox(TMP_InputField tmp)
    {
        int i;
        if (int.TryParse(tmp.text, out i))
        {
            editorCtrl.RegisterOffset(i);
        }
    }

    //グラウンドのスクロール
    public void ScrollGround(Scrollbar s)
    {
        if (!editorCtrl.ReturnIsPlayScore()) { editorCtrl.GroundVerticalScroll(s.value); }
    }

    //グラウンドの拡大率
    public void MagnificationGround(Slider s)
    {
        editorCtrl.GroundMagnificationChange(s.value);
    }

    //ノート選択ボタン
    public void PushNoteSelectButton(string kind) 
    {
        editorCtrl.ChoiceNote(kind);
    }

    //エクスポートボタン
    public void PushExportButton()
    {
        editorCtrl.ExportScore();
    }

}