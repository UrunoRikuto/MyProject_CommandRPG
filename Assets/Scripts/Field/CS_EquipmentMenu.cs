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

    public void OnSelectMember0Clicked()
    {
        _selectedIndex = 0;
        RefreshUI();
    }

    public void OnSelectMember1Clicked()
    {
        _selectedIndex = 1;
        RefreshUI();
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

        // 日本語グリフを持つTMP Font Assetが無いため、動的テキストは既存の慣例に合わせて英語表記にする
        _infoText.text =
            $"{characterName}\n" +
            $"Weapon: {DisplayName(weaponId)}\n" +
            $"Armor: {DisplayName(armorId)}\n" +
            $"Accessory: {DisplayName(accessoryId)}\n" +
            BuildStatPreview(characterName, weaponId, armorId, accessoryId);
    }

    private string DisplayName(string equipmentId)
    {
        var equipment = CS_ItemDatabase.GetEquipment(equipmentId);
        return equipment != null ? equipment.equipmentName : "None";
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
