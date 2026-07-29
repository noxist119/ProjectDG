using UnityEngine;

namespace TMPro.Examples;

public class TMP_UiFrameRateCounter : MonoBehaviour
{
	public enum FpsCounterAnchorPositions
	{
		TopLeft,
		BottomLeft,
		TopRight,
		BottomRight
	}

	public float UpdateInterval = 5f;

	private float m_LastInterval = 0f;

	private int m_Frames = 0;

	public FpsCounterAnchorPositions AnchorPosition = FpsCounterAnchorPositions.TopRight;

	private string htmlColorTag;

	private const string fpsLabel = "{0:2}</color> <#8080ff>FPS \n<#FF8000>{1:2} <#8080ff>MS";

	private TextMeshProUGUI m_TextMeshPro;

	private RectTransform m_frameCounter_transform;

	private FpsCounterAnchorPositions last_AnchorPosition;

	private void Awake()
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Expected O, but got Unknown
		if (((Behaviour)this).enabled)
		{
			Application.targetFrameRate = 1000;
			GameObject val = new GameObject("Frame Counter");
			m_frameCounter_transform = val.AddComponent<RectTransform>();
			((Transform)m_frameCounter_transform).SetParent(((Component)this).transform, false);
			m_TextMeshPro = val.AddComponent<TextMeshProUGUI>();
			((TMP_Text)m_TextMeshPro).font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
			((TMP_Text)m_TextMeshPro).fontSharedMaterial = Resources.Load<Material>("Fonts & Materials/LiberationSans SDF - Overlay");
			((TMP_Text)m_TextMeshPro).enableWordWrapping = false;
			((TMP_Text)m_TextMeshPro).fontSize = 36f;
			((TMP_Text)m_TextMeshPro).isOverlay = true;
			Set_FrameCounter_Position(AnchorPosition);
			last_AnchorPosition = AnchorPosition;
		}
	}

	private void Start()
	{
		m_LastInterval = Time.realtimeSinceStartup;
		m_Frames = 0;
	}

	private void Update()
	{
		if (AnchorPosition != last_AnchorPosition)
		{
			Set_FrameCounter_Position(AnchorPosition);
		}
		last_AnchorPosition = AnchorPosition;
		m_Frames++;
		float realtimeSinceStartup = Time.realtimeSinceStartup;
		if (realtimeSinceStartup > m_LastInterval + UpdateInterval)
		{
			float num = (float)m_Frames / (realtimeSinceStartup - m_LastInterval);
			float num2 = 1000f / Mathf.Max(num, 1E-05f);
			if (num < 30f)
			{
				htmlColorTag = "<color=yellow>";
			}
			else if (num < 10f)
			{
				htmlColorTag = "<color=red>";
			}
			else
			{
				htmlColorTag = "<color=green>";
			}
			((TMP_Text)m_TextMeshPro).SetText(htmlColorTag + "{0:2}</color> <#8080ff>FPS \n<#FF8000>{1:2} <#8080ff>MS", num, num2);
			m_Frames = 0;
			m_LastInterval = realtimeSinceStartup;
		}
	}

	private void Set_FrameCounter_Position(FpsCounterAnchorPositions anchor_position)
	{
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_0160: Unknown result type (might be due to invalid IL or missing references)
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0196: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01df: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0215: Unknown result type (might be due to invalid IL or missing references)
		switch (anchor_position)
		{
		case FpsCounterAnchorPositions.TopLeft:
			((TMP_Text)m_TextMeshPro).alignment = (TextAlignmentOptions)257;
			m_frameCounter_transform.pivot = new Vector2(0f, 1f);
			m_frameCounter_transform.anchorMin = new Vector2(0.01f, 0.99f);
			m_frameCounter_transform.anchorMax = new Vector2(0.01f, 0.99f);
			m_frameCounter_transform.anchoredPosition = new Vector2(0f, 1f);
			break;
		case FpsCounterAnchorPositions.BottomLeft:
			((TMP_Text)m_TextMeshPro).alignment = (TextAlignmentOptions)1025;
			m_frameCounter_transform.pivot = new Vector2(0f, 0f);
			m_frameCounter_transform.anchorMin = new Vector2(0.01f, 0.01f);
			m_frameCounter_transform.anchorMax = new Vector2(0.01f, 0.01f);
			m_frameCounter_transform.anchoredPosition = new Vector2(0f, 0f);
			break;
		case FpsCounterAnchorPositions.TopRight:
			((TMP_Text)m_TextMeshPro).alignment = (TextAlignmentOptions)260;
			m_frameCounter_transform.pivot = new Vector2(1f, 1f);
			m_frameCounter_transform.anchorMin = new Vector2(0.99f, 0.99f);
			m_frameCounter_transform.anchorMax = new Vector2(0.99f, 0.99f);
			m_frameCounter_transform.anchoredPosition = new Vector2(1f, 1f);
			break;
		case FpsCounterAnchorPositions.BottomRight:
			((TMP_Text)m_TextMeshPro).alignment = (TextAlignmentOptions)1028;
			m_frameCounter_transform.pivot = new Vector2(1f, 0f);
			m_frameCounter_transform.anchorMin = new Vector2(0.99f, 0.01f);
			m_frameCounter_transform.anchorMax = new Vector2(0.99f, 0.01f);
			m_frameCounter_transform.anchoredPosition = new Vector2(1f, 0f);
			break;
		}
	}
}
