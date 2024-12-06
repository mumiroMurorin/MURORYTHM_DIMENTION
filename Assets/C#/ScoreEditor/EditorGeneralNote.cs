using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EditorGeneralNote : MonoBehaviour
{
    EditorCtrl editor;
    GameObject note;
    GameObject left_obj;
    GameObject right_obj;
    SpriteRenderer spriteRenderer;
    BoxCollider mainCollider;
    BoxCollider leftCollider;
    BoxCollider rightCollider;
    Camera _camera;

    bool isEditLength;
    bool isEditLengthRight;
    int right_num, left_num;
    Vector3 mouseDown_pos;

    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnMouseDown()
    {
        editor.ChangeCursolMode(false);
        //長さ編集
        if (isEditLength)
        {

        }
        //場所編集
        else
        {
            mouseDown_pos = Input.mousePosition;
            EnableCollider(false);
        }
    }

    private void OnMouseUp()
    {
        editor.ChangeCursolMode(true);
        //長さ編集
        if (isEditLength)
        {

        }
        //場所編集
        else
        {
            EnableCollider(true);
        }
    }

    private void OnMouseEnter()
    {
        editor.ChangeCursol(2);
    }

    private void OnMouseExit()
    {
        editor.ChangeCursol(0);
    }

    private void OnMouseDrag()
    {
        //長さ編集
        if (isEditLength)
        {
            if (isEditLengthRight)
            {
                
            }
        }
        //場所編集
        else
        {
            // カーソル位置を取得
            Vector3 mousePos = Input.mousePosition;
            mousePos.z = 30;
            Vector3 pos = editor.ReturnNearestNotePos(mousePos);
            pos += new Vector3(((right_num - left_num) % 2) * 0.25f, 0, 0);
            if (pos.y > -1) 
            { 
                note.transform.position = pos;
                RegisterIndex(ReturnIndex(right_obj.transform.position), true);
                RegisterIndex(ReturnIndex(left_obj.transform.position), false);
            }
        }
    }

    //-----------------ノート選択系-----------------

    //初期化
    public void Init()
    {
        note = this.gameObject;
        editor = GameObject.Find("EditorCtrl").GetComponent<EditorCtrl>();
        spriteRenderer = note.GetComponent<SpriteRenderer>();
        mainCollider = note.GetComponent<BoxCollider>();
        right_obj = transform.Find("Right").gameObject;
        rightCollider = right_obj.GetComponent<BoxCollider>();
        left_obj = transform.Find("Left").gameObject;
        leftCollider = left_obj.GetComponent<BoxCollider>();
        _camera = Camera.main;
    }

    //座標番号の登録
    public void RegisterIndex(int num, bool isRight)
    {
        if(num < 0 || num > 15) { return; }
        if (isRight) { right_num = num; }
        else { left_num = num; }
    }

    //拡大縮小ドラッグによる呼び出し
    public void ScalingDrag()
    {
        ScalingNote(left_num, right_num);
    }

    //ノート拡大縮小処理
    private void ScalingNote(int l, int r)
    {
        if (l > r) { return; }
        spriteRenderer.size = new Vector2((r - l + 1) * 5f, 0.3f);
        mainCollider.size = new Vector3((r - l + 1) * 5f, 0.3f, 0.1f);
        note.transform.position = new Vector3((l + (r - l) / 2f) * 0.5f - 3.75f, note.transform.position.y, note.transform.position.z);
        right_obj.transform.localPosition = new Vector3((r - l) * 2.5f, 0, 0);
        left_obj.transform.localPosition = new Vector3((r - l) * -2.5f, 0, 0);
    }

    //ノート端のコライダー処理
    public void OnMouseOverEdge(bool isEdge, bool isRight)
    {
        isEditLength = isEdge;
        isEditLengthRight = isRight;
        if (isEdge) { editor.ChangeCursol(1); }
        else { editor.ChangeCursol(0); }
    }

    //ノートのコライダーオンオフ
    public void EnableCollider(bool b)
    {
        mainCollider.enabled = rightCollider.enabled = leftCollider.enabled = b;
    }

    //座標から横配置番号を返す
    private int ReturnIndex(Vector3 pos)
    {
        return (int)(Mathf.Clamp(pos.x + 4f, 0f, 7.75f) / 0.5f);
    }
}
