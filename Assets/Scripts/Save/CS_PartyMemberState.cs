using System;

/// <summary>
/// パーティメンバー1人分の現在HP/MP/レベル。CS_GameManagerでの保持とセーブデータ両方で使う
/// </summary>
[Serializable]
public class CS_PartyMemberState
{
    public string characterName;
    public int currentHealth;
    public int currentMP;
    public int level = 1;
    public int exp = 0;
}
