using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterController : MonoBehaviour
{
    [SerializeField] private float speed = 10f;
    private Animator anim;

    [SerializeField] private KeyCode keyEscape = KeyCode.Escape;

    private void Awake() => anim = GetComponent<Animator>();

    private void Start() {
        Cursor.lockState = CursorLockMode.Locked;		
    }
	
    void FixedUpdate() {

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        float translation = v * speed;
        float straffe = h * speed;
        translation *= Time.deltaTime;
        straffe *= Time.deltaTime;

        transform.Translate(straffe, 0, translation);

        Animating(h, v);

        // Use configurable keyEscape instead of hard-coded string
        if (IsEscapePressed())
            Cursor.lockState = CursorLockMode.None;
    }

    private bool IsEscapePressed() => Input.GetKeyDown(keyEscape);

    private void Animating(float h, float v)
    {
        anim.SetBool("IsWalking", IsWalking(h, v));
    }

    private bool IsWalking(float h, float v) => h != 0f || v != 0f;
}
