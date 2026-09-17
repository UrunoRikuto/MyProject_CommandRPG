using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CS_GameManager : MonoBehaviour
{
    public static CS_GameManager Instance { get; private set; }

    // 町シーンでプレイヤーを復活させる際の初期位置(CSED_TownSceneBuilderが配置する町の中心と揃えてある)
    private static readonly Vector3 TOWN_SPAWN_POSITION = new Vector3(8f, 6f, 0f);

    private CSO_EncounterData _currentEncounterData;
    private List<CS_EncounterPartyMember> _pendingEncounterParty;

    private GameObject _player;
    private GameObject _fieldEnvironment;
    private GameObject _pauseMenu;
    private GameObject _equipmentMenu;

    private List<CS_PartyMemberState> _partyState;
    private Vector3? _pendingPlayerPosition;
    // _pendingPlayerPosition/_spawnAtFieldExitがどのシーン向けかを覚えておく。異なるシーンに
    // 誤って適用してしまう(例: フィールドの座標を町にそのまま適用して町の範囲外に出現する)のを防ぐ
    private string _pendingPlayerPositionScene;
    // 町からマップ選択で選んだフィールドへ向かう場合、そのフィールドの「町へ戻る出入口」
    // (CS_SceneEntrance)の位置にプレイヤーを配置する。座標は町側では分からない
    // (対象のフィールドを読み込むまで存在しない)ため、フラグだけ持たせて読み込み後に解決する
    private bool _spawnAtFieldExit;

    // 「つづきから」で復帰するシーン名(セーブ対象。はじめからは常にTownSceneへ直接遷移する)
    private string _currentSceneName = "TownScene";
    public string continueSceneName => _currentSceneName;

    // 所持アイテム/装備、開封済み宝箱ID(いずれもセーブ対象)
    private List<CS_ItemStack> _ownedItems = new List<CS_ItemStack>();
    private List<CS_ItemStack> _ownedEquipment = new List<CS_ItemStack>();
    private List<string> _openedChestIds = new List<string>();

    // クエスト板の受注可能なクエスト・受注中のクエスト(いずれもセーブ対象)
    private const int QUEST_BOARD_SIZE = 3;
    private const int MAX_ACTIVE_QUESTS = 3;
    private List<CS_QuestData> _boardQuests = new List<CS_QuestData>();
    private List<CS_QuestData> _activeQuests = new List<CS_QuestData>();
    public IReadOnlyList<CS_QuestData> boardQuests => _boardQuests;
    public IReadOnlyList<CS_QuestData> activeQuests => _activeQuests;

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
            // DontDestroyOnLoadで永続化されるオブジェクトのStart()はゲーム起動時に一度しか
            // 呼ばれない(2回目以降のシーン遷移では発火しない)ため、プレイヤー位置の復元は
            // Start()ではなくシーンが切り替わるたびに発火するsceneLoadedイベント側で行う
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject); // 既にインスタンスが存在する場合は破棄する
        }
    }

    /// <summary>
    /// シーンが読み込まれるたびに呼ばれる。読み込まれたシーンが、予約しておいたプレイヤー配置の
    /// 対象と一致する場合のみ適用する(シーン名の一致を確認しないと、別シーン向けの座標や
    /// 出入口を誤って適用してしまい、範囲外に出現する不具合につながる)
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single) return; // 戦闘のAdditiveロードは対象外
        if (scene.name != _pendingPlayerPositionScene) return;

        var player = GameObject.FindAnyObjectByType<CS_PlayerMove>();
        if (player == null) return; // プレイヤーがいないシーン(タイトル等)では消費せず持ち越す

        if (_spawnAtFieldExit)
        {
            var entrance = GameObject.FindAnyObjectByType<CS_SceneEntrance>();
            if (entrance != null)
            {
                player.transform.position = entrance.transform.position;
            }
            _spawnAtFieldExit = false;
            _pendingPlayerPositionScene = null;
            return;
        }

        if (!_pendingPlayerPosition.HasValue) return;

        player.transform.position = _pendingPlayerPosition.Value;
        _pendingPlayerPosition = null;
        _pendingPlayerPositionScene = null;
    }

    /// <summary>
    /// 町からマップ選択でフィールドへ向かう際に呼ぶ。そのフィールドの「町へ戻る出入口」の
    /// 位置にプレイヤーを配置する予約をする(実際の配置はそのフィールドの読み込み完了後)
    /// </summary>
    public void RequestSpawnAtFieldExit(string sceneName)
    {
        _spawnAtFieldExit = true;
        _pendingPlayerPosition = null;
        _pendingPlayerPositionScene = sceneName;
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
        _boardQuests = saveData.boardQuests ?? new List<CS_QuestData>();
        _activeQuests = saveData.activeQuests ?? new List<CS_QuestData>();
        _currentSceneName = string.IsNullOrEmpty(saveData.currentSceneName) ? "TownScene" : saveData.currentSceneName;
        // この位置はcurrentSceneName(=保存時にいたシーン)向けのものなので、紐付けて覚えておく
        _pendingPlayerPositionScene = _currentSceneName;
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
        _pendingPlayerPositionScene = null;
        _spawnAtFieldExit = false;
        _ownedItems = new List<CS_ItemStack>();
        _ownedEquipment = new List<CS_ItemStack>();
        _openedChestIds = new List<string>();
        _boardQuests = new List<CS_QuestData>();
        _activeQuests = new List<CS_QuestData>();
        _currentSceneName = "TownScene";
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
    /// パーティ全員の体力・MPを最大値まで回復する(街の泉用)。
    /// 装備込みの最大値をCS_EquipmentMenu.BuildStatPreviewと同じ式で算出し直す
    /// (戦闘中と違い、町にいる間はCS_CharacterStateが存在しないため直接計算する)
    /// </summary>
    public void HealPartyToFull(List<CSO_CharacterData> playerPartyData)
    {
        List<CS_PartyMemberState> partyState = GetOrInitializePartyState(playerPartyData);

        for (int i = 0; i < partyState.Count && i < playerPartyData.Count; i++)
        {
            CSO_CharacterData data = playerPartyData[i];
            CS_PartyMemberState member = partyState[i];
            int levelBonus = member.level - 1;

            int maxHealth = data.baseHealth + data.healthGrowth * levelBonus;
            int maxMP = data.baseMP + data.mpGrowth * levelBonus;

            foreach (string equipmentId in new[] { member.equippedWeaponId, member.equippedArmorId, member.equippedAccessoryId })
            {
                CSO_EquipmentData equipment = CS_ItemDatabase.GetEquipment(equipmentId);
                if (equipment == null) continue;
                maxHealth += equipment.healthBonus;
                maxMP += equipment.mpBonus;
            }

            member.currentHealth = maxHealth;
            member.currentMP = maxMP;
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

        _currentSceneName = SceneManager.GetActiveScene().name;

        var saveData = new CS_SaveData
        {
            playerPosition = _player != null ? _player.transform.position : Vector3.zero,
            partyState = _partyState,
            ownedItems = _ownedItems,
            ownedEquipment = _ownedEquipment,
            openedChestIds = _openedChestIds,
            boardQuests = _boardQuests,
            activeQuests = _activeQuests,
            currentSceneName = _currentSceneName
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

    /// <summary>
    /// クエスト板の受注可能な一覧が既定数(3件)を下回っていたら、ランダムクエストで補充する。
    /// クエスト板を開くたびに呼ぶ
    /// </summary>
    public void EnsureQuestBoardFilled()
    {
        while (_boardQuests.Count < QUEST_BOARD_SIZE)
        {
            CS_QuestData quest = GenerateRandomQuest();
            if (quest == null) break;
            _boardQuests.Add(quest);
        }
    }

    /// <summary>
    /// 討伐対象・目標数・報酬をランダムに決めたクエストを1件作る。
    /// 討伐対象はCSO_QuestMonsterPool(Resources/Quest/DB_QuestMonsterPool)から、
    /// 報酬はCS_ItemDatabaseのアイテム・装備を合わせたプールから抽選する
    /// </summary>
    private CS_QuestData GenerateRandomQuest()
    {
        CSO_QuestMonsterPool monsterPool = Resources.Load<CSO_QuestMonsterPool>("Quest/DB_QuestMonsterPool");
        if (monsterPool == null || monsterPool.monsters.Count == 0)
        {
            Debug.LogWarning("Resources/Quest/DB_QuestMonsterPoolが見つからないか空です。クエストを生成できません。");
            return null;
        }

        CSO_CharacterData targetMonster = monsterPool.monsters[UnityEngine.Random.Range(0, monsterPool.monsters.Count)];

        var allItems = CS_ItemDatabase.AllItems;
        var allEquipment = CS_ItemDatabase.AllEquipment;
        int totalRewardCandidates = allItems.Count + allEquipment.Count;
        if (totalRewardCandidates == 0)
        {
            Debug.LogWarning("報酬候補となるアイテム/装備が見つかりません。クエストを生成できません。");
            return null;
        }

        int rewardIndex = UnityEngine.Random.Range(0, totalRewardCandidates);
        CSE_QuestRewardType rewardType;
        string rewardId;
        if (rewardIndex < allItems.Count)
        {
            rewardType = CSE_QuestRewardType.Item;
            rewardId = allItems[rewardIndex].itemId;
        }
        else
        {
            rewardType = CSE_QuestRewardType.Equipment;
            rewardId = allEquipment[rewardIndex - allItems.Count].equipmentId;
        }

        return new CS_QuestData
        {
            questId = System.Guid.NewGuid().ToString(),
            targetMonsterName = targetMonster.characterName,
            targetCount = UnityEngine.Random.Range(3, 9),
            currentCount = 0,
            rewardType = rewardType,
            rewardId = rewardId,
            rewardCount = rewardType == CSE_QuestRewardType.Item ? UnityEngine.Random.Range(1, 4) : 1,
            isAccepted = false,
        };
    }

    /// <summary>
    /// 指定したクエストを受注する。受注中のクエストが上限(3件)に達していたら失敗しfalseを返す
    /// </summary>
    public bool AcceptQuest(string questId)
    {
        if (_activeQuests.Count >= MAX_ACTIVE_QUESTS) return false;

        CS_QuestData quest = _boardQuests.Find(q => q.questId == questId);
        if (quest == null) return false;

        _boardQuests.Remove(quest);
        quest.isAccepted = true;
        _activeQuests.Add(quest);
        EnsureQuestBoardFilled();
        return true;
    }

    /// <summary>
    /// 敵を1体倒すたびに呼ぶ。対象種族が一致する受注中クエストの進捗を進め、
    /// 目標数に達したら報酬を即座に付与してクエストを完了扱いにする
    /// </summary>
    public void ReportMonsterDefeated(string characterName)
    {
        for (int i = _activeQuests.Count - 1; i >= 0; i--)
        {
            CS_QuestData quest = _activeQuests[i];
            if (quest.targetMonsterName != characterName) continue;

            quest.currentCount++;
            if (quest.currentCount < quest.targetCount) continue;

            if (quest.rewardType == CSE_QuestRewardType.Item)
            {
                AddItem(quest.rewardId, quest.rewardCount);
            }
            else
            {
                AddEquipment(quest.rewardId, quest.rewardCount);
            }
            Debug.Log($"クエスト達成: {quest.targetMonsterName}を{quest.targetCount}体討伐、報酬を獲得しました。");

            _activeQuests.RemoveAt(i);
        }
    }

    public void RequestBattle(CSO_EncounterData encounterData)
    {
        _currentEncounterData = encounterData;
        LoadBattleScene();
    }

    /// <summary>
    /// 段階の異なる敵が混ざった編成(例: Mid1体+Common2体)で戦闘を開始する。
    /// 種族・レベルは呼び出し側(CS_FieldMonster)で既に抽選済みの値をそのまま使う
    /// </summary>
    public void RequestBattle(List<CS_EncounterPartyMember> party)
    {
        _pendingEncounterParty = party;
        LoadBattleScene();
    }

    /// <summary>
    /// BattleSceneのAdditiveロードと、フィールド側(プレイヤー・環境・各種メニュー)の
    /// 非表示化。単一エンカウント/混成パーティどちらの開始経路からも共通で呼ぶ
    /// </summary>
    private void LoadBattleScene()
    {
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

    /// <summary>
    /// CS_FieldMonsterが組み立てた混成パーティを取り出す。無ければnull
    /// </summary>
    public List<CS_EncounterPartyMember> ConsumePendingEncounterParty()
    {
        var party = _pendingEncounterParty;
        _pendingEncounterParty = null;
        return party;
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

    /// <summary>
    /// 敗北時、フィールドを丸ごと離れて町シーンで復活させる。
    /// (ReturnToFieldと違いフィールド側のオブジェクトは再表示しない=このフィールドは破棄される)
    /// </summary>
    public void ReturnTown()
    {
        CS_SceneManager.Instance.UnloadSceneAdditive("BattleScene", () =>
        {
            Debug.Log("BattleSceneがアンロードされました。町へ戻ります。");

            // 次にロードされるTownSceneでプレイヤーを町の初期位置へ配置する
            _pendingPlayerPosition = TOWN_SPAWN_POSITION;
            _pendingPlayerPositionScene = "TownScene";
            _spawnAtFieldExit = false;

            // シーンをまたぐキャッシュは全てこのフィールド固有のものなので破棄する
            _player = null;
            _fieldEnvironment = null;
            _pauseMenu = null;
            _equipmentMenu = null;

            CS_SceneManager.Instance.LoadScene("TownScene");
            SaveGame();
        });
    }
}
