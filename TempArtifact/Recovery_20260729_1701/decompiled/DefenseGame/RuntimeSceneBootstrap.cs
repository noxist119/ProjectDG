using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DefenseGame;

public class RuntimeSceneBootstrap : MonoBehaviour
{
	private const float DefaultSlotLineZ = -8.45f;

	private const float DefaultGoalLineZ = -9.75f;

	[SerializeField]
	private bool buildOnStart = true;

	[SerializeField]
	private int slotCount = 10;

	[SerializeField]
	private int frontSlotCount = 5;

	[SerializeField]
	private float backSlotZOffset = -0.34f;

	[SerializeField]
	private float frontSlotZOffset = 1.28f;

	[SerializeField]
	private int laneCount = 4;

	[SerializeField]
	private Vector3 boardCenter = new Vector3(0f, 0f, -8.45f);

	[SerializeField]
	private Vector3 spawnCenter = new Vector3(0f, 0f, 8f);

	[SerializeField]
	private float laneSpacing = 3.2f;

	[SerializeField]
	private float slotSpacing = 1.2f;

	[SerializeField]
	private GamePresentationConfig presentationConfig;

	[SerializeField]
	private CharacterCombatTuningConfig characterCombatTuningConfig;

	[SerializeField]
	private MonsterCombatTuningConfig monsterCombatTuningConfig;

	[SerializeField]
	private OutgameProgressionConfig outgameProgressionConfig;

	[SerializeField]
	private bool hideDefaultStageDecorWhenUsingBackground = true;

	[SerializeField]
	private bool playMainBgm = true;

	[SerializeField]
	private string mainBgmResourcePath = "Audio/MainBGM";

	[SerializeField]
	private string bossBgmResourcePath = "Audio/BossBGM";

	[SerializeField]
	[Range(0f, 1f)]
	private float mainBgmVolume = 0.55f;

	[SerializeField]
	[Range(0f, 1f)]
	private float bossBgmVolume = 0.72f;

	[SerializeField]
	private float bgmFadeDuration = 0.6f;

	private static readonly Color[] DefaultSlotColors = (Color[])(object)new Color[10]
	{
		new Color(0.3f, 0.56f, 0.93f),
		new Color(0.25f, 0.78f, 0.77f),
		new Color(0.44f, 0.82f, 0.45f),
		new Color(0.98f, 0.75f, 0.24f),
		new Color(0.95f, 0.44f, 0.38f),
		new Color(0.82f, 0.38f, 0.85f),
		new Color(0.95f, 0.56f, 0.68f),
		new Color(0.52f, 0.64f, 0.95f),
		new Color(0.42f, 0.89f, 0.62f),
		new Color(0.98f, 0.87f, 0.45f)
	};

	private static readonly Color[] DefaultLaneColors = (Color[])(object)new Color[5]
	{
		new Color(0.16f, 0.7f, 0.98f),
		new Color(0.24f, 0.88f, 0.54f),
		new Color(0.98f, 0.66f, 0.2f),
		new Color(0.94f, 0.28f, 0.43f),
		new Color(0.72f, 0.38f, 0.95f)
	};

	private static Sprite roundedPanelSprite;

	private static Sprite circleSprite;

	private void Start()
	{
		if (buildOnStart)
		{
			float realtimeSinceStartup = Time.realtimeSinceStartup;
			BuildScene();
			Debug.Log((object)("[RuntimeSceneBootstrap] Runtime stage ready in " + (Time.realtimeSinceStartup - realtimeSinceStartup).ToString("F2") + "s"));
		}
	}

	[ContextMenu("Build Runtime Stage")]
	public void BuildScene()
	{
		CharacterDatabase orAdd = GetOrAdd<CharacterDatabase>(((Component)this).gameObject);
		MonsterDatabase orAdd2 = GetOrAdd<MonsterDatabase>(((Component)this).gameObject);
		DefenseBoardManager orAdd3 = GetOrAdd<DefenseBoardManager>(((Component)this).gameObject);
		RoundManager orAdd4 = GetOrAdd<RoundManager>(((Component)this).gameObject);
		DefenseGameController orAdd5 = GetOrAdd<DefenseGameController>(((Component)this).gameObject);
		DemoInputController orAdd6 = GetOrAdd<DemoInputController>(((Component)this).gameObject);
		GameUIButtonBinder orAdd7 = GetOrAdd<GameUIButtonBinder>(((Component)this).gameObject);
		SimpleGameHUD orAdd8 = GetOrAdd<SimpleGameHUD>(((Component)this).gameObject);
		AugmentManager orAdd9 = GetOrAdd<AugmentManager>(((Component)this).gameObject);
		CharacterCollectionUI orAdd10 = GetOrAdd<CharacterCollectionUI>(((Component)this).gameObject);
		MetaFlowUI orAdd11 = GetOrAdd<MetaFlowUI>(((Component)this).gameObject);
		OutgameProgressionSystem orAdd12 = GetOrAdd<OutgameProgressionSystem>(((Component)this).gameObject);
		BoardSynergySystem orAdd13 = GetOrAdd<BoardSynergySystem>(((Component)this).gameObject);
		TacticalMissionSystem orAdd14 = GetOrAdd<TacticalMissionSystem>(((Component)this).gameObject);
		BoardTileModifierSystem orAdd15 = GetOrAdd<BoardTileModifierSystem>(((Component)this).gameObject);
		RunShopSystem orAdd16 = GetOrAdd<RunShopSystem>(((Component)this).gameObject);
		RuntimeRenderBatchingUtility.Configure(presentationConfig);
		UnitAnimatorUpdateScheduler.Configure(presentationConfig);
		AnimationEventMaterialRegistry.Configure(((Object)(object)presentationConfig != (Object)null) ? presentationConfig.animationEventMaterials : null);
		MonsterUnit.ConfigurePetrifyMaterial(((Object)(object)characterCombatTuningConfig != (Object)null) ? characterCombatTuningConfig.defaultPetrifyMaterial : null);
		orAdd.ApplyPresentationConfig(presentationConfig);
		orAdd.ApplyCombatTuningConfig(characterCombatTuningConfig);
		orAdd12.Configure(outgameProgressionConfig, orAdd);
		orAdd2.ApplyPresentationConfig(presentationConfig);
		orAdd2.ApplyCombatTuningConfig(monsterCombatTuningConfig);
		Transform val = EnsureRoot("RuntimeStageRoot");
		Transform val2 = EnsureChild(val, "BoardSlots");
		Transform val3 = EnsureChild(val, "SpawnPoints");
		Transform val4 = EnsureChild(val, "Templates");
		Transform val5 = EnsureChild(val, "Misc");
		ClearChildren(val2);
		ClearChildren(val3);
		ClearChildren(val4);
		ClearChildren(val5);
		RemoveChildIfExists(val, "LaneDecor");
		EnsureGround(val);
		EnsureBackdrop(val);
		EnsureCamera();
		EnsureLight();
		List<BoardSlot> newSlots = BuildSlots(val2);
		Transform[] newSpawnPoints = BuildSpawnPoints(val3);
		bool flag = (Object)(object)presentationConfig != (Object)null && (Object)(object)presentationConfig.backgroundPrefab != (Object)null;
		Transform newGoalPoint = BuildGoal(val5, flag && hideDefaultStageDecorWhenUsingBackground);
		if (!flag || !hideDefaultStageDecorWhenUsingBackground)
		{
			BuildCenterCrystal(val5);
			BuildFlankTowers(val5);
			BuildSkyOrnaments(val5);
		}
		Projectile projectileTemplate = BuildProjectileTemplate(val4);
		DefenderUnit defenderUnit = BuildDefenderTemplate(val4, projectileTemplate);
		MonsterUnit fallbackPrefab = BuildMonsterTemplate(val4);
		orAdd3.Configure(newSlots, defenderUnit);
		orAdd4.Configure(orAdd2, fallbackPrefab, newSpawnPoints, newGoalPoint, ((Object)(object)presentationConfig != (Object)null) ? presentationConfig.spawnPortalPrefab : null);
		orAdd5.Configure(orAdd, orAdd2, orAdd3, orAdd4, defenderUnit);
		orAdd15.Configure(orAdd5, orAdd3);
		orAdd6.Configure(orAdd5);
		orAdd7.Configure(orAdd5);
		BuildCanvas(val, orAdd8, orAdd5, orAdd3, orAdd7, orAdd9, orAdd10, orAdd11, orAdd13, orAdd14, orAdd15, orAdd16, orAdd, orAdd12);
		EnsureRuntimeBgm(orAdd5);
	}

	private void EnsureRuntimeBgm(DefenseGameController gameController)
	{
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		if (Application.isPlaying && playMainBgm)
		{
			Transform val = ((Component)this).transform.Find("BGMPlayer");
			if ((Object)(object)val == (Object)null)
			{
				val = ((Component)this).transform.Find("MainBGMPlayer");
			}
			GameObject val2 = (GameObject)(((Object)(object)val != (Object)null) ? ((object)((Component)val).gameObject) : ((object)new GameObject("BGMPlayer")));
			((Object)val2).name = "BGMPlayer";
			if ((Object)(object)val == (Object)null)
			{
				val2.transform.SetParent(((Component)this).transform, false);
			}
			RuntimeBgmController runtimeBgmController = val2.GetComponent<RuntimeBgmController>();
			if ((Object)(object)runtimeBgmController == (Object)null)
			{
				runtimeBgmController = val2.AddComponent<RuntimeBgmController>();
			}
			runtimeBgmController.Configure(gameController, mainBgmResourcePath, bossBgmResourcePath, mainBgmVolume, bossBgmVolume, bgmFadeDuration);
		}
	}

	private T GetOrAdd<T>(GameObject target) where T : Component
	{
		T val = target.GetComponent<T>();
		if ((Object)(object)val == (Object)null)
		{
			val = target.AddComponent<T>();
		}
		return val;
	}

	private Transform EnsureRoot(string name)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Expected O, but got Unknown
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		Transform val = ((Component)this).transform.Find(name);
		if ((Object)(object)val != (Object)null)
		{
			return val;
		}
		GameObject val2 = new GameObject(name);
		val2.transform.SetParent(((Component)this).transform);
		val2.transform.localPosition = Vector3.zero;
		return val2.transform;
	}

	private Transform EnsureChild(Transform parent, string name)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Expected O, but got Unknown
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		Transform val = parent.Find(name);
		if ((Object)(object)val != (Object)null)
		{
			return val;
		}
		GameObject val2 = new GameObject(name);
		val2.transform.SetParent(parent);
		val2.transform.localPosition = Vector3.zero;
		return val2.transform;
	}

	private void ClearChildren(Transform root)
	{
		for (int num = root.childCount - 1; num >= 0; num--)
		{
			SafeDestroy(((Component)root.GetChild(num)).gameObject);
		}
	}

	private void RemoveChildIfExists(Transform parent, string childName)
	{
		Transform val = (((Object)(object)parent != (Object)null) ? parent.Find(childName) : null);
		if ((Object)(object)val != (Object)null)
		{
			SafeDestroy(((Component)val).gameObject);
		}
	}

	private void EnsureGround(Transform root)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		//IL_0165: Unknown result type (might be due to invalid IL or missing references)
		//IL_0179: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b2: Unknown result type (might be due to invalid IL or missing references)
		ReplaceNamedPrimitive(root, "Ground", (PrimitiveType)4, new Vector3(0f, -0.5f, 0f), new Vector3(2f, 1f, 1.8f), GetConfigColor((GamePresentationConfig config) => config.groundColor, new Color(0.08f, 0.11f, 0.14f)));
		ReplaceNamedPrimitive(root, "BoardStrip", (PrimitiveType)3, new Vector3(0f, -0.15f, -5.5f), new Vector3(20f, 0.25f, 2.6f), GetConfigColor((GamePresentationConfig config) => config.boardStripColor, new Color(0.12f, 0.18f, 0.24f)));
		ReplaceNamedPrimitive(root, "EnemyRunway", (PrimitiveType)3, new Vector3(0f, -0.15f, 2.1f), new Vector3(20f, 0.2f, 12.5f), GetConfigColor((GamePresentationConfig config) => config.enemyRunwayColor, new Color(0.18f, 0.1f, 0.11f)));
		ReplaceNamedPrimitive(root, "MidBridge", (PrimitiveType)3, new Vector3(0f, -0.12f, -1.6f), new Vector3(20f, 0.08f, 1.2f), GetConfigColor((GamePresentationConfig config) => config.midBridgeColor, new Color(0.25f, 0.29f, 0.36f)));
	}

	private void EnsureBackdrop(Transform root)
	{
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0172: Unknown result type (might be due to invalid IL or missing references)
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		//IL_0199: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0208: Unknown result type (might be due to invalid IL or missing references)
		//IL_021c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0250: Unknown result type (might be due to invalid IL or missing references)
		//IL_0255: Unknown result type (might be due to invalid IL or missing references)
		//IL_0277: Unknown result type (might be due to invalid IL or missing references)
		//IL_028b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0292: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cf: Unknown result type (might be due to invalid IL or missing references)
		Transform val = root.Find("BackgroundOverride");
		if ((Object)(object)val != (Object)null)
		{
			SafeDestroy(((Component)val).gameObject);
		}
		if ((Object)(object)presentationConfig != (Object)null && (Object)(object)presentationConfig.backgroundPrefab != (Object)null)
		{
			GameObject val2 = Object.Instantiate<GameObject>(presentationConfig.backgroundPrefab, root);
			((Object)val2).name = "BackgroundOverride";
			val2.transform.localPosition = Vector3.zero;
			val2.transform.localRotation = Quaternion.identity;
			val2.transform.localScale = Vector3.one;
			return;
		}
		ReplaceNamedPrimitive(root, "NorthWall", (PrimitiveType)3, new Vector3(0f, 2.5f, 10.5f), new Vector3(24f, 5f, 0.5f), GetConfigColor((GamePresentationConfig config) => config.northWallColor, new Color(0.17f, 0.14f, 0.22f)));
		ReplaceNamedPrimitive(root, "SouthWall", (PrimitiveType)3, new Vector3(0f, 2f, -9.8f), new Vector3(24f, 4f, 0.5f), GetConfigColor((GamePresentationConfig config) => config.southWallColor, new Color(0.13f, 0.19f, 0.24f)));
		ReplaceNamedPrimitive(root, "LeftCliff", (PrimitiveType)3, new Vector3(-11.2f, 1.5f, 0f), new Vector3(1.2f, 3f, 21f), GetConfigColor((GamePresentationConfig config) => config.sideWallColor, new Color(0.12f, 0.14f, 0.18f)));
		ReplaceNamedPrimitive(root, "RightCliff", (PrimitiveType)3, new Vector3(11.2f, 1.5f, 0f), new Vector3(1.2f, 3f, 21f), GetConfigColor((GamePresentationConfig config) => config.sideWallColor, new Color(0.12f, 0.14f, 0.18f)));
		ReplaceNamedPrimitive(root, "LeftBanner", (PrimitiveType)3, new Vector3(-9.5f, 3.5f, -5.7f), new Vector3(1.2f, 2.8f, 0.2f), GetLaneColor(0));
		ReplaceNamedPrimitive(root, "RightBanner", (PrimitiveType)3, new Vector3(9.5f, 3.5f, -5.7f), new Vector3(1.2f, 2.8f, 0.2f), GetLaneColor(3));
	}

	private void EnsureCamera()
	{
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Expected O, but got Unknown
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		Camera val = Camera.main;
		if ((Object)(object)val == (Object)null)
		{
			GameObject val2 = new GameObject("Main Camera");
			val = val2.AddComponent<Camera>();
			val2.tag = "MainCamera";
		}
		Vector3 val3 = default(Vector3);
		((Vector3)(ref val3))._002Ector(0f, 15f, -12.4f);
		Vector3 mobilePosition = default(Vector3);
		((Vector3)(ref mobilePosition))._002Ector(0f, 17.6f, -16.1f);
		Vector3 val4 = default(Vector3);
		((Vector3)(ref val4))._002Ector(53f, 0f, 0f);
		((Component)val).transform.position = val3;
		((Component)val).transform.rotation = Quaternion.Euler(val4);
		val.backgroundColor = new Color(0.05f, 0.07f, 0.11f);
		val.clearFlags = (CameraClearFlags)2;
		val.orthographic = false;
		val.fieldOfView = 50f;
		RuntimeBattleCameraFitter runtimeBattleCameraFitter = ((Component)val).GetComponent<RuntimeBattleCameraFitter>();
		if ((Object)(object)runtimeBattleCameraFitter == (Object)null)
		{
			runtimeBattleCameraFitter = ((Component)val).gameObject.AddComponent<RuntimeBattleCameraFitter>();
		}
		runtimeBattleCameraFitter.Configure(val3, mobilePosition, val4, 50f, 60f);
	}

	private void EnsureLight()
	{
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Expected O, but got Unknown
		Light val = Object.FindObjectOfType<Light>();
		if ((Object)(object)val == (Object)null)
		{
			GameObject val2 = new GameObject("Directional Light");
			val = val2.AddComponent<Light>();
			val.type = (LightType)1;
		}
		((Component)val).transform.rotation = Quaternion.Euler(45f, -40f, 0f);
		val.intensity = 1.2f;
	}

	private List<BoardSlot> BuildSlots(Transform boardRoot)
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0141: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		List<BoardSlot> list = new List<BoardSlot>();
		int num = Mathf.Max(1, slotCount);
		float num2 = (float)(num - 1) * slotSpacing;
		Vector3 val = boardCenter;
		if (val.z > -8.45f)
		{
			val.z = -8.45f;
		}
		val.z += backSlotZOffset;
		for (int i = 0; i < num; i++)
		{
			Vector3 position = val + new Vector3((0f - num2) * 0.5f + (float)i * slotSpacing, 0f, 0f);
			list.Add(CreateRuntimeBoardSlot(boardRoot, "Slot_" + i.ToString("D2"), position, i));
		}
		int num3 = Mathf.Max(0, frontSlotCount);
		if (num3 > 0)
		{
			float num4 = slotSpacing * 1.42f;
			float num5 = (float)(num3 - 1) * num4;
			Vector3 val2 = val + new Vector3(0f, 0f, frontSlotZOffset);
			for (int j = 0; j < num3; j++)
			{
				Vector3 position2 = val2 + new Vector3((0f - num5) * 0.5f + (float)j * num4, 0f, 0f);
				list.Add(CreateRuntimeBoardSlot(boardRoot, "FrontSlot_" + j.ToString("D2"), position2, num + j));
			}
		}
		return list;
	}

	private BoardSlot CreateRuntimeBoardSlot(Transform boardRoot, string slotName, Vector3 position, int colorIndex)
	{
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Expected O, but got Unknown
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		Color slotColor = GetSlotColor(colorIndex);
		Color slotColor2 = GetSlotColor(colorIndex + 3);
		GameObject val = GameObject.CreatePrimitive((PrimitiveType)3);
		((Object)val).name = slotName;
		val.transform.SetParent(boardRoot);
		val.transform.position = position;
		val.transform.localScale = new Vector3(1.15f, 0.13f, 1.04f);
		Renderer component = val.GetComponent<Renderer>();
		if ((Object)(object)component != (Object)null)
		{
			component.sharedMaterial = CreateRuntimeMaterial(Color.Lerp(slotColor, new Color(0.05f, 0.07f, 0.13f), 0.72f));
		}
		BoardSlot boardSlot = val.AddComponent<BoardSlot>();
		GameObject val2 = new GameObject("Anchor");
		val2.transform.SetParent(val.transform, false);
		val2.transform.localPosition = new Vector3(0f, 1.38f, 0f);
		AssignPrivateField((Object)(object)boardSlot, "unitAnchor", val2.transform);
		BuildSlotVisual(val.transform, slotColor, slotColor2);
		return boardSlot;
	}

	private void BuildSlotVisual(Transform slotRoot, Color baseColor, Color trimColor)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0167: Unknown result type (might be due to invalid IL or missing references)
		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
		//IL_018e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0198: Unknown result type (might be due to invalid IL or missing references)
		//IL_019d: Unknown result type (might be due to invalid IL or missing references)
		//IL_019e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_020d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0212: Unknown result type (might be due to invalid IL or missing references)
		//IL_0228: Unknown result type (might be due to invalid IL or missing references)
		//IL_022d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0243: Unknown result type (might be due to invalid IL or missing references)
		//IL_0248: Unknown result type (might be due to invalid IL or missing references)
		//IL_025a: Unknown result type (might be due to invalid IL or missing references)
		//IL_025b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0265: Unknown result type (might be due to invalid IL or missing references)
		//IL_0286: Unknown result type (might be due to invalid IL or missing references)
		//IL_028b: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0309: Unknown result type (might be due to invalid IL or missing references)
		//IL_030a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0314: Unknown result type (might be due to invalid IL or missing references)
		//IL_0335: Unknown result type (might be due to invalid IL or missing references)
		//IL_033a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0350: Unknown result type (might be due to invalid IL or missing references)
		//IL_0355: Unknown result type (might be due to invalid IL or missing references)
		//IL_036b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0370: Unknown result type (might be due to invalid IL or missing references)
		//IL_0382: Unknown result type (might be due to invalid IL or missing references)
		//IL_0383: Unknown result type (might be due to invalid IL or missing references)
		//IL_038d: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_040b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0410: Unknown result type (might be due to invalid IL or missing references)
		//IL_0426: Unknown result type (might be due to invalid IL or missing references)
		//IL_042b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0441: Unknown result type (might be due to invalid IL or missing references)
		//IL_0446: Unknown result type (might be due to invalid IL or missing references)
		//IL_045c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0461: Unknown result type (might be due to invalid IL or missing references)
		//IL_0482: Unknown result type (might be due to invalid IL or missing references)
		//IL_0487: Unknown result type (might be due to invalid IL or missing references)
		//IL_0491: Unknown result type (might be due to invalid IL or missing references)
		//IL_0496: Unknown result type (might be due to invalid IL or missing references)
		Color color = Color.Lerp(baseColor, new Color(0.03f, 0.04f, 0.09f), 0.58f);
		Color val = Color.Lerp(trimColor, Color.white, 0.28f);
		CreateSlotPrimitive(slotRoot, "TopInset", (PrimitiveType)3, new Vector3(0f, 0.6f, 0f), new Vector3(0.78f, 0.03f, 0.72f), Color.Lerp(baseColor, Color.white, 0.18f));
		CreateSlotPrimitive(slotRoot, "FrontLip", (PrimitiveType)3, new Vector3(0f, 0.68f, -0.48f), new Vector3(0.88f, 0.04f, 0.035f), val);
		CreateSlotPrimitive(slotRoot, "BackLip", (PrimitiveType)3, new Vector3(0f, 0.62f, 0.48f), new Vector3(0.8f, 0.028f, 0.026f), color);
		CreateSlotPrimitive(slotRoot, "AuraDisc", (PrimitiveType)2, new Vector3(0f, 0.72f, 0f), new Vector3(0.44f, 0.018f, 0.44f), Color.Lerp(baseColor, Color.white, 0.05f));
		CreateSlotPrimitive(slotRoot, "SummonHalo", (PrimitiveType)2, new Vector3(0f, 0.755f, 0f), new Vector3(0.62f, 0.012f, 0.62f), Color.Lerp(trimColor, Color.white, 0.38f));
		CreateSlotPrimitive(slotRoot, "SummonCore", (PrimitiveType)0, new Vector3(0f, 0.82f, 0f), Vector3.one * 0.065f, Color.Lerp(val, Color.white, 0.3f));
		CreateSlotLine(slotRoot, "SlotBorder", val, 0.02f, new Vector3(-0.5f, 0.82f, -0.45f), new Vector3(0.5f, 0.82f, -0.45f), new Vector3(0.5f, 0.82f, 0.45f), new Vector3(-0.5f, 0.82f, 0.45f), new Vector3(-0.5f, 0.82f, -0.45f));
		CreateSlotLine(slotRoot, "SlotRune", Color.Lerp(baseColor, Color.white, 0.5f), 0.014f, new Vector3(0f, 0.86f, -0.28f), new Vector3(0.3f, 0.86f, 0f), new Vector3(0f, 0.86f, 0.28f), new Vector3(-0.3f, 0.86f, 0f), new Vector3(0f, 0.86f, -0.28f));
		CreateSlotLine(slotRoot, "SummonChevronFront", Color.Lerp(trimColor, Color.white, 0.62f), 0.018f, new Vector3(-0.2f, 0.88f, -0.2f), new Vector3(0f, 0.88f, -0.34f), new Vector3(0.2f, 0.88f, -0.2f));
		CreateSlotLine(slotRoot, "SummonChevronBack", Color.Lerp(trimColor, Color.white, 0.42f), 0.014f, new Vector3(-0.17f, 0.875f, 0.18f), new Vector3(0f, 0.875f, 0.3f), new Vector3(0.17f, 0.875f, 0.18f));
		Vector3[] array = (Vector3[])(object)new Vector3[4]
		{
			new Vector3(-0.42f, 0.82f, -0.38f),
			new Vector3(0.42f, 0.82f, -0.38f),
			new Vector3(-0.42f, 0.82f, 0.38f),
			new Vector3(0.42f, 0.82f, 0.38f)
		};
		for (int i = 0; i < array.Length; i++)
		{
			CreateSlotPrimitive(slotRoot, "CornerGem_" + i, (PrimitiveType)0, array[i], Vector3.one * 0.055f, val);
		}
	}

	private GameObject CreateSlotPrimitive(Transform parent, string name, PrimitiveType type, Vector3 localPosition, Vector3 localScale, Color color)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = GameObject.CreatePrimitive(type);
		((Object)val).name = name;
		val.transform.SetParent(parent, false);
		val.transform.localPosition = localPosition;
		val.transform.localRotation = Quaternion.identity;
		val.transform.localScale = localScale;
		Collider component = val.GetComponent<Collider>();
		if ((Object)(object)component != (Object)null)
		{
			SafeDestroy((Component)(object)component);
		}
		Renderer component2 = val.GetComponent<Renderer>();
		if ((Object)(object)component2 != (Object)null)
		{
			component2.sharedMaterial = CreateRuntimeMaterial(color);
		}
		return val;
	}

	private void CreateSlotLine(Transform parent, string name, Color color, float width, params Vector3[] points)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject(name);
		val.transform.SetParent(parent, false);
		LineRenderer val2 = val.AddComponent<LineRenderer>();
		((Renderer)val2).sharedMaterial = CreateRuntimeLineMaterial();
		val2.useWorldSpace = false;
		val2.positionCount = Mathf.Max(2, (points != null) ? points.Length : 0);
		val2.startWidth = width;
		val2.endWidth = width;
		val2.numCapVertices = 3;
		val2.numCornerVertices = 3;
		val2.startColor = color;
		val2.endColor = color;
		for (int i = 0; i < val2.positionCount; i++)
		{
			val2.SetPosition(i, (points != null && i < points.Length) ? points[i] : Vector3.zero);
		}
	}

	private Material CreateRuntimeMaterial(Color color)
	{
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Expected O, but got Unknown
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		Shader val = Shader.Find("Universal Render Pipeline/Lit");
		if ((Object)(object)val == (Object)null)
		{
			val = Shader.Find("Standard");
		}
		if ((Object)(object)val == (Object)null)
		{
			val = Shader.Find("Sprites/Default");
		}
		Material val2 = new Material(val);
		val2.color = color;
		if (val2.HasProperty("_BaseColor"))
		{
			val2.SetColor("_BaseColor", color);
		}
		return val2;
	}

	private Material CreateRuntimeLineMaterial()
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Expected O, but got Unknown
		Shader val = Shader.Find("Sprites/Default");
		if ((Object)(object)val == (Object)null)
		{
			val = Shader.Find("Unlit/Color");
		}
		return new Material(val);
	}

	private Transform[] BuildSpawnPoints(Transform spawnRoot)
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Expected O, but got Unknown
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		Transform[] array = (Transform[])(object)new Transform[laneCount];
		float num = (float)(laneCount - 1) * laneSpacing;
		for (int i = 0; i < laneCount; i++)
		{
			GameObject val = new GameObject("Spawn_" + i.ToString("D2"));
			val.transform.SetParent(spawnRoot);
			val.transform.position = spawnCenter + new Vector3((0f - num) * 0.5f + (float)i * laneSpacing, 0f, 0f);
			array[i] = val.transform;
		}
		return array;
	}

	private Transform BuildGoal(Transform miscRoot, bool logicOnly)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Expected O, but got Unknown
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Expected O, but got Unknown
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_014e: Unknown result type (might be due to invalid IL or missing references)
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_020e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0218: Unknown result type (might be due to invalid IL or missing references)
		//IL_025d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0262: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_0335: Unknown result type (might be due to invalid IL or missing references)
		//IL_0356: Unknown result type (might be due to invalid IL or missing references)
		//IL_037c: Unknown result type (might be due to invalid IL or missing references)
		if (logicOnly)
		{
			GameObject val = new GameObject("GoalPoint");
			val.transform.SetParent(miscRoot);
			val.transform.position = new Vector3(0f, 0f, -9.75f);
			return val.transform;
		}
		if ((Object)(object)presentationConfig != (Object)null && (Object)(object)presentationConfig.goalPrefab != (Object)null)
		{
			GameObject val2 = Object.Instantiate<GameObject>(presentationConfig.goalPrefab, miscRoot);
			((Object)val2).name = "GoalPoint";
			val2.transform.localPosition = new Vector3(0f, 0f, -9.75f);
			val2.transform.localRotation = Quaternion.identity;
			return val2.transform;
		}
		GameObject val3 = new GameObject("GoalPoint");
		val3.transform.SetParent(miscRoot);
		val3.transform.position = new Vector3(0f, 0f, -9.75f);
		GameObject val4 = GameObject.CreatePrimitive((PrimitiveType)3);
		((Object)val4).name = "DefenseGate";
		val4.transform.SetParent(val3.transform);
		val4.transform.localPosition = new Vector3(0f, 1.3f, 0f);
		val4.transform.localScale = new Vector3(6.5f, 2.6f, 0.6f);
		val4.GetComponent<Renderer>().material.color = GetConfigColor((GamePresentationConfig config) => config.gateColor, new Color(0.24f, 0.54f, 0.72f));
		GameObject val5 = GameObject.CreatePrimitive((PrimitiveType)0);
		((Object)val5).name = "GateCore";
		val5.transform.SetParent(val3.transform);
		val5.transform.localPosition = new Vector3(0f, 1.7f, -0.1f);
		val5.transform.localScale = Vector3.one * 1.1f;
		val5.GetComponent<Renderer>().material.color = GetConfigColor((GamePresentationConfig config) => config.gateCoreColor, new Color(0.38f, 0.89f, 1f));
		GameObject val6 = GameObject.CreatePrimitive((PrimitiveType)2);
		((Object)val6).name = "GateTowerLeft";
		val6.transform.SetParent(val3.transform);
		val6.transform.localPosition = new Vector3(-3.6f, 1.5f, 0f);
		val6.transform.localScale = new Vector3(0.6f, 1.5f, 0.6f);
		val6.GetComponent<Renderer>().material.color = new Color(0.17f, 0.44f, 0.63f);
		GameObject val7 = GameObject.CreatePrimitive((PrimitiveType)2);
		((Object)val7).name = "GateTowerRight";
		val7.transform.SetParent(val3.transform);
		val7.transform.localPosition = new Vector3(3.6f, 1.5f, 0f);
		val7.transform.localScale = new Vector3(0.6f, 1.5f, 0.6f);
		val7.GetComponent<Renderer>().material.color = new Color(0.17f, 0.44f, 0.63f);
		return val3.transform;
	}

	private void BuildCenterCrystal(Transform miscRoot)
	{
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0160: Unknown result type (might be due to invalid IL or missing references)
		//IL_0180: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a5: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)presentationConfig != (Object)null && (Object)(object)presentationConfig.centerCrystalPrefab != (Object)null)
		{
			GameObject val = Object.Instantiate<GameObject>(presentationConfig.centerCrystalPrefab, miscRoot);
			((Object)val).name = "DefenseCrystal";
			val.transform.localPosition = new Vector3(0f, 1.2f, -6.7f);
			val.transform.localRotation = Quaternion.identity;
			return;
		}
		GameObject val2 = GameObject.CreatePrimitive((PrimitiveType)1);
		((Object)val2).name = "DefenseCrystal";
		val2.transform.SetParent(miscRoot);
		val2.transform.position = new Vector3(0f, 1.2f, -6.7f);
		val2.transform.localScale = new Vector3(0.8f, 1.3f, 0.8f);
		val2.GetComponent<Renderer>().material.color = GetConfigColor((GamePresentationConfig config) => config.crystalColor, new Color(0.3f, 0.95f, 0.86f));
		GameObject val3 = GameObject.CreatePrimitive((PrimitiveType)2);
		((Object)val3).name = "CrystalRing";
		val3.transform.SetParent(val2.transform);
		val3.transform.localPosition = new Vector3(0f, -0.85f, 0f);
		val3.transform.localScale = new Vector3(1.5f, 0.06f, 1.5f);
		val3.GetComponent<Renderer>().material.color = new Color(0.18f, 0.44f, 0.59f);
	}

	private void BuildFlankTowers(Transform miscRoot)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		BuildTower(miscRoot, "WestTower", new Vector3(-7.8f, 0f, -6.2f), GetSlotColor(1), GetSlotColor(5));
		BuildTower(miscRoot, "EastTower", new Vector3(7.8f, 0f, -6.2f), GetSlotColor(2), GetSlotColor(7));
	}

	private void BuildTower(Transform parent, string name, Vector3 position, Color baseColor, Color topColor)
	{
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Expected O, but got Unknown
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_015e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0174: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)presentationConfig != (Object)null && (Object)(object)presentationConfig.flankTowerPrefab != (Object)null)
		{
			GameObject val = Object.Instantiate<GameObject>(presentationConfig.flankTowerPrefab, parent);
			((Object)val).name = name;
			val.transform.localPosition = position;
			val.transform.localRotation = Quaternion.identity;
			return;
		}
		GameObject val2 = new GameObject(name);
		val2.transform.SetParent(parent);
		val2.transform.position = position;
		GameObject val3 = GameObject.CreatePrimitive((PrimitiveType)2);
		((Object)val3).name = "Base";
		val3.transform.SetParent(val2.transform);
		val3.transform.localPosition = new Vector3(0f, 0.9f, 0f);
		val3.transform.localScale = new Vector3(0.8f, 0.9f, 0.8f);
		val3.GetComponent<Renderer>().material.color = baseColor;
		GameObject val4 = GameObject.CreatePrimitive((PrimitiveType)3);
		((Object)val4).name = "Top";
		val4.transform.SetParent(val2.transform);
		val4.transform.localPosition = new Vector3(0f, 2.25f, 0f);
		val4.transform.localScale = new Vector3(1.15f, 0.5f, 1.15f);
		val4.GetComponent<Renderer>().material.color = topColor;
		GameObject val5 = GameObject.CreatePrimitive((PrimitiveType)0);
		((Object)val5).name = "Orb";
		val5.transform.SetParent(val2.transform);
		val5.transform.localPosition = new Vector3(0f, 2.85f, 0f);
		val5.transform.localScale = Vector3.one * 0.48f;
		val5.GetComponent<Renderer>().material.color = Color.Lerp(topColor, Color.white, 0.35f);
	}

	private void BuildSkyOrnaments(Transform miscRoot)
	{
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0154: Unknown result type (might be due to invalid IL or missing references)
		//IL_015e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		for (int i = 0; i < 5; i++)
		{
			if ((Object)(object)presentationConfig != (Object)null && (Object)(object)presentationConfig.skyAccentPrefab != (Object)null)
			{
				GameObject val = Object.Instantiate<GameObject>(presentationConfig.skyAccentPrefab, miscRoot);
				((Object)val).name = "SkyOrb_" + i.ToString("D2");
				val.transform.localPosition = new Vector3(-8f + (float)i * 4f, 4.8f + (float)(i % 2) * 0.6f, 6.2f - (float)i * 1.3f);
				val.transform.localRotation = Quaternion.identity;
			}
			else
			{
				GameObject val2 = GameObject.CreatePrimitive((PrimitiveType)0);
				((Object)val2).name = "SkyOrb_" + i.ToString("D2");
				val2.transform.SetParent(miscRoot);
				val2.transform.position = new Vector3(-8f + (float)i * 4f, 4.8f + (float)(i % 2) * 0.6f, 6.2f - (float)i * 1.3f);
				val2.transform.localScale = Vector3.one * (0.35f + (float)i * 0.05f);
				val2.GetComponent<Renderer>().material.color = GetLaneColor(i) * 0.95f;
			}
		}
	}

	private Projectile BuildProjectileTemplate(Transform templateRoot)
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = CreateTemplateObject(templateRoot, ((Object)(object)presentationConfig != (Object)null) ? presentationConfig.projectilePrefab : null, (PrimitiveType)0, "ProjectileTemplate", Vector3.one * 0.25f);
		Renderer componentInChildren = val.GetComponentInChildren<Renderer>();
		if ((Object)(object)componentInChildren != (Object)null)
		{
			componentInChildren.material.color = new Color(1f, 0.85f, 0.3f);
		}
		Projectile projectile = val.GetComponent<Projectile>();
		if ((Object)(object)projectile == (Object)null)
		{
			projectile = val.AddComponent<Projectile>();
		}
		val.SetActive(false);
		return projectile;
	}

	private DefenderUnit BuildDefenderTemplate(Transform templateRoot, Projectile projectileTemplate)
	{
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Expected O, but got Unknown
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = CreateTemplateObject(templateRoot, ((Object)(object)presentationConfig != (Object)null) ? presentationConfig.defaultDefenderPrefab : null, (PrimitiveType)1, "DefenderTemplate", new Vector3(0.8f, 1f, 0.8f));
		DefenderUnit defenderUnit = val.GetComponent<DefenderUnit>();
		if ((Object)(object)defenderUnit == (Object)null)
		{
			defenderUnit = val.AddComponent<DefenderUnit>();
		}
		Transform val2 = val.transform.Find("FirePoint");
		if ((Object)(object)val2 == (Object)null)
		{
			GameObject val3 = new GameObject("FirePoint");
			val3.transform.SetParent(val.transform);
			val3.transform.localPosition = new Vector3(0f, 0.8f, 0.6f);
			val2 = val3.transform;
		}
		defenderUnit.ConfigureRuntimePieces(projectileTemplate, val2, val.GetComponentsInChildren<Renderer>(true), ((Object)(object)presentationConfig != (Object)null) ? presentationConfig.summonedDefenderPrefab : null, ((Object)(object)presentationConfig != (Object)null) ? presentationConfig.defaultMuzzleEffectPrefab : null, ((Object)(object)presentationConfig != (Object)null) ? presentationConfig.defaultHitEffectPrefab : null, ((Object)(object)presentationConfig != (Object)null) ? presentationConfig.defaultAreaEffectPrefab : null, ((Object)(object)presentationConfig != (Object)null) ? presentationConfig.defenderDeathEffectPrefab : null);
		val.SetActive(false);
		return defenderUnit;
	}

	private MonsterUnit BuildMonsterTemplate(Transform templateRoot)
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = CreateTemplateObject(templateRoot, ((Object)(object)presentationConfig != (Object)null) ? presentationConfig.defaultMonsterPrefab : null, (PrimitiveType)3, "MonsterTemplate", Vector3.one);
		MonsterUnit monsterUnit = val.GetComponent<MonsterUnit>();
		if ((Object)(object)monsterUnit == (Object)null)
		{
			monsterUnit = val.AddComponent<MonsterUnit>();
		}
		monsterUnit.ConfigureRuntimePieces(((Object)(object)presentationConfig != (Object)null) ? presentationConfig.monsterDeathEffectPrefab : null, val.GetComponentsInChildren<Renderer>(true));
		val.SetActive(false);
		return monsterUnit;
	}

	private GameObject CreateTemplateObject(Transform parent, GameObject prefab, PrimitiveType fallbackPrimitive, string name, Vector3 scale)
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		GameObject val;
		if ((Object)(object)prefab != (Object)null)
		{
			val = Object.Instantiate<GameObject>(prefab, parent);
			((Object)val).name = name;
		}
		else
		{
			val = GameObject.CreatePrimitive(fallbackPrimitive);
			((Object)val).name = name;
			val.transform.SetParent(parent);
			val.transform.localScale = scale;
		}
		val.transform.localPosition = Vector3.zero;
		val.transform.localRotation = Quaternion.identity;
		if ((Object)(object)prefab == (Object)null)
		{
			val.transform.localScale = scale;
		}
		return val;
	}

	private void BuildCanvas(Transform root, SimpleGameHUD hud, DefenseGameController gameController, DefenseBoardManager boardManager, GameUIButtonBinder binder, AugmentManager augmentManager, CharacterCollectionUI collectionUI, MetaFlowUI metaFlowUI, BoardSynergySystem synergySystem, TacticalMissionSystem missionSystem, BoardTileModifierSystem tileModifierSystem, RunShopSystem runShopSystem, CharacterDatabase characterDatabase, OutgameProgressionSystem outgameProgression)
	{
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Expected O, but got Unknown
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Unknown result type (might be due to invalid IL or missing references)
		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0218: Unknown result type (might be due to invalid IL or missing references)
		//IL_0227: Unknown result type (might be due to invalid IL or missing references)
		//IL_0236: Unknown result type (might be due to invalid IL or missing references)
		//IL_0245: Unknown result type (might be due to invalid IL or missing references)
		//IL_0264: Unknown result type (might be due to invalid IL or missing references)
		//IL_0273: Unknown result type (might be due to invalid IL or missing references)
		//IL_028c: Unknown result type (might be due to invalid IL or missing references)
		//IL_029b: Unknown result type (might be due to invalid IL or missing references)
		//IL_02aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02de: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_0306: Unknown result type (might be due to invalid IL or missing references)
		//IL_0315: Unknown result type (might be due to invalid IL or missing references)
		//IL_0324: Unknown result type (might be due to invalid IL or missing references)
		//IL_0333: Unknown result type (might be due to invalid IL or missing references)
		//IL_034a: Unknown result type (might be due to invalid IL or missing references)
		//IL_035e: Unknown result type (might be due to invalid IL or missing references)
		//IL_036d: Unknown result type (might be due to invalid IL or missing references)
		//IL_037c: Unknown result type (might be due to invalid IL or missing references)
		//IL_038b: Unknown result type (might be due to invalid IL or missing references)
		//IL_039a: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_03dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_03eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_03fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0409: Unknown result type (might be due to invalid IL or missing references)
		//IL_0418: Unknown result type (might be due to invalid IL or missing references)
		//IL_0449: Unknown result type (might be due to invalid IL or missing references)
		//IL_0458: Unknown result type (might be due to invalid IL or missing references)
		//IL_046c: Unknown result type (might be due to invalid IL or missing references)
		//IL_048f: Unknown result type (might be due to invalid IL or missing references)
		//IL_049e: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_04c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_04e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_050e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0527: Unknown result type (might be due to invalid IL or missing references)
		//IL_052c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0531: Unknown result type (might be due to invalid IL or missing references)
		//IL_0540: Unknown result type (might be due to invalid IL or missing references)
		//IL_055a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0569: Unknown result type (might be due to invalid IL or missing references)
		//IL_0582: Unknown result type (might be due to invalid IL or missing references)
		//IL_0587: Unknown result type (might be due to invalid IL or missing references)
		//IL_058c: Unknown result type (might be due to invalid IL or missing references)
		//IL_059b: Unknown result type (might be due to invalid IL or missing references)
		//IL_05cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_05d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_05eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_05f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_05f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0604: Unknown result type (might be due to invalid IL or missing references)
		//IL_0632: Unknown result type (might be due to invalid IL or missing references)
		//IL_063c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0641: Unknown result type (might be due to invalid IL or missing references)
		//IL_0650: Unknown result type (might be due to invalid IL or missing references)
		//IL_0655: Unknown result type (might be due to invalid IL or missing references)
		//IL_065a: Unknown result type (might be due to invalid IL or missing references)
		//IL_068b: Unknown result type (might be due to invalid IL or missing references)
		//IL_069a: Unknown result type (might be due to invalid IL or missing references)
		//IL_06b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_06c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_06d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_06e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0746: Unknown result type (might be due to invalid IL or missing references)
		//IL_0755: Unknown result type (might be due to invalid IL or missing references)
		//IL_0764: Unknown result type (might be due to invalid IL or missing references)
		//IL_0773: Unknown result type (might be due to invalid IL or missing references)
		//IL_0782: Unknown result type (might be due to invalid IL or missing references)
		//IL_079b: Unknown result type (might be due to invalid IL or missing references)
		//IL_07a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_07fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0805: Expected O, but got Unknown
		//IL_082a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0839: Unknown result type (might be due to invalid IL or missing references)
		//IL_0852: Unknown result type (might be due to invalid IL or missing references)
		//IL_0861: Unknown result type (might be due to invalid IL or missing references)
		//IL_0870: Unknown result type (might be due to invalid IL or missing references)
		//IL_087f: Unknown result type (might be due to invalid IL or missing references)
		//IL_08a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_08b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_08b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_08c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_08c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_08ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_0903: Unknown result type (might be due to invalid IL or missing references)
		//IL_0912: Unknown result type (might be due to invalid IL or missing references)
		//IL_092b: Unknown result type (might be due to invalid IL or missing references)
		//IL_093a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0949: Unknown result type (might be due to invalid IL or missing references)
		//IL_0958: Unknown result type (might be due to invalid IL or missing references)
		//IL_097d: Unknown result type (might be due to invalid IL or missing references)
		//IL_098c: Unknown result type (might be due to invalid IL or missing references)
		//IL_09a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_09b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_09c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_09d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_09e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_09f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a04: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a13: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a22: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a31: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a5a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a6e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a7d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a8c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a9b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0aaa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ac4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ad5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ae4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0af3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b02: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b11: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b33: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b42: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b6c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b76: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b7b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b8a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b8f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b94: Unknown result type (might be due to invalid IL or missing references)
		//IL_0bde: Unknown result type (might be due to invalid IL or missing references)
		//IL_0bf2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c01: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c10: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c1f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c2e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c5a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c67: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c76: Expected O, but got Unknown
		//IL_0c88: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c95: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ca4: Expected O, but got Unknown
		//IL_0cb6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0cc3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0cd2: Expected O, but got Unknown
		//IL_0ce4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0cf1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d00: Expected O, but got Unknown
		//IL_0d12: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d34: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d40: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d4f: Expected O, but got Unknown
		//IL_0d6a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d79: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d88: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d97: Unknown result type (might be due to invalid IL or missing references)
		//IL_0da6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0dbf: Unknown result type (might be due to invalid IL or missing references)
		//IL_0dc4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0dd1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ddd: Expected O, but got Unknown
		//IL_0df6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e05: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e1e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e2d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e3c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e4b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e72: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e7c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e81: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e90: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e95: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ea4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ef6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f05: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f1e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f2d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f3c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f4b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f72: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f86: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f8b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f9a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0fa9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0fb8: Unknown result type (might be due to invalid IL or missing references)
		//IL_100f: Unknown result type (might be due to invalid IL or missing references)
		//IL_101e: Unknown result type (might be due to invalid IL or missing references)
		//IL_1037: Unknown result type (might be due to invalid IL or missing references)
		//IL_1046: Unknown result type (might be due to invalid IL or missing references)
		//IL_1055: Unknown result type (might be due to invalid IL or missing references)
		//IL_1064: Unknown result type (might be due to invalid IL or missing references)
		//IL_1090: Unknown result type (might be due to invalid IL or missing references)
		//IL_10a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_10c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_10d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_1102: Unknown result type (might be due to invalid IL or missing references)
		//IL_1116: Unknown result type (might be due to invalid IL or missing references)
		//IL_1134: Unknown result type (might be due to invalid IL or missing references)
		//IL_1143: Unknown result type (might be due to invalid IL or missing references)
		//IL_115c: Unknown result type (might be due to invalid IL or missing references)
		//IL_116b: Unknown result type (might be due to invalid IL or missing references)
		//IL_117a: Unknown result type (might be due to invalid IL or missing references)
		//IL_1189: Unknown result type (might be due to invalid IL or missing references)
		//IL_11bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_11cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_11e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_11f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_1202: Unknown result type (might be due to invalid IL or missing references)
		//IL_1211: Unknown result type (might be due to invalid IL or missing references)
		//IL_1237: Unknown result type (might be due to invalid IL or missing references)
		//IL_124b: Unknown result type (might be due to invalid IL or missing references)
		//IL_125a: Unknown result type (might be due to invalid IL or missing references)
		//IL_1269: Unknown result type (might be due to invalid IL or missing references)
		//IL_1278: Unknown result type (might be due to invalid IL or missing references)
		//IL_1287: Unknown result type (might be due to invalid IL or missing references)
		//IL_12ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_12bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_12de: Unknown result type (might be due to invalid IL or missing references)
		//IL_12f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_1307: Unknown result type (might be due to invalid IL or missing references)
		//IL_1316: Unknown result type (might be due to invalid IL or missing references)
		//IL_1325: Unknown result type (might be due to invalid IL or missing references)
		//IL_1334: Unknown result type (might be due to invalid IL or missing references)
		//IL_1343: Unknown result type (might be due to invalid IL or missing references)
		//IL_138e: Unknown result type (might be due to invalid IL or missing references)
		//IL_13a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_13b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_13c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_13cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_13de: Unknown result type (might be due to invalid IL or missing references)
		//IL_140c: Unknown result type (might be due to invalid IL or missing references)
		//IL_1420: Unknown result type (might be due to invalid IL or missing references)
		//IL_142f: Unknown result type (might be due to invalid IL or missing references)
		//IL_143e: Unknown result type (might be due to invalid IL or missing references)
		//IL_144d: Unknown result type (might be due to invalid IL or missing references)
		//IL_145c: Unknown result type (might be due to invalid IL or missing references)
		//IL_14af: Unknown result type (might be due to invalid IL or missing references)
		//IL_14be: Unknown result type (might be due to invalid IL or missing references)
		//IL_14cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_14dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_14eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_1504: Unknown result type (might be due to invalid IL or missing references)
		//IL_1509: Unknown result type (might be due to invalid IL or missing references)
		//IL_1515: Unknown result type (might be due to invalid IL or missing references)
		//IL_1521: Expected O, but got Unknown
		//IL_1541: Unknown result type (might be due to invalid IL or missing references)
		//IL_1550: Unknown result type (might be due to invalid IL or missing references)
		//IL_155f: Unknown result type (might be due to invalid IL or missing references)
		//IL_156e: Unknown result type (might be due to invalid IL or missing references)
		//IL_157d: Unknown result type (might be due to invalid IL or missing references)
		//IL_1596: Unknown result type (might be due to invalid IL or missing references)
		//IL_159b: Unknown result type (might be due to invalid IL or missing references)
		//IL_15a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_15b3: Expected O, but got Unknown
		//IL_15d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_15e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_15f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_1600: Unknown result type (might be due to invalid IL or missing references)
		//IL_160f: Unknown result type (might be due to invalid IL or missing references)
		//IL_1628: Unknown result type (might be due to invalid IL or missing references)
		//IL_162d: Unknown result type (might be due to invalid IL or missing references)
		//IL_1639: Unknown result type (might be due to invalid IL or missing references)
		//IL_1645: Expected O, but got Unknown
		//IL_1665: Unknown result type (might be due to invalid IL or missing references)
		//IL_1674: Unknown result type (might be due to invalid IL or missing references)
		//IL_1683: Unknown result type (might be due to invalid IL or missing references)
		//IL_1692: Unknown result type (might be due to invalid IL or missing references)
		//IL_16a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_16ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_16bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_178e: Unknown result type (might be due to invalid IL or missing references)
		//IL_179d: Unknown result type (might be due to invalid IL or missing references)
		//IL_17ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_17bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_17ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_17e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_17fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_1847: Unknown result type (might be due to invalid IL or missing references)
		//IL_185e: Unknown result type (might be due to invalid IL or missing references)
		//IL_18e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_18fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_1936: Unknown result type (might be due to invalid IL or missing references)
		//IL_194a: Unknown result type (might be due to invalid IL or missing references)
		//IL_1959: Unknown result type (might be due to invalid IL or missing references)
		//IL_1968: Unknown result type (might be due to invalid IL or missing references)
		//IL_1977: Unknown result type (might be due to invalid IL or missing references)
		//IL_1986: Unknown result type (might be due to invalid IL or missing references)
		//IL_19c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_19e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_1a00: Unknown result type (might be due to invalid IL or missing references)
		//IL_1a1c: Unknown result type (might be due to invalid IL or missing references)
		//IL_1a2e: Unknown result type (might be due to invalid IL or missing references)
		//IL_1a52: Unknown result type (might be due to invalid IL or missing references)
		//IL_1a61: Unknown result type (might be due to invalid IL or missing references)
		//IL_1a70: Unknown result type (might be due to invalid IL or missing references)
		//IL_1a7f: Unknown result type (might be due to invalid IL or missing references)
		//IL_1a8e: Unknown result type (might be due to invalid IL or missing references)
		//IL_1aa7: Unknown result type (might be due to invalid IL or missing references)
		//IL_1aac: Unknown result type (might be due to invalid IL or missing references)
		//IL_1ae2: Unknown result type (might be due to invalid IL or missing references)
		//IL_1af6: Unknown result type (might be due to invalid IL or missing references)
		//IL_1b05: Unknown result type (might be due to invalid IL or missing references)
		//IL_1b14: Unknown result type (might be due to invalid IL or missing references)
		//IL_1b23: Unknown result type (might be due to invalid IL or missing references)
		//IL_1b32: Unknown result type (might be due to invalid IL or missing references)
		//IL_1b68: Unknown result type (might be due to invalid IL or missing references)
		//IL_1b77: Unknown result type (might be due to invalid IL or missing references)
		//IL_1b86: Unknown result type (might be due to invalid IL or missing references)
		//IL_1b95: Unknown result type (might be due to invalid IL or missing references)
		//IL_1ba4: Unknown result type (might be due to invalid IL or missing references)
		//IL_1bbd: Unknown result type (might be due to invalid IL or missing references)
		//IL_1bc2: Unknown result type (might be due to invalid IL or missing references)
		//IL_1bf7: Unknown result type (might be due to invalid IL or missing references)
		//IL_1c06: Unknown result type (might be due to invalid IL or missing references)
		//IL_1c15: Unknown result type (might be due to invalid IL or missing references)
		//IL_1c24: Unknown result type (might be due to invalid IL or missing references)
		//IL_1c33: Unknown result type (might be due to invalid IL or missing references)
		//IL_1c4c: Unknown result type (might be due to invalid IL or missing references)
		//IL_1c51: Unknown result type (might be due to invalid IL or missing references)
		//IL_1c86: Unknown result type (might be due to invalid IL or missing references)
		//IL_1c95: Unknown result type (might be due to invalid IL or missing references)
		//IL_1ca4: Unknown result type (might be due to invalid IL or missing references)
		//IL_1cb3: Unknown result type (might be due to invalid IL or missing references)
		//IL_1cc2: Unknown result type (might be due to invalid IL or missing references)
		//IL_1cdb: Unknown result type (might be due to invalid IL or missing references)
		//IL_1ce0: Unknown result type (might be due to invalid IL or missing references)
		//IL_1d15: Unknown result type (might be due to invalid IL or missing references)
		//IL_1d24: Unknown result type (might be due to invalid IL or missing references)
		//IL_1d33: Unknown result type (might be due to invalid IL or missing references)
		//IL_1d42: Unknown result type (might be due to invalid IL or missing references)
		//IL_1d51: Unknown result type (might be due to invalid IL or missing references)
		//IL_1d6a: Unknown result type (might be due to invalid IL or missing references)
		//IL_1d6f: Unknown result type (might be due to invalid IL or missing references)
		//IL_1dce: Unknown result type (might be due to invalid IL or missing references)
		//IL_1dd8: Expected O, but got Unknown
		//IL_1de7: Unknown result type (might be due to invalid IL or missing references)
		//IL_1df1: Expected O, but got Unknown
		//IL_1e00: Unknown result type (might be due to invalid IL or missing references)
		//IL_1e0a: Expected O, but got Unknown
		//IL_1e1d: Unknown result type (might be due to invalid IL or missing references)
		//IL_1e2c: Unknown result type (might be due to invalid IL or missing references)
		//IL_1e45: Unknown result type (might be due to invalid IL or missing references)
		//IL_1e54: Unknown result type (might be due to invalid IL or missing references)
		//IL_1e63: Unknown result type (might be due to invalid IL or missing references)
		//IL_1e72: Unknown result type (might be due to invalid IL or missing references)
		//IL_1e97: Unknown result type (might be due to invalid IL or missing references)
		//IL_1ea6: Unknown result type (might be due to invalid IL or missing references)
		//IL_1ebf: Unknown result type (might be due to invalid IL or missing references)
		//IL_1ece: Unknown result type (might be due to invalid IL or missing references)
		//IL_1edd: Unknown result type (might be due to invalid IL or missing references)
		//IL_1eec: Unknown result type (might be due to invalid IL or missing references)
		//IL_1f03: Unknown result type (might be due to invalid IL or missing references)
		//IL_1f17: Unknown result type (might be due to invalid IL or missing references)
		//IL_1f26: Unknown result type (might be due to invalid IL or missing references)
		//IL_1f35: Unknown result type (might be due to invalid IL or missing references)
		//IL_1f44: Unknown result type (might be due to invalid IL or missing references)
		//IL_1f53: Unknown result type (might be due to invalid IL or missing references)
		//IL_1f81: Unknown result type (might be due to invalid IL or missing references)
		//IL_1f95: Unknown result type (might be due to invalid IL or missing references)
		//IL_1fa4: Unknown result type (might be due to invalid IL or missing references)
		//IL_1fb3: Unknown result type (might be due to invalid IL or missing references)
		//IL_1fc2: Unknown result type (might be due to invalid IL or missing references)
		//IL_1fd1: Unknown result type (might be due to invalid IL or missing references)
		//IL_2021: Unknown result type (might be due to invalid IL or missing references)
		//IL_2030: Unknown result type (might be due to invalid IL or missing references)
		//IL_203f: Unknown result type (might be due to invalid IL or missing references)
		//IL_204e: Unknown result type (might be due to invalid IL or missing references)
		//IL_205d: Unknown result type (might be due to invalid IL or missing references)
		//IL_2076: Unknown result type (might be due to invalid IL or missing references)
		//IL_207b: Unknown result type (might be due to invalid IL or missing references)
		//IL_20bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_20cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_20de: Unknown result type (might be due to invalid IL or missing references)
		//IL_20ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_20fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_210b: Unknown result type (might be due to invalid IL or missing references)
		//IL_2145: Unknown result type (might be due to invalid IL or missing references)
		//IL_2159: Unknown result type (might be due to invalid IL or missing references)
		//IL_2168: Unknown result type (might be due to invalid IL or missing references)
		//IL_2177: Unknown result type (might be due to invalid IL or missing references)
		//IL_2186: Unknown result type (might be due to invalid IL or missing references)
		//IL_2195: Unknown result type (might be due to invalid IL or missing references)
		//IL_21c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_21d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_21e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_21f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_2204: Unknown result type (might be due to invalid IL or missing references)
		//IL_2213: Unknown result type (might be due to invalid IL or missing references)
		//IL_2241: Unknown result type (might be due to invalid IL or missing references)
		//IL_2255: Unknown result type (might be due to invalid IL or missing references)
		//IL_2264: Unknown result type (might be due to invalid IL or missing references)
		//IL_2273: Unknown result type (might be due to invalid IL or missing references)
		//IL_2282: Unknown result type (might be due to invalid IL or missing references)
		//IL_2291: Unknown result type (might be due to invalid IL or missing references)
		//IL_22bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_22d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_22e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_22f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_2300: Unknown result type (might be due to invalid IL or missing references)
		//IL_230f: Unknown result type (might be due to invalid IL or missing references)
		//IL_18ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_2474: Unknown result type (might be due to invalid IL or missing references)
		//IL_247e: Expected O, but got Unknown
		//IL_243a: Unknown result type (might be due to invalid IL or missing references)
		//IL_2444: Expected O, but got Unknown
		Transform val = root.Find("RuntimeCanvas");
		if ((Object)(object)val != (Object)null)
		{
			SafeDestroy(((Component)val).gameObject);
		}
		EnsureEventSystem();
		GameObject val2 = new GameObject("RuntimeCanvas", new Type[1] { typeof(RectTransform) });
		val2.SetActive(false);
		val2.transform.SetParent(root, false);
		Canvas val3 = val2.AddComponent<Canvas>();
		val3.renderMode = (RenderMode)0;
		CanvasScaler val4 = val2.AddComponent<CanvasScaler>();
		val4.uiScaleMode = (ScaleMode)1;
		val4.screenMatchMode = (ScreenMatchMode)1;
		val4.referenceResolution = new Vector2(1080f, 1920f);
		val4.matchWidthOrHeight = 0.84f;
		val2.AddComponent<GraphicRaycaster>();
		val2.AddComponent<RuntimeKoreanTextCleaner>();
		Transform val5 = CreateSafeAreaRoot(((Component)val3).transform);
		Transform canvasRoot = CreateSafeAreaRoot(((Component)val3).transform, "MetaFlowSafeAreaRoot");
		Font val6 = RuntimeUiSkinUtility.ResolveFont(presentationConfig);
		Color color = (((Object)(object)presentationConfig != (Object)null) ? presentationConfig.hudTextColor : Color.white);
		bool flag = !Application.isMobilePlatform;
		string text = ((!flag) ? string.Empty : (((Object)(object)presentationConfig != (Object)null && !string.IsNullOrWhiteSpace(presentationConfig.hintText)) ? presentationConfig.hintText : "Space Round | S Summon | 1-5 Merge"));
		CreatePanel(val5, "TopSafeBackdrop", new Vector2(0f, -12f), new Vector2(0f, 232f), new Color(0.03f, 0.05f, 0.17f, 0.74f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), rounded: false, shadow: true);
		CreatePanel(val5, "TopGlow", new Vector2(0f, -224f), new Vector2(0f, 8f), new Color(0.17f, 0.42f, 0.72f, 0.35f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), rounded: false, shadow: false);
		Image val7 = CreatePanel(val5, "PlayerBadge", new Vector2(28f, -28f), new Vector2(276f, 88f), new Color(0.93f, 0.74f, 0.27f, 0.96f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), rounded: true, shadow: true);
		CreatePanel(((Component)val7).transform, "PlayerIcon", new Vector2(24f, -16f), new Vector2(62f, 62f), new Color(0.66f, 0.46f, 0.14f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), rounded: true, shadow: false);
		Text playerName = CreateText(((Component)val7).transform, val6, Color.white, "PlayerName", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(104f, -14f), new Vector2(148f, 32f), "레드X", 24, (TextAnchor)3, bold: true);
		Text rank = CreateText(((Component)val7).transform, val6, new Color(0.16f, 0.22f, 0.35f), "RankText", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(104f, -50f), new Vector2(150f, 26f), "RANK 1", 18, (TextAnchor)3, bold: true);
		Text lifeLabel = null;
		Text gold = CreateCurrencyPill(val5, val6, "GoldPill", "G", new Vector2(-90f, -36f), new Vector2(190f, 68f), new Color(1f, 0.76f, 0.22f), "0");
		Image val8 = CreatePanel(val5, "LifeProgressBar", new Vector2(188f, -36f), new Vector2(330f, 68f), new Color(0.08f, 0.12f, 0.28f, 0.96f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: true);
		CreatePanel(((Component)val8).transform, "LifeProgressGlow", Vector2.zero, new Vector2(-18f, -18f), new Color(0.2f, 1f, 0.48f, 0.13f), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), rounded: true, shadow: false);
		Image val9 = CreatePanel(((Component)val8).transform, "LifeProgressTrack", Vector2.zero, new Vector2(-14f, -14f), new Color(0.035f, 0.07f, 0.15f, 1f), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), rounded: true, shadow: false);
		Mask val10 = ((Component)val9).gameObject.AddComponent<Mask>();
		val10.showMaskGraphic = true;
		Image val11 = CreatePanel(((Component)val9).transform, "Fill", Vector2.zero, Vector2.zero, new Color(0.2f, 0.9f, 0.36f, 1f), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), rounded: true, shadow: false);
		val11.type = (Type)1;
		val11.fillAmount = 1f;
		Text val12 = CreateText(((Component)val8).transform, val6, Color.white, "TopHpText", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, "HP 10/10", 28, (TextAnchor)4, bold: true);
		AddStrongTextOutline(val12);
		Image optionsMenu = CreatePanel(val5, "OptionsMenu", new Vector2(-34f, -112f), new Vector2(274f, 322f), new Color(0.06f, 0.08f, 0.24f, 0.98f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), rounded: true, shadow: true);
		Canvas val13 = ((Component)optionsMenu).gameObject.AddComponent<Canvas>();
		val13.overrideSorting = true;
		val13.sortingOrder = 200;
		((Component)optionsMenu).gameObject.AddComponent<GraphicRaycaster>();
		Text state = null;
		Text labelText;
		Button val14 = CreateButton(val5, val6, "OptionsButton", string.Empty, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-38f, -36f), new Vector2(76f, 64f), new Color(0.08f, 0.14f, 0.31f, 0.96f), Color.white, null, out labelText);
		labelText.fontSize = 19;
		labelText.resizeTextForBestFit = true;
		labelText.resizeTextMinSize = 12;
		labelText.resizeTextMaxSize = 19;
		((Behaviour)labelText).enabled = false;
		BuildHamburgerIcon(((Component)val14).transform);
		((UnityEvent)val14.onClick).AddListener((UnityAction)delegate
		{
			((Component)optionsMenu).gameObject.SetActive(!((Component)optionsMenu).gameObject.activeSelf);
		});
		((Component)optionsMenu).gameObject.SetActive(false);
		Image val15 = CreatePanel(val5, "MergeResultStrip", new Vector2(-80f, -116f), new Vector2(865f, 30f), new Color(0.1f, 0.12f, 0.3f, 0.72f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: true);
		Text mergeResult = CreateText(((Component)val15).transform, val6, new Color(1f, 0.89f, 0.36f), "MergeResultText", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, string.Empty, 17, (TextAnchor)4, bold: true);
		((Component)val15).gameObject.SetActive(false);
		Image val16 = CreatePanel(val5, "BottomCommandDock", new Vector2(0f, 0f), new Vector2(0f, 340f), new Color(0.05f, 0.06f, 0.18f, 0.86f), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), rounded: false, shadow: true);
		CreatePanel(((Component)val16).transform, "DockTopLine", new Vector2(0f, 334f), new Vector2(0f, 8f), new Color(0.37f, 0.85f, 1f, 0.42f), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), rounded: false, shadow: false);
		Text deckSummary = CreateText(val5, val6, color, "DeckSummaryText", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(42f, 300f), new Vector2(250f, 30f), "보유 유닛 0 / 0", 21, (TextAnchor)3, bold: true);
		Text capacity = CreateText(val5, val6, new Color(0.75f, 0.91f, 1f), "CapacityText", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-42f, 300f), new Vector2(204f, 30f), "0칸 남음", 19, (TextAnchor)5, bold: true);
		Text round = CreateText(val5, val6, color, "RoundText", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-214f, 294f), new Vector2(176f, 30f), "ROUND 1", 19, (TextAnchor)4, bold: true);
		Image val17 = CreateProgressBar(val5, new Vector2(130f, 294f), new Vector2(438f, 28f));
		Text val18 = CreateText(((Component)val17).transform.parent, val6, new Color(0.76f, 0.94f, 1f), "BossRoundText", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, "다음 보스 ROUND 10", 16, (TextAnchor)4, bold: true);
		val18.resizeTextForBestFit = true;
		val18.fontSize = 18;
		val18.resizeTextMinSize = 14;
		val18.resizeTextMaxSize = 18;
		AddStrongTextOutline(val18);
		Text board = CreateText(val5, val6, Color.white, "BoardText", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(396f, 294f), new Vector2(126f, 28f), "0 / 0", 17, (TextAnchor)4, bold: true);
		UltimateRecipeSelectionUI ultimateRecipeSelection = null;
		Text normalMerge = CreateGradeCard(val5, val6, CharacterGrade.Normal, new Vector2(-380f, 126f), new UnityAction(binder.OnClickMergeNormal), "재료 3개");
		Text rareMerge = CreateGradeCard(val5, val6, CharacterGrade.Rare, new Vector2(-228f, 126f), new UnityAction(binder.OnClickMergeRare), "재료 3개");
		Text epicMerge = CreateGradeCard(val5, val6, CharacterGrade.Epic, new Vector2(-76f, 126f), new UnityAction(binder.OnClickMergeEpic), "재료 3개");
		Text legendaryMerge = CreateGradeCard(val5, val6, CharacterGrade.Legendary, new Vector2(76f, 126f), new UnityAction(binder.OnClickMergeLegendary), "재료 3개");
		Text mythicMerge = CreateGradeCard(val5, val6, CharacterGrade.Mythic, new Vector2(228f, 126f), null, "초월 재료");
		Text transcendentMerge = CreateGradeCard(val5, val6, CharacterGrade.Transcendent, new Vector2(380f, 126f), (UnityAction)delegate
		{
			if ((Object)(object)ultimateRecipeSelection != (Object)null)
			{
				ultimateRecipeSelection.Open();
			}
		}, "레시피 선택");
		Text labelText2;
		Button val19 = CreateButton(val5, val6, "SummonButton", "소환", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(54f, 26f), new Vector2(226f, 88f), new Color(0.19f, 0.78f, 0.42f, 1f), Color.white, new UnityAction(binder.OnClickSummon), out labelText2);
		Image val20 = CreatePanel(((Component)val19).transform, "LuckySummonProgressBadge", new Vector2(0f, -3f), new Vector2(194f, 26f), new Color(0.07f, 0.2f, 0.17f, 0.96f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
		Text val21 = CreateText(((Component)val20).transform, val6, new Color(0.94f, 1f, 0.78f), "LuckySummonProgressText", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-12f, -4f), string.Empty, 17, (TextAnchor)4, bold: true);
		val21.resizeTextForBestFit = true;
		val21.resizeTextMinSize = 14;
		val21.resizeTextMaxSize = 17;
		((Component)val20).gameObject.SetActive(false);
		Image val22 = CreatePanel(val5, "UltimateRecipeHudPanel", new Vector2(0f, 700f), new Vector2(920f, 76f), new Color(0.05f, 0.1f, 0.28f, 0.78f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), rounded: true, shadow: true);
		Text val23 = CreateText(((Component)val22).transform, val6, new Color(0.76f, 0.94f, 1f), "UltimateRecipeHudText", new Vector2(0f, 0f), Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(18f, 0f), new Vector2(-36f, -18f), "레시피 빙고\nTOP 0/3", 20, (TextAnchor)3, bold: true);
		val23.resizeTextForBestFit = true;
		val23.fontSize = 20;
		val23.resizeTextMinSize = 16;
		val23.resizeTextMaxSize = 20;
		AddStrongTextOutline(val23);
		Image val24 = CreatePanel(val5, "BuildReadoutPanel", new Vector2(0f, 592f), new Vector2(920f, 96f), new Color(0.04f, 0.08f, 0.24f, 0.78f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), rounded: true, shadow: true);
		Text synergyInsight = CreateBuildInsightCell(((Component)val24).transform, val6, "DangerInsight", "위험", new Vector2(-292f, 0f), new Color(1f, 0.48f, 0.3f));
		Text recipeInsight = CreateBuildInsightCell(((Component)val24).transform, val6, "ActionInsight", "추천 행동", Vector2.zero, new Color(0.36f, 0.92f, 1f));
		Text tileInsight = CreateBuildInsightCell(((Component)val24).transform, val6, "DealerInsight", "핵심 딜러", new Vector2(292f, 0f), new Color(1f, 0.76f, 0.26f));
		Image val25 = CreatePanel(val5, "FateInterventionPanel", new Vector2(0f, 434f), new Vector2(1000f, 560f), new Color(0.06f, 0.05f, 0.18f, 0.98f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), rounded: true, shadow: true);
		CanvasGroup fatePanelGroup = ((Component)val25).gameObject.AddComponent<CanvasGroup>();
		CreatePanel(((Component)val25).transform, "FateAccent", new Vector2(-488f, 0f), new Vector2(12f, 516f), new Color(1f, 0.3f, 0.88f, 0.96f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), rounded: true, shadow: false);
		CreateText(((Component)val25).transform, val6, new Color(1f, 0.74f, 0.96f), "FatePanelTitle", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 228f), new Vector2(620f, 42f), "마지막 계약 · 위기 탈출 카드", 30, (TextAnchor)4, bold: true);
		Image val26 = CreateProgressBar(((Component)val25).transform, new Vector2(-350f, 148f), new Vector2(220f, 16f));
		((Graphic)val26).color = new Color(1f, 0.36f, 0.92f, 0.96f);
		Text val27 = CreateText(((Component)val25).transform, val6, Color.white, "FateGaugeText", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-350f, 120f), new Vector2(264f, 28f), "마지막 계약 1/1", 18, (TextAnchor)4, bold: true);
		val27.resizeTextForBestFit = true;
		val27.resizeTextMinSize = 16;
		val27.resizeTextMaxSize = 20;
		Text fateDebt = CreateText(((Component)val25).transform, val6, new Color(1f, 0.82f, 0.42f), "FateDebtText", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-350f, 90f), new Vector2(248f, 24f), "카드 보유 1/1", 16, (TextAnchor)4, bold: true);
		Text val28 = CreateText(((Component)val25).transform, val6, new Color(0.84f, 0.94f, 1f), "FateCostBenefitText", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(90f, 174f), new Vector2(620f, 34f), "전투는 0.1배로 흐릅니다 · 3장 중 1장 선택", 19, (TextAnchor)4, bold: true);
		val28.resizeTextForBestFit = true;
		val28.resizeTextMinSize = 16;
		val28.resizeTextMaxSize = 19;
		Text earlyRunInsight = null;
		Text labelText3;
		Button fateSurvival = CreateButton(((Component)val25).transform, val6, "FateChoiceCard0", "운명 카드\n선택 1", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-320f, -52f), new Vector2(300f, 250f), new Color(0.7f, 0.24f, 1f, 0.98f), Color.white, (UnityAction)delegate
		{
			gameController.TryActivateFateSurvival();
		}, out labelText3);
		Text labelText4;
		Button fateGradeLock = CreateButton(((Component)val25).transform, val6, "FateChoiceCard1", "운명 카드\n선택 2", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -52f), new Vector2(300f, 250f), new Color(0.18f, 0.68f, 1f, 0.96f), Color.white, (UnityAction)delegate
		{
			gameController.TryActivateFateGradeLock(CharacterGrade.Rare, 3);
		}, out labelText4);
		Text labelText5;
		Button fateNormalBan = CreateButton(((Component)val25).transform, val6, "FateChoiceCard2", "운명 카드\n선택 3", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(320f, -52f), new Vector2(300f, 250f), new Color(1f, 0.34f, 0.24f, 0.96f), Color.white, (UnityAction)delegate
		{
			gameController.TryActivateFateNormalBan(4);
		}, out labelText5);
		Text labelText6;
		Button val29 = CreateButton(((Component)val25).transform, val6, "FateUnusedHiddenCard", string.Empty, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(9999f, 0f), new Vector2(1f, 1f), new Color(0f, 0f, 0f, 0f), Color.white, null, out labelText6);
		((Component)val29).gameObject.SetActive(false);
		labelText3.fontSize = 22;
		labelText4.fontSize = 22;
		labelText5.fontSize = 22;
		labelText6.fontSize = 1;
		labelText3.resizeTextForBestFit = true;
		labelText4.resizeTextForBestFit = true;
		labelText5.resizeTextForBestFit = true;
		labelText6.resizeTextForBestFit = true;
		labelText3.resizeTextMinSize = 18;
		labelText4.resizeTextMinSize = 18;
		labelText5.resizeTextMinSize = 18;
		labelText6.resizeTextMinSize = 1;
		labelText3.resizeTextMaxSize = 22;
		labelText4.resizeTextMaxSize = 22;
		labelText5.resizeTextMaxSize = 22;
		labelText6.resizeTextMaxSize = 1;
		Text labelText7;
		Button val30 = CreateButton(val5, val6, "FatePanelReopenButton", "계약", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-42f, 356f), new Vector2(154f, 48f), new Color(0.4f, 0.21f, 0.85f, 0.98f), new Color(1f, 0.98f, 0.94f, 1f), null, out labelText7);
		labelText7.fontSize = 18;
		labelText7.resizeTextForBestFit = true;
		labelText7.resizeTextMinSize = 13;
		labelText7.resizeTextMaxSize = 18;
		RectTransform component = ((Component)val30).GetComponent<RectTransform>();
		component.sizeDelta = new Vector2(250f, 84f);
		component.anchoredPosition = new Vector2(-80f, 356f);
		labelText7.fontSize = 28;
		labelText7.resizeTextMinSize = 22;
		labelText7.resizeTextMaxSize = 28;
		Shadow component2 = ((Component)val30).GetComponent<Shadow>();
		if ((Object)(object)component2 != (Object)null)
		{
			component2.effectDistance = new Vector2(0f, -4f);
			component2.useGraphicAlpha = true;
		}
		Outline val31 = ((Component)val30).gameObject.AddComponent<Outline>();
		((Shadow)val31).effectColor = new Color(1f, 0.78f, 0.34f, 0.94f);
		((Shadow)val31).effectDistance = new Vector2(2f, -2f);
		((Shadow)val31).useGraphicAlpha = true;
		((Component)val30).gameObject.SetActive(false);
		Text val32 = CreateText(((Component)val19).transform, val6, new Color(1f, 0.9f, 0.42f), "SummonCostText", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 7f), new Vector2(0f, 22f), "10 GOLD", 18, (TextAnchor)4, bold: true);
		AddStrongTextOutline(val32);
		labelText2.fontSize = 31;
		labelText2.alignment = (TextAnchor)4;
		((Graphic)labelText2).rectTransform.anchorMin = new Vector2(0f, 0.28f);
		((Graphic)labelText2).rectTransform.anchorMax = new Vector2(1f, 0.68f);
		((Graphic)labelText2).rectTransform.pivot = new Vector2(0.5f, 0.5f);
		((Graphic)labelText2).rectTransform.anchoredPosition = new Vector2(0f, 4f);
		((Graphic)labelText2).rectTransform.sizeDelta = Vector2.zero;
		Text labelText8;
		Button val33 = CreateButton(val5, val6, "BattleButton", "전투 시작", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-4f, 26f), new Vector2(340f, 88f), new Color(0.94f, 0.32f, 0.24f, 1f), Color.white, null, out labelText8);
		labelText8.fontSize = 36;
		CreateText(((Component)optionsMenu).transform, val6, new Color(0.72f, 0.92f, 1f), "OptionsHeader", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(220f, 34f), "설정", 25, (TextAnchor)4, bold: true);
		Text soundToggleLabel;
		Button val34 = CreateButton(((Component)optionsMenu).transform, val6, "SoundToggleButton", "사운드 ON", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -82f), new Vector2(222f, 50f), new Color(0.2f, 0.7f, 0.86f, 0.95f), Color.white, null, out soundToggleLabel);
		Text volumeLabel;
		Button val35 = CreateButton(((Component)optionsMenu).transform, val6, "VolumeButton", "음량 100%", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -142f), new Vector2(222f, 50f), new Color(0.18f, 0.48f, 0.9f, 0.95f), Color.white, null, out volumeLabel);
		Text languageLabel;
		Button val36 = CreateButton(((Component)optionsMenu).transform, val6, "LanguageButton", "언어 한국어", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -202f), new Vector2(222f, 50f), new Color(0.44f, 0.36f, 0.86f, 0.95f), Color.white, null, out languageLabel);
		Text labelText9;
		Button externalLobbyButton = CreateButton(((Component)optionsMenu).transform, val6, "LobbyButton", "나가기", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -266f), new Vector2(222f, 54f), new Color(0.86f, 0.34f, 0.24f, 0.95f), Color.white, null, out labelText9);
		Button externalLoadoutButton = null;
		Button val37 = null;
		float[] optionVolumeSteps = new float[4] { 1f, 0.7f, 0.4f, 0f };
		int optionVolumeIndex = 0;
		Action refreshOptionLabels = delegate
		{
			float num = Mathf.Clamp01(AudioListener.volume);
			soundToggleLabel.text = RuntimeKoreanTextUtility.Clean("SoundToggleButton", (num <= 0.001f) ? "사운드 켜기" : "사운드 끄기");
			volumeLabel.text = RuntimeKoreanTextUtility.Clean("VolumeButton", "음량 " + Mathf.RoundToInt(num * 100f) + "%");
			languageLabel.text = RuntimeKoreanTextUtility.Clean("LanguageButton", "언어 한국어");
		};
		refreshOptionLabels();
		((UnityEvent)val34.onClick).AddListener((UnityAction)delegate
		{
			AudioListener.volume = ((AudioListener.volume <= 0.001f) ? optionVolumeSteps[Mathf.Clamp(optionVolumeIndex, 0, optionVolumeSteps.Length - 2)] : 0f);
			refreshOptionLabels();
		});
		((UnityEvent)val35.onClick).AddListener((UnityAction)delegate
		{
			optionVolumeIndex = (optionVolumeIndex + 1) % optionVolumeSteps.Length;
			AudioListener.volume = optionVolumeSteps[optionVolumeIndex];
			refreshOptionLabels();
		});
		((UnityEvent)val36.onClick).AddListener((UnityAction)delegate
		{
			refreshOptionLabels();
		});
		Image val38 = CreatePanel(val5, "SelectedUnitSellPanel", new Vector2(0f, 450f), new Vector2(820f, 84f), new Color(0.1f, 0.08f, 0.2f, 0.94f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), rounded: true, shadow: true);
		CreatePanel(((Component)val38).transform, "SellAccent", new Vector2(18f, 0f), new Vector2(10f, 58f), new Color(1f, 0.58f, 0.24f, 0.95f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), rounded: true, shadow: false);
		Text sellTitle = CreateText(((Component)val38).transform, val6, Color.white, "SellTitle", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(44f, -14f), new Vector2(-254f, 30f), "선택 유닛", 22, (TextAnchor)3, bold: true);
		Text val39 = CreateText(((Component)val38).transform, val6, new Color(0.82f, 0.92f, 1f), "SellDetail", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(44f, 16f), new Vector2(-254f, 36f), "판매가 확인 중", 17, (TextAnchor)3, bold: false);
		val39.resizeTextForBestFit = true;
		val39.resizeTextMinSize = 13;
		val39.resizeTextMaxSize = 17;
		Text labelText10;
		Button sellButton = CreateButton(((Component)val38).transform, val6, "SellSelectedUnitButton", "판매", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-24f, 0f), new Vector2(206f, 62f), new Color(0.92f, 0.38f, 0.22f, 0.98f), Color.white, null, out labelText10);
		labelText10.fontSize = 23;
		((Component)val38).gameObject.SetActive(false);
		Text val40 = CreateText(val5, val6, new Color(0.84f, 0.92f, 1f, 0.86f), "HintText", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 780f), new Vector2(860f, 32f), text, 17, (TextAnchor)4, bold: false);
		((Component)val40).gameObject.SetActive(flag);
		Text countdown = CreateText(val5, val6, new Color(1f, 0.95f, 0.58f, 0f), "CountdownText", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 35f), new Vector2(220f, 120f), string.Empty, 96, (TextAnchor)4, bold: true);
		Text roundBanner = CreateText(val5, val6, new Color(0.48f, 1f, 0.72f, 0f), "RoundBannerText", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 136f), new Vector2(620f, 70f), string.Empty, 40, (TextAnchor)4, bold: true);
		Text mergeCelebration = CreateText(val5, val6, new Color(1f, 0.92f, 0.5f, 0f), "MergeCelebrationText", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 210f), new Vector2(720f, 76f), string.Empty, 52, (TextAnchor)4, bold: true);
		Text mergeCelebrationSub = CreateText(val5, val6, new Color(1f, 0.98f, 0.9f, 0f), "MergeCelebrationSubText", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 154f), new Vector2(820f, 42f), string.Empty, 25, (TextAnchor)4, bold: true);
		BuildSynergyPanelExpanded(val5, val6, synergySystem, gameController, boardManager);
		BuildTacticalMissionPanel(val5, val5, val6, missionSystem, gameController, boardManager);
		BuildRunShopPanel(val5, val6, runShopSystem, gameController, boardManager, tileModifierSystem, augmentManager);
		BuildAugmentPanel(val5, val6, augmentManager, gameController);
		if ((Object)(object)collectionUI != (Object)null)
		{
			collectionUI.Configure(characterDatabase, outgameProgression, val6, canvasRoot, ((Object)(object)presentationConfig != (Object)null) ? presentationConfig.uiSkin : null);
		}
		if ((Object)(object)metaFlowUI != (Object)null)
		{
			metaFlowUI.Configure(gameController, binder, augmentManager, characterDatabase, outgameProgression, collectionUI, val6, canvasRoot, ((Component)val5).gameObject, val33, externalLobbyButton, externalLoadoutButton, ((Object)(object)presentationConfig != (Object)null) ? presentationConfig.uiSkin : null);
			if ((Object)(object)val37 != (Object)null)
			{
				((UnityEventBase)val37.onClick).RemoveAllListeners();
				((UnityEvent)val37.onClick).AddListener(new UnityAction(metaFlowUI.ToggleCollectionPanel));
			}
		}
		else if ((Object)(object)collectionUI != (Object)null && (Object)(object)val37 != (Object)null)
		{
			((UnityEvent)val37.onClick).AddListener(new UnityAction(collectionUI.Toggle));
		}
		CanvasGroup canvasGroup;
		Text title;
		Text subtitle;
		GameObject bossWarning = BuildBossWarningPanel(val5, val6, out canvasGroup, out title, out subtitle);
		ultimateRecipeSelection = BuildUltimateRecipeSelectionPanel(val5, val6, gameController);
		BuildLuckySummonChoicePanel(val5, val6, gameController);
		hud.Configure(gameController, gold, lifeLabel, round, board, val12, val40, mergeResult, mergeCelebration, mergeCelebrationSub, countdown, roundBanner, text, playerName, rank, state, labelText8, labelText2, val32, deckSummary, capacity, normalMerge, rareMerge, epicMerge, legendaryMerge, mythicMerge, transcendentMerge, val23, val18, synergyInsight, recipeInsight, tileInsight, null, earlyRunInsight, val27, val26, fateDebt, val28, fateGradeLock, labelText4, fateNormalBan, labelText5, val29, labelText6, fateSurvival, labelText3, ((Component)val25).gameObject, fatePanelGroup, val30, labelText7, val17, val33, val19, bossWarning, canvasGroup, title, subtitle, boardManager, ((Component)val38).gameObject, sellTitle, val39, sellButton, labelText10, val11, val21);
		val2.SetActive(true);
	}

	private LuckySummonChoiceUI BuildLuckySummonChoicePanel(Transform parent, Font font, DefenseGameController gameController)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Expected O, but got Unknown
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_0152: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_0170: Unknown result type (might be due to invalid IL or missing references)
		//IL_017f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01da: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_021d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0231: Unknown result type (might be due to invalid IL or missing references)
		//IL_0240: Unknown result type (might be due to invalid IL or missing references)
		//IL_024f: Unknown result type (might be due to invalid IL or missing references)
		//IL_025e: Unknown result type (might be due to invalid IL or missing references)
		//IL_026d: Unknown result type (might be due to invalid IL or missing references)
		//IL_029a: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_02db: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_032b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0330: Unknown result type (might be due to invalid IL or missing references)
		//IL_034b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0350: Unknown result type (might be due to invalid IL or missing references)
		//IL_036b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0370: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_03fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0401: Unknown result type (might be due to invalid IL or missing references)
		//IL_0447: Unknown result type (might be due to invalid IL or missing references)
		//IL_0463: Unknown result type (might be due to invalid IL or missing references)
		//IL_0495: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0502: Unknown result type (might be due to invalid IL or missing references)
		//IL_0511: Unknown result type (might be due to invalid IL or missing references)
		//IL_0520: Unknown result type (might be due to invalid IL or missing references)
		//IL_052f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0548: Unknown result type (might be due to invalid IL or missing references)
		//IL_054d: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject("LuckySummonChoiceOverlay", new Type[1] { typeof(RectTransform) });
		val.transform.SetParent(parent, false);
		RectTransform component = val.GetComponent<RectTransform>();
		component.anchorMin = Vector2.zero;
		component.anchorMax = Vector2.one;
		component.offsetMin = Vector2.zero;
		component.offsetMax = Vector2.zero;
		Image val2 = val.AddComponent<Image>();
		((Graphic)val2).color = new Color(0.01f, 0.03f, 0.08f, 0.82f);
		((Graphic)val2).raycastTarget = true;
		CanvasGroup val3 = val.AddComponent<CanvasGroup>();
		Image val4 = CreatePanel(val.transform, "LuckySummonChoicePanel", new Vector2(0f, 72f), new Vector2(980f, 650f), new Color(0.05f, 0.1f, 0.2f, 0.99f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), rounded: true, shadow: true);
		CreatePanel(((Component)val4).transform, "LuckyTopGlow", new Vector2(0f, -18f), new Vector2(900f, 100f), new Color(0.46f, 0.78f, 0.26f, 0.25f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
		CreatePanel(((Component)val4).transform, "LuckyTopLine", new Vector2(0f, -6f), new Vector2(870f, 8f), new Color(0.72f, 0.9f, 0.38f, 0.96f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
		Text title = CreateText(((Component)val4).transform, font, new Color(0.92f, 1f, 0.78f), "LuckySummonTitle", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -58f), new Vector2(760f, 48f), "불운을 뒤집는 행운 소환", 34, (TextAnchor)4, bold: true);
		Text instruction = CreateText(((Component)val4).transform, font, new Color(0.8f, 0.9f, 1f), "LuckySummonInstruction", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -112f), new Vector2(800f, 42f), "일반 7회 연속의 불운을 한 번의 선택으로 바꿉니다.", 21, (TextAnchor)4, bold: false);
		Button[] array = (Button[])(object)new Button[3];
		Text[] array2 = (Text[])(object)new Text[3];
		Color[] array3 = (Color[])(object)new Color[3]
		{
			new Color(0.24f, 0.72f, 0.46f, 0.98f),
			new Color(0.22f, 0.58f, 0.9f, 0.98f),
			new Color(0.82f, 0.34f, 0.68f, 0.98f)
		};
		for (int i = 0; i < array.Length; i++)
		{
			float num = (float)(i - 1) * 310f;
			array[i] = CreateButton(((Component)val4).transform, font, "LuckySummonChoice" + i, string.Empty, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(num, -34f), new Vector2(286f, 330f), array3[i], Color.white, null, out var labelText);
			labelText.fontSize = 23;
			labelText.resizeTextForBestFit = true;
			labelText.resizeTextMinSize = 16;
			labelText.resizeTextMaxSize = 23;
			((Graphic)labelText).rectTransform.offsetMin = new Vector2(16f, 18f);
			((Graphic)labelText).rectTransform.offsetMax = new Vector2(-16f, -18f);
			Outline val5 = ((Component)array[i]).gameObject.AddComponent<Outline>();
			((Shadow)val5).effectColor = new Color(0.9f, 1f, 0.64f, 0.78f);
			((Shadow)val5).effectDistance = new Vector2(2f, -2f);
			array2[i] = labelText;
		}
		Text labelText2;
		Button close = CreateButton(((Component)val4).transform, font, "LuckySummonLaterButton", "나중에", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-66f, -48f), new Vector2(112f, 50f), new Color(0.24f, 0.32f, 0.46f, 0.98f), Color.white, null, out labelText2);
		labelText2.fontSize = 19;
		LuckySummonChoiceUI luckySummonChoiceUI = val.AddComponent<LuckySummonChoiceUI>();
		luckySummonChoiceUI.Configure(gameController, val3, title, instruction, array, array2, close);
		val.SetActive(false);
		return luckySummonChoiceUI;
	}

	private UltimateRecipeSelectionUI BuildUltimateRecipeSelectionPanel(Transform parent, Font font, DefenseGameController gameController)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Expected O, but got Unknown
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
		//IL_0160: Unknown result type (might be due to invalid IL or missing references)
		//IL_0179: Unknown result type (might be due to invalid IL or missing references)
		//IL_0188: Unknown result type (might be due to invalid IL or missing references)
		//IL_0197: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0201: Unknown result type (might be due to invalid IL or missing references)
		//IL_0210: Unknown result type (might be due to invalid IL or missing references)
		//IL_021f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0235: Unknown result type (might be due to invalid IL or missing references)
		//IL_0249: Unknown result type (might be due to invalid IL or missing references)
		//IL_0258: Unknown result type (might be due to invalid IL or missing references)
		//IL_0267: Unknown result type (might be due to invalid IL or missing references)
		//IL_0276: Unknown result type (might be due to invalid IL or missing references)
		//IL_0285: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0302: Unknown result type (might be due to invalid IL or missing references)
		//IL_0334: Unknown result type (might be due to invalid IL or missing references)
		//IL_0343: Unknown result type (might be due to invalid IL or missing references)
		//IL_0352: Unknown result type (might be due to invalid IL or missing references)
		//IL_0361: Unknown result type (might be due to invalid IL or missing references)
		//IL_0370: Unknown result type (might be due to invalid IL or missing references)
		//IL_0389: Unknown result type (might be due to invalid IL or missing references)
		//IL_038e: Unknown result type (might be due to invalid IL or missing references)
		//IL_054c: Unknown result type (might be due to invalid IL or missing references)
		//IL_055b: Unknown result type (might be due to invalid IL or missing references)
		//IL_056a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0579: Unknown result type (might be due to invalid IL or missing references)
		//IL_0588: Unknown result type (might be due to invalid IL or missing references)
		//IL_05a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_05a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0414: Unknown result type (might be due to invalid IL or missing references)
		//IL_0423: Unknown result type (might be due to invalid IL or missing references)
		//IL_0432: Unknown result type (might be due to invalid IL or missing references)
		//IL_043b: Unknown result type (might be due to invalid IL or missing references)
		//IL_044a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0463: Unknown result type (might be due to invalid IL or missing references)
		//IL_0468: Unknown result type (might be due to invalid IL or missing references)
		//IL_0489: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_04eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0507: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject("UltimateRecipeSelectionOverlay", new Type[1] { typeof(RectTransform) });
		val.transform.SetParent(parent, false);
		RectTransform component = val.GetComponent<RectTransform>();
		component.anchorMin = Vector2.zero;
		component.anchorMax = Vector2.one;
		component.offsetMin = Vector2.zero;
		component.offsetMax = Vector2.zero;
		Image val2 = val.AddComponent<Image>();
		((Graphic)val2).color = new Color(0.02f, 0.02f, 0.1f, 0.76f);
		((Graphic)val2).raycastTarget = true;
		Button val3 = val.AddComponent<Button>();
		((Selectable)val3).transition = (Transition)0;
		CanvasGroup val4 = val.AddComponent<CanvasGroup>();
		Image val5 = CreatePanel(val.transform, "UltimateRecipeDrawer", new Vector2(0f, 110f), new Vector2(980f, 820f), new Color(0.06f, 0.06f, 0.22f, 0.99f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), rounded: true, shadow: true);
		Button val6 = ((Component)val5).gameObject.AddComponent<Button>();
		((Selectable)val6).transition = (Transition)0;
		CreatePanel(((Component)val5).transform, "DrawerTopGlow", new Vector2(0f, -20f), new Vector2(900f, 94f), new Color(0.72f, 0.22f, 1f, 0.28f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
		CreatePanel(((Component)val5).transform, "DrawerGoldLine", new Vector2(0f, -6f), new Vector2(860f, 8f), new Color(1f, 0.82f, 0.22f, 0.94f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
		Text header = CreateText(((Component)val5).transform, font, Color.white, "UltimateRecipeSelectionHeader", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -42f), new Vector2(720f, 46f), "초월 조합 선택", 34, (TextAnchor)4, bold: true);
		Text instruction = CreateText(((Component)val5).transform, font, new Color(0.9f, 0.9f, 1f), "UltimateRecipeSelectionInstruction", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -94f), new Vector2(760f, 30f), "전체 레시피와 부족한 재료를 언제든 확인할 수 있습니다.", 20, (TextAnchor)4, bold: false);
		Text labelText;
		Button close = CreateButton(((Component)val5).transform, font, "UltimateRecipeSelectionClose", "X", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-28f, -40f), new Vector2(58f, 58f), new Color(0.9f, 0.26f, 0.34f, 0.98f), Color.white, null, out labelText);
		Button[] array = (Button[])(object)new Button[11];
		Text[] array2 = (Text[])(object)new Text[11];
		for (int i = 0; i < 11; i++)
		{
			int num = i % 2;
			int num2 = i / 2;
			float num3 = ((num == 0) ? (-226f) : 226f);
			float num4 = -146f - (float)num2 * 91f;
			array[i] = CreateButton(((Component)val5).transform, font, "UltimateRecipeOption_" + i, string.Empty, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(num3, num4), new Vector2(430f, 80f), new Color(0.12f, 0.12f, 0.34f, 0.98f), Color.white, null, out var labelText2);
			Outline val7 = ((Component)array[i]).gameObject.AddComponent<Outline>();
			((Shadow)val7).effectColor = Color.clear;
			((Shadow)val7).effectDistance = new Vector2(3f, -3f);
			((Shadow)val7).useGraphicAlpha = false;
			labelText2.alignment = (TextAnchor)3;
			labelText2.resizeTextForBestFit = true;
			labelText2.resizeTextMinSize = 12;
			labelText2.resizeTextMaxSize = 18;
			((Graphic)labelText2).rectTransform.offsetMin = new Vector2(16f, 6f);
			((Graphic)labelText2).rectTransform.offsetMax = new Vector2(-12f, -6f);
			array2[i] = labelText2;
		}
		Text labelText3;
		Button confirm = CreateButton(((Component)val5).transform, font, "UltimateRecipeConfirmButton", "레시피를 선택하세요", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(500f, 68f), new Color(0.72f, 0.24f, 0.94f, 0.98f), Color.white, null, out labelText3);
		labelText3.fontSize = 25;
		UltimateRecipeSelectionUI ultimateRecipeSelectionUI = val.AddComponent<UltimateRecipeSelectionUI>();
		ultimateRecipeSelectionUI.Configure(gameController, ((Graphic)val5).rectTransform, val4, val3, header, instruction, array, array2, close, confirm, labelText3);
		val.SetActive(false);
		return ultimateRecipeSelectionUI;
	}

	private GameObject BuildBossWarningPanel(Transform parent, Font font, out CanvasGroup canvasGroup, out Text title, out Text subtitle)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
		//IL_0156: Unknown result type (might be due to invalid IL or missing references)
		//IL_0165: Unknown result type (might be due to invalid IL or missing references)
		//IL_0188: Unknown result type (might be due to invalid IL or missing references)
		//IL_0197: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0200: Unknown result type (might be due to invalid IL or missing references)
		//IL_020f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0228: Unknown result type (might be due to invalid IL or missing references)
		//IL_0237: Unknown result type (might be due to invalid IL or missing references)
		//IL_0246: Unknown result type (might be due to invalid IL or missing references)
		//IL_0255: Unknown result type (might be due to invalid IL or missing references)
		//IL_0278: Unknown result type (might be due to invalid IL or missing references)
		//IL_0287: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02af: Unknown result type (might be due to invalid IL or missing references)
		//IL_02be: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0305: Unknown result type (might be due to invalid IL or missing references)
		//IL_0314: Unknown result type (might be due to invalid IL or missing references)
		//IL_0323: Unknown result type (might be due to invalid IL or missing references)
		//IL_0332: Unknown result type (might be due to invalid IL or missing references)
		//IL_0341: Unknown result type (might be due to invalid IL or missing references)
		//IL_035f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0373: Unknown result type (might be due to invalid IL or missing references)
		//IL_0382: Unknown result type (might be due to invalid IL or missing references)
		//IL_0391: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_03af: Unknown result type (might be due to invalid IL or missing references)
		//IL_03dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_040e: Unknown result type (might be due to invalid IL or missing references)
		//IL_041d: Unknown result type (might be due to invalid IL or missing references)
		//IL_042c: Unknown result type (might be due to invalid IL or missing references)
		Image val = CreatePanel(parent, "BossWarningPanel", new Vector2(0f, 92f), new Vector2(790f, 230f), new Color(0.2f, 0.02f, 0.08f, 0.94f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), rounded: true, shadow: true);
		canvasGroup = ((Component)val).gameObject.AddComponent<CanvasGroup>();
		canvasGroup.alpha = 0f;
		canvasGroup.blocksRaycasts = false;
		canvasGroup.interactable = false;
		CreatePanel(((Component)val).transform, "BossWarningGlow", Vector2.zero, new Vector2(-34f, -28f), new Color(1f, 0.12f, 0.18f, 0.18f), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), rounded: true, shadow: false);
		CreatePanel(((Component)val).transform, "BossWarningTopLine", new Vector2(0f, -8f), new Vector2(700f, 12f), new Color(1f, 0.24f, 0.18f, 0.98f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
		CreatePanel(((Component)val).transform, "BossWarningBottomLine", new Vector2(0f, 8f), new Vector2(700f, 10f), new Color(1f, 0.72f, 0.24f, 0.86f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), rounded: true, shadow: false);
		CreatePanel(((Component)val).transform, "LeftDangerIcon", new Vector2(52f, 0f), new Vector2(86f, 86f), new Color(1f, 0.18f, 0.16f, 0.95f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), rounded: true, shadow: false);
		CreatePanel(((Component)val).transform, "RightDangerIcon", new Vector2(-52f, 0f), new Vector2(86f, 86f), new Color(1f, 0.18f, 0.16f, 0.95f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), rounded: true, shadow: false);
		CreateText(((Component)val).transform, font, new Color(1f, 0.78f, 0.28f), "BossWarningKicker", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -38f), new Vector2(420f, 32f), "WARNING", 27, (TextAnchor)4, bold: true);
		title = CreateText(((Component)val).transform, font, Color.white, "BossWarningTitle", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 16f), new Vector2(560f, 72f), "보스 등장!", 58, (TextAnchor)4, bold: true);
		subtitle = CreateText(((Component)val).transform, font, new Color(1f, 0.88f, 0.74f), "BossWarningSub", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 42f), new Vector2(620f, 36f), "강력한 보스가 내려옵니다", 25, (TextAnchor)4, bold: true);
		((Component)val).gameObject.SetActive(false);
		return ((Component)val).gameObject;
	}

	private void BuildAugmentPanel(Transform parent, Font font, AugmentManager augmentManager, DefenseGameController gameController)
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Expected O, but got Unknown
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		//IL_017a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
		//IL_019e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01df: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_0224: Unknown result type (might be due to invalid IL or missing references)
		//IL_0238: Unknown result type (might be due to invalid IL or missing references)
		//IL_0247: Unknown result type (might be due to invalid IL or missing references)
		//IL_0256: Unknown result type (might be due to invalid IL or missing references)
		//IL_0265: Unknown result type (might be due to invalid IL or missing references)
		//IL_0274: Unknown result type (might be due to invalid IL or missing references)
		//IL_029e: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0316: Unknown result type (might be due to invalid IL or missing references)
		//IL_0325: Unknown result type (might be due to invalid IL or missing references)
		//IL_033e: Unknown result type (might be due to invalid IL or missing references)
		//IL_034d: Unknown result type (might be due to invalid IL or missing references)
		//IL_035c: Unknown result type (might be due to invalid IL or missing references)
		//IL_036b: Unknown result type (might be due to invalid IL or missing references)
		//IL_038e: Unknown result type (might be due to invalid IL or missing references)
		//IL_039d: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_040c: Unknown result type (might be due to invalid IL or missing references)
		//IL_041b: Unknown result type (might be due to invalid IL or missing references)
		//IL_042a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0439: Unknown result type (might be due to invalid IL or missing references)
		//IL_0448: Unknown result type (might be due to invalid IL or missing references)
		//IL_0461: Unknown result type (might be due to invalid IL or missing references)
		//IL_0466: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_04c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_04e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0505: Unknown result type (might be due to invalid IL or missing references)
		//IL_051e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0523: Unknown result type (might be due to invalid IL or missing references)
		//IL_054d: Unknown result type (might be due to invalid IL or missing references)
		//IL_05d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_05df: Unknown result type (might be due to invalid IL or missing references)
		//IL_05ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_05fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0609: Unknown result type (might be due to invalid IL or missing references)
		//IL_0622: Unknown result type (might be due to invalid IL or missing references)
		//IL_0627: Unknown result type (might be due to invalid IL or missing references)
		//IL_064d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0674: Unknown result type (might be due to invalid IL or missing references)
		//IL_0683: Unknown result type (might be due to invalid IL or missing references)
		//IL_069c: Unknown result type (might be due to invalid IL or missing references)
		//IL_06ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_06ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_06c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_06f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0700: Unknown result type (might be due to invalid IL or missing references)
		//IL_0719: Unknown result type (might be due to invalid IL or missing references)
		//IL_0728: Unknown result type (might be due to invalid IL or missing references)
		//IL_0737: Unknown result type (might be due to invalid IL or missing references)
		//IL_0746: Unknown result type (might be due to invalid IL or missing references)
		//IL_0768: Unknown result type (might be due to invalid IL or missing references)
		//IL_0777: Unknown result type (might be due to invalid IL or missing references)
		//IL_0786: Unknown result type (might be due to invalid IL or missing references)
		//IL_0795: Unknown result type (might be due to invalid IL or missing references)
		//IL_07a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_07a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_07d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_07e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_07f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0802: Unknown result type (might be due to invalid IL or missing references)
		//IL_0811: Unknown result type (might be due to invalid IL or missing references)
		//IL_0820: Unknown result type (might be due to invalid IL or missing references)
		//IL_0850: Unknown result type (might be due to invalid IL or missing references)
		//IL_0864: Unknown result type (might be due to invalid IL or missing references)
		//IL_0873: Unknown result type (might be due to invalid IL or missing references)
		//IL_0882: Unknown result type (might be due to invalid IL or missing references)
		//IL_0891: Unknown result type (might be due to invalid IL or missing references)
		//IL_08a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_08d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_08e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_08f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0902: Unknown result type (might be due to invalid IL or missing references)
		//IL_0911: Unknown result type (might be due to invalid IL or missing references)
		//IL_0920: Unknown result type (might be due to invalid IL or missing references)
		//IL_094b: Unknown result type (might be due to invalid IL or missing references)
		//IL_095a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0973: Unknown result type (might be due to invalid IL or missing references)
		//IL_0982: Unknown result type (might be due to invalid IL or missing references)
		//IL_0991: Unknown result type (might be due to invalid IL or missing references)
		//IL_09a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_09b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_09ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_09d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_09e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_09f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a06: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a6f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a83: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a92: Unknown result type (might be due to invalid IL or missing references)
		//IL_0aa1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ab0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0abf: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)augmentManager == (Object)null))
		{
			GameObject val = new GameObject("AugmentChoiceOverlay", new Type[1] { typeof(RectTransform) });
			val.transform.SetParent(parent, false);
			Image val2 = val.AddComponent<Image>();
			((Graphic)val2).color = new Color(0.03f, 0.04f, 0.15f, 0.78f);
			((Graphic)val2).raycastTarget = true;
			RectTransform component = val.GetComponent<RectTransform>();
			component.anchorMin = Vector2.zero;
			component.anchorMax = Vector2.one;
			component.offsetMin = Vector2.zero;
			component.offsetMax = Vector2.zero;
			Image val3 = CreatePanel(val.transform, "AugmentModal", new Vector2(0f, 62f), new Vector2(900f, 850f), new Color(0.075f, 0.075f, 0.22f, 0.99f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), rounded: true, shadow: true);
			CreatePanel(((Component)val3).transform, "AugmentHeaderPill", new Vector2(0f, -18f), new Vector2(420f, 66f), new Color(0.48f, 0.27f, 0.88f, 0.98f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
			Text val4 = CreateText(((Component)val3).transform, font, Color.white, "AugmentHeader", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -27f), new Vector2(380f, 46f), "증강체 선택", 34, (TextAnchor)4, bold: true);
			val4.fontSize = 36;
			CreateText(((Component)val3).transform, font, new Color(0.86f, 0.89f, 1f), "AugmentSubtitle", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -94f), new Vector2(740f, 36f), "무료 보너스 1개를 반드시 선택하세요.", 22, (TextAnchor)4, bold: false);
			CreatePanel(((Component)val3).transform, "AugmentTopLine", new Vector2(0f, -122f), new Vector2(720f, 5f), new Color(0.72f, 0.42f, 1f, 0.96f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
			CreatePanel(((Component)val3).transform, "AugmentLeftRail", new Vector2(18f, -18f), new Vector2(7f, 650f), new Color(1f, 0.72f, 0.2f, 0.92f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), rounded: true, shadow: false);
			CreatePanel(((Component)val3).transform, "AugmentRightRail", new Vector2(-18f, -18f), new Vector2(7f, 650f), new Color(0.67f, 0.36f, 1f, 0.92f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), rounded: true, shadow: false);
			Text labelText;
			Button close = CreateButton(((Component)val3).transform, font, "AugmentCloseButton", "X", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-32f, -40f), new Vector2(66f, 66f), new Color(0.94f, 0.36f, 0.3f, 0.98f), Color.white, null, out labelText);
			Text component2 = ((Component)((Component)val3).transform.Find("AugmentSubtitle")).GetComponent<Text>();
			component2.fontSize = 22;
			((Graphic)component2).rectTransform.sizeDelta = new Vector2(760f, 34f);
			Text labelText2;
			Button val5 = CreateButton(parent, font, "AugmentReopenButton", "증강체", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-136f, -206f), new Vector2(180f, 62f), new Color(0.5f, 0.28f, 0.96f, 0.96f), Color.white, null, out labelText2);
			labelText2.fontSize = 28;
			((Component)val5).GetComponent<RectTransform>().sizeDelta = new Vector2(210f, 72f);
			((Component)val5).gameObject.SetActive(false);
			Button[] array = (Button[])(object)new Button[3];
			Image[] array2 = (Image[])(object)new Image[3];
			Text[] array3 = (Text[])(object)new Text[3];
			Text[] array4 = (Text[])(object)new Text[3];
			Text[] array5 = (Text[])(object)new Text[3];
			for (int i = 0; i < 3; i++)
			{
				float num = -158f - (float)i * 176f;
				Button val6 = CreateButton(((Component)val3).transform, font, "AugmentChoice_" + i, string.Empty, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, num), new Vector2(790f, 164f), new Color(0.13f, 0.15f, 0.36f, 0.99f), Color.white, null, out labelText);
				AddCardOutline(val6, new Color(0.67f, 0.38f, 1f, 0.98f), 3f);
				CreatePanel(((Component)val6).transform, "IconBadgeBack", new Vector2(26f, -32f), new Vector2(100f, 100f), new Color(1f, 0.7f, 0.18f, 0.98f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), rounded: true, shadow: false);
				array2[i] = CreatePanel(((Component)val6).transform, "IconPlate", new Vector2(38f, -44f), new Vector2(76f, 76f), new Color(0.82f, 0.48f, 1f, 0.98f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), rounded: true, shadow: false);
				CreateSkinIcon(((Component)array2[i]).transform, "AugmentIcon", "augment", Vector2.zero, new Vector2(62f, 62f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Color.white);
				array3[i] = CreateText(((Component)val6).transform, font, new Color(0.18f, 0.1f, 0.3f), "Style", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(25f, 14f), new Vector2(110f, 28f), "확정", 18, (TextAnchor)4, bold: true);
				array4[i] = CreateText(((Component)val6).transform, font, new Color(1f, 0.86f, 0.28f), "Title", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(160f, -20f), new Vector2(-270f, 42f), "Augment", 31, (TextAnchor)3, bold: true);
				array5[i] = CreateText(((Component)val6).transform, font, new Color(0.94f, 0.95f, 1f), "Description", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(160f, -66f), new Vector2(-250f, 94f), "Description", 24, (TextAnchor)0, bold: false);
				CreatePanel(((Component)val6).transform, "PickPill", new Vector2(-20f, 16f), new Vector2(92f, 34f), new Color(0.26f, 0.76f, 0.58f, 0.92f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), rounded: true, shadow: false);
				CreateText(((Component)val6).transform, font, Color.white, "PickLabel", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-28f, 18f), new Vector2(76f, 30f), "선택", 18, (TextAnchor)4, bold: true);
				Text component3 = ((Component)((Component)val6).transform.Find("PickLabel")).GetComponent<Text>();
				component3.fontSize = 18;
				array[i] = val6;
			}
			CreateText(((Component)val3).transform, font, new Color(0.72f, 0.78f, 0.96f), "AugmentFooterHint", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(720f, 34f), "카드 전체를 눌러 선택합니다.", 19, (TextAnchor)4, bold: false);
			augmentManager.Configure(gameController, val, val4, array4, array5, array, array3, array2, close, val5);
		}
	}

	private void BuildRunShopPanel(Transform parent, Font font, RunShopSystem runShopSystem, DefenseGameController gameController, DefenseBoardManager boardManager, BoardTileModifierSystem tileModifierSystem, AugmentManager augmentManager)
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Expected O, but got Unknown
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0154: Unknown result type (might be due to invalid IL or missing references)
		//IL_0163: Unknown result type (might be due to invalid IL or missing references)
		//IL_0172: Unknown result type (might be due to invalid IL or missing references)
		//IL_0181: Unknown result type (might be due to invalid IL or missing references)
		//IL_0196: Unknown result type (might be due to invalid IL or missing references)
		//IL_01aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_021c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0230: Unknown result type (might be due to invalid IL or missing references)
		//IL_023f: Unknown result type (might be due to invalid IL or missing references)
		//IL_024e: Unknown result type (might be due to invalid IL or missing references)
		//IL_025d: Unknown result type (might be due to invalid IL or missing references)
		//IL_026c: Unknown result type (might be due to invalid IL or missing references)
		//IL_029c: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0303: Unknown result type (might be due to invalid IL or missing references)
		//IL_0312: Unknown result type (might be due to invalid IL or missing references)
		//IL_0335: Unknown result type (might be due to invalid IL or missing references)
		//IL_0344: Unknown result type (might be due to invalid IL or missing references)
		//IL_035d: Unknown result type (might be due to invalid IL or missing references)
		//IL_036c: Unknown result type (might be due to invalid IL or missing references)
		//IL_037b: Unknown result type (might be due to invalid IL or missing references)
		//IL_038a: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_03bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0402: Unknown result type (might be due to invalid IL or missing references)
		//IL_0425: Unknown result type (might be due to invalid IL or missing references)
		//IL_0434: Unknown result type (might be due to invalid IL or missing references)
		//IL_044d: Unknown result type (might be due to invalid IL or missing references)
		//IL_045c: Unknown result type (might be due to invalid IL or missing references)
		//IL_046b: Unknown result type (might be due to invalid IL or missing references)
		//IL_047a: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_04c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_04df: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_04fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0523: Unknown result type (might be due to invalid IL or missing references)
		//IL_0532: Unknown result type (might be due to invalid IL or missing references)
		//IL_0541: Unknown result type (might be due to invalid IL or missing references)
		//IL_0550: Unknown result type (might be due to invalid IL or missing references)
		//IL_055f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0578: Unknown result type (might be due to invalid IL or missing references)
		//IL_057d: Unknown result type (might be due to invalid IL or missing references)
		//IL_05a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_062e: Unknown result type (might be due to invalid IL or missing references)
		//IL_063d: Unknown result type (might be due to invalid IL or missing references)
		//IL_064c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0658: Unknown result type (might be due to invalid IL or missing references)
		//IL_0667: Unknown result type (might be due to invalid IL or missing references)
		//IL_0680: Unknown result type (might be due to invalid IL or missing references)
		//IL_0685: Unknown result type (might be due to invalid IL or missing references)
		//IL_06ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_06d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_06e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_06ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_070e: Unknown result type (might be due to invalid IL or missing references)
		//IL_071d: Unknown result type (might be due to invalid IL or missing references)
		//IL_072c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0757: Unknown result type (might be due to invalid IL or missing references)
		//IL_0766: Unknown result type (might be due to invalid IL or missing references)
		//IL_077a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0789: Unknown result type (might be due to invalid IL or missing references)
		//IL_0798: Unknown result type (might be due to invalid IL or missing references)
		//IL_07a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_07c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_07d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_07e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_07f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0805: Unknown result type (might be due to invalid IL or missing references)
		//IL_080a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0825: Unknown result type (might be due to invalid IL or missing references)
		//IL_0839: Unknown result type (might be due to invalid IL or missing references)
		//IL_0848: Unknown result type (might be due to invalid IL or missing references)
		//IL_0857: Unknown result type (might be due to invalid IL or missing references)
		//IL_0866: Unknown result type (might be due to invalid IL or missing references)
		//IL_0875: Unknown result type (might be due to invalid IL or missing references)
		//IL_08a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_08bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_08cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_08da: Unknown result type (might be due to invalid IL or missing references)
		//IL_08e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_08f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0926: Unknown result type (might be due to invalid IL or missing references)
		//IL_0935: Unknown result type (might be due to invalid IL or missing references)
		//IL_094e: Unknown result type (might be due to invalid IL or missing references)
		//IL_095d: Unknown result type (might be due to invalid IL or missing references)
		//IL_096c: Unknown result type (might be due to invalid IL or missing references)
		//IL_097b: Unknown result type (might be due to invalid IL or missing references)
		//IL_09a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_09b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_09ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_09c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_09d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_09e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a17: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a21: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a26: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a35: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a44: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a53: Unknown result type (might be due to invalid IL or missing references)
		//IL_0abc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ad0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0adf: Unknown result type (might be due to invalid IL or missing references)
		//IL_0aee: Unknown result type (might be due to invalid IL or missing references)
		//IL_0afd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b0c: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)runShopSystem == (Object)null))
		{
			GameObject val = new GameObject("RunShopOverlay", new Type[1] { typeof(RectTransform) });
			val.transform.SetParent(parent, false);
			RectTransform component = val.GetComponent<RectTransform>();
			component.anchorMin = Vector2.zero;
			component.anchorMax = Vector2.one;
			component.offsetMin = Vector2.zero;
			component.offsetMax = Vector2.zero;
			Image val2 = val.AddComponent<Image>();
			((Graphic)val2).color = new Color(0.02f, 0.04f, 0.16f, 0.76f);
			Image val3 = CreatePanel(val.transform, "RunShopModal", new Vector2(0f, 62f), new Vector2(900f, 850f), new Color(0.065f, 0.11f, 0.3f, 0.99f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), rounded: true, shadow: true);
			CreatePanel(((Component)val3).transform, "RunShopHeaderPill", new Vector2(0f, -18f), new Vector2(360f, 66f), new Color(0.1f, 0.62f, 0.84f, 0.98f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
			Text val4 = CreateText(((Component)val3).transform, font, Color.white, "RunShopHeader", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -42f), new Vector2(460f, 46f), "전투 상점", 36, (TextAnchor)4, bold: true);
			val4.fontSize = 36;
			Text val5 = CreateText(((Component)val3).transform, font, new Color(0.84f, 0.92f, 1f), "RunShopSubtitle", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -94f), new Vector2(720f, 32f), "이번 판 전용 상품입니다.", 21, (TextAnchor)4, bold: false);
			val5.fontSize = 22;
			((Graphic)val5).rectTransform.sizeDelta = new Vector2(760f, 34f);
			CreatePanel(((Component)val3).transform, "RunShopTopLine", new Vector2(0f, -122f), new Vector2(720f, 5f), new Color(0.28f, 0.78f, 1f, 0.94f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
			CreatePanel(((Component)val3).transform, "RunShopBottomLine", new Vector2(0f, 78f), new Vector2(720f, 5f), new Color(0.28f, 0.78f, 1f, 0.78f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), rounded: true, shadow: false);
			CreatePanel(((Component)val3).transform, "RunShopLeftRail", new Vector2(18f, -18f), new Vector2(7f, 620f), new Color(1f, 0.62f, 0.18f, 0.92f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), rounded: true, shadow: false);
			CreatePanel(((Component)val3).transform, "RunShopRightRail", new Vector2(-18f, -18f), new Vector2(7f, 620f), new Color(0.38f, 0.86f, 1f, 0.92f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), rounded: true, shadow: false);
			Text labelText;
			Button close = CreateButton(((Component)val3).transform, font, "RunShopCloseButton", "X", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-32f, -40f), new Vector2(66f, 66f), new Color(0.94f, 0.36f, 0.3f, 0.98f), Color.white, null, out labelText);
			Text labelText2;
			Button val6 = CreateButton(parent, font, "RunShopReopenButton", "상점", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-136f, -278f), new Vector2(180f, 62f), new Color(0.14f, 0.66f, 0.92f, 0.96f), Color.white, null, out labelText2);
			labelText2.fontSize = 28;
			((Component)val6).GetComponent<RectTransform>().sizeDelta = new Vector2(210f, 72f);
			((Component)val6).gameObject.SetActive(false);
			Button[] array = (Button[])(object)new Button[3];
			Text[] array2 = (Text[])(object)new Text[3];
			Text[] array3 = (Text[])(object)new Text[3];
			Text[] array4 = (Text[])(object)new Text[3];
			Image[] array5 = (Image[])(object)new Image[3];
			for (int i = 0; i < array.Length; i++)
			{
				float num = -158f - (float)i * 176f;
				array[i] = CreateButton(((Component)val3).transform, font, "RunShopOffer_" + i, string.Empty, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, num), new Vector2(790f, 164f), new Color(0.09f, 0.16f, 0.36f, 0.99f), Color.white, null, out labelText);
				AddCardOutline(array[i], new Color(0.28f, 0.78f, 1f, 0.96f), 3f);
				CreatePanel(((Component)array[i]).transform, "RunShopIconBadgeBack", new Vector2(26f, -32f), new Vector2(100f, 100f), new Color(0.18f, 0.46f, 0.72f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), rounded: true, shadow: false);
				array5[i] = CreatePanel(((Component)array[i]).transform, "RunShopOfferAccent", new Vector2(38f, -44f), new Vector2(76f, 76f), new Color(0.38f, 0.82f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), rounded: true, shadow: false);
				CreateSkinIcon(((Component)array5[i]).transform, "RunShopOfferIcon", "shop offer", Vector2.zero, new Vector2(64f, 56f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Color.white);
				array2[i] = CreateText(((Component)array[i]).transform, font, Color.white, "Title", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(160f, -20f), new Vector2(-370f, 42f), "상품", 31, (TextAnchor)3, bold: true);
				array3[i] = CreateText(((Component)array[i]).transform, font, new Color(0.88f, 0.92f, 1f), "Description", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(160f, -66f), new Vector2(-368f, 94f), "설명", 24, (TextAnchor)0, bold: false);
				Image val7 = CreatePanel(((Component)array[i]).transform, "PriceDock", new Vector2(-18f, 0f), new Vector2(166f, 104f), new Color(0.055f, 0.09f, 0.23f, 0.98f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), rounded: true, shadow: false);
				CreateText(((Component)val7).transform, font, new Color(0.62f, 0.82f, 1f), "PriceCaption", new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(-18f, 28f), "가격", 17, (TextAnchor)4, bold: true);
				array4[i] = CreateText(((Component)val7).transform, font, new Color(1f, 0.91f, 0.38f), "Price", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, -13f), new Vector2(-14f, -38f), "0G", 30, (TextAnchor)4, bold: true);
				array4[i].resizeTextForBestFit = true;
				array4[i].resizeTextMinSize = 18;
				array4[i].resizeTextMaxSize = 30;
			}
			CreateText(((Component)val3).transform, font, new Color(0.72f, 0.84f, 1f), "RunShopFooterHint", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(720f, 34f), "구매하지 않고 닫으면 이번 상점은 지나갑니다.", 19, (TextAnchor)4, bold: false);
			val.SetActive(false);
			runShopSystem.Configure(gameController, boardManager, tileModifierSystem, augmentManager, val, val4, val5, array, array2, array3, array4, array5, close, val6);
		}
	}

	private void BuildTacticalMissionPanel(Transform canvasRoot, Transform hudRoot, Font font, TacticalMissionSystem missionSystem, DefenseGameController gameController, DefenseBoardManager boardManager)
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0159: Unknown result type (might be due to invalid IL or missing references)
		//IL_0181: Unknown result type (might be due to invalid IL or missing references)
		//IL_0190: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01df: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0202: Unknown result type (might be due to invalid IL or missing references)
		//IL_0211: Unknown result type (might be due to invalid IL or missing references)
		//IL_0256: Unknown result type (might be due to invalid IL or missing references)
		//IL_026a: Unknown result type (might be due to invalid IL or missing references)
		//IL_026f: Unknown result type (might be due to invalid IL or missing references)
		//IL_027e: Unknown result type (might be due to invalid IL or missing references)
		//IL_028d: Unknown result type (might be due to invalid IL or missing references)
		//IL_029c: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_030e: Unknown result type (might be due to invalid IL or missing references)
		//IL_031d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0336: Unknown result type (might be due to invalid IL or missing references)
		//IL_033b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0348: Unknown result type (might be due to invalid IL or missing references)
		//IL_0354: Expected O, but got Unknown
		//IL_0393: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_03cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_03fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0406: Expected O, but got Unknown
		//IL_0447: Unknown result type (might be due to invalid IL or missing references)
		//IL_044e: Expected O, but got Unknown
		//IL_047c: Unknown result type (might be due to invalid IL or missing references)
		//IL_049b: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_04c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_04da: Unknown result type (might be due to invalid IL or missing references)
		//IL_04e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0502: Unknown result type (might be due to invalid IL or missing references)
		//IL_0511: Unknown result type (might be due to invalid IL or missing references)
		//IL_0520: Unknown result type (might be due to invalid IL or missing references)
		//IL_052f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0554: Unknown result type (might be due to invalid IL or missing references)
		//IL_0563: Unknown result type (might be due to invalid IL or missing references)
		//IL_057c: Unknown result type (might be due to invalid IL or missing references)
		//IL_058b: Unknown result type (might be due to invalid IL or missing references)
		//IL_059a: Unknown result type (might be due to invalid IL or missing references)
		//IL_05a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_05bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_05d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_05e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_05f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0600: Unknown result type (might be due to invalid IL or missing references)
		//IL_060f: Unknown result type (might be due to invalid IL or missing references)
		//IL_063c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0650: Unknown result type (might be due to invalid IL or missing references)
		//IL_065f: Unknown result type (might be due to invalid IL or missing references)
		//IL_066e: Unknown result type (might be due to invalid IL or missing references)
		//IL_067d: Unknown result type (might be due to invalid IL or missing references)
		//IL_068c: Unknown result type (might be due to invalid IL or missing references)
		//IL_06bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_06cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_06db: Unknown result type (might be due to invalid IL or missing references)
		//IL_06ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_06f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0712: Unknown result type (might be due to invalid IL or missing references)
		//IL_0717: Unknown result type (might be due to invalid IL or missing references)
		//IL_073d: Unknown result type (might be due to invalid IL or missing references)
		//IL_074c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0765: Unknown result type (might be due to invalid IL or missing references)
		//IL_0774: Unknown result type (might be due to invalid IL or missing references)
		//IL_0783: Unknown result type (might be due to invalid IL or missing references)
		//IL_0792: Unknown result type (might be due to invalid IL or missing references)
		//IL_07b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_07c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_07df: Unknown result type (might be due to invalid IL or missing references)
		//IL_07ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_07fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_080c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0835: Unknown result type (might be due to invalid IL or missing references)
		//IL_0844: Unknown result type (might be due to invalid IL or missing references)
		//IL_0853: Unknown result type (might be due to invalid IL or missing references)
		//IL_0862: Unknown result type (might be due to invalid IL or missing references)
		//IL_0871: Unknown result type (might be due to invalid IL or missing references)
		//IL_0876: Unknown result type (might be due to invalid IL or missing references)
		//IL_088a: Unknown result type (might be due to invalid IL or missing references)
		//IL_089e: Unknown result type (might be due to invalid IL or missing references)
		//IL_08ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_08bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_08cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_08da: Unknown result type (might be due to invalid IL or missing references)
		//IL_0907: Unknown result type (might be due to invalid IL or missing references)
		//IL_091b: Unknown result type (might be due to invalid IL or missing references)
		//IL_092a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0939: Unknown result type (might be due to invalid IL or missing references)
		//IL_0948: Unknown result type (might be due to invalid IL or missing references)
		//IL_0957: Unknown result type (might be due to invalid IL or missing references)
		//IL_0984: Unknown result type (might be due to invalid IL or missing references)
		//IL_0998: Unknown result type (might be due to invalid IL or missing references)
		//IL_09a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_09b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_09c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_09d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a58: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a67: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a76: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a82: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a91: Unknown result type (might be due to invalid IL or missing references)
		//IL_0aaa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0aaf: Unknown result type (might be due to invalid IL or missing references)
		//IL_0adb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0aea: Unknown result type (might be due to invalid IL or missing references)
		//IL_0afe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b0d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b1c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b2b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b52: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b61: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b7a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b89: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b98: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ba7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0bd3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0be2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0bf1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c00: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c0f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c14: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c2f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c43: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c52: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c61: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c70: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c7f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0cb2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0cc6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0cd5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ce4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0cf3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d02: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d35: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d49: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d58: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d67: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d76: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d85: Unknown result type (might be due to invalid IL or missing references)
		//IL_0db4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0dc8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0dd7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0de6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0df5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e04: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e3e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e4d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e66: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e75: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e84: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e93: Unknown result type (might be due to invalid IL or missing references)
		//IL_0edb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0eea: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f03: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f08: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f0d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f1c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f40: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f4f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f68: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f77: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f86: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f95: Unknown result type (might be due to invalid IL or missing references)
		//IL_0fbe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0fcd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0fdc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0feb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ffa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0fff: Unknown result type (might be due to invalid IL or missing references)
		//IL_1013: Unknown result type (might be due to invalid IL or missing references)
		//IL_1027: Unknown result type (might be due to invalid IL or missing references)
		//IL_102c: Unknown result type (might be due to invalid IL or missing references)
		//IL_103b: Unknown result type (might be due to invalid IL or missing references)
		//IL_104a: Unknown result type (might be due to invalid IL or missing references)
		//IL_1059: Unknown result type (might be due to invalid IL or missing references)
		//IL_1086: Unknown result type (might be due to invalid IL or missing references)
		//IL_109a: Unknown result type (might be due to invalid IL or missing references)
		//IL_109f: Unknown result type (might be due to invalid IL or missing references)
		//IL_10ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_10bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_10cc: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)missionSystem == (Object)null))
		{
			Text labelText;
			Button val = CreateButton(hudRoot, font, "MissionSummaryButton", string.Empty, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(22f, -154f), new Vector2(350f, 62f), new Color(0.12f, 0.18f, 0.38f, 0.96f), Color.white, null, out labelText);
			CreatePanel(((Component)val).transform, "MissionGlow", Vector2.zero, new Vector2(-20f, -18f), new Color(1f, 0.78f, 0.25f, 0.18f), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), rounded: true, shadow: false);
			CreatePanel(((Component)val).transform, "MissionIconSlot", new Vector2(18f, 0f), new Vector2(42f, 42f), new Color(1f, 0.74f, 0.24f, 0.92f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), rounded: true, shadow: false);
			CreateSkinIcon(((Component)val).transform, "MissionIcon", "mission", new Vector2(39f, 0f), new Vector2(30f, 30f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), Color.white);
			Text val2 = CreateText(((Component)val).transform, font, Color.white, "MissionSummaryText", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(62f, 0f), new Vector2(-178f, 0f), "미션 선택", 22, (TextAnchor)3, bold: true);
			val2.resizeTextForBestFit = true;
			val2.resizeTextMinSize = 16;
			val2.resizeTextMaxSize = 22;
			Text val3 = CreateText(((Component)val).transform, font, new Color(0.82f, 0.9f, 1f), "MissionOpenHint", new Vector2(1f, 0f), Vector2.one, new Vector2(1f, 0.5f), new Vector2(-14f, 0f), new Vector2(62f, 0f), "열기", 16, (TextAnchor)4, bold: true);
			val3.resizeTextForBestFit = true;
			val3.resizeTextMinSize = 12;
			val3.resizeTextMaxSize = 16;
			Text labelText2;
			Button val4 = CreateButton(hudRoot, font, "DebugDefeatButton", "DEV 패배  [F8]", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(22f, -224f), new Vector2(194f, 48f), new Color(0.74f, 0.16f, 0.2f, 0.96f), Color.white, new UnityAction(gameController.TriggerDebugDefeat), out labelText2);
			labelText2.fontSize = 17;
			labelText2.resizeTextForBestFit = true;
			labelText2.resizeTextMinSize = 13;
			labelText2.resizeTextMaxSize = 17;
			Text labelText3;
			Button val5 = CreateButton(hudRoot, font, "DebugNextRoundButton", "DEV 다음 R  [F9]", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(224f, -224f), new Vector2(194f, 48f), new Color(0.12f, 0.42f, 0.78f, 0.96f), Color.white, new UnityAction(gameController.TriggerDebugAdvanceRound), out labelText3);
			labelText3.fontSize = 17;
			labelText3.resizeTextForBestFit = true;
			labelText3.resizeTextMinSize = 13;
			labelText3.resizeTextMaxSize = 17;
			GameObject val6 = new GameObject("TacticalMissionOverlay", new Type[1] { typeof(RectTransform) });
			val6.transform.SetParent(canvasRoot, false);
			Image val7 = val6.AddComponent<Image>();
			((Graphic)val7).color = new Color(0.03f, 0.04f, 0.15f, 0.72f);
			((Graphic)val7).raycastTarget = true;
			RectTransform component = val6.GetComponent<RectTransform>();
			component.anchorMin = Vector2.zero;
			component.anchorMax = Vector2.one;
			component.offsetMin = Vector2.zero;
			component.offsetMax = Vector2.zero;
			Image val8 = CreatePanel(val6.transform, "MissionModal", Vector2.zero, new Vector2(860f, 760f), new Color(0.13f, 0.16f, 0.4f, 0.98f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), rounded: true, shadow: true);
			CreatePanel(((Component)val8).transform, "MissionTopGlow", new Vector2(0f, -34f), new Vector2(720f, 74f), new Color(1f, 0.76f, 0.22f, 0.2f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
			Text missionPanelHeader = CreateText(((Component)val8).transform, font, Color.white, "MissionPanelHeader", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -44f), new Vector2(420f, 44f), "전략 미션 선택", 35, (TextAnchor)4, bold: true);
			CreateText(((Component)val8).transform, font, new Color(0.86f, 0.92f, 1f), "MissionPanelSubHeader", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -104f), new Vector2(720f, 30f), "욕심을 낼지, 안정적으로 갈지 선택하세요.", 22, (TextAnchor)4, bold: false);
			Button missionCloseButton = CreateButton(((Component)val8).transform, font, "MissionCloseButton", "X", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-26f, -36f), new Vector2(58f, 58f), new Color(0.94f, 0.36f, 0.3f, 0.98f), Color.white, null, out labelText);
			Image val9 = CreatePanel(((Component)val8).transform, "ActiveMissionCard", new Vector2(0f, -168f), new Vector2(740f, 220f), new Color(0.08f, 0.12f, 0.3f, 0.94f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: true);
			CreatePanel(((Component)val9).transform, "ActiveMissionIcon", new Vector2(28f, -26f), new Vector2(76f, 76f), new Color(1f, 0.76f, 0.24f, 0.9f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), rounded: true, shadow: false);
			CreateSkinIcon(((Component)val9).transform, "ActiveMissionGlyph", "mission", new Vector2(66f, -64f), new Vector2(52f, 52f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), Color.white);
			Text activeTitle = CreateText(((Component)val9).transform, font, Color.white, "ActiveMissionTitle", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(124f, -24f), new Vector2(-164f, 38f), "미션", 30, (TextAnchor)3, bold: true);
			Text activeDescription = CreateText(((Component)val9).transform, font, new Color(0.88f, 0.94f, 1f), "ActiveMissionDescription", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(124f, -72f), new Vector2(-156f, 86f), "설명", 21, (TextAnchor)0, bold: false);
			Text activeProgress = CreateText(((Component)val9).transform, font, new Color(1f, 0.9f, 0.42f), "ActiveMissionProgress", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(560f, 34f), "0 / 0", 24, (TextAnchor)4, bold: true);
			Button[] array = (Button[])(object)new Button[3];
			Text[] array2 = (Text[])(object)new Text[3];
			Text[] array3 = (Text[])(object)new Text[3];
			Text[] array4 = (Text[])(object)new Text[3];
			Image[] array5 = (Image[])(object)new Image[3];
			for (int i = 0; i < 3; i++)
			{
				float num = -156f - (float)i * 162f;
				array[i] = CreateButton(((Component)val8).transform, font, "MissionOption_" + i, string.Empty, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, num), new Vector2(740f, 144f), new Color(0.1f, 0.14f, 0.34f, 0.96f), Color.white, null, out labelText);
				array5[i] = CreatePanel(((Component)array[i]).transform, "MissionOptionIcon", new Vector2(26f, -28f), new Vector2(82f, 82f), new Color(1f, 0.76f, 0.24f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), rounded: true, shadow: false);
				CreatePanel(((Component)array[i]).transform, "MissionOptionIconCore", new Vector2(67f, -69f), new Vector2(34f, 34f), new Color(1f, 1f, 1f, 0.24f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), rounded: true, shadow: false);
				CreateSkinIcon(((Component)array[i]).transform, "MissionOptionGlyph", "mission", new Vector2(67f, -69f), new Vector2(52f, 52f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), Color.white);
				array2[i] = CreateText(((Component)array[i]).transform, font, Color.white, "Title", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(132f, -18f), new Vector2(-182f, 34f), "미션", 28, (TextAnchor)3, bold: true);
				array3[i] = CreateText(((Component)array[i]).transform, font, new Color(0.88f, 0.94f, 1f), "Description", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(132f, -56f), new Vector2(-182f, 34f), "설명", 19, (TextAnchor)0, bold: false);
				array4[i] = CreateText(((Component)array[i]).transform, font, new Color(1f, 0.88f, 0.38f), "Reward", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(132f, 8f), new Vector2(-260f, 28f), "보상", 19, (TextAnchor)3, bold: true);
				CreateText(((Component)array[i]).transform, font, new Color(0.45f, 1f, 0.68f), "PickLabel", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-26f, 0f), new Vector2(92f, 36f), "선택", 21, (TextAnchor)5, bold: true);
			}
			Image val10 = CreatePanel(hudRoot, "MissionCompletionToast", new Vector2(0f, -228f), new Vector2(620f, 126f), new Color(0.05f, 0.15f, 0.32f, 0.96f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: true);
			CanvasGroup val11 = ((Component)val10).gameObject.AddComponent<CanvasGroup>();
			val11.alpha = 0f;
			val11.interactable = false;
			val11.blocksRaycasts = false;
			CreatePanel(((Component)val10).transform, "MissionToastGlow", Vector2.zero, new Vector2(-20f, -18f), new Color(0.28f, 1f, 0.76f, 0.24f), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), rounded: true, shadow: false);
			CreatePanel(((Component)val10).transform, "MissionToastIconSlot", new Vector2(38f, 0f), new Vector2(68f, 68f), new Color(1f, 0.76f, 0.24f, 0.95f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), rounded: true, shadow: false);
			CreateSkinIcon(((Component)val10).transform, "MissionToastIcon", "mission", new Vector2(38f, 0f), new Vector2(46f, 46f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), Color.white);
			Text missionCompletionToastTitle = CreateText(((Component)val10).transform, font, Color.white, "MissionToastTitle", new Vector2(0f, 1f), Vector2.one, new Vector2(0f, 1f), new Vector2(92f, -24f), new Vector2(-120f, 42f), "미션 완료!", 31, (TextAnchor)3, bold: true);
			Text missionCompletionToastReward = CreateText(((Component)val10).transform, font, new Color(1f, 0.9f, 0.42f), "MissionToastReward", new Vector2(0f, 0f), Vector2.one, new Vector2(0f, 0f), new Vector2(92f, 22f), new Vector2(-120f, 38f), "+보상", 23, (TextAnchor)3, bold: true);
			((Component)val10).gameObject.SetActive(false);
			val6.SetActive(false);
			missionSystem.Configure(gameController, boardManager, val, val2, val6, missionPanelHeader, ((Component)val9).gameObject, activeTitle, activeDescription, activeProgress, array, array2, array3, array4, array5, missionCloseButton, ((Component)val10).gameObject, val11, missionCompletionToastTitle, missionCompletionToastReward);
		}
	}

	private void BuildSynergyPanel(Transform parent, Font font, BoardSynergySystem synergySystem, DefenseGameController gameController, DefenseBoardManager boardManager)
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0200: Unknown result type (might be due to invalid IL or missing references)
		//IL_020f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0238: Unknown result type (might be due to invalid IL or missing references)
		//IL_0247: Unknown result type (might be due to invalid IL or missing references)
		//IL_025b: Unknown result type (might be due to invalid IL or missing references)
		//IL_026a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0279: Unknown result type (might be due to invalid IL or missing references)
		//IL_0288: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0320: Unknown result type (might be due to invalid IL or missing references)
		//IL_0334: Unknown result type (might be due to invalid IL or missing references)
		//IL_0343: Unknown result type (might be due to invalid IL or missing references)
		//IL_0352: Unknown result type (might be due to invalid IL or missing references)
		//IL_0361: Unknown result type (might be due to invalid IL or missing references)
		//IL_0370: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)synergySystem == (Object)null))
		{
			Image val = CreatePanel(parent, "SynergyPanel", new Vector2(-28f, -128f), new Vector2(312f, 272f), new Color(0.08f, 0.12f, 0.28f, 0.92f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), rounded: true, shadow: true);
			CreatePanel(((Component)val).transform, "SynergyGlow", new Vector2(0f, -12f), new Vector2(260f, 44f), new Color(0.28f, 0.94f, 1f, 0.22f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
			Text header = CreateText(((Component)val).transform, font, Color.white, "SynergyHeader", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(240f, 28f), "시너지 대기", 22, (TextAnchor)4, bold: true);
			Text[] array = (Text[])(object)new Text[5];
			Text[] array2 = (Text[])(object)new Text[5];
			Image[] array3 = (Image[])(object)new Image[5];
			for (int i = 0; i < 5; i++)
			{
				float num = -58f - (float)i * 40f;
				Image val2 = CreatePanel(((Component)val).transform, "SynergyRow_" + i, new Vector2(0f, num), new Vector2(282f, 38f), new Color(0.12f, 0.16f, 0.36f, 0.82f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
				array3[i] = CreatePanel(((Component)val2).transform, "Accent", new Vector2(14f, -17f), new Vector2(10f, 22f), new Color(0.42f, 0.48f, 0.64f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), rounded: true, shadow: false);
				array[i] = CreateText(((Component)val2).transform, font, Color.white, "Title", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(32f, -3f), new Vector2(224f, 18f), string.Empty, 17, (TextAnchor)3, bold: true);
				array2[i] = CreateText(((Component)val2).transform, font, new Color(0.84f, 0.91f, 1f), "Description", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(32f, -21f), new Vector2(224f, 16f), string.Empty, 14, (TextAnchor)3, bold: false);
			}
			synergySystem.Configure(gameController, boardManager, ((Component)val).gameObject, header, array, array2, array3);
		}
	}

	private void BuildSynergyPanelExpanded(Transform parent, Font font, BoardSynergySystem synergySystem, DefenseGameController gameController, DefenseBoardManager boardManager)
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0149: Unknown result type (might be due to invalid IL or missing references)
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		//IL_0180: Unknown result type (might be due to invalid IL or missing references)
		//IL_018f: Unknown result type (might be due to invalid IL or missing references)
		//IL_019e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0206: Unknown result type (might be due to invalid IL or missing references)
		//IL_0215: Unknown result type (might be due to invalid IL or missing references)
		//IL_0224: Unknown result type (might be due to invalid IL or missing references)
		//IL_024f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0263: Unknown result type (might be due to invalid IL or missing references)
		//IL_0268: Unknown result type (might be due to invalid IL or missing references)
		//IL_0277: Unknown result type (might be due to invalid IL or missing references)
		//IL_0286: Unknown result type (might be due to invalid IL or missing references)
		//IL_0295: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0300: Unknown result type (might be due to invalid IL or missing references)
		//IL_030f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0332: Unknown result type (might be due to invalid IL or missing references)
		//IL_0341: Unknown result type (might be due to invalid IL or missing references)
		//IL_035a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0369: Unknown result type (might be due to invalid IL or missing references)
		//IL_0378: Unknown result type (might be due to invalid IL or missing references)
		//IL_0387: Unknown result type (might be due to invalid IL or missing references)
		//IL_039c: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_03bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_03dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_0417: Unknown result type (might be due to invalid IL or missing references)
		//IL_042b: Unknown result type (might be due to invalid IL or missing references)
		//IL_043a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0449: Unknown result type (might be due to invalid IL or missing references)
		//IL_0458: Unknown result type (might be due to invalid IL or missing references)
		//IL_0467: Unknown result type (might be due to invalid IL or missing references)
		//IL_0497: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_04c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0559: Unknown result type (might be due to invalid IL or missing references)
		//IL_0568: Unknown result type (might be due to invalid IL or missing references)
		//IL_0581: Unknown result type (might be due to invalid IL or missing references)
		//IL_0590: Unknown result type (might be due to invalid IL or missing references)
		//IL_059f: Unknown result type (might be due to invalid IL or missing references)
		//IL_05ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_05d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_05e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_05fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0609: Unknown result type (might be due to invalid IL or missing references)
		//IL_0618: Unknown result type (might be due to invalid IL or missing references)
		//IL_0627: Unknown result type (might be due to invalid IL or missing references)
		//IL_064b: Unknown result type (might be due to invalid IL or missing references)
		//IL_065a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0673: Unknown result type (might be due to invalid IL or missing references)
		//IL_0682: Unknown result type (might be due to invalid IL or missing references)
		//IL_0691: Unknown result type (might be due to invalid IL or missing references)
		//IL_06a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_06ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_06ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_06dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_06ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_06fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_070a: Unknown result type (might be due to invalid IL or missing references)
		//IL_073a: Unknown result type (might be due to invalid IL or missing references)
		//IL_074e: Unknown result type (might be due to invalid IL or missing references)
		//IL_075d: Unknown result type (might be due to invalid IL or missing references)
		//IL_076c: Unknown result type (might be due to invalid IL or missing references)
		//IL_077b: Unknown result type (might be due to invalid IL or missing references)
		//IL_078a: Unknown result type (might be due to invalid IL or missing references)
		//IL_07b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_07c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_07dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_07ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_07fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_080a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0829: Unknown result type (might be due to invalid IL or missing references)
		//IL_0838: Unknown result type (might be due to invalid IL or missing references)
		//IL_0851: Unknown result type (might be due to invalid IL or missing references)
		//IL_0860: Unknown result type (might be due to invalid IL or missing references)
		//IL_086f: Unknown result type (might be due to invalid IL or missing references)
		//IL_087e: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)synergySystem == (Object)null))
		{
			Text labelText;
			Button val = CreateButton(parent, font, "SynergySummaryButton", string.Empty, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-318f, -154f), new Vector2(370f, 62f), new Color(0.1f, 0.16f, 0.34f, 0.96f), Color.white, null, out labelText);
			CreatePanel(((Component)val).transform, "SummaryGlow", Vector2.zero, new Vector2(-24f, -20f), new Color(0.22f, 0.92f, 1f, 0.2f), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), rounded: true, shadow: false);
			CreatePanel(((Component)val).transform, "SummaryIconSlot", new Vector2(24f, 0f), new Vector2(50f, 50f), new Color(0.25f, 0.1f, 0.68f, 0.86f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), rounded: true, shadow: false);
			CreateSkinIcon(((Component)val).transform, "SynergyIcon", "hero", new Vector2(49f, 0f), new Vector2(34f, 34f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), Color.white);
			Text summaryLabel = CreateText(((Component)val).transform, font, Color.white, "SummaryText", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(78f, 0f), new Vector2(200f, 42f), "시너지 대기중", 24, (TextAnchor)3, bold: true);
			CreateText(((Component)val).transform, font, new Color(0.8f, 0.88f, 1f), "SummaryHint", new Vector2(1f, 0f), Vector2.one, new Vector2(1f, 0.5f), new Vector2(-20f, 0f), new Vector2(84f, 0f), "열기", 22, (TextAnchor)4, bold: true);
			Image val2 = CreatePanel(parent, "SynergyExpandedPanel", new Vector2(-318f, -224f), new Vector2(500f, 560f), new Color(0.08f, 0.12f, 0.3f, 0.97f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), rounded: true, shadow: true);
			CreatePanel(((Component)val2).transform, "ExpandedGlow", new Vector2(0f, -16f), new Vector2(420f, 66f), new Color(0.22f, 0.92f, 1f, 0.2f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
			Text expandedHeader = CreateText(((Component)val2).transform, font, Color.white, "ExpandedHeader", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -34f), new Vector2(320f, 42f), "활성 시너지", 34, (TextAnchor)4, bold: true);
			CreateText(((Component)val2).transform, font, new Color(0.8f, 0.88f, 1f), "ExpandedHint", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -82f), new Vector2(410f, 34f), "같은 역할을 모아 강한 조합을 만드세요", 21, (TextAnchor)4, bold: false);
			Button closePanelButton = CreateButton(((Component)val2).transform, font, "SynergyCloseButton", "X", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-18f, -18f), new Vector2(58f, 58f), new Color(0.94f, 0.36f, 0.3f, 0.96f), Color.white, null, out labelText);
			Text[] array = (Text[])(object)new Text[5];
			Text[] array2 = (Text[])(object)new Text[5];
			Image[] array3 = (Image[])(object)new Image[5];
			Image[] array4 = (Image[])(object)new Image[5];
			for (int i = 0; i < 5; i++)
			{
				float num = -126f - (float)i * 82f;
				Image val3 = CreatePanel(((Component)val2).transform, "SynergyExpandedRow_" + i, new Vector2(0f, num), new Vector2(454f, 74f), new Color(0.12f, 0.16f, 0.36f, 0.9f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
				array3[i] = CreatePanel(((Component)val3).transform, "AccentIconPlate", new Vector2(18f, 0f), new Vector2(54f, 54f), new Color(0.42f, 0.48f, 0.64f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), rounded: true, shadow: false);
				CreatePanel(((Component)val3).transform, "AccentCore", new Vector2(45f, 0f), new Vector2(22f, 22f), new Color(1f, 1f, 1f, 0.32f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), rounded: true, shadow: false);
				array[i] = CreateText(((Component)val3).transform, font, Color.white, "Title", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(86f, -15f), new Vector2(286f, 28f), string.Empty, 24, (TextAnchor)3, bold: true);
				array2[i] = CreateText(((Component)val3).transform, font, new Color(0.84f, 0.91f, 1f), "Description", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(86f, -45f), new Vector2(292f, 28f), string.Empty, 18, (TextAnchor)3, bold: false);
				Image val4 = CreatePanel(((Component)val3).transform, "IconSlot", new Vector2(-16f, 0f), new Vector2(58f, 58f), new Color(0.05f, 0.08f, 0.2f, 0.72f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), rounded: true, shadow: false);
				array4[i] = CreatePanel(((Component)val4).transform, "IconImage", Vector2.zero, new Vector2(38f, 38f), new Color(1f, 1f, 1f, 0.24f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), rounded: true, shadow: false);
				array4[i].preserveAspect = true;
			}
			((Component)val2).gameObject.SetActive(false);
			synergySystem.Configure(gameController, boardManager, val, summaryLabel, ((Component)val2).gameObject, expandedHeader, array, array2, array3, array4, closePanelButton);
		}
	}

	private Transform CreateSafeAreaRoot(Transform parent, string rootName = "SafeAreaRoot")
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Expected O, but got Unknown
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject(rootName, new Type[1] { typeof(RectTransform) });
		val.transform.SetParent(parent, false);
		RectTransform component = val.GetComponent<RectTransform>();
		component.anchorMin = Vector2.zero;
		component.anchorMax = Vector2.one;
		component.offsetMin = Vector2.zero;
		component.offsetMax = Vector2.zero;
		val.AddComponent<RuntimeSafeAreaFitter>();
		return val.transform;
	}

	private void EnsureEventSystem()
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Expected O, but got Unknown
		if (!((Object)(object)Object.FindObjectOfType<EventSystem>() != (Object)null))
		{
			GameObject val = new GameObject("EventSystem");
			val.AddComponent<EventSystem>();
			val.AddComponent<StandaloneInputModule>();
		}
	}

	private Text CreateCurrencyPill(Transform parent, Font font, string name, string icon, Vector2 anchoredPosition, Vector2 size, Color accentColor, string value)
	{
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
		//IL_014e: Unknown result type (might be due to invalid IL or missing references)
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_016c: Unknown result type (might be due to invalid IL or missing references)
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		//IL_018a: Unknown result type (might be due to invalid IL or missing references)
		Image val = CreatePanel(parent, name, anchoredPosition, size, new Color(0.1f, 0.13f, 0.31f, 0.92f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: true);
		CreatePanel(((Component)val).transform, "IconPlate", new Vector2(18f, -14f), new Vector2(44f, 44f), accentColor, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), rounded: true, shadow: false);
		Image val2 = CreateSkinIcon(((Component)val).transform, "CurrencyIcon", name + " " + icon, new Vector2(40f, -36f), new Vector2(34f, 34f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), Color.white);
		if ((Object)(object)val2 == (Object)null)
		{
			CreateText(((Component)val).transform, font, Color.white, "IconLabel", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -14f), new Vector2(44f, 44f), icon, 18, (TextAnchor)4, bold: true);
		}
		return CreateText(((Component)val).transform, font, Color.white, "ValueText", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-24f, 0f), new Vector2(size.x - 88f, 42f), value, 28, (TextAnchor)5, bold: true);
	}

	private Image CreateProgressBar(Transform parent, Vector2 anchoredPosition, Vector2 size)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		Image val = CreatePanel(parent, "RoundProgressBar", anchoredPosition, size, new Color(0.13f, 0.17f, 0.3f, 0.96f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), rounded: true, shadow: true);
		Image val2 = CreatePanel(((Component)val).transform, "Fill", Vector2.zero, Vector2.zero, new Color(0.24f, 0.94f, 0.62f, 0.96f), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), rounded: true, shadow: false);
		val2.type = (Type)3;
		val2.fillMethod = (FillMethod)0;
		val2.fillOrigin = 0;
		val2.fillAmount = 0f;
		return val2;
	}

	private Text CreateBuildInsightCell(Transform parent, Font font, string name, string title, Vector2 anchoredPosition, Color accentColor)
	{
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_0141: Unknown result type (might be due to invalid IL or missing references)
		//IL_0155: Unknown result type (might be due to invalid IL or missing references)
		//IL_015a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0169: Unknown result type (might be due to invalid IL or missing references)
		//IL_0178: Unknown result type (might be due to invalid IL or missing references)
		//IL_0187: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d7: Unknown result type (might be due to invalid IL or missing references)
		Image val = CreatePanel(parent, name, anchoredPosition, new Vector2(292f, 84f), new Color(0.08f, 0.13f, 0.32f, 0.9f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), rounded: true, shadow: false);
		CreatePanel(((Component)val).transform, "Accent", new Vector2(9f, 0f), new Vector2(10f, 60f), accentColor, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), rounded: true, shadow: false);
		CreateText(((Component)val).transform, font, Color.Lerp(accentColor, Color.white, 0.18f), "Title", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(10f, -9f), new Vector2(-18f, 30f), title, 22, (TextAnchor)4, bold: true);
		Text val2 = CreateText(((Component)val).transform, font, Color.white, "Value", new Vector2(0f, 0f), Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(10f, -12f), new Vector2(-22f, 34f), "대기", 21, (TextAnchor)4, bold: true);
		val2.resizeTextForBestFit = true;
		val2.fontSize = 24;
		((Graphic)val2).rectTransform.anchoredPosition = new Vector2(10f, -18f);
		((Graphic)val2).rectTransform.sizeDelta = new Vector2(-22f, 44f);
		val2.resizeTextMinSize = 18;
		val2.resizeTextMaxSize = 24;
		AddStrongTextOutline(val2);
		return val2;
	}

	private Text CreateGradeCard(Transform parent, Font font, CharacterGrade grade, Vector2 anchoredPosition, UnityAction onClick, string mergeRequirementText)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		//IL_015b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0160: Unknown result type (might be due to invalid IL or missing references)
		//IL_0175: Unknown result type (might be due to invalid IL or missing references)
		//IL_017f: Unknown result type (might be due to invalid IL or missing references)
		//IL_018e: Unknown result type (might be due to invalid IL or missing references)
		//IL_019d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0202: Unknown result type (might be due to invalid IL or missing references)
		//IL_0211: Unknown result type (might be due to invalid IL or missing references)
		//IL_0241: Unknown result type (might be due to invalid IL or missing references)
		//IL_0250: Unknown result type (might be due to invalid IL or missing references)
		//IL_0269: Unknown result type (might be due to invalid IL or missing references)
		//IL_0278: Unknown result type (might be due to invalid IL or missing references)
		//IL_0287: Unknown result type (might be due to invalid IL or missing references)
		//IL_0296: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0306: Unknown result type (might be due to invalid IL or missing references)
		//IL_0337: Unknown result type (might be due to invalid IL or missing references)
		//IL_0346: Unknown result type (might be due to invalid IL or missing references)
		//IL_035f: Unknown result type (might be due to invalid IL or missing references)
		//IL_036e: Unknown result type (might be due to invalid IL or missing references)
		//IL_037d: Unknown result type (might be due to invalid IL or missing references)
		//IL_038c: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0433: Unknown result type (might be due to invalid IL or missing references)
		//IL_0442: Unknown result type (might be due to invalid IL or missing references)
		//IL_0447: Unknown result type (might be due to invalid IL or missing references)
		//IL_0456: Unknown result type (might be due to invalid IL or missing references)
		//IL_0465: Unknown result type (might be due to invalid IL or missing references)
		//IL_0474: Unknown result type (might be due to invalid IL or missing references)
		//IL_0498: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_04bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_04fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_050c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0511: Unknown result type (might be due to invalid IL or missing references)
		//IL_0520: Unknown result type (might be due to invalid IL or missing references)
		//IL_052f: Unknown result type (might be due to invalid IL or missing references)
		//IL_053e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0562: Unknown result type (might be due to invalid IL or missing references)
		//IL_0571: Unknown result type (might be due to invalid IL or missing references)
		//IL_0576: Unknown result type (might be due to invalid IL or missing references)
		//IL_0585: Unknown result type (might be due to invalid IL or missing references)
		//IL_0594: Unknown result type (might be due to invalid IL or missing references)
		//IL_05a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_05c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_05d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_05ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_05fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_060d: Unknown result type (might be due to invalid IL or missing references)
		//IL_061c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0642: Unknown result type (might be due to invalid IL or missing references)
		//IL_064c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0651: Unknown result type (might be due to invalid IL or missing references)
		//IL_0660: Unknown result type (might be due to invalid IL or missing references)
		//IL_0665: Unknown result type (might be due to invalid IL or missing references)
		//IL_066a: Unknown result type (might be due to invalid IL or missing references)
		string displayName = CharacterGradeUtility.GetDisplayName(grade);
		Color color = CharacterGradeUtility.GetColor(grade, Color.white);
		string name = grade.ToString() + "GradeCard";
		Text labelText;
		Button val = CreateButton(parent, font, name, string.Empty, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), anchoredPosition, new Vector2(144f, 124f), new Color(0.05f, 0.07f, 0.2f, 0.96f), Color.white, onClick, out labelText);
		if (onClick == null)
		{
			((Selectable)val).transition = (Transition)0;
		}
		Image val2 = CreatePanel(((Component)val).transform, "GradeBody", new Vector2(0f, -8f), new Vector2(132f, 108f), new Color(0.07f, 0.1f, 0.27f, 0.92f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
		((Component)val2).transform.SetAsFirstSibling();
		CreatePanel(((Component)val).transform, "TitleBack", new Vector2(0f, -9f), new Vector2(120f, 34f), Color.Lerp(color, new Color(0.02f, 0.04f, 0.14f, 1f), 0.1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
		Text text = CreateText(((Component)val).transform, font, Color.white, "Title", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -11f), new Vector2(118f, 30f), displayName, 23, (TextAnchor)4, bold: true);
		AddStrongTextOutline(text);
		CreatePanel(((Component)val).transform, "MergeNeedBack", new Vector2(0f, -54f), new Vector2(108f, 27f), new Color(0.02f, 0.04f, 0.15f, 0.76f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), rounded: true, shadow: false);
		Text text2 = CreateText(((Component)val).transform, font, Color.Lerp(color, Color.white, 0.28f), "MergeNeedText", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -54f), new Vector2(104f, 24f), mergeRequirementText, 16, (TextAnchor)4, bold: true);
		AddStrongTextOutline(text2);
		CreatePanel(((Component)val).transform, "CountBack", new Vector2(0f, 11f), new Vector2(116f, 30f), new Color(0.02f, 0.04f, 0.14f, 0.84f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), rounded: true, shadow: false);
		Text val3 = CreateText(((Component)val).transform, font, Color.white, "Count", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 13f), new Vector2(114f, 27f), "0 / 3", 19, (TextAnchor)4, bold: true);
		AddStrongTextOutline(val3);
		if (grade == CharacterGrade.Transcendent)
		{
			Image val4 = CreatePanel(((Component)val).transform, "ReadyGlowTop", new Vector2(0f, -3f), new Vector2(138f, 5f), Color.clear, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
			Image val5 = CreatePanel(((Component)val).transform, "ReadyGlowRight", new Vector2(-3f, 0f), new Vector2(5f, 118f), Color.clear, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), rounded: true, shadow: false);
			Image val6 = CreatePanel(((Component)val).transform, "ReadyGlowBottom", new Vector2(0f, 3f), new Vector2(138f, 5f), Color.clear, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), rounded: true, shadow: false);
			Image val7 = CreatePanel(((Component)val).transform, "ReadyGlowLeft", new Vector2(3f, 0f), new Vector2(5f, 118f), Color.clear, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), rounded: true, shadow: false);
			Image val8 = CreatePanel(((Component)val).transform, "ReadyBadge", new Vector2(-3f, -3f), new Vector2(88f, 28f), new Color(1f, 0.72f, 0.16f, 0.98f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), rounded: true, shadow: false);
			Text text3 = CreateText(((Component)val8).transform, font, new Color(0.18f, 0.05f, 0.24f), "ReadyBadgeText", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, "READY", 14, (TextAnchor)4, bold: true);
			AddStrongTextOutline(text3);
			((Component)val4).gameObject.SetActive(false);
			((Component)val5).gameObject.SetActive(false);
			((Component)val6).gameObject.SetActive(false);
			((Component)val7).gameObject.SetActive(false);
			((Component)val8).gameObject.SetActive(false);
		}
		return val3;
	}

	private Text CreateText(Transform parent, Font font, Color color, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 size, string value, int fontSize, TextAnchor alignment, bool bold)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Expected O, but got Unknown
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject(name, new Type[1] { typeof(RectTransform) });
		val.transform.SetParent(parent, false);
		Text val2 = val.AddComponent<Text>();
		val2.font = font;
		val2.fontSize = fontSize;
		((Graphic)val2).color = RuntimeUiSkinUtility.ResolveReadableTextColor(parent, color, ((Object)(object)presentationConfig != (Object)null) ? presentationConfig.uiSkin : null);
		val2.text = RuntimeKoreanTextUtility.Clean(name, value);
		val2.alignment = alignment;
		val2.fontStyle = (FontStyle)(bold ? 1 : 0);
		((Graphic)val2).raycastTarget = false;
		RectTransform component = ((Component)val2).GetComponent<RectTransform>();
		component.anchorMin = anchorMin;
		component.anchorMax = anchorMax;
		component.pivot = pivot;
		component.anchoredPosition = anchoredPosition;
		component.sizeDelta = size;
		AddTextShadow(val2);
		return val2;
	}

	private Image CreatePanel(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, bool rounded, bool shadow)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Expected O, but got Unknown
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject(name, new Type[1] { typeof(RectTransform) });
		val.transform.SetParent(parent, false);
		Image val2 = val.AddComponent<Image>();
		((Graphic)val2).color = color;
		((Graphic)val2).raycastTarget = false;
		RuntimeUiSkinUtility.ApplyImageSkin(val2, ((Object)(object)presentationConfig != (Object)null) ? presentationConfig.uiSkin : null, name, isButton: false, rounded);
		ApplyRuntimeRoundedShape(val2, rounded);
		RectTransform rectTransform = ((Graphic)val2).rectTransform;
		rectTransform.anchorMin = anchorMin;
		rectTransform.anchorMax = anchorMax;
		rectTransform.pivot = pivot;
		rectTransform.anchoredPosition = anchoredPosition;
		rectTransform.sizeDelta = size;
		if (shadow)
		{
			Shadow val3 = val.AddComponent<Shadow>();
			val3.effectColor = new Color(0f, 0f, 0f, 0.32f);
			val3.effectDistance = new Vector2(0f, -6f);
		}
		return val2;
	}

	private Button CreateButton(Transform parent, Font font, string name, string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 size, Color backgroundColor, Color labelColor, UnityAction onClick, out Text labelText)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Expected O, but got Unknown
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Expected O, but got Unknown
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0154: Unknown result type (might be due to invalid IL or missing references)
		//IL_0159: Unknown result type (might be due to invalid IL or missing references)
		//IL_0174: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject(name, new Type[1] { typeof(RectTransform) });
		val.transform.SetParent(parent, false);
		Image val2 = val.AddComponent<Image>();
		((Graphic)val2).color = backgroundColor;
		RuntimeUiSkinUtility.ApplyImageSkin(val2, ((Object)(object)presentationConfig != (Object)null) ? presentationConfig.uiSkin : null, name, isButton: true, rounded: true);
		ApplyRuntimeRoundedShape(val2, rounded: true);
		((Graphic)val2).raycastTarget = true;
		Shadow val3 = val.AddComponent<Shadow>();
		val3.effectColor = new Color(0f, 0f, 0f, 0.35f);
		val3.effectDistance = new Vector2(0f, -7f);
		Button val4 = val.AddComponent<Button>();
		((Selectable)val4).targetGraphic = (Graphic)(object)val2;
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
		labelText = CreateText(val.transform, font, labelColor, "Label", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, label, 27, (TextAnchor)4, bold: true);
		TryAddButtonIcon(val.transform, name, label, size, labelText);
		return val4;
	}

	private void ApplyRuntimeRoundedShape(Image image, bool rounded)
	{
		if (!((Object)(object)image == (Object)null) && rounded)
		{
			if ((Object)(object)image.sprite == (Object)null)
			{
				image.sprite = GetRoundedPanelSprite();
			}
			image.type = (Type)1;
			image.preserveAspect = false;
		}
	}

	private Image CreateSkinIcon(Transform parent, string name, string iconKey, Vector2 anchoredPosition, Vector2 size, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Color color)
	{
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Expected O, but got Unknown
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		UiSkinResources skin = (((Object)(object)presentationConfig != (Object)null) ? presentationConfig.uiSkin : null);
		Sprite val = RuntimeUiSkinUtility.ResolveIconSprite(skin, iconKey);
		if ((Object)(object)val == (Object)null)
		{
			return null;
		}
		GameObject val2 = new GameObject(name, new Type[1] { typeof(RectTransform) });
		val2.transform.SetParent(parent, false);
		Image val3 = val2.AddComponent<Image>();
		val3.sprite = val;
		val3.type = (Type)0;
		((Graphic)val3).color = color;
		val3.preserveAspect = true;
		((Graphic)val3).raycastTarget = false;
		RectTransform rectTransform = ((Graphic)val3).rectTransform;
		rectTransform.anchorMin = anchorMin;
		rectTransform.anchorMax = anchorMax;
		rectTransform.pivot = pivot;
		rectTransform.anchoredPosition = anchoredPosition;
		rectTransform.sizeDelta = size;
		return val3;
	}

	private void BuildHamburgerIcon(Transform parent)
	{
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		for (int i = 0; i < 3; i++)
		{
			float num = 10f - (float)i * 10f;
			CreatePanel(parent, "HamburgerLine_" + i, new Vector2(0f, num), new Vector2(34f, 5f), new Color(0.92f, 0.96f, 1f, 0.96f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), rounded: true, shadow: false);
		}
	}

	private void TryAddButtonIcon(Transform buttonTransform, string name, string label, Vector2 size, Text labelText)
	{
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		if (string.IsNullOrWhiteSpace(label))
		{
			return;
		}
		string text = name + " " + label;
		UiSkinResources skin = (((Object)(object)presentationConfig != (Object)null) ? presentationConfig.uiSkin : null);
		if ((Object)(object)RuntimeUiSkinUtility.ResolveIconSprite(skin, text) == (Object)null)
		{
			return;
		}
		bool flag = string.Equals(label, "X", StringComparison.OrdinalIgnoreCase);
		if (!flag)
		{
			return;
		}
		float num = Mathf.Clamp(Mathf.Min(size.x, size.y) * 0.44f, 24f, 42f);
		Vector2 anchoredPosition = (Vector2)(flag ? Vector2.zero : new Vector2(30f, 0f));
		Vector2 val = (flag ? new Vector2(0.5f, 0.5f) : new Vector2(0f, 0.5f));
		Image val2 = CreateSkinIcon(buttonTransform, "ButtonIcon", text, anchoredPosition, new Vector2(num, num), val, val, new Vector2(0.5f, 0.5f), Color.white);
		if (!((Object)(object)val2 == (Object)null) && !((Object)(object)labelText == (Object)null))
		{
			if (flag)
			{
				((Behaviour)labelText).enabled = false;
				return;
			}
			RectTransform rectTransform = ((Graphic)labelText).rectTransform;
			Vector2 offsetMin = rectTransform.offsetMin;
			offsetMin.x = Mathf.Max(offsetMin.x, num + 24f);
			rectTransform.offsetMin = offsetMin;
		}
	}

	private void AddTextShadow(Text text)
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		Shadow val = ((Component)text).gameObject.AddComponent<Shadow>();
		val.effectColor = new Color(0f, 0f, 0f, 0.42f);
		val.effectDistance = new Vector2(2f, -2f);
	}

	private void AddCardOutline(Button button, Color color, float distance)
	{
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)button == (Object)null))
		{
			Outline val = ((Component)button).GetComponent<Outline>();
			if ((Object)(object)val == (Object)null)
			{
				val = ((Component)button).gameObject.AddComponent<Outline>();
			}
			float num = Mathf.Max(1f, distance);
			((Shadow)val).effectColor = color;
			((Shadow)val).effectDistance = new Vector2(num, 0f - num);
			((Shadow)val).useGraphicAlpha = true;
		}
	}

	private void AddStrongTextOutline(Text text)
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
			((Shadow)val).effectColor = new Color(0f, 0f, 0f, 0.82f);
			((Shadow)val).effectDistance = new Vector2(1.7f, -1.7f);
		}
	}

	private Sprite GetRoundedPanelSprite()
	{
		if ((Object)(object)roundedPanelSprite == (Object)null)
		{
			roundedPanelSprite = CreateRuntimeSprite("RuntimeRoundedPanel", 64, 64, 18f);
		}
		return roundedPanelSprite;
	}

	private Sprite GetCircleSprite()
	{
		if ((Object)(object)circleSprite == (Object)null)
		{
			circleSprite = CreateRuntimeSprite("RuntimeCircle", 64, 64, 32f);
		}
		return circleSprite;
	}

	private Sprite CreateRuntimeSprite(string name, int width, int height, float radius)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Expected O, but got Unknown
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		Texture2D val = new Texture2D(width, height, (TextureFormat)5, false);
		((Object)val).name = name;
		((Texture)val).wrapMode = (TextureWrapMode)1;
		Color[] array = (Color[])(object)new Color[width * height];
		for (int i = 0; i < height; i++)
		{
			for (int j = 0; j < width; j++)
			{
				float num = Mathf.Clamp((float)j, radius, (float)width - radius - 1f);
				float num2 = Mathf.Clamp((float)i, radius, (float)height - radius - 1f);
				float num3 = Vector2.Distance(new Vector2((float)j, (float)i), new Vector2(num, num2));
				float num4 = Mathf.Clamp01(radius + 0.5f - num3);
				array[i * width + j] = new Color(1f, 1f, 1f, num4);
			}
		}
		val.SetPixels(array);
		val.Apply();
		return Sprite.Create(val, new Rect(0f, 0f, (float)width, (float)height), new Vector2(0.5f, 0.5f), 100f, 0u, (SpriteMeshType)0, new Vector4(radius, radius, radius, radius));
	}

	private void ReplaceNamedPrimitive(Transform parent, string name, PrimitiveType primitiveType, Vector3 position, Vector3 scale, Color color)
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		Transform val = parent.Find(name);
		if ((Object)(object)val != (Object)null)
		{
			SafeDestroy(((Component)val).gameObject);
		}
		GameObject val2 = GameObject.CreatePrimitive(primitiveType);
		((Object)val2).name = name;
		val2.transform.SetParent(parent);
		val2.transform.position = position;
		val2.transform.localScale = scale;
		Renderer component = val2.GetComponent<Renderer>();
		if ((Object)(object)component != (Object)null)
		{
			component.material.color = color;
		}
	}

	private Color GetSlotColor(int index)
	{
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		Color[] array = (((Object)(object)presentationConfig != (Object)null && presentationConfig.slotColors != null && presentationConfig.slotColors.Length != 0) ? presentationConfig.slotColors : DefaultSlotColors);
		return array[index % array.Length];
	}

	private Color GetLaneColor(int index)
	{
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		Color[] array = (((Object)(object)presentationConfig != (Object)null && presentationConfig.laneColors != null && presentationConfig.laneColors.Length != 0) ? presentationConfig.laneColors : DefaultLaneColors);
		return array[index % array.Length];
	}

	private Color GetConfigColor(Func<GamePresentationConfig, Color> selector, Color fallback)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		return ((Object)(object)presentationConfig != (Object)null) ? selector(presentationConfig) : fallback;
	}

	private void SafeDestroy(GameObject target)
	{
		if (Application.isPlaying)
		{
			Object.Destroy((Object)(object)target);
		}
		else
		{
			Object.DestroyImmediate((Object)(object)target);
		}
	}

	private void SafeDestroy(Component target)
	{
		if (!((Object)(object)target == (Object)null))
		{
			if (Application.isPlaying)
			{
				Object.Destroy((Object)(object)target);
			}
			else
			{
				Object.DestroyImmediate((Object)(object)target);
			}
		}
	}

	private void AssignPrivateField(Object target, string fieldName, object value)
	{
		FieldInfo field = ((object)target).GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
		if (field != null)
		{
			field.SetValue(target, value);
		}
	}
}
