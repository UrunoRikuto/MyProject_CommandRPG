using System.Collections.Generic;
using UnityEngine;

public class CS_GameManager : MonoBehaviour
{
    public static CS_GameManager Instance { get; private set; }

    private CSO_EncounterData _currentEncounterData;

    private GameObject _player;
    private GameObject _fieldEnvironment;
    private GameObject _pauseMenu;

    private List<CS_PartyMemberState> _partyState;
    private Vector3? _pendingPlayerPosition;

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
                    level = 1
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
            partyState = _partyState
        };
        CS_SaveManager.Instance.Save(saveData);
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
            // 戦闘中はポーズメニューを開けないようにする
            if (_pauseMenu != null)
                _pauseMenu.SetActive(false);
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
            // ポーズメニューを再度開けるようにする
            if (_pauseMenu != null)
                _pauseMenu.SetActive(true);

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
            // ポーズメニューを再度開けるようにする
            if (_pauseMenu != null)
                _pauseMenu.SetActive(true);

            // ここで宿屋に移動させる処理を追加する

            SaveGame();
        });
    }
}
