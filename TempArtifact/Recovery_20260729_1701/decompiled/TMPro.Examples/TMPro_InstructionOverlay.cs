using UnityEngine;

namespace TMPro.Examples;

public class TMPro_InstructionOverlay : MonoBehaviour
{
	public enum FpsCounterAnchorPositions
	{
		TopLeft,
		BottomLeft,
		TopRight,
		BottomRight
	}

	public FpsCounterAnchorPositions AnchorPosition = FpsCounterAnchorPositions.BottomLeft;

	private const string instructions = "Camera Control - <#ffff00>Shift + RMB\n</color>Zoom - <#ffff00>Mouse wheel.";

	private TextMeshPro m_TextMeshPro;

	private TextContainer m_textContainer;

	private Transform m_frameCounter_transform;

	private Camera m_camera;

	private void Awake()
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Expected O, but got Unknown
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		if (((Behaviour)this).enabled)
		{
			m_camera = Camera.main;
			GameObject val = new GameObject("Frame Counter");
			m_frameCounter_transform = val.transform;
			m_frameCounter_transform.parent = ((Component)m_camera).transform;
			m_frameCounter_transform.localRotation = Quaternion.identity;
			m_TextMeshPro = val.AddComponent<TextMeshPro>();
			((TMP_Text)m_TextMeshPro).font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
			((TMP_Text)m_TextMeshPro).fontSharedMaterial = Resources.Load<Material>("Fonts & Materials/LiberationSans SDF - Overlay");
			((TMP_Text)m_TextMeshPro).fontSize = 30f;
			((TMP_Text)m_TextMeshPro).isOverlay = true;
			m_textContainer = val.GetComponent<TextContainer>();
			Set_FrameCounter_Position(AnchorPosition);
			((TMP_Text)m_TextMeshPro).text = "Camera Control - <#ffff00>Shift + RMB\n</color>Zoom - <#ffff00>Mouse wheel.";
		}
	}

	private void Set_FrameCounter_Position(FpsCounterAnchorPositions anchor_position)
	{
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		switch (anchor_position)
		{
		case FpsCounterAnchorPositions.TopLeft:
			m_textContainer.anchorPosition = (TextContainerAnchors)0;
			m_frameCounter_transform.position = m_camera.ViewportToWorldPoint(new Vector3(0f, 1f, 100f));
			break;
		case FpsCounterAnchorPositions.BottomLeft:
			m_textContainer.anchorPosition = (TextContainerAnchors)6;
			m_frameCounter_transform.position = m_camera.ViewportToWorldPoint(new Vector3(0f, 0f, 100f));
			break;
		case FpsCounterAnchorPositions.TopRight:
			m_textContainer.anchorPosition = (TextContainerAnchors)2;
			m_frameCounter_transform.position = m_camera.ViewportToWorldPoint(new Vector3(1f, 1f, 100f));
			break;
		case FpsCounterAnchorPositions.BottomRight:
			m_textContainer.anchorPosition = (TextContainerAnchors)8;
			m_frameCounter_transform.position = m_camera.ViewportToWorldPoint(new Vector3(1f, 0f, 100f));
			break;
		}
	}
}
