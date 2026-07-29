using UnityEngine;

namespace DefenseGame;

[RequireComponent(typeof(Camera))]
public class RuntimeBattleCameraFitter : MonoBehaviour
{
	[SerializeField]
	private Vector3 widePosition = new Vector3(0f, 15f, -12.4f);

	[SerializeField]
	private Vector3 portraitPosition = new Vector3(0f, 17.6f, -16.1f);

	[SerializeField]
	private Vector3 wideEuler = new Vector3(53f, 0f, 0f);

	[SerializeField]
	private Vector3 portraitEuler = new Vector3(58f, 0f, 0f);

	[SerializeField]
	private float wideFieldOfView = 50f;

	[SerializeField]
	private float portraitFieldOfView = 60f;

	[SerializeField]
	private float wideAspect = 0.75f;

	[SerializeField]
	private float portraitAspect = 0.46f;

	[SerializeField]
	private Rect wideViewport = new Rect(0f, 0.12f, 1f, 0.78f);

	[SerializeField]
	private Rect portraitViewport = new Rect(0f, 0.17f, 1f, 0.72f);

	private Camera targetCamera;

	private Vector2Int lastScreenSize;

	public void Configure(Vector3 basePosition, Vector3 mobilePosition, Vector3 eulerRotation, float baseFov, float mobileFov)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		widePosition = basePosition;
		portraitPosition = mobilePosition;
		wideEuler = eulerRotation;
		portraitEuler = eulerRotation + new Vector3(5f, 0f, 0f);
		wideFieldOfView = baseFov;
		portraitFieldOfView = mobileFov;
		ApplyLayout(force: true);
	}

	private void Awake()
	{
		targetCamera = ((Component)this).GetComponent<Camera>();
	}

	private void OnEnable()
	{
		ApplyLayout(force: true);
	}

	private void Update()
	{
		ApplyLayout(force: false);
	}

	private void ApplyLayout(bool force)
	{
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)targetCamera == (Object)null)
		{
			targetCamera = ((Component)this).GetComponent<Camera>();
		}
		Vector2Int val = default(Vector2Int);
		((Vector2Int)(ref val))._002Ector(Screen.width, Screen.height);
		if (force || !(val == lastScreenSize))
		{
			lastScreenSize = val;
			float num = ((((Vector2Int)(ref val)).y <= 0) ? wideAspect : ((float)((Vector2Int)(ref val)).x / (float)((Vector2Int)(ref val)).y));
			float num2 = Mathf.InverseLerp(wideAspect, portraitAspect, num);
			bool flag = num >= 0.95f;
			((Component)this).transform.position = Vector3.Lerp(widePosition, portraitPosition, num2);
			((Component)this).transform.rotation = Quaternion.Euler(Vector3.Lerp(wideEuler, portraitEuler, num2));
			targetCamera.fieldOfView = Mathf.Lerp(wideFieldOfView, portraitFieldOfView, num2);
			targetCamera.rect = (Rect)(flag ? new Rect(0f, 0f, 1f, 1f) : LerpRect(wideViewport, portraitViewport, num2));
		}
	}

	private Rect LerpRect(Rect from, Rect to, float t)
	{
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		return new Rect(Mathf.Lerp(((Rect)(ref from)).x, ((Rect)(ref to)).x, t), Mathf.Lerp(((Rect)(ref from)).y, ((Rect)(ref to)).y, t), Mathf.Lerp(((Rect)(ref from)).width, ((Rect)(ref to)).width, t), Mathf.Lerp(((Rect)(ref from)).height, ((Rect)(ref to)).height, t));
	}
}
