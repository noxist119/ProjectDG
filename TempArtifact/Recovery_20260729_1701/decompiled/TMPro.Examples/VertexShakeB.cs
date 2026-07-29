using System;
using System.Collections;
using UnityEngine;

namespace TMPro.Examples;

public class VertexShakeB : MonoBehaviour
{
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
		if (Object.op_Implicit(obj = (Object)(object)m_TextComponent))
		{
			hasTextChanged = true;
		}
	}

	private IEnumerator AnimateVertexColors()
	{
		m_TextComponent.ForceMeshUpdate(false, false);
		TMP_TextInfo textInfo = m_TextComponent.textInfo;
		Vector3[][] copyOfVertices = new Vector3[0][];
		hasTextChanged = true;
		while (true)
		{
			if (hasTextChanged)
			{
				if (copyOfVertices.Length < textInfo.meshInfo.Length)
				{
					copyOfVertices = new Vector3[textInfo.meshInfo.Length][];
				}
				for (int i = 0; i < textInfo.meshInfo.Length; i++)
				{
					int length = textInfo.meshInfo[i].vertices.Length;
					copyOfVertices[i] = (Vector3[])(object)new Vector3[length];
				}
				hasTextChanged = false;
			}
			if (textInfo.characterCount == 0)
			{
				yield return (object)new WaitForSeconds(0.25f);
				continue;
			}
			int lineCount = textInfo.lineCount;
			for (int j = 0; j < lineCount; j++)
			{
				int first = textInfo.lineInfo[j].firstCharacterIndex;
				int last = textInfo.lineInfo[j].lastCharacterIndex;
				Vector3 centerOfLine = (textInfo.characterInfo[first].bottomLeft + textInfo.characterInfo[last].topRight) / 2f;
				Quaternion rotation = Quaternion.Euler(0f, 0f, Random.Range(-0.25f, 0.25f));
				for (int k = first; k <= last; k++)
				{
					if (textInfo.characterInfo[k].isVisible)
					{
						int materialIndex = textInfo.characterInfo[k].materialReferenceIndex;
						int vertexIndex = textInfo.characterInfo[k].vertexIndex;
						Vector3[] sourceVertices = textInfo.meshInfo[materialIndex].vertices;
						Vector3 charCenter = (sourceVertices[vertexIndex] + sourceVertices[vertexIndex + 2]) / 2f;
						copyOfVertices[materialIndex][vertexIndex] = sourceVertices[vertexIndex] - charCenter;
						copyOfVertices[materialIndex][vertexIndex + 1] = sourceVertices[vertexIndex + 1] - charCenter;
						copyOfVertices[materialIndex][vertexIndex + 2] = sourceVertices[vertexIndex + 2] - charCenter;
						copyOfVertices[materialIndex][vertexIndex + 3] = sourceVertices[vertexIndex + 3] - charCenter;
						float randomScale = Random.Range(0.95f, 1.05f);
						Matrix4x4 matrix = Matrix4x4.TRS(Vector3.one, Quaternion.identity, Vector3.one * randomScale);
						copyOfVertices[materialIndex][vertexIndex] = ((Matrix4x4)(ref matrix)).MultiplyPoint3x4(copyOfVertices[materialIndex][vertexIndex]);
						copyOfVertices[materialIndex][vertexIndex + 1] = ((Matrix4x4)(ref matrix)).MultiplyPoint3x4(copyOfVertices[materialIndex][vertexIndex + 1]);
						copyOfVertices[materialIndex][vertexIndex + 2] = ((Matrix4x4)(ref matrix)).MultiplyPoint3x4(copyOfVertices[materialIndex][vertexIndex + 2]);
						copyOfVertices[materialIndex][vertexIndex + 3] = ((Matrix4x4)(ref matrix)).MultiplyPoint3x4(copyOfVertices[materialIndex][vertexIndex + 3]);
						ref Vector3 reference = ref copyOfVertices[materialIndex][vertexIndex];
						reference += charCenter;
						ref Vector3 reference2 = ref copyOfVertices[materialIndex][vertexIndex + 1];
						reference2 += charCenter;
						ref Vector3 reference3 = ref copyOfVertices[materialIndex][vertexIndex + 2];
						reference3 += charCenter;
						ref Vector3 reference4 = ref copyOfVertices[materialIndex][vertexIndex + 3];
						reference4 += charCenter;
						ref Vector3 reference5 = ref copyOfVertices[materialIndex][vertexIndex];
						reference5 -= centerOfLine;
						ref Vector3 reference6 = ref copyOfVertices[materialIndex][vertexIndex + 1];
						reference6 -= centerOfLine;
						ref Vector3 reference7 = ref copyOfVertices[materialIndex][vertexIndex + 2];
						reference7 -= centerOfLine;
						ref Vector3 reference8 = ref copyOfVertices[materialIndex][vertexIndex + 3];
						reference8 -= centerOfLine;
						matrix = Matrix4x4.TRS(Vector3.one, rotation, Vector3.one);
						copyOfVertices[materialIndex][vertexIndex] = ((Matrix4x4)(ref matrix)).MultiplyPoint3x4(copyOfVertices[materialIndex][vertexIndex]);
						copyOfVertices[materialIndex][vertexIndex + 1] = ((Matrix4x4)(ref matrix)).MultiplyPoint3x4(copyOfVertices[materialIndex][vertexIndex + 1]);
						copyOfVertices[materialIndex][vertexIndex + 2] = ((Matrix4x4)(ref matrix)).MultiplyPoint3x4(copyOfVertices[materialIndex][vertexIndex + 2]);
						copyOfVertices[materialIndex][vertexIndex + 3] = ((Matrix4x4)(ref matrix)).MultiplyPoint3x4(copyOfVertices[materialIndex][vertexIndex + 3]);
						ref Vector3 reference9 = ref copyOfVertices[materialIndex][vertexIndex];
						reference9 += centerOfLine;
						ref Vector3 reference10 = ref copyOfVertices[materialIndex][vertexIndex + 1];
						reference10 += centerOfLine;
						ref Vector3 reference11 = ref copyOfVertices[materialIndex][vertexIndex + 2];
						reference11 += centerOfLine;
						ref Vector3 reference12 = ref copyOfVertices[materialIndex][vertexIndex + 3];
						reference12 += centerOfLine;
					}
				}
			}
			for (int l = 0; l < textInfo.meshInfo.Length; l++)
			{
				textInfo.meshInfo[l].mesh.vertices = copyOfVertices[l];
				m_TextComponent.UpdateGeometry(textInfo.meshInfo[l].mesh, l);
			}
			yield return (object)new WaitForSeconds(0.1f);
		}
	}
}
