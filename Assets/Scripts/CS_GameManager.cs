using System.Collections.Generic;
using UnityEngine;

public class CS_GameManager : MonoBehaviour
{
    public static CS_GameManager Instance { get; private set; }

    private CSO_EncounterData _currentEncounterData;

    private GameObject _player;
    private GameObject _fieldEnvironment;
    private GameObject _pauseMenu;
    private GameObject _equipmentMenu;

    private List<CS_PartyMemberState> _partyState;
    private Vector3? _pendingPlayerPosition;

    // 所持アイテム/装備、開封済み宝箱ID(いずれもセーブ対象)
    private List<CS_ItemStack> _ownedItems = new List<CS_ItemStack>();
    private List<CS_ItemStack> _ownedEquipment = new List<CS_ItemStack>();
    private List<string> _openedChestIds = new List<string>();

    // デバッグモード(タイトル画面でキーを押しながら「はじめから」した場合のみtrue。セーブ対象外)
    private bool _isDebugMode;
    public bool isDebugMode => _isDebugMode;
    public void SetDebugMode(bool value) => _isDebugMode = value;

    void Awake()
    {
        // シングルトンのインスタンスを作成する
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // シーンが切り替わっても破棄されないようにする

            LoadGameOnStartup();
        }
        else
        {
            Destroy(gameObject); // 既にインスタンスが存在する場合は破棄する
        }
    }

    private void Start()
    {
        if (!_pendingPlayerPosition.HasValue)
        {
            return;
        }

        var player = GameObject.FindAnyObjectByType<CS_PlayerMove>();
        if (player != null)
        {
            player.transform.position = _pendingPlayerPosition.Value;
        }

        _pendingPlayerPosition = null;
    }

    /// <summary>
    /// 起動時にセーブデータがあれば読み込み、パーティ状態とプレイヤー位置を復元する
    /// </summary>
    private void LoadGameOnStartup()
    {
        CS_SaveData saveData = CS_SaveManager.Instance.Load();
        if (saveData == null)
        {
            return;
        }

        _partyState = saveData.partyState;
        _pendingPlayerPosition = saveData.playerPosition;
        _ownedItems = saveData.ownedItems ?? new List<CS_ItemStack>();
        _ownedEquipment = saveData.ownedEquipment ?? new List<CS_ItemStack>();
        _openedChestIds = saveData.openedChestIds ?? new List<string>();
    }

    /// <summary>
    /// 味方パーティの現在HP/MPを取得する。未生成なら初期値からフル生成する
    /// </summary>
    public List<CS_PartyMemberState> GetOrInitializePartyState(List<CSO_CharacterData> defaultPartyData)
    {
        if (_partyState == null)
        {
            _partyState = new List<CS_PartyMemberState>();
            foreach (var data in defaultPartyData)
            {
                _partyState.Add(new CS_PartyMemberState
                {
                    characterName = data.characterName,
                    currentHealth = data.baseHealth,
                    currentMP = data.baseMP,
                    level = 1,
                    exp = 0
                });
            }
        }

        return _partyState;
    }

    public bool HasSaveData()
    {
        return CS_SaveManager.Instance.HasSaveData();
    }

    /// <summary>
    /// 「はじめから」用。保持中のパーティ状態・プレイヤー位置をリセットする。
    /// セーブファイル自体はここでは消さず、次にフィールドへ戻った際の自動保存で上書きする
    /// </summary>
    public void StartNewGame()
    {
        _partyState = null;
        _pendingPlayerPosition = null;
        _ownedItems = new List<CS_ItemStack>();
        _ownedEquipment = new List<CS_ItemStack>();
        _openedChestIds = new List<string>();
    }

    /// <summary>
    /// 戦闘後の味方の現在HP/MPを永続状態へ書き戻す
    /// </summary>
    public void UpdatePartyState(IReadOnlyList<CS_CharacterState> allyParty)
    {
        if (_partyState == null || _partyState.Count != allyParty.Count)
        {
            return;
        }

        for (int i = 0; i < allyParty.Count; i++)
        {
            _partyState[i].currentHealth = allyParty[i].currentHealth;
            _partyState[i].currentMP = allyParty[i].currentMP;
            _partyState[i].level = allyParty[i].level;
            _partyState[i].exp = allyParty[i].currentExp;
        }
    }

    /// <summary>
    /// 現在のプレイヤー位置とパーティ状態をセーブデータとして保存する。
    /// ポーズメニューなど、戦闘を経由せずに呼ばれる場合に備えて_playerを保険で探しておく
    /// </summary>
    public void SaveGame()
    {
        if (_player == null)
        {
            var player = GameObject.FindAnyObjectByType<CS_PlayerMove>();
            if (player != null)
            {
                _player = player.gameObject;
            }
        }

        var saveData = new CS_SaveData
        {
            playerPosition = _player != null ? _player.transform.position : Vector3.zero,
            partyState = _partyState,
            ownedItems = _ownedItems,
            ownedEquipment = _ownedEquipment,
            openedChestIds = _openedChestIds
        };
        CS_SaveManager.Instance.Save(saveData);
    }

    /// <summary>
    /// 所持しているアイテムの個数を返す(未所持なら0)
    /// </summary>
    public int GetItemCount(string itemId)
    {
        var stack = _ownedItems.Find(s => s.id == itemId);
        return stack != null ? stack.count : 0;
    }

    /// <summary>
    /// アイテムを所持数に加算する(ドロップ・宝箱用)
    /// </summary>
    public void AddItem(string itemId, int count)
    {
        var stack = _ownedItems.Find(s => s.id == itemId);
        if (stack != null)
        {
            stack.count += count;
        }
        else
        {
            _ownedItems.Add(new CS_ItemStack { id = itemId, count = count });
        }
    }

    /// <summary>
    /// アイテムを1個消費できれば消費してtrueを返す。デバッグモード中は所持数を確認・消費せず常にtrueを返す
    /// </summary>
    public bool TryConsumeItem(string itemId)
    {
        if (_isDebugMode) return true;

        var stack = _ownedItems.Find(s => s.id == itemId);
        if (stack == null || stack.count <= 0) return false;

        stack.count--;
        return true;
    }

    /// <summary>
    /// 所持している装備の一覧(未装備の手持ち分)。デバッグモード中はUI側でCS_ItemDatabase.AllEquipmentを使うこと
    /// </summary>
    public IReadOnlyList<CS_ItemStack> ownedEquipmentList => _ownedEquipment;
    public IReadOnlyList<CS_ItemStack> ownedItemsList => _ownedItems;

    /// <summary>
    /// 装備を所持数に加算する(ドロップ・宝箱用)
    /// </summary>
    public void AddEquipment(string equipmentId, int count)
    {
        var stack = _ownedEquipment.Find(s => s.id == equipmentId);
        if (stack != null)
        {
            stack.count += count;
        }
        else
        {
            _ownedEquipment.Add(new CS_ItemStack { id = equipmentId, count = count });
        }
    }

    /// <summary>
    /// 指定した味方キャラクターの指定スロットに装備IDをセットする(空文字で外す)
    /// </summary>
    public void SetEquipped(string characterName, CSE_EquipmentSlot slot, string equipmentId)
    {
        var member = _partyState?.Find(m => m.characterName == characterName);
        if (member == null) return;

        switch (slot)
        {
            case CSE_EquipmentSlot.Weapon: member.equippedWeaponId = equipmentId; break;
            case CSE_EquipmentSlot.Armor: member.equippedArmorId = equipmentId; break;
            case CSE_EquipmentSlot.Accessory: member.equippedAccessoryId = equipmentId; break;
        }
    }

    /// <summary>
    /// 指定した味方キャラクターの指定スロットに装備している装備IDを返す(未装備は空文字)
    /// </summary>
    public string GetEquipped(string characterName, CSE_EquipmentSlot slot)
    {
        var member = _partyState?.Find(m => m.characterName == characterName);
        if (member == null) return "";

        return slot switch
        {
            CSE_EquipmentSlot.Weapon => member.equippedWeaponId,
            CSE_EquipmentSlot.Armor => member.equippedArmorId,
            CSE_EquipmentSlot.Accessory => member.equippedAccessoryId,
            _ => ""
        };
    }

    public bool IsChestOpened(string chestId) => _openedChestIds.Contains(chestId);

    public void MarkChestOpened(string chestId)
    {
        if (!_openedChestIds.Contains(chestId))
        {
            _openedChestIds.Add(chestId);
        }
    }

    public void RequestBattle(CSO_EncounterData encounterData)
    {
        _currentEncounterData = encounterData;

        if (_player == null)
            _player = GameObject.FindAnyObjectByType<CS_PlayerMove>().gameObject;
        if (_fieldEnvironment == null)
            _fieldEnvironment = GameObject.Find("FieldEnvironment");
        if (_pauseMenu == null)
            _pauseMenu = GameObject.Find("PauseMenuController");
        if (_equipmentMenu == null)
            _equipmentMenu = GameObject.Find("EquipmentMenuController");

        CS_SceneManager.Instance.LoadSceneAdditive("BattleScene", () =>
        {
            // BattleSceneがロードされた後の処理
            Debug.Log("BattleSceneがロードされました。");

            // プレイヤーとカメラを非表示にする
            if (_player != null)
                _player.SetActive(false); // プレイヤーの移動を無効化する
            // フィールド側を非表示にする
            if (_fieldEnvironment != null)
                _fieldEnvironment.SetActive(false);
            // 戦闘中はポーズメニュー・装備メニューを開けないようにする
            if (_pauseMenu != null)
                _pauseMenu.SetActive(false);
            if (_equipmentMenu != null)
                _equipmentMenu.SetActive(false);
        });
    }

    public CSO_EncounterData ConsumePendingEncounter()
    {
        var encounterData = _currentEncounterData;
        _currentEncounterData = null; // 消化したらnullにする
        return encounterData;
    }

    public void ReturnToField()
    {
        CS_SceneManager.Instance.UnloadSceneAdditive("BattleScene", () =>
        {
            // BattleSceneがアンロードされた後の処理
            Debug.Log("BattleSceneがアンロードされました。");

            // プレイヤーとカメラを再表示する
            if (_player != null)
                _player.SetActive(true);
            // フィールド側を再表示にする
            if (_fieldEnvironment != null)
                _fieldEnvironment.SetActive(true);
            // ポーズメニュー・装備メニューを再度開けるようにする
            if (_pauseMenu != null)
                _pauseMenu.SetActive(true);
            if (_equipmentMenu != null)
                _equipmentMenu.SetActive(true);

            SaveGame();
        });
    }

    public void ReturnTown()
    {
        CS_SceneManager.Instance.UnloadSceneAdditive("BattleScene", () =>
        {
            // BattleSceneがアンロードされた後の処理
            Debug.Log("BattleSceneがアンロードされました。");

            // プレイヤーとカメラを再表示する
            if (_player != null)
                _player.SetActive(true);
            // フィールド側を再表示にする
            if (_fieldEnvironment != null)
                _fieldEnvironment.SetActive(true);
            // ポーズメニュー・装備メニューを再度開けるようにする
            if (_pauseMenu != null)
                _pauseMenu.SetActive(true);
            if (_equipmentMenu != null)
                _equipmentMenu.SetActive(true);

            // ここで宿屋に移動させる処理を追加する

            SaveGame();
        });
    }
}
