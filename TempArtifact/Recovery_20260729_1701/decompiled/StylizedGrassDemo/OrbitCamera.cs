using UnityEngine;

namespace StylizedGrassDemo;

public class OrbitCamera : MonoBehaviour
{
	[Space]
	public Transform pivot;

	[Space]
	public bool enableMouse = true;

	public float idleRotationSpeed = 0.05f;

	public float lookSmoothSpeed = 5f;

	public float moveSmoothSpeed = 5f;

	public float scrollSmoothSpeed = 5f;

	private Transform cam;

	private float cameraRotSide;

	private float cameraRotUp;

	private float cameraRotSideCur;

	private float cameraRotUpCur;

	private float distance;

	private void Start()
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		cam = ((Component)Camera.main).transform;
		cameraRotSide = ((Component)this).transform.eulerAngles.y;
		cameraRotSideCur = ((Component)this).transform.eulerAngles.y;
		cameraRotUp = ((Component)this).transform.eulerAngles.x;
		cameraRotUpCur = ((Component)this).transform.eulerAngles.x;
		distance = 0f - cam.localPosition.z;
	}

	private void LateUpdate()
	{
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		//IL_0150: Unknown result type (might be due to invalid IL or missing references)
		//IL_0155: Unknown result type (might be due to invalid IL or missing references)
		//IL_0162: Unknown result type (might be due to invalid IL or missing references)
		//IL_0184: Unknown result type (might be due to invalid IL or missing references)
		//IL_019a: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ce: Unknown result type (might be due to invalid IL or missing references)
		Cursor.visible = false;
		if (Object.op_Implicit((Object)(object)pivot))
		{
			if (Input.GetMouseButton(0) && enableMouse)
			{
				cameraRotSide += Input.GetAxis("Mouse X") * 5f;
				cameraRotUp -= Input.GetAxis("Mouse Y") * 5f;
			}
			else
			{
				cameraRotSide += idleRotationSpeed;
			}
			cameraRotSideCur = Mathf.LerpAngle(cameraRotSideCur, cameraRotSide, Time.deltaTime * lookSmoothSpeed);
			cameraRotUpCur = Mathf.Lerp(cameraRotUpCur, cameraRotUp, Time.deltaTime * lookSmoothSpeed);
			if (Input.GetMouseButton(1) && enableMouse)
			{
				distance *= 1f - 0.1f * Input.GetAxis("Mouse Y");
			}
			if (enableMouse)
			{
				distance *= 1f - 1f * Input.GetAxis("Mouse ScrollWheel");
			}
			Vector3 position = pivot.position;
			((Component)this).transform.position = Vector3.Lerp(((Component)this).transform.position, position, Time.deltaTime * moveSmoothSpeed);
			((Component)this).transform.rotation = Quaternion.Euler(cameraRotUpCur, cameraRotSideCur, 0f);
			float num = Mathf.Lerp(0f - ((Component)cam).transform.localPosition.z, distance, Time.deltaTime * scrollSmoothSpeed);
			cam.localPosition = -Vector3.forward * num;
		}
	}
}
