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
		rb = ((Component)this).GetComponent<Rigidbody>();
	}

	private void OnCollisionEnter(Collision collision)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		int num = (int)Mathf.Log((float)LayerMask.op_Implicit(groundLayer), 2f);
		if (collision.gameObject.layer == num)
		{
			speed = 0f;
			ContactPoint val = collision.contacts[0];
			Quaternion val2 = Quaternion.FromToRotation(Vector3.up, ((ContactPoint)(ref val)).normal);
			Vector3 point = ((ContactPoint)(ref val)).point;
			if ((Object)(object)impactPrefab != (Object)null)
			{
				Debug.Log((object)("player: " + ((Object)collision.gameObject).name));
				GameObject val3 = Object.Instantiate<GameObject>(impactPrefab, point, val2);
				Object.Destroy((Object)(object)val3, 5f);
			}
			Object.Destroy((Object)(object)((Component)this).gameObject);
		}
	}

	private void FixedUpdate()
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		if (speed != 0f && (Object)(object)rb != (Object)null)
		{
			Rigidbody obj = rb;
			obj.position += ((Component)this).transform.forward * (speed * Time.deltaTime);
		}
	}
}
