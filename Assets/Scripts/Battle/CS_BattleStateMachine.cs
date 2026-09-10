using System;
using System.Collections.Generic;
using UnityEngine;

public class CS_BattleStateMachine : MonoBehaviour
{
    [SerializeField]
    private List<CSO_CharacterData> _playerPartyData;

    [SerializeField]
    private List<CSO_CharacterData> _enemyPartyData;

    [SerializeField] private CS_CommandButtonInput _commandButtonInput;
    public CS_CommandButtonInput commandButtonInput => _commandButtonInput;

    [SerializeField] private CS_CharacterUIWindow _characterUIWindow;
    public CS_CharacterUIWindow characterUIWindow => _characterUIWindow;

    [SerializeField] private CS_BattleEffectPlayer _effectPlayer;
    public CS_BattleEffectPlayer effectPlayer => _effectPlayer;

    private CS_BattleContext _context;
    private IBattleState _currentState;

    public CS_BattleContext context => _context;

    private bool _isChangingState;
    private IBattleState _pendingNextState;
    private bool _hasPendingNextState;

    private bool _hasStarted = false;

    public event Action<CSE_BattleResult> onBattleEnd;
    public void NotifyBattleEnd(CSE_BattleResult result) => onBattleEnd?.Invoke(result);

    private void Start()
    {
        if (_hasStarted) return;

        List<int> enemyLevels = null;
        var encounterData = CS_GameManager.Instance.ConsumePendingEncounter();
        if (encounterData != null)
        {
            _enemyPartyData.Clear();
            enemyLevels = new List<int>();
            for (int i = 0; i < encounterData.enemyDataList.Count; i++)
            {
                var enemyData = encounterData.enemyDataList[i];
                if (enemyData != null)
                {
                    _enemyPartyData.Add(enemyData);
                    // エンカウントのレベル範囲内でランダムにレベルを決定する
                    enemyLevels.Add(UnityEngine.Random.Range(encounterData.minLevel, encounterData.maxLevel + 1));
                }
            }
        }

        StartBattle(_playerPartyData, _enemyPartyData, enemyLevels);
    }

    public void StartBattle(List<CSO_CharacterData> playerPartyData, IReadOnlyList<CSO_CharacterData> enemyPartyData)
    {
        StartBattle(playerPartyData, enemyPartyData, null);
    }

    public void StartBattle(List<CSO_CharacterData> playerPartyData, IReadOnlyList<CSO_CharacterData> enemyPartyData, IReadOnlyList<int> enemyLevels)
    {
        BuildContext(playerPartyData, enemyPartyData, enemyLevels);

        _characterUIWindow.CreateCharacterUI(_context.allyParty, _context.enemyParty);

        ChangeState(new CS_BattleStateStart());

        _hasStarted = true;
    }

    private void BuildContext(List<CSO_CharacterData> playerPartyData, IReadOnlyList<CSO_CharacterData> enemyPartyData, IReadOnlyList<int> enemyLevels)
    {
        List<CS_PartyMemberState> partyState = CS_GameManager.Instance.GetOrInitializePartyState(playerPartyData);

        List<CS_CharacterState> playerParty = new List<CS_CharacterState>();
        for (int i = 0; i < playerPartyData.Count; i++)
        {
            int level = i < partyState.Count ? partyState[i].level : 1;
            CS_CharacterState characterState = new CS_CharacterState(playerPartyData[i], level);
            if (i < partyState.Count)
            {
                characterState.SetCurrentStats(partyState[i].currentHealth, partyState[i].currentMP);
                characterState.SetExp(partyState[i].exp);
            }
            playerParty.Add(characterState);
        }
        List<CS_CharacterState> enemyParty = new List<CS_CharacterState>();
        for (int i = 0; i < enemyPartyData.Count; i++)
        {
            int level = (enemyLevels != null && i < enemyLevels.Count) ? enemyLevels[i] : 1;
            enemyParty.Add(new CS_CharacterState(enemyPartyData[i], level));
        }
        _context = new CS_BattleContext(playerParty, enemyParty);
    }

    private void Update()
    {
        _currentState?.Update(_context, this);
    }

    /// <summary>
    /// 状態を切り替える
    /// </summary>
    public void ChangeState(IBattleState nextState)
    {
        // 既にChangeState実行中なら、次の遷移先を予約するだけ
        if (_isChangingState)
        {
            _pendingNextState = nextState;
            _hasPendingNextState = true;
            return;
        }

        _isChangingState = true;

        IBattleState stateToEnter = nextState;
        while (stateToEnter != null)
        {
            _currentState?.Exit(_context, this);
            _currentState = stateToEnter;

            _hasPendingNextState = false;
            _pendingNextState = null;
            _currentState.Enter(_context, this);

            stateToEnter = _hasPendingNextState ? _pendingNextState : null;
        }

        _isChangingState = false;
    }
}
