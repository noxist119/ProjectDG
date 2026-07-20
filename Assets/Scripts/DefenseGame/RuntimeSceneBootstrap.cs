
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DefenseGame
{
    public class RuntimeSceneBootstrap : MonoBehaviour
    {
        private const float DefaultSlotLineZ = -8.45f;
        private const float DefaultGoalLineZ = -9.75f;

        [SerializeField] private bool buildOnStart = true;
        [SerializeField] private int slotCount = 10;
        [SerializeField] private int frontSlotCount = 5;
        [SerializeField] private float backSlotZOffset = -0.34f;
        [SerializeField] private float frontSlotZOffset = 1.28f;
        [SerializeField] private int laneCount = 4;
        [SerializeField] private Vector3 boardCenter = new Vector3(0f, 0f, DefaultSlotLineZ);
        [SerializeField] private Vector3 spawnCenter = new Vector3(0f, 0f, 8f);
        [SerializeField] private float laneSpacing = 3.2f;
        [SerializeField] private float slotSpacing = 1.2f;
        [SerializeField] private GamePresentationConfig presentationConfig;
        [SerializeField] private CharacterCombatTuningConfig characterCombatTuningConfig;
        [SerializeField] private MonsterCombatTuningConfig monsterCombatTuningConfig;
        [SerializeField] private OutgameProgressionConfig outgameProgressionConfig;
        [SerializeField] private bool hideDefaultStageDecorWhenUsingBackground = true;
        [SerializeField] private bool playMainBgm = true;
        [SerializeField] private string mainBgmResourcePath = "Audio/MainBGM";
        [SerializeField] private string bossBgmResourcePath = "Audio/BossBGM";
        [SerializeField, Range(0f, 1f)] private float mainBgmVolume = 0.55f;
        [SerializeField, Range(0f, 1f)] private float bossBgmVolume = 0.72f;
        [SerializeField] private float bgmFadeDuration = 0.6f;

        private static readonly Color[] DefaultSlotColors =
        {
            new Color(0.30f, 0.56f, 0.93f),
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

        private static readonly Color[] DefaultLaneColors =
        {
            new Color(0.16f, 0.70f, 0.98f),
            new Color(0.24f, 0.88f, 0.54f),
            new Color(0.98f, 0.66f, 0.20f),
            new Color(0.94f, 0.28f, 0.43f),
            new Color(0.72f, 0.38f, 0.95f)
        };

        private static Sprite roundedPanelSprite;
        private static Sprite circleSprite;

        private void Start()
        {
            if (buildOnStart)
            {
                float buildStartedAt = Time.realtimeSinceStartup;
                BuildScene();
                Debug.Log("[RuntimeSceneBootstrap] Runtime stage ready in " +
                    (Time.realtimeSinceStartup - buildStartedAt).ToString("F2") + "s");
            }
        }

        [ContextMenu("Build Runtime Stage")]
        public void BuildScene()
        {
            CharacterDatabase characterDatabase = GetOrAdd<CharacterDatabase>(gameObject);
            MonsterDatabase monsterDatabase = GetOrAdd<MonsterDatabase>(gameObject);
            DefenseBoardManager boardManager = GetOrAdd<DefenseBoardManager>(gameObject);
            RoundManager roundManager = GetOrAdd<RoundManager>(gameObject);
            DefenseGameController gameController = GetOrAdd<DefenseGameController>(gameObject);
            DemoInputController demoInput = GetOrAdd<DemoInputController>(gameObject);
            GameUIButtonBinder buttonBinder = GetOrAdd<GameUIButtonBinder>(gameObject);
            SimpleGameHUD hud = GetOrAdd<SimpleGameHUD>(gameObject);
            AugmentManager augmentManager = GetOrAdd<AugmentManager>(gameObject);
            CharacterCollectionUI collectionUI = GetOrAdd<CharacterCollectionUI>(gameObject);
            MetaFlowUI metaFlowUI = GetOrAdd<MetaFlowUI>(gameObject);
            OutgameProgressionSystem outgameProgression = GetOrAdd<OutgameProgressionSystem>(gameObject);
            BoardSynergySystem synergySystem = GetOrAdd<BoardSynergySystem>(gameObject);
            TacticalMissionSystem missionSystem = GetOrAdd<TacticalMissionSystem>(gameObject);
            BoardTileModifierSystem tileModifierSystem = GetOrAdd<BoardTileModifierSystem>(gameObject);
            RunShopSystem runShopSystem = GetOrAdd<RunShopSystem>(gameObject);

            RuntimeRenderBatchingUtility.Configure(presentationConfig);
            MonsterUnit.ConfigurePetrifyMaterial(characterCombatTuningConfig != null ? characterCombatTuningConfig.defaultPetrifyMaterial : null);
            characterDatabase.ApplyPresentationConfig(presentationConfig);
            characterDatabase.ApplyCombatTuningConfig(characterCombatTuningConfig);
            outgameProgression.Configure(outgameProgressionConfig, characterDatabase);
            monsterDatabase.ApplyPresentationConfig(presentationConfig);
            monsterDatabase.ApplyCombatTuningConfig(monsterCombatTuningConfig);

            Transform root = EnsureRoot("RuntimeStageRoot");
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
            if (!(useCustomBackground && hideDefaultStageDecorWhenUsingBackground))
            {
                BuildCenterCrystal(miscRoot);
                BuildFlankTowers(miscRoot);
                BuildSkyOrnaments(miscRoot);
            }

            Projectile projectileTemplate = BuildProjectileTemplate(templateRoot);
            DefenderUnit defenderTemplate = BuildDefenderTemplate(templateRoot, projectileTemplate);
            MonsterUnit monsterTemplate = BuildMonsterTemplate(templateRoot);

            boardManager.Configure(slots, defenderTemplate);
            roundManager.Configure(
                monsterDatabase,
                monsterTemplate,
                spawnPoints,
                goalPoint,
                presentationConfig != null ? presentationConfig.spawnPortalPrefab : null);
            gameController.Configure(characterDatabase, monsterDatabase, boardManager, roundManager, defenderTemplate);
            tileModifierSystem.Configure(gameController, boardManager);
            demoInput.Configure(gameController);
            buttonBinder.Configure(gameController);

            BuildCanvas(root, hud, gameController, boardManager, buttonBinder, augmentManager, collectionUI, metaFlowUI, synergySystem, missionSystem, tileModifierSystem, runShopSystem, characterDatabase, outgameProgression);
            EnsureRuntimeBgm(gameController);
        }

        private void EnsureRuntimeBgm(DefenseGameController gameController)
        {
            if (!Application.isPlaying || !playMainBgm)
            {
                return;
            }

            Transform existing = transform.Find("BGMPlayer");
            if (existing == null)
            {
                existing = transform.Find("MainBGMPlayer");
            }

            GameObject player = existing != null ? existing.gameObject : new GameObject("BGMPlayer");
            player.name = "BGMPlayer";
            if (existing == null)
            {
                player.transform.SetParent(transform, false);
            }

            RuntimeBgmController bgmController = player.GetComponent<RuntimeBgmController>();
            if (bgmController == null)
            {
                bgmController = player.AddComponent<RuntimeBgmController>();
            }

            bgmController.Configure(gameController, mainBgmResourcePath, bossBgmResourcePath, mainBgmVolume, bossBgmVolume, bgmFadeDuration);
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
            Transform existing = transform.Find(name);
            if (existing != null)
            {
                return existing;
            }

            GameObject root = new GameObject(name);
            root.transform.SetParent(transform);
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
            Transform child = parent != null ? parent.Find(childName) : null;
            if (child != null)
            {
                SafeDestroy(child.gameObject);
            }
        }

        private void EnsureGround(Transform root)
        {
            ReplaceNamedPrimitive(root, "Ground", PrimitiveType.Plane, new Vector3(0f, -0.5f, 0f), new Vector3(2f, 1f, 1.8f), GetConfigColor(config => config.groundColor, new Color(0.08f, 0.11f, 0.14f)));
            ReplaceNamedPrimitive(root, "BoardStrip", PrimitiveType.Cube, new Vector3(0f, -0.15f, -5.5f), new Vector3(20f, 0.25f, 2.6f), GetConfigColor(config => config.boardStripColor, new Color(0.12f, 0.18f, 0.24f)));
            ReplaceNamedPrimitive(root, "EnemyRunway", PrimitiveType.Cube, new Vector3(0f, -0.15f, 2.1f), new Vector3(20f, 0.2f, 12.5f), GetConfigColor(config => config.enemyRunwayColor, new Color(0.18f, 0.10f, 0.11f)));
            ReplaceNamedPrimitive(root, "MidBridge", PrimitiveType.Cube, new Vector3(0f, -0.12f, -1.6f), new Vector3(20f, 0.08f, 1.2f), GetConfigColor(config => config.midBridgeColor, new Color(0.25f, 0.29f, 0.36f)));
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
                GameObject overrideObject = Instantiate(presentationConfig.backgroundPrefab, root);
                overrideObject.name = "BackgroundOverride";
                overrideObject.transform.localPosition = Vector3.zero;
                overrideObject.transform.localRotation = Quaternion.identity;
                overrideObject.transform.localScale = Vector3.one;
                return;
            }

            ReplaceNamedPrimitive(root, "NorthWall", PrimitiveType.Cube, new Vector3(0f, 2.5f, 10.5f), new Vector3(24f, 5f, 0.5f), GetConfigColor(config => config.northWallColor, new Color(0.17f, 0.14f, 0.22f)));
            ReplaceNamedPrimitive(root, "SouthWall", PrimitiveType.Cube, new Vector3(0f, 2f, -9.8f), new Vector3(24f, 4f, 0.5f), GetConfigColor(config => config.southWallColor, new Color(0.13f, 0.19f, 0.24f)));
            ReplaceNamedPrimitive(root, "LeftCliff", PrimitiveType.Cube, new Vector3(-11.2f, 1.5f, 0f), new Vector3(1.2f, 3f, 21f), GetConfigColor(config => config.sideWallColor, new Color(0.12f, 0.14f, 0.18f)));
            ReplaceNamedPrimitive(root, "RightCliff", PrimitiveType.Cube, new Vector3(11.2f, 1.5f, 0f), new Vector3(1.2f, 3f, 21f), GetConfigColor(config => config.sideWallColor, new Color(0.12f, 0.14f, 0.18f)));
            ReplaceNamedPrimitive(root, "LeftBanner", PrimitiveType.Cube, new Vector3(-9.5f, 3.5f, -5.7f), new Vector3(1.2f, 2.8f, 0.2f), GetLaneColor(0));
            ReplaceNamedPrimitive(root, "RightBanner", PrimitiveType.Cube, new Vector3(9.5f, 3.5f, -5.7f), new Vector3(1.2f, 2.8f, 0.2f), GetLaneColor(3));
        }
        private void EnsureCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.tag = "MainCamera";
            }

            Vector3 widePosition = new Vector3(0f, 15f, -12.4f);
            Vector3 portraitPosition = new Vector3(0f, 17.6f, -16.1f);
            Vector3 cameraEuler = new Vector3(53f, 0f, 0f);
            camera.transform.position = widePosition;
            camera.transform.rotation = Quaternion.Euler(cameraEuler);
            camera.backgroundColor = new Color(0.05f, 0.07f, 0.11f);
            camera.clearFlags = CameraClearFlags.SolidColor;
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
            Light light = FindObjectOfType<Light>();
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
            float backWidth = (backCount - 1) * slotSpacing;
            Vector3 backCenter = boardCenter;
            if (backCenter.z > DefaultSlotLineZ)
            {
                backCenter.z = DefaultSlotLineZ;
            }

            backCenter.z += backSlotZOffset;

            for (int i = 0; i < backCount; i++)
            {
                Vector3 position = backCenter + new Vector3(-backWidth * 0.5f + i * slotSpacing, 0f, 0f);
                slots.Add(CreateRuntimeBoardSlot(boardRoot, "Slot_" + i.ToString("D2"), position, i));
            }

            int frontCount = Mathf.Max(0, frontSlotCount);
            if (frontCount > 0)
            {
                float frontSpacing = slotSpacing * 1.42f;
                float frontWidth = (frontCount - 1) * frontSpacing;
                Vector3 frontCenter = backCenter + new Vector3(0f, 0f, frontSlotZOffset);
                for (int i = 0; i < frontCount; i++)
                {
                    Vector3 position = frontCenter + new Vector3(-frontWidth * 0.5f + i * frontSpacing, 0f, 0f);
                    slots.Add(CreateRuntimeBoardSlot(boardRoot, "FrontSlot_" + i.ToString("D2"), position, backCount + i));
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
            anchor.transform.SetParent(slotObject.transform, false);
            anchor.transform.localPosition = new Vector3(0f, 1.38f, 0f);
            AssignPrivateField(slot, "unitAnchor", anchor.transform);

            BuildSlotVisual(slotObject.transform, baseColor, trimColor);
            return slot;
        }
        private void BuildSlotVisual(Transform slotRoot, Color baseColor, Color trimColor)
        {
            Color dark = Color.Lerp(baseColor, new Color(0.03f, 0.04f, 0.09f), 0.58f);
            Color bright = Color.Lerp(trimColor, Color.white, 0.28f);
            CreateSlotPrimitive(slotRoot, "TopInset", PrimitiveType.Cube, new Vector3(0f, 0.60f, 0f), new Vector3(0.78f, 0.030f, 0.72f), Color.Lerp(baseColor, Color.white, 0.18f));
            CreateSlotPrimitive(slotRoot, "FrontLip", PrimitiveType.Cube, new Vector3(0f, 0.68f, -0.48f), new Vector3(0.88f, 0.040f, 0.035f), bright);
            CreateSlotPrimitive(slotRoot, "BackLip", PrimitiveType.Cube, new Vector3(0f, 0.62f, 0.48f), new Vector3(0.80f, 0.028f, 0.026f), dark);
            CreateSlotPrimitive(slotRoot, "AuraDisc", PrimitiveType.Cylinder, new Vector3(0f, 0.72f, 0f), new Vector3(0.44f, 0.018f, 0.44f), Color.Lerp(baseColor, Color.white, 0.05f));
            CreateSlotPrimitive(slotRoot, "SummonHalo", PrimitiveType.Cylinder, new Vector3(0f, 0.755f, 0f), new Vector3(0.62f, 0.012f, 0.62f), Color.Lerp(trimColor, Color.white, 0.38f));
            CreateSlotPrimitive(slotRoot, "SummonCore", PrimitiveType.Sphere, new Vector3(0f, 0.82f, 0f), Vector3.one * 0.065f, Color.Lerp(bright, Color.white, 0.30f));

            CreateSlotLine(slotRoot, "SlotBorder", bright, 0.020f,
                new Vector3(-0.50f, 0.82f, -0.45f),
                new Vector3(0.50f, 0.82f, -0.45f),
                new Vector3(0.50f, 0.82f, 0.45f),
                new Vector3(-0.50f, 0.82f, 0.45f),
                new Vector3(-0.50f, 0.82f, -0.45f));

            CreateSlotLine(slotRoot, "SlotRune", Color.Lerp(baseColor, Color.white, 0.50f), 0.014f,
                new Vector3(0f, 0.86f, -0.28f),
                new Vector3(0.30f, 0.86f, 0f),
                new Vector3(0f, 0.86f, 0.28f),
                new Vector3(-0.30f, 0.86f, 0f),
                new Vector3(0f, 0.86f, -0.28f));

            CreateSlotLine(slotRoot, "SummonChevronFront", Color.Lerp(trimColor, Color.white, 0.62f), 0.018f,
                new Vector3(-0.20f, 0.88f, -0.20f),
                new Vector3(0f, 0.88f, -0.34f),
                new Vector3(0.20f, 0.88f, -0.20f));

            CreateSlotLine(slotRoot, "SummonChevronBack", Color.Lerp(trimColor, Color.white, 0.42f), 0.014f,
                new Vector3(-0.17f, 0.875f, 0.18f),
                new Vector3(0f, 0.875f, 0.30f),
                new Vector3(0.17f, 0.875f, 0.18f));

            Vector3[] corners =
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
            primitive.transform.SetParent(parent, false);
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
            lineObject.transform.SetParent(parent, false);
            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.sharedMaterial = CreateRuntimeLineMaterial();
            line.useWorldSpace = false;
            line.positionCount = Mathf.Max(2, points != null ? points.Length : 0);
            line.startWidth = width;
            line.endWidth = width;
            line.numCapVertices = 3;
            line.numCornerVertices = 3;
            line.startColor = color;
            line.endColor = color;
            for (int i = 0; i < line.positionCount; i++)
            {
                line.SetPosition(i, points != null && i < points.Length ? points[i] : Vector3.zero);
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
            float width = (laneCount - 1) * laneSpacing;

            for (int i = 0; i < laneCount; i++)
            {
                GameObject point = new GameObject("Spawn_" + i.ToString("D2"));
                point.transform.SetParent(spawnRoot);
                point.transform.position = spawnCenter + new Vector3(-width * 0.5f + i * laneSpacing, 0f, 0f);
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
                hiddenGoal.transform.position = new Vector3(0f, 0f, DefaultGoalLineZ);
                return hiddenGoal.transform;
            }

            if (presentationConfig != null && presentationConfig.goalPrefab != null)
            {
                GameObject overrideGoal = Instantiate(presentationConfig.goalPrefab, miscRoot);
                overrideGoal.name = "GoalPoint";
                overrideGoal.transform.localPosition = new Vector3(0f, 0f, DefaultGoalLineZ);
                overrideGoal.transform.localRotation = Quaternion.identity;
                return overrideGoal.transform;
            }

            GameObject goal = new GameObject("GoalPoint");
            goal.transform.SetParent(miscRoot);
            goal.transform.position = new Vector3(0f, 0f, DefaultGoalLineZ);

            GameObject gate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gate.name = "DefenseGate";
            gate.transform.SetParent(goal.transform);
            gate.transform.localPosition = new Vector3(0f, 1.3f, 0f);
            gate.transform.localScale = new Vector3(6.5f, 2.6f, 0.6f);
            gate.GetComponent<Renderer>().material.color = GetConfigColor(config => config.gateColor, new Color(0.24f, 0.54f, 0.72f));

            GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            core.name = "GateCore";
            core.transform.SetParent(goal.transform);
            core.transform.localPosition = new Vector3(0f, 1.7f, -0.1f);
            core.transform.localScale = Vector3.one * 1.1f;
            core.GetComponent<Renderer>().material.color = GetConfigColor(config => config.gateCoreColor, new Color(0.38f, 0.89f, 1f));
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
                GameObject overrideCrystal = Instantiate(presentationConfig.centerCrystalPrefab, miscRoot);
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
            crystal.GetComponent<Renderer>().material.color = GetConfigColor(config => config.crystalColor, new Color(0.30f, 0.95f, 0.86f));

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
                GameObject overrideTower = Instantiate(presentationConfig.flankTowerPrefab, parent);
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
                    GameObject overrideOrb = Instantiate(presentationConfig.skyAccentPrefab, miscRoot);
                    overrideOrb.name = "SkyOrb_" + i.ToString("D2");
                    overrideOrb.transform.localPosition = new Vector3(-8f + i * 4f, 4.8f + (i % 2) * 0.6f, 6.2f - i * 1.3f);
                    overrideOrb.transform.localRotation = Quaternion.identity;
                    continue;
                }

                GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                orb.name = "SkyOrb_" + i.ToString("D2");
                orb.transform.SetParent(miscRoot);
                orb.transform.position = new Vector3(-8f + i * 4f, 4.8f + (i % 2) * 0.6f, 6.2f - i * 1.3f);
                orb.transform.localScale = Vector3.one * (0.35f + i * 0.05f);
                orb.GetComponent<Renderer>().material.color = GetLaneColor(i) * 0.95f;
            }
        }

        private Projectile BuildProjectileTemplate(Transform templateRoot)
        {
            GameObject projectileObject = CreateTemplateObject(templateRoot, presentationConfig != null ? presentationConfig.projectilePrefab : null, PrimitiveType.Sphere, "ProjectileTemplate", Vector3.one * 0.25f);
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

            projectileObject.SetActive(false);
            return projectile;
        }

        private DefenderUnit BuildDefenderTemplate(Transform templateRoot, Projectile projectileTemplate)
        {
            GameObject unitObject = CreateTemplateObject(templateRoot, presentationConfig != null ? presentationConfig.defaultDefenderPrefab : null, PrimitiveType.Capsule, "DefenderTemplate", new Vector3(0.8f, 1f, 0.8f));
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

            unit.ConfigureRuntimePieces(
                projectileTemplate,
                firePoint,
                unitObject.GetComponentsInChildren<Renderer>(true),
                presentationConfig != null ? presentationConfig.summonedDefenderPrefab : null,
                presentationConfig != null ? presentationConfig.defaultMuzzleEffectPrefab : null,
                presentationConfig != null ? presentationConfig.defaultHitEffectPrefab : null,
                presentationConfig != null ? presentationConfig.defaultAreaEffectPrefab : null);
            unitObject.SetActive(false);
            return unit;
        }
        private MonsterUnit BuildMonsterTemplate(Transform templateRoot)
        {
            GameObject monsterObject = CreateTemplateObject(templateRoot, presentationConfig != null ? presentationConfig.defaultMonsterPrefab : null, PrimitiveType.Cube, "MonsterTemplate", Vector3.one);
            MonsterUnit monster = monsterObject.GetComponent<MonsterUnit>();
            if (monster == null)
            {
                monster = monsterObject.AddComponent<MonsterUnit>();
            }

            monster.ConfigureRuntimePieces(
                presentationConfig != null ? presentationConfig.monsterDeathEffectPrefab : null,
                monsterObject.GetComponentsInChildren<Renderer>(true));
            monsterObject.SetActive(false);
            return monster;
        }

        private GameObject CreateTemplateObject(Transform parent, GameObject prefab, PrimitiveType fallbackPrimitive, string name, Vector3 scale)
        {
            GameObject instance;
            if (prefab != null)
            {
                instance = Instantiate(prefab, parent);
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
            Transform existingCanvasTransform = root.Find("RuntimeCanvas");
            if (existingCanvasTransform != null)
            {
                SafeDestroy(existingCanvasTransform.gameObject);
            }

            EnsureEventSystem();

            GameObject canvasObject = new GameObject("RuntimeCanvas", typeof(RectTransform));
            // Building hundreds of active UI objects causes repeated mobile Canvas rebuilds.
            // Assemble the complete hierarchy off-screen, then enable it once it is configured.
            canvasObject.SetActive(false);
            canvasObject.transform.SetParent(root, false);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.84f;
            canvasObject.AddComponent<GraphicRaycaster>();
            canvasObject.AddComponent<RuntimeKoreanTextCleaner>();
            Transform hudRoot = CreateSafeAreaRoot(canvas.transform);
            Transform metaFlowRoot = CreateSafeAreaRoot(canvas.transform, "MetaFlowSafeAreaRoot");

            Font font = RuntimeUiSkinUtility.ResolveFont(presentationConfig);
            Color textColor = presentationConfig != null ? presentationConfig.hudTextColor : Color.white;
            bool showKeyboardHint = !Application.isMobilePlatform;
            string hintValue = showKeyboardHint
                ? (presentationConfig != null && !string.IsNullOrWhiteSpace(presentationConfig.hintText)
                    ? presentationConfig.hintText : "Space Round | S Summon | 1-5 Merge")
                : string.Empty;

            CreatePanel(hudRoot, "TopSafeBackdrop", new Vector2(0f, -12f), new Vector2(0f, 232f), new Color(0.03f, 0.05f, 0.17f, 0.74f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), false, true);
            CreatePanel(hudRoot, "TopGlow", new Vector2(0f, -224f), new Vector2(0f, 8f), new Color(0.17f, 0.42f, 0.72f, 0.35f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), false, false);

            Image playerPanel = CreatePanel(hudRoot, "PlayerBadge", new Vector2(28f, -28f), new Vector2(276f, 88f), new Color(0.93f, 0.74f, 0.27f, 0.96f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), true, true);
            CreatePanel(playerPanel.transform, "PlayerIcon", new Vector2(24f, -16f), new Vector2(62f, 62f), new Color(0.66f, 0.46f, 0.14f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), true, false);
            Text playerName = CreateText(playerPanel.transform, font, Color.white, "PlayerName", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(104f, -14f), new Vector2(148f, 32f), "레드X", 24, TextAnchor.MiddleLeft, true);
            Text rank = CreateText(playerPanel.transform, font, new Color(0.16f, 0.22f, 0.35f), "RankText", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(104f, -50f), new Vector2(150f, 26f), "RANK 1", 18, TextAnchor.MiddleLeft, true);
            Text life = null;

            Text gold = CreateCurrencyPill(hudRoot, font, "GoldPill", "G", new Vector2(-90f, -36f), new Vector2(190f, 68f), new Color(1f, 0.76f, 0.22f), "0");
            Image lifeProgressBack = CreatePanel(hudRoot, "LifeProgressBar", new Vector2(188f, -36f), new Vector2(330f, 68f), new Color(0.08f, 0.12f, 0.28f, 0.96f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), true, true);
            CreatePanel(lifeProgressBack.transform, "LifeProgressGlow", Vector2.zero, new Vector2(-18f, -18f), new Color(0.20f, 1f, 0.48f, 0.13f), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), true, false);
            Image lifeProgressTrack = CreatePanel(lifeProgressBack.transform, "LifeProgressTrack", Vector2.zero, new Vector2(-14f, -14f), new Color(0.035f, 0.07f, 0.15f, 1f), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), true, false);
            Mask lifeProgressMask = lifeProgressTrack.gameObject.AddComponent<Mask>();
            lifeProgressMask.showMaskGraphic = true;
            Image lifeProgressFill = CreatePanel(lifeProgressTrack.transform, "Fill", Vector2.zero, Vector2.zero, new Color(0.20f, 0.90f, 0.36f, 1f), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), true, false);
            lifeProgressFill.type = Image.Type.Sliced;
            lifeProgressFill.fillAmount = 1f;
            Text content = CreateText(lifeProgressBack.transform, font, Color.white, "TopHpText", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, "HP 10/10", 28, TextAnchor.MiddleCenter, true);
            AddStrongTextOutline(content);
            Image optionsMenu = CreatePanel(hudRoot, "OptionsMenu", new Vector2(-34f, -112f), new Vector2(274f, 322f), new Color(0.06f, 0.08f, 0.24f, 0.98f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), true, true);
            Canvas optionsCanvas = optionsMenu.gameObject.AddComponent<Canvas>();
            optionsCanvas.overrideSorting = true;
            optionsCanvas.sortingOrder = 200;
            optionsMenu.gameObject.AddComponent<GraphicRaycaster>();
            Text state = null;
            Button optionsButton = CreateButton(hudRoot, font, "OptionsButton", string.Empty, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-38f, -36f), new Vector2(76f, 64f), new Color(0.08f, 0.14f, 0.31f, 0.96f), Color.white, null, out Text optionsLabel);
            optionsLabel.fontSize = 19;
            optionsLabel.resizeTextForBestFit = true;
            optionsLabel.resizeTextMinSize = 12;
            optionsLabel.resizeTextMaxSize = 19;
            optionsLabel.enabled = false;
            BuildHamburgerIcon(optionsButton.transform);
            optionsButton.onClick.AddListener(() => optionsMenu.gameObject.SetActive(!optionsMenu.gameObject.activeSelf));
            optionsMenu.gameObject.SetActive(false);

            Image mergeStrip = CreatePanel(hudRoot, "MergeResultStrip", new Vector2(-80f, -116f), new Vector2(865f, 30f), new Color(0.10f, 0.12f, 0.30f, 0.72f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), true, true);
            Text mergeResult = CreateText(mergeStrip.transform, font, new Color(1f, 0.89f, 0.36f), "MergeResultText", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, string.Empty, 17, TextAnchor.MiddleCenter, true);
            mergeStrip.gameObject.SetActive(false);

            Image bottomPanel = CreatePanel(hudRoot, "BottomCommandDock", new Vector2(0f, 0f), new Vector2(0f, 340f), new Color(0.05f, 0.06f, 0.18f, 0.86f), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), false, true);
            CreatePanel(bottomPanel.transform, "DockTopLine", new Vector2(0f, 334f), new Vector2(0f, 8f), new Color(0.37f, 0.85f, 1f, 0.42f), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), false, false);

            Text deckSummary = CreateText(hudRoot, font, textColor, "DeckSummaryText", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(42f, 300f), new Vector2(250f, 30f), "보유 유닛 0 / 0", 21, TextAnchor.MiddleLeft, true);
            Text capacity = CreateText(hudRoot, font, new Color(0.75f, 0.91f, 1f), "CapacityText", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-42f, 300f), new Vector2(204f, 30f), "0칸 남음", 19, TextAnchor.MiddleRight, true);

            Text round = CreateText(hudRoot, font, textColor, "RoundText", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-214f, 294f), new Vector2(176f, 30f), "ROUND 1", 19, TextAnchor.MiddleCenter, true);
            Image roundProgressFill = CreateProgressBar(hudRoot, new Vector2(130f, 294f), new Vector2(438f, 28f));
            Text bossRoundHud = CreateText(roundProgressFill.transform.parent, font, new Color(0.76f, 0.94f, 1f), "BossRoundText", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, "다음 보스 ROUND 10", 16, TextAnchor.MiddleCenter, true);
            bossRoundHud.resizeTextForBestFit = true;
            bossRoundHud.fontSize = 18;
            bossRoundHud.resizeTextMinSize = 14;
            bossRoundHud.resizeTextMaxSize = 18;
            AddStrongTextOutline(bossRoundHud);
            Text board = CreateText(hudRoot, font, Color.white, "BoardText", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(396f, 294f), new Vector2(126f, 28f), "0 / 0", 17, TextAnchor.MiddleCenter, true);

            UltimateRecipeSelectionUI ultimateRecipeSelection = null;
            Text normalCount = CreateGradeCard(hudRoot, font, CharacterGrade.Normal, new Vector2(-380f, 126f), binder.OnClickMergeNormal, "재료 3개");
            Text rareCount = CreateGradeCard(hudRoot, font, CharacterGrade.Rare, new Vector2(-228f, 126f), binder.OnClickMergeRare, "재료 3개");
            Text epicCount = CreateGradeCard(hudRoot, font, CharacterGrade.Epic, new Vector2(-76f, 126f), binder.OnClickMergeEpic, "재료 3개");
            Text legendaryCount = CreateGradeCard(hudRoot, font, CharacterGrade.Legendary, new Vector2(76f, 126f), binder.OnClickMergeLegendary, "재료 3개");
            Text mythicCount = CreateGradeCard(hudRoot, font, CharacterGrade.Mythic, new Vector2(228f, 126f), null, "초월 재료");
            Text transcendentCount = CreateGradeCard(
                hudRoot, font, CharacterGrade.Transcendent, new Vector2(380f, 126f),
                () => { if (ultimateRecipeSelection != null) ultimateRecipeSelection.Open(); },
                "레시피 선택");

            Button summonButton = CreateButton(hudRoot, font, "SummonButton", "소환", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(54f, 29f), new Vector2(226f, 76f), new Color(0.19f, 0.78f, 0.42f, 1f), Color.white, binder.OnClickSummon, out Text summonLabel);
            Image ultimateRecipePanel = CreatePanel(hudRoot, "UltimateRecipeHudPanel", new Vector2(0f, 700f), new Vector2(920f, 76f), new Color(0.05f, 0.10f, 0.28f, 0.78f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), true, true);
            Text ultimateRecipeHud = CreateText(ultimateRecipePanel.transform, font, new Color(0.76f, 0.94f, 1f), "UltimateRecipeHudText", new Vector2(0f, 0f), Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(18f, 0f), new Vector2(-36f, -18f), "레시피 빙고\nTOP 0/3", 20, TextAnchor.MiddleLeft, true);
            ultimateRecipeHud.resizeTextForBestFit = true;
            ultimateRecipeHud.fontSize = 20;
            ultimateRecipeHud.resizeTextMinSize = 16;
            ultimateRecipeHud.resizeTextMaxSize = 20;
            AddStrongTextOutline(ultimateRecipeHud);

            Image buildReadoutPanel = CreatePanel(hudRoot, "BuildReadoutPanel", new Vector2(0f, 592f), new Vector2(920f, 96f), new Color(0.04f, 0.08f, 0.24f, 0.78f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), true, true);
            Text synergyInsight = CreateBuildInsightCell(buildReadoutPanel.transform, font, "DangerInsight", "위험", new Vector2(-292f, 0f), new Color(1f, 0.48f, 0.30f));
            Text recipeInsight = CreateBuildInsightCell(buildReadoutPanel.transform, font, "ActionInsight", "추천 행동", Vector2.zero, new Color(0.36f, 0.92f, 1f));
            Text tileInsight = CreateBuildInsightCell(buildReadoutPanel.transform, font, "DealerInsight", "핵심 딜러", new Vector2(292f, 0f), new Color(1f, 0.76f, 0.26f));

            Image fatePanel = CreatePanel(hudRoot, "FateInterventionPanel", new Vector2(0f, 434f), new Vector2(1000f, 560f), new Color(0.06f, 0.05f, 0.18f, 0.98f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), true, true);
            CanvasGroup fatePanelCanvasGroup = fatePanel.gameObject.AddComponent<CanvasGroup>();
            CreatePanel(fatePanel.transform, "FateAccent", new Vector2(-488f, 0f), new Vector2(12f, 516f), new Color(1f, 0.30f, 0.88f, 0.96f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), true, false);
            CreateText(fatePanel.transform, font, new Color(1f, 0.74f, 0.96f), "FatePanelTitle", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 228f), new Vector2(620f, 42f), "마지막 계약 · 위기 탈출 카드", 30, TextAnchor.MiddleCenter, true);
            Image fateGaugeFill = CreateProgressBar(fatePanel.transform, new Vector2(-350f, 148f), new Vector2(220f, 16f));
            fateGaugeFill.color = new Color(1f, 0.36f, 0.92f, 0.96f);
            Text fateGaugeText = CreateText(fatePanel.transform, font, Color.white, "FateGaugeText", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-350f, 120f), new Vector2(264f, 28f), "마지막 계약 1/1", 18, TextAnchor.MiddleCenter, true);
            fateGaugeText.resizeTextForBestFit = true;
            fateGaugeText.resizeTextMinSize = 16;
            fateGaugeText.resizeTextMaxSize = 20;
            Text fateDebtText = CreateText(fatePanel.transform, font, new Color(1f, 0.82f, 0.42f), "FateDebtText", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-350f, 90f), new Vector2(248f, 24f), "카드 보유 1/1", 16, TextAnchor.MiddleCenter, true);
            Text fateCostBenefit = CreateText(fatePanel.transform, font, new Color(0.84f, 0.94f, 1f), "FateCostBenefitText", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(90f, 174f), new Vector2(620f, 34f), "전투는 0.1배로 흐릅니다 · 3장 중 1장 선택", 19, TextAnchor.MiddleCenter, true);
            fateCostBenefit.resizeTextForBestFit = true;
            fateCostBenefit.resizeTextMinSize = 16;
            fateCostBenefit.resizeTextMaxSize = 19;
            Text earlyRunInsight = null;

            Button fateSurvivalButton = CreateButton(fatePanel.transform, font, "FateChoiceCard0", "운명 카드\n선택 1", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-320f, -52f), new Vector2(300f, 250f), new Color(0.70f, 0.24f, 1f, 0.98f), Color.white, () => gameController.TryActivateFateSurvival(), out Text fateSurvivalLabel);
            Button fateGradeLockButton = CreateButton(fatePanel.transform, font, "FateChoiceCard1", "운명 카드\n선택 2", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -52f), new Vector2(300f, 250f), new Color(0.18f, 0.68f, 1f, 0.96f), Color.white, () => gameController.TryActivateFateGradeLock(CharacterGrade.Rare, 3), out Text fateGradeLockLabel);
            Button fateNormalBanButton = CreateButton(fatePanel.transform, font, "FateChoiceCard2", "운명 카드\n선택 3", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(320f, -52f), new Vector2(300f, 250f), new Color(1f, 0.34f, 0.24f, 0.96f), Color.white, () => gameController.TryActivateFateNormalBan(4), out Text fateNormalBanLabel);
            Button fateForceShopButton = CreateButton(fatePanel.transform, font, "FateUnusedHiddenCard", string.Empty, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(9999f, 0f), new Vector2(1f, 1f), new Color(0f, 0f, 0f, 0f), Color.white, null, out Text fateForceShopLabel);
            fateForceShopButton.gameObject.SetActive(false);
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
            Button fatePanelReopenButton = CreateButton(hudRoot, font, "FatePanelReopenButton", "계약", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-42f, 356f), new Vector2(154f, 48f), new Color(0.45f, 0.14f, 0.62f, 0.94f), Color.white, null, out Text fatePanelReopenLabel);
            fatePanelReopenLabel.fontSize = 18;
            fatePanelReopenLabel.resizeTextForBestFit = true;
            fatePanelReopenLabel.resizeTextMinSize = 13;
            fatePanelReopenLabel.resizeTextMaxSize = 18;
            RectTransform fateEntryRect = fatePanelReopenButton.GetComponent<RectTransform>();
            fateEntryRect.sizeDelta = new Vector2(250f, 84f);
            fateEntryRect.anchoredPosition = new Vector2(-54f, 356f);
            fatePanelReopenLabel.fontSize = 28;
            fatePanelReopenLabel.resizeTextMinSize = 22;
            fatePanelReopenLabel.resizeTextMaxSize = 28;
            Outline fateEntryOutline = fatePanelReopenButton.gameObject.AddComponent<Outline>();
            fateEntryOutline.effectColor = new Color(1f, 0.72f, 0.20f, 0.94f);
            fateEntryOutline.effectDistance = new Vector2(3f, -3f);
            fateEntryOutline.useGraphicAlpha = false;
            fatePanelReopenButton.gameObject.SetActive(false);

            Text summonCost = CreateText(summonButton.transform, font, new Color(0.13f, 0.28f, 0.12f), "SummonCostText", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 7f), new Vector2(0f, 24f), "10", 18, TextAnchor.MiddleCenter, true);
            summonLabel.fontSize = 36;
            summonLabel.alignment = TextAnchor.MiddleCenter;
            summonLabel.rectTransform.anchorMin = new Vector2(0f, 0.28f);
            summonLabel.rectTransform.anchorMax = Vector2.one;
            summonLabel.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            summonLabel.rectTransform.anchoredPosition = Vector2.zero;
            summonLabel.rectTransform.sizeDelta = Vector2.zero;
            Button battleButton = CreateButton(hudRoot, font, "BattleButton", "전투 시작", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-4f, 26f), new Vector2(340f, 88f), new Color(0.94f, 0.32f, 0.24f, 1f), Color.white, null, out Text battleLabel);
            battleLabel.fontSize = 36;

            CreateText(optionsMenu.transform, font, new Color(0.72f, 0.92f, 1f), "OptionsHeader", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(220f, 34f), "\uC124\uC815", 25, TextAnchor.MiddleCenter, true);
            Button soundToggleButton = CreateButton(optionsMenu.transform, font, "SoundToggleButton", "\uC0AC\uC6B4\uB4DC ON", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -82f), new Vector2(222f, 50f), new Color(0.20f, 0.70f, 0.86f, 0.95f), Color.white, null, out Text soundToggleLabel);
            Button volumeButton = CreateButton(optionsMenu.transform, font, "VolumeButton", "\uC74C\uB7C9 100%", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -142f), new Vector2(222f, 50f), new Color(0.18f, 0.48f, 0.90f, 0.95f), Color.white, null, out Text volumeLabel);
            Button languageButton = CreateButton(optionsMenu.transform, font, "LanguageButton", "\uC5B8\uC5B4 \uD55C\uAD6D\uC5B4", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -202f), new Vector2(222f, 50f), new Color(0.44f, 0.36f, 0.86f, 0.95f), Color.white, null, out Text languageLabel);
            Button lobbyButton = CreateButton(optionsMenu.transform, font, "LobbyButton", "\uB098\uAC00\uAE30", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -266f), new Vector2(222f, 54f), new Color(0.86f, 0.34f, 0.24f, 0.95f), Color.white, null, out _);
            Button loadoutButton = null;
            Button infoButton = null;
            float[] optionVolumeSteps = { 1f, 0.7f, 0.4f, 0f };
            int optionVolumeIndex = 0;
            System.Action refreshOptionLabels = () =>
            {
                float currentVolume = Mathf.Clamp01(AudioListener.volume);
                soundToggleLabel.text = RuntimeKoreanTextUtility.Clean("SoundToggleButton", currentVolume <= 0.001f ? "\uC0AC\uC6B4\uB4DC \uCF1C\uAE30" : "\uC0AC\uC6B4\uB4DC \uB044\uAE30");
                volumeLabel.text = RuntimeKoreanTextUtility.Clean("VolumeButton", "\uC74C\uB7C9 " + Mathf.RoundToInt(currentVolume * 100f) + "%");
                languageLabel.text = RuntimeKoreanTextUtility.Clean("LanguageButton", "\uC5B8\uC5B4 \uD55C\uAD6D\uC5B4");
            };
            refreshOptionLabels();
            soundToggleButton.onClick.AddListener(() =>
            {
                AudioListener.volume = AudioListener.volume <= 0.001f ? optionVolumeSteps[Mathf.Clamp(optionVolumeIndex, 0, optionVolumeSteps.Length - 2)] : 0f;
                refreshOptionLabels();
            });
            volumeButton.onClick.AddListener(() =>
            {
                optionVolumeIndex = (optionVolumeIndex + 1) % optionVolumeSteps.Length;
                AudioListener.volume = optionVolumeSteps[optionVolumeIndex];
                refreshOptionLabels();
            });
            languageButton.onClick.AddListener(() => refreshOptionLabels());
            Image unitSellPanel = CreatePanel(hudRoot, "SelectedUnitSellPanel", new Vector2(0f, 450f), new Vector2(820f, 84f), new Color(0.10f, 0.08f, 0.20f, 0.94f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), true, true);
            CreatePanel(unitSellPanel.transform, "SellAccent", new Vector2(18f, 0f), new Vector2(10f, 58f), new Color(1f, 0.58f, 0.24f, 0.95f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), true, false);
            Text unitSellTitle = CreateText(unitSellPanel.transform, font, Color.white, "SellTitle", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(44f, -14f), new Vector2(-254f, 30f), "선택 유닛", 22, TextAnchor.MiddleLeft, true);
            Text unitSellDetail = CreateText(unitSellPanel.transform, font, new Color(0.82f, 0.92f, 1f), "SellDetail", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(44f, 16f), new Vector2(-254f, 36f), "판매가 확인 중", 17, TextAnchor.MiddleLeft, false);
            unitSellDetail.resizeTextForBestFit = true;
            unitSellDetail.resizeTextMinSize = 13;
            unitSellDetail.resizeTextMaxSize = 17;
            Button unitSellButton = CreateButton(unitSellPanel.transform, font, "SellSelectedUnitButton", "판매", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-24f, 0f), new Vector2(206f, 62f), new Color(0.92f, 0.38f, 0.22f, 0.98f), Color.white, null, out Text unitSellButtonLabel);
            unitSellButtonLabel.fontSize = 23;
            unitSellPanel.gameObject.SetActive(false);

            Text hint = CreateText(hudRoot, font, new Color(0.84f, 0.92f, 1f, 0.86f), "HintText", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 780f), new Vector2(860f, 32f), hintValue, 17, TextAnchor.MiddleCenter, false);
            hint.gameObject.SetActive(showKeyboardHint);

            Text countdown = CreateText(hudRoot, font, new Color(1f, 0.95f, 0.58f, 0f), "CountdownText", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 35f), new Vector2(220f, 120f), string.Empty, 96, TextAnchor.MiddleCenter, true);
            Text roundBanner = CreateText(hudRoot, font, new Color(0.48f, 1f, 0.72f, 0f), "RoundBannerText", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 136f), new Vector2(620f, 70f), string.Empty, 40, TextAnchor.MiddleCenter, true);

            Text mergeCelebration = CreateText(hudRoot, font, new Color(1f, 0.92f, 0.5f, 0f), "MergeCelebrationText", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 210f), new Vector2(720f, 76f), string.Empty, 52, TextAnchor.MiddleCenter, true);
            Text mergeCelebrationSub = CreateText(hudRoot, font, new Color(1f, 0.98f, 0.9f, 0f), "MergeCelebrationSubText", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 154f), new Vector2(820f, 42f), string.Empty, 25, TextAnchor.MiddleCenter, true);

            BuildSynergyPanelExpanded(hudRoot, font, synergySystem, gameController, boardManager);
            BuildTacticalMissionPanel(hudRoot, hudRoot, font, missionSystem, gameController, boardManager);
            BuildRunShopPanel(hudRoot, font, runShopSystem, gameController, boardManager, tileModifierSystem, augmentManager);

            BuildAugmentPanel(hudRoot, font, augmentManager, gameController);
            if (collectionUI != null)
            {
                collectionUI.Configure(characterDatabase, outgameProgression, font, metaFlowRoot, presentationConfig != null ? presentationConfig.uiSkin : null);
            }

            if (metaFlowUI != null)
            {
                metaFlowUI.Configure(gameController, binder, augmentManager, characterDatabase, outgameProgression, collectionUI, font, metaFlowRoot, hudRoot.gameObject, battleButton, lobbyButton, loadoutButton, presentationConfig != null ? presentationConfig.uiSkin : null);
                if (infoButton != null)
                {
                    infoButton.onClick.RemoveAllListeners();
                    infoButton.onClick.AddListener(metaFlowUI.ToggleCollectionPanel);
                }
            }
            else if (collectionUI != null && infoButton != null)
            {
                infoButton.onClick.AddListener(collectionUI.Toggle);
            }

            GameObject bossWarningPanel = BuildBossWarningPanel(hudRoot, font, out CanvasGroup bossWarningGroup, out Text bossWarningTitle, out Text bossWarningSub);
            ultimateRecipeSelection = BuildUltimateRecipeSelectionPanel(hudRoot, font, gameController);

            hud.Configure(
                gameController,
                gold,
                life,
                round,
                board,
                content,
                hint,
                mergeResult,
                mergeCelebration,
                mergeCelebrationSub,
                countdown,
                roundBanner,
                hintValue,
                playerName,
                rank,
                state,
                battleLabel,
                summonLabel,
                summonCost,
                deckSummary,
                capacity,
                normalCount,
                rareCount,
                epicCount,
                legendaryCount,
                mythicCount,
                transcendentCount,
                ultimateRecipeHud,
                bossRoundHud,
                synergyInsight,
                recipeInsight,
                tileInsight,
                null,
                earlyRunInsight,
                fateGaugeText,
                fateGaugeFill,
                fateDebtText,
                fateCostBenefit,
                fateGradeLockButton,
                fateGradeLockLabel,
                fateNormalBanButton,
                fateNormalBanLabel,
                fateForceShopButton,
                fateForceShopLabel,
                fateSurvivalButton,
                fateSurvivalLabel,
                fatePanel.gameObject,
                fatePanelCanvasGroup,
                fatePanelReopenButton,
                fatePanelReopenLabel,
                roundProgressFill,
                battleButton,
                summonButton,
                bossWarningPanel,
                bossWarningGroup,
                bossWarningTitle,
                bossWarningSub,
                boardManager,
                unitSellPanel.gameObject,
                unitSellTitle,
                unitSellDetail,
                unitSellButton,
                unitSellButtonLabel,
                lifeProgressFill);
            canvasObject.SetActive(true);
        }

        private UltimateRecipeSelectionUI BuildUltimateRecipeSelectionPanel(Transform parent, Font font, DefenseGameController gameController)
        {
            GameObject root = new GameObject("UltimateRecipeSelectionOverlay", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            Image blocker = root.AddComponent<Image>();
            blocker.color = new Color(0.02f, 0.02f, 0.10f, 0.76f);
            blocker.raycastTarget = true;
            Button blockerButton = root.AddComponent<Button>();
            blockerButton.transition = Selectable.Transition.None;
            CanvasGroup group = root.AddComponent<CanvasGroup>();

            Image drawer = CreatePanel(root.transform, "UltimateRecipeDrawer", new Vector2(0f, 110f), new Vector2(980f, 820f), new Color(0.06f, 0.06f, 0.22f, 0.99f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), true, true);
            Button drawerInputBlocker = drawer.gameObject.AddComponent<Button>();
            drawerInputBlocker.transition = Selectable.Transition.None;
            CreatePanel(drawer.transform, "DrawerTopGlow", new Vector2(0f, -20f), new Vector2(900f, 94f), new Color(0.72f, 0.22f, 1f, 0.28f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), true, false);
            CreatePanel(drawer.transform, "DrawerGoldLine", new Vector2(0f, -6f), new Vector2(860f, 8f), new Color(1f, 0.82f, 0.22f, 0.94f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), true, false);
            Text header = CreateText(drawer.transform, font, Color.white, "UltimateRecipeSelectionHeader", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -42f), new Vector2(720f, 46f), "초월 조합 선택", 34, TextAnchor.MiddleCenter, true);
            Text instruction = CreateText(drawer.transform, font, new Color(0.90f, 0.90f, 1f), "UltimateRecipeSelectionInstruction", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -94f), new Vector2(760f, 30f), "전체 레시피와 부족한 재료를 언제든 확인할 수 있습니다.", 20, TextAnchor.MiddleCenter, false);
            Button closeButton = CreateButton(drawer.transform, font, "UltimateRecipeSelectionClose", "X", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-28f, -40f), new Vector2(58f, 58f), new Color(0.90f, 0.26f, 0.34f, 0.98f), Color.white, null, out _);

            const int optionCapacity = 11;
            Button[] optionButtons = new Button[optionCapacity];
            Text[] optionLabels = new Text[optionCapacity];
            for (int i = 0; i < optionCapacity; i++)
            {
                int column = i % 2;
                int row = i / 2;
                float x = column == 0 ? -226f : 226f;
                float y = -146f - row * 91f;
                optionButtons[i] = CreateButton(drawer.transform, font, "UltimateRecipeOption_" + i, string.Empty, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(x, y), new Vector2(430f, 80f), new Color(0.12f, 0.12f, 0.34f, 0.98f), Color.white, null, out Text optionLabel);
                Outline readyOutline = optionButtons[i].gameObject.AddComponent<Outline>();
                readyOutline.effectColor = Color.clear;
                readyOutline.effectDistance = new Vector2(3f, -3f);
                readyOutline.useGraphicAlpha = false;
                optionLabel.alignment = TextAnchor.MiddleLeft;
                optionLabel.resizeTextForBestFit = true;
                optionLabel.resizeTextMinSize = 12;
                optionLabel.resizeTextMaxSize = 18;
                optionLabel.rectTransform.offsetMin = new Vector2(16f, 6f);
                optionLabel.rectTransform.offsetMax = new Vector2(-12f, -6f);
                optionLabels[i] = optionLabel;
            }

            Button confirmButton = CreateButton(drawer.transform, font, "UltimateRecipeConfirmButton", "레시피를 선택하세요", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(500f, 68f), new Color(0.72f, 0.24f, 0.94f, 0.98f), Color.white, null, out Text confirmLabel);
            confirmLabel.fontSize = 25;

            UltimateRecipeSelectionUI selection = root.AddComponent<UltimateRecipeSelectionUI>();
            selection.Configure(gameController, drawer.rectTransform, group, blockerButton, header, instruction, optionButtons, optionLabels, closeButton, confirmButton, confirmLabel);
            root.SetActive(false);
            return selection;
        }

        private GameObject BuildBossWarningPanel(Transform parent, Font font, out CanvasGroup canvasGroup, out Text title, out Text subtitle)
        {
            Image panel = CreatePanel(parent, "BossWarningPanel", new Vector2(0f, 92f), new Vector2(790f, 230f), new Color(0.20f, 0.02f, 0.08f, 0.94f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), true, true);
            canvasGroup = panel.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            CreatePanel(panel.transform, "BossWarningGlow", Vector2.zero, new Vector2(-34f, -28f), new Color(1f, 0.12f, 0.18f, 0.18f), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), true, false);
            CreatePanel(panel.transform, "BossWarningTopLine", new Vector2(0f, -8f), new Vector2(700f, 12f), new Color(1f, 0.24f, 0.18f, 0.98f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), true, false);
            CreatePanel(panel.transform, "BossWarningBottomLine", new Vector2(0f, 8f), new Vector2(700f, 10f), new Color(1f, 0.72f, 0.24f, 0.86f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), true, false);
            CreatePanel(panel.transform, "LeftDangerIcon", new Vector2(52f, 0f), new Vector2(86f, 86f), new Color(1f, 0.18f, 0.16f, 0.95f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), true, false);
            CreatePanel(panel.transform, "RightDangerIcon", new Vector2(-52f, 0f), new Vector2(86f, 86f), new Color(1f, 0.18f, 0.16f, 0.95f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), true, false);
            CreateText(panel.transform, font, new Color(1f, 0.78f, 0.28f), "BossWarningKicker", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -38f), new Vector2(420f, 32f), "WARNING", 27, TextAnchor.MiddleCenter, true);
            title = CreateText(panel.transform, font, Color.white, "BossWarningTitle", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 16f), new Vector2(560f, 72f), "보스 등장!", 58, TextAnchor.MiddleCenter, true);
            subtitle = CreateText(panel.transform, font, new Color(1f, 0.88f, 0.74f), "BossWarningSub", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 42f), new Vector2(620f, 36f), "강력한 보스가 내려옵니다", 25, TextAnchor.MiddleCenter, true);

            panel.gameObject.SetActive(false);
            return panel.gameObject;
        }

        private void BuildAugmentPanel(Transform parent, Font font, AugmentManager augmentManager, DefenseGameController gameController)
        {
            if (augmentManager == null)
            {
                return;
            }

            GameObject root = new GameObject("AugmentChoiceOverlay", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            Image blocker = root.AddComponent<Image>();
            blocker.color = new Color(0.03f, 0.04f, 0.15f, 0.78f);
            blocker.raycastTarget = true;
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            Image modal = CreatePanel(root.transform, "AugmentModal", new Vector2(0f, 80f), new Vector2(940f, 900f), new Color(0.10f, 0.11f, 0.30f, 0.98f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), true, true);
            CreatePanel(modal.transform, "HeaderGlow", new Vector2(0f, -16f), new Vector2(830f, 104f), new Color(0.45f, 0.26f, 0.84f, 0.92f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), true, false);
            Text header = CreateText(modal.transform, font, Color.white, "AugmentHeader", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(720f, 48f), "증강체 선택", 34, TextAnchor.MiddleCenter, true);
            header.fontSize = 44;
            CreateText(modal.transform, font, new Color(0.83f, 0.88f, 1f), "AugmentSubtitle", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -92f), new Vector2(740f, 32f), "전투 흐름을 바꿀 보너스 하나를 고르세요.", 20, TextAnchor.MiddleCenter, false);
            Button closeButton = CreateButton(modal.transform, font, "AugmentCloseButton", "X", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-32f, -40f), new Vector2(66f, 66f), new Color(0.94f, 0.36f, 0.30f, 0.98f), Color.white, null, out _);
            Text augmentSubtitle = modal.transform.Find("AugmentSubtitle").GetComponent<Text>();
            augmentSubtitle.fontSize = 26;
            augmentSubtitle.rectTransform.sizeDelta = new Vector2(780f, 42f);
            Button reopenButton = CreateButton(parent, font, "AugmentReopenButton", "증강체", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-136f, -206f), new Vector2(180f, 62f), new Color(0.50f, 0.28f, 0.96f, 0.96f), Color.white, null, out Text reopenLabel);
            reopenLabel.fontSize = 28;
            reopenButton.GetComponent<RectTransform>().sizeDelta = new Vector2(210f, 72f);
            reopenButton.gameObject.SetActive(false);

            Button[] buttons = new Button[3];
            Image[] accents = new Image[3];
            Text[] styles = new Text[3];
            Text[] titles = new Text[3];
            Text[] descriptions = new Text[3];
            for (int i = 0; i < 3; i++)
            {
                float y = -154f - i * 212f;
                Button choiceButton = CreateButton(modal.transform, font, "AugmentChoice_" + i, string.Empty, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(840f, 188f), new Color(0.16f, 0.18f, 0.43f, 0.96f), Color.white, null, out _);
                accents[i] = CreatePanel(choiceButton.transform, "IconPlate", new Vector2(26f, -38f), new Vector2(96f, 96f), new Color(0.82f, 0.48f, 1f, 0.92f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), true, false);
                styles[i] = CreateText(choiceButton.transform, font, Color.white, "Style", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(26f, -44f), new Vector2(74f, 30f), "확정", 21, TextAnchor.MiddleCenter, true);
                titles[i] = CreateText(choiceButton.transform, font, Color.white, "Title", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(140f, -18f), new Vector2(-220f, 46f), "Augment", 35, TextAnchor.MiddleLeft, true);
                descriptions[i] = CreateText(choiceButton.transform, font, new Color(0.91f, 0.93f, 1f), "Description", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(140f, -70f), new Vector2(-214f, 98f), "Description", 28, TextAnchor.UpperLeft, false);
                styles[i].fontSize = 24;
                styles[i].rectTransform.sizeDelta = new Vector2(84f, 36f);
                styles[i].rectTransform.anchoredPosition = new Vector2(26f, -50f);
                CreateText(choiceButton.transform, font, new Color(0.66f, 1f, 0.78f), "PickLabel", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-28f, 0f), new Vector2(90f, 34f), "선택", 20, TextAnchor.MiddleRight, true);
                Text pickLabel = choiceButton.transform.Find("PickLabel").GetComponent<Text>();
                pickLabel.fontSize = 24;
                buttons[i] = choiceButton;
            }

            augmentManager.Configure(gameController, root, header, titles, descriptions, buttons, styles, accents, closeButton, reopenButton);
        }

        private void BuildRunShopPanel(Transform parent, Font font, RunShopSystem runShopSystem, DefenseGameController gameController, DefenseBoardManager boardManager, BoardTileModifierSystem tileModifierSystem, AugmentManager augmentManager)
        {
            if (runShopSystem == null)
            {
                return;
            }

            GameObject root = new GameObject("RunShopOverlay", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            Image dim = root.AddComponent<Image>();
            dim.color = new Color(0.02f, 0.04f, 0.16f, 0.76f);

            Image modal = CreatePanel(root.transform, "RunShopModal", new Vector2(0f, 70f), new Vector2(940f, 900f), new Color(0.08f, 0.14f, 0.36f, 0.98f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), true, true);
            CreatePanel(modal.transform, "RunShopTopGlow", new Vector2(0f, -38f), new Vector2(780f, 82f), new Color(0.32f, 0.86f, 1f, 0.20f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), true, false);
            Text header = CreateText(modal.transform, font, Color.white, "RunShopHeader", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -42f), new Vector2(460f, 46f), "전투 상점", 36, TextAnchor.MiddleCenter, true);
            header.fontSize = 44;
            Text subtitle = CreateText(modal.transform, font, new Color(0.84f, 0.92f, 1f), "RunShopSubtitle", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -94f), new Vector2(720f, 32f), "이번 판 전용 상품입니다.", 21, TextAnchor.MiddleCenter, false);
            subtitle.fontSize = 28;
            subtitle.rectTransform.sizeDelta = new Vector2(780f, 42f);
            Button closeButton = CreateButton(modal.transform, font, "RunShopCloseButton", "X", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-32f, -40f), new Vector2(66f, 66f), new Color(0.94f, 0.36f, 0.30f, 0.98f), Color.white, null, out _);
            Button reopenButton = CreateButton(parent, font, "RunShopReopenButton", "상점", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-136f, -278f), new Vector2(180f, 62f), new Color(0.14f, 0.66f, 0.92f, 0.96f), Color.white, null, out Text reopenLabel);
            reopenLabel.fontSize = 28;
            reopenButton.GetComponent<RectTransform>().sizeDelta = new Vector2(210f, 72f);
            reopenButton.gameObject.SetActive(false);

            Button[] buttons = new Button[3];
            Text[] titles = new Text[3];
            Text[] descriptions = new Text[3];
            Text[] prices = new Text[3];
            Image[] accents = new Image[3];

            for (int i = 0; i < buttons.Length; i++)
            {
                float y = -154f - i * 212f;
                buttons[i] = CreateButton(modal.transform, font, "RunShopOffer_" + i, string.Empty, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(840f, 188f), new Color(0.10f, 0.18f, 0.42f, 0.96f), Color.white, null, out _);
                accents[i] = CreatePanel(buttons[i].transform, "RunShopOfferAccent", new Vector2(28f, -38f), new Vector2(96f, 96f), new Color(0.38f, 0.82f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), true, false);
                CreateText(accents[i].transform, font, Color.white, "RunShopOfferIcon", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, "SHOP", 20, TextAnchor.MiddleCenter, true);
                titles[i] = CreateText(buttons[i].transform, font, Color.white, "Title", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(120f, -18f), new Vector2(-238f, 34f), "상품", 27, TextAnchor.MiddleLeft, true);
                descriptions[i] = CreateText(buttons[i].transform, font, new Color(0.84f, 0.91f, 1f), "Description", new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0.5f), new Vector2(120f, -12f), new Vector2(-250f, 54f), "설명", 20, TextAnchor.MiddleLeft, false);
                prices[i] = CreateText(buttons[i].transform, font, new Color(1f, 0.91f, 0.38f), "Price", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-30f, 0f), new Vector2(126f, 48f), "0G", 30, TextAnchor.MiddleCenter, true);
                titles[i].fontSize = 35;
                titles[i].rectTransform.anchoredPosition = new Vector2(140f, -18f);
                titles[i].rectTransform.sizeDelta = new Vector2(-284f, 46f);

                descriptions[i].fontSize = 28;
                descriptions[i].alignment = TextAnchor.UpperLeft;
                descriptions[i].rectTransform.anchorMin = new Vector2(0f, 1f);
                descriptions[i].rectTransform.pivot = new Vector2(0f, 1f);
                descriptions[i].rectTransform.anchoredPosition = new Vector2(140f, -70f);
                descriptions[i].rectTransform.sizeDelta = new Vector2(-286f, 98f);

                prices[i].fontSize = 38;
                prices[i].rectTransform.anchoredPosition = new Vector2(-32f, 0f);
                prices[i].rectTransform.sizeDelta = new Vector2(142f, 54f);
            }

            root.SetActive(false);
            runShopSystem.Configure(gameController, boardManager, tileModifierSystem, augmentManager, root, header, subtitle, buttons, titles, descriptions, prices, accents, closeButton, reopenButton);
        }

        private void BuildTacticalMissionPanel(Transform canvasRoot, Transform hudRoot, Font font, TacticalMissionSystem missionSystem, DefenseGameController gameController, DefenseBoardManager boardManager)
        {
            if (missionSystem == null)
            {
                return;
            }

            Button summaryButton = CreateButton(hudRoot, font, "MissionSummaryButton", string.Empty, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(22f, -154f), new Vector2(350f, 62f), new Color(0.12f, 0.18f, 0.38f, 0.96f), Color.white, null, out _);
            CreatePanel(summaryButton.transform, "MissionGlow", Vector2.zero, new Vector2(-20f, -18f), new Color(1f, 0.78f, 0.25f, 0.18f), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), true, false);
            CreatePanel(summaryButton.transform, "MissionIconSlot", new Vector2(18f, 0f), new Vector2(42f, 42f), new Color(1f, 0.74f, 0.24f, 0.92f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), true, false);
            CreateSkinIcon(summaryButton.transform, "MissionIcon", "mission", new Vector2(39f, 0f), new Vector2(30f, 30f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), Color.white);
            Text summaryText = CreateText(summaryButton.transform, font, Color.white, "MissionSummaryText", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(62f, 0f), new Vector2(-178f, 0f), "미션 선택", 22, TextAnchor.MiddleLeft, true);
            summaryText.resizeTextForBestFit = true;
            summaryText.resizeTextMinSize = 16;
            summaryText.resizeTextMaxSize = 22;
            Text summaryHint = CreateText(summaryButton.transform, font, new Color(0.82f, 0.90f, 1f), "MissionOpenHint", new Vector2(1f, 0f), Vector2.one, new Vector2(1f, 0.5f), new Vector2(-14f, 0f), new Vector2(62f, 0f), "열기", 16, TextAnchor.MiddleCenter, true);
            summaryHint.resizeTextForBestFit = true;
            summaryHint.resizeTextMinSize = 12;
            summaryHint.resizeTextMaxSize = 16;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Button debugDefeatButton = CreateButton(hudRoot, font, "DebugDefeatButton", "DEV 패배  [F8]", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(22f, -224f), new Vector2(194f, 48f), new Color(0.74f, 0.16f, 0.20f, 0.96f), Color.white, gameController.TriggerDebugDefeat, out Text debugDefeatLabel);
            debugDefeatLabel.fontSize = 17;
            debugDefeatLabel.resizeTextForBestFit = true;
            debugDefeatLabel.resizeTextMinSize = 13;
            debugDefeatLabel.resizeTextMaxSize = 17;
            Button debugNextRoundButton = CreateButton(hudRoot, font, "DebugNextRoundButton", "DEV 다음 R  [F9]", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(224f, -224f), new Vector2(194f, 48f), new Color(0.12f, 0.42f, 0.78f, 0.96f), Color.white, gameController.TriggerDebugAdvanceRound, out Text debugNextRoundLabel);
            debugNextRoundLabel.fontSize = 17;
            debugNextRoundLabel.resizeTextForBestFit = true;
            debugNextRoundLabel.resizeTextMinSize = 13;
            debugNextRoundLabel.resizeTextMaxSize = 17;
#endif

            GameObject root = new GameObject("TacticalMissionOverlay", typeof(RectTransform));
            root.transform.SetParent(canvasRoot, false);
            Image blocker = root.AddComponent<Image>();
            blocker.color = new Color(0.03f, 0.04f, 0.15f, 0.72f);
            blocker.raycastTarget = true;
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            Image modal = CreatePanel(root.transform, "MissionModal", Vector2.zero, new Vector2(860f, 760f), new Color(0.13f, 0.16f, 0.40f, 0.98f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), true, true);
            CreatePanel(modal.transform, "MissionTopGlow", new Vector2(0f, -34f), new Vector2(720f, 74f), new Color(1f, 0.76f, 0.22f, 0.20f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), true, false);
            Text header = CreateText(modal.transform, font, Color.white, "MissionPanelHeader", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -44f), new Vector2(420f, 44f), "전략 미션 선택", 35, TextAnchor.MiddleCenter, true);
            CreateText(modal.transform, font, new Color(0.86f, 0.92f, 1f), "MissionPanelSubHeader", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -104f), new Vector2(720f, 30f), "욕심을 낼지, 안정적으로 갈지 선택하세요.", 22, TextAnchor.MiddleCenter, false);
            Button closeButton = CreateButton(modal.transform, font, "MissionCloseButton", "X", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-26f, -36f), new Vector2(58f, 58f), new Color(0.94f, 0.36f, 0.30f, 0.98f), Color.white, null, out _);

            Image activeCard = CreatePanel(modal.transform, "ActiveMissionCard", new Vector2(0f, -168f), new Vector2(740f, 220f), new Color(0.08f, 0.12f, 0.30f, 0.94f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), true, true);
            CreatePanel(activeCard.transform, "ActiveMissionIcon", new Vector2(28f, -26f), new Vector2(76f, 76f), new Color(1f, 0.76f, 0.24f, 0.90f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), true, false);
            CreateSkinIcon(activeCard.transform, "ActiveMissionGlyph", "mission", new Vector2(66f, -64f), new Vector2(52f, 52f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), Color.white);
            Text activeTitle = CreateText(activeCard.transform, font, Color.white, "ActiveMissionTitle", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(124f, -24f), new Vector2(-164f, 38f), "미션", 30, TextAnchor.MiddleLeft, true);
            Text activeDescription = CreateText(activeCard.transform, font, new Color(0.88f, 0.94f, 1f), "ActiveMissionDescription", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(124f, -72f), new Vector2(-156f, 86f), "설명", 21, TextAnchor.UpperLeft, false);
            Text activeProgress = CreateText(activeCard.transform, font, new Color(1f, 0.90f, 0.42f), "ActiveMissionProgress", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(560f, 34f), "0 / 0", 24, TextAnchor.MiddleCenter, true);

            const int optionCount = 3;
            Button[] optionButtons = new Button[optionCount];
            Text[] optionTitles = new Text[optionCount];
            Text[] optionDescriptions = new Text[optionCount];
            Text[] optionRewards = new Text[optionCount];
            Image[] optionAccents = new Image[optionCount];

            for (int i = 0; i < optionCount; i++)
            {
                float y = -156f - i * 162f;
                optionButtons[i] = CreateButton(modal.transform, font, "MissionOption_" + i, string.Empty, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(740f, 144f), new Color(0.10f, 0.14f, 0.34f, 0.96f), Color.white, null, out _);
                optionAccents[i] = CreatePanel(optionButtons[i].transform, "MissionOptionIcon", new Vector2(26f, -28f), new Vector2(82f, 82f), new Color(1f, 0.76f, 0.24f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), true, false);
                CreatePanel(optionButtons[i].transform, "MissionOptionIconCore", new Vector2(67f, -69f), new Vector2(34f, 34f), new Color(1f, 1f, 1f, 0.24f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), true, false);
                CreateSkinIcon(optionButtons[i].transform, "MissionOptionGlyph", "mission", new Vector2(67f, -69f), new Vector2(52f, 52f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), Color.white);
                optionTitles[i] = CreateText(optionButtons[i].transform, font, Color.white, "Title", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(132f, -18f), new Vector2(-182f, 34f), "미션", 28, TextAnchor.MiddleLeft, true);
                optionDescriptions[i] = CreateText(optionButtons[i].transform, font, new Color(0.88f, 0.94f, 1f), "Description", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(132f, -56f), new Vector2(-182f, 34f), "설명", 19, TextAnchor.UpperLeft, false);
                optionRewards[i] = CreateText(optionButtons[i].transform, font, new Color(1f, 0.88f, 0.38f), "Reward", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(132f, 8f), new Vector2(-260f, 28f), "보상", 19, TextAnchor.MiddleLeft, true);
                CreateText(optionButtons[i].transform, font, new Color(0.45f, 1f, 0.68f), "PickLabel", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-26f, 0f), new Vector2(92f, 36f), "선택", 21, TextAnchor.MiddleRight, true);
            }

            Image toast = CreatePanel(hudRoot, "MissionCompletionToast", new Vector2(0f, -228f), new Vector2(620f, 126f), new Color(0.05f, 0.15f, 0.32f, 0.96f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), true, true);
            CanvasGroup toastGroup = toast.gameObject.AddComponent<CanvasGroup>();
            toastGroup.alpha = 0f;
            toastGroup.interactable = false;
            toastGroup.blocksRaycasts = false;
            CreatePanel(toast.transform, "MissionToastGlow", Vector2.zero, new Vector2(-20f, -18f), new Color(0.28f, 1f, 0.76f, 0.24f), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), true, false);
            CreatePanel(toast.transform, "MissionToastIconSlot", new Vector2(38f, 0f), new Vector2(68f, 68f), new Color(1f, 0.76f, 0.24f, 0.95f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), true, false);
            CreateSkinIcon(toast.transform, "MissionToastIcon", "mission", new Vector2(38f, 0f), new Vector2(46f, 46f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), Color.white);
            Text toastTitle = CreateText(toast.transform, font, Color.white, "MissionToastTitle", new Vector2(0f, 1f), Vector2.one, new Vector2(0f, 1f), new Vector2(92f, -24f), new Vector2(-120f, 42f), "미션 완료!", 31, TextAnchor.MiddleLeft, true);
            Text toastReward = CreateText(toast.transform, font, new Color(1f, 0.90f, 0.42f), "MissionToastReward", new Vector2(0f, 0f), Vector2.one, new Vector2(0f, 0f), new Vector2(92f, 22f), new Vector2(-120f, 38f), "+보상", 23, TextAnchor.MiddleLeft, true);
            toast.gameObject.SetActive(false);

            root.SetActive(false);
            missionSystem.Configure(gameController, boardManager, summaryButton, summaryText, root, header, activeCard.gameObject, activeTitle, activeDescription, activeProgress, optionButtons, optionTitles, optionDescriptions, optionRewards, optionAccents, closeButton, toast.gameObject, toastGroup, toastTitle, toastReward);
        }

        private void BuildSynergyPanel(Transform parent, Font font, BoardSynergySystem synergySystem, DefenseGameController gameController, DefenseBoardManager boardManager)
        {
            if (synergySystem == null)
            {
                return;
            }

            Image panel = CreatePanel(parent, "SynergyPanel", new Vector2(-28f, -128f), new Vector2(312f, 272f), new Color(0.08f, 0.12f, 0.28f, 0.92f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), true, true);
            CreatePanel(panel.transform, "SynergyGlow", new Vector2(0f, -12f), new Vector2(260f, 44f), new Color(0.28f, 0.94f, 1f, 0.22f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), true, false);
            Text header = CreateText(panel.transform, font, Color.white, "SynergyHeader", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(240f, 28f), "시너지 대기", 22, TextAnchor.MiddleCenter, true);

            const int rowCount = 5;
            Text[] titles = new Text[rowCount];
            Text[] descriptions = new Text[rowCount];
            Image[] accents = new Image[rowCount];

            for (int i = 0; i < rowCount; i++)
            {
                float y = -58f - i * 40f;
                Image row = CreatePanel(panel.transform, "SynergyRow_" + i, new Vector2(0f, y), new Vector2(282f, 38f), new Color(0.12f, 0.16f, 0.36f, 0.82f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), true, false);
                accents[i] = CreatePanel(row.transform, "Accent", new Vector2(14f, -17f), new Vector2(10f, 22f), new Color(0.42f, 0.48f, 0.64f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), true, false);
                titles[i] = CreateText(row.transform, font, Color.white, "Title", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(32f, -3f), new Vector2(224f, 18f), string.Empty, 17, TextAnchor.MiddleLeft, true);
                descriptions[i] = CreateText(row.transform, font, new Color(0.84f, 0.91f, 1f), "Description", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(32f, -21f), new Vector2(224f, 16f), string.Empty, 14, TextAnchor.MiddleLeft, false);
            }

            synergySystem.Configure(gameController, boardManager, panel.gameObject, header, titles, descriptions, accents);
        }

        private void BuildSynergyPanelExpanded(Transform parent, Font font, BoardSynergySystem synergySystem, DefenseGameController gameController, DefenseBoardManager boardManager)
        {
            if (synergySystem == null)
            {
                return;
            }

            Button summaryButton = CreateButton(parent, font, "SynergySummaryButton", string.Empty, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-318f, -154f), new Vector2(370f, 62f), new Color(0.10f, 0.16f, 0.34f, 0.96f), Color.white, null, out _);
            CreatePanel(summaryButton.transform, "SummaryGlow", Vector2.zero, new Vector2(-24f, -20f), new Color(0.22f, 0.92f, 1f, 0.20f), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), true, false);
            CreatePanel(summaryButton.transform, "SummaryIconSlot", new Vector2(24f, 0f), new Vector2(50f, 50f), new Color(0.25f, 0.10f, 0.68f, 0.86f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), true, false);
            CreateSkinIcon(summaryButton.transform, "SynergyIcon", "hero", new Vector2(49f, 0f), new Vector2(34f, 34f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), Color.white);
            Text summaryHeader = CreateText(summaryButton.transform, font, Color.white, "SummaryText", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(78f, 0f), new Vector2(200f, 42f), "시너지 대기중", 24, TextAnchor.MiddleLeft, true);
            CreateText(summaryButton.transform, font, new Color(0.80f, 0.88f, 1f), "SummaryHint", new Vector2(1f, 0f), Vector2.one, new Vector2(1f, 0.5f), new Vector2(-20f, 0f), new Vector2(84f, 0f), "열기", 22, TextAnchor.MiddleCenter, true);

            Image expandedPanel = CreatePanel(parent, "SynergyExpandedPanel", new Vector2(-318f, -224f), new Vector2(500f, 560f), new Color(0.08f, 0.12f, 0.30f, 0.97f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), true, true);
            CreatePanel(expandedPanel.transform, "ExpandedGlow", new Vector2(0f, -16f), new Vector2(420f, 66f), new Color(0.22f, 0.92f, 1f, 0.20f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), true, false);
            Text expandedHeader = CreateText(expandedPanel.transform, font, Color.white, "ExpandedHeader", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -34f), new Vector2(320f, 42f), "활성 시너지", 34, TextAnchor.MiddleCenter, true);
            CreateText(expandedPanel.transform, font, new Color(0.80f, 0.88f, 1f), "ExpandedHint", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -82f), new Vector2(410f, 34f), "같은 역할을 모아 강한 조합을 만드세요", 21, TextAnchor.MiddleCenter, false);
            Button closeButton = CreateButton(expandedPanel.transform, font, "SynergyCloseButton", "X", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-18f, -18f), new Vector2(58f, 58f), new Color(0.94f, 0.36f, 0.30f, 0.96f), Color.white, null, out _);

            const int rowCount = 5;
            Text[] titles = new Text[rowCount];
            Text[] descriptions = new Text[rowCount];
            Image[] accents = new Image[rowCount];
            Image[] icons = new Image[rowCount];

            for (int i = 0; i < rowCount; i++)
            {
                float y = -126f - i * 82f;
                Image row = CreatePanel(expandedPanel.transform, "SynergyExpandedRow_" + i, new Vector2(0f, y), new Vector2(454f, 74f), new Color(0.12f, 0.16f, 0.36f, 0.90f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), true, false);
                accents[i] = CreatePanel(row.transform, "AccentIconPlate", new Vector2(18f, 0f), new Vector2(54f, 54f), new Color(0.42f, 0.48f, 0.64f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), true, false);
                CreatePanel(row.transform, "AccentCore", new Vector2(45f, 0f), new Vector2(22f, 22f), new Color(1f, 1f, 1f, 0.32f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), true, false);
                titles[i] = CreateText(row.transform, font, Color.white, "Title", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(86f, -15f), new Vector2(286f, 28f), string.Empty, 24, TextAnchor.MiddleLeft, true);
                descriptions[i] = CreateText(row.transform, font, new Color(0.84f, 0.91f, 1f), "Description", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(86f, -45f), new Vector2(292f, 28f), string.Empty, 18, TextAnchor.MiddleLeft, false);
                Image iconSlot = CreatePanel(row.transform, "IconSlot", new Vector2(-16f, 0f), new Vector2(58f, 58f), new Color(0.05f, 0.08f, 0.20f, 0.72f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), true, false);
                icons[i] = CreatePanel(iconSlot.transform, "IconImage", Vector2.zero, new Vector2(38f, 38f), new Color(1f, 1f, 1f, 0.24f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), true, false);
                icons[i].preserveAspect = true;
            }

            expandedPanel.gameObject.SetActive(false);
            synergySystem.Configure(gameController, boardManager, summaryButton, summaryHeader, expandedPanel.gameObject, expandedHeader, titles, descriptions, accents, icons, closeButton);
        }

        private Transform CreateSafeAreaRoot(Transform parent, string rootName = "SafeAreaRoot")
        {
            GameObject safeAreaRoot = new GameObject(rootName, typeof(RectTransform));
            safeAreaRoot.transform.SetParent(parent, false);
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
            if (FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }

        private Text CreateCurrencyPill(Transform parent, Font font, string name, string icon, Vector2 anchoredPosition, Vector2 size, Color accentColor, string value)
        {
            Image pill = CreatePanel(parent, name, anchoredPosition, size, new Color(0.10f, 0.13f, 0.31f, 0.92f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), true, true);
            CreatePanel(pill.transform, "IconPlate", new Vector2(18f, -14f), new Vector2(44f, 44f), accentColor, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), true, false);
            Image iconImage = CreateSkinIcon(pill.transform, "CurrencyIcon", name + " " + icon, new Vector2(40f, -36f), new Vector2(34f, 34f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), Color.white);
            if (iconImage == null)
            {
                CreateText(pill.transform, font, Color.white, "IconLabel", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -14f), new Vector2(44f, 44f), icon, 18, TextAnchor.MiddleCenter, true);
            }

            return CreateText(pill.transform, font, Color.white, "ValueText", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-24f, 0f), new Vector2(size.x - 88f, 42f), value, 28, TextAnchor.MiddleRight, true);
        }

        private Image CreateProgressBar(Transform parent, Vector2 anchoredPosition, Vector2 size)
        {
            Image background = CreatePanel(parent, "RoundProgressBar", anchoredPosition, size, new Color(0.13f, 0.17f, 0.30f, 0.96f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), true, true);
            Image fill = CreatePanel(background.transform, "Fill", Vector2.zero, Vector2.zero, new Color(0.24f, 0.94f, 0.62f, 0.96f), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), true, false);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 0f;
            return fill;
        }

        private Text CreateBuildInsightCell(Transform parent, Font font, string name, string title, Vector2 anchoredPosition, Color accentColor)
        {
            Image cell = CreatePanel(parent, name, anchoredPosition, new Vector2(292f, 84f), new Color(0.08f, 0.13f, 0.32f, 0.90f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), true, false);
            CreatePanel(cell.transform, "Accent", new Vector2(9f, 0f), new Vector2(10f, 60f), accentColor, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), true, false);
            CreateText(cell.transform, font, Color.Lerp(accentColor, Color.white, 0.18f), "Title", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(10f, -9f), new Vector2(-18f, 30f), title, 22, TextAnchor.MiddleCenter, true);
            Text value = CreateText(cell.transform, font, Color.white, "Value", new Vector2(0f, 0f), Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(10f, -12f), new Vector2(-22f, 34f), "대기", 21, TextAnchor.MiddleCenter, true);
            value.resizeTextForBestFit = true;
            value.fontSize = 24;
            value.rectTransform.anchoredPosition = new Vector2(10f, -18f);
            value.rectTransform.sizeDelta = new Vector2(-22f, 44f);
            value.resizeTextMinSize = 18;
            value.resizeTextMaxSize = 24;
            AddStrongTextOutline(value);
            return value;
        }

        private Text CreateGradeCard(Transform parent, Font font, CharacterGrade grade, Vector2 anchoredPosition, UnityEngine.Events.UnityAction onClick, string mergeRequirementText)
        {
            string title = CharacterGradeUtility.GetDisplayName(grade);
            Color accentColor = CharacterGradeUtility.GetColor(grade, Color.white);
            string cardName = grade + "GradeCard";

            Button card = CreateButton(parent, font, cardName, string.Empty, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), anchoredPosition, new Vector2(144f, 124f), new Color(0.05f, 0.07f, 0.20f, 0.96f), Color.white, onClick, out _);
            if (onClick == null)
            {
                card.transition = Selectable.Transition.None;
            }

            Image body = CreatePanel(card.transform, "GradeBody", new Vector2(0f, -8f), new Vector2(132f, 108f), new Color(0.07f, 0.10f, 0.27f, 0.92f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), true, false);
            body.transform.SetAsFirstSibling();

            CreatePanel(card.transform, "TitleBack", new Vector2(0f, -9f), new Vector2(120f, 34f), Color.Lerp(accentColor, new Color(0.02f, 0.04f, 0.14f, 1f), 0.10f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), true, false);
            Text titleText = CreateText(card.transform, font, Color.white, "Title", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -11f), new Vector2(118f, 30f), title, 23, TextAnchor.MiddleCenter, true);
            AddStrongTextOutline(titleText);

            CreatePanel(card.transform, "MergeNeedBack", new Vector2(0f, -54f), new Vector2(108f, 27f), new Color(0.02f, 0.04f, 0.15f, 0.76f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), true, false);
            Text needText = CreateText(card.transform, font, Color.Lerp(accentColor, Color.white, 0.28f), "MergeNeedText", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -54f), new Vector2(104f, 24f), mergeRequirementText, 16, TextAnchor.MiddleCenter, true);
            AddStrongTextOutline(needText);

            CreatePanel(card.transform, "CountBack", new Vector2(0f, 11f), new Vector2(116f, 30f), new Color(0.02f, 0.04f, 0.14f, 0.84f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), true, false);
            Text count = CreateText(card.transform, font, Color.white, "Count", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 13f), new Vector2(114f, 27f), "0 / 3", 19, TextAnchor.MiddleCenter, true);
            AddStrongTextOutline(count);
            if (grade == CharacterGrade.Transcendent)
            {
                Image top = CreatePanel(card.transform, "ReadyGlowTop", new Vector2(0f, -3f), new Vector2(138f, 5f), Color.clear, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), true, false);
                Image right = CreatePanel(card.transform, "ReadyGlowRight", new Vector2(-3f, 0f), new Vector2(5f, 118f), Color.clear, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), true, false);
                Image bottom = CreatePanel(card.transform, "ReadyGlowBottom", new Vector2(0f, 3f), new Vector2(138f, 5f), Color.clear, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), true, false);
                Image left = CreatePanel(card.transform, "ReadyGlowLeft", new Vector2(3f, 0f), new Vector2(5f, 118f), Color.clear, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), true, false);
                Image readyBadge = CreatePanel(card.transform, "ReadyBadge", new Vector2(-3f, -3f), new Vector2(88f, 28f), new Color(1f, 0.72f, 0.16f, 0.98f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), true, false);
                Text readyBadgeText = CreateText(readyBadge.transform, font, new Color(0.18f, 0.05f, 0.24f), "ReadyBadgeText", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, "READY", 14, TextAnchor.MiddleCenter, true);
                AddStrongTextOutline(readyBadgeText);
                top.gameObject.SetActive(false);
                right.gameObject.SetActive(false);
                bottom.gameObject.SetActive(false);
                left.gameObject.SetActive(false);
                readyBadge.gameObject.SetActive(false);
            }
            return count;
        }

        private Text CreateText(Transform parent, Font font, Color color, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 size, string value, int fontSize, TextAnchor alignment, bool bold)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.color = RuntimeUiSkinUtility.ResolveReadableTextColor(parent, color, presentationConfig != null ? presentationConfig.uiSkin : null);
            text.text = RuntimeKoreanTextUtility.Clean(name, value);
            text.alignment = alignment;
            text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            text.raycastTarget = false;

            RectTransform rect = text.GetComponent<RectTransform>();
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
            panelObject.transform.SetParent(parent, false);
            Image image = panelObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            RuntimeUiSkinUtility.ApplyImageSkin(image, presentationConfig != null ? presentationConfig.uiSkin : null, name, false, rounded);
            ApplyRuntimeRoundedShape(image, rounded);

            RectTransform rect = image.rectTransform;
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

        private Button CreateButton(Transform parent, Font font, string name, string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 size, Color backgroundColor, Color labelColor, UnityEngine.Events.UnityAction onClick, out Text labelText)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform));
            buttonObject.transform.SetParent(parent, false);
            Image image = buttonObject.AddComponent<Image>();
            image.color = backgroundColor;
            RuntimeUiSkinUtility.ApplyImageSkin(image, presentationConfig != null ? presentationConfig.uiSkin : null, name, true, true);
            ApplyRuntimeRoundedShape(image, true);
            image.raycastTarget = true;

            Shadow shadow = buttonObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.35f);
            shadow.effectDistance = new Vector2(0f, -7f);

            Button button = buttonObject.AddComponent<Button>();
            button.onClick.AddListener(RuntimeAudioUtility.PlayButton);
            if (onClick != null)
            {
                button.onClick.AddListener(onClick);
            }

            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            labelText = CreateText(buttonObject.transform, font, labelColor, "Label", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, label, 27, TextAnchor.MiddleCenter, true);
            TryAddButtonIcon(buttonObject.transform, name, label, size, labelText);
            return button;
        }

        private void ApplyRuntimeRoundedShape(Image image, bool rounded)
        {
            if (image == null || !rounded)
            {
                return;
            }

            image.sprite = GetRoundedPanelSprite();
            image.type = Image.Type.Sliced;
            image.preserveAspect = false;
        }

        private Image CreateSkinIcon(Transform parent, string name, string iconKey, Vector2 anchoredPosition, Vector2 size, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Color color)
        {
            UiSkinResources skin = presentationConfig != null ? presentationConfig.uiSkin : null;
            Sprite sprite = RuntimeUiSkinUtility.ResolveIconSprite(skin, iconKey);
            if (sprite == null)
            {
                return null;
            }

            GameObject iconObject = new GameObject(name, typeof(RectTransform));
            iconObject.transform.SetParent(parent, false);
            Image image = iconObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.color = color;
            image.preserveAspect = true;
            image.raycastTarget = false;

            RectTransform rect = image.rectTransform;
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
                float y = 10f - i * 10f;
                CreatePanel(parent, "HamburgerLine_" + i, new Vector2(0f, y), new Vector2(34f, 5f), new Color(0.92f, 0.96f, 1f, 0.96f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), true, false);
            }
        }

        private void TryAddButtonIcon(Transform buttonTransform, string name, string label, Vector2 size, Text labelText)
        {
            if (string.IsNullOrWhiteSpace(label))
            {
                return;
            }

            string iconKey = name + " " + label;
            UiSkinResources skin = presentationConfig != null ? presentationConfig.uiSkin : null;
            if (RuntimeUiSkinUtility.ResolveIconSprite(skin, iconKey) == null)
            {
                return;
            }

            bool iconOnly = string.Equals(label, "X", System.StringComparison.OrdinalIgnoreCase);
            if (!iconOnly)
            {
                return;
            }

            float iconSize = Mathf.Clamp(Mathf.Min(size.x, size.y) * 0.44f, 24f, 42f);
            Vector2 iconPosition = iconOnly ? Vector2.zero : new Vector2(30f, 0f);
            Vector2 anchor = iconOnly ? new Vector2(0.5f, 0.5f) : new Vector2(0f, 0.5f);
            Image icon = CreateSkinIcon(buttonTransform, "ButtonIcon", iconKey, iconPosition, new Vector2(iconSize, iconSize), anchor, anchor, new Vector2(0.5f, 0.5f), Color.white);
            if (icon == null || labelText == null)
            {
                return;
            }

            if (iconOnly)
            {
                labelText.enabled = false;
                return;
            }

            RectTransform labelRect = labelText.rectTransform;
            Vector2 offsetMin = labelRect.offsetMin;
            offsetMin.x = Mathf.Max(offsetMin.x, iconSize + 24f);
            labelRect.offsetMin = offsetMin;
        }

        private void AddTextShadow(Text text)
        {
            Shadow shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.42f);
            shadow.effectDistance = new Vector2(2f, -2f);
        }

        private void AddStrongTextOutline(Text text)
        {
            if (text == null)
            {
                return;
            }

            Outline outline = text.GetComponent<Outline>();
            if (outline == null)
            {
                outline = text.gameObject.AddComponent<Outline>();
            }

            outline.effectColor = new Color(0f, 0f, 0f, 0.82f);
            outline.effectDistance = new Vector2(1.7f, -1.7f);
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
            Texture2D texture = new Texture2D(width, height, TextureFormat.ARGB32, false);
            texture.name = name;
            texture.wrapMode = TextureWrapMode.Clamp;

            Color[] pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float nearestX = Mathf.Clamp(x, radius, width - radius - 1f);
                    float nearestY = Mathf.Clamp(y, radius, height - radius - 1f);
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(nearestX, nearestY));
                    float alpha = Mathf.Clamp01(radius + 0.5f - distance);
                    pixels[y * width + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
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
            Color[] colors = presentationConfig != null && presentationConfig.slotColors != null && presentationConfig.slotColors.Length > 0
                ? presentationConfig.slotColors
                : DefaultSlotColors;
            return colors[index % colors.Length];
        }

        private Color GetLaneColor(int index)
        {
            Color[] colors = presentationConfig != null && presentationConfig.laneColors != null && presentationConfig.laneColors.Length > 0
                ? presentationConfig.laneColors
                : DefaultLaneColors;
            return colors[index % colors.Length];
        }

        private Color GetConfigColor(System.Func<GamePresentationConfig, Color> selector, Color fallback)
        {
            return presentationConfig != null ? selector(presentationConfig) : fallback;
        }

        private void SafeDestroy(GameObject target)
        {
            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }

        private void SafeDestroy(Component target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }

        private void AssignPrivateField(Object target, string fieldName, object value)
        {
            System.Reflection.FieldInfo field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (field != null)
            {
                field.SetValue(target, value);
            }
        }
    }

    public sealed class UltimateRecipeSelectionUI : MonoBehaviour
    {
        private const float SlideDuration = 0.24f;

        private DefenseGameController gameController;
        private RectTransform drawer;
        private CanvasGroup canvasGroup;
        private Button blockerButton;
        private Text headerText;
        private Text instructionText;
        private Button[] optionButtons;
        private Text[] optionLabels;
        private Button closeButton;
        private Button confirmButton;
        private Text confirmLabel;
        private UltimateRecipeOption[] options = new UltimateRecipeOption[0];
        private Vector2 drawerOpenPosition;
        private Vector2 drawerClosedPosition;
        private float slideProgress;
        private bool targetOpen;
        private int selectedIndex = -1;

        public void Configure(
            DefenseGameController controller,
            RectTransform drawerRect,
            CanvasGroup group,
            Button blocker,
            Text header,
            Text instruction,
            Button[] buttons,
            Text[] labels,
            Button close,
            Button confirm,
            Text confirmText)
        {
            gameController = controller;
            drawer = drawerRect;
            canvasGroup = group;
            blockerButton = blocker;
            headerText = header;
            instructionText = instruction;
            optionButtons = buttons ?? new Button[0];
            optionLabels = labels ?? new Text[0];
            closeButton = close;
            confirmButton = confirm;
            confirmLabel = confirmText;
            drawerOpenPosition = drawer != null ? drawer.anchoredPosition : Vector2.zero;
            drawerClosedPosition = drawerOpenPosition + Vector2.down * 860f;

            if (blockerButton != null)
            {
                blockerButton.onClick.RemoveAllListeners();
                blockerButton.onClick.AddListener(Close);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(Close);
            }

            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveAllListeners();
                confirmButton.onClick.AddListener(ConfirmSelection);
            }

            for (int i = 0; i < optionButtons.Length; i++)
            {
                int optionIndex = i;
                if (optionButtons[i] == null)
                {
                    continue;
                }

                optionButtons[i].onClick.RemoveAllListeners();
                optionButtons[i].onClick.AddListener(() => SelectOption(optionIndex));
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = true;
            }

            if (drawer != null)
            {
                drawer.anchoredPosition = drawerClosedPosition;
            }
        }

        public void Open()
        {
            if (gameController == null || gameController.IsCombatInteractionLocked)
            {
                return;
            }

            options = gameController.GetAllUltimateRecipeOptions();
            if (options == null || options.Length == 0)
            {
                gameController.RequestBanner("초월 레시피 정보를 불러오지 못했습니다", new Color(0.72f, 0.82f, 1f), 1.8f);
                return;
            }

            selectedIndex = -1;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            slideProgress = 0f;
            targetOpen = true;
            if (drawer != null)
            {
                drawer.anchoredPosition = drawerClosedPosition;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            RefreshOptionVisuals();
            PreviewSelectedRecipe();
        }

        public void Close()
        {
            if (!gameObject.activeSelf)
            {
                return;
            }

            targetOpen = false;
            if (canvasGroup != null)
            {
                canvasGroup.interactable = false;
            }

            if (gameController != null)
            {
                gameController.SetUltimateRecipePreview(null);
            }
        }

        private void Update()
        {
            float direction = targetOpen ? 1f : -1f;
            slideProgress = Mathf.Clamp01(slideProgress + direction * Time.unscaledDeltaTime / SlideDuration);
            float eased = 1f - Mathf.Pow(1f - slideProgress, 3f);
            if (drawer != null)
            {
                drawer.anchoredPosition = Vector2.LerpUnclamped(drawerClosedPosition, drawerOpenPosition, eased);
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = eased;
            }

            RefreshReadyOutlinePulse();
            if (!targetOpen && slideProgress <= 0f)
            {
                gameObject.SetActive(false);
            }
        }

        private void RefreshReadyOutlinePulse()
        {
            if (optionButtons == null)
            {
                return;
            }

            int optionCount = options != null ? options.Length : 0;
            float pulse = (Mathf.Sin(Time.unscaledTime * 5.2f) + 1f) * 0.5f;
            Color lowGlow = new Color(0.70f, 0.24f, 1f, 0.72f);
            Color highGlow = new Color(1f, 0.86f, 0.24f, 1f);
            for (int i = 0; i < optionButtons.Length; i++)
            {
                Button button = optionButtons[i];
                if (button == null)
                {
                    continue;
                }

                Outline outline = button.GetComponent<Outline>();
                if (outline == null)
                {
                    continue;
                }

                bool ready = i < optionCount && options[i].isReady && button.gameObject.activeSelf;
                if (!ready)
                {
                    outline.effectColor = Color.clear;
                    continue;
                }

                float selectedBoost = i == selectedIndex ? 0.18f : 0f;
                outline.effectColor = Color.Lerp(lowGlow, highGlow, Mathf.Clamp01(pulse + selectedBoost));
                float width = 2.5f + pulse * 2.5f + selectedBoost * 3f;

                outline.effectDistance = new Vector2(width, -width);
            }
        }
        private void SelectOption(int index)
        {
            if (index < 0 || index >= options.Length)
            {
                return;
            }

            selectedIndex = index;
            RefreshOptionVisuals();
            PreviewSelectedRecipe();
        }

        private void PreviewSelectedRecipe()
        {
            string recipeName = selectedIndex >= 0 && selectedIndex < options.Length
                ? options[selectedIndex].recipeName
                : null;
            if (gameController != null)
            {
                gameController.SetUltimateRecipePreview(recipeName, true);
            }
        }

        private void ConfirmSelection()
        {
            if (gameController == null || selectedIndex < 0 || selectedIndex >= options.Length || !options[selectedIndex].isReady)
            {
                return;
            }

            string recipeName = options[selectedIndex].recipeName;
            if (gameController.TryMergeUltimateRecipe(recipeName))
            {
                Close();
                return;
            }

            gameController.RequestBanner("초월 재료 상태가 변경되었습니다. 다시 선택하세요", new Color(1f, 0.58f, 0.24f), 2.0f);
            options = gameController.GetAllUltimateRecipeOptions();
            selectedIndex = options != null && options.Length == 1 ? 0 : -1;
            RefreshOptionVisuals();
            PreviewSelectedRecipe();
        }

        private void RefreshOptionVisuals()
        {
            int optionCount = options != null ? options.Length : 0;
            int readyCount = 0;
            for (int i = 0; i < optionCount; i++)
            {
                if (options[i].isReady)
                {
                    readyCount++;
                }
            }
            if (headerText != null)
            {
                headerText.text = "초월 레시피  READY " + readyCount + " / " + optionCount;
            }

            if (instructionText != null)
            {
                instructionText.text = selectedIndex >= 0
                    ? options[selectedIndex].isReady
                        ? "재료가 모두 준비됐습니다. 보드의 빛나는 재료를 확인하고 실행하세요."
                        : "부족 재료: " + options[selectedIndex].missingSummary
                    : "레시피를 누르면 보유 재료와 부족한 유닛을 확인할 수 있습니다.";
            }

            for (int i = 0; i < optionButtons.Length; i++)
            {
                Button button = optionButtons[i];
                bool visible = i < optionCount;
                if (button == null)
                {
                    continue;
                }

                button.gameObject.SetActive(visible);
                if (!visible)
                {
                    continue;
                }

                UltimateRecipeOption option = options[i];
                bool selected = i == selectedIndex;
                Color readinessColor = option.isReady ? option.accentColor : new Color(0.36f, 0.40f, 0.58f, 1f);
                Color baseColor = Color.Lerp(readinessColor, new Color(0.08f, 0.07f, 0.24f, 1f), selected ? 0.36f : 0.76f);
                Graphic graphic = button.targetGraphic;
                if (graphic != null)
                {
                    graphic.color = baseColor;
                }

                ColorBlock colors = button.colors;
                colors.normalColor = baseColor;
                colors.highlightedColor = Color.Lerp(baseColor, Color.white, 0.16f);
                colors.selectedColor = Color.Lerp(baseColor, Color.white, 0.22f);
                button.colors = colors;

                if (i < optionLabels.Length && optionLabels[i] != null)
                {
                    string state = option.isReady ? "READY" : option.progress + "/" + option.required;
                    optionLabels[i].text = (selected ? "▶ " : string.Empty) + "[" + state + "] " + option.displayName +
                        "\n결과  " + Compact(option.resultSummary, 32) +
                        "\n" + (option.isReady ? "소모  " + Compact(option.materialSummary, 46) : "부족  " + Compact(option.missingSummary, 46));
                    optionLabels[i].color = selected ? new Color(1f, 0.94f, 0.58f) : Color.white;
                }
            }

            bool canConfirm = selectedIndex >= 0 && selectedIndex < optionCount && options[selectedIndex].isReady;
            if (confirmButton != null)
            {
                confirmButton.interactable = canConfirm;
            }

            if (confirmLabel != null)
            {
                confirmLabel.text = canConfirm ? "선택한 초월 실행" : "레시피를 선택하세요";
            }
        }

        private static string Compact(string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length <= maxLength)
            {
                return string.IsNullOrWhiteSpace(value) ? "-" : value;
            }

            return value.Substring(0, Mathf.Max(1, maxLength - 3)) + "...";
        }

        private void OnDisable()
        {
            targetOpen = false;
            if (gameController != null)
            {
                gameController.SetUltimateRecipePreview(null);
            }
        }
    }
}
