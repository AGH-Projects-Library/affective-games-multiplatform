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
    float camRayLength = 100f;    

    void Awake()
    {
        floorMask = LayerMask.GetMask("Floor");
        anim = GetComponent<Animator> ();
        playerRigidbody = GetComponent<Rigidbody> ();
    }

    void FixedUpdate() //called every physics step
    {
        // mapping from keyboard, values = {-1,0,1}
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        float r = Input.GetAxisRaw("Mouse X");
        float t = Input.GetAxisRaw("Mouse Y");

        LogManager.logManager.AddEvent(Time.time, "Joystick;Left;Horizontal;" + h + ";Vertical;" + v);
        LogManager.logManager.AddEvent(Time.time, "Joystick;Right;Horizontal;" + r + ";Vertical;" + t);

        // Move(h, v);
        // Turning();
        TurningAndMoving(r, t, h, v);
        Animating(h, v);
    }

    // void Move (float h, float v)
    // {
    //     movement.Set(h, 0f, v);
    //     movement = movement.normalized * speed *  Time.deltaTime;

    //     playerRigidbody.MovePosition(transform.position + movement);
    // }

    void TurningAndMoving(float r, float t, float h, float v)
    {
        float r_pos = 0.0f;
        float t_pos = 0.0f;

        if(Mathf.Abs(r) > 0.01f)
        {
            r_pos = r;
        }

        if(Mathf.Abs(t) > 0.01f)
        {
            t_pos = t;
        }

        rotate.Set(0.0f, r_pos, 0.0f);
        
        // transform.Rotate(rotate * Time.deltaTime * speedRotor * 1000) ;

        movement.Set(h, 0f, v);
        movement = movement.normalized * speed *  Time.deltaTime;

        Quaternion deltaRotation = Quaternion.Euler(rotate * Time.deltaTime * speedRotor * 1000);
        playerRigidbody.MoveRotation(playerRigidbody.rotation * deltaRotation);

        playerRigidbody.MovePosition(transform.position + movement);
    
    }
    void Turning()
    {
        Ray camRay = Camera.main.ScreenPointToRay(Input.mousePosition);

        RaycastHit floorHit;

        if(Physics.Raycast(camRay, out floorHit, camRayLength, floorMask))
        {
            Vector3 playerToMouse = floorHit.point - transform.position;
            playerToMouse.y = 0f;

            Quaternion newRotation = Quaternion.LookRotation(playerToMouse);
            playerRigidbody.MoveRotation(newRotation);
        }
    }

    void Animating (float h, float v)
    {
        bool walking = h != 0f || v != 0f;
        anim.SetBool("IsWalking", walking);
    }
       
}
