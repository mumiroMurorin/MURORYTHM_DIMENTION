using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoteChild : MonoBehaviour
{
    [SerializeField] private bool isRight;

    EditorGeneralNote note;

    void Start()
    {
        note = GetComponentInParent<EditorGeneralNote>();
        //�����o�^
        note.RegisterIndex(ReturnIndex(this.gameObject.transform.position), isRight);
    }

    private void OnMouseEnter()
    {
        note.OnMouseOverEdge(true, isRight);
    }

    private void OnMouseExit()
    {
        note.OnMouseOverEdge(false, isRight);
    }

    private void OnMouseDrag()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = 30;
        Vector3 target = Camera.main.ScreenToWorldPoint(mousePos);
        int i = ReturnIndex(target);
        note.RegisterIndex(i, isRight);
        note.ScalingDrag();
    }

    //���W���牡�z�u�ԍ���Ԃ�
    private int ReturnIndex(Vector3 pos)
    {
        return (int)(Mathf.Clamp(pos.x + 4f, 0f, 7.75f) / 0.5f);
    }
}
