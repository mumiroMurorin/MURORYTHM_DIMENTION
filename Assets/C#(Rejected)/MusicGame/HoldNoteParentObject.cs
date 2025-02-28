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

    //‰Šú‰»
    public void Init(float s)
    {
        speed = s;
    }

    //ƒm[ƒg‚ÌŠÔ‚ği‚ß‚é
    private void TimeAdvance()
    {
        this.gameObject.transform.position += -Vector3.forward * Time.fixedDeltaTime * speed;
    }
}
