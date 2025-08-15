using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 6f;
    public float speedRotor = 2f;

    Vector3 movement;

    Vector3 rotate;
    Animator anim;
    Rigidbody playerRigidbody;
    int  floorMask;
    [SerializeField] private float camRayLength = 100f;

    void Awake()
    {
        floorMask = LayerMask.GetMask("Floor");
        anim = GetComponent<Animator>();
        playerRigidbody = GetComponent<Rigidbody>();
    }
    
    void FixedUpdate() //called every physics step
    {
        // mapping from keyboard, values = {-1,0,1}
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        float r = Input.GetAxisRaw("Mouse X");
        float t = Input.GetAxisRaw("Mouse Y");

        LogManager.Instance.AddEvent(Time.time, "Joystick;Left;Horizontal;" + h + ";Vertical;" + v);
        LogManager.Instance.AddEvent(Time.time, "Joystick;Right;Horizontal;" + r + ";Vertical;" + t);
        
        TurningAndMoving(r, t, h, v);
        Animating(h, v);
    }

    void TurningAndMoving(float r, float t, float h, float v)
    {
        float r_pos = HasSignificantInput(r) ? r : 0.0f;
        float t_pos = HasSignificantInput(t) ? t : 0.0f;

        rotate.Set(0.0f, r_pos, 0.0f);
        Quaternion deltaRotation = Quaternion.Euler(rotate * Time.deltaTime * speedRotor * 1000);
        playerRigidbody.MoveRotation(playerRigidbody.rotation * deltaRotation);

        movement.Set(h, 0f, v);
        movement = movement.normalized * speed * Time.deltaTime;
        playerRigidbody.MovePosition(transform.position + movement);
    }

    private bool HasSignificantInput(float input)
    {
        return Mathf.Abs(input) > 0.01f;
    }

    private bool IsWalking(float h, float v)
    {
        return h != 0f || v != 0f;
    }

    void Animating(float h, float v)
    {
        anim.SetBool("IsWalking", IsWalking(h, v));
    }
       
}
