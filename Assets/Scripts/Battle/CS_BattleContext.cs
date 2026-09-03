using System.Collections.Generic;
using UnityEngine;

public class CS_BattleContext
{
    // プレイヤーの状態
    private readonly List<CS_CharacterState> _playerParty;
    public IReadOnlyList<CS_CharacterState> playerParty => _playerParty;
    // プレイヤーが操作するキャラクターの状態
    public CS_CharacterState playerState => _playerParty.Count > 0 ? _playerParty[0] : null;

    // 敵の状態
    private readonly List<CS_CharacterState> _enemyParty;
    public IReadOnlyList<CS_CharacterState> enemyParty => _enemyParty;

    // 行動順キュー
    private readonly Queue<CS_BattleActionEntry> _actionQueue = new Queue<CS_BattleActionEntry>();
    public Queue<CS_BattleActionEntry> actionQueue => _actionQueue;


    public IReadOnlyList<CS_CharacterState> GetOpposingParty(CS_CharacterState actor)
    {
        return _playerParty.Contains(actor) ? (IReadOnlyList<CS_CharacterState>)_enemyParty : _playerParty;
    }

    public CS_CharacterState PickRandomLivingTarget(IReadOnlyList<CS_CharacterState> party)
    {
        List<CS_CharacterState> living = new List<CS_CharacterState>();
        foreach (var c in party)
        {
            if (!c.isDead) living.Add(c);
        }
        return living.Count > 0 ? living[Random.Range(0, living.Count)] : null;
    }

    // 戦闘結果
    public CSE_BattleResult result { get; set; } = CSE_BattleResult.None;

    public CS_BattleContext(List<CS_CharacterState> playerParty, List<CS_CharacterState> enemyParty)
    {
        _playerParty = playerParty;
        _enemyParty = enemyParty;
    }
}