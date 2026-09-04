using System.Collections.Generic;
using UnityEngine;

public class CS_BattleContext
{
    // プレイヤーの状態
    private readonly List<CS_CharacterState> _allyParty;
    public IReadOnlyList<CS_CharacterState> allyParty => _allyParty;
    // プレイヤーが操作するキャラクターの状態
    public CS_CharacterState playerState => _allyParty.Count > 0 ? _allyParty[0] : null;
    // プレイヤー以外の味方キャラクターの状態
    public IReadOnlyList<CS_CharacterState> allyPartyWithoutPlayer => _allyParty.Count > 1 ? _allyParty.GetRange(1, _allyParty.Count - 1) : new List<CS_CharacterState>();

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