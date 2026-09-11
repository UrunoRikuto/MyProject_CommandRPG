using System;

/// <summary>
/// 所持アイテム/装備1種類分の(ID, 個数)。CS_GameManagerの所持品リストとセーブデータ両方で使う
/// </summary>
[Serializable]
public class CS_ItemStack
{
    public string id;
    public int count;
}
