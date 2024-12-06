using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EditorCursol : MonoBehaviour
{
    [SerializeField] private Sprite general_spr;
    [SerializeField] private Sprite scaling_spr;
    [SerializeField] private Sprite rePos_spr;

    SpriteRenderer spriteRenderer;
    Vector3 mousePos, worldPos;

    private void Start()
    {
        spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        //マウス座標の取得
        mousePos = Input.mousePosition;
        worldPos = Camera.main.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 10f));
        transform.position = worldPos;
    }

    //スプライトの変更
    public void ChangeSprite(int num)
    {
        switch (num) {
            case 0:
                spriteRenderer.sprite = general_spr;
                break;
            case 1:
                spriteRenderer.sprite = scaling_spr;
                break;
            case 2:
                spriteRenderer.sprite = rePos_spr;
                break;
        }
    }
}