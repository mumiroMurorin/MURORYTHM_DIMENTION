using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HoldNoteParentObject : MonoBehaviour
{
    private float speed;

    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        TimeAdvance();
        if(transform.childCount == 0) { Destroy(this.gameObject); }
    }

    //������
    public void Init(float s)
    {
        speed = s;
    }

    //�m�[�g�̎��Ԃ�i�߂�
    private void TimeAdvance()
    {
        this.gameObject.transform.position += -Vector3.forward * Time.fixedDeltaTime * speed;
    }
}
