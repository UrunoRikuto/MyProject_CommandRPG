using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 味方全員をボタン操作で1体ずつ(素早さ降順で)行動決定させる。敵はAIで即決定する。
/// </summary>
public class CS_BattleStateCommandInput : IBattleState
{
    private CS_BattleContext _context;
    private CS_BattleStateMachine _machine;
    private Queue<CS_CharacterState> _pendingAllies;
    private CS_CharacterState _currentActor;

    public void Enter(CS_BattleContext context, CS_BattleStateMachine machine)
    {
        RegenMPForRound(context);

        DecideActionsForParty(context, context.enemyParty);

        _context = context;
        _machine = machine;

        _pendingAllies = new Queue<CS_CharacterState>(
            context.allyParty.Where(a => !a.isDead).OrderByDescending(a => a.currentSpeed));

        machine.commandButtonInput.onCommandDecided += HandleCommandDecided;

        PromptNextAlly();
    }

    /// <summary>
    /// ラウンド開始時に、生存している全キャラクター(味方・敵とも)のMPを自然回復させる
    /// </summary>
    private void RegenMPForRound(CS_BattleContext context)
    {
        foreach (var ally in context.allyParty)
        {
            if (!ally.isDead) ally.RegenMP();
        }
        foreach (var enemy in context.enemyParty)
        {
            if (!enemy.isDead) enemy.RegenMP();
        }
    }

    private void DecideActionsForParty(CS_BattleContext context, IReadOnlyList<CS_CharacterState> party)
    {
        foreach (var actor in party)
        {
            if (actor.isDead) continue;

            IBattleCommand command = CS_BattleAI.DecideCommand(actor);
            CS_CharacterState target = context.PickRandomLivingTarget(context.GetOpposingParty(actor));
            if (target == null) continue;

            context.actionQueue.Enqueue(new CS_BattleActionEntry(actor, target, command));
        }
    }

    /// <summary>
    /// 行動未決定の味方が残っていれば次の1体のUIを開く。いなければ行動順決定へ進む
    /// </summary>
    private void PromptNextAlly()
    {
        if (_pendingAllies.Count == 0)
        {
            _machine.characterUIWindow.SetActingCharacter(null);
            _machine.ChangeState(new CS_BattleStateActionOrder());
            return;
        }

        _currentActor = _pendingAllies.Dequeue();

        _machine.characterUIWindow.SetActingCharacter(_currentActor);
        _machine.commandButtonInput.SetAvailableSkills(_currentActor.currentSkills);
        _machine.commandButtonInput.SetAvailableTargets(_context.enemyParty);
        _machine.commandButtonInput.SetAvailableAllyTargets(_context.allyParty);
        _machine.commandButtonInput.SetAvailableItems(GetUsableItems());
        _machine.commandButtonInput.Show();
    }

    /// <summary>
    /// 現在使用可能などうぐの一覧を返す。デバッグモード中は全アイテムを無制限(count=-1)で、
    /// 通常時は所持数が1以上のものだけを実際の所持数付きで返す
    /// </summary>
    private List<(CSO_ItemData item, int count)> GetUsableItems()
    {
        var result = new List<(CSO_ItemData item, int count)>();

        if (CS_GameManager.Instance.isDebugMode)
        {
            foreach (var item in CS_ItemDatabase.AllItems)
            {
                result.Add((item, -1));
            }
            return result;
        }

        foreach (var stack in CS_GameManager.Instance.ownedItemsList)
        {
            if (stack.count <= 0) continue;
            var item = CS_ItemDatabase.GetItem(stack.id);
            if (item != null) result.Add((item, stack.count));
        }
        return result;
    }

    private void HandleCommandDecided(IBattleCommand command, CS_CharacterState target)
    {
        // にげるはtarget==nullで届くので、素早さ比較の基準として生存中の敵からランダムに1体補う
        CS_CharacterState resolvedTarget = target ?? _context.PickRandomLivingTarget(_context.enemyParty);
        if (resolvedTarget != null)
        {
            _context.actionQueue.Enqueue(new CS_BattleActionEntry(_currentActor, resolvedTarget, command));
        }

        _machine.commandButtonInput.Hide();
        PromptNextAlly();
    }

    public void Update(CS_BattleContext context, CS_BattleStateMachine machine) { }

    public void Exit(CS_BattleContext context, CS_BattleStateMachine machine)
    {
        machine.commandButtonInput.onCommandDecided -= HandleCommandDecided;
        machine.commandButtonInput.Hide();
    }
}
