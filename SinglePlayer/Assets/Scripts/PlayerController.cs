using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour {

	private Rigidbody rb;

	void Start() {
		rb = GetComponent<Rigidbody>();
	}

	void Update() {
		float x = Input.GetAxis ("Horizontal");
		float z = Input.GetAxis ("Vertical");

		rb.AddForce (new Vector3 (x, 0.0f, z) * 10);
	}
}
