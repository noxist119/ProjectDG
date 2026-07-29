using System.Collections;
using UnityEngine;

namespace TMPro.Examples;

public class SkewTextExample : MonoBehaviour
{
	private TMP_Text m_TextComponent;

	public AnimationCurve VertexCurve = new AnimationCurve((Keyframe[])(object)new Keyframe[5]
	{
		new Keyframe(0f, 0f),
		new Keyframe(0.25f, 2f),
		new Keyframe(0.5f, 0f),
		new Keyframe(0.75f, 2f),
		new Keyframe(1f, 0f)
	});

	public float CurveScale = 1f;

	public float ShearAmount = 1f;

	private void Awake()
	{
		m_TextComponent = ((Component)this).gameObject.GetComponent<TMP_Text>();
	}

	private void Start()
	{
		((MonoBehaviour)this).StartCoroutine(WarpText());
	}

	private AnimationCurve CopyAnimationCurve(AnimationCurve curve)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Expected O, but got Unknown
		AnimationCurve val = new AnimationCurve();
		val.keys = curve.keys;
		return val;
	}

	private IEnumerator WarpText()
	{
		VertexCurve.preWrapMode = (WrapMode)1;
		VertexCurve.postWrapMode = (WrapMode)1;
		m_TextComponent.havePropertiesChanged = true;
		CurveScale *= 10f;
		float old_CurveScale = CurveScale;
		float old_ShearValue = ShearAmount;
		AnimationCurve old_curve = CopyAnimationCurve(VertexCurve);
		while (true)
		{
			if (!m_TextComponent.havePropertiesChanged && old_CurveScale == CurveScale && ((Keyframe)(ref old_curve.keys[1])).value == ((Keyframe)(ref VertexCurve.keys[1])).value && old_ShearValue == ShearAmount)
			{
				yield return null;
				continue;
			}
			old_CurveScale = CurveScale;
			old_curve = CopyAnimationCurve(VertexCurve);
			old_ShearValue = ShearAmount;
			m_TextComponent.ForceMeshUpdate(false, false);
			TMP_TextInfo textInfo = m_TextComponent.textInfo;
			int characterCount = textInfo.characterCount;
			if (characterCount == 0)
			{
				continue;
			}
			Bounds bounds = m_TextComponent.bounds;
			float boundsMinX = ((Bounds)(ref bounds)).min.x;
			bounds = m_TextComponent.bounds;
			float boundsMaxX = ((Bounds)(ref bounds)).max.x;
			for (int i = 0; i < characterCount; i++)
			{
				if (textInfo.characterInfo[i].isVisible)
				{
					int vertexIndex = textInfo.characterInfo[i].vertexIndex;
					int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;
					Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;
					Vector3 offsetToMidBaseline = Vector2.op_Implicit(new Vector2((vertices[vertexIndex].x + vertices[vertexIndex + 2].x) / 2f, textInfo.characterInfo[i].baseLine));
					ref Vector3 reference = ref vertices[vertexIndex];
					reference += -offsetToMidBaseline;
					ref Vector3 reference2 = ref vertices[vertexIndex + 1];
					reference2 += -offsetToMidBaseline;
					ref Vector3 reference3 = ref vertices[vertexIndex + 2];
					reference3 += -offsetToMidBaseline;
					ref Vector3 reference4 = ref vertices[vertexIndex + 3];
					reference4 += -offsetToMidBaseline;
					float shear_value = ShearAmount * 0.01f;
					Vector3 topShear = new Vector3(shear_value * (textInfo.characterInfo[i].topRight.y - textInfo.characterInfo[i].baseLine), 0f, 0f);
					Vector3 bottomShear = new Vector3(shear_value * (textInfo.characterInfo[i].baseLine - textInfo.characterInfo[i].bottomRight.y), 0f, 0f);
					ref Vector3 reference5 = ref vertices[vertexIndex];
					reference5 += -bottomShear;
					ref Vector3 reference6 = ref vertices[vertexIndex + 1];
					reference6 += topShear;
					ref Vector3 reference7 = ref vertices[vertexIndex + 2];
					reference7 += topShear;
					ref Vector3 reference8 = ref vertices[vertexIndex + 3];
					reference8 += -bottomShear;
					float x0 = (offsetToMidBaseline.x - boundsMinX) / (boundsMaxX - boundsMinX);
					float x1 = x0 + 0.0001f;
					float y0 = VertexCurve.Evaluate(x0) * CurveScale;
					float y1 = VertexCurve.Evaluate(x1) * CurveScale;
					Vector3 horizontal = new Vector3(1f, 0f, 0f);
					Vector3 tangent = new Vector3(x1 * (boundsMaxX - boundsMinX) + boundsMinX, y1) - new Vector3(offsetToMidBaseline.x, y0);
					float dot = Mathf.Acos(Vector3.Dot(horizontal, ((Vector3)(ref tangent)).normalized)) * 57.29578f;
					float angle = ((Vector3.Cross(horizontal, tangent).z > 0f) ? dot : (360f - dot));
					Matrix4x4 matrix = Matrix4x4.TRS(new Vector3(0f, y0, 0f), Quaternion.Euler(0f, 0f, angle), Vector3.one);
					vertices[vertexIndex] = ((Matrix4x4)(ref matrix)).MultiplyPoint3x4(vertices[vertexIndex]);
					vertices[vertexIndex + 1] = ((Matrix4x4)(ref matrix)).MultiplyPoint3x4(vertices[vertexIndex + 1]);
					vertices[vertexIndex + 2] = ((Matrix4x4)(ref matrix)).MultiplyPoint3x4(vertices[vertexIndex + 2]);
					vertices[vertexIndex + 3] = ((Matrix4x4)(ref matrix)).MultiplyPoint3x4(vertices[vertexIndex + 3]);
					ref Vector3 reference9 = ref vertices[vertexIndex];
					reference9 += offsetToMidBaseline;
					ref Vector3 reference10 = ref vertices[vertexIndex + 1];
					reference10 += offsetToMidBaseline;
					ref Vector3 reference11 = ref vertices[vertexIndex + 2];
					reference11 += offsetToMidBaseline;
					ref Vector3 reference12 = ref vertices[vertexIndex + 3];
					reference12 += offsetToMidBaseline;
				}
			}
			m_TextComponent.UpdateVertexData();
			yield return null;
		}
	}
}
