using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CamMouseLook : MonoBehaviour {

    public float speed = 5f;
    public float turn = 3f;
    public float limit_up = 30;
    public float limit_down = 30;
    public float limit_three = 60;
    GameObject parentObj; 
    Rigidbody otherRb;
    Vector3 angles;
 
    void Awake () 
    {
        Cursor.lockState = CursorLockMode.Locked;
        parentObj = this.transform.parent.gameObject;
        otherRb = parentObj.GetComponent <Rigidbody> ();
        float xstart = Input.mousePosition.x;
        float ystart = Input.mousePosition.y;
    }

    void FixedUpdate () 
    {
        float xturn = Input.GetAxis("Mouse X");
        float yturn = Input.GetAxis ("Mouse Y");

        parentObj.transform.Rotate (0f, xturn * turn, 0f);
        if (angles.x <= limit_up && angles.x >= limit_down) 
        {
            transform.Rotate (-yturn * turn, 0f, 0f);
        }
        else if(angles.x < limit_down) 
        {
            angles.x = limit_down + 1;
            transform.localEulerAngles = angles;
        }

        else if (angles.x < limit_three) 
        {
            angles.x = limit_up - 1;
            transform.localEulerAngles= angles;
        }

        else
        {
            angles.x = limit_down + 1;
            transform.localEulerAngles = angles;
        }

        angles = transform.localEulerAngles;

    }
}