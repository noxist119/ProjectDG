using UnityEngine;

namespace TMPro.Examples
{
	public class TMP_FrameRateCounter : MonoBehaviour
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

		private TextMeshPro m_TextMeshPro;

		private Transform m_frameCounter_transform;

		private Camera m_camera;

		private FpsCounterAnchorPositions last_AnchorPosition;

		private void Awake()
		{
			if (base.enabled)
			{
				m_camera = Camera.main;
				Application.targetFrameRate = 9999;
				GameObject frameCounter = new GameObject("Frame Counter");
				m_TextMeshPro = frameCounter.AddComponent<TextMeshPro>();
				((TMP_Text)m_TextMeshPro).font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
				((TMP_Text)m_TextMeshPro).fontSharedMaterial = Resources.Load<Material>("Fonts & Materials/LiberationSans SDF - Overlay");
				m_frameCounter_transform = frameCounter.transform;
				m_frameCounter_transform.SetParent(m_camera.transform);
				m_frameCounter_transform.localRotation = Quaternion.identity;
				((TMP_Text)m_TextMeshPro).enableWordWrapping = false;
				((TMP_Text)m_TextMeshPro).fontSize = 24f;
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
			float timeNow = Time.realtimeSinceStartup;
			if (timeNow > m_LastInterval + UpdateInterval)
			{
				float fps = (float)m_Frames / (timeNow - m_LastInterval);
				float ms = 1000f / Mathf.Max(fps, 1E-05f);
				if (fps < 30f)
				{
					htmlColorTag = "<color=yellow>";
				}
				else if (fps < 10f)
				{
					htmlColorTag = "<color=red>";
				}
				else
				{
					htmlColorTag = "<color=green>";
				}
				((TMP_Text)m_TextMeshPro).SetText(htmlColorTag + "{0:2}</color> <#8080ff>FPS \n<#FF8000>{1:2} <#8080ff>MS", fps, ms);
				m_Frames = 0;
				m_LastInterval = timeNow;
			}
		}

		private void Set_FrameCounter_Position(FpsCounterAnchorPositions anchor_position)
		{
			((TMP_Text)m_TextMeshPro).margin = new Vector4(1f, 1f, 1f, 1f);
			switch (anchor_position)
			{
			case FpsCounterAnchorPositions.TopLeft:
				((TMP_Text)m_TextMeshPro).alignment = (TextAlignmentOptions)257;
				((TMP_Text)m_TextMeshPro).rectTransform.pivot = new Vector2(0f, 1f);
				m_frameCounter_transform.position = m_camera.ViewportToWorldPoint(new Vector3(0f, 1f, 100f));
				break;
			case FpsCounterAnchorPositions.BottomLeft:
				((TMP_Text)m_TextMeshPro).alignment = (TextAlignmentOptions)1025;
				((TMP_Text)m_TextMeshPro).rectTransform.pivot = new Vector2(0f, 0f);
				m_frameCounter_transform.position = m_camera.ViewportToWorldPoint(new Vector3(0f, 0f, 100f));
				break;
			case FpsCounterAnchorPositions.TopRight:
				((TMP_Text)m_TextMeshPro).alignment = (TextAlignmentOptions)260;
				((TMP_Text)m_TextMeshPro).rectTransform.pivot = new Vector2(1f, 1f);
				m_frameCounter_transform.position = m_camera.ViewportToWorldPoint(new Vector3(1f, 1f, 100f));
				break;
			case FpsCounterAnchorPositions.BottomRight:
				((TMP_Text)m_TextMeshPro).alignment = (TextAlignmentOptions)1028;
				((TMP_Text)m_TextMeshPro).rectTransform.pivot = new Vector2(1f, 0f);
				m_frameCounter_transform.position = m_camera.ViewportToWorldPoint(new Vector3(1f, 0f, 100f));
				break;
			}
		}
	}
}
