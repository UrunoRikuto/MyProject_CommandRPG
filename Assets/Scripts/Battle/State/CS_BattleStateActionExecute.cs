using System.Collections;
using UnityEngine;

/// <summary>
/// 行動順キューを1件ずつ実行する。攻撃・スキルには弾のエフェクトを再生してから
/// ダメージを適用するため、コルーチンで進行する
/// </summary>
public class CS_BattleStateActionExecute : IBattleState
{
    public void Enter(CS_BattleContext context, CS_BattleStateMachine machine)
    {
        machine.StartCoroutine(ExecuteQueue(context, machine));
    }

    private IEnumerator ExecuteQueue(CS_BattleContext context, CS_BattleStateMachine machine)
    {
        while (context.actionQueue.Count > 0)
        {
            CS_BattleActionEntry entry = context.actionQueue.Dequeue();
            if (entry.actor.isDead) continue;

            CS_CharacterState target = entry.target;
            if (target.isDead)
            {
                target = context.PickRandomLivingTarget(context.GetOpposingParty(entry.actor));
                if (target == null) continue; // 相手が全滅していたらこの行動はキャンセル
            }

            yield return PlayAttackEffect(machine, entry.actor, target, entry.command);

            entry.command.Execute(context, entry.actor, target);

            if (context.result != CSE_BattleResult.None) break;
        }

        machine.ChangeState(new CS_BattleStateJudgeResult());
    }

    /// <summary>
    /// たたかう/スキルの場合のみ、攻撃者から対象へ弾を飛ばす演出を再生する
    /// </summary>
    private IEnumerator PlayAttackEffect(CS_BattleStateMachine machine, CS_CharacterState actor, CS_CharacterState target, IBattleCommand command)
    {
        if (machine.effectPlayer == null) yield break;

        CSE_ElementType element;
        if (!TryGetAnimatedElement(actor, command, out element)) yield break;

        Vector3 start = machine.characterUIWindow.GetIconWorldPosition(actor);
        Vector3 end = machine.characterUIWindow.GetIconWorldPosition(target);
        yield return machine.effectPlayer.PlayProjectile(start, end, element);
    }

    /// <summary>
    /// アニメーション対象のコマンドかどうかを判定し、対象なら弾の色に使う属性を返す。
    /// スキルはMPを払えない(=実行しても不発になる)場合は演出しない
    /// </summary>
    private bool TryGetAnimatedElement(CS_CharacterState actor, IBattleCommand command, out CSE_ElementType element)
    {
        element = CSE_ElementType.None;

        if (command is CS_AttackCommand)
        {
            element = actor.attackElement;
            return true;
        }

        if (command is CS_SkillCommand skillCommand)
        {
            int index = skillCommand.skillIndex;
            if (index < 0 || index >= actor.currentSkills.Count) return false;

            CSO_SkillData skillData = actor.currentSkills[index];
            if (actor.currentMP < skillData.cost) return false;

            element = skillData.element;
            return true;
        }

        if (command is CS_ItemCommand itemCommand)
        {
            bool usable = CS_GameManager.Instance.isDebugMode
                || CS_GameManager.Instance.GetItemCount(itemCommand.itemData.itemId) > 0;
            if (!usable) return false;

            element = itemCommand.itemData.element;
            return true;
        }

        return false;
    }

    public void Update(CS_BattleContext context, CS_BattleStateMachine machine) { }
    public void Exit(CS_BattleContext context, CS_BattleStateMachine machine) { }
}
