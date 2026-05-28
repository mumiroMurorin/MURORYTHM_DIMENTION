using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class HandCaptureObject : MonoBehaviour
{
    [SerializeField] float radius = 10f;
    [SerializeField] GameObject handObject;
    [SerializeField] GameObject verticalLineObj;
    [SerializeField] GameObject horizontalLineObj;
 
    public void OnMoveHandPosition(Vector2 pos)
    {
        handObject.transform.position = new Vector3(pos.x, pos.y, handObject.transform.position.z);

        // x^2 + y^2 = r^2
        float scaleX = Mathf.Sqrt(radius * radius - pos.y * pos.y) * 2f;
        float dy = Mathf.Sqrt(radius * radius - pos.x * pos.x);
        float scaleY = pos.y + dy;

        horizontalLineObj.transform.localScale = new Vector3(scaleX, horizontalLineObj.transform.localScale.y, horizontalLineObj.transform.localScale.z);
        verticalLineObj.transform.localScale = new Vector3(verticalLineObj.transform.localScale.x, scaleY, verticalLineObj.transform.localScale.z);

        horizontalLineObj.transform.position = new Vector3(0f, pos.y, horizontalLineObj.transform.position.z);
        verticalLineObj.transform.position = new Vector3(pos.x, -dy + scaleY / 2f, verticalLineObj.transform.position.z);
    }
}
