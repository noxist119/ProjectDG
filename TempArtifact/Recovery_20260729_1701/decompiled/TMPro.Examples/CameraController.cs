using UnityEngine;

namespace TMPro.Examples;

public class CameraController : MonoBehaviour
{
	public enum CameraModes
	{
		Follow,
		Isometric,
		Free
	}

	private Transform cameraTransform;

	private Transform dummyTarget;

	public Transform CameraTarget;

	public float FollowDistance = 30f;

	public float MaxFollowDistance = 100f;

	public float MinFollowDistance = 2f;

	public float ElevationAngle = 30f;

	public float MaxElevationAngle = 85f;

	public float MinElevationAngle = 0f;

	public float OrbitalAngle = 0f;

	public CameraModes CameraMode = CameraModes.Follow;

	public bool MovementSmoothing = true;

	public bool RotationSmoothing = false;

	private bool previousSmoothing;

	public float MovementSmoothingValue = 25f;

	public float RotationSmoothingValue = 5f;

	public float MoveSensitivity = 2f;

	private Vector3 currentVelocity = Vector3.zero;

	private Vector3 desiredPosition;

	private float mouseX;

	private float mouseY;

	private Vector3 moveVector;

	private float mouseWheel;

	private const string event_SmoothingValue = "Slider - Smoothing Value";

	private const string event_FollowDistance = "Slider - Camera Zoom";

	private void Awake()
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Invalid comparison between Unknown and I4
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Invalid comparison between Unknown and I4
		if (QualitySettings.vSyncCount > 0)
		{
			Application.targetFrameRate = 60;
		}
		else
		{
			Application.targetFrameRate = -1;
		}
		if ((int)Application.platform == 8 || (int)Application.platform == 11)
		{
			Input.simulateMouseWithTouches = false;
		}
		cameraTransform = ((Component)this).transform;
		previousSmoothing = MovementSmoothing;
	}

	private void Start()
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)CameraTarget == (Object)null)
		{
			dummyTarget = new GameObject("Camera Target").transform;
			CameraTarget = dummyTarget;
		}
	}

	private void LateUpdate()
	{
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
		//IL_0152: Unknown result type (might be due to invalid IL or missing references)
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0162: Unknown result type (might be due to invalid IL or missing references)
		//IL_0167: Unknown result type (might be due to invalid IL or missing references)
		//IL_0178: Unknown result type (might be due to invalid IL or missing references)
		GetPlayerInput();
		if ((Object)(object)CameraTarget != (Object)null)
		{
			if (CameraMode == CameraModes.Isometric)
			{
				desiredPosition = CameraTarget.position + Quaternion.Euler(ElevationAngle, OrbitalAngle, 0f) * new Vector3(0f, 0f, 0f - FollowDistance);
			}
			else if (CameraMode == CameraModes.Follow)
			{
				desiredPosition = CameraTarget.position + CameraTarget.TransformDirection(Quaternion.Euler(ElevationAngle, OrbitalAngle, 0f) * new Vector3(0f, 0f, 0f - FollowDistance));
			}
			if (MovementSmoothing)
			{
				cameraTransform.position = Vector3.SmoothDamp(cameraTransform.position, desiredPosition, ref currentVelocity, MovementSmoothingValue * Time.fixedDeltaTime);
			}
			else
			{
				cameraTransform.position = desiredPosition;
			}
			if (RotationSmoothing)
			{
				cameraTransform.rotation = Quaternion.Lerp(cameraTransform.rotation, Quaternion.LookRotation(CameraTarget.position - cameraTransform.position), RotationSmoothingValue * Time.deltaTime);
			}
			else
			{
				cameraTransform.LookAt(CameraTarget);
			}
		}
	}

	private void GetPlayerInput()
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_04bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_04c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_04c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_04c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_04cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_04de: Unknown result type (might be due to invalid IL or missing references)
		//IL_04e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_04e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_04fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_050b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0512: Unknown result type (might be due to invalid IL or missing references)
		//IL_0517: Unknown result type (might be due to invalid IL or missing references)
		//IL_051c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ce: Invalid comparison between Unknown and I4
		//IL_01de: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0388: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_024c: Unknown result type (might be due to invalid IL or missing references)
		//IL_021b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0485: Unknown result type (might be due to invalid IL or missing references)
		//IL_048a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0496: Unknown result type (might be due to invalid IL or missing references)
		//IL_049b: Unknown result type (might be due to invalid IL or missing references)
		//IL_040b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0422: Unknown result type (might be due to invalid IL or missing references)
		//IL_025a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0279: Unknown result type (might be due to invalid IL or missing references)
		moveVector = Vector3.zero;
		mouseWheel = Input.GetAxis("Mouse ScrollWheel");
		float num = Input.touchCount;
		if (Input.GetKey((KeyCode)304) || Input.GetKey((KeyCode)303) || num > 0f)
		{
			mouseWheel *= 10f;
			if (Input.GetKeyDown((KeyCode)105))
			{
				CameraMode = CameraModes.Isometric;
			}
			if (Input.GetKeyDown((KeyCode)102))
			{
				CameraMode = CameraModes.Follow;
			}
			if (Input.GetKeyDown((KeyCode)115))
			{
				MovementSmoothing = !MovementSmoothing;
			}
			if (Input.GetMouseButton(1))
			{
				mouseY = Input.GetAxis("Mouse Y");
				mouseX = Input.GetAxis("Mouse X");
				if (mouseY > 0.01f || mouseY < -0.01f)
				{
					ElevationAngle -= mouseY * MoveSensitivity;
					ElevationAngle = Mathf.Clamp(ElevationAngle, MinElevationAngle, MaxElevationAngle);
				}
				if (mouseX > 0.01f || mouseX < -0.01f)
				{
					OrbitalAngle += mouseX * MoveSensitivity;
					if (OrbitalAngle > 360f)
					{
						OrbitalAngle -= 360f;
					}
					if (OrbitalAngle < 0f)
					{
						OrbitalAngle += 360f;
					}
				}
			}
			if (num == 1f)
			{
				Touch touch = Input.GetTouch(0);
				if ((int)((Touch)(ref touch)).phase == 1)
				{
					touch = Input.GetTouch(0);
					Vector2 deltaPosition = ((Touch)(ref touch)).deltaPosition;
					if (deltaPosition.y > 0.01f || deltaPosition.y < -0.01f)
					{
						ElevationAngle -= deltaPosition.y * 0.1f;
						ElevationAngle = Mathf.Clamp(ElevationAngle, MinElevationAngle, MaxElevationAngle);
					}
					if (deltaPosition.x > 0.01f || deltaPosition.x < -0.01f)
					{
						OrbitalAngle += deltaPosition.x * 0.1f;
						if (OrbitalAngle > 360f)
						{
							OrbitalAngle -= 360f;
						}
						if (OrbitalAngle < 0f)
						{
							OrbitalAngle += 360f;
						}
					}
				}
			}
			if (Input.GetMouseButton(0))
			{
				Ray val = Camera.main.ScreenPointToRay(Input.mousePosition);
				RaycastHit val2 = default(RaycastHit);
				if (Physics.Raycast(val, ref val2, 300f, 23552))
				{
					if ((Object)(object)((RaycastHit)(ref val2)).transform == (Object)(object)CameraTarget)
					{
						OrbitalAngle = 0f;
					}
					else
					{
						CameraTarget = ((RaycastHit)(ref val2)).transform;
						OrbitalAngle = 0f;
						MovementSmoothing = previousSmoothing;
					}
				}
			}
			if (Input.GetMouseButton(2))
			{
				if ((Object)(object)dummyTarget == (Object)null)
				{
					dummyTarget = new GameObject("Camera Target").transform;
					dummyTarget.position = CameraTarget.position;
					dummyTarget.rotation = CameraTarget.rotation;
					CameraTarget = dummyTarget;
					previousSmoothing = MovementSmoothing;
					MovementSmoothing = false;
				}
				else if ((Object)(object)dummyTarget != (Object)(object)CameraTarget)
				{
					dummyTarget.position = CameraTarget.position;
					dummyTarget.rotation = CameraTarget.rotation;
					CameraTarget = dummyTarget;
					previousSmoothing = MovementSmoothing;
					MovementSmoothing = false;
				}
				mouseY = Input.GetAxis("Mouse Y");
				mouseX = Input.GetAxis("Mouse X");
				moveVector = cameraTransform.TransformDirection(mouseX, mouseY, 0f);
				dummyTarget.Translate(-moveVector, (Space)0);
			}
		}
		if (num == 2f)
		{
			Touch touch2 = Input.GetTouch(0);
			Touch touch3 = Input.GetTouch(1);
			Vector2 val3 = ((Touch)(ref touch2)).position - ((Touch)(ref touch2)).deltaPosition;
			Vector2 val4 = ((Touch)(ref touch3)).position - ((Touch)(ref touch3)).deltaPosition;
			Vector2 val5 = val3 - val4;
			float magnitude = ((Vector2)(ref val5)).magnitude;
			val5 = ((Touch)(ref touch2)).position - ((Touch)(ref touch3)).position;
			float magnitude2 = ((Vector2)(ref val5)).magnitude;
			float num2 = magnitude - magnitude2;
			if (num2 > 0.01f || num2 < -0.01f)
			{
				FollowDistance += num2 * 0.25f;
				FollowDistance = Mathf.Clamp(FollowDistance, MinFollowDistance, MaxFollowDistance);
			}
		}
		if (mouseWheel < -0.01f || mouseWheel > 0.01f)
		{
			FollowDistance -= mouseWheel * 5f;
			FollowDistance = Mathf.Clamp(FollowDistance, MinFollowDistance, MaxFollowDistance);
		}
	}
}
