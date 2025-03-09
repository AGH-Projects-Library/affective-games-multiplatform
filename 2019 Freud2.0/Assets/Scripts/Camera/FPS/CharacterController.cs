using System.Collections;
using UnityEngine;

public class CharacterController : MonoBehaviour 
{
	public float speed = 10f;
	Animator anim;

	void Awake()
    {
        anim = GetComponent<Animator> ();
    }

	// Use this for initialization
	void Start () {
		Cursor.lockState = CursorLockMode.Locked;		
	}
	
	// Update is called once per frame
	void FixedUpdate () {

		float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

		float translation = v * speed;
		float straffe = h * speed;
		translation *= Time.deltaTime;
		straffe *= Time.deltaTime;

		transform.Translate(straffe, 0, translation);

		Animating(h,v);

		if(Input.GetKeyDown("escape"))
			Cursor.lockState = CursorLockMode.None;
		
	}

	void Animating (float h, float v)
    {
        bool walking = h != 0f || v != 0f;
        anim.SetBool("IsWalking", walking);
    }
}
