public interface IBattleCommand
{
    /// <summary>表示用のコマンド名</summary>
    string commandName { get; }

    /// <summary>行動を実行する。</summary>
    /// <param name="user">行動する側</param>
    /// <param name="target">対象</param>
    void Execute(CS_CharacterState user, CS_CharacterState target);
}
