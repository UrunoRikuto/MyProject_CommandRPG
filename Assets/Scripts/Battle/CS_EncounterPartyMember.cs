/// <summary>
/// フィールドモンスターとの接触で組み立てる敵パーティ1体分。種族とレベルは呼び出し側で
/// 既に抽選済みの値を渡す(混成パーティは段階ごとにレベル範囲が異なるため、
/// 単一のCSO_EncounterDataだけでは表現できず、この形で個別に受け渡す)
/// </summary>
public class CS_EncounterPartyMember
{
    public readonly CSO_CharacterData character;
    public readonly int level;

    public CS_EncounterPartyMember(CSO_CharacterData character, int level)
    {
        this.character = character;
        this.level = level;
    }
}
