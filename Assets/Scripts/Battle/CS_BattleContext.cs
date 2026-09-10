using System.Collections.Generic;
using UnityEngine;

public class CS_BattleContext
{
    // 味方の状態(全員ボタン操作で行動を決定する)
    private readonly List<CS_CharacterState> _allyParty;
    public IReadOnlyList<CS_CharacterState> allyParty => _allyParty;

    // 敵の状態
    private readonly List<CS_CharacterState> _enemyParty;
    public IReadOnlyList<CS_CharacterState> enemyParty => _enemyParty;

    // 行動順キュー
    private readonly Queue<CS_BattleActionEntry> _actionQueue = new Queue<CS_BattleActionEntry>();
    public Queue<CS_BattleActionEntry> actionQueue => _actionQueue;

    // 指定したキャラクターの所属するパーティーの相手側のパーティーを返す
    public IReadOnlyList<CS_CharacterState> GetOpposingParty(CS_CharacterState actor)
    {
        return _allyParty.Contains(actor) ? (IReadOnlyList<CS_CharacterState>)_enemyParty : _allyParty;
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
        _allyParty = playerParty;
        _enemyParty = enemyParty;
    }
}