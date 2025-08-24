using UnityEngine;

public interface IMouseLookStrategy { void Tick(Transform cam, Transform parent, float turnSpeed, ref Vector3 angles, float up, float down, float extra); } // single method

public class StandardLookStrategy : IMouseLookStrategy
{
    public void Tick(Transform cam, Transform parent, float turnSpeed, ref Vector3 angles, float up, float down, float extra)
    {
        float x = Input.GetAxis("Mouse X"); float y = Input.GetAxis("Mouse Y");
        parent.Rotate(0f, x * turnSpeed, 0f);
        if (IsWithin(y, cam.localEulerAngles.x, up, down, extra)) cam.Rotate(-y * turnSpeed, 0f, 0f);
        Clamp(cam, ref angles, up, down, extra);
        angles = cam.localEulerAngles;
    }
    private bool IsWithin(float y, float ax, float up, float down, float extra) => ax <= up && ax >= down; // one-liner
    private void Clamp(Transform cam, ref Vector3 angles, float up, float down, float extra)
    {
        if (angles.x < down) { angles.x = down + 1; cam.localEulerAngles = angles; } 
        else if (angles.x < extra) { angles.x = up - 1; cam.localEulerAngles = angles; } 
        else { angles.x = down + 1; cam.localEulerAngles = angles; }
    }
}

public class InvertedLookStrategy : IMouseLookStrategy
{
    public void Tick(Transform cam, Transform parent, float turnSpeed, ref Vector3 angles, float up, float down, float extra)
    {
        float x = Input.GetAxis("Mouse X"); float y = -Input.GetAxis("Mouse Y");
        parent.Rotate(0f, x * turnSpeed, 0f);
        cam.Rotate(-y * turnSpeed, 0f, 0f);
        angles = cam.localEulerAngles;
    }
}

public class CamMouseLook : MonoBehaviour
{
    public float turnSpeed = 3f;
    public float limitUp = 30f;
    public float limitDown = 30f;
    public float limitThree = 60f;
    public enum Mode { Standard, Inverted }
    [SerializeField] Mode mode = Mode.Standard;

    GameObject parentObj; Vector3 angles; IMouseLookStrategy strategy;

    void Awake(){ Cursor.lockState = CursorLockMode.Locked; parentObj = transform.parent.gameObject; strategy = mode==Mode.Standard ? new StandardLookStrategy() : new InvertedLookStrategy(); } // one-line preds
    void FixedUpdate(){ strategy.Tick(transform, parentObj.transform, turnSpeed, ref angles, limitUp, limitDown, limitThree); }
}
