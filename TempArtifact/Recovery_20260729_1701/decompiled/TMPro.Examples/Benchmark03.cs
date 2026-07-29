using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace TMPro.Examples;

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
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Expected O, but got Unknown
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_017e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0183: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b8: Expected O, but got Unknown
		//IL_01ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_0230: Unknown result type (might be due to invalid IL or missing references)
		//IL_0235: Unknown result type (might be due to invalid IL or missing references)
		TMP_FontAsset val = null;
		switch (Benchmark)
		{
		case BenchmarkType.TMP_SDF_MOBILE:
			val = TMP_FontAsset.CreateFontAsset(SourceFont, 90, 9, (GlyphRenderMode)4165, 256, 256, (AtlasPopulationMode)1, true);
			break;
		case BenchmarkType.TMP_SDF__MOBILE_SSD:
			val = TMP_FontAsset.CreateFontAsset(SourceFont, 90, 9, (GlyphRenderMode)4165, 256, 256, (AtlasPopulationMode)1, true);
			((TMP_Asset)val).material.shader = Shader.Find("TextMeshPro/Mobile/Distance Field SSD");
			break;
		case BenchmarkType.TMP_SDF:
			val = TMP_FontAsset.CreateFontAsset(SourceFont, 90, 9, (GlyphRenderMode)4165, 256, 256, (AtlasPopulationMode)1, true);
			((TMP_Asset)val).material.shader = Shader.Find("TextMeshPro/Distance Field");
			break;
		case BenchmarkType.TMP_BITMAP_MOBILE:
			val = TMP_FontAsset.CreateFontAsset(SourceFont, 90, 9, (GlyphRenderMode)4117, 256, 256, (AtlasPopulationMode)1, true);
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
				GameObject val4 = new GameObject();
				val4.transform.position = new Vector3(0f, 1.2f, 0f);
				TextMeshPro val5 = val4.AddComponent<TextMeshPro>();
				((TMP_Text)val5).font = val;
				((TMP_Text)val5).fontSize = 128f;
				((TMP_Text)val5).text = "@";
				((TMP_Text)val5).alignment = (TextAlignmentOptions)514;
				((Graphic)val5).color = Color32.op_Implicit(new Color32(byte.MaxValue, byte.MaxValue, (byte)0, byte.MaxValue));
				if (Benchmark == BenchmarkType.TMP_BITMAP_MOBILE)
				{
					((TMP_Text)val5).fontSize = 132f;
				}
				break;
			}
			case BenchmarkType.TEXTMESH_BITMAP:
			{
				GameObject val2 = new GameObject();
				val2.transform.position = new Vector3(0f, 1.2f, 0f);
				TextMesh val3 = val2.AddComponent<TextMesh>();
				((Component)val3).GetComponent<Renderer>().sharedMaterial = SourceFont.material;
				val3.font = SourceFont;
				val3.anchor = (TextAnchor)4;
				val3.fontSize = 130;
				val3.color = Color32.op_Implicit(new Color32(byte.MaxValue, byte.MaxValue, (byte)0, byte.MaxValue));
				val3.text = "@";
				break;
			}
			}
		}
	}
}
