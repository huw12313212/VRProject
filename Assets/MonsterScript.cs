﻿using UnityEngine;
using System.Collections;

// Require these components when using this script
[RequireComponent(typeof (Animator))]
[RequireComponent(typeof (CapsuleCollider))]
[RequireComponent(typeof (Rigidbody))]
public class MonsterScript : MonoBehaviour
{
	
	public float animSpeed = 1.5f;				// a public setting for overall animator animation speed
	public float lookSmoother = 3f;				// a smoothing setting for camera motion
	public float moveSpeed = 1;
	
	private Animator anim;							// a reference to the animator on the character
	private AnimatorStateInfo currentBaseState;			// a reference to the current state of the animator, used for base layer
	private AnimatorStateInfo layer2CurrentState;	// a reference to the current state of the animator, used for layer 2
	private CapsuleCollider col;					// a reference to the capsule collider of the character
	
	
	static int idleState = Animator.StringToHash("Base Layer.Idle");	
	static int locoState = Animator.StringToHash("Base Layer.Locomotion");			// these integers are references to our animator's states
	static int jumpState = Animator.StringToHash("Base Layer.Jump");				// and are used to check state for various actions to occur
	static int jumpDownState = Animator.StringToHash("Base Layer.JumpDown");		// within our FixedUpdate() function below
	static int fallState = Animator.StringToHash("Base Layer.Fall");
	static int rollState = Animator.StringToHash("Base Layer.Roll");
	static int waveState = Animator.StringToHash("Layer2.Wave");
	static int attackState = Animator.StringToHash("Base Layer.Attack");

	GameManager manager;
	GameObject player;

	GameObject movingTarget;
	
	void Start ()
	{
		// initialising reference variables
		anim = gameObject.GetComponent<Animator>();					  
		col = gameObject.GetComponent<CapsuleCollider>();				
		if(anim.layerCount ==2)
			anim.SetLayerWeight(1, 1);

		manager = GameObject.FindGameObjectWithTag ("GameManager").GetComponent<GameManager> ();

		player = manager.Player;

		movingTarget = player;
	}

	void MoveToTarget()
	{
		//float h = Input.GetAxis("Horizontal");				// setup h variable as our horizontal input axis
		
		float MyRotate = transform.rotation.eulerAngles.y ;
		
		MyRotate %= 360;
		
		Vector3 difPosition = movingTarget.transform.position-transform.position;
		
		float rotate = -(180f*Mathf.Atan2(difPosition.z,difPosition.x)/Mathf.PI) +90;
		
		if (rotate < 0)
			rotate += 360;
		
		
		Debug.Log ("MyRotate:"+MyRotate + " : "+ "Rotate:"+rotate);
		
		float rotateValue = (MyRotate - rotate);
		
		if (rotateValue > 180)
			rotateValue = -(360 - rotateValue) ;
		else if (rotateValue < -180)
			rotateValue = 360 + rotateValue;
		
		rotateValue = -rotateValue;
		
		rotateValue = rotateValue / 10f;
		
		Debug.Log ("rotate Value:"+rotateValue);
		
		if (rotateValue > 1)
			rotateValue = 1;
		else if (rotateValue < -1)
			rotateValue = -1;
		
		
		
		float v = 0.2f;				// setup v variables as our vertical input axis
		
		anim.SetFloat("Speed", v*moveSpeed);							// set our animator's float parameter 'Speed' equal to the vertical input axis				
		anim.SetFloat("Direction", rotateValue); 						// set our animator's float parameter 'Direction' equal to the horizontal input axis		
		anim.speed = animSpeed;								// set the speed of our animator to the public variable 'animSpeed'


	}


	
	void FixedUpdate ()
	{
		MoveToTarget();

		currentBaseState = anim.GetCurrentAnimatorStateInfo(0);	// set our currentState variable to the current state of the Base Layer (0) of animation
		
		if(anim.layerCount ==2)		
			layer2CurrentState = anim.GetCurrentAnimatorStateInfo(1);	// set our layer2CurrentState variable to the current state of the second Layer (1) of animation
		
		/*
		bool attackPressed = Input.GetMouseButtonDown (0);
		
		if (attackPressed) 
		{
			anim.SetBool("Attack",true);
		}
		
		if (currentBaseState.nameHash == attackState)
		{
			
			if(!anim.IsInTransition(0))
			{				
				// reset the Jump bool so we can jump again, and so that the state does not loop 
				anim.SetBool("Attack", false);
			}
		}*/
		
		
		// STANDARD JUMPING
		
		// if we are currently in a state called Locomotion, then allow Jump input (Space) to set the Jump bool parameter in the Animator to true
		if (currentBaseState.nameHash == locoState)
		{
			if(Input.GetButtonDown("Jump"))
			{
				anim.SetBool("Jump", true);
				rigidbody.AddForce(Vector3.up*10);
			}
		}
		
		
		
		// if we are in the jumping state... 
		else if(currentBaseState.nameHash == jumpState)
		{
			//  ..and not still in transition..
			if(!anim.IsInTransition(0))
			{				
				// reset the Jump bool so we can jump again, and so that the state does not loop 
				anim.SetBool("Jump", false);
			}
			
			// Raycast down from the center of the character.. 
			Ray ray = new Ray(transform.position + Vector3.up, -Vector3.up);
			RaycastHit hitInfo = new RaycastHit();
			
			if (Physics.Raycast(ray, out hitInfo))
			{
				// ..if distance to the ground is more than 1.75, use Match Target
				if (hitInfo.distance > 1.75f)
				{
					
					// MatchTarget allows us to take over animation and smoothly transition our character towards a location - the hit point from the ray.
					// Here we're telling the Root of the character to only be influenced on the Y axis (MatchTargetWeightMask) and only occur between 0.35 and 0.5
					// of the timeline of our animation clip
					anim.MatchTarget(hitInfo.point, Quaternion.identity, AvatarTarget.Root, new MatchTargetWeightMask(new Vector3(0, 1, 0), 0), 0.35f, 0.5f);
				}
			}
		}
		
		
		// JUMP DOWN AND ROLL 
		
		// if we are jumping down, set our Collider's Y position to the float curve from the animation clip - 
		// this is a slight lowering so that the collider hits the floor as the character extends his legs
		else if (currentBaseState.nameHash == jumpDownState)
		{
			col.center = new Vector3(0, anim.GetFloat("ColliderY"), 0);
		}
		
		// if we are falling, set our Grounded boolean to true when our character's root 
		// position is less that 0.6, this allows us to transition from fall into roll and run
		// we then set the Collider's Height equal to the float curve from the animation clip
		else if (currentBaseState.nameHash == fallState)
		{
			col.height = anim.GetFloat("ColliderHeight");
		}
		
		// if we are in the roll state and not in transition, set Collider Height to the float curve from the animation clip 
		// this ensures we are in a short spherical capsule height during the roll, so we can smash through the lower
		// boxes, and then extends the collider as we come out of the roll
		// we also moderate the Y position of the collider using another of these curves on line 128
		else if (currentBaseState.nameHash == rollState)
		{
			if(!anim.IsInTransition(0))
			{
				col.center = new Vector3(0, anim.GetFloat("ColliderY"), 0);
				
			}
		}
		// IDLE
		
		// check if we are at idle, if so, let us Wave!
		else if (currentBaseState.nameHash == idleState)
		{
			if(Input.GetButtonUp("Jump"))
			{
				anim.SetBool("Wave", true);
			}
		}
		// if we enter the waving state, reset the bool to let us wave again in future
		if(layer2CurrentState.nameHash == waveState)
		{
			anim.SetBool("Wave", false);
		}
	}
}