using UnityEngine;

namespace TMPro.Examples;

public class ObjectSpin : MonoBehaviour
{
	public enum MotionType
	{
		Rotation,
		BackAndForth,
		Translation
	}

	public float SpinSpeed = 5f;

	public int RotationRange = 15;

	private Transform m_transform;

	private float m_time;

	private Vector3 m_prevPOS;

	private Vector3 m_initial_Rotation;

	private Vector3 m_initial_Position;

	private Color32 m_lightColor;

	private int frames = 0;

	public MotionType Motion;

	private void Awake()
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		m_transform = ((Component)this).transform;
		Quaternion rotation = m_transform.rotation;
		m_initial_Rotation = ((Quaternion)(ref rotation)).eulerAngles;
		m_initial_Position = m_transform.position;
		Light component = ((Component)this).GetComponent<Light>();
		m_lightColor = Color32.op_Implicit(((Object)(object)component != (Object)null) ? component.color : Color.black);
	}

	private void Update()
	{
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		if (Motion == MotionType.Rotation)
		{
			m_transform.Rotate(0f, SpinSpeed * Time.deltaTime, 0f);
			return;
		}
		if (Motion == MotionType.BackAndForth)
		{
			m_time += SpinSpeed * Time.deltaTime;
			m_transform.rotation = Quaternion.Euler(m_initial_Rotation.x, Mathf.Sin(m_time) * (float)RotationRange + m_initial_Rotation.y, m_initial_Rotation.z);
			return;
		}
		m_time += SpinSpeed * Time.deltaTime;
		float num = 15f * Mathf.Cos(m_time * 0.95f);
		float num2 = 10f;
		float num3 = 0f;
		m_transform.position = m_initial_Position + new Vector3(num, num3, num2);
		m_prevPOS = m_transform.position;
		frames++;
	}
}
