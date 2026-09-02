using System.Collections.Generic;

public class CS_BattleContext
{
    // プレイヤーの状態
    private readonly CS_CharacterState _playerState;
    public CS_CharacterState playerState => _playerState;

    // 敵の状態
    private readonly CS_CharacterState _enemyState;
    public CS_CharacterState enemyState => _enemyState;

    // 行動順キュー
    private readonly Queue<CS_BattleActionEntry> _actionQueue = new Queue<CS_BattleActionEntry>();
    public Queue<CS_BattleActionEntry> actionQueue => _actionQueue;

    // コマンド
    public IBattleCommand playerCommand { get; set; }
    public IBattleCommand enemyCommand { get; set; }

    // 戦闘結果
    public CSE_BattleResult result { get; set; } = CSE_BattleResult.None;

    public CS_BattleContext(CS_CharacterState playerState, CS_CharacterState enemyState)
    {
        _playerState = playerState;
        _enemyState = enemyState;
    }
}