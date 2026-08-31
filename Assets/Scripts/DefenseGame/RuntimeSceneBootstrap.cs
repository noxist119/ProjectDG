using System;
using System.Collections.Generic;
using System.Reflection;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DefenseGame
{
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

		private static readonly Color[] DefaultSlotColors = new Color[10]
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

		private static readonly Color[] DefaultLaneColors = new Color[5]
		{
			new Color(0.16f, 0.7f, 0.98f),
			new Color(0.24f, 0.88f, 0.54f),
			new Color(0.98f, 0.66f, 0.2f),
			new Color(0.94f, 0.28f, 0.43f),
			new Color(0.72f, 0.38f, 0.95f)
		};

		private static Sprite roundedPanelSprite;
		private static Sprite circleSprite;

		private Transform runtimeStageRoot;
		private Transform runtimeCombatCameraRoot;
		private Camera runtimeCombatCamera;
		private int runtimeSceneBuildCount;

		public int RuntimeSceneBuildCount => runtimeSceneBuildCount;
		public bool IsGameplayStageVisible => runtimeStageRoot != null && runtimeStageRoot.gameObject.activeSelf && runtimeCombatCameraRoot != null && runtimeCombatCameraRoot.gameObject.activeSelf;

		private void Start()
		{
			if (buildOnStart)
			{
				float buildStartedAt = Time.realtimeSinceStartup;
				BuildScene();
				Debug.Log("[RuntimeSceneBootstrap] Runtime stage ready in " + (Time.realtimeSinceStartup - buildStartedAt).ToString("F2") + "s");
			}
		}

		[ContextMenu("Build Runtime Stage")]
		public void BuildScene()
		{
			DOTween.SetTweensCapacity(400, 150);
			CharacterDatabase characterDatabase = GetOrAdd<CharacterDatabase>(base.gameObject);
			MonsterDatabase monsterDatabase = GetOrAdd<MonsterDatabase>(base.gameObject);
			DefenseBoardManager boardManager = GetOrAdd<DefenseBoardManager>(base.gameObject);
			RoundManager roundManager = GetOrAdd<RoundManager>(base.gameObject);
			DefenseGameController gameController = GetOrAdd<DefenseGameController>(base.gameObject);
			DemoInputController demoInput = GetOrAdd<DemoInputController>(base.gameObject);
			GameUIButtonBinder buttonBinder = GetOrAdd<GameUIButtonBinder>(base.gameObject);
			SimpleGameHUD hud = GetOrAdd<SimpleGameHUD>(base.gameObject);
			AugmentManager augmentManager = GetOrAdd<AugmentManager>(base.gameObject);
			CharacterCollectionUI collectionUI = GetOrAdd<CharacterCollectionUI>(base.gameObject);
			MetaFlowUI metaFlowUI = GetOrAdd<MetaFlowUI>(base.gameObject);
			OutgameProgressionSystem outgameProgression = GetOrAdd<OutgameProgressionSystem>(base.gameObject);
			YahtzeeProgressionSystem yahtzeeProgression = GetOrAdd<YahtzeeProgressionSystem>(base.gameObject);
			BoardSynergySystem synergySystem = GetOrAdd<BoardSynergySystem>(base.gameObject);
			TacticalMissionSystem missionSystem = GetOrAdd<TacticalMissionSystem>(base.gameObject);
			BoardTileModifierSystem tileModifierSystem = GetOrAdd<BoardTileModifierSystem>(base.gameObject);
			RunShopSystem runShopSystem = GetOrAdd<RunShopSystem>(base.gameObject);
			RuntimeRenderBatchingUtility.Configure(presentationConfig);
			UnitAnimatorUpdateScheduler.Configure(presentationConfig);
			AnimationEventMaterialRegistry.Configure((presentationConfig != null) ? presentationConfig.animationEventMaterials : null);
			MonsterUnit.ConfigurePetrifyMaterial((characterCombatTuningConfig != null) ? characterCombatTuningConfig.defaultPetrifyMaterial : null);
			characterDatabase.ApplyPresentationConfig(presentationConfig);
			characterDatabase.ApplyCombatTuningConfig(characterCombatTuningConfig);
			outgameProgression.Configure(outgameProgressionConfig, characterDatabase);
			yahtzeeProgression.Configure(outgameProgression);
			monsterDatabase.ApplyPresentationConfig(presentationConfig);
			monsterDatabase.ApplyCombatTuningConfig(monsterCombatTuningConfig);
			Transform root = EnsureRoot("RuntimeStageRoot");
			runtimeStageRoot = root;
			root.gameObject.SetActive(true);
			Transform boardRoot = EnsureChild(root, "BoardSlots");
			Transform spawnRoot = EnsureChild(root, "SpawnPoints");
			Transform templateRoot = EnsureChild(root, "Templates");
			Transform miscRoot = EnsureChild(root, "Misc");
			ClearChildren(boardRoot);
			ClearChildren(spawnRoot);
			ClearChildren(templateRoot);
			ClearChildren(miscRoot);
			RemoveChildIfExists(root, "LaneDecor");
			EnsureGround(root);
			EnsureBackdrop(root);
			EnsureCamera();
			EnsureLight();
			List<BoardSlot> slots = BuildSlots(boardRoot);
			Transform[] spawnPoints = BuildSpawnPoints(spawnRoot);
			bool useCustomBackground = presentationConfig != null && presentationConfig.backgroundPrefab != null;
			Transform goalPoint = BuildGoal(miscRoot, useCustomBackground && hideDefaultStageDecorWhenUsingBackground);
			if (!useCustomBackground || !hideDefaultStageDecorWhenUsingBackground)
			{
				BuildCenterCrystal(miscRoot);
				BuildFlankTowers(miscRoot);
				BuildSkyOrnaments(miscRoot);
			}
			Projectile projectileTemplate = BuildProjectileTemplate(templateRoot);
			DefenderUnit defenderTemplate = BuildDefenderTemplate(templateRoot, projectileTemplate);
			MonsterUnit monsterTemplate = BuildMonsterTemplate(templateRoot);
			boardManager.Configure(slots, defenderTemplate);
			roundManager.Configure(monsterDatabase, monsterTemplate, spawnPoints, goalPoint, (presentationConfig != null) ? presentationConfig.spawnPortalPrefab : null);
			gameController.Configure(characterDatabase, monsterDatabase, boardManager, roundManager, defenderTemplate);
			tileModifierSystem.Configure(gameController, boardManager);
			gameController.RegisterRunShopSystem(runShopSystem);
			gameController.RegisterTacticalMissionSystem(missionSystem);
			demoInput.Configure(gameController);
			buttonBinder.Configure(gameController);
            BuildCanvas(root, hud, gameController, boardManager, buttonBinder, augmentManager, collectionUI, metaFlowUI, synergySystem, missionSystem, tileModifierSystem, runShopSystem, characterDatabase, outgameProgression);
            EnsureRuntimeBgm(gameController);
            runtimeSceneBuildCount++;
            SetGameplayStageVisible(false);
        }

        /// <summary>
        /// Keeps meta systems and the screen-space outgame UI alive while disabling the
        /// world board, spawn/template hierarchy, and combat camera outside battle.
        /// </summary>
        public void SetGameplayStageVisible(bool visible)
        {
            if (visible && runtimeStageRoot == null)
            {
                BuildScene();
                return;
            }

            if (runtimeStageRoot != null)
            {
                runtimeStageRoot.gameObject.SetActive(visible);
            }

            if (runtimeCombatCameraRoot != null)
            {
                runtimeCombatCameraRoot.gameObject.SetActive(visible);
            }
            else if (runtimeCombatCamera != null)
            {
                runtimeCombatCamera.enabled = visible;
            }
        }

		private void EnsureRuntimeBgm(DefenseGameController gameController)
		{
			if (Application.isPlaying && playMainBgm)
			{
				Transform existing = base.transform.Find("BGMPlayer");
				if (existing == null)
				{
					existing = base.transform.Find("MainBGMPlayer");
				}
				GameObject player = ((existing != null) ? existing.gameObject : new GameObject("BGMPlayer"));
				player.name = "BGMPlayer";
				if (existing == null)
				{
					player.transform.SetParent(base.transform, worldPositionStays: false);
				}
				RuntimeBgmController bgmController = player.GetComponent<RuntimeBgmController>();
				if (bgmController == null)
				{
					bgmController = player.AddComponent<RuntimeBgmController>();
				}
				bgmController.Configure(gameController, mainBgmResourcePath, bossBgmResourcePath, mainBgmVolume, bossBgmVolume, bgmFadeDuration);
			}
		}

		private T GetOrAdd<T>(GameObject target) where T : Component
		{
			T component = target.GetComponent<T>();
			if (component == null)
			{
				component = target.AddComponent<T>();
			}
			return component;
		}

		private Transform EnsureRoot(string name)
		{
			Transform existing = base.transform.Find(name);
			if (existing != null)
			{
				return existing;
			}
			GameObject root = new GameObject(name);
			root.transform.SetParent(base.transform);
			root.transform.localPosition = Vector3.zero;
			return root.transform;
		}

		private Transform EnsureChild(Transform parent, string name)
		{
			Transform existing = parent.Find(name);
			if (existing != null)
			{
				return existing;
			}
			GameObject child = new GameObject(name);
			child.transform.SetParent(parent);
			child.transform.localPosition = Vector3.zero;
			return child.transform;
		}

		private void ClearChildren(Transform root)
		{
			for (int i = root.childCount - 1; i >= 0; i--)
			{
				SafeDestroy(root.GetChild(i).gameObject);
			}
		}

		private void RemoveChildIfExists(Transform parent, string childName)
		{
			Transform child = ((parent != null) ? parent.Find(childName) : null);
			if (child != null)
			{
				SafeDestroy(child.gameObject);
			}
		}

		private void EnsureGround(Transform root)
		{
			ReplaceNamedPrimitive(root, "Ground", PrimitiveType.Plane, new Vector3(0f, -0.5f, 0f), new Vector3(2f, 1f, 1.8f), GetConfigColor((GamePresentationConfig config) => config.groundColor, new Color(0.08f, 0.11f, 0.14f)));
			ReplaceNamedPrimitive(root, "BoardStrip", PrimitiveType.Cube, new Vector3(0f, -0.15f, -5.5f), new Vector3(20f, 0.25f, 2.6f), GetConfigColor((GamePresentationConfig config) => config.boardStripColor, new Color(0.12f, 0.18f, 0.24f)));
			ReplaceNamedPrimitive(root, "EnemyRunway", PrimitiveType.Cube, new Vector3(0f, -0.15f, 2.1f), new Vector3(20f, 0.2f, 12.5f), GetConfigColor((GamePresentationConfig config) => config.enemyRunwayColor, new Color(0.18f, 0.1f, 0.11f)));
			ReplaceNamedPrimitive(root, "MidBridge", PrimitiveType.Cube, new Vector3(0f, -0.12f, -1.6f), new Vector3(20f, 0.08f, 1.2f), GetConfigColor((GamePresentationConfig config) => config.midBridgeColor, new Color(0.25f, 0.29f, 0.36f)));
		}

		private void EnsureBackdrop(Transform root)
		{
			Transform oldOverride = root.Find("BackgroundOverride");
			if (oldOverride != null)
			{
				SafeDestroy(oldOverride.gameObject);
			}
			if (presentationConfig != null && presentationConfig.backgroundPrefab != null)
			{
				GameObject overrideObject = UnityEngine.Object.Instantiate(presentationConfig.backgroundPrefab, root);
				overrideObject.name = "BackgroundOverride";
				overrideObject.transform.localPosition = Vector3.zero;
				overrideObject.transform.localRotation = Quaternion.identity;
				overrideObject.transform.localScale = Vector3.one;
				return;
			}
			ReplaceNamedPrimitive(root, "NorthWall", PrimitiveType.Cube, new Vector3(0f, 2.5f, 10.5f), new Vector3(24f, 5f, 0.5f), GetConfigColor((GamePresentationConfig config) => config.northWallColor, new Color(0.17f, 0.14f, 0.22f)));
			ReplaceNamedPrimitive(root, "SouthWall", PrimitiveType.Cube, new Vector3(0f, 2f, -9.8f), new Vector3(24f, 4f, 0.5f), GetConfigColor((GamePresentationConfig config) => config.southWallColor, new Color(0.13f, 0.19f, 0.24f)));
			ReplaceNamedPrimitive(root, "LeftCliff", PrimitiveType.Cube, new Vector3(-11.2f, 1.5f, 0f), new Vector3(1.2f, 3f, 21f), GetConfigColor((GamePresentationConfig config) => config.sideWallColor, new Color(0.12f, 0.14f, 0.18f)));
			ReplaceNamedPrimitive(root, "RightCliff", PrimitiveType.Cube, new Vector3(11.2f, 1.5f, 0f), new Vector3(1.2f, 3f, 21f), GetConfigColor((GamePresentationConfig config) => config.sideWallColor, new Color(0.12f, 0.14f, 0.18f)));
			ReplaceNamedPrimitive(root, "LeftBanner", PrimitiveType.Cube, new Vector3(-9.5f, 3.5f, -5.7f), new Vector3(1.2f, 2.8f, 0.2f), GetLaneColor(0));
			ReplaceNamedPrimitive(root, "RightBanner", PrimitiveType.Cube, new Vector3(9.5f, 3.5f, -5.7f), new Vector3(1.2f, 2.8f, 0.2f), GetLaneColor(3));
		}        private void EnsureCamera()
        {
            if (runtimeCombatCameraRoot == null)
            {
                runtimeCombatCameraRoot = EnsureRoot("RuntimeCombatCameraRoot");
            }

            // Reactivate only while rebuilding so Camera.main resolves the existing
            // runtime camera instead of allocating a duplicate after an outgame page.
            runtimeCombatCameraRoot.gameObject.SetActive(true);
            Camera camera = runtimeCombatCamera != null ? runtimeCombatCamera : Camera.main;
            if (camera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.tag = "MainCamera";
            }

            camera.transform.SetParent(runtimeCombatCameraRoot, true);
            runtimeCombatCamera = camera;
            Vector3 widePosition = new Vector3(0f, 15f, -12.4f);
            Vector3 portraitPosition = new Vector3(0f, 17.6f, -16.1f);
            Vector3 cameraEuler = new Vector3(53f, 0f, 0f);
            camera.transform.position = widePosition;
            camera.transform.rotation = Quaternion.Euler(cameraEuler);
            camera.backgroundColor = new Color(0.05f, 0.07f, 0.11f);
            camera.clearFlags = CameraClearFlags.Color;
            camera.orthographic = false;
            camera.fieldOfView = 50f;
            RuntimeBattleCameraFitter fitter = camera.GetComponent<RuntimeBattleCameraFitter>();
            if (fitter == null)
            {
                fitter = camera.gameObject.AddComponent<RuntimeBattleCameraFitter>();
            }
            fitter.Configure(widePosition, portraitPosition, cameraEuler, 50f, 60f);
        }
private void EnsureLight()
		{
			Light light = UnityEngine.Object.FindObjectOfType<Light>();
			if (light == null)
			{
				GameObject lightObject = new GameObject("Directional Light");
				light = lightObject.AddComponent<Light>();
				light.type = LightType.Directional;
			}
			light.transform.rotation = Quaternion.Euler(45f, -40f, 0f);
			light.intensity = 1.2f;
		}

		private List<BoardSlot> BuildSlots(Transform boardRoot)
		{
			List<BoardSlot> slots = new List<BoardSlot>();
			int backCount = Mathf.Max(1, slotCount);
			float backWidth = (float)(backCount - 1) * slotSpacing;
			Vector3 backCenter = boardCenter;
			if (backCenter.z > -8.45f)
			{
				backCenter.z = -8.45f;
			}
			backCenter.z += backSlotZOffset;
			for (int i = 0; i < backCount; i++)
			{
				Vector3 position = backCenter + new Vector3((0f - backWidth) * 0.5f + (float)i * slotSpacing, 0f, 0f);
				slots.Add(CreateRuntimeBoardSlot(boardRoot, "Slot_" + i.ToString("D2"), position, i));
			}
			int frontCount = Mathf.Max(0, frontSlotCount);
			if (frontCount > 0)
			{
				float frontSpacing = slotSpacing * 1.42f;
				float frontWidth = (float)(frontCount - 1) * frontSpacing;
				Vector3 frontCenter = backCenter + new Vector3(0f, 0f, frontSlotZOffset);
				for (int j = 0; j < frontCount; j++)
				{
					Vector3 position2 = frontCenter + new Vector3((0f - frontWidth) * 0.5f + (float)j * frontSpacing, 0f, 0f);
					slots.Add(CreateRuntimeBoardSlot(boardRoot, "FrontSlot_" + j.ToString("D2"), position2, backCount + j));
				}
			}
			return slots;
		}

		private BoardSlot CreateRuntimeBoardSlot(Transform boardRoot, string slotName, Vector3 position, int colorIndex)
		{
			Color baseColor = GetSlotColor(colorIndex);
			Color trimColor = GetSlotColor(colorIndex + 3);
			GameObject slotObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
			slotObject.name = slotName;
			slotObject.transform.SetParent(boardRoot);
			slotObject.transform.position = position;
			slotObject.transform.localScale = new Vector3(1.15f, 0.13f, 1.04f);
			Renderer slotRenderer = slotObject.GetComponent<Renderer>();
			if (slotRenderer != null)
			{
				slotRenderer.sharedMaterial = CreateRuntimeMaterial(Color.Lerp(baseColor, new Color(0.05f, 0.07f, 0.13f), 0.72f));
			}
			BoardSlot slot = slotObject.AddComponent<BoardSlot>();
			GameObject anchor = new GameObject("Anchor");
			anchor.transform.SetParent(slotObject.transform, worldPositionStays: false);
			anchor.transform.localPosition = new Vector3(0f, 1.38f, 0f);
			AssignPrivateField(slot, "unitAnchor", anchor.transform);
			BuildSlotVisual(slotObject.transform, baseColor, trimColor);
			return slot;
		}

		private void BuildSlotVisual(Transform slotRoot, Color baseColor, Color trimColor)
		{
			Color dark = Color.Lerp(baseColor, new Color(0.03f, 0.04f, 0.09f), 0.58f);
			Color bright = Color.Lerp(trimColor, Color.white, 0.28f);
			CreateSlotPrimitive(slotRoot, "TopInset", PrimitiveType.Cube, new Vector3(0f, 0.6f, 0f), new Vector3(0.78f, 0.03f, 0.72f), Color.Lerp(baseColor, Color.white, 0.18f));
			CreateSlotPrimitive(slotRoot, "FrontLip", PrimitiveType.Cube, new Vector3(0f, 0.68f, -0.48f), new Vector3(0.88f, 0.04f, 0.035f), bright);
			CreateSlotPrimitive(slotRoot, "BackLip", PrimitiveType.Cube, new Vector3(0f, 0.62f, 0.48f), new Vector3(0.8f, 0.028f, 0.026f), dark);
			CreateSlotPrimitive(slotRoot, "AuraDisc", PrimitiveType.Cylinder, new Vector3(0f, 0.72f, 0f), new Vector3(0.44f, 0.018f, 0.44f), Color.Lerp(baseColor, Color.white, 0.05f));
			CreateSlotPrimitive(slotRoot, "SummonHalo", PrimitiveType.Cylinder, new Vector3(0f, 0.755f, 0f), new Vector3(0.62f, 0.012f, 0.62f), Color.Lerp(trimColor, Color.white, 0.38f));
			CreateSlotPrimitive(slotRoot, "SummonCore", PrimitiveType.Sphere, new Vector3(0f, 0.82f, 0f), Vector3.one * 0.065f, Color.Lerp(bright, Color.white, 0.3f));
			CreateSlotLine(slotRoot, "SlotBorder", bright, 0.02f, new Vector3(-0.5f, 0.82f, -0.45f), new Vector3(0.5f, 0.82f, -0.45f), new Vector3(0.5f, 0.82f, 0.45f), new Vector3(-0.5f, 0.82f, 0.45f), new Vector3(-0.5f, 0.82f, -0.45f));
			CreateSlotLine(slotRoot, "SlotRune", Color.Lerp(baseColor, Color.white, 0.5f), 0.014f, new Vector3(0f, 0.86f, -0.28f), new Vector3(0.3f, 0.86f, 0f), new Vector3(0f, 0.86f, 0.28f), new Vector3(-0.3f, 0.86f, 0f), new Vector3(0f, 0.86f, -0.28f));
			CreateSlotLine(slotRoot, "SummonChevronFront", Color.Lerp(trimColor, Color.white, 0.62f), 0.018f, new Vector3(-0.2f, 0.88f, -0.2f), new Vector3(0f, 0.88f, -0.34f), new Vector3(0.2f, 0.88f, -0.2f));
			CreateSlotLine(slotRoot, "SummonChevronBack", Color.Lerp(trimColor, Color.white, 0.42f), 0.014f, new Vector3(-0.17f, 0.875f, 0.18f), new Vector3(0f, 0.875f, 0.3f), new Vector3(0.17f, 0.875f, 0.18f));
			Vector3[] corners = new Vector3[4]
			{
				new Vector3(-0.42f, 0.82f, -0.38f),
				new Vector3(0.42f, 0.82f, -0.38f),
				new Vector3(-0.42f, 0.82f, 0.38f),
				new Vector3(0.42f, 0.82f, 0.38f)
			};
			for (int i = 0; i < corners.Length; i++)
			{
				CreateSlotPrimitive(slotRoot, "CornerGem_" + i, PrimitiveType.Sphere, corners[i], Vector3.one * 0.055f, bright);
			}
		}

		private GameObject CreateSlotPrimitive(Transform parent, string name, PrimitiveType type, Vector3 localPosition, Vector3 localScale, Color color)
		{
			GameObject primitive = GameObject.CreatePrimitive(type);
			primitive.name = name;
			primitive.transform.SetParent(parent, worldPositionStays: false);
			primitive.transform.localPosition = localPosition;
			primitive.transform.localRotation = Quaternion.identity;
			primitive.transform.localScale = localScale;
			Collider collider = primitive.GetComponent<Collider>();
			if (collider != null)
			{
				SafeDestroy(collider);
			}
			Renderer renderer = primitive.GetComponent<Renderer>();
			if (renderer != null)
			{
				renderer.sharedMaterial = CreateRuntimeMaterial(color);
			}
			return primitive;
		}

		private void CreateSlotLine(Transform parent, string name, Color color, float width, params Vector3[] points)
		{
			GameObject lineObject = new GameObject(name);
			lineObject.transform.SetParent(parent, worldPositionStays: false);
			LineRenderer line = lineObject.AddComponent<LineRenderer>();
			line.sharedMaterial = CreateRuntimeLineMaterial();
			line.useWorldSpace = false;
			line.positionCount = Mathf.Max(2, (points != null) ? points.Length : 0);
			line.startWidth = width;
			line.endWidth = width;
			line.numCapVertices = 3;
			line.numCornerVertices = 3;
			line.startColor = color;
			line.endColor = color;
			for (int i = 0; i < line.positionCount; i++)
			{
				line.SetPosition(i, (points != null && i < points.Length) ? points[i] : Vector3.zero);
			}
		}

		private Material CreateRuntimeMaterial(Color color)
		{
			Shader shader = Shader.Find("Universal Render Pipeline/Lit");
			if (shader == null)
			{
				shader = Shader.Find("Standard");
			}
			if (shader == null)
			{
				shader = Shader.Find("Sprites/Default");
			}
			Material material = new Material(shader);
			material.color = color;
			if (material.HasProperty("_BaseColor"))
			{
				material.SetColor("_BaseColor", color);
			}
			return material;
		}

		private Material CreateRuntimeLineMaterial()
		{
			Shader shader = Shader.Find("Sprites/Default");
			if (shader == null)
			{
				shader = Shader.Find("Unlit/Color");
			}
			return new Material(shader);
		}

		private Transform[] BuildSpawnPoints(Transform spawnRoot)
		{
			Transform[] points = new Transform[laneCount];
			float width = (float)(laneCount - 1) * laneSpacing;
			for (int i = 0; i < laneCount; i++)
			{
				GameObject point = new GameObject("Spawn_" + i.ToString("D2"));
				point.transform.SetParent(spawnRoot);
				point.transform.position = spawnCenter + new Vector3((0f - width) * 0.5f + (float)i * laneSpacing, 0f, 0f);
				points[i] = point.transform;
			}
			return points;
		}

		private Transform BuildGoal(Transform miscRoot, bool logicOnly)
		{
			if (logicOnly)
			{
				GameObject hiddenGoal = new GameObject("GoalPoint");
				hiddenGoal.transform.SetParent(miscRoot);
				hiddenGoal.transform.position = new Vector3(0f, 0f, -9.75f);
				return hiddenGoal.transform;
			}
			if (presentationConfig != null && presentationConfig.goalPrefab != null)
			{
				GameObject overrideGoal = UnityEngine.Object.Instantiate(presentationConfig.goalPrefab, miscRoot);
				overrideGoal.name = "GoalPoint";
				overrideGoal.transform.localPosition = new Vector3(0f, 0f, -9.75f);
				overrideGoal.transform.localRotation = Quaternion.identity;
				return overrideGoal.transform;
			}
			GameObject goal = new GameObject("GoalPoint");
			goal.transform.SetParent(miscRoot);
			goal.transform.position = new Vector3(0f, 0f, -9.75f);
			GameObject gate = GameObject.CreatePrimitive(PrimitiveType.Cube);
			gate.name = "DefenseGate";
			gate.transform.SetParent(goal.transform);
			gate.transform.localPosition = new Vector3(0f, 1.3f, 0f);
			gate.transform.localScale = new Vector3(6.5f, 2.6f, 0.6f);
			gate.GetComponent<Renderer>().material.color = GetConfigColor((GamePresentationConfig config) => config.gateColor, new Color(0.24f, 0.54f, 0.72f));
			GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			core.name = "GateCore";
			core.transform.SetParent(goal.transform);
			core.transform.localPosition = new Vector3(0f, 1.7f, -0.1f);
			core.transform.localScale = Vector3.one * 1.1f;
			core.GetComponent<Renderer>().material.color = GetConfigColor((GamePresentationConfig config) => config.gateCoreColor, new Color(0.38f, 0.89f, 1f));
			GameObject towerLeft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
			towerLeft.name = "GateTowerLeft";
			towerLeft.transform.SetParent(goal.transform);
			towerLeft.transform.localPosition = new Vector3(-3.6f, 1.5f, 0f);
			towerLeft.transform.localScale = new Vector3(0.6f, 1.5f, 0.6f);
			towerLeft.GetComponent<Renderer>().material.color = new Color(0.17f, 0.44f, 0.63f);
			GameObject towerRight = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
			towerRight.name = "GateTowerRight";
			towerRight.transform.SetParent(goal.transform);
			towerRight.transform.localPosition = new Vector3(3.6f, 1.5f, 0f);
			towerRight.transform.localScale = new Vector3(0.6f, 1.5f, 0.6f);
			towerRight.GetComponent<Renderer>().material.color = new Color(0.17f, 0.44f, 0.63f);
			return goal.transform;
		}

		private void BuildCenterCrystal(Transform miscRoot)
		{
			if (presentationConfig != null && presentationConfig.centerCrystalPrefab != null)
			{
				GameObject overrideCrystal = UnityEngine.Object.Instantiate(presentationConfig.centerCrystalPrefab, miscRoot);
				overrideCrystal.name = "DefenseCrystal";
				overrideCrystal.transform.localPosition = new Vector3(0f, 1.2f, -6.7f);
				overrideCrystal.transform.localRotation = Quaternion.identity;
				return;
			}
			GameObject crystal = GameObject.CreatePrimitive(PrimitiveType.Capsule);
			crystal.name = "DefenseCrystal";
			crystal.transform.SetParent(miscRoot);
			crystal.transform.position = new Vector3(0f, 1.2f, -6.7f);
			crystal.transform.localScale = new Vector3(0.8f, 1.3f, 0.8f);
			crystal.GetComponent<Renderer>().material.color = GetConfigColor((GamePresentationConfig config) => config.crystalColor, new Color(0.3f, 0.95f, 0.86f));
			GameObject baseRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
			baseRing.name = "CrystalRing";
			baseRing.transform.SetParent(crystal.transform);
			baseRing.transform.localPosition = new Vector3(0f, -0.85f, 0f);
			baseRing.transform.localScale = new Vector3(1.5f, 0.06f, 1.5f);
			baseRing.GetComponent<Renderer>().material.color = new Color(0.18f, 0.44f, 0.59f);
		}

		private void BuildFlankTowers(Transform miscRoot)
		{
			BuildTower(miscRoot, "WestTower", new Vector3(-7.8f, 0f, -6.2f), GetSlotColor(1), GetSlotColor(5));
			BuildTower(miscRoot, "EastTower", new Vector3(7.8f, 0f, -6.2f), GetSlotColor(2), GetSlotColor(7));
		}

		private void BuildTower(Transform parent, string name, Vector3 position, Color baseColor, Color topColor)
		{
			if (presentationConfig != null && presentationConfig.flankTowerPrefab != null)
			{
				GameObject overrideTower = UnityEngine.Object.Instantiate(presentationConfig.flankTowerPrefab, parent);
				overrideTower.name = name;
				overrideTower.transform.localPosition = position;
				overrideTower.transform.localRotation = Quaternion.identity;
				return;
			}
			GameObject towerRoot = new GameObject(name);
			towerRoot.transform.SetParent(parent);
			towerRoot.transform.position = position;
			GameObject basePart = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
			basePart.name = "Base";
			basePart.transform.SetParent(towerRoot.transform);
			basePart.transform.localPosition = new Vector3(0f, 0.9f, 0f);
			basePart.transform.localScale = new Vector3(0.8f, 0.9f, 0.8f);
			basePart.GetComponent<Renderer>().material.color = baseColor;
			GameObject topPart = GameObject.CreatePrimitive(PrimitiveType.Cube);
			topPart.name = "Top";
			topPart.transform.SetParent(towerRoot.transform);
			topPart.transform.localPosition = new Vector3(0f, 2.25f, 0f);
			topPart.transform.localScale = new Vector3(1.15f, 0.5f, 1.15f);
			topPart.GetComponent<Renderer>().material.color = topColor;
			GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			orb.name = "Orb";
			orb.transform.SetParent(towerRoot.transform);
			orb.transform.localPosition = new Vector3(0f, 2.85f, 0f);
			orb.transform.localScale = Vector3.one * 0.48f;
			orb.GetComponent<Renderer>().material.color = Color.Lerp(topColor, Color.white, 0.35f);
		}

		private void BuildSkyOrnaments(Transform miscRoot)
		{
			for (int i = 0; i < 5; i++)
			{
				if (presentationConfig != null && presentationConfig.skyAccentPrefab != null)
				{
					GameObject overrideOrb = UnityEngine.Object.Instantiate(presentationConfig.skyAccentPrefab, miscRoot);
					overrideOrb.name = "SkyOrb_" + i.ToString("D2");
					overrideOrb.transform.localPosition = new Vector3(-8f + (float)i * 4f, 4.8f + (float)(i % 2) * 0.6f, 6.2f - (float)i * 1.3f);
					overrideOrb.transform.localRotation = Quaternion.identity;
				}
				else
				{
					GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
					orb.name = "SkyOrb_" + i.ToString("D2");
					orb.transform.SetParent(miscRoot);
					orb.transform.position = new Vector3(-8f + (float)i * 4f, 4.8f + (float)(i % 2) * 0.6f, 6.2f - (float)i * 1.3f);
					orb.transform.localScale = Vector3.one * (0.35f + (float)i * 0.05f);
					orb.GetComponent<Renderer>().material.color = GetLaneColor(i) * 0.95f;
				}
			}
		}

		private Projectile BuildProjectileTemplate(Transform templateRoot)
		{
			GameObject projectileObject = CreateTemplateObject(templateRoot, (presentationConfig != null) ? presentationConfig.projectilePrefab : null, PrimitiveType.Sphere, "ProjectileTemplate", Vector3.one * 0.25f);
			Renderer renderer = projectileObject.GetComponentInChildren<Renderer>();
			if (renderer != null)
			{
				renderer.material.color = new Color(1f, 0.85f, 0.3f);
			}
			Projectile projectile = projectileObject.GetComponent<Projectile>();
			if (projectile == null)
			{
				projectile = projectileObject.AddComponent<Projectile>();
			}
			projectileObject.SetActive(value: false);
			return projectile;
		}

		private DefenderUnit BuildDefenderTemplate(Transform templateRoot, Projectile projectileTemplate)
		{
			GameObject unitObject = CreateTemplateObject(templateRoot, (presentationConfig != null) ? presentationConfig.defaultDefenderPrefab : null, PrimitiveType.Capsule, "DefenderTemplate", new Vector3(0.8f, 1f, 0.8f));
			DefenderUnit unit = unitObject.GetComponent<DefenderUnit>();
			if (unit == null)
			{
				unit = unitObject.AddComponent<DefenderUnit>();
			}
			Transform firePoint = unitObject.transform.Find("FirePoint");
			if (firePoint == null)
			{
				GameObject firePointObject = new GameObject("FirePoint");
				firePointObject.transform.SetParent(unitObject.transform);
				firePointObject.transform.localPosition = new Vector3(0f, 0.8f, 0.6f);
				firePoint = firePointObject.transform;
			}
			unit.ConfigureRuntimePieces(projectileTemplate, firePoint, unitObject.GetComponentsInChildren<Renderer>(includeInactive: true), (presentationConfig != null) ? presentationConfig.summonedDefenderPrefab : null, (presentationConfig != null) ? presentationConfig.defaultMuzzleEffectPrefab : null, (presentationConfig != null) ? presentationConfig.defaultHitEffectPrefab : null, (presentationConfig != null) ? presentationConfig.defaultAreaEffectPrefab : null, (presentationConfig != null) ? presentationConfig.defenderDeathEffectPrefab : null, (presentationConfig != null) ? presentationConfig.diceAutoDormantEffectPrefab : null);
			unitObject.SetActive(value: false);
			return unit;
		}

		private MonsterUnit BuildMonsterTemplate(Transform templateRoot)
		{
			GameObject monsterObject = CreateTemplateObject(templateRoot, (presentationConfig != null) ? presentationConfig.defaultMonsterPrefab : null, PrimitiveType.Cube, "MonsterTemplate", Vector3.one);
			MonsterUnit monster = monsterObject.GetComponent<MonsterUnit>();
			if (monster == null)
			{
				monster = monsterObject.AddComponent<MonsterUnit>();
			}
			monster.ConfigureRuntimePieces((presentationConfig != null) ? presentationConfig.monsterDeathEffectPrefab : null, monsterObject.GetComponentsInChildren<Renderer>(includeInactive: true));
			monsterObject.SetActive(value: false);
			return monster;
		}

		private GameObject CreateTemplateObject(Transform parent, GameObject prefab, PrimitiveType fallbackPrimitive, string name, Vector3 scale)
		{
			GameObject instance;
			if (prefab != null)
			{
				instance = UnityEngine.Object.Instantiate(prefab, parent);
				instance.name = name;
			}
			else
			{
				instance = GameObject.CreatePrimitive(fallbackPrimitive);
				instance.name = name;
				instance.transform.SetParent(parent);
				instance.transform.localScale = scale;
			}
			instance.transform.localPosition = Vector3.zero;
			instance.transform.localRotation = Quaternion.identity;
			if (prefab == null)
			{
				instance.transform.localScale = scale;
			}
			return instance;
		}

		private void BuildCanvas(Transform root, SimpleGameHUD hud, DefenseGameController gameController, DefenseBoardManager boardManager, GameUIButtonBinder binder, AugmentManager augmentManager, CharacterCollectionUI collectionUI, MetaFlowUI metaFlowUI, BoardSynergySystem synergySystem, TacticalMissionSystem missionSystem, BoardTileModifierSystem tileModifierSystem, RunShopSystem runShopSystem, CharacterDatabase characterDatabase, OutgameProgressionSystem outgameProgression)
		{
            Transform existingCanvasTransform = base.transform.Find("RuntimeCanvas");
			if (existingCanvasTransform != null)
			{
				SafeDestroy(existingCanvasTransform.gameObject);
			}
			EnsureEventSystem();
			GameObject canvasObject = new GameObject("RuntimeCanvas", typeof(RectTransform));
			canvasObject.SetActive(value: false);
            // The canvas is meta/UI infrastructure. Keep it outside RuntimeStageRoot so
            // outgame pages remain available while the 3D combat stage is inactive.
            canvasObject.transform.SetParent(base.transform, worldPositionStays: false);
			Canvas canvas = canvasObject.AddComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
			scaler.uiScaleMode = (CanvasScaler.ScaleMode)1;
			scaler.screenMatchMode = (CanvasScaler.ScreenMatchMode)1;
			scaler.referenceResolution = new Vector2(1080f, 1920f);
			scaler.matchWidthOrHeight = 0.84f;
			canvasObject.AddComponent<GraphicRaycaster>();
			canvasObject.AddComponent<RuntimeKoreanTextCleaner>();
            Image topFullBleedBackdrop = CreatePanel(canvas.transform, "TopFullBleedBackdrop", Vector2.zero, new Vector2(0f, 160f), new Color(0.03f, 0.05f, 0.17f, 1f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), rounded: false, shadow: false);
            topFullBleedBackdrop.raycastTarget = false;
            topFullBleedBackdrop.transform.SetSiblingIndex(0);
			Transform hudRoot = CreateSafeAreaRoot(canvas.transform);
			Transform metaFlowRoot = CreateSafeAreaRoot(canvas.transform, "MetaFlowSafeAreaRoot");
			Font font = RuntimeUiSkinUtility.ResolveFont(presentationConfig);
			Color textColor = ((presentationConfig != null) ? presentationConfig.hudTextColor : Color.white);
			string hintValue = string.Empty;
			CreatePanel(hudRoot, "TopSafeBackdrop", new Vector2(0f, -12f), new Vector2(0f, 232f), new Color(0.03f, 0.05f, 0.17f, 0.74f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), rounded: false, shadow: true);
			CreatePanel(hudRoot, "TopGlow", new Vector2(0f, -224f), new Vector2(0f, 8f), new Color(0.17f, 0.42f, 0.72f, 0.35f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), rounded: false, shadow: false);
			Image playerPanel = CreatePanel(hudRoot, "PlayerBadge", new Vector2(28f, -28f), new Vector2(276f, 88f), new Color(0.93f, 0.74f, 0.27f, 0.96f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), rounded: true, shadow: true);
			CreatePanel(((Component)(object)playerPanel).transform, "PlayerIcon", new Vector2(24f, -16f), new Vector2(62f, 62f), new Color(0.66f, 0.46f, 0.14f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), rounded: true, shadow: false);
			Text playerName = CreateText(((Component)(object)playerPanel).transform, font, Color.white, "PlayerName", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(104f, -14f), new Vector2(148f, 32f), "레드X", 24, TextAnchor.MiddleLeft, bold: true);
			Text rank = CreateText(((Component)(object)playerPanel).transform, font, new Color(0.16f, 0.22f, 0.35f), "RankText", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(104f, -50f), new Vector2(150f, 26f), "RANK 1", 18, TextAnchor.MiddleLeft, bold: true);
			Text life = null;
			Text gold = CreateCurrencyPill(hudRoot, font, "GoldPill", "G", new Vector2(-90f, -36f), new Vector2(190f, 68f), new Color(1f, 0.76f, 0.22f), "0");
			Image lifeProgressBack = CreatePanel(hudRoot, "LifeProgressBar", new Vector2(188f, -36f), new Vector2(330f, 68f), new Color(0.08f, 0.12f, 0.28f, 0.96f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: true);
			CreatePanel(((Component)(object)lifeProgressBack).transform, "LifeProgressGlow", Vector2.zero, new Vector2(-18f, -18f), new Color(0.2f, 1f, 0.48f, 0.13f), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), rounded: true, shadow: false);
			Image lifeProgressTrack = CreatePanel(((Component)(object)lifeProgressBack).transform, "LifeProgressTrack", Vector2.zero, new Vector2(-14f, -14f), new Color(0.035f, 0.07f, 0.15f, 1f), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), rounded: true, shadow: false);
			Mask lifeProgressMask = ((Component)(object)lifeProgressTrack).gameObject.AddComponent<Mask>();
			lifeProgressMask.showMaskGraphic = true;
			Image lifeProgressFill = CreatePanel(((Component)(object)lifeProgressTrack).transform, "Fill", Vector2.zero, Vector2.zero, new Color(0.2f, 0.9f, 0.36f, 1f), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), rounded: true, shadow: false);
			lifeProgressFill.type = (Image.Type)1;
			lifeProgressFill.fillAmount = 1f;
			Text content = CreateText(((Component)(object)lifeProgressBack).transform, font, Color.white, "TopHpText", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, "HP 10/10", 28, TextAnchor.MiddleCenter, bold: true);
			AddStrongTextOutline(content);
			Image optionsMenu = CreatePanel(hudRoot, "OptionsMenu", new Vector2(-34f, -112f), new Vector2(274f, 322f), new Color(0.06f, 0.08f, 0.24f, 0.98f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), rounded: true, shadow: true);
			Canvas optionsCanvas = ((Component)(object)optionsMenu).gameObject.AddComponent<Canvas>();
			optionsCanvas.overrideSorting = true;
			optionsCanvas.sortingOrder = 200;
			((Component)(object)optionsMenu).gameObject.AddComponent<GraphicRaycaster>();
			Text state = null;
			Text optionsLabel;
			Button optionsButton = CreateButton(hudRoot, font, "OptionsButton", string.Empty, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-38f, -36f), new Vector2(76f, 64f), new Color(0.08f, 0.14f, 0.31f, 0.96f), Color.white, null, out optionsLabel);
			optionsLabel.fontSize = 19;
			optionsLabel.resizeTextForBestFit = true;
			optionsLabel.resizeTextMinSize = 12;
			optionsLabel.resizeTextMaxSize = 19;
			((Behaviour)(object)optionsLabel).enabled = false;
			BuildHamburgerIcon(((Component)(object)optionsButton).transform);
			((UnityEvent)(object)optionsButton.onClick).AddListener((UnityAction)delegate
			{
				bool open = !((Component)(object)optionsMenu).gameObject.activeSelf;
				((Component)(object)optionsMenu).gameObject.SetActive(open);
			});
			((Component)(object)optionsMenu).gameObject.SetActive(value: false);
			Image mergeStrip = CreatePanel(hudRoot, "MergeResultStrip", new Vector2(-80f, -116f), new Vector2(865f, 30f), new Color(0.1f, 0.12f, 0.3f, 0.72f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: true);
			Text mergeResult = CreateText(((Component)(object)mergeStrip).transform, font, new Color(1f, 0.89f, 0.36f), "MergeResultText", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, string.Empty, 17, TextAnchor.MiddleCenter, bold: true);
			((Component)(object)mergeStrip).gameObject.SetActive(value: false);
			Image bottomPanel = CreatePanel(hudRoot, "BottomCommandDock", new Vector2(0f, 0f), new Vector2(0f, 340f), new Color(0.05f, 0.06f, 0.18f, 0.86f), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), rounded: false, shadow: true);
			CreatePanel(((Component)(object)bottomPanel).transform, "DockTopLine", new Vector2(0f, 334f), new Vector2(0f, 8f), new Color(0.37f, 0.85f, 1f, 0.42f), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), rounded: false, shadow: false);
			Text deckSummary = CreateText(hudRoot, font, textColor, "DeckSummaryText", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(42f, 300f), new Vector2(250f, 30f), "보유 유닛 0 / 0", 21, TextAnchor.MiddleLeft, bold: true);
			Text capacity = CreateText(hudRoot, font, new Color(0.75f, 0.91f, 1f), "CapacityText", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-42f, 300f), new Vector2(204f, 30f), "0칸 남음", 19, TextAnchor.MiddleRight, bold: true);
			Text round = CreateText(hudRoot, font, textColor, "RoundText", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-214f, 294f), new Vector2(176f, 30f), "ROUND 1", 19, TextAnchor.MiddleCenter, bold: true);
			Image roundProgressFill = CreateProgressBar(hudRoot, new Vector2(130f, 294f), new Vector2(438f, 28f));
			Text bossRoundHud = CreateText(((Component)(object)roundProgressFill).transform.parent, font, new Color(0.76f, 0.94f, 1f), "BossRoundText", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, "다음 보스 ROUND 10", 16, TextAnchor.MiddleCenter, bold: true);
			bossRoundHud.resizeTextForBestFit = true;
			bossRoundHud.fontSize = 18;
			bossRoundHud.resizeTextMinSize = 14;
			bossRoundHud.resizeTextMaxSize = 18;
			AddStrongTextOutline(bossRoundHud);
			Text board = CreateText(hudRoot, font, Color.white, "BoardText", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(396f, 294f), new Vector2(126f, 28f), "0 / 0", 17, TextAnchor.MiddleCenter, bold: true);
			UltimateRecipeSelectionUI ultimateRecipeSelection = null;
			Text normalCount = CreateGradeCard(hudRoot, font, CharacterGrade.Normal, new Vector2(-380f, 126f), binder.OnClickMergeNormal, "재료 3개");
			Text rareCount = CreateGradeCard(hudRoot, font, CharacterGrade.Rare, new Vector2(-228f, 126f), binder.OnClickMergeRare, "재료 3개");
			Text epicCount = CreateGradeCard(hudRoot, font, CharacterGrade.Epic, new Vector2(-76f, 126f), binder.OnClickMergeEpic, "재료 3개");
			Text legendaryCount = CreateGradeCard(hudRoot, font, CharacterGrade.Legendary, new Vector2(76f, 126f), binder.OnClickMergeLegendary, "재료 3개");
			Text mythicCount = CreateGradeCard(hudRoot, font, CharacterGrade.Mythic, new Vector2(228f, 126f), null, "초월 재료");
			Text transcendentCount = CreateGradeCard(hudRoot, font, CharacterGrade.Transcendent, new Vector2(380f, 126f), delegate
			{
				if (ultimateRecipeSelection != null)
				{
					ultimateRecipeSelection.Open();
				}
			}, "레시피 선택");
			Text summonLabel;
			Button summonButton = CreateButton(hudRoot, font, "SummonButton", "소환", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(54f, 26f), new Vector2(226f, 88f), new Color(0.19f, 0.78f, 0.42f, 1f), Color.white, binder.OnClickSummon, out summonLabel);
			Image luckySummonBadge = CreatePanel(((Component)(object)summonButton).transform, "LuckySummonProgressBadge", new Vector2(0f, -3f), new Vector2(194f, 26f), new Color(0.07f, 0.2f, 0.17f, 0.96f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
			Text luckySummonProgress = CreateText(((Component)(object)luckySummonBadge).transform, font, new Color(0.94f, 1f, 0.78f), "LuckySummonProgressText", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-12f, -4f), string.Empty, 17, TextAnchor.MiddleCenter, bold: true);
			luckySummonProgress.resizeTextForBestFit = true;
			luckySummonProgress.resizeTextMinSize = 14;
			luckySummonProgress.resizeTextMaxSize = 17;
			((Component)(object)luckySummonBadge).gameObject.SetActive(value: false);
			Text ultimateRecipeHud = null;
			Image buildReadoutPanel = CreatePanel(hudRoot, "BuildReadoutPanel", new Vector2(0f, 630f), new Vector2(920f, 96f), new Color(0.04f, 0.08f, 0.24f, 0.78f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), rounded: true, shadow: true);
			Text synergyInsight = CreateBuildInsightCell(((Component)(object)buildReadoutPanel).transform, font, "DangerInsight", "위험", new Vector2(-292f, 0f), new Color(1f, 0.48f, 0.3f));
			Text recipeInsight = CreateBuildInsightCell(((Component)(object)buildReadoutPanel).transform, font, "ActionInsight", "추천 행동", Vector2.zero, new Color(0.36f, 0.92f, 1f));
			Text tileInsight = CreateBuildInsightCell(((Component)(object)buildReadoutPanel).transform, font, "DealerInsight", "핵심 딜러", new Vector2(292f, 0f), new Color(1f, 0.76f, 0.26f));
			GradeUpgradeBarUI.Create(hudRoot, font, gameController, presentationConfig);
			Image fatePanel = CreatePanel(hudRoot, "FateInterventionPanel", new Vector2(0f, 434f), new Vector2(1000f, 560f), new Color(0.06f, 0.05f, 0.18f, 0.98f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), rounded: true, shadow: true);
			CanvasGroup fatePanelCanvasGroup = ((Component)(object)fatePanel).gameObject.AddComponent<CanvasGroup>();
			CreatePanel(((Component)(object)fatePanel).transform, "FateAccent", new Vector2(-488f, 0f), new Vector2(12f, 516f), new Color(1f, 0.3f, 0.88f, 0.96f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), rounded: true, shadow: false);
			CreateText(((Component)(object)fatePanel).transform, font, new Color(1f, 0.74f, 0.96f), "FatePanelTitle", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 228f), new Vector2(620f, 42f), "마지막 계약 · 위기 탈출 카드", 30, TextAnchor.MiddleCenter, bold: true);
			Image fateGaugeFill = CreateProgressBar(((Component)(object)fatePanel).transform, new Vector2(-350f, 148f), new Vector2(220f, 16f));
			((Graphic)fateGaugeFill).color = new Color(1f, 0.36f, 0.92f, 0.96f);
			Text fateGaugeText = CreateText(((Component)(object)fatePanel).transform, font, Color.white, "FateGaugeText", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-350f, 120f), new Vector2(264f, 28f), "마지막 계약 1/1", 18, TextAnchor.MiddleCenter, bold: true);
			fateGaugeText.resizeTextForBestFit = true;
			fateGaugeText.resizeTextMinSize = 16;
			fateGaugeText.resizeTextMaxSize = 20;
			Text fateDebtText = CreateText(((Component)(object)fatePanel).transform, font, new Color(1f, 0.82f, 0.42f), "FateDebtText", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-350f, 90f), new Vector2(248f, 24f), "카드 보유 1/1", 16, TextAnchor.MiddleCenter, bold: true);
			Text fateCostBenefit = CreateText(((Component)(object)fatePanel).transform, font, new Color(0.84f, 0.94f, 1f), "FateCostBenefitText", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(90f, 174f), new Vector2(620f, 34f), "전투는 0.1배로 흐릅니다 · 3장 중 1장 선택", 19, TextAnchor.MiddleCenter, bold: true);
			fateCostBenefit.resizeTextForBestFit = true;
			fateCostBenefit.resizeTextMinSize = 16;
			fateCostBenefit.resizeTextMaxSize = 19;
			Text earlyRunInsight = null;
			Text fateSurvivalLabel;
			Button fateSurvivalButton = CreateButton(((Component)(object)fatePanel).transform, font, "FateChoiceCard0", "운명 카드\n선택 1", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-320f, -52f), new Vector2(300f, 250f), new Color(0.7f, 0.24f, 1f, 0.98f), Color.white, delegate
			{
				gameController.TryActivateFateSurvival();
			}, out fateSurvivalLabel);
			Text fateGradeLockLabel;
			Button fateGradeLockButton = CreateButton(((Component)(object)fatePanel).transform, font, "FateChoiceCard1", "운명 카드\n선택 2", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -52f), new Vector2(300f, 250f), new Color(0.18f, 0.68f, 1f, 0.96f), Color.white, delegate
			{
				gameController.TryActivateFateGradeLock(CharacterGrade.Rare, 3);
			}, out fateGradeLockLabel);
			Text fateNormalBanLabel;
			Button fateNormalBanButton = CreateButton(((Component)(object)fatePanel).transform, font, "FateChoiceCard2", "운명 카드\n선택 3", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(320f, -52f), new Vector2(300f, 250f), new Color(1f, 0.34f, 0.24f, 0.96f), Color.white, delegate
			{
				gameController.TryActivateFateNormalBan(4);
			}, out fateNormalBanLabel);
			Text fateForceShopLabel;
			Button fateForceShopButton = CreateButton(((Component)(object)fatePanel).transform, font, "FateUnusedHiddenCard", string.Empty, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(9999f, 0f), new Vector2(1f, 1f), new Color(0f, 0f, 0f, 0f), Color.white, null, out fateForceShopLabel);
			((Component)(object)fateForceShopButton).gameObject.SetActive(value: false);
			fateSurvivalLabel.fontSize = 22;
			fateGradeLockLabel.fontSize = 22;
			fateNormalBanLabel.fontSize = 22;
			fateForceShopLabel.fontSize = 1;
			fateSurvivalLabel.resizeTextForBestFit = true;
			fateGradeLockLabel.resizeTextForBestFit = true;
			fateNormalBanLabel.resizeTextForBestFit = true;
			fateForceShopLabel.resizeTextForBestFit = true;
			fateSurvivalLabel.resizeTextMinSize = 18;
			fateGradeLockLabel.resizeTextMinSize = 18;
			fateNormalBanLabel.resizeTextMinSize = 18;
			fateForceShopLabel.resizeTextMinSize = 1;
			fateSurvivalLabel.resizeTextMaxSize = 22;
			fateGradeLockLabel.resizeTextMaxSize = 22;
			fateNormalBanLabel.resizeTextMaxSize = 22;
			fateForceShopLabel.resizeTextMaxSize = 1;
			Text fatePanelReopenLabel;
			Button fatePanelReopenButton = CreateButton(hudRoot, font, "FatePanelReopenButton", "\uc6b4\uba85 \uce74\ub4dc\n\uaebc\ub0b4\uae30", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-150f, 478f), new Vector2(238f, 82f), new Color(0.4f, 0.21f, 0.85f, 0.98f), new Color(1f, 0.98f, 0.94f, 1f), null, out fatePanelReopenLabel);
			fatePanelReopenLabel.fontSize = 18;
			fatePanelReopenLabel.resizeTextForBestFit = true;
			fatePanelReopenLabel.resizeTextMinSize = 13;
			fatePanelReopenLabel.resizeTextMaxSize = 18;
			RectTransform fateEntryRect = ((Component)(object)fatePanelReopenButton).GetComponent<RectTransform>();
			fateEntryRect.sizeDelta = new Vector2(238f, 82f);
			fateEntryRect.anchoredPosition = new Vector2(-150f, 478f);
			fatePanelReopenLabel.fontSize = 28;
			fatePanelReopenLabel.resizeTextMinSize = 22;
			fatePanelReopenLabel.resizeTextMaxSize = 28;
			Shadow fateEntryShadow = ((Component)(object)fatePanelReopenButton).GetComponent<Shadow>();
			if ((UnityEngine.Object)(object)fateEntryShadow != null)
			{
				fateEntryShadow.effectDistance = new Vector2(0f, -4f);
				fateEntryShadow.useGraphicAlpha = true;
			}
			Outline fateEntryOutline = ((Component)(object)fatePanelReopenButton).gameObject.AddComponent<Outline>();
			((Shadow)fateEntryOutline).effectColor = new Color(1f, 0.78f, 0.34f, 0.94f);
			((Shadow)fateEntryOutline).effectDistance = new Vector2(2f, -2f);
			((Shadow)fateEntryOutline).useGraphicAlpha = true;
			((Component)(object)fatePanelReopenButton).gameObject.SetActive(value: false);
			Text summonCost = CreateText(((Component)(object)summonButton).transform, font, new Color(1f, 0.9f, 0.42f), "SummonCostText", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 7f), new Vector2(0f, 22f), "10 GOLD", 18, TextAnchor.MiddleCenter, bold: true);
			AddStrongTextOutline(summonCost);
			summonLabel.fontSize = 31;
			summonLabel.alignment = TextAnchor.MiddleCenter;
			((Graphic)summonLabel).rectTransform.anchorMin = new Vector2(0f, 0.28f);
			((Graphic)summonLabel).rectTransform.anchorMax = new Vector2(1f, 0.68f);
			((Graphic)summonLabel).rectTransform.pivot = new Vector2(0.5f, 0.5f);
			((Graphic)summonLabel).rectTransform.anchoredPosition = new Vector2(0f, 4f);
			((Graphic)summonLabel).rectTransform.sizeDelta = Vector2.zero;
			Text battleLabel;
			Button battleButton = CreateButton(hudRoot, font, "BattleButton", "전투 시작", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-4f, 26f), new Vector2(340f, 88f), new Color(0.94f, 0.32f, 0.24f, 1f), Color.white, null, out battleLabel);
			battleLabel.fontSize = 36;
			CreateText(((Component)(object)optionsMenu).transform, font, new Color(0.72f, 0.92f, 1f), "OptionsHeader", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(220f, 34f), "설정", 25, TextAnchor.MiddleCenter, bold: true);
			Text soundToggleLabel;
			Button soundToggleButton = CreateButton(((Component)(object)optionsMenu).transform, font, "SoundToggleButton", "사운드 ON", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -82f), new Vector2(222f, 50f), new Color(0.2f, 0.7f, 0.86f, 0.95f), Color.white, null, out soundToggleLabel);
			Text volumeLabel;
			Button volumeButton = CreateButton(((Component)(object)optionsMenu).transform, font, "VolumeButton", "음량 100%", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -142f), new Vector2(222f, 50f), new Color(0.18f, 0.48f, 0.9f, 0.95f), Color.white, null, out volumeLabel);
			Text languageLabel;
			Button languageButton = CreateButton(((Component)(object)optionsMenu).transform, font, "LanguageButton", "언어 한국어", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -202f), new Vector2(222f, 50f), new Color(0.44f, 0.36f, 0.86f, 0.95f), Color.white, null, out languageLabel);
			Text labelText;
			Button lobbyButton = CreateButton(((Component)(object)optionsMenu).transform, font, "LobbyButton", "나가기", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -266f), new Vector2(222f, 54f), new Color(0.86f, 0.34f, 0.24f, 0.95f), Color.white, null, out labelText);
			Button loadoutButton = null;
			Button infoButton = null;
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
			((UnityEvent)(object)soundToggleButton.onClick).AddListener((UnityAction)delegate
			{
				AudioListener.volume = ((AudioListener.volume <= 0.001f) ? optionVolumeSteps[Mathf.Clamp(optionVolumeIndex, 0, optionVolumeSteps.Length - 2)] : 0f);
				refreshOptionLabels();
			});
			((UnityEvent)(object)volumeButton.onClick).AddListener((UnityAction)delegate
			{
				optionVolumeIndex = (optionVolumeIndex + 1) % optionVolumeSteps.Length;
				AudioListener.volume = optionVolumeSteps[optionVolumeIndex];
				refreshOptionLabels();
			});
			((UnityEvent)(object)languageButton.onClick).AddListener((UnityAction)delegate
			{
				refreshOptionLabels();
			});
			Image unitSellPanel = CreatePanel(hudRoot, "SelectedUnitSellPanel", new Vector2(0f, 450f), new Vector2(820f, 84f), new Color(0.1f, 0.08f, 0.2f, 0.94f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), rounded: true, shadow: true);
			CreatePanel(((Component)(object)unitSellPanel).transform, "SellAccent", new Vector2(18f, 0f), new Vector2(10f, 58f), new Color(1f, 0.58f, 0.24f, 0.95f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), rounded: true, shadow: false);
			Text unitSellTitle = CreateText(((Component)(object)unitSellPanel).transform, font, Color.white, "SellTitle", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(44f, -14f), new Vector2(-254f, 30f), "선택 유닛", 22, TextAnchor.MiddleLeft, bold: true);
			Text unitSellDetail = CreateText(((Component)(object)unitSellPanel).transform, font, new Color(0.82f, 0.92f, 1f), "SellDetail", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(44f, 16f), new Vector2(-254f, 36f), "판매가 확인 중", 17, TextAnchor.MiddleLeft, bold: false);
			unitSellDetail.resizeTextForBestFit = true;
			unitSellDetail.resizeTextMinSize = 13;
			unitSellDetail.resizeTextMaxSize = 17;
			Text unitSellButtonLabel;
			Button unitSellButton = CreateButton(((Component)(object)unitSellPanel).transform, font, "SellSelectedUnitButton", "판매", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-24f, 0f), new Vector2(206f, 62f), new Color(0.92f, 0.38f, 0.22f, 0.98f), Color.white, null, out unitSellButtonLabel);
			unitSellButtonLabel.fontSize = 23;
			((Component)(object)unitSellPanel).gameObject.SetActive(value: false);
			Text hint = null;
			Text countdown = CreateText(hudRoot, font, new Color(1f, 0.95f, 0.58f, 0f), "CountdownText", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 35f), new Vector2(220f, 120f), string.Empty, 96, TextAnchor.MiddleCenter, bold: true);
			Text roundBanner = CreateText(hudRoot, font, new Color(0.48f, 1f, 0.72f, 0f), "RoundBannerText", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 136f), new Vector2(620f, 70f), string.Empty, 40, TextAnchor.MiddleCenter, bold: true);
			Text mergeCelebration = CreateText(hudRoot, font, new Color(1f, 0.92f, 0.5f, 0f), "MergeCelebrationText", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 210f), new Vector2(720f, 76f), string.Empty, 52, TextAnchor.MiddleCenter, bold: true);
			Text mergeCelebrationSub = CreateText(hudRoot, font, new Color(1f, 0.98f, 0.9f, 0f), "MergeCelebrationSubText", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 154f), new Vector2(820f, 42f), string.Empty, 25, TextAnchor.MiddleCenter, bold: true);
			BuildSynergyPanelExpanded(hudRoot, font, synergySystem, gameController, boardManager);
			BuildTacticalMissionPanel(hudRoot, hudRoot, font, missionSystem, gameController, boardManager);
			BuildRunShopPanel(hudRoot, font, runShopSystem, gameController, boardManager, tileModifierSystem, augmentManager);
			BuildAugmentPanel(hudRoot, font, augmentManager, gameController);
			if (collectionUI != null)
			{
				collectionUI.Configure(characterDatabase, outgameProgression, font, metaFlowRoot, (presentationConfig != null) ? presentationConfig.uiSkin : null);
			}
			if (metaFlowUI != null)
			{
				metaFlowUI.Configure(gameController, binder, augmentManager, characterDatabase, outgameProgression, collectionUI, font, metaFlowRoot, hudRoot.gameObject, battleButton, lobbyButton, loadoutButton, (presentationConfig != null) ? presentationConfig.uiSkin : null);
				if ((UnityEngine.Object)(object)infoButton != null)
				{
					((UnityEventBase)(object)infoButton.onClick).RemoveAllListeners();
					((UnityEvent)(object)infoButton.onClick).AddListener((UnityAction)metaFlowUI.ToggleCollectionPanel);
				}
			}
			else if (collectionUI != null && (UnityEngine.Object)(object)infoButton != null)
			{
				((UnityEvent)(object)infoButton.onClick).AddListener((UnityAction)collectionUI.Toggle);
			}
			CanvasGroup bossWarningGroup;
			Text bossWarningTitle;
			Text bossWarningSub;
			GameObject bossWarningPanel = BuildBossWarningPanel(hudRoot, font, out bossWarningGroup, out bossWarningTitle, out bossWarningSub);
			ultimateRecipeSelection = BuildUltimateRecipeSelectionPanel(hudRoot, font, gameController);
			BuildLuckySummonChoicePanel(hudRoot, font, gameController);
			BuildBossForecastBetPanel(hudRoot, font, gameController);
			hud.Configure(gameController, gold, life, round, board, content, hint, mergeResult, mergeCelebration, mergeCelebrationSub, countdown, roundBanner, hintValue, playerName, rank, state, battleLabel, summonLabel, summonCost, deckSummary, capacity, normalCount, rareCount, epicCount, legendaryCount, mythicCount, transcendentCount, ultimateRecipeHud, bossRoundHud, synergyInsight, recipeInsight, tileInsight, null, earlyRunInsight, fateGaugeText, fateGaugeFill, fateDebtText, fateCostBenefit, fateGradeLockButton, fateGradeLockLabel, fateNormalBanButton, fateNormalBanLabel, fateForceShopButton, fateForceShopLabel, fateSurvivalButton, fateSurvivalLabel, ((Component)(object)fatePanel).gameObject, fatePanelCanvasGroup, fatePanelReopenButton, fatePanelReopenLabel, roundProgressFill, battleButton, summonButton, bossWarningPanel, bossWarningGroup, bossWarningTitle, bossWarningSub, boardManager, ((Component)(object)unitSellPanel).gameObject, unitSellTitle, unitSellDetail, unitSellButton, unitSellButtonLabel, lifeProgressFill, luckySummonProgress);
			canvasObject.SetActive(value: true);
		}

		private BossForecastBetUI BuildBossForecastBetPanel(Transform parent, Font font, DefenseGameController gameController)
		{
			GameObject root = new GameObject("BossForecastBetOverlay", typeof(RectTransform));
			root.transform.SetParent(parent, worldPositionStays: false);
			RectTransform rootRect = root.GetComponent<RectTransform>();
			rootRect.anchorMin = Vector2.zero;
			rootRect.anchorMax = Vector2.one;
			rootRect.offsetMin = Vector2.zero;
			rootRect.offsetMax = Vector2.zero;
			Image backdrop = root.AddComponent<Image>();
			backdrop.color = new Color(0.01f, 0.02f, 0.07f, 0.86f);
			backdrop.raycastTarget = true;
			CanvasGroup group = root.AddComponent<CanvasGroup>();

			Image panel = CreatePanel(root.transform, "BossForecastBetPanel", new Vector2(0f, 72f), new Vector2(980f, 690f), new Color(0.04f, 0.08f, 0.19f, 0.99f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), rounded: true, shadow: true);
			CreatePanel(panel.transform, "ForecastTopLine", new Vector2(0f, -6f), new Vector2(870f, 8f), new Color(1f, 0.68f, 0.20f, 0.96f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
			CreateText(panel.transform, font, new Color(1f, 0.86f, 0.42f), "BossForecastTitle", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -58f), new Vector2(760f, 50f), "R10 \ubcf4\uc2a4 \ub300\ube44", 35, TextAnchor.MiddleCenter, bold: true);
			Text instruction = CreateText(panel.transform, font, new Color(0.82f, 0.91f, 1f), "BossForecastInstruction", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -118f), new Vector2(850f, 62f), "R10\uc744 \uc5b4\ub5bb\uac8c \uc900\ube44\ud560\uae4c\uc694?\n\uc9c0\uae08 \ubcf4\ub108\uc2a4\ub97c \ud558\ub098 \ubc1b\uace0, \ubaa9\ud45c\uae4c\uc9c0 \ub2ec\uc131\ud558\uba74 \ucd94\uac00 \ubcf4\uc0c1\uc744 \ubc1b\uc2b5\ub2c8\ub2e4.", 20, TextAnchor.MiddleCenter, bold: false);

			Button[] buttons = new Button[3];
			Text[] labels = new Text[3];
			Color[] colors =
			{
				new Color(0.20f, 0.66f, 0.46f, 0.98f),
				new Color(0.28f, 0.52f, 0.94f, 0.98f),
				new Color(0.74f, 0.36f, 0.88f, 0.98f)
			};
			for (int i = 0; i < buttons.Length; i++)
			{
				float x = (i - 1) * 310f;
				buttons[i] = CreateButton(panel.transform, font, "BossForecastChoice_" + i, string.Empty, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, -28f), new Vector2(286f, 350f), colors[i], Color.white, null, out Text label);
				label.fontSize = 23;
				label.resizeTextForBestFit = false;
				label.rectTransform.offsetMin = new Vector2(16f, 18f);
				label.rectTransform.offsetMax = new Vector2(-16f, -18f);
				Outline outline = buttons[i].gameObject.AddComponent<Outline>();
				outline.effectColor = new Color(1f, 0.82f, 0.38f, 0.76f);
				outline.effectDistance = new Vector2(2f, -2f);
				labels[i] = label;
			}

			CreateText(panel.transform, font, new Color(0.74f, 0.82f, 1f), "BossForecastFooter", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(780f, 34f), "예측은 한 판에 한 번만 선택할 수 있습니다.", 19, TextAnchor.MiddleCenter, bold: false);
			BossForecastBetUI ui = root.AddComponent<BossForecastBetUI>();
			ui.Configure(gameController, group, instruction, buttons, labels);
			root.SetActive(false);
			return ui;
		}
		private LuckySummonChoiceUI BuildLuckySummonChoicePanel(Transform parent, Font font, DefenseGameController gameController)
		{
			GameObject root = new GameObject("LuckySummonChoiceOverlay", typeof(RectTransform));
			root.transform.SetParent(parent, worldPositionStays: false);
			RectTransform rootRect = root.GetComponent<RectTransform>();
			rootRect.anchorMin = Vector2.zero;
			rootRect.anchorMax = Vector2.one;
			rootRect.offsetMin = Vector2.zero;
			rootRect.offsetMax = Vector2.zero;
			Image backdrop = root.AddComponent<Image>();
			((Graphic)backdrop).color = new Color(0.01f, 0.03f, 0.08f, 0.82f);
			((Graphic)backdrop).raycastTarget = true;
			CanvasGroup group = root.AddComponent<CanvasGroup>();
			Image panel = CreatePanel(root.transform, "LuckySummonChoicePanel", new Vector2(0f, 72f), new Vector2(980f, 650f), new Color(0.05f, 0.1f, 0.2f, 0.99f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), rounded: true, shadow: true);
			CreatePanel(((Component)(object)panel).transform, "LuckyTopGlow", new Vector2(0f, -18f), new Vector2(900f, 100f), new Color(0.46f, 0.78f, 0.26f, 0.25f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
			CreatePanel(((Component)(object)panel).transform, "LuckyTopLine", new Vector2(0f, -6f), new Vector2(870f, 8f), new Color(0.72f, 0.9f, 0.38f, 0.96f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
			Text title = CreateText(((Component)(object)panel).transform, font, new Color(0.92f, 1f, 0.78f), "LuckySummonTitle", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -58f), new Vector2(760f, 48f), "\ubd88\uc6b4 \ubcf4\uc815 READY! \ud2b9\ubcc4 \uc18c\ud658", 34, TextAnchor.MiddleCenter, bold: true);
			Text instruction = CreateText(((Component)(object)panel).transform, font, new Color(0.8f, 0.9f, 1f), "LuckySummonInstruction", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -112f), new Vector2(800f, 42f), "\ub0ae\uc740 \ub4f1\uae09\uc774 \uc624\ub798 \uc774\uc5b4\uc84c\uc2b5\ub2c8\ub2e4. \ud2b9\ubcc4 \uc18c\ud658\uc744 \uc120\ud0dd\ud558\uc138\uc694.", 21, TextAnchor.MiddleCenter, bold: false);
			Button[] buttons = (Button[])(object)new Button[3];
			Text[] labels = (Text[])(object)new Text[3];
			Color[] colors = new Color[3]
			{
				new Color(0.24f, 0.72f, 0.46f, 0.98f),
				new Color(0.22f, 0.58f, 0.9f, 0.98f),
				new Color(0.82f, 0.34f, 0.68f, 0.98f)
			};
			for (int i = 0; i < buttons.Length; i++)
			{
				float x = (float)(i - 1) * 310f;
				buttons[i] = CreateButton(((Component)(object)panel).transform, font, "LuckySummonChoice" + i, string.Empty, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, -34f), new Vector2(286f, 330f), colors[i], Color.white, null, out var label);
				label.fontSize = 23;
				label.resizeTextForBestFit = false;
				((Graphic)label).rectTransform.offsetMin = new Vector2(16f, 18f);
				((Graphic)label).rectTransform.offsetMax = new Vector2(-16f, -18f);
				Outline outline = ((Component)(object)buttons[i]).gameObject.AddComponent<Outline>();
				((Shadow)outline).effectColor = new Color(0.9f, 1f, 0.64f, 0.78f);
				((Shadow)outline).effectDistance = new Vector2(2f, -2f);
				labels[i] = label;
			}
			Text closeLabel;
			Button closeButton = CreateButton(((Component)(object)panel).transform, font, "LuckySummonLaterButton", "나중에", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-66f, -48f), new Vector2(112f, 50f), new Color(0.24f, 0.32f, 0.46f, 0.98f), Color.white, null, out closeLabel);
			closeLabel.fontSize = 19;
			LuckySummonChoiceUI choiceUi = root.AddComponent<LuckySummonChoiceUI>();
			choiceUi.Configure(gameController, group, title, instruction, buttons, labels, closeButton);
			root.SetActive(value: false);
			return choiceUi;
		}

		private UltimateRecipeSelectionUI BuildUltimateRecipeSelectionPanel(Transform parent, Font font, DefenseGameController gameController)
		{
			GameObject root = new GameObject("UltimateRecipeSelectionOverlay", typeof(RectTransform));
			root.transform.SetParent(parent, worldPositionStays: false);
			RectTransform rootRect = root.GetComponent<RectTransform>();
			rootRect.anchorMin = Vector2.zero;
			rootRect.anchorMax = Vector2.one;
			rootRect.offsetMin = Vector2.zero;
			rootRect.offsetMax = Vector2.zero;
			Image blocker = root.AddComponent<Image>();
			((Graphic)blocker).color = new Color(0.02f, 0.02f, 0.1f, 0.76f);
			((Graphic)blocker).raycastTarget = true;
			Button blockerButton = root.AddComponent<Button>();
			((Selectable)blockerButton).transition = (Selectable.Transition)0;
			CanvasGroup group = root.AddComponent<CanvasGroup>();
			Image drawer = CreatePanel(root.transform, "UltimateRecipeDrawer", new Vector2(0f, 110f), new Vector2(980f, 820f), new Color(0.06f, 0.06f, 0.22f, 0.99f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), rounded: true, shadow: true);
			Button drawerInputBlocker = ((Component)(object)drawer).gameObject.AddComponent<Button>();
			((Selectable)drawerInputBlocker).transition = (Selectable.Transition)0;
			CreatePanel(((Component)(object)drawer).transform, "DrawerTopGlow", new Vector2(0f, -20f), new Vector2(900f, 94f), new Color(0.72f, 0.22f, 1f, 0.28f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
			CreatePanel(((Component)(object)drawer).transform, "DrawerGoldLine", new Vector2(0f, -6f), new Vector2(860f, 8f), new Color(1f, 0.82f, 0.22f, 0.94f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
			Text header = CreateText(((Component)(object)drawer).transform, font, Color.white, "UltimateRecipeSelectionHeader", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -42f), new Vector2(720f, 46f), "초월 조합 선택", 34, TextAnchor.MiddleCenter, bold: true);
			Text instruction = CreateText(((Component)(object)drawer).transform, font, new Color(0.9f, 0.9f, 1f), "UltimateRecipeSelectionInstruction", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -94f), new Vector2(760f, 30f), "현재 보드와 관련된 초월 레시피만 보여줍니다.", 20, TextAnchor.MiddleCenter, bold: false);
			Text labelText;
			Button closeButton = CreateButton(((Component)(object)drawer).transform, font, "UltimateRecipeSelectionClose", "X", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-28f, -40f), new Vector2(58f, 58f), new Color(0.9f, 0.26f, 0.34f, 0.98f), Color.white, null, out labelText);
			Image optionViewport = CreatePanel(((Component)(object)drawer).transform, "UltimateRecipeOptionViewport", new Vector2(0f, -126f), new Vector2(900f, 316f), new Color(0.02f, 0.03f, 0.12f, 0.36f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
			((Component)(object)optionViewport).gameObject.AddComponent<RectMask2D>();
			ScrollRect optionScroll = ((Component)(object)optionViewport).gameObject.AddComponent<ScrollRect>();
			optionScroll.horizontal = false;
			optionScroll.vertical = true;
			optionScroll.movementType = ScrollRect.MovementType.Clamped;
			GameObject optionContentObject = new GameObject("UltimateRecipeOptionContent", typeof(RectTransform));
			optionContentObject.transform.SetParent(((Component)(object)optionViewport).transform, worldPositionStays: false);
			RectTransform optionContent = optionContentObject.GetComponent<RectTransform>();
			optionContent.anchorMin = new Vector2(0.5f, 1f);
			optionContent.anchorMax = new Vector2(0.5f, 1f);
			optionContent.pivot = new Vector2(0.5f, 1f);
			optionContent.sizeDelta = new Vector2(880f, 316f);
			optionContent.anchoredPosition = Vector2.zero;
			optionScroll.viewport = ((Graphic)optionViewport).rectTransform;
			optionScroll.content = optionContent;
			Text optionLabel;
			Button optionTemplate = CreateButton(optionContent, font, "UltimateRecipeOptionTemplate", string.Empty, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(430f, 60f), new Color(0.12f, 0.12f, 0.34f, 0.98f), Color.white, null, out optionLabel);
			Outline readyOutline = ((Component)(object)optionTemplate).gameObject.AddComponent<Outline>();
			((Shadow)readyOutline).effectColor = Color.clear;
			((Shadow)readyOutline).effectDistance = new Vector2(3f, -3f);
			((Shadow)readyOutline).useGraphicAlpha = false;
			optionLabel.alignment = TextAnchor.MiddleLeft;
			optionLabel.resizeTextForBestFit = true;
			optionLabel.resizeTextMinSize = 13;
			optionLabel.resizeTextMaxSize = 17;
			((Graphic)optionLabel).rectTransform.offsetMin = new Vector2(16f, 6f);
			((Graphic)optionLabel).rectTransform.offsetMax = new Vector2(-12f, -6f);
			Image detailPanel = CreatePanel(((Component)(object)drawer).transform, "UltimateRecipeDetailPanel", new Vector2(0f, 112f), new Vector2(900f, 226f), new Color(0.075f, 0.10f, 0.28f, 0.99f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), rounded: true, shadow: true);
			CreatePanel(((Component)(object)detailPanel).transform, "DetailTopLine", new Vector2(0f, -4f), new Vector2(846f, 5f), new Color(0.94f, 0.70f, 0.22f, 0.94f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
			Image resultCard = CreatePanel(((Component)(object)detailPanel).transform, "ResultCard", new Vector2(24f, -32f), new Vector2(188f, 172f), new Color(0.28f, 0.16f, 0.48f, 0.98f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), rounded: true, shadow: false);
			Image resultPortrait = CreatePanel(((Component)(object)resultCard).transform, "ResultPortrait", new Vector2(20f, -22f), new Vector2(70f, 70f), new Color(0.86f, 0.70f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), rounded: true, shadow: false);
			Text resultFallback = CreateText(((Component)(object)resultPortrait).transform, font, new Color(0.16f, 0.11f, 0.34f), "ResultPortraitFallback", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(66f, 50f), "?", 23, TextAnchor.MiddleCenter, bold: true);
			Text resultName = CreateText(((Component)(object)resultCard).transform, font, Color.white, "ResultName", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -104f), new Vector2(170f, 32f), "Result", 17, TextAnchor.MiddleCenter, bold: true);
			Text resultState = CreateText(((Component)(object)resultCard).transform, font, new Color(1f, 0.86f, 0.24f), "ResultState", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(172f, 38f), "READY", 14, TextAnchor.MiddleCenter, bold: true);
			Text materialHeader = CreateText(((Component)(object)detailPanel).transform, font, new Color(0.72f, 0.92f, 1f), "MaterialHeader", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(230f, -21f), new Vector2(300f, 28f), "Materials", 18, TextAnchor.MiddleLeft, bold: true);
			Text missingText = CreateText(((Component)(object)detailPanel).transform, font, new Color(0.38f, 1f, 0.72f), "MaterialMissingText", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-20f, 14f), new Vector2(630f, 26f), "Ready", 15, TextAnchor.MiddleRight, bold: true);
			Image materialViewport = CreatePanel(((Component)(object)detailPanel).transform, "UltimateRecipeMaterialViewport", new Vector2(230f, -48f), new Vector2(640f, 142f), new Color(0f, 0f, 0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), rounded: false, shadow: false);
			((Component)(object)materialViewport).gameObject.AddComponent<RectMask2D>();
			ScrollRect materialScroll = ((Component)(object)materialViewport).gameObject.AddComponent<ScrollRect>();
			materialScroll.horizontal = false;
			materialScroll.vertical = true;
			materialScroll.movementType = ScrollRect.MovementType.Clamped;
			GameObject materialContentObject = new GameObject("UltimateRecipeMaterialContent", typeof(RectTransform));
			materialContentObject.transform.SetParent(((Component)(object)materialViewport).transform, worldPositionStays: false);
			RectTransform materialContent = materialContentObject.GetComponent<RectTransform>();
			materialContent.anchorMin = new Vector2(0f, 1f);
			materialContent.anchorMax = new Vector2(0f, 1f);
			materialContent.pivot = new Vector2(0f, 1f);
			materialContent.sizeDelta = new Vector2(636f, 142f);
			materialContent.anchoredPosition = Vector2.zero;
			materialScroll.viewport = ((Graphic)materialViewport).rectTransform;
			materialScroll.content = materialContent;
			Image materialTemplate = CreatePanel(materialContent, "UltimateRecipeMaterialTemplate", Vector2.zero, new Vector2(196f, 66f), new Color(0.13f, 0.30f, 0.25f, 0.98f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), rounded: true, shadow: false);
			Image materialPortrait = CreatePanel(((Component)(object)materialTemplate).transform, "Portrait", new Vector2(8f, -8f), new Vector2(50f, 50f), new Color(0.75f, 0.90f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), rounded: true, shadow: false);
			CreateText(((Component)(object)materialPortrait).transform, font, new Color(0.10f, 0.18f, 0.30f), "Fallback", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(46f, 40f), "?", 16, TextAnchor.MiddleCenter, bold: true);
			CreateText(((Component)(object)materialTemplate).transform, font, Color.white, "Label", new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(64f, 0f), new Vector2(-70f, 54f), "Material", 14, TextAnchor.MiddleLeft, bold: true);
			UltimateRecipeDetailView detailView = new UltimateRecipeDetailView
			{
				panel = detailPanel,
				resultPortrait = resultPortrait,
				resultFallback = resultFallback,
				resultName = resultName,
				resultState = resultState,
				materialHeader = materialHeader,
				missingText = missingText,
				materialContent = materialContent,
				materialTemplate = materialTemplate,
				materialScroll = materialScroll
			};
			Text confirmLabel;
			Button confirmButton = CreateButton(((Component)(object)drawer).transform, font, "UltimateRecipeConfirmButton", "레시피를 선택하세요", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(500f, 68f), new Color(0.72f, 0.24f, 0.94f, 0.98f), Color.white, null, out confirmLabel);
			confirmLabel.fontSize = 25;
			UltimateRecipeSelectionUI selection = root.AddComponent<UltimateRecipeSelectionUI>();
			selection.Configure(gameController, ((Graphic)drawer).rectTransform, group, blockerButton, header, instruction, optionContent, optionTemplate, optionScroll, closeButton, confirmButton, confirmLabel, detailView);
			root.SetActive(value: false);
			return selection;
		}

		private GameObject BuildBossWarningPanel(Transform parent, Font font, out CanvasGroup canvasGroup, out Text title, out Text subtitle)
		{
			Image panel = CreatePanel(parent, "BossWarningPanel", new Vector2(0f, 92f), new Vector2(790f, 230f), new Color(0.2f, 0.02f, 0.08f, 0.94f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), rounded: true, shadow: true);
			canvasGroup = ((Component)(object)panel).gameObject.AddComponent<CanvasGroup>();
			canvasGroup.alpha = 0f;
			canvasGroup.blocksRaycasts = false;
			canvasGroup.interactable = false;
			CreatePanel(((Component)(object)panel).transform, "BossWarningGlow", Vector2.zero, new Vector2(-34f, -28f), new Color(1f, 0.12f, 0.18f, 0.18f), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), rounded: true, shadow: false);
			CreatePanel(((Component)(object)panel).transform, "BossWarningTopLine", new Vector2(0f, -8f), new Vector2(700f, 12f), new Color(1f, 0.24f, 0.18f, 0.98f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
			CreatePanel(((Component)(object)panel).transform, "BossWarningBottomLine", new Vector2(0f, 8f), new Vector2(700f, 10f), new Color(1f, 0.72f, 0.24f, 0.86f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), rounded: true, shadow: false);
			CreatePanel(((Component)(object)panel).transform, "LeftDangerIcon", new Vector2(52f, 0f), new Vector2(86f, 86f), new Color(1f, 0.18f, 0.16f, 0.95f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), rounded: true, shadow: false);
			CreatePanel(((Component)(object)panel).transform, "RightDangerIcon", new Vector2(-52f, 0f), new Vector2(86f, 86f), new Color(1f, 0.18f, 0.16f, 0.95f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), rounded: true, shadow: false);
			CreateText(((Component)(object)panel).transform, font, new Color(1f, 0.78f, 0.28f), "BossWarningKicker", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -38f), new Vector2(420f, 32f), "WARNING", 27, TextAnchor.MiddleCenter, bold: true);
			title = CreateText(((Component)(object)panel).transform, font, Color.white, "BossWarningTitle", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 16f), new Vector2(560f, 72f), "보스 등장!", 58, TextAnchor.MiddleCenter, bold: true);
			subtitle = CreateText(((Component)(object)panel).transform, font, new Color(1f, 0.88f, 0.74f), "BossWarningSub", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 42f), new Vector2(620f, 36f), "강력한 보스가 내려옵니다", 25, TextAnchor.MiddleCenter, bold: true);
			((Component)(object)panel).gameObject.SetActive(value: false);
			return ((Component)(object)panel).gameObject;
		}

		private void BuildAugmentPanel(Transform parent, Font font, AugmentManager augmentManager, DefenseGameController gameController)
		{
			if (!(augmentManager == null))
			{
				GameObject root = new GameObject("AugmentChoiceOverlay", typeof(RectTransform));
				root.transform.SetParent(parent, worldPositionStays: false);
				Image blocker = root.AddComponent<Image>();
				((Graphic)blocker).color = new Color(0.03f, 0.04f, 0.15f, 0.78f);
				((Graphic)blocker).raycastTarget = true;
				RectTransform rootRect = root.GetComponent<RectTransform>();
				rootRect.anchorMin = Vector2.zero;
				rootRect.anchorMax = Vector2.one;
				rootRect.offsetMin = Vector2.zero;
				rootRect.offsetMax = Vector2.zero;
				Image modal = CreatePanel(root.transform, "AugmentModal", new Vector2(0f, 62f), new Vector2(900f, 850f), new Color(0.075f, 0.075f, 0.22f, 0.99f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), rounded: true, shadow: true);
				CreatePanel(((Component)(object)modal).transform, "AugmentHeaderPill", new Vector2(0f, -18f), new Vector2(420f, 66f), new Color(0.48f, 0.27f, 0.88f, 0.98f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
				Text header = CreateText(((Component)(object)modal).transform, font, Color.white, "AugmentHeader", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -27f), new Vector2(380f, 46f), "증강체 선택", 34, TextAnchor.MiddleCenter, bold: true);
				header.fontSize = 36;
				CreateText(((Component)(object)modal).transform, font, new Color(0.86f, 0.89f, 1f), "AugmentSubtitle", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -94f), new Vector2(740f, 36f), "무료 보너스 1개를 반드시 선택하세요.", 22, TextAnchor.MiddleCenter, bold: false);
				CreatePanel(((Component)(object)modal).transform, "AugmentTopLine", new Vector2(0f, -122f), new Vector2(720f, 5f), new Color(0.72f, 0.42f, 1f, 0.96f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
				CreatePanel(((Component)(object)modal).transform, "AugmentLeftRail", new Vector2(18f, -18f), new Vector2(7f, 650f), new Color(1f, 0.72f, 0.2f, 0.92f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), rounded: true, shadow: false);
				CreatePanel(((Component)(object)modal).transform, "AugmentRightRail", new Vector2(-18f, -18f), new Vector2(7f, 650f), new Color(0.67f, 0.36f, 1f, 0.92f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), rounded: true, shadow: false);
				Text labelText;
				Button closeButton = CreateButton(((Component)(object)modal).transform, font, "AugmentCloseButton", "X", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-32f, -40f), new Vector2(66f, 66f), new Color(0.94f, 0.36f, 0.3f, 0.98f), Color.white, null, out labelText);
				Text augmentSubtitle = ((Component)(object)modal).transform.Find("AugmentSubtitle").GetComponent<Text>();
				augmentSubtitle.fontSize = 22;
				((Graphic)augmentSubtitle).rectTransform.sizeDelta = new Vector2(760f, 34f);
				Text reopenLabel;
				Button reopenButton = CreateButton(parent, font, "AugmentReopenButton", "증강체", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-136f, -206f), new Vector2(180f, 62f), new Color(0.5f, 0.28f, 0.96f, 0.96f), Color.white, null, out reopenLabel);
				reopenLabel.fontSize = 28;
				((Component)(object)reopenButton).GetComponent<RectTransform>().sizeDelta = new Vector2(210f, 72f);
				((Component)(object)reopenButton).gameObject.SetActive(value: false);
				Button[] buttons = (Button[])(object)new Button[3];
				Image[] accents = (Image[])(object)new Image[3];
				Text[] styles = (Text[])(object)new Text[3];
				Text[] titles = (Text[])(object)new Text[3];
				Text[] descriptions = (Text[])(object)new Text[3];
				for (int i = 0; i < 3; i++)
				{
					float y = -158f - (float)i * 176f;
					Button choiceButton = CreateButton(((Component)(object)modal).transform, font, "AugmentChoice_" + i, string.Empty, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(790f, 164f), new Color(0.13f, 0.15f, 0.36f, 0.99f), Color.white, null, out labelText);
					AddCardOutline(choiceButton, new Color(0.67f, 0.38f, 1f, 0.98f), 3f);
					CreatePanel(((Component)(object)choiceButton).transform, "IconBadgeBack", new Vector2(26f, -32f), new Vector2(100f, 100f), new Color(1f, 0.7f, 0.18f, 0.98f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), rounded: true, shadow: false);
					accents[i] = CreatePanel(((Component)(object)choiceButton).transform, "IconPlate", new Vector2(38f, -44f), new Vector2(76f, 76f), new Color(0.82f, 0.48f, 1f, 0.98f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), rounded: true, shadow: false);
					CreateSkinIcon(((Component)(object)accents[i]).transform, "AugmentIcon", "augment", Vector2.zero, new Vector2(62f, 62f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Color.white);
					styles[i] = CreateText(((Component)(object)choiceButton).transform, font, new Color(0.18f, 0.1f, 0.3f), "Style", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(25f, 14f), new Vector2(110f, 28f), "확정", 18, TextAnchor.MiddleCenter, bold: true);
					titles[i] = CreateText(((Component)(object)choiceButton).transform, font, new Color(1f, 0.86f, 0.28f), "Title", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(160f, -20f), new Vector2(-270f, 42f), "Augment", 31, TextAnchor.MiddleLeft, bold: true);
					descriptions[i] = CreateText(((Component)(object)choiceButton).transform, font, new Color(0.94f, 0.95f, 1f), "Description", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(160f, -66f), new Vector2(-250f, 94f), "Description", 24, TextAnchor.UpperLeft, bold: false);
					CreatePanel(((Component)(object)choiceButton).transform, "PickPill", new Vector2(-20f, 16f), new Vector2(92f, 34f), new Color(0.26f, 0.76f, 0.58f, 0.92f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), rounded: true, shadow: false);
					CreateText(((Component)(object)choiceButton).transform, font, Color.white, "PickLabel", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-28f, 18f), new Vector2(76f, 30f), "선택", 18, TextAnchor.MiddleCenter, bold: true);
					Text pickLabel = ((Component)(object)choiceButton).transform.Find("PickLabel").GetComponent<Text>();
					pickLabel.fontSize = 18;
					buttons[i] = choiceButton;
				}
				CreateText(((Component)(object)modal).transform, font, new Color(0.72f, 0.78f, 0.96f), "AugmentFooterHint", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(720f, 34f), "카드 전체를 눌러 선택합니다.", 19, TextAnchor.MiddleCenter, bold: false);
				augmentManager.Configure(gameController, root, header, titles, descriptions, buttons, styles, accents, closeButton, reopenButton);
			}
		}

		private void BuildRunShopPanel(Transform parent, Font font, RunShopSystem runShopSystem, DefenseGameController gameController, DefenseBoardManager boardManager, BoardTileModifierSystem tileModifierSystem, AugmentManager augmentManager)
		{
			if (!(runShopSystem == null))
			{
				GameObject root = new GameObject("RunShopOverlay", typeof(RectTransform));
				root.transform.SetParent(parent, worldPositionStays: false);
				RectTransform rootRect = root.GetComponent<RectTransform>();
				rootRect.anchorMin = Vector2.zero;
				rootRect.anchorMax = Vector2.one;
				rootRect.offsetMin = Vector2.zero;
				rootRect.offsetMax = Vector2.zero;
				Image dim = root.AddComponent<Image>();
				((Graphic)dim).color = new Color(0.02f, 0.04f, 0.16f, 0.76f);
				Image modal = CreatePanel(root.transform, "RunShopModal", new Vector2(0f, 62f), new Vector2(900f, 850f), new Color(0.065f, 0.11f, 0.3f, 0.99f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), rounded: true, shadow: true);
				CreatePanel(((Component)(object)modal).transform, "RunShopHeaderPill", new Vector2(0f, -18f), new Vector2(360f, 66f), new Color(0.1f, 0.62f, 0.84f, 0.98f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
				Text header = CreateText(((Component)(object)modal).transform, font, Color.white, "RunShopHeader", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -42f), new Vector2(460f, 46f), "전투 상점", 36, TextAnchor.MiddleCenter, bold: true);
				header.fontSize = 36;
				Text subtitle = CreateText(((Component)(object)modal).transform, font, new Color(0.84f, 0.92f, 1f), "RunShopSubtitle", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -94f), new Vector2(720f, 32f), "이번 판 전용 상품입니다.", 21, TextAnchor.MiddleCenter, bold: false);
				subtitle.fontSize = 22;
				((Graphic)subtitle).rectTransform.sizeDelta = new Vector2(760f, 34f);
				CreatePanel(((Component)(object)modal).transform, "RunShopTopLine", new Vector2(0f, -122f), new Vector2(720f, 5f), new Color(0.28f, 0.78f, 1f, 0.94f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
				CreatePanel(((Component)(object)modal).transform, "RunShopBottomLine", new Vector2(0f, 78f), new Vector2(720f, 5f), new Color(0.28f, 0.78f, 1f, 0.78f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), rounded: true, shadow: false);
				CreatePanel(((Component)(object)modal).transform, "RunShopLeftRail", new Vector2(18f, -18f), new Vector2(7f, 620f), new Color(1f, 0.62f, 0.18f, 0.92f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), rounded: true, shadow: false);
				CreatePanel(((Component)(object)modal).transform, "RunShopRightRail", new Vector2(-18f, -18f), new Vector2(7f, 620f), new Color(0.38f, 0.86f, 1f, 0.92f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), rounded: true, shadow: false);
				Text labelText;
				Button closeButton = CreateButton(((Component)(object)modal).transform, font, "RunShopCloseButton", "X", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-32f, -40f), new Vector2(66f, 66f), new Color(0.94f, 0.36f, 0.3f, 0.98f), Color.white, null, out labelText);
				Text reopenLabel;
				Button reopenButton = CreateButton(parent, font, "RunShopReopenButton", "상점", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-136f, -278f), new Vector2(180f, 62f), new Color(0.14f, 0.66f, 0.92f, 0.96f), Color.white, null, out reopenLabel);
				reopenLabel.fontSize = 28;
				((Component)(object)reopenButton).GetComponent<RectTransform>().sizeDelta = new Vector2(210f, 72f);
				((Component)(object)reopenButton).gameObject.SetActive(value: false);
				Button[] buttons = (Button[])(object)new Button[3];
				Text[] titles = (Text[])(object)new Text[3];
				Text[] descriptions = (Text[])(object)new Text[3];
				Text[] prices = (Text[])(object)new Text[3];
				Image[] accents = (Image[])(object)new Image[3];
				for (int i = 0; i < buttons.Length; i++)
				{
					float y = -158f - (float)i * 176f;
					buttons[i] = CreateButton(((Component)(object)modal).transform, font, "RunShopOffer_" + i, string.Empty, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(790f, 164f), new Color(0.09f, 0.16f, 0.36f, 0.99f), Color.white, null, out labelText);
					AddCardOutline(buttons[i], new Color(0.28f, 0.78f, 1f, 0.96f), 3f);
					CreatePanel(((Component)(object)buttons[i]).transform, "RunShopIconBadgeBack", new Vector2(26f, -32f), new Vector2(100f, 100f), new Color(0.18f, 0.46f, 0.72f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), rounded: true, shadow: false);
					accents[i] = CreatePanel(((Component)(object)buttons[i]).transform, "RunShopOfferAccent", new Vector2(38f, -44f), new Vector2(76f, 76f), new Color(0.38f, 0.82f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), rounded: true, shadow: false);
					CreateSkinIcon(((Component)(object)accents[i]).transform, "RunShopOfferIcon", "shop offer", Vector2.zero, new Vector2(64f, 56f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Color.white);
					titles[i] = CreateText(((Component)(object)buttons[i]).transform, font, Color.white, "Title", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(160f, -20f), new Vector2(-370f, 42f), "상품", 31, TextAnchor.MiddleLeft, bold: true);
					descriptions[i] = CreateText(((Component)(object)buttons[i]).transform, font, new Color(0.88f, 0.92f, 1f), "Description", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(160f, -66f), new Vector2(-368f, 94f), "설명", 24, TextAnchor.UpperLeft, bold: false);
					Image priceDock = CreatePanel(((Component)(object)buttons[i]).transform, "PriceDock", new Vector2(-18f, 0f), new Vector2(166f, 104f), new Color(0.055f, 0.09f, 0.23f, 0.98f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), rounded: true, shadow: false);
					CreateText(((Component)(object)priceDock).transform, font, new Color(0.62f, 0.82f, 1f), "PriceCaption", new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(-18f, 28f), "가격", 17, TextAnchor.MiddleCenter, bold: true);
					prices[i] = CreateText(((Component)(object)priceDock).transform, font, new Color(1f, 0.91f, 0.38f), "Price", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, -13f), new Vector2(-14f, -38f), "0G", 30, TextAnchor.MiddleCenter, bold: true);
					prices[i].resizeTextForBestFit = true;
					prices[i].resizeTextMinSize = 18;
					prices[i].resizeTextMaxSize = 30;
				}
				CreateText(((Component)(object)modal).transform, font, new Color(0.72f, 0.84f, 1f), "RunShopFooterHint", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(720f, 34f), "구매하지 않고 닫으면 이번 상점은 지나갑니다.", 19, TextAnchor.MiddleCenter, bold: false);
				root.SetActive(value: false);
				runShopSystem.Configure(gameController, boardManager, tileModifierSystem, augmentManager, root, header, subtitle, buttons, titles, descriptions, prices, accents, closeButton, reopenButton);
			}
		}

		private void BuildTacticalMissionPanel(Transform canvasRoot, Transform hudRoot, Font font, TacticalMissionSystem missionSystem, DefenseGameController gameController, DefenseBoardManager boardManager)
		{
			if (!(missionSystem == null))
			{
				Text labelText;
				Button summaryButton = CreateButton(hudRoot, font, "MissionSummaryButton", string.Empty, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(22f, -154f), new Vector2(350f, 62f), new Color(0.12f, 0.18f, 0.38f, 0.96f), Color.white, null, out labelText);
				CreatePanel(((Component)(object)summaryButton).transform, "MissionGlow", Vector2.zero, new Vector2(-20f, -18f), new Color(1f, 0.78f, 0.25f, 0.18f), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), rounded: true, shadow: false);
				CreatePanel(((Component)(object)summaryButton).transform, "MissionIconSlot", new Vector2(18f, 0f), new Vector2(42f, 42f), new Color(1f, 0.74f, 0.24f, 0.92f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), rounded: true, shadow: false);
				CreateSkinIcon(((Component)(object)summaryButton).transform, "MissionIcon", "mission", new Vector2(39f, 0f), new Vector2(30f, 30f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), Color.white);
				Text summaryText = CreateText(((Component)(object)summaryButton).transform, font, Color.white, "MissionSummaryText", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(62f, 0f), new Vector2(-178f, 0f), "미션 선택", 22, TextAnchor.MiddleLeft, bold: true);
				summaryText.resizeTextForBestFit = true;
				summaryText.resizeTextMinSize = 16;
				summaryText.resizeTextMaxSize = 22;
				Text summaryHint = CreateText(((Component)(object)summaryButton).transform, font, new Color(0.82f, 0.9f, 1f), "MissionOpenHint", new Vector2(1f, 0f), Vector2.one, new Vector2(1f, 0.5f), new Vector2(-14f, 0f), new Vector2(62f, 0f), "열기", 16, TextAnchor.MiddleCenter, bold: true);
				summaryHint.resizeTextForBestFit = true;
				summaryHint.resizeTextMinSize = 12;
				summaryHint.resizeTextMaxSize = 16;
				Text debugDefeatLabel;
				Button debugDefeatButton = CreateButton(hudRoot, font, "DebugDefeatButton", "DEV 패배  [F8]", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(22f, -224f), new Vector2(194f, 48f), new Color(0.74f, 0.16f, 0.2f, 0.96f), Color.white, gameController.TriggerDebugDefeat, out debugDefeatLabel);
				debugDefeatLabel.fontSize = 17;
				debugDefeatLabel.resizeTextForBestFit = true;
				debugDefeatLabel.resizeTextMinSize = 13;
				debugDefeatLabel.resizeTextMaxSize = 17;
				Text debugNextRoundLabel;
				Button debugNextRoundButton = CreateButton(hudRoot, font, "DebugNextRoundButton", "DEV 다음 R  [F9]", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(224f, -224f), new Vector2(194f, 48f), new Color(0.12f, 0.42f, 0.78f, 0.96f), Color.white, gameController.TriggerDebugAdvanceRound, out debugNextRoundLabel);
				debugNextRoundLabel.fontSize = 17;
				debugNextRoundLabel.resizeTextForBestFit = true;
				debugNextRoundLabel.resizeTextMinSize = 13;
				debugNextRoundLabel.resizeTextMaxSize = 17;
				GameObject root = new GameObject("TacticalMissionOverlay", typeof(RectTransform));
				root.transform.SetParent(canvasRoot, worldPositionStays: false);
				Image blocker = root.AddComponent<Image>();
				((Graphic)blocker).color = new Color(0.03f, 0.04f, 0.15f, 0.72f);
				((Graphic)blocker).raycastTarget = true;
				RectTransform rootRect = root.GetComponent<RectTransform>();
				rootRect.anchorMin = Vector2.zero;
				rootRect.anchorMax = Vector2.one;
				rootRect.offsetMin = Vector2.zero;
				rootRect.offsetMax = Vector2.zero;
				Image modal = CreatePanel(root.transform, "MissionModal", Vector2.zero, new Vector2(860f, 1340f), new Color(0.13f, 0.16f, 0.4f, 0.98f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), rounded: true, shadow: true);
				CreatePanel(((Component)(object)modal).transform, "MissionTopGlow", new Vector2(0f, -34f), new Vector2(720f, 74f), new Color(1f, 0.76f, 0.22f, 0.2f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
				Text header = CreateText(((Component)(object)modal).transform, font, Color.white, "MissionPanelHeader", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -44f), new Vector2(420f, 44f), "\uc804\uc220 \uacc4\uc57d \uc120\ud0dd", 35, TextAnchor.MiddleCenter, bold: true);
				CreateText(((Component)(object)modal).transform, font, new Color(0.86f, 0.92f, 1f), "MissionPanelSubHeader", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -104f), new Vector2(720f, 30f), "\uc6d0\ud558\ub294 \uacc4\uc57d\uc744 \uc120\ud0dd\ud558\uc138\uc694. \uc120\ud0dd\ud558\uc9c0 \uc54a\uc544\ub3c4 \ub2e4\uc74c \ub77c\uc6b4\ub4dc\ub97c \uc2dc\uc791\ud560 \uc218 \uc788\uc2b5\ub2c8\ub2e4.", 22, TextAnchor.MiddleCenter, bold: false);
				Button closeButton = CreateButton(((Component)(object)modal).transform, font, "MissionCloseButton", "\ub098\uc911\uc5d0", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-54f, -36f), new Vector2(118f, 58f), new Color(0.94f, 0.36f, 0.3f, 0.98f), Color.white, null, out labelText);
				Image activeCard = CreatePanel(((Component)(object)modal).transform, "ActiveMissionCard", new Vector2(0f, -168f), new Vector2(740f, 238f), new Color(0.08f, 0.12f, 0.3f, 0.94f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: true);
				CreatePanel(((Component)(object)activeCard).transform, "ActiveMissionIcon", new Vector2(28f, -26f), new Vector2(76f, 76f), new Color(1f, 0.76f, 0.24f, 0.9f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), rounded: true, shadow: false);
				CreateSkinIcon(((Component)(object)activeCard).transform, "ActiveMissionGlyph", "mission", new Vector2(66f, -64f), new Vector2(52f, 52f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), Color.white);
				Text activeTitle = CreateText(((Component)(object)activeCard).transform, font, Color.white, "ActiveMissionTitle", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(124f, -24f), new Vector2(-164f, 38f), "미션", 30, TextAnchor.MiddleLeft, bold: true);
				Text activeDescription = CreateText(((Component)(object)activeCard).transform, font, new Color(0.88f, 0.94f, 1f), "ActiveMissionDescription", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(124f, -72f), new Vector2(-156f, 96f), "\uc124\uba85", 24, TextAnchor.UpperLeft, bold: false);
				Text activeProgress = CreateText(((Component)(object)activeCard).transform, font, new Color(1f, 0.9f, 0.42f), "ActiveMissionProgress", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(560f, 40f), "0 / 0", 32, TextAnchor.MiddleCenter, bold: true);
				Button[] optionButtons = (Button[])(object)new Button[3];
				Text[] optionTitles = (Text[])(object)new Text[3];
				Text[] optionDescriptions = (Text[])(object)new Text[3];
				Text[] optionRewards = (Text[])(object)new Text[3];
				Image[] optionAccents = (Image[])(object)new Image[3];
				for (int i = 0; i < 3; i++)
				{
					float y = -166f - (float)i * 190f;
					optionButtons[i] = CreateButton(((Component)(object)modal).transform, font, "MissionOption_" + i, string.Empty, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(740f, 172f), new Color(0.1f, 0.14f, 0.34f, 0.96f), Color.white, null, out labelText);
					optionAccents[i] = CreatePanel(((Component)(object)optionButtons[i]).transform, "MissionOptionIcon", new Vector2(26f, -28f), new Vector2(82f, 82f), new Color(1f, 0.76f, 0.24f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), rounded: true, shadow: false);
					CreatePanel(((Component)(object)optionButtons[i]).transform, "MissionOptionIconCore", new Vector2(67f, -69f), new Vector2(34f, 34f), new Color(1f, 1f, 1f, 0.24f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), rounded: true, shadow: false);
					CreateSkinIcon(((Component)(object)optionButtons[i]).transform, "MissionOptionGlyph", "mission", new Vector2(67f, -69f), new Vector2(52f, 52f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), Color.white);
					optionTitles[i] = CreateText(((Component)(object)optionButtons[i]).transform, font, Color.white, "Title", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(132f, -20f), new Vector2(-182f, 38f), "\ubbf8\uc158", 30, TextAnchor.MiddleLeft, bold: true);
					optionDescriptions[i] = CreateText(((Component)(object)optionButtons[i]).transform, font, new Color(0.88f, 0.94f, 1f), "Description", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(132f, -62f), new Vector2(-182f, 58f), "\uc124\uba85", 23, TextAnchor.UpperLeft, bold: false);
					optionRewards[i] = CreateText(((Component)(object)optionButtons[i]).transform, font, new Color(1f, 0.88f, 0.38f), "Reward", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(132f, 12f), new Vector2(-260f, 34f), "\ubcf4\uc0c1", 24, TextAnchor.MiddleLeft, bold: true);
					CreateText(((Component)(object)optionButtons[i]).transform, font, new Color(0.45f, 1f, 0.68f), "PickLabel", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-26f, 0f), new Vector2(92f, 36f), "선택", 21, TextAnchor.MiddleRight, bold: true);
				}
				Image toast = CreatePanel(hudRoot, "MissionCompletionToast", new Vector2(22f, -226f), new Vector2(350f, 64f), new Color(0.05f, 0.15f, 0.32f, 0.96f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), rounded: true, shadow: true);
				CanvasGroup toastGroup = ((Component)(object)toast).gameObject.AddComponent<CanvasGroup>();
				toastGroup.alpha = 0f;
				toastGroup.interactable = false;
				toastGroup.blocksRaycasts = false;
				CreatePanel(((Component)(object)toast).transform, "MissionToastGlow", Vector2.zero, new Vector2(-20f, -18f), new Color(0.28f, 1f, 0.76f, 0.24f), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), rounded: true, shadow: false);
				CreatePanel(((Component)(object)toast).transform, "MissionToastIconSlot", new Vector2(38f, 0f), new Vector2(68f, 68f), new Color(1f, 0.76f, 0.24f, 0.95f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), rounded: true, shadow: false);
				CreateSkinIcon(((Component)(object)toast).transform, "MissionToastIcon", "mission", new Vector2(38f, 0f), new Vector2(46f, 46f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), Color.white);
				Text toastTitle = CreateText(((Component)(object)toast).transform, font, Color.white, "MissionToastTitle", new Vector2(0f, 1f), Vector2.one, new Vector2(0f, 1f), new Vector2(92f, -24f), new Vector2(-120f, 42f), "미션 완료!", 31, TextAnchor.MiddleLeft, bold: true);
				Text toastReward = CreateText(((Component)(object)toast).transform, font, new Color(1f, 0.9f, 0.42f), "MissionToastReward", new Vector2(0f, 0f), Vector2.one, new Vector2(0f, 0f), new Vector2(92f, 22f), new Vector2(-120f, 38f), "+보상", 23, TextAnchor.MiddleLeft, bold: true);
				((Component)(object)toast).gameObject.SetActive(value: false);
				root.SetActive(value: false);
				missionSystem.Configure(gameController, boardManager, summaryButton, summaryText, root, header, ((Component)(object)activeCard).gameObject, activeTitle, activeDescription, activeProgress, optionButtons, optionTitles, optionDescriptions, optionRewards, optionAccents, closeButton, ((Component)(object)toast).gameObject, toastGroup, toastTitle, toastReward);
			}
		}

		private void BuildSynergyPanel(Transform parent, Font font, BoardSynergySystem synergySystem, DefenseGameController gameController, DefenseBoardManager boardManager)
		{
			if (!(synergySystem == null))
			{
				Image panel = CreatePanel(parent, "SynergyPanel", new Vector2(-28f, -128f), new Vector2(312f, 272f), new Color(0.08f, 0.12f, 0.28f, 0.92f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), rounded: true, shadow: true);
				CreatePanel(((Component)(object)panel).transform, "SynergyGlow", new Vector2(0f, -12f), new Vector2(260f, 44f), new Color(0.28f, 0.94f, 1f, 0.22f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
				Text header = CreateText(((Component)(object)panel).transform, font, Color.white, "SynergyHeader", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(240f, 28f), "시너지 대기", 22, TextAnchor.MiddleCenter, bold: true);
				Text[] titles = (Text[])(object)new Text[5];
				Text[] descriptions = (Text[])(object)new Text[5];
				Image[] accents = (Image[])(object)new Image[5];
				for (int i = 0; i < 5; i++)
				{
					float y = -58f - (float)i * 40f;
					Image row = CreatePanel(((Component)(object)panel).transform, "SynergyRow_" + i, new Vector2(0f, y), new Vector2(282f, 38f), new Color(0.12f, 0.16f, 0.36f, 0.82f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
					accents[i] = CreatePanel(((Component)(object)row).transform, "Accent", new Vector2(14f, -17f), new Vector2(10f, 22f), new Color(0.42f, 0.48f, 0.64f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), rounded: true, shadow: false);
					titles[i] = CreateText(((Component)(object)row).transform, font, Color.white, "Title", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(32f, -3f), new Vector2(224f, 18f), string.Empty, 17, TextAnchor.MiddleLeft, bold: true);
					descriptions[i] = CreateText(((Component)(object)row).transform, font, new Color(0.84f, 0.91f, 1f), "Description", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(32f, -21f), new Vector2(224f, 16f), string.Empty, 14, TextAnchor.MiddleLeft, bold: false);
				}
				synergySystem.Configure(gameController, boardManager, ((Component)(object)panel).gameObject, header, titles, descriptions, accents);
			}
		}

		private void BuildSynergyPanelExpanded(Transform parent, Font font, BoardSynergySystem synergySystem, DefenseGameController gameController, DefenseBoardManager boardManager)
		{
			if (!(synergySystem == null))
			{
				Text labelText;
				Button summaryButton = CreateButton(parent, font, "SynergySummaryButton", string.Empty, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-318f, -154f), new Vector2(370f, 62f), new Color(0.1f, 0.16f, 0.34f, 0.96f), Color.white, null, out labelText);
				CreatePanel(((Component)(object)summaryButton).transform, "SummaryGlow", Vector2.zero, new Vector2(-24f, -20f), new Color(0.22f, 0.92f, 1f, 0.2f), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), rounded: true, shadow: false);
				CreatePanel(((Component)(object)summaryButton).transform, "SummaryIconSlot", new Vector2(24f, 0f), new Vector2(50f, 50f), new Color(0.25f, 0.1f, 0.68f, 0.86f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), rounded: true, shadow: false);
				CreateSkinIcon(((Component)(object)summaryButton).transform, "SynergyIcon", "hero", new Vector2(49f, 0f), new Vector2(34f, 34f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), Color.white);
				Text summaryHeader = CreateText(((Component)(object)summaryButton).transform, font, Color.white, "SummaryText", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(78f, 0f), new Vector2(200f, 42f), "시너지 대기중", 24, TextAnchor.MiddleLeft, bold: true);
				CreateText(((Component)(object)summaryButton).transform, font, new Color(0.8f, 0.88f, 1f), "SummaryHint", new Vector2(1f, 0f), Vector2.one, new Vector2(1f, 0.5f), new Vector2(-20f, 0f), new Vector2(84f, 0f), "열기", 22, TextAnchor.MiddleCenter, bold: true);
				Image expandedPanel = CreatePanel(parent, "SynergyExpandedPanel", new Vector2(-318f, -224f), new Vector2(500f, 560f), new Color(0.08f, 0.12f, 0.3f, 0.97f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), rounded: true, shadow: true);
				CreatePanel(((Component)(object)expandedPanel).transform, "ExpandedGlow", new Vector2(0f, -16f), new Vector2(420f, 66f), new Color(0.22f, 0.92f, 1f, 0.2f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
				Text expandedHeader = CreateText(((Component)(object)expandedPanel).transform, font, Color.white, "ExpandedHeader", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -34f), new Vector2(320f, 42f), "활성 시너지", 34, TextAnchor.MiddleCenter, bold: true);
				CreateText(((Component)(object)expandedPanel).transform, font, new Color(0.8f, 0.88f, 1f), "ExpandedHint", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -82f), new Vector2(410f, 34f), "같은 역할을 모아 강한 조합을 만드세요", 21, TextAnchor.MiddleCenter, bold: false);
				Button closeButton = CreateButton(((Component)(object)expandedPanel).transform, font, "SynergyCloseButton", "X", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-18f, -18f), new Vector2(58f, 58f), new Color(0.94f, 0.36f, 0.3f, 0.96f), Color.white, null, out labelText);
				Text[] titles = (Text[])(object)new Text[5];
				Text[] descriptions = (Text[])(object)new Text[5];
				Image[] accents = (Image[])(object)new Image[5];
				Image[] icons = (Image[])(object)new Image[5];
				for (int i = 0; i < 5; i++)
				{
					float y = -126f - (float)i * 82f;
					Image row = CreatePanel(((Component)(object)expandedPanel).transform, "SynergyExpandedRow_" + i, new Vector2(0f, y), new Vector2(454f, 74f), new Color(0.12f, 0.16f, 0.36f, 0.9f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
					accents[i] = CreatePanel(((Component)(object)row).transform, "AccentIconPlate", new Vector2(18f, 0f), new Vector2(54f, 54f), new Color(0.42f, 0.48f, 0.64f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), rounded: true, shadow: false);
					CreatePanel(((Component)(object)row).transform, "AccentCore", new Vector2(45f, 0f), new Vector2(22f, 22f), new Color(1f, 1f, 1f, 0.32f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), rounded: true, shadow: false);
					titles[i] = CreateText(((Component)(object)row).transform, font, Color.white, "Title", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(86f, -15f), new Vector2(286f, 28f), string.Empty, 24, TextAnchor.MiddleLeft, bold: true);
					descriptions[i] = CreateText(((Component)(object)row).transform, font, new Color(0.84f, 0.91f, 1f), "Description", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(86f, -45f), new Vector2(292f, 28f), string.Empty, 18, TextAnchor.MiddleLeft, bold: false);
					Image iconSlot = CreatePanel(((Component)(object)row).transform, "IconSlot", new Vector2(-16f, 0f), new Vector2(58f, 58f), new Color(0.05f, 0.08f, 0.2f, 0.72f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), rounded: true, shadow: false);
					icons[i] = CreatePanel(((Component)(object)iconSlot).transform, "IconImage", Vector2.zero, new Vector2(38f, 38f), new Color(1f, 1f, 1f, 0.24f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), rounded: true, shadow: false);
					icons[i].preserveAspect = true;
				}
				((Component)(object)expandedPanel).gameObject.SetActive(value: false);
				synergySystem.Configure(gameController, boardManager, summaryButton, summaryHeader, ((Component)(object)expandedPanel).gameObject, expandedHeader, titles, descriptions, accents, icons, closeButton);
			}
		}

		private Transform CreateSafeAreaRoot(Transform parent, string rootName = "SafeAreaRoot")
		{
			GameObject safeAreaRoot = new GameObject(rootName, typeof(RectTransform));
			safeAreaRoot.transform.SetParent(parent, worldPositionStays: false);
			RectTransform rect = safeAreaRoot.GetComponent<RectTransform>();
			rect.anchorMin = Vector2.zero;
			rect.anchorMax = Vector2.one;
			rect.offsetMin = Vector2.zero;
			rect.offsetMax = Vector2.zero;
			safeAreaRoot.AddComponent<RuntimeSafeAreaFitter>();
			return safeAreaRoot.transform;
		}

		private void EnsureEventSystem()
		{
			if (!((UnityEngine.Object)(object)UnityEngine.Object.FindObjectOfType<EventSystem>() != null))
			{
				GameObject eventSystemObject = new GameObject("EventSystem");
				eventSystemObject.AddComponent<EventSystem>();
				eventSystemObject.AddComponent<StandaloneInputModule>();
			}
		}

		private Text CreateCurrencyPill(Transform parent, Font font, string name, string icon, Vector2 anchoredPosition, Vector2 size, Color accentColor, string value)
		{
			Image pill = CreatePanel(parent, name, anchoredPosition, size, new Color(0.1f, 0.13f, 0.31f, 0.92f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: true);
			CreatePanel(((Component)(object)pill).transform, "IconPlate", new Vector2(18f, -14f), new Vector2(44f, 44f), accentColor, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), rounded: true, shadow: false);
			Image iconImage = CreateSkinIcon(((Component)(object)pill).transform, "CurrencyIcon", name + " " + icon, new Vector2(40f, -36f), new Vector2(34f, 34f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), Color.white);
			if ((UnityEngine.Object)(object)iconImage == null)
			{
				CreateText(((Component)(object)pill).transform, font, Color.white, "IconLabel", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -14f), new Vector2(44f, 44f), icon, 18, TextAnchor.MiddleCenter, bold: true);
			}
			return CreateText(((Component)(object)pill).transform, font, Color.white, "ValueText", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-24f, 0f), new Vector2(size.x - 88f, 42f), value, 28, TextAnchor.MiddleRight, bold: true);
		}

		private Image CreateProgressBar(Transform parent, Vector2 anchoredPosition, Vector2 size)
		{
			Image background = CreatePanel(parent, "RoundProgressBar", anchoredPosition, size, new Color(0.13f, 0.17f, 0.3f, 0.96f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), rounded: true, shadow: true);
			Image fill = CreatePanel(((Component)(object)background).transform, "Fill", Vector2.zero, Vector2.zero, new Color(0.24f, 0.94f, 0.62f, 0.96f), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), rounded: true, shadow: false);
			fill.type = (Image.Type)3;
			fill.fillMethod = (Image.FillMethod)0;
			fill.fillOrigin = 0;
			fill.fillAmount = 0f;
			return fill;
		}

		private Text CreateBuildInsightCell(Transform parent, Font font, string name, string title, Vector2 anchoredPosition, Color accentColor)
		{
			Image cell = CreatePanel(parent, name, anchoredPosition, new Vector2(292f, 84f), new Color(0.08f, 0.13f, 0.32f, 0.9f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), rounded: true, shadow: false);
			CreatePanel(((Component)(object)cell).transform, "Accent", new Vector2(9f, 0f), new Vector2(10f, 60f), accentColor, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), rounded: true, shadow: false);
			CreateText(((Component)(object)cell).transform, font, Color.Lerp(accentColor, Color.white, 0.18f), "Title", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(10f, -9f), new Vector2(-18f, 30f), title, 22, TextAnchor.MiddleCenter, bold: true);
			Text value = CreateText(((Component)(object)cell).transform, font, Color.white, "Value", new Vector2(0f, 0f), Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(10f, -12f), new Vector2(-22f, 34f), "대기", 21, TextAnchor.MiddleCenter, bold: true);
			value.resizeTextForBestFit = true;
			value.fontSize = 24;
			((Graphic)value).rectTransform.anchoredPosition = new Vector2(10f, -18f);
			((Graphic)value).rectTransform.sizeDelta = new Vector2(-22f, 44f);
			value.resizeTextMinSize = 18;
			value.resizeTextMaxSize = 24;
			AddStrongTextOutline(value);
			return value;
		}

		private Text CreateGradeCard(Transform parent, Font font, CharacterGrade grade, Vector2 anchoredPosition, UnityAction onClick, string mergeRequirementText)
		{
			string title = CharacterGradeUtility.GetDisplayName(grade);
			Color accentColor = CharacterGradeUtility.GetColor(grade, Color.white);
			string cardName = grade.ToString() + "GradeCard";
			Text labelText;
			Button card = CreateButton(parent, font, cardName, string.Empty, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), anchoredPosition, new Vector2(144f, 124f), new Color(0.05f, 0.07f, 0.2f, 0.96f), Color.white, onClick, out labelText);
			if (onClick == null)
			{
				((Selectable)card).transition = (Selectable.Transition)0;
			}
			Image body = CreatePanel(((Component)(object)card).transform, "GradeBody", new Vector2(0f, -8f), new Vector2(132f, 108f), new Color(0.07f, 0.1f, 0.27f, 0.92f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
			((Component)(object)body).transform.SetAsFirstSibling();
			CreatePanel(((Component)(object)card).transform, "TitleBack", new Vector2(0f, -9f), new Vector2(120f, 34f), Color.Lerp(accentColor, new Color(0.02f, 0.04f, 0.14f, 1f), 0.1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
			Text titleText = CreateText(((Component)(object)card).transform, font, Color.white, "Title", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -11f), new Vector2(118f, 30f), title, 23, TextAnchor.MiddleCenter, bold: true);
			AddStrongTextOutline(titleText);
			CreatePanel(((Component)(object)card).transform, "MergeNeedBack", new Vector2(0f, -54f), new Vector2(108f, 27f), new Color(0.02f, 0.04f, 0.15f, 0.76f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), rounded: true, shadow: false);
			Text needText = CreateText(((Component)(object)card).transform, font, Color.Lerp(accentColor, Color.white, 0.28f), "MergeNeedText", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -54f), new Vector2(104f, 24f), mergeRequirementText, 16, TextAnchor.MiddleCenter, bold: true);
			AddStrongTextOutline(needText);
			CreatePanel(((Component)(object)card).transform, "CountBack", new Vector2(0f, 11f), new Vector2(116f, 30f), new Color(0.02f, 0.04f, 0.14f, 0.84f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), rounded: true, shadow: false);
			Text count = CreateText(((Component)(object)card).transform, font, Color.white, "Count", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 13f), new Vector2(114f, 27f), "0 / 3", 19, TextAnchor.MiddleCenter, bold: true);
			AddStrongTextOutline(count);
			if (grade == CharacterGrade.Transcendent)
			{
				Image top = CreatePanel(((Component)(object)card).transform, "ReadyGlowTop", new Vector2(0f, -3f), new Vector2(138f, 5f), Color.clear, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), rounded: true, shadow: false);
				Image right = CreatePanel(((Component)(object)card).transform, "ReadyGlowRight", new Vector2(-3f, 0f), new Vector2(5f, 118f), Color.clear, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), rounded: true, shadow: false);
				Image bottom = CreatePanel(((Component)(object)card).transform, "ReadyGlowBottom", new Vector2(0f, 3f), new Vector2(138f, 5f), Color.clear, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), rounded: true, shadow: false);
				Image left = CreatePanel(((Component)(object)card).transform, "ReadyGlowLeft", new Vector2(3f, 0f), new Vector2(5f, 118f), Color.clear, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), rounded: true, shadow: false);
				Image readyBadge = CreatePanel(((Component)(object)card).transform, "ReadyBadge", new Vector2(-3f, -3f), new Vector2(88f, 28f), new Color(1f, 0.72f, 0.16f, 0.98f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), rounded: true, shadow: false);
				Text readyBadgeText = CreateText(((Component)(object)readyBadge).transform, font, new Color(0.18f, 0.05f, 0.24f), "ReadyBadgeText", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, "READY", 14, TextAnchor.MiddleCenter, bold: true);
				AddStrongTextOutline(readyBadgeText);
				((Component)(object)top).gameObject.SetActive(value: false);
				((Component)(object)right).gameObject.SetActive(value: false);
				((Component)(object)bottom).gameObject.SetActive(value: false);
				((Component)(object)left).gameObject.SetActive(value: false);
				((Component)(object)readyBadge).gameObject.SetActive(value: false);
			}
			return count;
		}

		private Text CreateText(Transform parent, Font font, Color color, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 size, string value, int fontSize, TextAnchor alignment, bool bold)
		{
			GameObject textObject = new GameObject(name, typeof(RectTransform));
			textObject.transform.SetParent(parent, worldPositionStays: false);
			Text text = textObject.AddComponent<Text>();
			text.font = font;
			text.fontSize = fontSize;
			((Graphic)text).color = RuntimeUiSkinUtility.ResolveReadableTextColor(parent, color, (presentationConfig != null) ? presentationConfig.uiSkin : null);
			text.text = RuntimeKoreanTextUtility.Clean(name, value);
			text.alignment = alignment;
			text.fontStyle = (bold ? FontStyle.Bold : FontStyle.Normal);
			((Graphic)text).raycastTarget = false;
			RectTransform rect = ((Component)(object)text).GetComponent<RectTransform>();
			rect.anchorMin = anchorMin;
			rect.anchorMax = anchorMax;
			rect.pivot = pivot;
			rect.anchoredPosition = anchoredPosition;
			rect.sizeDelta = size;
			AddTextShadow(text);
			return text;
		}

		private Image CreatePanel(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, bool rounded, bool shadow)
		{
			GameObject panelObject = new GameObject(name, typeof(RectTransform));
			panelObject.transform.SetParent(parent, worldPositionStays: false);
			Image image = panelObject.AddComponent<Image>();
			((Graphic)image).color = color;
			((Graphic)image).raycastTarget = false;
			RuntimeUiSkinUtility.ApplyImageSkin(image, (presentationConfig != null) ? presentationConfig.uiSkin : null, name, isButton: false, rounded);
			ApplyRuntimeRoundedShape(image, rounded);
			RectTransform rect = ((Graphic)image).rectTransform;
			rect.anchorMin = anchorMin;
			rect.anchorMax = anchorMax;
			rect.pivot = pivot;
			rect.anchoredPosition = anchoredPosition;
			rect.sizeDelta = size;
			if (shadow)
			{
				Shadow shadowComponent = panelObject.AddComponent<Shadow>();
				shadowComponent.effectColor = new Color(0f, 0f, 0f, 0.32f);
				shadowComponent.effectDistance = new Vector2(0f, -6f);
			}
			return image;
		}

		private Button CreateButton(Transform parent, Font font, string name, string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 size, Color backgroundColor, Color labelColor, UnityAction onClick, out Text labelText)
		{
			GameObject buttonObject = new GameObject(name, typeof(RectTransform));
			buttonObject.transform.SetParent(parent, worldPositionStays: false);
			Image image = buttonObject.AddComponent<Image>();
			((Graphic)image).color = backgroundColor;
			RuntimeUiSkinUtility.ApplyImageSkin(image, (presentationConfig != null) ? presentationConfig.uiSkin : null, name, isButton: true, rounded: true);
			ApplyRuntimeRoundedShape(image, rounded: true);
			((Graphic)image).raycastTarget = true;
			Shadow shadow = buttonObject.AddComponent<Shadow>();
			shadow.effectColor = new Color(0f, 0f, 0f, 0.35f);
			shadow.effectDistance = new Vector2(0f, -7f);
			Button button = buttonObject.AddComponent<Button>();
			((Selectable)button).targetGraphic = (Graphic)(object)image;
			((UnityEvent)(object)button.onClick).AddListener((UnityAction)RuntimeAudioUtility.PlayButton);
			if (onClick != null)
			{
				((UnityEvent)(object)button.onClick).AddListener(onClick);
			}
			RectTransform rect = ((Component)(object)button).GetComponent<RectTransform>();
			rect.anchorMin = anchorMin;
			rect.anchorMax = anchorMax;
			rect.pivot = pivot;
			rect.anchoredPosition = anchoredPosition;
			rect.sizeDelta = size;
			labelText = CreateText(buttonObject.transform, font, labelColor, "Label", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, label, 27, TextAnchor.MiddleCenter, bold: true);
			TryAddButtonIcon(buttonObject.transform, name, label, size, labelText);
			return button;
		}

		private void ApplyRuntimeRoundedShape(Image image, bool rounded)
		{
			if (!((UnityEngine.Object)(object)image == null) && rounded)
			{
				if (image.sprite == null)
				{
					image.sprite = GetRoundedPanelSprite();
				}
				image.type = (Image.Type)1;
				image.preserveAspect = false;
			}
		}

		private Image CreateSkinIcon(Transform parent, string name, string iconKey, Vector2 anchoredPosition, Vector2 size, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Color color)
		{
			UiSkinResources skin = ((presentationConfig != null) ? presentationConfig.uiSkin : null);
			Sprite sprite = RuntimeUiSkinUtility.ResolveIconSprite(skin, iconKey);
			if (sprite == null)
			{
				return null;
			}
			GameObject iconObject = new GameObject(name, typeof(RectTransform));
			iconObject.transform.SetParent(parent, worldPositionStays: false);
			Image image = iconObject.AddComponent<Image>();
			image.sprite = sprite;
			image.type = (Image.Type)0;
			((Graphic)image).color = color;
			image.preserveAspect = true;
			((Graphic)image).raycastTarget = false;
			RectTransform rect = ((Graphic)image).rectTransform;
			rect.anchorMin = anchorMin;
			rect.anchorMax = anchorMax;
			rect.pivot = pivot;
			rect.anchoredPosition = anchoredPosition;
			rect.sizeDelta = size;
			return image;
		}

		private void BuildHamburgerIcon(Transform parent)
		{
			for (int i = 0; i < 3; i++)
			{
				float y = 10f - (float)i * 10f;
				CreatePanel(parent, "HamburgerLine_" + i, new Vector2(0f, y), new Vector2(34f, 5f), new Color(0.92f, 0.96f, 1f, 0.96f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), rounded: true, shadow: false);
			}
		}

		private void TryAddButtonIcon(Transform buttonTransform, string name, string label, Vector2 size, Text labelText)
		{
			if (string.IsNullOrWhiteSpace(label))
			{
				return;
			}
			string iconKey = name + " " + label;
			UiSkinResources skin = ((presentationConfig != null) ? presentationConfig.uiSkin : null);
			if (RuntimeUiSkinUtility.ResolveIconSprite(skin, iconKey) == null)
			{
				return;
			}
			bool iconOnly = string.Equals(label, "X", StringComparison.OrdinalIgnoreCase);
			if (!iconOnly)
			{
				return;
			}
			float iconSize = Mathf.Clamp(Mathf.Min(size.x, size.y) * 0.44f, 24f, 42f);
			Vector2 iconPosition = (iconOnly ? Vector2.zero : new Vector2(30f, 0f));
			Vector2 anchor = (iconOnly ? new Vector2(0.5f, 0.5f) : new Vector2(0f, 0.5f));
			Image icon = CreateSkinIcon(buttonTransform, "ButtonIcon", iconKey, iconPosition, new Vector2(iconSize, iconSize), anchor, anchor, new Vector2(0.5f, 0.5f), Color.white);
			if (!((UnityEngine.Object)(object)icon == null) && !((UnityEngine.Object)(object)labelText == null))
			{
				if (iconOnly)
				{
					((Behaviour)(object)labelText).enabled = false;
					return;
				}
				RectTransform labelRect = ((Graphic)labelText).rectTransform;
				Vector2 offsetMin = labelRect.offsetMin;
				offsetMin.x = Mathf.Max(offsetMin.x, iconSize + 24f);
				labelRect.offsetMin = offsetMin;
			}
		}

		private void AddTextShadow(Text text)
		{
			Shadow shadow = ((Component)(object)text).gameObject.AddComponent<Shadow>();
			shadow.effectColor = new Color(0f, 0f, 0f, 0.42f);
			shadow.effectDistance = new Vector2(2f, -2f);
		}

		private void AddCardOutline(Button button, Color color, float distance)
		{
			if (!((UnityEngine.Object)(object)button == null))
			{
				Outline outline = ((Component)(object)button).GetComponent<Outline>();
				if ((UnityEngine.Object)(object)outline == null)
				{
					outline = ((Component)(object)button).gameObject.AddComponent<Outline>();
				}
				float safeDistance = Mathf.Max(1f, distance);
				((Shadow)outline).effectColor = color;
				((Shadow)outline).effectDistance = new Vector2(safeDistance, 0f - safeDistance);
				((Shadow)outline).useGraphicAlpha = true;
			}
		}

		private void AddStrongTextOutline(Text text)
		{
			if (!((UnityEngine.Object)(object)text == null))
			{
				Outline outline = ((Component)(object)text).GetComponent<Outline>();
				if ((UnityEngine.Object)(object)outline == null)
				{
					outline = ((Component)(object)text).gameObject.AddComponent<Outline>();
				}
				((Shadow)outline).effectColor = new Color(0f, 0f, 0f, 0.82f);
				((Shadow)outline).effectDistance = new Vector2(1.7f, -1.7f);
			}
		}

		private Sprite GetRoundedPanelSprite()
		{
			if (roundedPanelSprite == null)
			{
				roundedPanelSprite = CreateRuntimeSprite("RuntimeRoundedPanel", 64, 64, 18f);
			}
			return roundedPanelSprite;
		}

		private Sprite GetCircleSprite()
		{
			if (circleSprite == null)
			{
				circleSprite = CreateRuntimeSprite("RuntimeCircle", 64, 64, 32f);
			}
			return circleSprite;
		}

		private Sprite CreateRuntimeSprite(string name, int width, int height, float radius)
		{
			Texture2D texture = new Texture2D(width, height, TextureFormat.ARGB32, mipChain: false);
			texture.name = name;
			texture.wrapMode = TextureWrapMode.Clamp;
			Color[] pixels = new Color[width * height];
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					float nearestX = Mathf.Clamp(x, radius, (float)width - radius - 1f);
					float nearestY = Mathf.Clamp(y, radius, (float)height - radius - 1f);
					float distance = Vector2.Distance(new Vector2(x, y), new Vector2(nearestX, nearestY));
					float alpha = Mathf.Clamp01(radius + 0.5f - distance);
					pixels[y * width + x] = new Color(1f, 1f, 1f, alpha);
				}
			}
			texture.SetPixels(pixels);
			texture.Apply();
			return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f, 0u, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
		}

		private void ReplaceNamedPrimitive(Transform parent, string name, PrimitiveType primitiveType, Vector3 position, Vector3 scale, Color color)
		{
			Transform existing = parent.Find(name);
			if (existing != null)
			{
				SafeDestroy(existing.gameObject);
			}
			GameObject primitive = GameObject.CreatePrimitive(primitiveType);
			primitive.name = name;
			primitive.transform.SetParent(parent);
			primitive.transform.position = position;
			primitive.transform.localScale = scale;
			Renderer renderer = primitive.GetComponent<Renderer>();
			if (renderer != null)
			{
				renderer.material.color = color;
			}
		}

		private Color GetSlotColor(int index)
		{
			Color[] colors = ((presentationConfig != null && presentationConfig.slotColors != null && presentationConfig.slotColors.Length != 0) ? presentationConfig.slotColors : DefaultSlotColors);
			return colors[index % colors.Length];
		}

		private Color GetLaneColor(int index)
		{
			Color[] colors = ((presentationConfig != null && presentationConfig.laneColors != null && presentationConfig.laneColors.Length != 0) ? presentationConfig.laneColors : DefaultLaneColors);
			return colors[index % colors.Length];
		}

		private Color GetConfigColor(Func<GamePresentationConfig, Color> selector, Color fallback)
		{
			return (presentationConfig != null) ? selector(presentationConfig) : fallback;
		}

		private void SafeDestroy(GameObject target)
		{
			if (Application.isPlaying)
			{
				UnityEngine.Object.Destroy(target);
			}
			else
			{
				UnityEngine.Object.DestroyImmediate(target);
			}
		}

		private void SafeDestroy(Component target)
		{
			if (!(target == null))
			{
				if (Application.isPlaying)
				{
					UnityEngine.Object.Destroy(target);
				}
				else
				{
					UnityEngine.Object.DestroyImmediate(target);
				}
			}
		}

		private void AssignPrivateField(UnityEngine.Object target, string fieldName, object value)
		{
			FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
			if (field != null)
			{
				field.SetValue(target, value);
			}
		}
	}
    /// <summary>Run-scoped stat and summon-quality investments. Both remain available during combat unless a blocking choice or end state is active.</summary>
    public sealed class GradeUpgradeBarUI : MonoBehaviour
    {
        private static readonly CharacterGrade[] Grades =
        {
            CharacterGrade.Normal, CharacterGrade.Rare, CharacterGrade.Epic,
            CharacterGrade.Legendary, CharacterGrade.Mythic, CharacterGrade.Transcendent
        };

        private DefenseGameController controller;
        private Button[] buttons;
        private Text[] labels;
        private Image[] bodies;
        private Button summonGradeLuckButton;
        private Text summonGradeLuckLabel;
        private Image summonGradeLuckBody;
        private Button summonGradeLuckInfoButton;
        private GameObject summonGradeLuckInfoTooltip;
        private CanvasGroup canvasGroup;
        private bool subscribed;
        private bool infoVisible;
        private bool observedRound;
        private int lastObservedRound;

        public static GradeUpgradeBarUI Create(Transform parent, Font font, DefenseGameController controller, GamePresentationConfig presentationConfig)
        {
            UiSkinResources skin = presentationConfig != null ? presentationConfig.uiSkin : null;
            GameObject root = new GameObject("GradeUpgradeBar", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0f);
            rootRect.anchorMax = new Vector2(0.5f, 0f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = new Vector2(0f, 470f);
            rootRect.sizeDelta = new Vector2(850f, 214f);

            Image background = root.AddComponent<Image>();
            background.color = new Color(0.035f, 0.08f, 0.20f, 0.88f);
            background.raycastTarget = false;
            RuntimeUiSkinUtility.ApplyImageSkin(background, skin, "GradeUpgradeBar", false, true);

            Text title = CreateText(root.transform, font, "GradeUpgradeTitle", new Vector2(0f, 96f), new Vector2(810f, 22f), "\ub4f1\uae09 \uac15\ud654  \u00b7  \uc804\ud22c \uc911 \uc989\uc2dc \uc801\uc6a9", 16, new Color(0.72f, 0.91f, 1f));
            title.alignment = TextAnchor.MiddleCenter;

            Button[] buttons = new Button[Grades.Length];
            Text[] labels = new Text[Grades.Length];
            Image[] bodies = new Image[Grades.Length];
            for (int i = 0; i < Grades.Length; i++)
            {
                CharacterGrade grade = Grades[i];
                GameObject buttonObject = new GameObject("GradeUpgrade_" + grade, typeof(RectTransform), typeof(Image), typeof(Button));
                buttonObject.transform.SetParent(root.transform, false);
                RectTransform rect = buttonObject.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(-310f + i * 124f, -68f);
                rect.sizeDelta = new Vector2(116f, 58f);
                Image body = buttonObject.GetComponent<Image>();
                body.color = new Color(0.08f, 0.13f, 0.28f, 0.98f);
                RuntimeUiSkinUtility.ApplyImageSkin(body, skin, "GradeUpgradeButton", true, true);

                Text label = CreateText(buttonObject.transform, font, "Label", Vector2.zero, new Vector2(108f, 52f), string.Empty, 15, Color.white);
                label.alignment = TextAnchor.MiddleCenter;
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 11;
                label.resizeTextMaxSize = 15;
                AddTextShadow(label);

                int capturedIndex = i;
                buttonObject.GetComponent<Button>().onClick.AddListener(delegate
                {
                    controller?.TryUpgradeGrade(Grades[capturedIndex]);
                });
                buttons[i] = buttonObject.GetComponent<Button>();
                labels[i] = label;
                bodies[i] = body;
            }

            GameObject luckObject = new GameObject("SummonGradeLuckUpgrade", typeof(RectTransform), typeof(Image), typeof(Button));
            luckObject.transform.SetParent(root.transform, false);
            RectTransform luckRect = luckObject.GetComponent<RectTransform>();
            luckRect.anchorMin = new Vector2(0.5f, 0.5f);
            luckRect.anchorMax = new Vector2(0.5f, 0.5f);
            luckRect.pivot = new Vector2(0.5f, 0.5f);
            luckRect.anchoredPosition = new Vector2(-230f, 36f);
            luckRect.sizeDelta = new Vector2(350f, 84f);
            Image luckBody = luckObject.GetComponent<Image>();
            luckBody.color = new Color(0.14f, 0.10f, 0.28f, 0.98f);
            RuntimeUiSkinUtility.ApplyImageSkin(luckBody, skin, "GradeUpgradeButton", true, true);
            Text luckLabel = CreateText(luckObject.transform, font, "Label", Vector2.zero, new Vector2(304f, 72f), string.Empty, 17, Color.white);
            luckLabel.alignment = TextAnchor.MiddleCenter;
            luckLabel.supportRichText = true;
            luckLabel.resizeTextForBestFit = true;
            luckLabel.resizeTextMinSize = 13;
            luckLabel.resizeTextMaxSize = 17;
            AddTextShadow(luckLabel);
            luckObject.GetComponent<Button>().onClick.AddListener(delegate
            {
                controller?.TryUpgradeSummonGradeLuck();
            });

            GameObject infoObject = new GameObject("SummonGradeLuckInfoButton", typeof(RectTransform), typeof(Image), typeof(Button));
            infoObject.transform.SetParent(root.transform, false);
            RectTransform infoRect = infoObject.GetComponent<RectTransform>();
            infoRect.anchorMin = new Vector2(0.5f, 0.5f);
            infoRect.anchorMax = new Vector2(0.5f, 0.5f);
            infoRect.pivot = new Vector2(0.5f, 0.5f);
            infoRect.anchoredPosition = new Vector2(-80f, 54f);
            infoRect.sizeDelta = new Vector2(34f, 34f);
            Image infoBody = infoObject.GetComponent<Image>();
            infoBody.color = new Color(0.16f, 0.54f, 0.82f, 0.98f);
            RuntimeUiSkinUtility.ApplyImageSkin(infoBody, skin, "InfoButton", true, true);
            Image infoIcon = CreateIcon(infoObject.transform, "Icon", new Vector2(28f, 28f));
            if (!RuntimeUiSkinUtility.ApplyIconSkin(infoIcon, skin, "info"))
            {
                infoIcon.sprite = Resources.Load<Sprite>("UI/RollRoll/Common/inventory-minimi-itemrenderer-detail-info-button-icon");
            }
            if (infoIcon.sprite == null)
            {
                Text infoFallback = CreateText(infoObject.transform, font, "Fallback", Vector2.zero, new Vector2(28f, 28f), "i", 22, Color.white);
                infoFallback.alignment = TextAnchor.MiddleCenter;
            }

            GameObject tooltip = CreateInfoTooltip(root.transform, font, skin);
            root.AddComponent<CanvasGroup>();
            GradeUpgradeBarUI ui = root.AddComponent<GradeUpgradeBarUI>();
            infoObject.GetComponent<Button>().onClick.AddListener(ui.ToggleInfoTooltip);
            ui.Configure(controller, buttons, labels, bodies, luckObject.GetComponent<Button>(), luckLabel, luckBody, infoObject.GetComponent<Button>(), tooltip);
            return ui;
        }

        public void Configure(DefenseGameController value, Button[] newButtons, Text[] newLabels, Image[] newBodies, Button newSummonGradeLuckButton, Text newSummonGradeLuckLabel, Image newSummonGradeLuckBody, Button newSummonGradeLuckInfoButton, GameObject newSummonGradeLuckInfoTooltip)
        {
            Unsubscribe();
            controller = value;
            canvasGroup = GetComponent<CanvasGroup>();
            buttons = newButtons;
            labels = newLabels;
            bodies = newBodies;
            summonGradeLuckButton = newSummonGradeLuckButton;
            summonGradeLuckLabel = newSummonGradeLuckLabel;
            summonGradeLuckBody = newSummonGradeLuckBody;
            summonGradeLuckInfoButton = newSummonGradeLuckInfoButton;
            summonGradeLuckInfoTooltip = newSummonGradeLuckInfoTooltip;
            SetInfoTooltipVisible(false);
            Subscribe();
            Refresh();
        }

        private void OnEnable()
        {
            Subscribe();
            Refresh();
        }

        private void OnDisable()
        {
            SetInfoTooltipVisible(false);
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (subscribed || controller == null)
            {
                return;
            }

            controller.OnStateChanged += Refresh;
            controller.OnGameOver += HideInfoTooltip;
            controller.OnRunReset += HideInfoTooltip;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || controller == null)
            {
                subscribed = false;
                return;
            }

            controller.OnStateChanged -= Refresh;
            controller.OnGameOver -= HideInfoTooltip;
            controller.OnRunReset -= HideInfoTooltip;
            subscribed = false;
        }

        private void ToggleInfoTooltip()
        {
            SetInfoTooltipVisible(!infoVisible);
        }

        private void HideInfoTooltip()
        {
            SetInfoTooltipVisible(false);
        }

        private void SetInfoTooltipVisible(bool visible)
        {
            infoVisible = visible;
            if (summonGradeLuckInfoTooltip != null)
            {
                summonGradeLuckInfoTooltip.SetActive(visible);
            }
        }

        private void Refresh()
        {
            if (controller == null || buttons == null || labels == null || bodies == null)
            {
                return;
            }

            int currentRound = controller.CurrentRound;
            if (observedRound && currentRound < lastObservedRound)
            {
                HideInfoTooltip();
            }
            lastObservedRound = currentRound;
            observedRound = true;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
                canvasGroup.interactable = true;
            }

            for (int i = 0; i < Grades.Length && i < buttons.Length && i < labels.Length && i < bodies.Length; i++)
            {
                CharacterGrade grade = Grades[i];
                int level = controller.GetGradeUpgradeLevel(grade);
                bool isMax = level >= DefenseGameController.GradeUpgradeMaximumLevel;
                int cost = controller.GetGradeUpgradeCost(grade);
                bool available = controller.CanUpgradeGrade(grade);
                Color gradeColor = CharacterGradeUtility.GetColor(grade, Color.white);
                buttons[i].interactable = available;
                bodies[i].color = available
                    ? Color.Lerp(new Color(0.06f, 0.10f, 0.26f, 0.98f), gradeColor, 0.24f)
                    : new Color(0.09f, 0.10f, 0.16f, 0.66f);
                labels[i].color = isMax ? Color.Lerp(gradeColor, Color.white, 0.25f) : (available ? Color.white : new Color(0.72f, 0.74f, 0.80f));
                string gradeName = CharacterGradeUtility.GetDisplayName(grade);
                labels[i].text = isMax
                    ? gradeName + "\nLv.MAX"
                    : gradeName + "  Lv." + level + "\n\u2191 " + cost + "G";
            }

            if (summonGradeLuckButton != null && summonGradeLuckLabel != null && summonGradeLuckBody != null)
            {
                int level = controller.SummonGradeLuckLevel;
                bool isMax = level >= DefenseGameController.SummonGradeLuckMaximumLevel;
                int cost = controller.GetSummonGradeLuckCost();
                bool available = controller.CanUpgradeSummonGradeLuck();
                summonGradeLuckButton.interactable = available;
                summonGradeLuckBody.color = available
                    ? new Color(0.30f, 0.20f, 0.56f, 0.98f)
                    : new Color(0.13f, 0.12f, 0.19f, 0.66f);
                summonGradeLuckLabel.color = isMax ? new Color(0.94f, 0.82f, 1f) : (available ? Color.white : new Color(0.72f, 0.74f, 0.80f));
                summonGradeLuckLabel.text = isMax
                    ? "\uace0\ub4f1\uae09 \ud655\ub960\nLv.MAX  |  Epic+ +<color=#FFD84A>7</color>%p"
                    : "\uace0\ub4f1\uae09 \ud655\ub960\nLv.<color=#FFD84A>" + level + "</color>  |  Epic+ +<color=#FFD84A>" + level + "</color>%p\n<color=#FFD84A>" + cost + "</color> GOLD";
            }

            if (summonGradeLuckInfoButton != null)
            {
                summonGradeLuckInfoButton.interactable = !controller.IsBlockingChoiceOpen && !controller.FateCardChoicePanelOpen;
            }
        }

        private static GameObject CreateInfoTooltip(Transform parent, Font font, UiSkinResources skin)
        {
            GameObject tooltip = new GameObject("SummonGradeLuckInfoTooltip", typeof(RectTransform), typeof(Image));
            tooltip.transform.SetParent(parent, false);
            RectTransform rect = tooltip.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(-88f, 168f);
            rect.sizeDelta = new Vector2(620f, 210f);
            Image body = tooltip.GetComponent<Image>();
            body.color = new Color(0.04f, 0.10f, 0.25f, 0.97f);
            body.raycastTarget = false;
            RuntimeUiSkinUtility.ApplyImageSkin(body, skin, "InfoTooltip", false, true);

            Text title = CreateText(tooltip.transform, font, "Title", new Vector2(0f, 73f), new Vector2(570f, 38f), "\uace0\ub4f1\uae09 \ud655\ub960 \uac15\ud654", 23, new Color(1f, 0.86f, 0.32f));
            title.alignment = TextAnchor.MiddleCenter;
            Text bodyText = CreateText(tooltip.transform, font, "Body", new Vector2(0f, -24f), new Vector2(570f, 126f), "\uc77c\ubc18 \uc18c\ud658\uc5d0\uc11c\ub9cc \uc801\uc6a9\n\ub808\ubca8\ub2f9 \uc5d0\ud53d \uc774\uc0c1 \ub4f1\uc7a5 \ud655\ub960 +1%p\n\uace0\ub4f1\uae09 \ub4f1\uc7a5 \ub2e8\uacc4\ubd80\ud130 \uc801\uc6a9\n\ud2b9\uc218 \uc18c\ud658\u00b7\uc0c1\uc810\u00b7\ub808\uc2dc\ud53c\u00b7\ubcf4\uc0c1 \uc18c\ud658\uc5d0\ub294 \ubbf8\uc801\uc6a9", 20, new Color(0.88f, 0.94f, 1f));
            bodyText.alignment = TextAnchor.MiddleCenter;
            bodyText.resizeTextForBestFit = false;
            tooltip.SetActive(false);
            return tooltip;
        }

        private static Image CreateIcon(Transform parent, string name, Vector2 size)
        {
            GameObject iconObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(parent, false);
            RectTransform rect = iconObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
            Image icon = iconObject.GetComponent<Image>();
            icon.color = Color.white;
            icon.raycastTarget = false;
            icon.preserveAspect = true;
            return icon;
        }

        private static void AddTextShadow(Text text)
        {
            Shadow shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
            shadow.effectDistance = new Vector2(1f, -1f);
        }

        private static Text CreateText(Transform parent, Font font, string name, Vector2 position, Vector2 size, string value, int fontSize, Color color)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Text text = textObject.GetComponent<Text>();
            text.font = font;
            text.fontStyle = FontStyle.Bold;
            text.fontSize = fontSize;
            text.color = color;
            text.text = value;
            text.raycastTarget = false;
            return text;
        }
    }
}
