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

    private CS_BattleContext _context;
    private IBattleState _currentState;

    public CS_BattleContext context => _context;

    private void Awake()
    {
        List<CS_CharacterState> playerParty = new List<CS_CharacterState>();
        foreach (var playerData in _playerPartyData)
        {
            playerParty.Add(new CS_CharacterState(playerData));
        }
        List<CS_CharacterState> enemyParty = new List<CS_CharacterState>();
        foreach (var enemyData in _enemyPartyData)
        {
            enemyParty.Add(new CS_CharacterState(enemyData));
        }
        _context = new CS_BattleContext(playerParty, enemyParty);
    }

    private void Start()
    {
        ChangeState(new CS_BattleStateStart());
    }

    private void Update()
    {
        _currentState?.Update(_context, this);
    }

    /// <summary>状態を切り替える。呼び出し元は各ステートクラス自身。</summary>
    public void ChangeState(IBattleState nextState)
    {
        _currentState?.Exit(_context, this);
        _currentState = nextState;
        _currentState.Enter(_context, this);
    }
}