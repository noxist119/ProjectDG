using UnityEngine;

namespace StylizedGrassDemo;

public class MoveInCircle : MonoBehaviour
{
	public float radius = 1f;

	public float speed = 1f;

	public Vector3 offset;

	private void Update()
	{
		Move();
	}

	private void Move()
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		float num = Mathf.Sin(Time.realtimeSinceStartup * speed) * radius + offset.x;
		float num2 = ((Component)this).transform.position.y + offset.y;
		float num3 = Mathf.Cos(Time.realtimeSinceStartup * speed) * radius + offset.z;
		((Component)this).transform.localPosition = new Vector3(num, num2, num3);
	}
}
