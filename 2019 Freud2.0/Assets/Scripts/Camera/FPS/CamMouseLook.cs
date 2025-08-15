using System.Collections;
using UnityEngine;

public class CamMouseLook : MonoBehaviour {
    public float turnSpeed = 3f;
    public float limitUp = 30f;
    public float limitDown = 30f;
    public float limitThree = 60f;
    private GameObject parentObj;
    private Vector3 angles;

    void Awake() {
        Cursor.lockState = CursorLockMode.Locked;
        parentObj = this.transform.parent.gameObject;
    }

    void FixedUpdate() {
        float xturn = Input.GetAxis("Mouse X");
        float yturn = Input.GetAxis("Mouse Y");

        parentObj.transform.Rotate(0f, xturn * turnSpeed, 0f);

        if (IsWithinLimits(angles.x)) {
            transform.Rotate(-yturn * turnSpeed, 0f, 0f);
        }
        else if (angles.x < limitDown) {
            angles.x = limitDown + 1;
            transform.localEulerAngles = angles;
        }
        else if (angles.x < limitThree) {
            angles.x = limitUp - 1;
            transform.localEulerAngles = angles;
        }
        else {
            angles.x = limitDown + 1;
            transform.localEulerAngles = angles;
        }

        angles = transform.localEulerAngles;
    }

    private bool IsWithinLimits(float angle) {
        return angle <= limitUp && angle >= limitDown;
    }
}
