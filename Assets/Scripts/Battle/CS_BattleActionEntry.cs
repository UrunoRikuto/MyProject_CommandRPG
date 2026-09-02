/// <summary>
/// 行動順キューの1件分。「誰が」「誰に」「何をするか」を保持するだけの単純なデータ保持クラス。
/// </summary>
public class CS_BattleActionEntry
{
    private readonly CS_CharacterState _actor;
    public CS_CharacterState actor => _actor;

    private readonly CS_CharacterState _target;
    public CS_CharacterState target => _target;

    private readonly IBattleCommand _command;
    public IBattleCommand command => _command;

    public CS_BattleActionEntry(CS_CharacterState actor, CS_CharacterState target, IBattleCommand command)
    {
        _actor = actor;
        _target = target;
        _command = command;
    }
}
