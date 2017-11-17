using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class Player2d : MonoBehaviour {

    Rigidbody2D rb;
    Vector2 velocity;
    
    void Start () {
        rb = GetComponent<Rigidbody2D> ();
    }

    void Update () {
        velocity = new Vector2 (Input.GetAxisRaw ("Horizontal"), Input.GetAxisRaw ("Vertical")).normalized * 10;
    }

    void FixedUpdate() {
        rb.MovePosition (rb.position + velocity * Time.fixedDeltaTime);
    }
}
