using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace TMPro.Examples;

public class TextMeshProFloatingText : MonoBehaviour
{
	public Font TheFont;

	private GameObject m_floatingText;

	private TextMeshPro m_textMeshPro;

	private TextMesh m_textMesh;

	private Transform m_transform;

	private Transform m_floatingText_Transform;

	private Transform m_cameraTransform;

	private Vector3 lastPOS = Vector3.zero;

	private Quaternion lastRotation = Quaternion.identity;

	public int SpawnType;

	public bool IsTextObjectScaleStatic;

	private static WaitForEndOfFrame k_WaitForEndOfFrame = new WaitForEndOfFrame();

	private static WaitForSeconds[] k_WaitForSecondsRandom = (WaitForSeconds[])(object)new WaitForSeconds[20]
	{
		new WaitForSeconds(0.05f),
		new WaitForSeconds(0.1f),
		new WaitForSeconds(0.15f),
		new WaitForSeconds(0.2f),
		new WaitForSeconds(0.25f),
		new WaitForSeconds(0.3f),
		new WaitForSeconds(0.35f),
		new WaitForSeconds(0.4f),
		new WaitForSeconds(0.45f),
		new WaitForSeconds(0.5f),
		new WaitForSeconds(0.55f),
		new WaitForSeconds(0.6f),
		new WaitForSeconds(0.65f),
		new WaitForSeconds(0.7f),
		new WaitForSeconds(0.75f),
		new WaitForSeconds(0.8f),
		new WaitForSeconds(0.85f),
		new WaitForSeconds(0.9f),
		new WaitForSeconds(0.95f),
		new WaitForSeconds(1f)
	};

	private void Awake()
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Expected O, but got Unknown
		m_transform = ((Component)this).transform;
		m_floatingText = new GameObject(((Object)this).name + " floating text");
		m_cameraTransform = ((Component)Camera.main).transform;
	}

	private void Start()
	{
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0156: Unknown result type (might be due to invalid IL or missing references)
		//IL_016a: Unknown result type (might be due to invalid IL or missing references)
		//IL_016f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f6: Unknown result type (might be due to invalid IL or missing references)
		if (SpawnType == 0)
		{
			m_textMeshPro = m_floatingText.AddComponent<TextMeshPro>();
			((TMP_Text)m_textMeshPro).rectTransform.sizeDelta = new Vector2(3f, 3f);
			m_floatingText_Transform = m_floatingText.transform;
			m_floatingText_Transform.position = m_transform.position + new Vector3(0f, 15f, 0f);
			((TMP_Text)m_textMeshPro).alignment = (TextAlignmentOptions)514;
			((Graphic)m_textMeshPro).color = Color32.op_Implicit(new Color32((byte)Random.Range(0, 255), (byte)Random.Range(0, 255), (byte)Random.Range(0, 255), byte.MaxValue));
			((TMP_Text)m_textMeshPro).fontSize = 24f;
			((TMP_Text)m_textMeshPro).enableKerning = false;
			((TMP_Text)m_textMeshPro).text = string.Empty;
			((TMP_Text)m_textMeshPro).isTextObjectScaleStatic = IsTextObjectScaleStatic;
			((MonoBehaviour)this).StartCoroutine(DisplayTextMeshProFloatingText());
		}
		else if (SpawnType == 1)
		{
			m_floatingText_Transform = m_floatingText.transform;
			m_floatingText_Transform.position = m_transform.position + new Vector3(0f, 15f, 0f);
			m_textMesh = m_floatingText.AddComponent<TextMesh>();
			m_textMesh.font = Resources.Load<Font>("Fonts/ARIAL");
			((Component)m_textMesh).GetComponent<Renderer>().sharedMaterial = m_textMesh.font.material;
			m_textMesh.color = Color32.op_Implicit(new Color32((byte)Random.Range(0, 255), (byte)Random.Range(0, 255), (byte)Random.Range(0, 255), byte.MaxValue));
			m_textMesh.anchor = (TextAnchor)7;
			m_textMesh.fontSize = 24;
			((MonoBehaviour)this).StartCoroutine(DisplayTextMeshFloatingText());
		}
		else if (SpawnType != 2)
		{
		}
	}

	public IEnumerator DisplayTextMeshProFloatingText()
	{
		float CountDuration = 2f;
		float starting_Count = Random.Range(5f, 20f);
		float current_Count = starting_Count;
		Vector3 start_pos = m_floatingText_Transform.position;
		Color32 start_color = Color32.op_Implicit(((Graphic)m_textMeshPro).color);
		float alpha = 255f;
		float fadeDuration = 3f / starting_Count * CountDuration;
		while (current_Count > 0f)
		{
			current_Count -= Time.deltaTime / CountDuration * starting_Count;
			if (current_Count <= 3f)
			{
				alpha = Mathf.Clamp(alpha - Time.deltaTime / fadeDuration * 255f, 0f, 255f);
			}
			((TMP_Text)m_textMeshPro).text = ((int)current_Count).ToString();
			((Graphic)m_textMeshPro).color = Color32.op_Implicit(new Color32(start_color.r, start_color.g, start_color.b, (byte)alpha));
			Transform floatingText_Transform = m_floatingText_Transform;
			floatingText_Transform.position += new Vector3(0f, starting_Count * Time.deltaTime, 0f);
			if (!TMPro_ExtensionMethods.Compare(lastPOS, m_cameraTransform.position, 1000) || !TMPro_ExtensionMethods.Compare(lastRotation, m_cameraTransform.rotation, 1000))
			{
				lastPOS = m_cameraTransform.position;
				lastRotation = m_cameraTransform.rotation;
				m_floatingText_Transform.rotation = lastRotation;
				Vector3 dir = m_transform.position - lastPOS;
				m_transform.forward = new Vector3(dir.x, 0f, dir.z);
			}
			yield return k_WaitForEndOfFrame;
		}
		yield return k_WaitForSecondsRandom[Random.Range(0, 19)];
		m_floatingText_Transform.position = start_pos;
		((MonoBehaviour)this).StartCoroutine(DisplayTextMeshProFloatingText());
	}

	public IEnumerator DisplayTextMeshFloatingText()
	{
		float CountDuration = 2f;
		float starting_Count = Random.Range(5f, 20f);
		float current_Count = starting_Count;
		Vector3 start_pos = m_floatingText_Transform.position;
		Color32 start_color = Color32.op_Implicit(m_textMesh.color);
		float alpha = 255f;
		float fadeDuration = 3f / starting_Count * CountDuration;
		while (current_Count > 0f)
		{
			current_Count -= Time.deltaTime / CountDuration * starting_Count;
			if (current_Count <= 3f)
			{
				alpha = Mathf.Clamp(alpha - Time.deltaTime / fadeDuration * 255f, 0f, 255f);
			}
			m_textMesh.text = ((int)current_Count).ToString();
			m_textMesh.color = Color32.op_Implicit(new Color32(start_color.r, start_color.g, start_color.b, (byte)alpha));
			Transform floatingText_Transform = m_floatingText_Transform;
			floatingText_Transform.position += new Vector3(0f, starting_Count * Time.deltaTime, 0f);
			if (!TMPro_ExtensionMethods.Compare(lastPOS, m_cameraTransform.position, 1000) || !TMPro_ExtensionMethods.Compare(lastRotation, m_cameraTransform.rotation, 1000))
			{
				lastPOS = m_cameraTransform.position;
				lastRotation = m_cameraTransform.rotation;
				m_floatingText_Transform.rotation = lastRotation;
				Vector3 dir = m_transform.position - lastPOS;
				m_transform.forward = new Vector3(dir.x, 0f, dir.z);
			}
			yield return k_WaitForEndOfFrame;
		}
		yield return k_WaitForSecondsRandom[Random.Range(0, 20)];
		m_floatingText_Transform.position = start_pos;
		((MonoBehaviour)this).StartCoroutine(DisplayTextMeshFloatingText());
	}
}
