using UnityEngine;

/// <summary>
/// このフィールドシーンをCSED_FieldMapGeneratorで生成したときのSeed値を記録しておくだけの
/// マーカー。ゲームロジックには関与しない、エディタツールが同じ地形を再生成するための情報保持用
/// </summary>
public class CS_FieldMapInfo : MonoBehaviour
{
    public int seed;
}
