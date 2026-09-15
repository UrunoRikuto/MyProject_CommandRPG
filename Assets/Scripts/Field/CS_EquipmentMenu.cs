using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// フィールドでTabキーを押すと開閉する装備画面。CS_PauseMenuと同じ二分割構成(ロジック側)。
/// パーティメンバーを選び、武器/防具/装飾品の3スロットを個別に変更できる
/// </summary>
public class CS_EquipmentMenu : MonoBehaviour
{
    [SerializeField] private GameObject _menuPanel;
    [SerializeField] private List<CSO_CharacterData> _playerPartyData;
    [SerializeField] private CS_EquipmentPickWindow _pickWindow;

    // パーティメンバー選択ボタン(人数分だけ動的に生成する。固定2人決め打ちにしないことで
    // パーティ人数が変わってもシーン側の修正なしに対応できる)
    [SerializeField] private Button _memberButtonPrefab;
    [SerializeField] private Transform _memberButtonParent;

    // 選択中メンバー名・3スロットの装備名・ステータスプレビューをまとめて1つのテキストに表示する
    [SerializeField] private TextMeshProUGUI _infoText;

    private CS_PlayerMove _playerMove;
    private int _selectedIndex = 0;
    private CSE_EquipmentSlot _pendingSlot;

    private void Start()
    {
        _menuPanel.SetActive(false);
        _playerMove = GameObject.FindAnyObjectByType<CS_PlayerMove>();
        _pickWindow.onEquipmentSelected += HandleEquipmentSelected;
        _pickWindow.onCancelled += RefreshUI;
        BuildMemberButtons();
    }

    private const float MEMBER_BUTTON_WIDTH = 130f;
    private const float MEMBER_BUTTON_HEIGHT = 50f;
    private const float MEMBER_BUTTON_SPACING = 140f;
    private const float MEMBER_BUTTON_Y = 251f;

    /// <summary>
    /// パーティメンバーの人数分だけ選択ボタンを横一列に並べて生成する。
    /// 人数に応じて中央揃えになるよう配置位置を計算するので、人数が変わっても崩れない
    /// </summary>
    private void BuildMemberButtons()
    {
        foreach (Transform child in _memberButtonParent)
        {
            Destroy(child.gameObject);
        }

        int count = _playerPartyData.Count;
        float startX = -(count - 1) * MEMBER_BUTTON_SPACING / 2f;

        for (int i = 0; i < count; i++)
        {
            int capturedIndex = i;
            Button button = Instantiate(_memberButtonPrefab, _memberButtonParent);
            button.GetComponentInChildren<TextMeshProUGUI>().text = _playerPartyData[i].characterName;
            button.onClick.AddListener(() => SelectMember(capturedIndex));

            // _memberButtonPrefab(SkillButtonPrefab)は本来レイアウトグループ配下で使う前提のため、
            // 4倍スケール・左上アンカーのまま持っている。ここには置かないので明示的に基準を揃え直す
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.localScale = Vector3.one;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(MEMBER_BUTTON_WIDTH, MEMBER_BUTTON_HEIGHT);
            rect.anchoredPosition = new Vector2(startX + i * MEMBER_BUTTON_SPACING, MEMBER_BUTTON_Y);
        }
    }

    private void SelectMember(int index)
    {
        _selectedIndex = index;
        RefreshUI();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleMenu();
        }
    }

    private void ToggleMenu()
    {
        bool isOpening = !_menuPanel.activeSelf;
        _menuPanel.SetActive(isOpening);

        if (_playerMove != null)
        {
            _playerMove.enabled = !isOpening;
        }

        if (isOpening)
        {
            RefreshUI();
        }
    }

    public void OnChangeWeaponClicked() => OpenPicker(CSE_EquipmentSlot.Weapon);
    public void OnChangeArmorClicked() => OpenPicker(CSE_EquipmentSlot.Armor);
    public void OnChangeAccessoryClicked() => OpenPicker(CSE_EquipmentSlot.Accessory);

    /// <summary>
    /// 指定スロットの候補一覧を開く。デバッグモード中は全装備、通常時は所持品のみをスロットでフィルタする
    /// </summary>
    private void OpenPicker(CSE_EquipmentSlot slot)
    {
        _pendingSlot = slot;
        var candidates = new List<CSO_EquipmentData>();

        if (CS_GameManager.Instance.isDebugMode)
        {
            foreach (var equipment in CS_ItemDatabase.AllEquipment)
            {
                if (equipment.slotType == slot) candidates.Add(equipment);
            }
        }
        else
        {
            foreach (var stack in CS_GameManager.Instance.ownedEquipmentList)
            {
                if (stack.count <= 0) continue;
                var equipment = CS_ItemDatabase.GetEquipment(stack.id);
                if (equipment != null && equipment.slotType == slot) candidates.Add(equipment);
            }
        }

        _pickWindow.Open(candidates);
    }

    private void HandleEquipmentSelected(CSO_EquipmentData equipment)
    {
        string characterName = _playerPartyData[_selectedIndex].characterName;
        string equipmentId = equipment != null ? equipment.equipmentId : "";
        CS_GameManager.Instance.SetEquipped(characterName, _pendingSlot, equipmentId);
        RefreshUI();
    }

    private void RefreshUI()
    {
        string characterName = _playerPartyData[_selectedIndex].characterName;
        string weaponId = CS_GameManager.Instance.GetEquipped(characterName, CSE_EquipmentSlot.Weapon);
        string armorId = CS_GameManager.Instance.GetEquipped(characterName, CSE_EquipmentSlot.Armor);
        string accessoryId = CS_GameManager.Instance.GetEquipped(characterName, CSE_EquipmentSlot.Accessory);

        _infoText.text =
            $"{characterName}\n" +
            $"武器: {DisplayName(weaponId)}\n" +
            $"防具: {DisplayName(armorId)}\n" +
            $"装飾品: {DisplayName(accessoryId)}\n" +
            BuildStatPreview(characterName, weaponId, armorId, accessoryId);
    }

    private string DisplayName(string equipmentId)
    {
        var equipment = CS_ItemDatabase.GetEquipment(equipmentId);
        return equipment != null ? equipment.equipmentName : "なし";
    }

    /// <summary>
    /// 装備込みの想定ステータスを算出する(CS_CharacterState.RecalculateStatsと同じ式)。
    /// 実際の戦闘状態には影響しない、画面表示専用の計算
    /// </summary>
    private string BuildStatPreview(string characterName, string weaponId, string armorId, string accessoryId)
    {
        List<CS_PartyMemberState> partyState = CS_GameManager.Instance.GetOrInitializePartyState(_playerPartyData);
        CS_PartyMemberState memberState = partyState.Find(m => m.characterName == characterName);
        CSO_CharacterData characterData = _playerPartyData[_selectedIndex];
        int level = memberState != null ? memberState.level : 1;
        int levelBonus = level - 1;

        int health = characterData.baseHealth + characterData.healthGrowth * levelBonus;
        int mp = characterData.baseMP + characterData.mpGrowth * levelBonus;
        int attack = characterData.baseAttack + characterData.attackGrowth * levelBonus;
        int defense = characterData.baseDefense + characterData.defenseGrowth * levelBonus;
        int speed = characterData.baseSpeed + characterData.speedGrowth * levelBonus;

        foreach (var equipmentId in new[] { weaponId, armorId, accessoryId })
        {
            CSO_EquipmentData equipment = CS_ItemDatabase.GetEquipment(equipmentId);
            if (equipment == null) continue;

            health += equipment.healthBonus;
            mp += equipment.mpBonus;
            attack += equipment.attackBonus;
            defense += equipment.defenseBonus;
            speed += equipment.speedBonus;
        }

        return $"HP {health} / MP {mp} / ATK {attack} / DEF {defense} / SPD {speed}";
    }
}
