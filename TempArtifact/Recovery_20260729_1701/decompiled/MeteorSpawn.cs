using UnityEngine;

public class MeteorSpawn : MonoBehaviour
{
	public GameObject vfx;

	public Transform startPoint;

	public Transform endPoint;

	public Vector3 startPos;

	private void Start()
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		startPos = startPoint.position;
	}

	private void Update()
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		if (Input.GetKeyDown((KeyCode)109))
		{
			GameObject obj = Object.Instantiate<GameObject>(vfx, startPos, Quaternion.identity);
			Vector3 position = endPoint.position;
			RotateTo(obj, position);
		}
	}

	private void RotateTo(GameObject obj, Vector3 destination)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		Vector3 val = destination - obj.transform.position;
		Quaternion val2 = Quaternion.LookRotation(val);
		obj.transform.localRotation = Quaternion.Lerp(obj.transform.rotation, val2, 1f);
	}
}
