using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DefenseGame;

public class CharacterCollectionUI : MonoBehaviour
{
	[SerializeField]
	private CharacterDatabase characterDatabase;

	[SerializeField]
	private OutgameProgressionSystem outgameProgression;

	[SerializeField]
	private int cardsPerPage = 12;

	private GameObject root;

	private Font font;

	private UiSkinResources uiSkin;

	private Text pageText;

	private Text collectionCountText;

	private Text selectedNameText;

	private Text selectedGradeText;

	private Text selectedRoleText;

	private Text selectedDescriptionText;

	private Text selectedPrefabText;

	private Image selectedGradeBack;

	private Text statAttackText;

	private Text statHealthText;

	private Text statCritText;

	private Text statSpeedText;

	private Text statManaText;

	private Text statRangeText;

	private Text skillText;

	private Text selectedPortraitLabelText;

	private Image selectedPortrait;

	private readonly List<Button> cardButtons = new List<Button>();

	private readonly List<Image> cardBackgroundImages = new List<Image>();

	private readonly List<Text> cardNameTexts = new List<Text>();

	private readonly List<Text> cardGradeTexts = new List<Text>();

	private readonly List<Text> cardRoleTexts = new List<Text>();

	private readonly List<Text> cardPortraitTexts = new List<Text>();

	private readonly List<Image> cardPortraitImages = new List<Image>();

	private readonly List<Image> cardAccentImages = new List<Image>();

	private int currentPage;

	private int selectedIndex;

	private Sprite roundedSprite;

	public bool IsOpen => (Object)(object)root != (Object)null && root.activeSelf;

	public event Action OnClosed;

	public event Action OnOpened;

	public void Configure(CharacterDatabase database, OutgameProgressionSystem progression, Font uiFont, Transform canvasRoot, UiSkinResources skin = null)
	{
		if ((Object)(object)outgameProgression != (Object)null)
		{
			outgameProgression.OnProgressChanged -= HandleProgressChanged;
		}
		characterDatabase = database;
		outgameProgression = progression;
		font = uiFont;
		uiSkin = skin;
		if ((Object)(object)outgameProgression != (Object)null)
		{
			outgameProgression.OnProgressChanged += HandleProgressChanged;
		}
		if ((Object)(object)root != (Object)null)
		{
			if (Application.isPlaying)
			{
				Object.Destroy((Object)(object)root);
			}
			else
			{
				Object.DestroyImmediate((Object)(object)root);
			}
		}
		Build(canvasRoot);
		ShowPage(0);
		Close();
	}

	private void OnDestroy()
	{
		if ((Object)(object)outgameProgression != (Object)null)
		{
			outgameProgression.OnProgressChanged -= HandleProgressChanged;
		}
	}

	public void Open()
	{
		if (!((Object)(object)root == (Object)null))
		{
			root.SetActive(true);
			root.transform.SetAsLastSibling();
			this.OnOpened?.Invoke();
			ShowPage(currentPage);
		}
	}

	public void Close()
	{
		if ((Object)(object)root != (Object)null && root.activeSelf)
		{
			root.SetActive(false);
			this.OnClosed?.Invoke();
		}
	}

	public void Toggle()
	{
		if (!((Object)(object)root == (Object)null))
		{
			if (root.activeSelf)
			{
				Close();
			}
			else
			{
				Open();
			}
		}
	}

	private void Build(Transform parent)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Expected O, but got Unknown
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_0152: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_0170: Unknown result type (might be due to invalid IL or missing references)
		//IL_017f: Unknown result type (might be due to invalid IL or missing references)
		//IL_019d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0218: Unknown result type (might be due to invalid IL or missing references)
		//IL_0227: Unknown result type (might be due to invalid IL or missing references)
		//IL_0236: Unknown result type (might be due to invalid IL or missing references)
		//IL_0245: Unknown result type (might be due to invalid IL or missing references)
		//IL_0254: Unknown result type (might be due to invalid IL or missing references)
		//IL_0263: Unknown result type (might be due to invalid IL or missing references)
		//IL_028c: Unknown result type (might be due to invalid IL or missing references)
		//IL_029b: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_030e: Unknown result type (might be due to invalid IL or missing references)
		//IL_031d: Unknown result type (might be due to invalid IL or missing references)
		//IL_032c: Unknown result type (might be due to invalid IL or missing references)
		//IL_033b: Unknown result type (might be due to invalid IL or missing references)
		//IL_034a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0381: Unknown result type (might be due to invalid IL or missing references)
		//IL_0390: Unknown result type (might be due to invalid IL or missing references)
		//IL_039f: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_03bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ee: Expected O, but got Unknown
		//IL_040b: Unknown result type (might be due to invalid IL or missing references)
		//IL_041a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0429: Unknown result type (might be due to invalid IL or missing references)
		//IL_0438: Unknown result type (might be due to invalid IL or missing references)
		//IL_0447: Unknown result type (might be due to invalid IL or missing references)
		//IL_0460: Unknown result type (might be due to invalid IL or missing references)
		//IL_046c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0478: Expected O, but got Unknown
		//IL_048c: Unknown result type (might be due to invalid IL or missing references)
		//IL_049b: Unknown result type (might be due to invalid IL or missing references)
		//IL_04aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_04c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d7: Unknown result type (might be due to invalid IL or missing references)
		root = new GameObject("CharacterCollectionOverlay", new Type[1] { typeof(RectTransform) });
		root.transform.SetParent(parent, false);
		Image val = root.AddComponent<Image>();
		((Graphic)val).color = Color.clear;
		((Graphic)val).raycastTarget = false;
		RectTransform component = root.GetComponent<RectTransform>();
		component.anchorMin = Vector2.zero;
		component.anchorMax = Vector2.one;
		component.offsetMin = Vector2.zero;
		component.offsetMax = Vector2.zero;
		Image val2 = CreatePanel(root.transform, "CollectionModal", new Vector2(0f, 76f), new Vector2(0f, -152f), new Color(0.25f, 0.34f, 0.7f, 1f), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), rounded: false, shadow: false);
		val2.sprite = null;
		val2.type = (Type)0;
		val2.preserveAspect = false;
		CreatePanel(((Component)val2).transform, "Header", new Vector2(0f, -160f), new Vector2(912f, 116f), new Color(0.96f, 0.8f, 0.18f, 0.96f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
		CreateText(((Component)val2).transform, "Title", "캐릭터 도감", Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -184f), new Vector2(420f, 52f), 38, (TextAnchor)4, bold: true);
		collectionCountText = CreateText(((Component)val2).transform, "CollectionCount", "등록 캐릭터 0명", new Color(0.18f, 0.22f, 0.34f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -226f), new Vector2(420f, 28f), 18, (TextAnchor)4, bold: true);
		Image val3 = CreatePanel(((Component)val2).transform, "CardGridPanel", new Vector2(-194f, -430f), new Vector2(556f, 1130f), new Color(0.19f, 0.24f, 0.54f, 0.95f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: true);
		CreateText(((Component)val3).transform, "GridHeader", "등록된 영웅", Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(220f, 28f), 22, (TextAnchor)4, bold: true);
		BuildCardGrid(((Component)val3).transform);
		Button val4 = CreateButton(((Component)val2).transform, "PrevPageButton", "<", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-190f, -1732f), new Vector2(90f, 58f), new Color(0.15f, 0.2f, 0.43f, 1f), new UnityAction(PreviousPage), 28);
		Button val5 = CreateButton(((Component)val2).transform, "NextPageButton", ">", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(190f, -1732f), new Vector2(90f, 58f), new Color(0.15f, 0.2f, 0.43f, 1f), new UnityAction(NextPage), 28);
		pageText = CreateText(((Component)val2).transform, "PageText", "1 / 1", Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -1732f), new Vector2(220f, 46f), 23, (TextAnchor)4, bold: true);
		((Object)((Component)val4).gameObject).name = "PrevPageButton";
		((Object)((Component)val5).gameObject).name = "NextPageButton";
		BuildDetailPanel(((Component)val2).transform);
	}

	private void BuildCardGrid(Transform parent)
	{
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Expected O, but got Unknown
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		//IL_015b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0160: Unknown result type (might be due to invalid IL or missing references)
		//IL_016f: Unknown result type (might be due to invalid IL or missing references)
		//IL_017e: Unknown result type (might be due to invalid IL or missing references)
		//IL_018d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01da: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0207: Unknown result type (might be due to invalid IL or missing references)
		//IL_022b: Unknown result type (might be due to invalid IL or missing references)
		//IL_023a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0253: Unknown result type (might be due to invalid IL or missing references)
		//IL_0262: Unknown result type (might be due to invalid IL or missing references)
		//IL_0271: Unknown result type (might be due to invalid IL or missing references)
		//IL_0280: Unknown result type (might be due to invalid IL or missing references)
		//IL_02af: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0308: Unknown result type (might be due to invalid IL or missing references)
		//IL_0321: Unknown result type (might be due to invalid IL or missing references)
		//IL_0330: Unknown result type (might be due to invalid IL or missing references)
		//IL_033f: Unknown result type (might be due to invalid IL or missing references)
		//IL_034e: Unknown result type (might be due to invalid IL or missing references)
		//IL_037c: Unknown result type (might be due to invalid IL or missing references)
		//IL_038b: Unknown result type (might be due to invalid IL or missing references)
		//IL_039a: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0407: Unknown result type (might be due to invalid IL or missing references)
		//IL_0416: Unknown result type (might be due to invalid IL or missing references)
		//IL_0425: Unknown result type (might be due to invalid IL or missing references)
		//IL_0434: Unknown result type (might be due to invalid IL or missing references)
		//IL_0443: Unknown result type (might be due to invalid IL or missing references)
		//IL_0465: Unknown result type (might be due to invalid IL or missing references)
		//IL_0474: Unknown result type (might be due to invalid IL or missing references)
		//IL_0483: Unknown result type (might be due to invalid IL or missing references)
		//IL_0492: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b0: Unknown result type (might be due to invalid IL or missing references)
		cardButtons.Clear();
		cardBackgroundImages.Clear();
		cardNameTexts.Clear();
		cardGradeTexts.Clear();
		cardRoleTexts.Clear();
		cardPortraitTexts.Clear();
		cardPortraitImages.Clear();
		cardAccentImages.Clear();
		Vector2 anchoredPosition = default(Vector2);
		for (int i = 0; i < cardsPerPage; i++)
		{
			int localIndex = i;
			int num = i % 3;
			int num2 = i / 3;
			((Vector2)(ref anchoredPosition))._002Ector(-168f + (float)num * 168f, -94f - (float)num2 * 242f);
			Button val = CreateButton(parent, "CharacterCard_" + i, string.Empty, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), anchoredPosition, new Vector2(152f, 210f), new Color(0.95f, 0.96f, 0.98f, 0.98f), (UnityAction)delegate
			{
				SelectCard(currentPage * cardsPerPage + localIndex);
			}, 24);
			Image component = ((Component)val).GetComponent<Image>();
			Image item = CreatePanel(((Component)val).transform, "Accent", new Vector2(0f, -8f), new Vector2(132f, 46f), Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
			CreatePanel(((Component)val).transform, "GradePlate", new Vector2(0f, -10f), new Vector2(120f, 32f), new Color(0.03f, 0.05f, 0.18f, 0.78f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
			Image val2 = CreatePanel(((Component)val).transform, "Portrait", new Vector2(0f, -54f), new Vector2(126f, 88f), new Color(0.8f, 0.87f, 1f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
			Text item2 = CreateText(((Component)val2).transform, "PortraitLabel", "HG", new Color(0.18f, 0.24f, 0.36f), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, 26, (TextAnchor)4, bold: true);
			CreatePanel(((Component)val).transform, "InfoBack", new Vector2(0f, 8f), new Vector2(132f, 58f), new Color(0.03f, 0.05f, 0.18f, 0.78f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), rounded: true, shadow: false);
			Text item3 = CreateText(((Component)val).transform, "Name", "Hero", new Color(0.22f, 0.25f, 0.36f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 36f), new Vector2(132f, 26f), 16, (TextAnchor)4, bold: true);
			Text item4 = CreateText(((Component)val).transform, "Role", "전위", new Color(0.2f, 0.24f, 0.39f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 13f), new Vector2(132f, 22f), 13, (TextAnchor)4, bold: false);
			Text val3 = CreateText(((Component)val).transform, "Grade", "일반", Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -11f), new Vector2(118f, 28f), 18, (TextAnchor)4, bold: true);
			AddReadableOutline(val3);
			cardButtons.Add(val);
			cardBackgroundImages.Add(component);
			cardNameTexts.Add(item3);
			cardGradeTexts.Add(val3);
			cardRoleTexts.Add(item4);
			cardPortraitTexts.Add(item2);
			cardPortraitImages.Add(val2);
			cardAccentImages.Add(item);
		}
	}

	private void BuildDetailPanel(Transform parent)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
		//IL_015f: Unknown result type (might be due to invalid IL or missing references)
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		//IL_017d: Unknown result type (might be due to invalid IL or missing references)
		//IL_018c: Unknown result type (might be due to invalid IL or missing references)
		//IL_019b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_020b: Unknown result type (might be due to invalid IL or missing references)
		//IL_021a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0229: Unknown result type (might be due to invalid IL or missing references)
		//IL_025b: Unknown result type (might be due to invalid IL or missing references)
		//IL_026a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0279: Unknown result type (might be due to invalid IL or missing references)
		//IL_0288: Unknown result type (might be due to invalid IL or missing references)
		//IL_0297: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0305: Unknown result type (might be due to invalid IL or missing references)
		//IL_0314: Unknown result type (might be due to invalid IL or missing references)
		//IL_0323: Unknown result type (might be due to invalid IL or missing references)
		//IL_0332: Unknown result type (might be due to invalid IL or missing references)
		//IL_0366: Unknown result type (might be due to invalid IL or missing references)
		//IL_0375: Unknown result type (might be due to invalid IL or missing references)
		//IL_0384: Unknown result type (might be due to invalid IL or missing references)
		//IL_0393: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0403: Unknown result type (might be due to invalid IL or missing references)
		//IL_0412: Unknown result type (might be due to invalid IL or missing references)
		//IL_0421: Unknown result type (might be due to invalid IL or missing references)
		//IL_0430: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0502: Unknown result type (might be due to invalid IL or missing references)
		//IL_0511: Unknown result type (might be due to invalid IL or missing references)
		//IL_0520: Unknown result type (might be due to invalid IL or missing references)
		//IL_052f: Unknown result type (might be due to invalid IL or missing references)
		//IL_053e: Unknown result type (might be due to invalid IL or missing references)
		//IL_056e: Unknown result type (might be due to invalid IL or missing references)
		//IL_057d: Unknown result type (might be due to invalid IL or missing references)
		//IL_058c: Unknown result type (might be due to invalid IL or missing references)
		//IL_059b: Unknown result type (might be due to invalid IL or missing references)
		//IL_05aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_05b9: Unknown result type (might be due to invalid IL or missing references)
		Image val = CreatePanel(parent, "DetailPanel", new Vector2(274f, -430f), new Vector2(360f, 1130f), new Color(0.13f, 0.18f, 0.46f, 0.96f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: true);
		selectedPortrait = CreatePanel(((Component)val).transform, "SelectedPortrait", new Vector2(0f, -38f), new Vector2(288f, 218f), new Color(0.9f, 0.93f, 1f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
		selectedPortraitLabelText = CreateText(((Component)selectedPortrait).transform, "PortraitText", "HG", new Color(0.18f, 0.24f, 0.36f), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, 48, (TextAnchor)4, bold: true);
		selectedNameText = CreateText(((Component)val).transform, "SelectedName", "Hero", Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -292f), new Vector2(300f, 42f), 30, (TextAnchor)4, bold: true);
		selectedGradeBack = CreatePanel(((Component)val).transform, "SelectedGradeBack", new Vector2(0f, -340f), new Vector2(210f, 42f), new Color(0.05f, 0.07f, 0.2f, 0.82f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
		selectedGradeText = CreateText(((Component)val).transform, "SelectedGrade", "일반", new Color(1f, 0.92f, 0.48f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -342f), new Vector2(206f, 36f), 25, (TextAnchor)4, bold: true);
		AddReadableOutline(selectedGradeText);
		selectedRoleText = CreateText(((Component)val).transform, "SelectedRole", "전위", new Color(0.8f, 0.88f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -386f), new Vector2(300f, 28f), 18, (TextAnchor)4, bold: true);
		selectedDescriptionText = CreateText(((Component)val).transform, "SelectedDescription", string.Empty, new Color(0.88f, 0.91f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -430f), new Vector2(308f, 94f), 16, (TextAnchor)0, bold: false);
		selectedPrefabText = CreateText(((Component)val).transform, "SelectedPrefabText", string.Empty, new Color(0.68f, 0.86f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -526f), new Vector2(308f, 28f), 15, (TextAnchor)3, bold: true);
		statAttackText = CreateStatRow(((Component)val).transform, 0, "공격력");
		statHealthText = CreateStatRow(((Component)val).transform, 1, "체력");
		statCritText = CreateStatRow(((Component)val).transform, 2, "치명타");
		statSpeedText = CreateStatRow(((Component)val).transform, 3, "공속");
		statManaText = CreateStatRow(((Component)val).transform, 4, "마나");
		statRangeText = CreateStatRow(((Component)val).transform, 5, "사거리");
		CreateText(((Component)val).transform, "SkillHeader", "스킬 정보", new Color(1f, 0.92f, 0.48f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -820f), new Vector2(308f, 28f), 22, (TextAnchor)3, bold: true);
		skillText = CreateText(((Component)val).transform, "SkillText", string.Empty, new Color(0.92f, 0.94f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -868f), new Vector2(308f, 210f), 14, (TextAnchor)0, bold: false);
	}

	private Text CreateStatRow(Transform parent, int index, string label)
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		float num = -568f - (float)index * 42f;
		Image val = CreatePanel(parent, "StatRow_" + label, new Vector2(0f, num), new Vector2(308f, 34f), new Color(0.11f, 0.15f, 0.34f, 0.84f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
		CreateText(((Component)val).transform, "Label", label, new Color(0.73f, 0.83f, 1f), Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), new Vector2(18f, 0f), new Vector2(132f, 0f), 16, (TextAnchor)3, bold: true);
		return CreateText(((Component)val).transform, "Value", "0", Color.white, Vector2.zero, Vector2.one, new Vector2(1f, 0.5f), new Vector2(-18f, 0f), new Vector2(144f, 0f), 17, (TextAnchor)5, bold: true);
	}

	private void ShowPage(int page)
	{
		//IL_0219: Unknown result type (might be due to invalid IL or missing references)
		//IL_021e: Unknown result type (might be due to invalid IL or missing references)
		int characterCount = GetCharacterCount();
		int num = Mathf.Max(1, Mathf.CeilToInt((float)characterCount / (float)cardsPerPage));
		currentPage = Mathf.Clamp(page, 0, num - 1);
		if ((Object)(object)pageText != (Object)null)
		{
			pageText.text = currentPage + 1 + " / " + num;
		}
		if ((Object)(object)collectionCountText != (Object)null)
		{
			collectionCountText.text = (((Object)(object)outgameProgression != (Object)null) ? outgameProgression.BuildCollectionSummary() : ("등록 캐릭터 " + characterCount + "명"));
		}
		for (int i = 0; i < cardsPerPage; i++)
		{
			int index = currentPage * cardsPerPage + i;
			CharacterDefinition character = GetCharacter(index);
			bool flag = character != null;
			((Component)cardButtons[i]).gameObject.SetActive(flag);
			if (flag)
			{
				cardNameTexts[i].text = character.displayName;
				cardGradeTexts[i].text = GetGradeName(character.grade);
				int num2 = (((Object)(object)outgameProgression != (Object)null) ? outgameProgression.GetDisplayCardLevel(character.id) : 0);
				string text = ((num2 > 0) ? ("Lv." + num2) : "미획득");
				cardRoleTexts[i].text = text + " / " + GetRoleName(character.role);
				cardPortraitTexts[i].text = BuildPortraitLabel(character.displayName);
				ApplyCharacterPortrait(cardPortraitImages[i], cardPortraitTexts[i], character);
				((Graphic)cardAccentImages[i]).color = GetGradeColor(character.grade, character.accentColor);
			}
		}
		if (characterCount > 0)
		{
			int index2 = Mathf.Clamp(selectedIndex, currentPage * cardsPerPage, Mathf.Min(characterCount - 1, currentPage * cardsPerPage + cardsPerPage - 1));
			SelectCard(index2);
		}
	}

	private void SelectCard(int index)
	{
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		CharacterDefinition character = GetCharacter(index);
		if (character != null)
		{
			selectedIndex = index;
			ApplyCharacterPortrait(selectedPortrait, selectedPortraitLabelText, character);
			selectedNameText.text = character.displayName;
			selectedGradeText.text = GetGradeName(character.grade);
			RuntimeUiSkinUtility.ApplyReadableTextColor(selectedGradeText, GetGradeColor(character.grade, character.accentColor), uiSkin);
			if ((Object)(object)selectedGradeBack != (Object)null)
			{
				((Graphic)selectedGradeBack).color = Color.Lerp(GetGradeColor(character.grade, character.accentColor), new Color(0.03f, 0.05f, 0.18f, 1f), 0.34f);
			}
			selectedRoleText.text = GetRoleName(character.role);
			selectedDescriptionText.text = character.description;
			selectedPrefabText.text = (((Object)(object)outgameProgression != (Object)null) ? outgameProgression.BuildProgressText(character.id) : (((Object)(object)character.prefab != (Object)null) ? ("연결 프리팹: " + ((Object)character.prefab).name) : "연결 프리팹: 기본 템플릿 사용"));
			CombatStats stats = character.stats;
			int num = (((Object)(object)outgameProgression != (Object)null) ? Mathf.Max(0, outgameProgression.GetCardLevel(character.id) - 1) : 0);
			float num2 = (((Object)(object)outgameProgression != (Object)null) ? (1f + (float)num * outgameProgression.Settings.attackPowerPerGrowthLevel) : 1f);
			float num3 = (((Object)(object)outgameProgression != (Object)null) ? (1f + (float)num * outgameProgression.Settings.maxHealthPerGrowthLevel) : 1f);
			statAttackText.text = Mathf.RoundToInt(stats.attackPower * num2).ToString();
			statHealthText.text = Mathf.RoundToInt(stats.maxHealth * num3).ToString();
			statCritText.text = Mathf.RoundToInt(stats.criticalChance * 100f) + "%";
			statSpeedText.text = stats.attackSpeed.ToString("0.00");
			statManaText.text = Mathf.RoundToInt(stats.maxMana).ToString();
			statRangeText.text = stats.attackRange.ToString("0.0");
			skillText.text = BuildSkillSummary(character);
			UpdateCardSelection();
		}
	}

	private void HandleProgressChanged()
	{
		ShowPage(currentPage);
	}

	private void UpdateCardSelection()
	{
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		for (int i = 0; i < cardButtons.Count; i++)
		{
			int num = currentPage * cardsPerPage + i;
			CharacterDefinition character = GetCharacter(num);
			if (character != null)
			{
				bool flag = num == selectedIndex;
				if ((Object)(object)cardBackgroundImages[i] != (Object)null)
				{
					((Graphic)cardBackgroundImages[i]).color = (Color)(flag ? Color.Lerp(character.accentColor, Color.white, 0.58f) : new Color(0.95f, 0.96f, 0.98f, 0.98f));
				}
				RectTransform component = ((Component)cardButtons[i]).GetComponent<RectTransform>();
				if ((Object)(object)component != (Object)null)
				{
					((Transform)component).localScale = (flag ? (Vector3.one * 1.04f) : Vector3.one);
				}
			}
		}
	}

	private void PreviousPage()
	{
		ShowPage(currentPage - 1);
	}

	private void NextPage()
	{
		ShowPage(currentPage + 1);
	}

	private int GetCharacterCount()
	{
		return ((Object)(object)characterDatabase != (Object)null && characterDatabase.Characters != null) ? characterDatabase.Characters.Count : 0;
	}

	private CharacterDefinition GetCharacter(int index)
	{
		if ((Object)(object)characterDatabase == (Object)null || characterDatabase.Characters == null || index < 0 || index >= characterDatabase.Characters.Count)
		{
			return null;
		}
		return characterDatabase.Characters[index];
	}

	private string BuildSkillSummary(CharacterDefinition definition)
	{
		if (definition.skills == null || definition.skills.Count == 0)
		{
			return "보유 스킬 없음";
		}
		string text = string.Empty;
		for (int i = 0; i < definition.skills.Count; i++)
		{
			SkillDefinition skillDefinition = definition.skills[i];
			if (i > 0)
			{
				text += "\n";
			}
			string categoryDisplayName = SkillDefinitionUtility.GetCategoryDisplayName(skillDefinition.ResolvedCategory);
			string text2 = SkillDefinitionUtility.BuildDisplayDescription(skillDefinition);
			string text3 = SkillDefinitionUtility.BuildGrowthDisplayText(skillDefinition);
			text = text + "• " + skillDefinition.displayName + " [" + categoryDisplayName + "]\n  " + text2;
			if (!string.IsNullOrWhiteSpace(text3))
			{
				text = text + "\n  " + text3;
			}
		}
		return text;
	}

	private void ApplyCharacterPortrait(Image portraitImage, Text fallbackText, CharacterDefinition definition)
	{
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)portraitImage == (Object)null)
		{
			return;
		}
		Sprite val = RollRollUiResource.ResolveCharacterSprite(definition);
		if ((Object)(object)val != (Object)null)
		{
			portraitImage.sprite = val;
			portraitImage.type = (Type)0;
			portraitImage.preserveAspect = true;
			((Graphic)portraitImage).color = Color.white;
			if ((Object)(object)fallbackText != (Object)null)
			{
				((Component)fallbackText).gameObject.SetActive(false);
			}
		}
		else
		{
			portraitImage.sprite = null;
			((Graphic)portraitImage).color = (Color)((definition != null) ? Color.Lerp(definition.accentColor, Color.white, 0.35f) : new Color(0.8f, 0.87f, 1f, 1f));
			if ((Object)(object)fallbackText != (Object)null)
			{
				((Component)fallbackText).gameObject.SetActive(true);
				fallbackText.text = ((definition != null) ? BuildPortraitLabel(definition.displayName) : "??");
			}
		}
	}

	private string BuildPortraitLabel(string displayName)
	{
		if (string.IsNullOrWhiteSpace(displayName))
		{
			return "??";
		}
		string text = displayName.Trim();
		if (text.Length <= 2)
		{
			return text.ToUpperInvariant();
		}
		string[] array = text.Split(' ');
		if (array.Length >= 2 && !string.IsNullOrWhiteSpace(array[0]) && !string.IsNullOrWhiteSpace(array[1]))
		{
			return (array[0][0].ToString() + array[1][0]).ToUpperInvariant();
		}
		return text.Substring(0, 2).ToUpperInvariant();
	}

	private string GetGradeName(CharacterGrade grade)
	{
		return CharacterGradeUtility.GetDisplayName(grade);
	}

	private string GetRoleName(CharacterRole role)
	{
		return role switch
		{
			CharacterRole.Vanguard => "전위", 
			CharacterRole.Ranger => "사수", 
			CharacterRole.Mage => "마법", 
			CharacterRole.Support => "지원", 
			CharacterRole.Assassin => "암살", 
			_ => "소환", 
		};
	}

	private Color GetGradeColor(CharacterGrade grade, Color fallback)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		return CharacterGradeUtility.GetColor(grade, fallback);
	}

	private void AddReadableOutline(Text text)
	{
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)text == (Object)null))
		{
			Outline val = ((Component)text).GetComponent<Outline>();
			if ((Object)(object)val == (Object)null)
			{
				val = ((Component)text).gameObject.AddComponent<Outline>();
			}
			((Shadow)val).effectColor = new Color(0f, 0f, 0f, 0.78f);
			((Shadow)val).effectDistance = new Vector2(1.5f, -1.5f);
		}
	}

	private Image CreatePanel(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, bool rounded, bool shadow)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Expected O, but got Unknown
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject(name, new Type[1] { typeof(RectTransform) });
		val.transform.SetParent(parent, false);
		Image val2 = val.AddComponent<Image>();
		((Graphic)val2).color = color;
		((Graphic)val2).raycastTarget = false;
		RuntimeUiSkinUtility.ApplyImageSkin(val2, uiSkin, name, isButton: false, rounded);
		RollRollUiResource.TryApplyElementSprite(val2, name, isButton: false, rounded);
		RectTransform rectTransform = ((Graphic)val2).rectTransform;
		rectTransform.anchorMin = anchorMin;
		rectTransform.anchorMax = anchorMax;
		rectTransform.pivot = pivot;
		rectTransform.anchoredPosition = anchoredPosition;
		rectTransform.sizeDelta = size;
		if (shadow)
		{
			Shadow val3 = val.AddComponent<Shadow>();
			val3.effectColor = new Color(0f, 0f, 0f, 0.34f);
			val3.effectDistance = new Vector2(0f, -7f);
		}
		return val2;
	}

	private Button CreateButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 size, Color backgroundColor, UnityAction onClick, int fontSize)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Expected O, but got Unknown
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Expected O, but got Unknown
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject(name, new Type[1] { typeof(RectTransform) });
		val.transform.SetParent(parent, false);
		Image val2 = val.AddComponent<Image>();
		((Graphic)val2).color = backgroundColor;
		RuntimeUiSkinUtility.ApplyImageSkin(val2, uiSkin, name, isButton: true, rounded: true);
		RollRollUiResource.TryApplyElementSprite(val2, name, isButton: true, rounded: true);
		((Graphic)val2).raycastTarget = true;
		Shadow val3 = val.AddComponent<Shadow>();
		val3.effectColor = new Color(0f, 0f, 0f, 0.32f);
		val3.effectDistance = new Vector2(0f, -6f);
		Button val4 = val.AddComponent<Button>();
		((UnityEvent)val4.onClick).AddListener(new UnityAction(RuntimeAudioUtility.PlayButton));
		if (onClick != null)
		{
			((UnityEvent)val4.onClick).AddListener(onClick);
		}
		RectTransform component = ((Component)val4).GetComponent<RectTransform>();
		component.anchorMin = anchorMin;
		component.anchorMax = anchorMax;
		component.pivot = pivot;
		component.anchoredPosition = anchoredPosition;
		component.sizeDelta = size;
		if (!string.IsNullOrEmpty(label))
		{
			CreateText(val.transform, "Label", label, Color.white, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, fontSize, (TextAnchor)4, bold: true);
		}
		return val4;
	}

	private Text CreateText(Transform parent, string name, string value, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 size, int fontSize, TextAnchor alignment, bool bold)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Expected O, but got Unknown
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject(name, new Type[1] { typeof(RectTransform) });
		val.transform.SetParent(parent, false);
		Text val2 = val.AddComponent<Text>();
		val2.font = font;
		val2.text = RuntimeKoreanTextUtility.Clean(name, value);
		((Graphic)val2).color = RuntimeUiSkinUtility.ResolveReadableTextColor(parent, color, uiSkin);
		val2.fontSize = fontSize;
		val2.alignment = alignment;
		val2.fontStyle = (FontStyle)(bold ? 1 : 0);
		((Graphic)val2).raycastTarget = false;
		RectTransform component = ((Component)val2).GetComponent<RectTransform>();
		component.anchorMin = anchorMin;
		component.anchorMax = anchorMax;
		component.pivot = pivot;
		component.anchoredPosition = anchoredPosition;
		component.sizeDelta = size;
		Shadow val3 = val.AddComponent<Shadow>();
		val3.effectColor = new Color(0f, 0f, 0f, 0.36f);
		val3.effectDistance = new Vector2(2f, -2f);
		return val2;
	}

	private Sprite GetRoundedSprite()
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Expected O, but got Unknown
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)roundedSprite != (Object)null)
		{
			return roundedSprite;
		}
		int num = 64;
		float num2 = 18f;
		Texture2D val = new Texture2D(num, num, (TextureFormat)5, false);
		((Texture)val).wrapMode = (TextureWrapMode)1;
		Color[] array = (Color[])(object)new Color[num * num];
		for (int i = 0; i < num; i++)
		{
			for (int j = 0; j < num; j++)
			{
				float num3 = Mathf.Clamp((float)j, num2, (float)num - num2 - 1f);
				float num4 = Mathf.Clamp((float)i, num2, (float)num - num2 - 1f);
				float num5 = Vector2.Distance(new Vector2((float)j, (float)i), new Vector2(num3, num4));
				float num6 = Mathf.Clamp01(num2 + 0.5f - num5);
				array[i * num + j] = new Color(1f, 1f, 1f, num6);
			}
		}
		val.SetPixels(array);
		val.Apply();
		roundedSprite = Sprite.Create(val, new Rect(0f, 0f, (float)num, (float)num), new Vector2(0.5f, 0.5f), 100f, 0u, (SpriteMeshType)0, new Vector4(num2, num2, num2, num2));
		return roundedSprite;
	}
}
