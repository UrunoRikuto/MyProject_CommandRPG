/// <summary>
/// 素早さを比較して行動順を決定し、行動キューを作成する。
/// 同速の場合はプレイヤーを先に行動させる。
/// </summary>
public class CS_BattleStateActionOrder : IBattleState
{
    public void Enter(CS_BattleContext context, CS_BattleStateMachine machine)
    {
        bool playerFirst = context.playerState.currentSpeed >= context.enemyState.currentSpeed;

        if (playerFirst)
        {
            context.actionQueue.Enqueue(new CS_BattleActionEntry(context.playerState, context.enemyState, context.playerCommand));
            context.actionQueue.Enqueue(new CS_BattleActionEntry(context.enemyState, context.playerState, context.enemyCommand));
        }
        else
        {
            context.actionQueue.Enqueue(new CS_BattleActionEntry(context.enemyState, context.playerState, context.enemyCommand));
            context.actionQueue.Enqueue(new CS_BattleActionEntry(context.playerState, context.enemyState, context.playerCommand));
        }

        machine.ChangeState(new CS_BattleStateActionExecute());
    }

    public void Update(CS_BattleContext context, CS_BattleStateMachine machine) { }
    public void Exit(CS_BattleContext context, CS_BattleStateMachine machine) { }
}
