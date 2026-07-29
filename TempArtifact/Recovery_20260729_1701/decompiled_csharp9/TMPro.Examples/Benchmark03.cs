using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace TMPro.Examples
{
	public class Benchmark03 : MonoBehaviour
	{
		public enum BenchmarkType
		{
			TMP_SDF_MOBILE,
			TMP_SDF__MOBILE_SSD,
			TMP_SDF,
			TMP_BITMAP_MOBILE,
			TEXTMESH_BITMAP
		}

		public int NumberOfSamples = 100;

		public BenchmarkType Benchmark;

		public Font SourceFont;

		private void Awake()
		{
		}

		private void Start()
		{
			TMP_FontAsset fontAsset = null;
			switch (Benchmark)
			{
			case BenchmarkType.TMP_SDF_MOBILE:
				fontAsset = TMP_FontAsset.CreateFontAsset(SourceFont, 90, 9, GlyphRenderMode.SDFAA, 256, 256, (AtlasPopulationMode)1, true);
				break;
			case BenchmarkType.TMP_SDF__MOBILE_SSD:
				fontAsset = TMP_FontAsset.CreateFontAsset(SourceFont, 90, 9, GlyphRenderMode.SDFAA, 256, 256, (AtlasPopulationMode)1, true);
				((TMP_Asset)fontAsset).material.shader = Shader.Find("TextMeshPro/Mobile/Distance Field SSD");
				break;
			case BenchmarkType.TMP_SDF:
				fontAsset = TMP_FontAsset.CreateFontAsset(SourceFont, 90, 9, GlyphRenderMode.SDFAA, 256, 256, (AtlasPopulationMode)1, true);
				((TMP_Asset)fontAsset).material.shader = Shader.Find("TextMeshPro/Distance Field");
				break;
			case BenchmarkType.TMP_BITMAP_MOBILE:
				fontAsset = TMP_FontAsset.CreateFontAsset(SourceFont, 90, 9, GlyphRenderMode.SMOOTH, 256, 256, (AtlasPopulationMode)1, true);
				break;
			}
			for (int i = 0; i < NumberOfSamples; i++)
			{
				switch (Benchmark)
				{
				case BenchmarkType.TMP_SDF_MOBILE:
				case BenchmarkType.TMP_SDF__MOBILE_SSD:
				case BenchmarkType.TMP_SDF:
				case BenchmarkType.TMP_BITMAP_MOBILE:
				{
					GameObject go2 = new GameObject();
					go2.transform.position = new Vector3(0f, 1.2f, 0f);
					TextMeshPro textComponent = go2.AddComponent<TextMeshPro>();
					((TMP_Text)textComponent).font = fontAsset;
					((TMP_Text)textComponent).fontSize = 128f;
					((TMP_Text)textComponent).text = "@";
					((TMP_Text)textComponent).alignment = (TextAlignmentOptions)514;
					((Graphic)textComponent).color = new Color32(byte.MaxValue, byte.MaxValue, 0, byte.MaxValue);
					if (Benchmark == BenchmarkType.TMP_BITMAP_MOBILE)
					{
						((TMP_Text)textComponent).fontSize = 132f;
					}
					break;
				}
				case BenchmarkType.TEXTMESH_BITMAP:
				{
					GameObject go = new GameObject();
					go.transform.position = new Vector3(0f, 1.2f, 0f);
					TextMesh textMesh = go.AddComponent<TextMesh>();
					textMesh.GetComponent<Renderer>().sharedMaterial = SourceFont.material;
					textMesh.font = SourceFont;
					textMesh.anchor = TextAnchor.MiddleCenter;
					textMesh.fontSize = 130;
					textMesh.color = new Color32(byte.MaxValue, byte.MaxValue, 0, byte.MaxValue);
					textMesh.text = "@";
					break;
				}
				}
			}
		}
	}
}
