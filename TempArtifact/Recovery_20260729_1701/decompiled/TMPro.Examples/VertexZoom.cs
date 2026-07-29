using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TMPro.Examples;

public class VertexZoom : MonoBehaviour
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
		if (obj == (Object)(object)m_TextComponent)
		{
			hasTextChanged = true;
		}
	}

	private IEnumerator AnimateVertexColors()
	{
		m_TextComponent.ForceMeshUpdate(false, false);
		TMP_TextInfo textInfo = m_TextComponent.textInfo;
		TMP_MeshInfo[] cachedMeshInfoVertexData = textInfo.CopyMeshInfoVertexData();
		List<float> modifiedCharScale = new List<float>();
		List<int> scaleSortingOrder = new List<int>();
		hasTextChanged = true;
		while (true)
		{
			if (hasTextChanged)
			{
				cachedMeshInfoVertexData = textInfo.CopyMeshInfoVertexData();
				hasTextChanged = false;
			}
			int characterCount = textInfo.characterCount;
			if (characterCount == 0)
			{
				yield return (object)new WaitForSeconds(0.25f);
				continue;
			}
			modifiedCharScale.Clear();
			scaleSortingOrder.Clear();
			for (int i = 0; i < characterCount; i++)
			{
				TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
				if (charInfo.isVisible)
				{
					int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;
					int vertexIndex = textInfo.characterInfo[i].vertexIndex;
					Vector3[] sourceVertices = cachedMeshInfoVertexData[materialIndex].vertices;
					Vector2 charMidBasline = Vector2.op_Implicit((sourceVertices[vertexIndex] + sourceVertices[vertexIndex + 2]) / 2f);
					Vector3 offset = Vector2.op_Implicit(charMidBasline);
					Vector3[] destinationVertices = textInfo.meshInfo[materialIndex].vertices;
					destinationVertices[vertexIndex] = sourceVertices[vertexIndex] - offset;
					destinationVertices[vertexIndex + 1] = sourceVertices[vertexIndex + 1] - offset;
					destinationVertices[vertexIndex + 2] = sourceVertices[vertexIndex + 2] - offset;
					destinationVertices[vertexIndex + 3] = sourceVertices[vertexIndex + 3] - offset;
					float randomScale = Random.Range(1f, 1.5f);
					modifiedCharScale.Add(randomScale);
					scaleSortingOrder.Add(modifiedCharScale.Count - 1);
					Matrix4x4 matrix = Matrix4x4.TRS(new Vector3(0f, 0f, 0f), Quaternion.identity, Vector3.one * randomScale);
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
					Vector2[] sourceUVs0 = cachedMeshInfoVertexData[materialIndex].uvs0;
					Vector2[] destinationUVs0 = textInfo.meshInfo[materialIndex].uvs0;
					destinationUVs0[vertexIndex] = sourceUVs0[vertexIndex];
					destinationUVs0[vertexIndex + 1] = sourceUVs0[vertexIndex + 1];
					destinationUVs0[vertexIndex + 2] = sourceUVs0[vertexIndex + 2];
					destinationUVs0[vertexIndex + 3] = sourceUVs0[vertexIndex + 3];
					Color32[] sourceColors32 = cachedMeshInfoVertexData[materialIndex].colors32;
					Color32[] destinationColors32 = textInfo.meshInfo[materialIndex].colors32;
					destinationColors32[vertexIndex] = sourceColors32[vertexIndex];
					destinationColors32[vertexIndex + 1] = sourceColors32[vertexIndex + 1];
					destinationColors32[vertexIndex + 2] = sourceColors32[vertexIndex + 2];
					destinationColors32[vertexIndex + 3] = sourceColors32[vertexIndex + 3];
				}
			}
			for (int j = 0; j < textInfo.meshInfo.Length; j++)
			{
				scaleSortingOrder.Sort((int a, int b) => modifiedCharScale[a].CompareTo(modifiedCharScale[b]));
				((TMP_MeshInfo)(ref textInfo.meshInfo[j])).SortGeometry((IList<int>)scaleSortingOrder);
				textInfo.meshInfo[j].mesh.vertices = textInfo.meshInfo[j].vertices;
				textInfo.meshInfo[j].mesh.uv = textInfo.meshInfo[j].uvs0;
				textInfo.meshInfo[j].mesh.colors32 = textInfo.meshInfo[j].colors32;
				m_TextComponent.UpdateGeometry(textInfo.meshInfo[j].mesh, j);
			}
			yield return (object)new WaitForSeconds(0.1f);
		}
	}
}
