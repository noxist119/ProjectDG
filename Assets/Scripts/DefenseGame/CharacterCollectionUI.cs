using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DefenseGame
{
    public class CharacterCollectionUI : MonoBehaviour
    {
        [SerializeField] private CharacterDatabase characterDatabase;
        [SerializeField] private OutgameProgressionSystem outgameProgression;
        [SerializeField] private int cardsPerPage = 12;

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

        public event System.Action OnClosed;
        public bool IsOpen => root != null && root.activeSelf;

        public void Configure(CharacterDatabase database, OutgameProgressionSystem progression, Font uiFont, Transform canvasRoot, UiSkinResources skin = null)
        {
            if (outgameProgression != null)
            {
                outgameProgression.OnProgressChanged -= HandleProgressChanged;
            }

            characterDatabase = database;
            outgameProgression = progression;
            font = uiFont;
            uiSkin = skin;
            if (outgameProgression != null)
            {
                outgameProgression.OnProgressChanged += HandleProgressChanged;
            }

            if (root != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(root);
                }
                else
                {
                    DestroyImmediate(root);
                }
            }

            Build(canvasRoot);
            ShowPage(0);
            Close();
        }

        private void OnDestroy()
        {
            if (outgameProgression != null)
            {
                outgameProgression.OnProgressChanged -= HandleProgressChanged;
            }
        }

        public void Open()
        {
            if (root == null)
            {
                return;
            }

            root.SetActive(true);
            root.transform.SetAsLastSibling();
            ShowPage(currentPage);
        }

        public void Close()
        {
            if (root != null && root.activeSelf)
            {
                root.SetActive(false);
                OnClosed?.Invoke();
            }
        }

        public void Toggle()
        {
            if (root == null)
            {
                return;
            }

            if (root.activeSelf)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        private void Build(Transform parent)
        {
            root = new GameObject("CharacterCollectionOverlay", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            Image blocker = root.AddComponent<Image>();
            blocker.color = new Color(0.03f, 0.05f, 0.18f, 0.86f);
            blocker.raycastTarget = true;

            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            Image modal = CreatePanel(root.transform, "CollectionModal", new Vector2(0f, 0f), new Vector2(980f, 1480f), new Color(0.25f, 0.34f, 0.70f, 0.98f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), true, true);
            CreatePanel(modal.transform, "Header", new Vector2(0f, -18f), new Vector2(912f, 116f), new Color(0.96f, 0.80f, 0.18f, 0.96f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), true, false);
            CreateText(modal.transform, "Title", "캐릭터 도감", Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -42f), new Vector2(420f, 52f), 38, TextAnchor.MiddleCenter, true);
            collectionCountText = CreateText(modal.transform, "CollectionCount", "등록 캐릭터 0명", new Color(0.18f, 0.22f, 0.34f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -84f), new Vector2(420f, 28f), 18, TextAnchor.MiddleCenter, true);
            CreateButton(modal.transform, "CloseButton", "닫기", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-34f, -30f), new Vector2(102f, 64f), new Color(0.93f, 0.32f, 0.24f, 1f), Close, 22);

            Image gridPanel = CreatePanel(modal.transform, "CardGridPanel", new Vector2(-194f, -160f), new Vector2(556f, 1130f), new Color(0.19f, 0.24f, 0.54f, 0.95f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), true, true);
            CreateText(gridPanel.transform, "GridHeader", "등록된 영웅", Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(220f, 28f), 22, TextAnchor.MiddleCenter, true);
            BuildCardGrid(gridPanel.transform);

            Button prevButton = CreateButton(modal.transform, "PrevPageButton", "<", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-388f, -1320f), new Vector2(90f, 58f), new Color(0.15f, 0.20f, 0.43f, 1f), PreviousPage, 28);
            Button nextButton = CreateButton(modal.transform, "NextPageButton", ">", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-14f, -1320f), new Vector2(90f, 58f), new Color(0.15f, 0.20f, 0.43f, 1f), NextPage, 28);
            pageText = CreateText(modal.transform, "PageText", "1 / 1", Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-200f, -1320f), new Vector2(220f, 46f), 23, TextAnchor.MiddleCenter, true);
            prevButton.gameObject.name = "PrevPageButton";
            nextButton.gameObject.name = "NextPageButton";

            BuildDetailPanel(modal.transform);
        }

        private void BuildCardGrid(Transform parent)
        {
            cardButtons.Clear();
            cardBackgroundImages.Clear();
            cardNameTexts.Clear();
            cardGradeTexts.Clear();
            cardRoleTexts.Clear();
            cardPortraitTexts.Clear();
            cardPortraitImages.Clear();
            cardAccentImages.Clear();

            for (int i = 0; i < cardsPerPage; i++)
            {
                int localIndex = i;
                int column = i % 3;
                int row = i / 3;
                Vector2 position = new Vector2(-168f + column * 168f, -94f - row * 242f);
                Button button = CreateButton(parent, "CharacterCard_" + i, string.Empty, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), position, new Vector2(152f, 210f), new Color(0.95f, 0.96f, 0.98f, 0.98f), () => SelectCard(currentPage * cardsPerPage + localIndex), 24);
                Image background = button.GetComponent<Image>();
                Image accent = CreatePanel(button.transform, "Accent", new Vector2(0f, -8f), new Vector2(132f, 46f), Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), true, false);
                CreatePanel(button.transform, "GradePlate", new Vector2(0f, -10f), new Vector2(120f, 32f), new Color(0.03f, 0.05f, 0.18f, 0.78f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), true, false);
                Image portrait = CreatePanel(button.transform, "Portrait", new Vector2(0f, -54f), new Vector2(126f, 88f), new Color(0.80f, 0.87f, 1f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), true, false);
                Text portraitText = CreateText(portrait.transform, "PortraitLabel", "HG", new Color(0.18f, 0.24f, 0.36f), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, 26, TextAnchor.MiddleCenter, true);
                CreatePanel(button.transform, "InfoBack", new Vector2(0f, 8f), new Vector2(132f, 58f), new Color(0.03f, 0.05f, 0.18f, 0.78f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), true, false);
                Text nameText = CreateText(button.transform, "Name", "Hero", new Color(0.22f, 0.25f, 0.36f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 36f), new Vector2(132f, 26f), 16, TextAnchor.MiddleCenter, true);
                Text roleText = CreateText(button.transform, "Role", "전위", new Color(0.20f, 0.24f, 0.39f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 13f), new Vector2(132f, 22f), 13, TextAnchor.MiddleCenter, false);
                Text gradeText = CreateText(button.transform, "Grade", "일반", Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -11f), new Vector2(118f, 28f), 18, TextAnchor.MiddleCenter, true);
                AddReadableOutline(gradeText);

                cardButtons.Add(button);
                cardBackgroundImages.Add(background);
                cardNameTexts.Add(nameText);
                cardGradeTexts.Add(gradeText);
                cardRoleTexts.Add(roleText);
                cardPortraitTexts.Add(portraitText);
                cardPortraitImages.Add(portrait);
                cardAccentImages.Add(accent);
            }
        }

        private void BuildDetailPanel(Transform parent)
        {
            Image detail = CreatePanel(parent, "DetailPanel", new Vector2(274f, -160f), new Vector2(360f, 1130f), new Color(0.13f, 0.18f, 0.46f, 0.96f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), true, true);
            selectedPortrait = CreatePanel(detail.transform, "SelectedPortrait", new Vector2(0f, -38f), new Vector2(288f, 218f), new Color(0.90f, 0.93f, 1f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), true, false);
            selectedPortraitLabelText = CreateText(selectedPortrait.transform, "PortraitText", "HG", new Color(0.18f, 0.24f, 0.36f), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, 48, TextAnchor.MiddleCenter, true);
            selectedNameText = CreateText(detail.transform, "SelectedName", "Hero", Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -292f), new Vector2(300f, 42f), 30, TextAnchor.MiddleCenter, true);
            selectedGradeBack = CreatePanel(detail.transform, "SelectedGradeBack", new Vector2(0f, -340f), new Vector2(210f, 42f), new Color(0.05f, 0.07f, 0.20f, 0.82f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), true, false);
            selectedGradeText = CreateText(detail.transform, "SelectedGrade", "일반", new Color(1f, 0.92f, 0.48f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -342f), new Vector2(206f, 36f), 25, TextAnchor.MiddleCenter, true);
            AddReadableOutline(selectedGradeText);
            selectedRoleText = CreateText(detail.transform, "SelectedRole", "전위", new Color(0.80f, 0.88f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -386f), new Vector2(300f, 28f), 18, TextAnchor.MiddleCenter, true);
            selectedDescriptionText = CreateText(detail.transform, "SelectedDescription", string.Empty, new Color(0.88f, 0.91f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -430f), new Vector2(308f, 94f), 16, TextAnchor.UpperLeft, false);
            selectedPrefabText = CreateText(detail.transform, "SelectedPrefabText", string.Empty, new Color(0.68f, 0.86f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -526f), new Vector2(308f, 28f), 15, TextAnchor.MiddleLeft, true);

            statAttackText = CreateStatRow(detail.transform, 0, "공격력");
            statHealthText = CreateStatRow(detail.transform, 1, "체력");
            statCritText = CreateStatRow(detail.transform, 2, "치명타");
            statSpeedText = CreateStatRow(detail.transform, 3, "공속");
            statManaText = CreateStatRow(detail.transform, 4, "마나");
            statRangeText = CreateStatRow(detail.transform, 5, "사거리");

            CreateText(detail.transform, "SkillHeader", "스킬 정보", new Color(1f, 0.92f, 0.48f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -820f), new Vector2(308f, 28f), 22, TextAnchor.MiddleLeft, true);
            skillText = CreateText(detail.transform, "SkillText", string.Empty, new Color(0.92f, 0.94f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -868f), new Vector2(308f, 210f), 14, TextAnchor.UpperLeft, false);
        }

        private Text CreateStatRow(Transform parent, int index, string label)
        {
            float y = -568f - index * 42f;
            Image row = CreatePanel(parent, "StatRow_" + label, new Vector2(0f, y), new Vector2(308f, 34f), new Color(0.11f, 0.15f, 0.34f, 0.84f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), true, false);
            CreateText(row.transform, "Label", label, new Color(0.73f, 0.83f, 1f), Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), new Vector2(18f, 0f), new Vector2(132f, 0f), 16, TextAnchor.MiddleLeft, true);
            return CreateText(row.transform, "Value", "0", Color.white, Vector2.zero, Vector2.one, new Vector2(1f, 0.5f), new Vector2(-18f, 0f), new Vector2(144f, 0f), 17, TextAnchor.MiddleRight, true);
        }

        private void ShowPage(int page)
        {
            int characterCount = GetCharacterCount();
            int pageCount = Mathf.Max(1, Mathf.CeilToInt((float)characterCount / cardsPerPage));
            currentPage = Mathf.Clamp(page, 0, pageCount - 1);

            if (pageText != null)
            {
                pageText.text = currentPage + 1 + " / " + pageCount;
            }

            if (collectionCountText != null)
            {
                collectionCountText.text = outgameProgression != null
                    ? outgameProgression.BuildCollectionSummary()
                    : "등록 캐릭터 " + characterCount + "명";
            }

            for (int i = 0; i < cardsPerPage; i++)
            {
                int index = currentPage * cardsPerPage + i;
                CharacterDefinition definition = GetCharacter(index);
                bool hasCharacter = definition != null;

                cardButtons[i].gameObject.SetActive(hasCharacter);
                if (!hasCharacter)
                {
                    continue;
                }

                cardNameTexts[i].text = definition.displayName;
                cardGradeTexts[i].text = GetGradeName(definition.grade);
                int cardLevel = outgameProgression != null ? outgameProgression.GetDisplayCardLevel(definition.id) : 0;
                string availability = cardLevel > 0
                    ? "Lv." + cardLevel
                    : "미획득";
                cardRoleTexts[i].text = availability + " / " + GetRoleName(definition.role);
                cardPortraitTexts[i].text = BuildPortraitLabel(definition.displayName);
                ApplyCharacterPortrait(cardPortraitImages[i], cardPortraitTexts[i], definition);
                cardAccentImages[i].color = GetGradeColor(definition.grade, definition.accentColor);
            }

            if (characterCount > 0)
            {
                int targetIndex = Mathf.Clamp(selectedIndex, currentPage * cardsPerPage, Mathf.Min(characterCount - 1, currentPage * cardsPerPage + cardsPerPage - 1));
                SelectCard(targetIndex);
            }
        }

        private void SelectCard(int index)
        {
            CharacterDefinition definition = GetCharacter(index);
            if (definition == null)
            {
                return;
            }

            selectedIndex = index;
            ApplyCharacterPortrait(selectedPortrait, selectedPortraitLabelText, definition);
            selectedNameText.text = definition.displayName;
            selectedGradeText.text = GetGradeName(definition.grade);
            RuntimeUiSkinUtility.ApplyReadableTextColor(selectedGradeText, GetGradeColor(definition.grade, definition.accentColor), uiSkin);
            if (selectedGradeBack != null)
            {
                selectedGradeBack.color = Color.Lerp(GetGradeColor(definition.grade, definition.accentColor), new Color(0.03f, 0.05f, 0.18f, 1f), 0.34f);
            }
            selectedRoleText.text = GetRoleName(definition.role);
            selectedDescriptionText.text = definition.description;
            selectedPrefabText.text = outgameProgression != null
                ? outgameProgression.BuildProgressText(definition.id)
                : definition.prefab != null
                    ? "연결 프리팹: " + definition.prefab.name
                    : "연결 프리팹: 기본 템플릿 사용";

            CombatStats stats = definition.stats;
            int growthLevel = outgameProgression != null ? Mathf.Max(0, outgameProgression.GetCardLevel(definition.id) - 1) : 0;
            float attackMultiplier = outgameProgression != null ? 1f + growthLevel * outgameProgression.Settings.attackPowerPerGrowthLevel : 1f;
            float healthMultiplier = outgameProgression != null ? 1f + growthLevel * outgameProgression.Settings.maxHealthPerGrowthLevel : 1f;
            statAttackText.text = Mathf.RoundToInt(stats.attackPower * attackMultiplier).ToString();
            statHealthText.text = Mathf.RoundToInt(stats.maxHealth * healthMultiplier).ToString();
            statCritText.text = Mathf.RoundToInt(stats.criticalChance * 100f) + "%";
            statSpeedText.text = stats.attackSpeed.ToString("0.00");
            statManaText.text = Mathf.RoundToInt(stats.maxMana).ToString();
            statRangeText.text = stats.attackRange.ToString("0.0");
            skillText.text = BuildSkillSummary(definition);

            UpdateCardSelection();
        }

        private void HandleProgressChanged()
        {
            ShowPage(currentPage);
        }

        private void UpdateCardSelection()
        {
            for (int i = 0; i < cardButtons.Count; i++)
            {
                int cardIndex = currentPage * cardsPerPage + i;
                CharacterDefinition definition = GetCharacter(cardIndex);
                if (definition == null)
                {
                    continue;
                }

                bool isSelected = cardIndex == selectedIndex;
                if (cardBackgroundImages[i] != null)
                {
                    cardBackgroundImages[i].color = isSelected
                        ? Color.Lerp(definition.accentColor, Color.white, 0.58f)
                        : new Color(0.95f, 0.96f, 0.98f, 0.98f);
                }

                RectTransform rect = cardButtons[i].GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.localScale = isSelected ? Vector3.one * 1.04f : Vector3.one;
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
            return characterDatabase != null && characterDatabase.Characters != null ? characterDatabase.Characters.Count : 0;
        }

        private CharacterDefinition GetCharacter(int index)
        {
            if (characterDatabase == null || characterDatabase.Characters == null || index < 0 || index >= characterDatabase.Characters.Count)
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

            string result = string.Empty;
            for (int i = 0; i < definition.skills.Count; i++)
            {
                SkillDefinition skill = definition.skills[i];
                if (i > 0)
                {
                    result += "\n";
                }

                string category = SkillDefinitionUtility.GetCategoryDisplayName(skill.ResolvedCategory);
                string description = SkillDefinitionUtility.BuildDisplayDescription(skill);
                string growth = SkillDefinitionUtility.BuildGrowthDisplayText(skill);

                result += "• " + skill.displayName + " [" + category + "]\n  " + description;
                if (!string.IsNullOrWhiteSpace(growth))
                {
                    result += "\n  " + growth;
                }
            }

            return result;
        }

        private void ApplyCharacterPortrait(Image portraitImage, Text fallbackText, CharacterDefinition definition)
        {
            if (portraitImage == null)
            {
                return;
            }

            Sprite portraitSprite = RollRollUiResource.ResolveCharacterSprite(definition);
            if (portraitSprite != null)
            {
                portraitImage.sprite = portraitSprite;
                portraitImage.type = Image.Type.Simple;
                portraitImage.preserveAspect = true;
                portraitImage.color = Color.white;
                if (fallbackText != null)
                {
                    fallbackText.gameObject.SetActive(false);
                }

                return;
            }

            portraitImage.sprite = null;
            portraitImage.color = definition != null
                ? Color.Lerp(definition.accentColor, Color.white, 0.35f)
                : new Color(0.80f, 0.87f, 1f, 1f);
            if (fallbackText != null)
            {
                fallbackText.gameObject.SetActive(true);
                fallbackText.text = definition != null ? BuildPortraitLabel(definition.displayName) : "??";
            }
        }

        private string BuildPortraitLabel(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                return "??";
            }

            string trimmed = displayName.Trim();
            if (trimmed.Length <= 2)
            {
                return trimmed.ToUpperInvariant();
            }

            string[] tokens = trimmed.Split(' ');
            if (tokens.Length >= 2 && !string.IsNullOrWhiteSpace(tokens[0]) && !string.IsNullOrWhiteSpace(tokens[1]))
            {
                return (tokens[0][0].ToString() + tokens[1][0].ToString()).ToUpperInvariant();
            }

            return trimmed.Substring(0, 2).ToUpperInvariant();
        }

        private string GetGradeName(CharacterGrade grade)
        {
            return CharacterGradeUtility.GetDisplayName(grade);
        }

        private string GetRoleName(CharacterRole role)
        {
            if (role == CharacterRole.Vanguard) return "전위";
            if (role == CharacterRole.Ranger) return "사수";
            if (role == CharacterRole.Mage) return "마법";
            if (role == CharacterRole.Support) return "지원";
            if (role == CharacterRole.Assassin) return "암살";
            return "소환";
        }

        private Color GetGradeColor(CharacterGrade grade, Color fallback)
        {
            return CharacterGradeUtility.GetColor(grade, fallback);
        }

        private void AddReadableOutline(Text text)
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

            outline.effectColor = new Color(0f, 0f, 0f, 0.78f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
        }

        private Image CreatePanel(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, bool rounded, bool shadow)
        {
            GameObject panelObject = new GameObject(name, typeof(RectTransform));
            panelObject.transform.SetParent(parent, false);
            Image image = panelObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            RuntimeUiSkinUtility.ApplyImageSkin(image, uiSkin, name, false, rounded);
            RollRollUiResource.TryApplyElementSprite(image, name, false, rounded);

            RectTransform rect = image.rectTransform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            if (shadow)
            {
                Shadow shadowComponent = panelObject.AddComponent<Shadow>();
                shadowComponent.effectColor = new Color(0f, 0f, 0f, 0.34f);
                shadowComponent.effectDistance = new Vector2(0f, -7f);
            }

            return image;
        }

        private Button CreateButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 size, Color backgroundColor, UnityEngine.Events.UnityAction onClick, int fontSize)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform));
            buttonObject.transform.SetParent(parent, false);
            Image image = buttonObject.AddComponent<Image>();
            image.color = backgroundColor;
            RuntimeUiSkinUtility.ApplyImageSkin(image, uiSkin, name, true, true);
            RollRollUiResource.TryApplyElementSprite(image, name, true, true);
            image.raycastTarget = true;

            Shadow shadow = buttonObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.32f);
            shadow.effectDistance = new Vector2(0f, -6f);

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

            if (!string.IsNullOrEmpty(label))
            {
                CreateText(buttonObject.transform, "Label", label, Color.white, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, fontSize, TextAnchor.MiddleCenter, true);
            }

            return button;
        }

        private Text CreateText(Transform parent, string name, string value, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 size, int fontSize, TextAnchor alignment, bool bold)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.AddComponent<Text>();
            text.font = font;
            text.text = RuntimeKoreanTextUtility.Clean(name, value);
            text.color = RuntimeUiSkinUtility.ResolveReadableTextColor(parent, color, uiSkin);
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            text.raycastTarget = false;

            RectTransform rect = text.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Shadow shadow = textObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.36f);
            shadow.effectDistance = new Vector2(2f, -2f);
            return text;
        }

        private Sprite GetRoundedSprite()
        {
            if (roundedSprite != null)
            {
                return roundedSprite;
            }

            int size = 64;
            float radius = 18f;
            Texture2D texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nearestX = Mathf.Clamp(x, radius, size - radius - 1f);
                    float nearestY = Mathf.Clamp(y, radius, size - radius - 1f);
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(nearestX, nearestY));
                    float alpha = Mathf.Clamp01(radius + 0.5f - distance);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            roundedSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
            return roundedSprite;
        }
    }
}
