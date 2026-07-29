using UnityEngine;

public class MeteorSpawn : MonoBehaviour
{
	public GameObject vfx;

	public Transform startPoint;

	public Transform endPoint;

	public Vector3 startPos;

	private void Start()
	{
		startPos = startPoint.position;
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.M))
		{
			GameObject objVFX = Object.Instantiate(vfx, startPos, Quaternion.identity);
			Vector3 endPos = endPoint.position;
			RotateTo(objVFX, endPos);
		}
	}

	private void RotateTo(GameObject obj, Vector3 destination)
	{
		Vector3 direction = destination - obj.transform.position;
		Quaternion rotation = Quaternion.LookRotation(direction);
		obj.transform.localRotation = Quaternion.Lerp(obj.transform.rotation, rotation, 1f);
	}
}
