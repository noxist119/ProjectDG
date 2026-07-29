using System;
using System.Collections;
using UnityEngine;

namespace TMPro.Examples;

public class VertexJitter : MonoBehaviour
{
	private struct VertexAnim
	{
		public float angleRange;

		public float angle;

		public float speed;
	}

	public float AngleMultiplier = 1f;

	public float SpeedMultiplier = 1f;

	public float CurveScale = 1f;

	private TMP_Text m_TextComponent;

	private bool hasTextChanged;

	private void Awake()
	{
		m_TextComponent = ((Component)this).GetComponent<TMP_Text>();
	}

	private void OnEnable()
	{
		TMPro_EventManager.TEXT_CHANGED_EVENT.Add((Action<Object>)ON_TEXT_CHANGED);
	}

	private void OnDisable()
	{
		TMPro_EventManager.TEXT_CHANGED_EVENT.Remove((Action<Object>)ON_TEXT_CHANGED);
	}

	private void Start()
	{
		((MonoBehaviour)this).StartCoroutine(AnimateVertexColors());
	}

	private void ON_TEXT_CHANGED(Object obj)
	{
		if (obj == (Object)(object)m_TextComponent)
		{
			hasTextChanged = true;
		}
	}

	private IEnumerator AnimateVertexColors()
	{
		m_TextComponent.ForceMeshUpdate(false, false);
		TMP_TextInfo textInfo = m_TextComponent.textInfo;
		int loopCount = 0;
		hasTextChanged = true;
		VertexAnim[] vertexAnim = new VertexAnim[1024];
		for (int i = 0; i < 1024; i++)
		{
			vertexAnim[i].angleRange = Random.Range(10f, 25f);
			vertexAnim[i].speed = Random.Range(1f, 3f);
		}
		TMP_MeshInfo[] cachedMeshInfo = textInfo.CopyMeshInfoVertexData();
		while (true)
		{
			if (hasTextChanged)
			{
				cachedMeshInfo = textInfo.CopyMeshInfoVertexData();
				hasTextChanged = false;
			}
			int characterCount = textInfo.characterCount;
			if (characterCount == 0)
			{
				yield return (object)new WaitForSeconds(0.25f);
				continue;
			}
			for (int j = 0; j < characterCount; j++)
			{
				TMP_CharacterInfo charInfo = textInfo.characterInfo[j];
				if (charInfo.isVisible)
				{
					VertexAnim vertAnim = vertexAnim[j];
					int materialIndex = textInfo.characterInfo[j].materialReferenceIndex;
					int vertexIndex = textInfo.characterInfo[j].vertexIndex;
					Vector3[] sourceVertices = cachedMeshInfo[materialIndex].vertices;
					Vector2 charMidBasline = Vector2.op_Implicit((sourceVertices[vertexIndex] + sourceVertices[vertexIndex + 2]) / 2f);
					Vector3 offset = Vector2.op_Implicit(charMidBasline);
					Vector3[] destinationVertices = textInfo.meshInfo[materialIndex].vertices;
					destinationVertices[vertexIndex] = sourceVertices[vertexIndex] - offset;
					destinationVertices[vertexIndex + 1] = sourceVertices[vertexIndex + 1] - offset;
					destinationVertices[vertexIndex + 2] = sourceVertices[vertexIndex + 2] - offset;
					destinationVertices[vertexIndex + 3] = sourceVertices[vertexIndex + 3] - offset;
					vertAnim.angle = Mathf.SmoothStep(0f - vertAnim.angleRange, vertAnim.angleRange, Mathf.PingPong((float)loopCount / 25f * vertAnim.speed, 1f));
					Vector3 jitterOffset = new Vector3(Random.Range(-0.25f, 0.25f), Random.Range(-0.25f, 0.25f), 0f);
					Matrix4x4 matrix = Matrix4x4.TRS(jitterOffset * CurveScale, Quaternion.Euler(0f, 0f, Random.Range(-5f, 5f) * AngleMultiplier), Vector3.one);
					destinationVertices[vertexIndex] = ((Matrix4x4)(ref matrix)).MultiplyPoint3x4(destinationVertices[vertexIndex]);
					destinationVertices[vertexIndex + 1] = ((Matrix4x4)(ref matrix)).MultiplyPoint3x4(destinationVertices[vertexIndex + 1]);
					destinationVertices[vertexIndex + 2] = ((Matrix4x4)(ref matrix)).MultiplyPoint3x4(destinationVertices[vertexIndex + 2]);
					destinationVertices[vertexIndex + 3] = ((Matrix4x4)(ref matrix)).MultiplyPoint3x4(destinationVertices[vertexIndex + 3]);
					ref Vector3 reference = ref destinationVertices[vertexIndex];
					reference += offset;
					ref Vector3 reference2 = ref destinationVertices[vertexIndex + 1];
					reference2 += offset;
					ref Vector3 reference3 = ref destinationVertices[vertexIndex + 2];
					reference3 += offset;
					ref Vector3 reference4 = ref destinationVertices[vertexIndex + 3];
					reference4 += offset;
					vertexAnim[j] = vertAnim;
				}
			}
			for (int k = 0; k < textInfo.meshInfo.Length; k++)
			{
				textInfo.meshInfo[k].mesh.vertices = textInfo.meshInfo[k].vertices;
				m_TextComponent.UpdateGeometry(textInfo.meshInfo[k].mesh, k);
			}
			loopCount++;
			yield return (object)new WaitForSeconds(0.1f);
		}
	}
}
