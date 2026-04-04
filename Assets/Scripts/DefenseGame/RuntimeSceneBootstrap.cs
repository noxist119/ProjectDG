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

            Transform root = EnsureRoot("RuntimeStageRoot");
            Transform boardRoot = EnsureChild(root, "BoardSlots");
            Transform spawnRoot = EnsureChild(root, "SpawnPoints");
            Transform templateRoot = EnsureChild(root, "Templates");
            Transform miscRoot = EnsureChild(root, "Misc");

            ClearChildren(boardRoot);
            ClearChildren(spawnRoot);
            ClearChildren(templateRoot);
            ClearChildren(miscRoot);

            EnsureGround(root);
            EnsureCamera();
            EnsureLight();

            List<BoardSlot> slots = BuildSlots(boardRoot);
            Transform[] spawnPoints = BuildSpawnPoints(spawnRoot);
            Transform goalPoint = BuildGoal(miscRoot);

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
            Transform existing = root.Find("Ground");
            if (existing != null)
            {
                SafeDestroy(existing.gameObject);
            }

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(root);
            ground.transform.position = new Vector3(0f, -0.5f, 0f);
            ground.transform.localScale = new Vector3(2f, 1f, 1.8f);
            Renderer renderer = ground.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(0.14f, 0.17f, 0.2f);
            }
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
            camera.backgroundColor = new Color(0.07f, 0.08f, 0.11f);
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
            light.intensity = 1.15f;
        }

        private List<BoardSlot> BuildSlots(Transform boardRoot)
        {
            List<BoardSlot> slots = new List<BoardSlot>();
            float width = (slotCount - 1) * slotSpacing;

            for (int i = 0; i < slotCount; i++)
            {
                GameObject slotObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                slotObject.name = "Slot_" + i.ToString("D2");
                slotObject.transform.SetParent(boardRoot);
                slotObject.transform.position = boardCenter + new Vector3(-width * 0.5f + i * slotSpacing, 0f, 0f);
                slotObject.transform.localScale = new Vector3(1.2f, 0.2f, 1.2f);
                slotObject.GetComponent<Renderer>().material.color = new Color(0.22f, 0.24f, 0.3f);

                BoardSlot slot = slotObject.AddComponent<BoardSlot>();
                GameObject anchor = new GameObject("Anchor");
                anchor.transform.SetParent(slotObject.transform);
                anchor.transform.localPosition = new Vector3(0f, 0.8f, 0f);
                AssignPrivateField(slot, "unitAnchor", anchor.transform);
                slots.Add(slot);
            }

            return slots;
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

        private Transform BuildGoal(Transform miscRoot)
        {
            GameObject goal = new GameObject("GoalPoint");
            goal.transform.SetParent(miscRoot);
            goal.transform.position = new Vector3(0f, 0f, -8.5f);
            return goal.transform;
        }

        private Projectile BuildProjectileTemplate(Transform templateRoot)
        {
            GameObject projectileObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectileObject.name = "ProjectileTemplate";
            projectileObject.transform.SetParent(templateRoot);
            projectileObject.transform.localScale = Vector3.one * 0.25f;
            projectileObject.GetComponent<Renderer>().material.color = new Color(1f, 0.85f, 0.3f);
            Projectile projectile = projectileObject.AddComponent<Projectile>();
            projectileObject.SetActive(false);
            return projectile;
        }

        private DefenderUnit BuildDefenderTemplate(Transform templateRoot, Projectile projectileTemplate)
        {
            GameObject unitObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            unitObject.name = "DefenderTemplate";
            unitObject.transform.SetParent(templateRoot);
            unitObject.transform.localScale = new Vector3(0.8f, 1f, 0.8f);
            DefenderUnit unit = unitObject.AddComponent<DefenderUnit>();
            GameObject firePoint = new GameObject("FirePoint");
            firePoint.transform.SetParent(unitObject.transform);
            firePoint.transform.localPosition = new Vector3(0f, 0.8f, 0.6f);
            unit.ConfigureRuntimePieces(projectileTemplate, firePoint.transform, unitObject.GetComponentsInChildren<Renderer>(true));
            unitObject.SetActive(false);
            return unit;
        }

        private MonsterUnit BuildMonsterTemplate(Transform templateRoot)
        {
            GameObject monsterObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            monsterObject.name = "MonsterTemplate";
            monsterObject.transform.SetParent(templateRoot);
            monsterObject.transform.localScale = new Vector3(1f, 1f, 1f);
            MonsterUnit monster = monsterObject.AddComponent<MonsterUnit>();
            monsterObject.SetActive(false);
            return monster;
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

            Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            Text gold = CreateText(canvas.transform, font, new Vector2(120f, -30f), "Gold : 0");
            Text life = CreateText(canvas.transform, font, new Vector2(120f, -60f), "Life : 0");
            Text round = CreateText(canvas.transform, font, new Vector2(120f, -90f), "Round : 0");
            Text board = CreateText(canvas.transform, font, new Vector2(120f, -120f), "Units : 0");
            Text content = CreateText(canvas.transform, font, new Vector2(180f, -150f), "Characters : 0 / Monsters : 0");
            Text hint = CreateText(canvas.transform, font, new Vector2(310f, -30f), "Space Round | S Summon | 1-4 Merge | C Add Heroes | M Add Monsters");
            hint.alignment = TextAnchor.MiddleLeft;
            RectTransform hintRect = hint.GetComponent<RectTransform>();
            hintRect.sizeDelta = new Vector2(620f, 30f);

            CreateButton(canvas.transform, font, "Start", new Vector2(90f, 60f), binder.OnClickStartRound);
            CreateButton(canvas.transform, font, "Summon", new Vector2(210f, 60f), binder.OnClickSummon);
            CreateButton(canvas.transform, font, "Merge N", new Vector2(330f, 60f), binder.OnClickMergeNormal);
            CreateButton(canvas.transform, font, "Merge R", new Vector2(450f, 60f), binder.OnClickMergeRare);
            CreateButton(canvas.transform, font, "Merge E", new Vector2(570f, 60f), binder.OnClickMergeEpic);
            CreateButton(canvas.transform, font, "Merge L", new Vector2(690f, 60f), binder.OnClickMergeLegendary);

            hud.Configure(gameController, gold, life, round, board, content, hint);
        }

        private Text CreateText(Transform parent, Font font, Vector2 anchoredPosition, string value)
        {
            GameObject textObject = new GameObject(value.Replace(" ", string.Empty) + "Text");
            textObject.transform.SetParent(parent);
            Text text = textObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = 20;
            text.color = Color.white;
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

        private void CreateButton(Transform parent, Font font, string label, Vector2 anchoredPosition, UnityEngine.Events.UnityAction onClick)
        {
            GameObject buttonObject = new GameObject(label + "Button");
            buttonObject.transform.SetParent(parent);
            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.16f, 0.19f, 0.26f, 0.92f);
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
            text.color = Color.white;
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;

            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
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
