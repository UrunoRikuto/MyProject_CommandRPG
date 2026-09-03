using UnityEngine;

public class CS_BattleStateMachine : MonoBehaviour
{
    [SerializeField] 
    private CSO_CharacterData _playerData;

    [SerializeField] 
    private CSO_CharacterData _enemyData;

    [SerializeField] private CS_CommandButtonInput _commandButtonInput;
    public CS_CommandButtonInput commandButtonInput => _commandButtonInput;

    private CS_BattleContext _context;
    private IBattleState _currentState;

    public CS_BattleContext context => _context;

    private void Awake()
    {
        CS_CharacterState player = new CS_CharacterState(_playerData);
        CS_CharacterState enemy = new CS_CharacterState(_enemyData);
        _context = new CS_BattleContext(player, enemy);

        // Tools > Value Observer でHPの変化をリアルタイム監視
        CS_ValueObserver.Instance.Register(gameObject, this, "playerHP", () => _context.playerState.currentHealth);
        CS_ValueObserver.Instance.Register(gameObject, this, "enemyHP", () => _context.enemyState.currentHealth);
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