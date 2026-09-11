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

    // 装備中の装備ID(CS_ItemDatabaseで解決する)。未装備は空文字
    public string equippedWeaponId = "";
    public string equippedArmorId = "";
    public string equippedAccessoryId = "";
}
