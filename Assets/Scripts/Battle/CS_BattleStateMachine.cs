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

    private bool _isChangingState;
    private IBattleState _pendingNextState;
    private bool _hasPendingNextState;


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

    /// <summary>
    /// ó‘Ô‚ğØ‚è‘Ö‚¦‚é
    /// </summary>
    public void ChangeState(IBattleState nextState)
    {
        // Šù‚ÉChangeStateÀs’†‚È‚çAŸ‚Ì‘JˆÚæ‚ğ—\–ñ‚·‚é‚¾‚¯
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