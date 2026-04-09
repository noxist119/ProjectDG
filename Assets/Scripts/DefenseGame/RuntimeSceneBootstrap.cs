
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DefenseGame
{
    public class RuntimeSceneBootstrap : MonoBehaviour
    {
        [SerializeField] private bool buildOnStart = true;
        [SerializeField] private int slotCount = 10;
        [SerializeField] private int laneCount = 4;
        [SerializeField] private Vector3 boardCenter = new Vector3(0f, 0f, -5.5f);
        [SerializeField] private Vector3 spawnCenter = new Vector3(0f, 0f, 8f);
        [SerializeField] private float laneSpacing = 3.2f;
        [SerializeField] private float slotSpacing = 1.8f;
        [SerializeField] private GamePresentationConfig presentationConfig;
        [SerializeField] private bool hideDefaultStageDecorWhenUsingBackground = true;

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

        private void Start()
        {
            if (buildOnStart)
            {
                BuildScene();
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

            characterDatabase.ApplyPresentationConfig(presentationConfig);
            monsterDatabase.ApplyPresentationConfig(presentationConfig);

            Transform root = EnsureRoot("RuntimeStageRoot");
            Transform boardRoot = EnsureChild(root, "BoardSlots");
            Transform laneRoot = EnsureChild(root, "LaneDecor");
            Transform spawnRoot = EnsureChild(root, "SpawnPoints");
            Transform templateRoot = EnsureChild(root, "Templates");
            Transform miscRoot = EnsureChild(root, "Misc");

            ClearChildren(boardRoot);
            ClearChildren(laneRoot);
            ClearChildren(spawnRoot);
            ClearChildren(templateRoot);
            ClearChildren(miscRoot);

            EnsureGround(root);
            EnsureBackdrop(root);
            EnsureCamera();
            EnsureLight();

            List<BoardSlot> slots = BuildSlots(boardRoot);
            BuildLaneDecor(laneRoot);
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
            roundManager.Configure(monsterDatabase, monsterTemplate, spawnPoints, goalPoint);
            gameController.Configure(characterDatabase, monsterDatabase, boardManager, roundManager, defenderTemplate);
            demoInput.Configure(gameController);
            buttonBinder.Configure(gameController);

            BuildCanvas(root, hud, gameController, buttonBinder);
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

            camera.transform.position = new Vector3(0f, 14f, -11f);
            camera.transform.rotation = Quaternion.Euler(50f, 0f, 0f);
            camera.backgroundColor = new Color(0.05f, 0.07f, 0.11f);
            camera.clearFlags = CameraClearFlags.SolidColor;
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
            float width = (slotCount - 1) * slotSpacing;

            for (int i = 0; i < slotCount; i++)
            {
                Color baseColor = GetSlotColor(i);
                Color trimColor = GetSlotColor(i + 3);

                GameObject slotObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                slotObject.name = "Slot_" + i.ToString("D2");
                slotObject.transform.SetParent(boardRoot);
                slotObject.transform.position = boardCenter + new Vector3(-width * 0.5f + i * slotSpacing, 0f, 0f);
                slotObject.transform.localScale = new Vector3(1.28f, 0.22f, 1.35f);
                slotObject.GetComponent<Renderer>().material.color = baseColor;

                BoardSlot slot = slotObject.AddComponent<BoardSlot>();
                GameObject anchor = new GameObject("Anchor");
                anchor.transform.SetParent(slotObject.transform);
                anchor.transform.localPosition = new Vector3(0f, 0.8f, 0f);
                AssignPrivateField(slot, "unitAnchor", anchor.transform);
                slots.Add(slot);

                GameObject plate = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                plate.name = "Aura";
                plate.transform.SetParent(slotObject.transform);
                plate.transform.localPosition = new Vector3(0f, -0.05f, 0f);
                plate.transform.localScale = new Vector3(0.75f, 0.03f, 0.75f);
                plate.GetComponent<Renderer>().material.color = trimColor * 0.9f;

                GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                marker.name = "Marker";
                marker.transform.SetParent(slotObject.transform);
                marker.transform.localPosition = new Vector3(0f, 0.22f, -0.35f);
                marker.transform.localScale = Vector3.one * 0.22f;
                marker.GetComponent<Renderer>().material.color = Color.Lerp(baseColor, Color.white, 0.35f);
            }

            return slots;
        }

        private void BuildLaneDecor(Transform laneRoot)
        {
            float width = (laneCount - 1) * laneSpacing;

            for (int i = 0; i < laneCount; i++)
            {
                Color laneColor = GetLaneColor(i);
                float x = -width * 0.5f + i * laneSpacing;

                GameObject beacon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                beacon.name = "LaneBeacon_" + i.ToString("D2");
                beacon.transform.SetParent(laneRoot);
                beacon.transform.position = new Vector3(x, 0.55f, 7.6f);
                beacon.transform.localScale = new Vector3(0.45f, 0.55f, 0.45f);
                beacon.GetComponent<Renderer>().material.color = laneColor;

                GameObject crystal = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                crystal.name = "LaneCrystal_" + i.ToString("D2");
                crystal.transform.SetParent(laneRoot);
                crystal.transform.position = new Vector3(x, 1.55f, 7.6f);
                crystal.transform.localScale = new Vector3(0.35f, 0.75f, 0.35f);
                crystal.GetComponent<Renderer>().material.color = laneColor * 1.15f;

                GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                stripe.name = "LaneStripe_" + i.ToString("D2");
                stripe.transform.SetParent(laneRoot);
                stripe.transform.position = new Vector3(x, -0.1f, 1.0f);
                stripe.transform.localScale = new Vector3(0.18f, 0.03f, 13.5f);
                stripe.GetComponent<Renderer>().material.color = laneColor * 0.75f;
            }
        }

        private Transform[] BuildSpawnPoints(Transform spawnRoot)
        {
            Transform[] points = new Transform[laneCount];
            float width = (laneCount - 1) * laneSpacing;

            for (int i = 0; i < laneCount; i++)
            {
                Color laneColor = GetLaneColor(i);
                GameObject point = new GameObject("Spawn_" + i.ToString("D2"));
                point.transform.SetParent(spawnRoot);
                point.transform.position = spawnCenter + new Vector3(-width * 0.5f + i * laneSpacing, 0f, 0f);
                points[i] = point.transform;

                if (presentationConfig != null && presentationConfig.spawnPortalPrefab != null)
                {
                    GameObject portalOverride = Instantiate(presentationConfig.spawnPortalPrefab, point.transform);
                    portalOverride.name = "PortalVisual";
                    portalOverride.transform.localPosition = Vector3.zero;
                    portalOverride.transform.localRotation = Quaternion.identity;
                }
                else
                {
                    GameObject portal = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    portal.name = "PortalVisual";
                    portal.transform.SetParent(point.transform);
                    portal.transform.localPosition = new Vector3(0f, 1.2f, 0f);
                    portal.transform.localScale = Vector3.one * 0.9f;
                    portal.GetComponent<Renderer>().material.color = laneColor * 1.1f;

                    GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    ring.name = "PortalRing";
                    ring.transform.SetParent(point.transform);
                    ring.transform.localPosition = new Vector3(0f, 0.08f, 0f);
                    ring.transform.localScale = new Vector3(0.85f, 0.02f, 0.85f);
                    ring.GetComponent<Renderer>().material.color = Color.Lerp(laneColor, Color.white, 0.25f);
                }
            }

            return points;
        }

        private Transform BuildGoal(Transform miscRoot, bool logicOnly)
        {
            if (logicOnly)
            {
                GameObject hiddenGoal = new GameObject("GoalPoint");
                hiddenGoal.transform.SetParent(miscRoot);
                hiddenGoal.transform.position = new Vector3(0f, 0f, -8.5f);
                return hiddenGoal.transform;
            }

            if (presentationConfig != null && presentationConfig.goalPrefab != null)
            {
                GameObject overrideGoal = Instantiate(presentationConfig.goalPrefab, miscRoot);
                overrideGoal.name = "GoalPoint";
                overrideGoal.transform.localPosition = new Vector3(0f, 0f, -8.5f);
                overrideGoal.transform.localRotation = Quaternion.identity;
                return overrideGoal.transform;
            }

            GameObject goal = new GameObject("GoalPoint");
            goal.transform.SetParent(miscRoot);
            goal.transform.position = new Vector3(0f, 0f, -8.5f);

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

            unit.ConfigureRuntimePieces(projectileTemplate, firePoint, unitObject.GetComponentsInChildren<Renderer>(true));
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

        private void BuildCanvas(Transform root, SimpleGameHUD hud, DefenseGameController gameController, GameUIButtonBinder binder)
        {
            Canvas existingCanvas = FindObjectOfType<Canvas>();
            if (existingCanvas != null)
            {
                SafeDestroy(existingCanvas.gameObject);
            }

            GameObject canvasObject = new GameObject("RuntimeCanvas");
            canvasObject.transform.SetParent(root);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObject.AddComponent<GraphicRaycaster>();

            Font font = presentationConfig != null && presentationConfig.uiFont != null
                ? presentationConfig.uiFont
                : RuntimeFontProvider.GetDefaultFont();
            Color textColor = presentationConfig != null ? presentationConfig.hudTextColor : Color.white;
            string hintValue = presentationConfig != null && !string.IsNullOrWhiteSpace(presentationConfig.hintText)
                ? presentationConfig.hintText
                : "Space Round | S Summon | 1-4 Merge | C Add Heroes | M Add Monsters";

            CreatePanel(canvas.transform, "TopPanel", new Vector2(14f, -14f), new Vector2(760f, 154f), new Color(0.05f, 0.08f, 0.12f, 0.78f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
            CreatePanel(canvas.transform, "BottomPanel", new Vector2(18f, 18f), new Vector2(810f, 86f), new Color(0.05f, 0.08f, 0.12f, 0.82f), Vector2.zero, Vector2.zero, Vector2.zero);
            Text title = CreateText(canvas.transform, font, new Color(0.82f, 0.92f, 1f), new Vector2(130f, -8f), "Defense Command");
            title.fontSize = 24;
            title.fontStyle = FontStyle.Bold;

            Text gold = CreateText(canvas.transform, font, textColor, new Vector2(120f, -30f), "Gold : 0");
            Text life = CreateText(canvas.transform, font, textColor, new Vector2(120f, -60f), "Life : 0");
            Text round = CreateText(canvas.transform, font, textColor, new Vector2(120f, -90f), "Round : 0");
            Text board = CreateText(canvas.transform, font, textColor, new Vector2(120f, -120f), "Units : 0");
            Text content = CreateText(canvas.transform, font, textColor, new Vector2(180f, -150f), "Characters : 0 / Monsters : 0");
            Text mergeResult = CreateText(canvas.transform, font, new Color(0.95f, 0.87f, 0.42f), new Vector2(520f, -92f), "Merge Result : waiting");
            mergeResult.alignment = TextAnchor.MiddleLeft;
            mergeResult.fontStyle = FontStyle.Bold;
            mergeResult.fontSize = 18;
            RectTransform mergeRect = mergeResult.GetComponent<RectTransform>();
            mergeRect.sizeDelta = new Vector2(420f, 28f);
            Text mergeCelebration = CreateText(canvas.transform, font, new Color(1f, 0.92f, 0.5f, 0f), Vector2.zero, string.Empty);
            mergeCelebration.alignment = TextAnchor.MiddleCenter;
            mergeCelebration.fontStyle = FontStyle.Bold;
            mergeCelebration.fontSize = 42;
            RectTransform mergeCelebrationRect = mergeCelebration.GetComponent<RectTransform>();
            mergeCelebrationRect.anchorMin = new Vector2(0.5f, 0.5f);
            mergeCelebrationRect.anchorMax = new Vector2(0.5f, 0.5f);
            mergeCelebrationRect.pivot = new Vector2(0.5f, 0.5f);
            mergeCelebrationRect.anchoredPosition = new Vector2(0f, 170f);
            mergeCelebrationRect.sizeDelta = new Vector2(640f, 64f);

            Text mergeCelebrationSub = CreateText(canvas.transform, font, new Color(1f, 0.98f, 0.9f, 0f), Vector2.zero, string.Empty);
            mergeCelebrationSub.alignment = TextAnchor.MiddleCenter;
            mergeCelebrationSub.fontStyle = FontStyle.Bold;
            mergeCelebrationSub.fontSize = 24;
            RectTransform mergeCelebrationSubRect = mergeCelebrationSub.GetComponent<RectTransform>();
            mergeCelebrationSubRect.anchorMin = new Vector2(0.5f, 0.5f);
            mergeCelebrationSubRect.anchorMax = new Vector2(0.5f, 0.5f);
            mergeCelebrationSubRect.pivot = new Vector2(0.5f, 0.5f);
            mergeCelebrationSubRect.anchoredPosition = new Vector2(0f, 126f);
            mergeCelebrationSubRect.sizeDelta = new Vector2(760f, 40f);
            Text hint = CreateText(canvas.transform, font, textColor, new Vector2(310f, -30f), hintValue);
            hint.alignment = TextAnchor.MiddleLeft;
            RectTransform hintRect = hint.GetComponent<RectTransform>();
            hintRect.sizeDelta = new Vector2(620f, 30f);
            Text countdown = CreateText(canvas.transform, font, new Color(1f, 0.95f, 0.58f, 0f), Vector2.zero, string.Empty);
            countdown.alignment = TextAnchor.MiddleCenter;
            countdown.fontStyle = FontStyle.Bold;
            countdown.fontSize = 82;
            RectTransform countdownRect = countdown.GetComponent<RectTransform>();
            countdownRect.anchorMin = new Vector2(0.5f, 0.5f);
            countdownRect.anchorMax = new Vector2(0.5f, 0.5f);
            countdownRect.pivot = new Vector2(0.5f, 0.5f);
            countdownRect.anchoredPosition = new Vector2(0f, 35f);
            countdownRect.sizeDelta = new Vector2(220f, 120f);

            Text roundBanner = CreateText(canvas.transform, font, new Color(0.48f, 1f, 0.72f, 0f), Vector2.zero, string.Empty);
            roundBanner.alignment = TextAnchor.MiddleCenter;
            roundBanner.fontStyle = FontStyle.Bold;
            roundBanner.fontSize = 34;
            RectTransform bannerRect = roundBanner.GetComponent<RectTransform>();
            bannerRect.anchorMin = new Vector2(0.5f, 0.5f);
            bannerRect.anchorMax = new Vector2(0.5f, 0.5f);
            bannerRect.pivot = new Vector2(0.5f, 0.5f);
            bannerRect.anchoredPosition = new Vector2(0f, 120f);
            bannerRect.sizeDelta = new Vector2(520f, 60f);

            CreateButton(canvas.transform, font, "Start", new Vector2(90f, 60f), binder.OnClickStartRound);
            CreateButton(canvas.transform, font, "Summon", new Vector2(210f, 60f), binder.OnClickSummon);
            CreateButton(canvas.transform, font, "Merge N", new Vector2(330f, 60f), binder.OnClickMergeNormal);
            CreateButton(canvas.transform, font, "Merge R", new Vector2(450f, 60f), binder.OnClickMergeRare);
            CreateButton(canvas.transform, font, "Merge E", new Vector2(570f, 60f), binder.OnClickMergeEpic);
            CreateButton(canvas.transform, font, "Merge L", new Vector2(690f, 60f), binder.OnClickMergeLegendary);

            hud.Configure(gameController, gold, life, round, board, content, hint, mergeResult, mergeCelebration, mergeCelebrationSub, countdown, roundBanner, hintValue);
        }

        private Text CreateText(Transform parent, Font font, Color color, Vector2 anchoredPosition, string value)
        {
            GameObject textObject = new GameObject(value.Replace(" ", string.Empty) + "Text");
            textObject.transform.SetParent(parent);
            Text text = textObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = 20;
            text.color = color;
            text.text = value;
            text.alignment = TextAnchor.MiddleCenter;

            RectTransform rect = text.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(300f, 28f);
            return text;
        }

        private void CreatePanel(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
        {
            GameObject panelObject = new GameObject(name);
            panelObject.transform.SetParent(parent);
            Image image = panelObject.AddComponent<Image>();
            image.color = color;

            RectTransform rect = image.rectTransform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private void CreateButton(Transform parent, Font font, string label, Vector2 anchoredPosition, UnityEngine.Events.UnityAction onClick)
        {
            GameObject buttonObject = new GameObject(label + "Button");
            buttonObject.transform.SetParent(parent);
            Image image = buttonObject.AddComponent<Image>();
            image.color = presentationConfig != null ? presentationConfig.buttonColor : new Color(0.16f, 0.19f, 0.26f, 0.92f);
            Button button = buttonObject.AddComponent<Button>();
            button.onClick.AddListener(onClick);

            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(100f, 36f);

            GameObject textObject = new GameObject("Label");
            textObject.transform.SetParent(buttonObject.transform);
            Text text = textObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = 18;
            text.color = presentationConfig != null ? presentationConfig.buttonTextColor : Color.white;
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;

            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
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

        private void AssignPrivateField(Object target, string fieldName, object value)
        {
            System.Reflection.FieldInfo field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (field != null)
            {
                field.SetValue(target, value);
            }
        }
    }
}
