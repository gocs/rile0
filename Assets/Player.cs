using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour {

	Rigidbody rb;
	Vector3 velocity;

	void Start () {
		rb = GetComponent<Rigidbody>();
	}
	
	void Update () {
		velocity = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized * 10;
	}

	void FixedUpdate() {
		rb.MovePosition(rb.position + velocity * Time.fixedDeltaTime);
	}
}
