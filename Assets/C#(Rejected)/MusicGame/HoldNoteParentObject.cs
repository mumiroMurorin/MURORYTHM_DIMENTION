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

    //初期化
    public void Init(float s)
    {
        speed = s;
    }

    //ノートの時間を進める
    private void TimeAdvance()
    {
        this.gameObject.transform.position += -Vector3.forward * Time.fixedDeltaTime * speed;
    }
}
