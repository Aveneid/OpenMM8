﻿using Assets.OpenMM8.Scripts.Data;
using Assets.OpenMM8.Scripts.Gameplay.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Assets.OpenMM8.Scripts.Gameplay
{
    public partial class UiMgr : Singleton<UiMgr>
    {
        public const int UI_WIDTH = 640;
        public const int UI_HEIGHT = 480;

        public RawImage SceneVideoImage;

        // Public
        [Header("UI - Canvases")]
        private Canvas m_PartyCanvas;
        private Canvas m_PartyBuffsAndButtonsCanvas;
        private Canvas m_PartyInventoryCanvas;
        private Canvas m_HouseCanvas;

        // Private
        private GameObject m_PartyCanvasObj;
        private GameObject m_NpcInfoCanvasObj;
        private PlayerParty m_PlayerParty;
        private float m_TimeSinceLastPartyText = 0.0f;
        private float m_PartyTextLockTime = 0.0f;

        public Item m_HoveredItem = null;
        public InventoryItem m_HeldItem = null;
        public InspectableUiText m_HoveredInspectableUiText = null;

        // State
        private UIState m_CurrUIState = null;

        // TODO: UI states should be on stack 
        private ConsoleUIState m_ConsoleUIState = null;

        [Header("UI")]
        private InspectNpcUI m_InspectNpcUI;
        private InspectItemUI m_InspectItemUI;
        private InspectUiTextUI m_InspectUiTextUI;
        private PartyUI m_PartyUI;
        private NpcTalkUI m_NpcTalkUI;
        private Minimap m_Minimap;
        private Image m_MinimapCloseButtonImage;
        private MapQuestNotesUI m_MapQuestNotesUI;
        private List<Image> m_EmptySlotBanners = new List<Image>();
        private CharDetailUI m_CharDetailUI;
        private SpellbookUI m_SpellbookUI;

        [Header("UI - Map, Quest, Notes, History")]
        int placeholder;

        private Dictionary<int, Sprite> m_NpcAvatarsMap = 
            new Dictionary<int, Sprite>();

        // Character type (e.g. Knight_1) -> class holding facial sprites
        private Dictionary<int, CharacterSprites> m_CharacterSpritesMap =
            new Dictionary<int, CharacterSprites>();

        // Item name (e.g. item001) -> Sprite
        private Dictionary<string, Sprite> m_InventoryItemSpriteMap =
            new Dictionary<string, Sprite>();

        // Item name (e.g. item001) -> Sprite
        private Dictionary<string, Sprite> m_EquippableItemSpriteMap =
            new Dictionary<string, Sprite>();

        // Object (item) name -> Sprite
        private Dictionary<string, Sprite> m_OutdoorItemSpriteMap =
            new Dictionary<string, Sprite>();

        // Sprite name -> Sprite
        private Dictionary<string, Sprite> m_DollSpriteMap =
            new Dictionary<string, Sprite>();

        // TODO: Just add all sprites to this dictionary...
        public Dictionary<string, Sprite> m_SpriteMap = new Dictionary<string, Sprite>();


        [Header("Sprites")]
        private Sprite[] m_QuestEffectSprites;

        //=================================== Unity Lifecycle ===================================

        private void Awake()
        {
            GameEvents.OnPauseGame += OnPauseGame;

            GameEvents.OnCharacterJoinedParty += OnCharacterJoinedParty;
            GameEvents.OnCharacterLeftParty += OnCharacterLeftParty;
            GameEvents.OnGoldChanged += OnGoldChanged;
            GameEvents.OnFoodChanged += OnFoodChanged;
            GameEvents.OnFoundGold += OnFoundGold;
            GameEvents.OnHoverObject += OnHoverObject;

            GameEvents.OnCharHealthChanged += OnCharHealthChanged;
            GameEvents.OnCharManaChanged += OnCharManaChanged;
            GameEvents.OnRecovered += OnCharRecovered;
            GameEvents.OnRecoveryTimeChanged += OnCharRecoveryTimeChanged;
            GameEvents.OnCharHitNpc += OnCharHitNpc;
            GameEvents.OnCharGotHit += OnCharGotHit;
            GameEvents.OnCharAttack += OnCharAttack;
            GameEvents.OnItemEquipped += OnItemEquipped;

            GameEvents.OnNpcInspectStart += OnNpcInspectStart;
            GameEvents.OnNpcInspectEnd += OnNpcInspectEnd;

            GameEvents.OnMinimapMarkerCreated += OnMinimapMarkerCreated;
            GameEvents.OnMinimapMarkerDestroyed += OnMinimapMarkerDestroyed;

            GameEvents.OnTalkSceneStart += OnTalkSceneStart;


            GameEvents.OnCharacterFinishedEvent += OnCharacterFinishedEvent;

            GameEvents.OnQuestBitAdded += OnQuestBitAdded;

            GameEvents.OnInventoryItemHoverStart += OnInventoryItemHoverStart;
            GameEvents.OnInventoryItemHoverEnd += OnInventoryItemHoverEnd;
            GameEvents.OnInventoryItemClicked += OnInventoryItemClicked;

            GameEvents.OnOutdoorItemInspectStart += OnOutdoorItemInspectStart;
            GameEvents.OnOutdoorItemInspectEnd += OnOutdoorItemInspectEnd;

            GameEvents.OnInventoryCellClicked += OnInventoryCellClicked;
            GameEvents.OnDollClicked += OnDollClicked;

            GameEvents.OnCharacterAvatarClicked += OnCharacterAvatarClicked;

            GameEvents.OnInspectableUiTextHoverStart += OnInspectableUiTextHoverStart;
            GameEvents.OnInspectableUiTextHoverEnd += OnInspectableUiTextHoverEnd;
        }

        // Init sequence: DbMgr(1) -> GameMgr(1) -> UiMgr(1) -> GameMgr(2)
        public bool Init()
        {
            m_PartyCanvasObj = GameObject.Find("PartyCanvas");
            if (m_PartyCanvasObj != null)
            {
                m_PartyCanvas = m_PartyCanvasObj.GetComponent<Canvas>();
                m_PartyBuffsAndButtonsCanvas = m_PartyCanvasObj.transform.Find("BuffsAndButtonsCanvas").GetComponent<Canvas>();
                m_Minimap = m_PartyCanvasObj.transform.Find("BuffsAndButtonsCanvas").Find("Minimap").GetComponent<Minimap>();
            }
            else
            {
                Debug.LogError("Could not find PartyCanvas gameobject !");
            }

            m_MinimapCloseButtonImage = m_PartyCanvasObj.transform.Find("MinimapCloseButton").GetComponent<Image>();

            m_MapQuestNotesUI = new MapQuestNotesUI();
            GameObject mapQuestNotesObject = m_PartyCanvasObj.transform.Find("MapQuestNotesCanvas").gameObject;
            m_MapQuestNotesUI.Canvas = mapQuestNotesObject.GetComponent<Canvas>();
            m_MapQuestNotesUI.MapNameText = mapQuestNotesObject.transform.Find("MapCanvas").transform.Find("MapNameText").GetComponent<Text>();

            m_NpcTalkUI = new NpcTalkUI();
            GameObject npcTalkCanvasObject = m_PartyCanvasObj.transform.Find("NpcTalkCanvas").gameObject;
            m_NpcTalkUI.NpcTalkCanvas = npcTalkCanvasObject.GetComponent<Canvas>();
            m_NpcTalkUI.NpcTalkObj = npcTalkCanvasObject.transform.Find("NpcResponseBackground").gameObject;
            m_NpcTalkUI.NpcTalkBackgroundImg = npcTalkCanvasObject.transform.Find("NpcResponseBackground").GetComponent<Image>();
            m_NpcTalkUI.NpcResponseText = npcTalkCanvasObject.transform.Find("NpcResponseBackground").Find("NpcResponseText").GetComponent<Text>();
            m_NpcTalkUI.LocationNameText = npcTalkCanvasObject.transform.Find("LocationNameText").GetComponent<Text>();

            m_NpcTalkUI.TalkAvatar = new TalkAvatarUI();
            m_NpcTalkUI.TalkAvatar.Holder = npcTalkCanvasObject.transform.Find("Avatar").gameObject;
            m_NpcTalkUI.TalkAvatar.Avatar = m_NpcTalkUI.TalkAvatar.Holder.transform.Find("AvatarImage").GetComponent<Image>();
            m_NpcTalkUI.TalkAvatar.NpcNameText = m_NpcTalkUI.TalkAvatar.Holder.transform.Find("NpcNameText").GetComponent<Text>();

            //m_SceneVideoPlayer = npcTalkCanvasObject.transform.Find("VideoPlayer").GetComponent<Video>();
            SceneVideoImage = npcTalkCanvasObject.transform.Find("SceneVideoImage").GetComponent<RawImage>();

            m_NpcTalkUI.TopicButtonHolder = npcTalkCanvasObject.transform.Find("TopicsHolder").GetComponent<RectTransform>();
            m_NpcTalkUI.TopicButtonList.Add(m_NpcTalkUI.TopicButtonHolder.Find("Button_1").gameObject);
            m_NpcTalkUI.TopicButtonList.Add(m_NpcTalkUI.TopicButtonHolder.Find("Button_2").gameObject);
            m_NpcTalkUI.TopicButtonList.Add(m_NpcTalkUI.TopicButtonHolder.Find("Button_3").gameObject);
            m_NpcTalkUI.TopicButtonList.Add(m_NpcTalkUI.TopicButtonHolder.Find("Button_4").gameObject);
            m_NpcTalkUI.TopicButtonList.Add(m_NpcTalkUI.TopicButtonHolder.Find("Button_5").gameObject);
            m_NpcTalkUI.TopicButtonList.Add(m_NpcTalkUI.TopicButtonHolder.Find("Button_6").gameObject);

            foreach (GameObject topicBtn in m_NpcTalkUI.TopicButtonList)
            {
                topicBtn.GetComponent<Button>().onClick.AddListener(
                    delegate { TalkEventMgr.Instance.OnTopicClicked(topicBtn.GetComponent<TopicBtnContext>()); });
            }

            m_NpcTalkUI.AvatarBtnHolder = npcTalkCanvasObject.transform.Find("AvatarButtonsHolder").GetComponent<RectTransform>();
            m_NpcTalkUI.AvatarBtnList.Add(m_NpcTalkUI.AvatarBtnHolder.Find("AvatarButton_1").GetComponent<AvatarBtnContext>());
            m_NpcTalkUI.AvatarBtnList.Add(m_NpcTalkUI.AvatarBtnHolder.Find("AvatarButton_2").GetComponent<AvatarBtnContext>());
            m_NpcTalkUI.AvatarBtnList.Add(m_NpcTalkUI.AvatarBtnHolder.Find("AvatarButton_3").GetComponent<AvatarBtnContext>());

            if (m_NpcTalkUI.AvatarBtnHolder == null) { Debug.Log("null av btn holder"); }
            foreach (AvatarBtnContext avatarCtx in m_NpcTalkUI.AvatarBtnList)
            {
                avatarCtx.AvatarButton.onClick.AddListener(
                    delegate { TalkEventMgr.Instance.OnAvatarClicked(avatarCtx); });
            }

            m_PartyUI = new PartyUI();
            m_PartyUI.GoldText = m_PartyCanvasObj.transform.Find("GoldCountText").GetComponent<Text>();
            m_PartyUI.FoodText = m_PartyCanvasObj.transform.Find("FoodCountText").GetComponent<Text>();
            m_PartyUI.HoverInfoText = m_PartyCanvasObj.transform.Find("BaseBarImage").transform.Find("HoverInfoText").GetComponent<Text>();

            // ------------ InspectNpcUI --------------

            GameObject objectInfoCanvasObject = GameObject.Find("NpcInfoCanvas");
            Debug.Assert(objectInfoCanvasObject != null);
            Transform npcInfoBackgroundObject = objectInfoCanvasObject.transform.Find("Background");

            m_InspectNpcUI = new InspectNpcUI();
            m_InspectNpcUI.Canvas = objectInfoCanvasObject.GetComponent<Canvas>();

            m_InspectNpcUI.Healthbar_Background = npcInfoBackgroundObject.transform.Find("Healthbar_Background").GetComponent<Image>();
            m_InspectNpcUI.Healthbar = npcInfoBackgroundObject.transform.Find("Healthbar").GetComponent<Image>();
            m_InspectNpcUI.Healthbar_CapLeft = npcInfoBackgroundObject.transform.Find("Healthbar_CapLeft").GetComponent<Image>();
            m_InspectNpcUI.Healthbar_CapRight = npcInfoBackgroundObject.transform.Find("Healthbar_CapRight").GetComponent<Image>();

            m_InspectNpcUI.NpcNameText = npcInfoBackgroundObject.transform.Find("NpcNameText").GetComponent<Text>();
            m_InspectNpcUI.HitPointsText = npcInfoBackgroundObject.transform.Find("HitPointsText").GetComponent<Text>();
            m_InspectNpcUI.ArmorClassText = npcInfoBackgroundObject.transform.Find("ArmorClassText").GetComponent<Text>();
            m_InspectNpcUI.AttackText = npcInfoBackgroundObject.transform.Find("AttackText").GetComponent<Text>();
            m_InspectNpcUI.DamageText = npcInfoBackgroundObject.transform.Find("DamageText").GetComponent<Text>();
            m_InspectNpcUI.SpellText = npcInfoBackgroundObject.transform.Find("SpellText").GetComponent<Text>();
            m_InspectNpcUI.FireResistanceText = npcInfoBackgroundObject.transform.Find("FireResistanceText").GetComponent<Text>();
            m_InspectNpcUI.AirResistanceText = npcInfoBackgroundObject.transform.Find("AirResistanceText").GetComponent<Text>();
            m_InspectNpcUI.WaterResistanceText = npcInfoBackgroundObject.transform.Find("WaterResistanceText").GetComponent<Text>();
            m_InspectNpcUI.EarthResistanceText = npcInfoBackgroundObject.transform.Find("EarthResistanceText").GetComponent<Text>();
            m_InspectNpcUI.MindResistanceText = npcInfoBackgroundObject.transform.Find("MindResistanceText").GetComponent<Text>();
            m_InspectNpcUI.SpiritResistanceText = npcInfoBackgroundObject.transform.Find("SpiritResistanceText").GetComponent<Text>();
            m_InspectNpcUI.BodyResistanceText = npcInfoBackgroundObject.transform.Find("BodyResistanceText").GetComponent<Text>();
            m_InspectNpcUI.LightResistanceText = npcInfoBackgroundObject.transform.Find("LightResistanceText").GetComponent<Text>();
            m_InspectNpcUI.DarkResistanceText = npcInfoBackgroundObject.transform.Find("DarkResistanceText").GetComponent<Text>();
            m_InspectNpcUI.PhysicalResistanceText = npcInfoBackgroundObject.transform.Find("PhysicalResistanceText").GetComponent<Text>();

            // ------------ InspectItemUI --------------

            m_InspectItemUI = new InspectItemUI();
            m_InspectItemUI.Holder = OpenMM8Util.GetGameObjAtScenePath("ItemInfoCanvas");
            m_InspectItemUI.BackgroundTransfrom = OpenMM8Util.GetComponentAtScenePath<RectTransform>("Background", m_InspectItemUI.Holder);

            GameObject btmAnchor = OpenMM8Util.GetGameObjAtScenePath("Background/BottomAnchor", m_InspectItemUI.Holder);
            m_InspectItemUI.Value = OpenMM8Util.GetComponentAtScenePath<Text>("ValueText", btmAnchor);

            GameObject topAnchor = OpenMM8Util.GetGameObjAtScenePath("Background/TopAnchor", m_InspectItemUI.Holder);
            m_InspectItemUI.ItemName = OpenMM8Util.GetComponentAtScenePath<Text>("ItemNameText", topAnchor);
            m_InspectItemUI.ItemSpecific = OpenMM8Util.GetComponentAtScenePath<Text>("TypeText", topAnchor);
            m_InspectItemUI.Description = OpenMM8Util.GetComponentAtScenePath<Text>("TypeText/DescriptionText", topAnchor);
            m_InspectItemUI.ItemImage = OpenMM8Util.GetComponentAtScenePath<Image>("ItemImage", topAnchor);
            m_InspectItemUI.LeftEdge = OpenMM8Util.GetComponentAtScenePath<Image>("Edge_Left", topAnchor);
            m_InspectItemUI.RightEdge = OpenMM8Util.GetComponentAtScenePath<Image>("Edge_Right", topAnchor);


            m_InspectUiTextUI = new InspectUiTextUI();
            m_InspectUiTextUI.Holder = OpenMM8Util.GetGameObjAtScenePath("UiTextInfoCanvas");
            m_InspectUiTextUI.BackgroundTransfrom = OpenMM8Util.GetComponentAtScenePath<RectTransform>("Background", m_InspectUiTextUI.Holder);

            topAnchor = OpenMM8Util.GetGameObjAtScenePath("Background/TopAnchor", m_InspectUiTextUI.Holder);
            m_InspectUiTextUI.NameText = OpenMM8Util.GetComponentAtScenePath<Text>("NameText", topAnchor);
            m_InspectUiTextUI.InfoText = OpenMM8Util.GetComponentAtScenePath<Text>("InfoText", topAnchor);
            m_InspectUiTextUI.LeftEdge = OpenMM8Util.GetComponentAtScenePath<Image>("Edge_Left", topAnchor);
            m_InspectUiTextUI.RightEdge = OpenMM8Util.GetComponentAtScenePath<Image>("Edge_Right", topAnchor);


            m_EmptySlotBanners.Add(m_PartyCanvasObj.transform.Find("PC1_EmptySlot").GetComponent<Image>());
            m_EmptySlotBanners.Add(m_PartyCanvasObj.transform.Find("PC2_EmptySlot").GetComponent<Image>());
            m_EmptySlotBanners.Add(m_PartyCanvasObj.transform.Find("PC3_EmptySlot").GetComponent<Image>());
            m_EmptySlotBanners.Add(m_PartyCanvasObj.transform.Find("PC4_EmptySlot").GetComponent<Image>());
            m_EmptySlotBanners.Add(m_PartyCanvasObj.transform.Find("PC5_EmptySlot").GetComponent<Image>());

            m_InspectNpcUI.PreviewImage = npcInfoBackgroundObject.transform.Find("PreviewImageMask").transform.Find("PreviewImage").GetComponent<Image>();

            Debug.Log("Load");
            m_CharDetailUI = CharDetailUI.Load();

            m_PartyCanvasObj = GameObject.Find("PartyCanvas");
            m_NpcInfoCanvasObj = GameObject.Find("NpcInfoCanvas");
            m_PlayerParty = GameMgr.Instance.PlayerParty;

            // ------ Load state-changing sprites ------ 
            Dictionary<string, Sprite> agroStSpriteMap = new Dictionary<string, Sprite>();
            string path = "UI/Party/BarsAgroStatus";
            Sprite[] agroStSprites = Resources.LoadAll<Sprite>(path);
            foreach (Sprite sprite in agroStSprites)
            {
                agroStSpriteMap[sprite.name] = sprite;
            }

            CharacterUI.HealthBarSprite_Green = agroStSpriteMap["ManaG"];
            CharacterUI.HealthBarSprite_Yellow = agroStSpriteMap["manaY"];
            CharacterUI.HealthBarSprite_Red = agroStSpriteMap["manar"];
            CharacterUI.AgroStatusSprite_Green = agroStSpriteMap["statG"];
            CharacterUI.AgroStatusSprite_Yellow = agroStSpriteMap["statY"];
            CharacterUI.AgroStatusSprite_Red = agroStSpriteMap["statR"];
            CharacterUI.AgroStatusSprite_Gray = agroStSpriteMap["statBL"];

            InspectNpcUI.HealthbarSprite_Green = agroStSpriteMap["MHP_GRN"];
            InspectNpcUI.HealthbarSprite_Yellow = agroStSpriteMap["MHP_YEL"];
            InspectNpcUI.HealthbarSprite_Red = agroStSpriteMap["MHP_RED"];

            // Load Npc Avatars
            Sprite[] npcAvatarSprites = Resources.LoadAll<Sprite>("UI/NPC/NpcAvatars");
            foreach (Sprite npcSprite in npcAvatarSprites)
            {
                // Get ID from name
                string idStr = npcSprite.name.Substring(3);
                int spriteId = int.Parse(idStr);
                m_NpcAvatarsMap[spriteId] = npcSprite;
            }

            m_QuestEffectSprites = Resources.LoadAll<Sprite>("UI/EffectFaceSprites/FaceEffect_Quest");

            Sprite[] invItemSprites = Resources.LoadAll<Sprite>("UI/Items/ITEMS");
            foreach (Sprite sprite in invItemSprites)
            {
                m_InventoryItemSpriteMap[sprite.name.ToLower()] = sprite;
            }

            Sprite[] invEqItemSprites = Resources.LoadAll<Sprite>("UI/Items/ARMOR_EQ_ITEMS");
            foreach (Sprite sprite in invEqItemSprites)
            {
                //Debug.Log(sprite.name.ToLower());
                m_EquippableItemSpriteMap[sprite.name.ToLower()] = sprite;
            }

            Sprite[] outdoorItemSprites = Resources.LoadAll<Sprite>("UI/Items/OUTDOOR_ITEMS");
            foreach (Sprite sprite in outdoorItemSprites)
            {
                //Debug.Log(sprite.name.ToLower());
                m_OutdoorItemSpriteMap[sprite.name.ToLower()] = sprite;
            }

            // Assign sprites to all items
            foreach (var spritePair in DbMgr.Instance.ItemDb.Data)
            {
                ItemData itemData = spritePair.Value;
                if (m_InventoryItemSpriteMap.ContainsKey(itemData.ImageName))
                {
                    itemData.InvSprite = m_InventoryItemSpriteMap[itemData.ImageName];

                    // Calculate how many inventory cells it occupies
                    int width = (int)itemData.InvSprite.rect.width;
                    int height = (int)itemData.InvSprite.rect.height;

                    itemData.InvSize.x = width / 32;
                    itemData.InvSize.y = height / 32;
                    if ((width % 32) >= 16)
                    {
                        itemData.InvSize.x++;
                    }
                    if ((height % 32) >= 16)
                    {
                        itemData.InvSize.y++;
                    }

                    if (itemData.InvSize.x == 0)
                    {
                        itemData.InvSize.x = 1;
                    }
                    if (itemData.InvSize.y == 0)
                    {
                        itemData.InvSize.y = 1;
                    }

                    //Debug.Log(itemData.Id + ": " + itemData.Name + ": " + itemData.InvSize.ToString());
                }
                else
                {
                    Debug.LogWarning(itemData.Name + ": No inventory sprite found");
                }

                // Equip sprite
                string[] itemEqExts = { "v1", "v1a", "v1b", "v2", "v2a", "v2b", "v3", "v3a", "v3b", "v4", "v4a", "v4b" };
                foreach (string itemEqExt in itemEqExts)
                {
                    var itemEqKey = itemData.ImageName + itemEqExt;
                    if (m_EquippableItemSpriteMap.ContainsKey(itemEqKey))
                    {
                        itemData.EquipSprites.Add(m_EquippableItemSpriteMap[itemEqKey]);
                    }
                }

                // Outdoor sprite
                ObjectDisplayData outdoorDisplayData = DbMgr.Instance.ObjectDisplayDb.Get(itemData.SpriteIndex);
                if (outdoorDisplayData != null)
                {
                    if (m_OutdoorItemSpriteMap.ContainsKey(outdoorDisplayData.SpriteName))
                    {
                        itemData.OutdoorSprite = m_OutdoorItemSpriteMap[outdoorDisplayData.SpriteName];
                    }
                    else
                    {
                        Debug.LogError("Item: " + itemData.Name + " does not have outoor display sprite");
                    }
                }
                else
                {
                    Debug.LogError("Item: " + itemData.Name + " does not have outoor display data");
                }
            }

            // Dolls
            OpenMM8Util.AppendResourcesToMap(m_DollSpriteMap, "Sprites/DOLLS");
            OpenMM8Util.AppendResourcesToMap(m_SpriteMap, "Sprites/BUFF_ANIM_SPRITES");

            m_SpellbookUI = SpellbookUI.Create();

            return true;
        }

        public bool PostInit()
        {
            return true;
        }

        private void UpdateEmptySlotBanners(PlayerParty party)
        {
            int numPartyMembers = party.Characters.Count;
            for (int emptySlotIdx = 0; emptySlotIdx < m_EmptySlotBanners.Count; emptySlotIdx++)
            {
                if (emptySlotIdx < numPartyMembers)
                {
                    m_EmptySlotBanners[emptySlotIdx].enabled = false;
                }
                else
                {
                    m_EmptySlotBanners[emptySlotIdx].enabled = true;
                }
            }
        }

        private void Update()
        {
            m_TimeSinceLastPartyText += Time.deltaTime;
            if (m_TimeSinceLastPartyText > 2.0f)
            {
                SetPartyInfoText("", false);
            }

            m_PartyTextLockTime -= Time.deltaTime;

            // ---------------------------------------------------------
            // Inspect item
            // ---------------------------------------------------------
            if (Input.GetButton("InspectObject") && m_HoveredItem != null)
            {
                // Show item info
                Item item = m_HoveredItem;
                m_InspectItemUI.ItemImage.sprite = item.Data.InvSprite;
                m_InspectItemUI.ItemImage.SetNativeSize();

                m_InspectItemUI.ItemName.text = item.Data.Name;
                if (item.Enchant != null && item.Enchant.OfTypeText != "")
                {
                    m_InspectItemUI.ItemName.text += " " + item.Enchant.OfTypeText;
                }

                m_InspectItemUI.ItemSpecific.text = "Type: " + item.Data.NotIdentifiedName;
                switch (item.Data.ItemType)
                {
                    case ItemType.WeaponOneHanded:
                    case ItemType.WeaponTwoHanded:
                        m_InspectItemUI.ItemSpecific.text +=
                                "\nAttack: +" + item.Data.Mod2 + "    Damage: " + item.Data.Mod1 + " + " + item.Data.Mod2;
                        break;

                    case ItemType.Missile:
                        m_InspectItemUI.ItemSpecific.text += 
                            "\nShoot: +" + item.Data.Mod2 + "    Damage: " + item.Data.Mod1 + " + " + item.Data.Mod2;
                        break;

                    case ItemType.Armor:
                    case ItemType.Helmet:
                    case ItemType.Cloak:
                    case ItemType.Shield:
                    case ItemType.Gauntlets:
                    case ItemType.Boots:
                        m_InspectItemUI.ItemSpecific.text +=
                            "\nArmor: +" + (int.Parse(item.Data.Mod1) + int.Parse(item.Data.Mod2));
                        break;

                    case ItemType.Bottle:
                    case ItemType.Reagent:
                        m_InspectItemUI.ItemSpecific.text += "\nPower: " + item.Data.Mod1;
                        break;

                    case ItemType.Belt:
                    case ItemType.Ring:
                    case ItemType.Amulet:
                    case ItemType.Misc:
                    case ItemType.Ore:
                    case ItemType.Gem:
                    case ItemType.MessageScroll:
                    case ItemType.SpellScroll:
                    case ItemType.SpellBook:
                    case ItemType.Gold:
                    case ItemType.NotAvailable:
                        break;

                    default:
                        m_InspectItemUI.ItemSpecific.text += "\nUnhandled item type: " + item.Data.ItemType;
                        break;
                }

                if (item.Enchant != null && item.Enchant.BonusDescText != "")
                {
                    m_InspectItemUI.ItemSpecific.text += "\nSpecial: " + item.Enchant.BonusDescText;
                }

                float specHeight = GetTextHeight(m_InspectItemUI.ItemSpecific);
                m_InspectItemUI.ItemSpecific.rectTransform.sizeDelta = new Vector2(
                        m_InspectItemUI.ItemSpecific.rectTransform.rect.width,
                        specHeight);

                m_InspectItemUI.Description.text = item.Data.Notes;
                float descHeight = GetTextHeight(m_InspectItemUI.Description);
                m_InspectItemUI.Description.rectTransform.sizeDelta = new Vector2(
                        m_InspectItemUI.Description.rectTransform.rect.width,
                        descHeight);

                m_InspectItemUI.Value.text = "Value: " + item.GetValue();
                float valueHeight = GetTextHeight(m_InspectItemUI.Value);

                // TODO: ...
                float dynTextHeight = (specHeight / 10.0f) + (descHeight / 10.0f) + (valueHeight / 10.0f) + 35.0f;
                float imageHeight = m_InspectItemUI.ItemImage.transform.GetComponent<RectTransform>().rect.height;

                if (imageHeight > dynTextHeight)
                {
                    m_InspectItemUI.BackgroundTransfrom.sizeDelta = new Vector2(
                        m_InspectItemUI.BackgroundTransfrom.rect.width,
                        imageHeight + InspectItemUI.TOP_SPACE_PX + InspectItemUI.BOTTOM_SPACE_PX);
                }
                else
                {
                    m_InspectItemUI.BackgroundTransfrom.sizeDelta = new Vector2(
                        m_InspectItemUI.BackgroundTransfrom.rect.width,
                        dynTextHeight + InspectItemUI.TOP_SPACE_PX + InspectItemUI.BOTTOM_SPACE_PX);
                }

                float edgeFillAmount = (m_InspectItemUI.BackgroundTransfrom.rect.height - 20.0f) /
                    m_InspectItemUI.LeftEdge.rectTransform.rect.height;
                m_InspectItemUI.LeftEdge.fillAmount = edgeFillAmount;
                m_InspectItemUI.RightEdge.fillAmount = edgeFillAmount;

                Vector2 mouseNormPos = GetMouseRatioCoord();
                Vector2 mousePixelPosUI = new Vector2(mouseNormPos.x * UI_WIDTH, mouseNormPos.y * UI_HEIGHT);

                const float cursorSpace = InspectItemUI.CURSOR_SPACE;
                float rightEdgePos = mouseNormPos.x * UI_WIDTH + cursorSpace + m_InspectItemUI.BackgroundTransfrom.rect.width;

                // If outdoor put it in center of the crosshair
                Vector2 inspectTopLeft = new Vector2();
                if (m_CurrUIState == null)
                {
                    inspectTopLeft.Set(Constants.CrosshairScreenRelPos.x * UI_WIDTH -
                        m_InspectItemUI.BackgroundTransfrom.rect.width / 2,
                        Constants.CrosshairScreenRelPos.y * UI_HEIGHT +
                        m_InspectItemUI.BackgroundTransfrom.rect.height / 2);
                }
                else
                {
                    bool isItemInspectLeft = (rightEdgePos - 50) > UI_WIDTH;
                    if (isItemInspectLeft)
                    {
                        bool isCursorOverlapping = mousePixelPosUI.x < (m_InspectItemUI.BackgroundTransfrom.rect.width + cursorSpace);
                        if (isCursorOverlapping)
                        {
                            bool isTopLeft = (UI_HEIGHT - mousePixelPosUI.y) + m_InspectItemUI.BackgroundTransfrom.rect.height + cursorSpace > UI_HEIGHT;
                            if (isTopLeft)
                            {
                                // No room to be neither left nor below the cursor => in the top left corner
                                inspectTopLeft.Set(0, UI_HEIGHT);
                            }
                            else
                            {
                                // Below cursor on the left side
                                inspectTopLeft.Set(0, mousePixelPosUI.y - cursorSpace);
                            }
                        }
                        else
                        {
                            // Above curser on the left side
                            inspectTopLeft.Set(
                                mousePixelPosUI.x - cursorSpace - m_InspectItemUI.BackgroundTransfrom.rect.width,
                                InspectItemUI.DEFAULT_Y);
                        }
                    }
                    else
                    {
                        bool isCursorOverlapping = mousePixelPosUI.x > (UI_WIDTH - m_InspectItemUI.BackgroundTransfrom.rect.width);
                        if (isCursorOverlapping)
                        {
                            bool isTopRight = (UI_HEIGHT - mousePixelPosUI.y) + m_InspectItemUI.BackgroundTransfrom.rect.height + cursorSpace > UI_HEIGHT;
                            if (isTopRight)
                            {
                                // No room to be neither right nor below the cursor => in the top right corner
                                inspectTopLeft.Set(UI_WIDTH - m_InspectItemUI.BackgroundTransfrom.rect.width, UI_HEIGHT);
                            }
                            else
                            {
                                // Below cursor on the right side
                                inspectTopLeft.Set(UI_WIDTH - m_InspectItemUI.BackgroundTransfrom.rect.width, mousePixelPosUI.y - cursorSpace);
                            }
                        }
                        else
                        {
                            // Above curspr on the right side
                            float x = Mathf.Min(UI_WIDTH - m_InspectItemUI.BackgroundTransfrom.rect.width, mousePixelPosUI.x + cursorSpace);
                            inspectTopLeft.Set(
                                x,
                                InspectItemUI.DEFAULT_Y);
                        }
                    }
                }

                m_InspectItemUI.BackgroundTransfrom.anchoredPosition = inspectTopLeft;

                m_InspectItemUI.Holder.GetComponent<Canvas>().enabled = true;
            }
            else
            {
                m_InspectItemUI.Holder.GetComponent<Canvas>().enabled = false;
            }

            // ---------------------------------------------------------
            // Inspecting Stat / Skill / Reading a letter
            // ---------------------------------------------------------
            if (Input.GetButton("InspectObject") && m_HoveredInspectableUiText != null)
            {
                // Header + Text
                m_InspectUiTextUI.NameText.text = m_HoveredInspectableUiText.GetHeader();
                m_InspectUiTextUI.InfoText.text = m_HoveredInspectableUiText.GetInfoText();

                // Inspect canvas height
                float nameHeight = GetTextHeight(m_InspectUiTextUI.NameText);
                float infoHeight = GetTextHeight(m_InspectUiTextUI.InfoText);

                // TODO: ...
                float dynTextHeight = (nameHeight / 10.0f) + (infoHeight / 10.0f) + 10.0f;
                m_InspectUiTextUI.BackgroundTransfrom.sizeDelta = new Vector2(
                    m_InspectUiTextUI.BackgroundTransfrom.rect.width,
                    dynTextHeight + InspectItemUI.TOP_SPACE_PX + InspectItemUI.BOTTOM_SPACE_PX);

                float edgeFillAmount = (m_InspectUiTextUI.BackgroundTransfrom.rect.height - 20.0f) /
                    m_InspectUiTextUI.LeftEdge.rectTransform.rect.height;
                m_InspectUiTextUI.LeftEdge.fillAmount = edgeFillAmount;
                m_InspectUiTextUI.RightEdge.fillAmount = edgeFillAmount;

                // Y position - below cursor
                Vector2 mouseNormPos = GetMouseRatioCoord();
                Vector2 mousePixelPosUI = new Vector2(mouseNormPos.x * UI_WIDTH, mouseNormPos.y * UI_HEIGHT);

                Vector2 inspectTopLeft = new Vector2(m_InspectUiTextUI.BackgroundTransfrom.anchoredPosition.x,
                    mousePixelPosUI.y - 30.0f);

                // Check if it is below screen
                if (inspectTopLeft.y - m_InspectUiTextUI.BackgroundTransfrom.rect.height < 20.0f)
                {
                    inspectTopLeft = new Vector2(m_InspectUiTextUI.BackgroundTransfrom.anchoredPosition.x,
                        UI_HEIGHT - 30.0f);
                }

                m_InspectUiTextUI.BackgroundTransfrom.anchoredPosition = inspectTopLeft;
                m_InspectUiTextUI.Holder.GetComponent<Canvas>().enabled = true;
            }
            else
            {
                m_InspectUiTextUI.Holder.GetComponent<Canvas>().enabled = false;
            }

            // Held item position update
            if (m_HeldItem != null)
            {
                //transform.position = Camera.main.ScreenToWorldPoint(Input.mousePosition.x, Input.mousePosition.y);
                //m_HeldItem.gameObject.GetComponent<RectTransform>().rect.
                //Debug.Log(m_HeldItem.gameObject.GetComponent<RectTransform>().rect.ToString());
                /*m_HeldItem.gameObject.GetComponent<RectTransform>().anchoredPosition =
                    new Vector2(Input.mousePosition.x, Input.mousePosition.y);*/


                var rt = m_HeldItem.gameObject.GetComponent<RectTransform>();
                Vector2 mousePixelPosUI = new Vector2();

                if (m_CurrUIState == null)
                {
                    mousePixelPosUI.Set(Constants.CrosshairScreenRelPos.x * UI_WIDTH - rt.rect.width / 2, 
                        Constants.CrosshairScreenRelPos.y * UI_HEIGHT + rt.rect.height / 2);
                }
                else
                {
                    Vector2 mouseNormPos = GetMouseRatioCoord();
                    mousePixelPosUI.Set(mouseNormPos.x * UI_WIDTH, mouseNormPos.y * UI_HEIGHT);
                }
                

                /*Vector2 outPoint;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, mousePixelPosUI, null, out outPoint);
                Debug.Log("----- Post: " + outPoint.ToString());*/

                //mousePixelPosUI += outPoint;

                rt.anchoredPosition = mousePixelPosUI;
            }
        }


        
        public Rect GetScreenCoordinates(RectTransform uiElement)
        {
            var worldCorners = new Vector3[4];
            uiElement.GetWorldCorners(worldCorners);
            var result = new Rect(
                          worldCorners[0].x,
                          worldCorners[0].y,
                          worldCorners[2].x - worldCorners[0].x,
                          worldCorners[2].y - worldCorners[0].y);
            return result;
        }

        //=================================== Static Helpers ===================================


        static public Ray GetCrosshairRay()
        {
            return Camera.main.ViewportPointToRay(Constants.CrosshairScreenRelPos);
        }

        static public float GetTextHeight(Text text, bool applyScale = false)
        {

            Vector2 extents = new Vector2(text.rectTransform.rect.width, 1);

            float scaleY = 1.0f;
            if (applyScale)
            {
                scaleY = text.transform.GetComponent<RectTransform>().localScale.y;
            }

            TextGenerator textGen = new TextGenerator();
            TextGenerationSettings generationSettings =
                text.GetGenerationSettings(extents);
            textGen.GetPreferredHeight(text.text, generationSettings);
            float lineSpacing = text.lineSpacing;
            float height = (textGen.lineCount * (text.fontSize * lineSpacing * 1.15f)) * scaleY;

            return height;
        }

        static public Sprite RandomSprite(List<Sprite> sprites)
        {
            return sprites[UnityEngine.Random.Range(0, sprites.Count)];
        }

        static Vector2 GetMouseRatioCoord()
        {
            return new Vector2(
                (float)Input.mousePosition.x / (float)Screen.width,
                (float)Input.mousePosition.y / (float)Screen.height);
        }


        //=================================== Methods ===================================


        public bool HandleButtonDown(string button)
        {
            Debug.Log("ButtonDown: " + button);

            // "HACK": Console takes precedence to all UI states
            if (button == "Console" && m_ConsoleUIState == null)
            {
                m_ConsoleUIState = new ConsoleUIState();
                m_ConsoleUIState.EnterState(null);
                return true;
            }
            else if (m_ConsoleUIState != null)
            {
                return m_ConsoleUIState.OnActionPressed(button);
            }

            if (m_CurrUIState != null)
            {
                // States need to know how to handle actions
                return m_CurrUIState.OnActionPressed(button);
            }
            else
            {
                // Check the button corresponds to any UI state
                switch (button)
                {
                    case "Escape":
                        break;

                    case "Map":
                        m_CurrUIState = new MapUIState();
                        m_CurrUIState.EnterState(null);
                        break;

                    case "Quest":
                        break;

                    case "Story":
                        break;

                    case "Notes":
                        break;

                    case "Rest":
                        break;

                    case "Inventory":
                        m_CurrUIState = new CharDetailUIState();
                        m_CurrUIState.EnterState(new CharDetailUIStateArgs(CharDetailState.Inventory));
                        break;

                    case "Spellbook":
                        // Active character HAS TO exist
                        if (m_PlayerParty.ActiveCharacter == null)
                        {
                            break;
                        }

                        m_CurrUIState = new SpellbookUIState();
                        m_CurrUIState.EnterState(m_PlayerParty.GetActiveCharacter());
                        break;

                    case "Stats":
                        break;

                    default:
                        break;
                }
            }

            return false;
        }

        public bool IsInGameBlockingState()
        {
            return m_CurrUIState != null && m_CurrUIState.IsGameBlocking();
        }

        public Sprite GetNpcAvatarSprite(int pictureId)
        {
            const int PLACEHOLDER_ID = 2200;

            Sprite sprite = null;
            if (m_NpcAvatarsMap.ContainsKey(pictureId))
            {
                sprite = m_NpcAvatarsMap[pictureId];
            }
            else
            {
                sprite = m_NpcAvatarsMap[PLACEHOLDER_ID];
                Debug.LogWarning("Could not find NPC avatar with ID: " + pictureId + ". Setting placeholder.");
            }

            return sprite;
        }

        public void SetupForFullscreenUiState(UIState invoker)
        {
            m_Minimap.enabled = false;
            m_PartyBuffsAndButtonsCanvas.enabled = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            foreach (Character chr in m_PlayerParty.Characters)
            {
                chr.UI.Holder.SetActive(false);
            }

            if (invoker.IsGameBlocking())
            {
                GameMgr.Instance.PauseGame();
            }
        }

        public void SetupForPartialUiState(UIState invoker)
        {
            m_Minimap.enabled = false;
            m_PartyBuffsAndButtonsCanvas.enabled = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (invoker.IsGameBlocking())
            {
                GameMgr.Instance.PauseGame();
            }
        }

        private void SetPartyInfoText(string text, bool forceRewrite = true)
        {
            if (!forceRewrite)
            {
                if (m_PartyTextLockTime < 0)
                {
                    m_PartyUI.HoverInfoText.text = text;
                    m_TimeSinceLastPartyText = 0.0f;
                }

                return;
            }

            m_PartyUI.HoverInfoText.text = text;
            m_TimeSinceLastPartyText = 0.0f;
            m_PartyTextLockTime = 2.0f;
        }

        private CharacterSprites GetCharacterSprites(int characterId)
        {
            // Caching
            if (m_CharacterSpritesMap.ContainsKey(characterId) && m_CharacterSpritesMap[characterId] != null)
            {
                return m_CharacterSpritesMap[characterId];
            }
            else
            {
                m_CharacterSpritesMap[characterId] = CharacterSprites.Load(characterId);
                return m_CharacterSpritesMap[characterId];
            }
        }

        public void ReturnToGame()
        {
            m_PartyBuffsAndButtonsCanvas.enabled = true;
            m_NpcTalkUI.NpcTalkCanvas.enabled = false;
            m_InspectNpcUI.Canvas.enabled = false;
            m_MapQuestNotesUI.Canvas.enabled = false;

            m_Minimap.enabled = true;
            m_MinimapCloseButtonImage.enabled = false;
            m_PartyBuffsAndButtonsCanvas.enabled = true;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            m_HoveredItem = null;

            if (m_CurrUIState != null)
            {
                m_CurrUIState.LeaveState();
                m_CurrUIState = null;
            }

            foreach (Character chr in m_PlayerParty.Characters)
            {
                chr.UI.Holder.SetActive(true);
            }

            GameMgr.Instance.UnpauseGame();
        }

        public void SetHeldItem(Item item)
        {
            if (m_HeldItem)
            {
                Logger.LogDebug("Destroying already held item: " + m_HeldItem.Item.Data.Name);
                GameObject.Destroy(m_HeldItem.gameObject);
                m_HeldItem = null;
            }

            GameObject inventoryItemObj = (GameObject)GameObject.Instantiate(
                Resources.Load("Prefabs/UI/Inventory/InventoryItem"), m_PartyCanvasObj.transform);

            InventoryItem inventoryItem = inventoryItemObj.GetComponent<InventoryItem>();
            m_HeldItem = inventoryItemObj.GetComponent<InventoryItem>();
            m_HeldItem.IsHeld = true;
            m_HeldItem.Item = item;
            m_HeldItem.Image.raycastTarget = false;

            RectTransform rt = m_HeldItem.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.0f, 1.0f);

            m_HeldItem.Image.sprite = item.Data.InvSprite;
            m_HeldItem.Image.SetNativeSize();
        }


        //=================================== Events ===================================


        private void OnCharacterJoinedParty(Character chr, PlayerParty party)
        {
            // Set up UI
            int numPartyMembers = party.Characters.Count;

            CharacterUI chrUI = new CharacterUI();
            //float chrUiOffsetX = (Constants.PC_WidthDelta * m_PartyCanvasObj.transform.localScale.x) * chr.GetPartyIndex();
            GameObject pc = (GameObject)Instantiate(Resources.Load("Prefabs/UI/PC"), m_PartyCanvasObj.transform);
            pc.transform.localPosition = new Vector3(Constants.PC_WidthDelta, 0.0f, 0.0f) * chr.GetPartyIndex();
            //pc.transform.position += new Vector3(Constants.PC_WidthDelta, 0.0f, 0.0f) * chr.Data.PartyIndex;
            pc.name = "PC_" + chr.Name;

            chrUI.Holder = pc;
            chrUI.PlayerCharacter = pc.transform.Find("PC_Avatar").GetComponent<Image>();
            chrUI.SelectionRing = pc.transform.Find("PC_SelectRing").GetComponent<Image>();
            chrUI.AgroStatus = pc.transform.Find("PC_AgroStatus").GetComponent<Image>();
            chrUI.HealthBar = pc.transform.Find("PC_HealthBar").GetComponent<Image>();
            chrUI.ManaBar = pc.transform.Find("PC_ManaBar").GetComponent<Image>();
            chrUI.BlessBuff = pc.transform.Find("PC_BlessBuff").GetComponent<Image>();
            chrUI.FaceOverlayAnimation = pc.transform.Find("PC_AvatarAnim").GetComponent<SpriteAnimation>();

            chr.UI = chrUI;

            chr.CharFaceUpdater = new CharFaceUpdater(chr);
            chr.UI.Sprites = GetCharacterSprites(chr.CharacterId);

            // Doll for char detail UI (Inventory/Stats/Awards/Skills UI, Adventurer's Inn, Character creation page)
            //string dollPrefabName = "DOLL_PC_" + ((int)chr.CharacterId).ToString();
            string dollPrefabName = "GenericDoll";
            chr.UI.DollUI = new DollUI();

            chr.UI.DollUI.Holder = (GameObject)Instantiate(Resources.Load("Prefabs/UI/Dolls/" + dollPrefabName), m_CharDetailUI.CanvasHolder.transform);
            chr.UI.DollUI.Holder.transform.SetSiblingIndex(0);
            chr.UI.DollUI.BackgroundImage = OpenMM8Util.GetComponentAtScenePath<Image>("Background", chr.UI.DollUI.Holder);
            chr.UI.DollUI.BodyImage = OpenMM8Util.GetComponentAtScenePath<Image>("Body", chr.UI.DollUI.Holder);
            chr.UI.DollUI.LH_OpenImage = OpenMM8Util.GetComponentAtScenePath<Image>("LeftHand_Open", chr.UI.DollUI.Holder);
            chr.UI.DollUI.LH_ClosedImage = OpenMM8Util.GetComponentAtScenePath<Image>("LeftHand_Closed", chr.UI.DollUI.Holder);
            chr.UI.DollUI.LH_HoldImage = OpenMM8Util.GetComponentAtScenePath<Image>("LeftHand_Hold", chr.UI.DollUI.Holder);
            chr.UI.DollUI.RH_OpenImage = OpenMM8Util.GetComponentAtScenePath<Image>("RightHand_Open", chr.UI.DollUI.Holder);
            chr.UI.DollUI.RH_HoldImage = OpenMM8Util.GetComponentAtScenePath<Image>("RightHand_Hold", chr.UI.DollUI.Holder);
            chr.UI.DollUI.RH_WeaponAnchorHolder = OpenMM8Util.GetGameObjAtScenePath("RightHand_WeaponHoldAnchor", chr.UI.DollUI.Holder);
            //chr.UI.DollUI.RH_HoldFingersImage = OpenMM8Util.GetComponentAtScenePath<Image>("RightHand_HoldFingers", chr.UI.DollUI.Holder);
            chr.UI.DollUI.Cloak = OpenMM8Util.GetComponentAtScenePath<InventoryItem>("CloakSlot", chr.UI.DollUI.Holder);
            chr.UI.DollUI.Bow = OpenMM8Util.GetComponentAtScenePath<InventoryItem>("BowSlot", chr.UI.DollUI.Holder);
            chr.UI.DollUI.Armor = OpenMM8Util.GetComponentAtScenePath<InventoryItem>("ArmorSlot", chr.UI.DollUI.Holder);
            chr.UI.DollUI.Boots = OpenMM8Util.GetComponentAtScenePath<InventoryItem>("BootsSlot", chr.UI.DollUI.Holder);
            chr.UI.DollUI.Helmet = OpenMM8Util.GetComponentAtScenePath<InventoryItem>("HelmetSlot", chr.UI.DollUI.Holder);
            chr.UI.DollUI.Belt = OpenMM8Util.GetComponentAtScenePath<InventoryItem>("BeltSlot", chr.UI.DollUI.Holder);
            chr.UI.DollUI.RH_Weapon = 
                OpenMM8Util.GetComponentAtScenePath<InventoryItem>("RightHand_WeaponHoldAnchor/RightHand_WeaponSlot", chr.UI.DollUI.Holder);

            // Background
            chr.UI.DollUI.BackgroundImage.sprite = m_DollSpriteMap[chr.CharacterData.Background];
            chr.UI.DollUI.BackgroundImage.SetNativeSize();
            // Body
            chr.UI.DollUI.BodyImage.sprite = m_DollSpriteMap[chr.CharacterData.Body];
            chr.UI.DollUI.BodyImage.SetNativeSize();
            chr.UI.DollUI.BodyImage.rectTransform.anchoredPosition = chr.CharacterData.DollBodyPos;
            // LHo (Left Hand Open)
            if (chr.CharacterData.LHo != "none")
            {
                chr.UI.DollUI.LH_OpenImage.sprite = m_DollSpriteMap[chr.CharacterData.LHo];
                chr.UI.DollUI.LH_OpenImage.SetNativeSize();
                chr.UI.DollUI.LH_OpenImage.rectTransform.anchoredPosition = chr.DollTypeData.LH_FingersPos;
            }
            // LHd (Left Hand Closed)
            if (chr.CharacterData.LHd != "none")
            {
                chr.UI.DollUI.LH_ClosedImage.sprite = m_DollSpriteMap[chr.CharacterData.LHd];
                chr.UI.DollUI.LH_ClosedImage.SetNativeSize();
                chr.UI.DollUI.LH_ClosedImage.rectTransform.anchoredPosition = chr.DollTypeData.LH_ClosedPos;
            }
            // LHu (Left Hand Hold)
            if (chr.CharacterData.LHu != "none")
            {
                chr.UI.DollUI.LH_HoldImage.sprite = m_DollSpriteMap[chr.CharacterData.LHu];
                chr.UI.DollUI.LH_HoldImage.SetNativeSize();
                chr.UI.DollUI.LH_HoldImage.rectTransform.anchoredPosition = chr.DollTypeData.LH_OpenPos;
            }
            // RHd (Right Hand Open)
            if (chr.CharacterData.RHd != "none")
            {
                chr.UI.DollUI.RH_OpenImage.sprite = m_DollSpriteMap[chr.CharacterData.RHd];
                chr.UI.DollUI.RH_OpenImage.SetNativeSize();
                chr.UI.DollUI.RH_OpenImage.rectTransform.anchoredPosition = chr.DollTypeData.RH_OpenPos;
            }
            // RHu (Right Hand Closed)
            if (chr.CharacterData.RHu != "none")
            {
                chr.UI.DollUI.RH_HoldImage.sprite = m_DollSpriteMap[chr.CharacterData.RHu];
                chr.UI.DollUI.RH_HoldImage.SetNativeSize();
                chr.UI.DollUI.RH_HoldImage.rectTransform.anchoredPosition = chr.DollTypeData.RH_ClosedPos;
            }
            
            // Right hand fingers
            if (chr.CharacterData.RHb != "none")
            {
                // Right hand fingers + weapon holder
                Image weaponAnchorImage = chr.UI.DollUI.RH_WeaponAnchorHolder.GetComponent<Image>();
                weaponAnchorImage.sprite = m_DollSpriteMap[chr.CharacterData.RHb];
                weaponAnchorImage.SetNativeSize();
                chr.UI.DollUI.RH_WeaponAnchorHolder.GetComponent<RectTransform>().anchoredPosition =
                    chr.DollTypeData.RH_FingersPos;

                Image holdFingersImage = OpenMM8Util.GetComponentAtScenePath<Image>("RightHand_HoldFingers", chr.UI.DollUI.RH_WeaponAnchorHolder);
                holdFingersImage.sprite = m_DollSpriteMap[chr.CharacterData.RHb];
                holdFingersImage.SetNativeSize();
                holdFingersImage.rectTransform.anchoredPosition.Set(0.0f, 0.0f);
            }

            chr.UI.DollUI.AccessoryBackgroundHolder = OpenMM8Util.GetGameObjAtScenePath("AccessoryBackground", chr.UI.DollUI.Holder);
            chr.UI.DollUI.Ring_1 = OpenMM8Util.GetComponentAtScenePath<InventoryItem>("RingSlot_1", chr.UI.DollUI.AccessoryBackgroundHolder);
            chr.UI.DollUI.Ring_2 = OpenMM8Util.GetComponentAtScenePath<InventoryItem>("RingSlot_2", chr.UI.DollUI.AccessoryBackgroundHolder);
            chr.UI.DollUI.Ring_3 = OpenMM8Util.GetComponentAtScenePath<InventoryItem>("RingSlot_3", chr.UI.DollUI.AccessoryBackgroundHolder);
            chr.UI.DollUI.Ring_4 = OpenMM8Util.GetComponentAtScenePath<InventoryItem>("RingSlot_4", chr.UI.DollUI.AccessoryBackgroundHolder);
            chr.UI.DollUI.Ring_5 = OpenMM8Util.GetComponentAtScenePath<InventoryItem>("RingSlot_5", chr.UI.DollUI.AccessoryBackgroundHolder);
            chr.UI.DollUI.Ring_6 = OpenMM8Util.GetComponentAtScenePath<InventoryItem>("RingSlot_6", chr.UI.DollUI.AccessoryBackgroundHolder);
            chr.UI.DollUI.Gauntlets = OpenMM8Util.GetComponentAtScenePath<InventoryItem>("GauntletSlot", chr.UI.DollUI.AccessoryBackgroundHolder);
            chr.UI.DollUI.Necklace = OpenMM8Util.GetComponentAtScenePath<InventoryItem>("NecklaceSlot", chr.UI.DollUI.AccessoryBackgroundHolder);

            // Fill inventory slots
            chr.EquipSlots.Add(chr.UI.DollUI.Armor);
            chr.EquipSlots.Add(chr.UI.DollUI.Boots);
            chr.EquipSlots.Add(chr.UI.DollUI.Cloak);
            chr.EquipSlots.Add(chr.UI.DollUI.Belt);
            chr.EquipSlots.Add(chr.UI.DollUI.Helmet);
            chr.EquipSlots.Add(chr.UI.DollUI.Bow);
            chr.EquipSlots.Add(chr.UI.DollUI.RH_Weapon);
            //chr.EquipSlots.Add(chr.UI.DollUI.LH_Weapon);
            //chr.EquipSlots.Add(chr.UI.DollUI.Shield);

            chr.EquipSlots.Add(chr.UI.DollUI.Ring_1);
            chr.EquipSlots.Add(chr.UI.DollUI.Ring_2);
            chr.EquipSlots.Add(chr.UI.DollUI.Ring_3);
            chr.EquipSlots.Add(chr.UI.DollUI.Ring_4);
            chr.EquipSlots.Add(chr.UI.DollUI.Ring_5);
            chr.EquipSlots.Add(chr.UI.DollUI.Ring_6);
            chr.EquipSlots.Add(chr.UI.DollUI.Gauntlets);
            chr.EquipSlots.Add(chr.UI.DollUI.Necklace);

            chr.UI.DollUI.Holder.SetActive(false);

            OpenMM8Util.GetComponentAtScenePath<Button>("MagnifyGlass", chr.UI.DollUI.Holder).onClick.AddListener(
                delegate { chr.UI.DollUI.AccessoryBackgroundHolder.SetActive(!chr.UI.DollUI.AccessoryBackgroundHolder.active); });

            // To raycast only non-transparent areas
            // Settings for the texture-import for dolls was also changed because of this
            // Read-Write - enabled
            // Mesh Type - Full Rect
            chr.UI.DollUI.BodyImage.alphaHitTestMinimumThreshold = 0.4f;
            chr.UI.DollUI.LH_OpenImage.alphaHitTestMinimumThreshold = 0.4f;
            chr.UI.DollUI.LH_ClosedImage.alphaHitTestMinimumThreshold = 0.4f;
            chr.UI.DollUI.LH_HoldImage.alphaHitTestMinimumThreshold = 0.4f;
            chr.UI.DollUI.RH_OpenImage.alphaHitTestMinimumThreshold = 0.4f;
            chr.UI.DollUI.RH_HoldImage.alphaHitTestMinimumThreshold = 0.4f;

            chr.UI.InventoryUI = InventoryUI.Create(chr);
            chr.UI.StatsUI = StatsUI.Create(chr);
            chr.UI.SkillsUI = SkillsUI.Create(chr);

            //chr.UI.StatsUI.Holder = 

            UpdateEmptySlotBanners(party);
        }

        private void OnCharacterLeftParty(Character removedChr, PlayerParty party)
        {
            removedChr.UI.Destroy();

            foreach (Character chr in party.Characters)
            {
                chr.UI.Holder.transform.localPosition =
                    new Vector3(Constants.PC_WidthDelta, 0.0f, 0.0f) * chr.GetPartyIndex();
            }

            UpdateEmptySlotBanners(party);
        }

        public void OnCharHealthChanged(Character chr, int maxHealth, int currHealth, int delta)
        {
            float healthPercent = ((float)currHealth / (float)maxHealth) * 100.0f;
            chr.UI.SetHealth(healthPercent);

            //Debug.Log(maxHealth + " " + currHealth);
        }

        public void OnCharManaChanged(Character chr, int maxMana, int currMana)
        {

        }

        public void OnFoundGold(int amount)
        {
            SetPartyInfoText("You found " + amount.ToString() + " gold !");
        }

        public void OnGoldChanged(int oldGold, int newGold, int delta)
        {
            m_PartyUI.GoldText.text = newGold.ToString();
        }

        public void OnFoodChanged(int oldFood, int newFood, int delta)
        {
            m_PartyUI.FoodText.text = newFood.ToString();
        }

        public void OnCharRecovered(Character chr)
        {
            chr.UI.AgroStatus.enabled = true;
        }

        public void OnCharRecoveryTimeChanged(Character chr, float recoveryTime)
        {
            if (recoveryTime > 0.0f)
            {
                chr.UI.AgroStatus.enabled = false;
            }
        }

        public void OnCharConditionChanged(Character chr, Condition newCondition)
        {

        }

        public void OnCharHitNpc(Character chr, AttackInfo attackInfo, AttackResult result)
        {
            chr.CharFaceUpdater.ResetTimer();

            string hitText = "";
            switch (result.Type)
            {
                case AttackResultType.Hit:
                    hitText = chr.Name + " hits " + result.VictimName + 
                        " for " + result.DamageDealt + " damage";
                    break;

                case AttackResultType.Kill:
                    hitText = chr.Name + " inflicts " + result.DamageDealt + 
                        " points killing " + result.VictimName;
                    chr.CharFaceUpdater.SetAvatar(RandomSprite(chr.UI.Sprites.Smile), 0.75f);
                    break;

                case AttackResultType.Miss:
                    hitText = chr.Name + " missed attack on " + result.VictimName;
                    chr.CharFaceUpdater.SetAvatar(RandomSprite(chr.UI.Sprites.FailAction), 0.75f);
                    break;

                default:
                    Logger.LogError("Unknown attack result type: " + result.Type);
                    break;
            }

            SetPartyInfoText(hitText);
        }

        public void OnCharGotHit(Character chr, AttackInfo attackInfo, AttackResult attackResult)
        {
            if (attackResult.Type == AttackResultType.Hit)
            {
                chr.CharFaceUpdater.SetAvatar(RandomSprite(chr.UI.Sprites.TakeDamage), 0.5f);
            }
        }

        private void OnCharAttack(Character chr, AttackInfo attackInfo)
        {
            chr.CharFaceUpdater.ResetTimer();
        }

        public void OnHoverObject(HoverInfo hoverInfo)
        {
            SetPartyInfoText(hoverInfo.HoverText, false);
        }

        public void OnNpcInspectStart(Character inspector, BaseNpc npc, MonsterData npcData)
        {
            m_InspectNpcUI.Canvas.enabled = true;
            m_InspectNpcUI.NpcNameText.text = npcData.Name;
            m_InspectNpcUI.HitPointsText.text = npc.CurrentHitPoints.ToString() + "/" + npcData.HitPoints;
            m_InspectNpcUI.ArmorClassText.text = npcData.ArmorClass.ToString();
            m_InspectNpcUI.AttackText.text = npcData.Attack1.DamageType.ToString();
            m_InspectNpcUI.DamageText.text = npcData.AttackAmountText;
            m_InspectNpcUI.SpellText.text = npcData.SpellAttack1.SpellName == "" ? "None" : npcData.SpellAttack1.SpellName;
            m_InspectNpcUI.FireResistanceText.text = npcData.Resistances[SpellElement.Fire].ToString();
            m_InspectNpcUI.AirResistanceText.text = npcData.Resistances[SpellElement.Air].ToString();
            m_InspectNpcUI.WaterResistanceText.text = npcData.Resistances[SpellElement.Water].ToString();
            m_InspectNpcUI.EarthResistanceText.text = npcData.Resistances[SpellElement.Earth].ToString();
            m_InspectNpcUI.MindResistanceText.text = npcData.Resistances[SpellElement.Mind].ToString();
            m_InspectNpcUI.SpiritResistanceText.text = npcData.Resistances[SpellElement.Spirit].ToString();
            m_InspectNpcUI.BodyResistanceText.text = npcData.Resistances[SpellElement.Body].ToString();
            m_InspectNpcUI.LightResistanceText.text = npcData.Resistances[SpellElement.Light].ToString();
            m_InspectNpcUI.DarkResistanceText.text = npcData.Resistances[SpellElement.Dark].ToString();
            m_InspectNpcUI.PhysicalResistanceText.text = npcData.Resistances[SpellElement.Physical].ToString();

            m_InspectNpcUI.PreviewImage.sprite = npc.PreviewImage;

            m_InspectNpcUI.Healthbar.fillAmount = (float)npc.CurrentHitPoints / (float)npcData.HitPoints;
            if (m_InspectNpcUI.Healthbar.fillAmount > 0.66f)
            {
                m_InspectNpcUI.Healthbar.sprite = InspectNpcUI.HealthbarSprite_Green;
            }
            else if (m_InspectNpcUI.Healthbar.fillAmount > 0.33f)
            {
                m_InspectNpcUI.Healthbar.sprite = InspectNpcUI.HealthbarSprite_Yellow;
            }
            else
            {
                m_InspectNpcUI.Healthbar.sprite = InspectNpcUI.HealthbarSprite_Red;
            }
        }

        public void OnNpcInspectEnd(Character inspector, BaseNpc npc, MonsterData npcData)
        {
            m_InspectNpcUI.Canvas.enabled = false;
        }

        public void OnItemInspect(Character inspectorChr, ItemData itemData/*, InspectResult result*/)
        {

        }

        public void OnItemInspectEnd()
        {

        }

        public void OnItemEquip(/*Item item, EquipResult equipResult*/)
        {

        }

        public void OnItemHold(ItemData item)
        {

        }

        public void OnItemHoldEnd()
        {

        }

        public void OnMinimapMarkerCreated(MinimapMarker marker)
        {
            m_Minimap.MinimapMarkers.Add(marker);
        }

        public void OnMinimapMarkerDestroyed(MinimapMarker marker)
        {
            m_Minimap.MinimapMarkers.Remove(marker);
        }

        /*
         *  Called when Player talks with Talkable object (House, NPC, ...) for the FIRST time
         *  All the rest converstaion takes place inside TalkUIState
         */
        public void OnTalkSceneStart(Character talkerChr, TalkScene talkScene)
        {
            if (m_CurrUIState != null)
            {
                Debug.LogError("Talk attempt when already talking. Not supported now !");
                return;
            }

            m_CurrUIState = new TalkUIState();

            SetupForPartialUiState(m_CurrUIState);
            m_MinimapCloseButtonImage.enabled = true;

            m_CurrUIState.EnterState(new TalkUIStateArgs(talkerChr, talkScene, m_NpcTalkUI));
        }

        private void OnQuestBitAdded(int questId)
        {
            Character chr = m_PlayerParty.GetActiveCharacter();
            if (chr == null)
            {
                chr = m_PlayerParty.GetFirstCharacter();
            }

            SpriteAnimation FaceOverlayAnim = chr.UI.FaceOverlayAnimation;
            FaceOverlayAnim.AnimationSprites = m_QuestEffectSprites;
            FaceOverlayAnim.Play();
            chr.CharFaceUpdater.SetAvatar(RandomSprite(chr.UI.Sprites.Smile), 1.0f);
        }

        private void OnCharacterFinishedEvent(Character chr)
        {
            SpriteAnimation FaceOverlayAnim = chr.UI.FaceOverlayAnimation;
            FaceOverlayAnim.AnimationSprites = m_QuestEffectSprites;
            FaceOverlayAnim.Play();
            chr.CharFaceUpdater.SetAvatar(RandomSprite(chr.UI.Sprites.Smile), 1.0f);
        }

        private void OnInventoryItemHoverStart(InventoryItem inventoryItem)
        {
            //Debug.Log("Hovered over item: " + inventoryItem.Item.Data.Name);
            m_HoveredItem = inventoryItem.Item;
        }

        private void OnInventoryItemHoverEnd(InventoryItem inventoryItem)
        {
            //Debug.Log("Unhovered over item: " + inventoryItem.Item.Data.Name);
            if (m_HoveredItem != null && m_HoveredItem == inventoryItem.Item)
            {
                m_HoveredItem = null;
            }
        }

        private void OnInventoryItemClicked(InventoryItem inventoryItem)
        {
            if (inventoryItem.isEquipped)
            {
                HandleDollItemClicked(inventoryItem);
                return;
            }

            if (m_HeldItem != null)
            {
                // Check if we can replace the existing item
                //GameObject.Destroy(m_HeldItem.gameObject);

                Vector2Int newPos;
                if (m_PlayerParty.ActiveCharacter.Inventory.CanReplaceItem(inventoryItem.Item, m_HeldItem.Item, out newPos))
                {
                    Item heldItem = m_HeldItem.Item;
                    GameObject.Destroy(m_HeldItem.gameObject);

                    SetHeldItem(inventoryItem.Item);

                    m_PlayerParty.ActiveCharacter.Inventory.RemoveItem(inventoryItem.Item);
                    m_PlayerParty.ActiveCharacter.Inventory.PlaceItem(heldItem, newPos.x, newPos.y);
                }
                else
                {
                    Debug.Log("Cannot replace");
                    return;
                }
            }
            else
            {
                SetHeldItem(inventoryItem.Item);

                m_PlayerParty.ActiveCharacter.Inventory.RemoveItem(inventoryItem.Item);
            }
        }

        private void OnOutdoorItemInspectStart(Item item)
        {
            //Debug.Log("Hovered over item: " + item.Data.Name);
            m_HoveredItem = item;
        }

        private void OnOutdoorItemInspectEnd(Item item)
        {
            //Debug.Log("Unhovered over item: " + inventoryItem.Item.Data.Name);
            if (m_HoveredItem != null && m_HoveredItem == item)
            {
                m_HoveredItem = null;
            }
        }

        private void OnInventoryCellClicked(int x, int y)
        {
            // Try to place on cell
            if (m_HeldItem != null)
            {
                // Check if we can replace the existing item
                if (m_PlayerParty.ActiveCharacter.Inventory.PlaceItem(m_HeldItem.Item, x, y, true))
                {
                    GameObject.Destroy(m_HeldItem.gameObject);
                }
            }
        }

        // Item equipped by doll was clicked
        public void HandleDollItemClicked(InventoryItem clickedItem)
        {
            if (!m_HeldItem)
            {
                // If not holding anything, remove the item from doll and hold it
                SetHeldItem(clickedItem.Item);

                m_PlayerParty.ActiveCharacter.Inventory.RemoveItemFromDoll(clickedItem);

                GameEvents.InvokeEvent_OnItemUnequipped(m_PlayerParty.ActiveCharacter, clickedItem.Item);
            }
            else
            {
                // HACK: We try to "replace" rings
                if (clickedItem.Item.Data.ItemType == ItemType.Ring &&
                    m_HeldItem.Item.Data.ItemType == ItemType.Ring)
                {
                    Item tmpClickedItem = clickedItem.Item;

                    clickedItem.Image.sprite = m_HeldItem.Item.Data.InvSprite;
                    clickedItem.Image.SetNativeSize();
                    clickedItem.Item = m_HeldItem.Item;

                    SetHeldItem(tmpClickedItem);
                }
                else
                {
                    // If holding an item already, just do doll clicked routine
                    OnDollClicked(null);
                }
            }
        }

        // When holding an item and clicked on a doll
        private void OnDollClicked(DollClickHandler sender)
        {
            if (!m_HeldItem)
            {
                // If we are not holding any item, we cant do anything
                return;
            }

            Debug.Log("Doll click handling");

            // TODO: This should really be handled by Character class, not in UiMgr

            Character currChar = m_PlayerParty.ActiveCharacter;
            if (currChar == null)
            {
                Debug.LogError("null active character when doll is clicked ?");
                return;
            }

            // InteractWithItem invokes 2 events - interact result and item equipped if item was equipped
            ItemInteractResult interactResult = currChar.InteractWithItem(m_HeldItem.Item);
            if (interactResult == ItemInteractResult.Learned ||
                interactResult == ItemInteractResult.Consumed ||
                interactResult == ItemInteractResult.Casted)
            {
                GameObject.Destroy(m_HeldItem.gameObject);
                m_HeldItem = null;

                // If item was equiipped it is handled in OnItemEquipped event
            }
        }

        private void OnCharacterAvatarClicked(Character chr)
        {
            // Try to add item to inventory
            if (m_HeldItem != null)
            {
                // Check if we can replace the existing item
                if (chr.Inventory.PlaceItem(m_HeldItem.Item, 0 , 0, true))
                {
                    GameObject.Destroy(m_HeldItem.gameObject);
                }
            }
        }

        private void OnItemEquipped(Character chr, Item equippedItem, Item replacedItem)
        {
            GameObject.Destroy(m_HeldItem.gameObject);
            m_HeldItem = null;

            if (replacedItem != null)
            {
                // If we replaced an item which was equipped by doll, then we have to hold this item
                SetHeldItem(replacedItem);
            }
        }

        private void OnInspectableUiTextHoverStart(InspectableUiText inspectableUiText)
        {
            m_HoveredInspectableUiText = inspectableUiText;
        }

        private void OnInspectableUiTextHoverEnd(InspectableUiText inspectableUiText)
        {
            if (m_HoveredInspectableUiText == inspectableUiText)
            {
                m_HoveredInspectableUiText = null;
            }
        }


        // =========== Game states

        public void OnPauseGame()
        {

        }

        // =========== Buttons

        public void OnCharDetailButtonPressed(string action)
        {
            if ((m_CurrUIState != null) && (m_CurrUIState.GetType() == typeof(CharDetailUIState)))
            {
                m_CurrUIState.OnActionPressed(action);
            }
        }
    }
}
