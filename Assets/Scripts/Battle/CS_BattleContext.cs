using System.Collections.Generic;

public class CS_BattleContext
{
    // プレイヤーの状態
    private readonly List<CS_CharacterState> _playerParty;
    public IReadOnlyList<CS_CharacterState> playerParty => _playerParty;
    // ※ひとまず一番前のプレイヤーのみ取得させるようにする
    public CS_CharacterState playerState => _playerParty.Count > 0 ? _playerParty[0] : null;

    // 敵の状態
    private readonly List<CS_CharacterState> _enemyParty;
    public IReadOnlyList<CS_CharacterState> enemyParty => _enemyParty;
    // ※ひとまず一番前の敵のみ取得させるようにする
    public CS_CharacterState enemyState => _enemyParty.Count > 0 ? _enemyParty[0] : null;

    // 行動順キュー
    private readonly Queue<CS_BattleActionEntry> _actionQueue = new Queue<CS_BattleActionEntry>();
    public Queue<CS_BattleActionEntry> actionQueue => _actionQueue;

    // コマンド
    public IBattleCommand playerCommand { get; set; }
    public IBattleCommand enemyCommand { get; set; }

    // 戦闘結果
    public CSE_BattleResult result { get; set; } = CSE_BattleResult.None;

    public CS_BattleContext(List<CS_CharacterState> playerParty, List<CS_CharacterState> enemyParty)
    {
        _playerParty = playerParty;
        _enemyParty = enemyParty;
    }
}