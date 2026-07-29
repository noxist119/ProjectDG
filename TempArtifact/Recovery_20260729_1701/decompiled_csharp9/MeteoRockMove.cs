using UnityEngine;

public class MeteoRockMove : MonoBehaviour
{
	public float speed;

	public GameObject rockObj;

	public Vector3 rotDir;

	public float rotSpeed;

	[SerializeField]
	private LayerMask groundLayer;

	public GameObject impactPrefab;

	private Rigidbody rb;

	private void Start()
	{
		rb = GetComponent<Rigidbody>();
	}

	private void OnCollisionEnter(Collision collision)
	{
		int layerIndex = (int)Mathf.Log((int)groundLayer, 2f);
		if (collision.gameObject.layer == layerIndex)
		{
			speed = 0f;
			ContactPoint contact = collision.contacts[0];
			Quaternion rot = Quaternion.FromToRotation(Vector3.up, contact.normal);
			Vector3 pos = contact.point;
			if (impactPrefab != null)
			{
				Debug.Log("player: " + collision.gameObject.name);
				GameObject impactVFX = Object.Instantiate(impactPrefab, pos, rot);
				Object.Destroy(impactVFX, 5f);
			}
			Object.Destroy(base.gameObject);
		}
	}

	private void FixedUpdate()
	{
		if (speed != 0f && rb != null)
		{
			rb.position += base.transform.forward * (speed * Time.deltaTime);
		}
	}
}
