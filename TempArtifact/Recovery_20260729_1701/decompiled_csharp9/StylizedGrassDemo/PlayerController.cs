using UnityEngine;

namespace StylizedGrassDemo
{
	public class PlayerController : MonoBehaviour
	{
		public Camera cam;

		private float speed = 15f;

		private float jumpForce = 350f;

		private Rigidbody rb;

		private bool isGrounded;

		public ParticleSystem landBendEffect;

		private RaycastHit raycastHit;

		private void Start()
		{
			rb = GetComponent<Rigidbody>();
			if (!cam)
			{
				cam = Camera.main;
			}
			isGrounded = true;
		}

		private void FixedUpdate()
		{
			Vector3 input = new Vector3(cam.transform.forward.x, 0f, cam.transform.forward.z);
			input = (input * Input.GetAxis("Vertical")).normalized;
			rb.AddForce(input * speed);
			if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
			{
				rb.AddForce(Vector3.up * jumpForce * rb.mass);
				isGrounded = false;
			}
		}

		private void Update()
		{
			if (isGrounded)
			{
				return;
			}
			Physics.Raycast(base.transform.position, -Vector3.up, out raycastHit, 0.5f);
			if ((bool)raycastHit.collider && raycastHit.collider.GetType() == typeof(TerrainCollider))
			{
				isGrounded = true;
				if ((bool)landBendEffect)
				{
					landBendEffect.Emit(1);
				}
			}
		}
	}
}
