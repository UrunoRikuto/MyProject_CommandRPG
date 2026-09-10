using System.Collections;
using UnityEngine;

/// <summary>
/// 戦闘中の演出(弾のエフェクトなど)をまとめて再生する。BattleSceneに配置する
/// </summary>
public class CS_BattleEffectPlayer : MonoBehaviour
{
    [SerializeField] private CS_BattleProjectile _projectilePrefab;
    [SerializeField] private Transform _effectParent;

    private const float FLIGHT_DURATION = 0.35f;

    /// <summary>
    /// startからendへ、属性に応じた色の弾を飛ばす。飛び終わるまで待機できるIEnumeratorを返す
    /// </summary>
    public IEnumerator PlayProjectile(Vector3 start, Vector3 end, CSE_ElementType element)
    {
        if (_projectilePrefab == null) yield break;

        Transform parent = _effectParent != null ? _effectParent : transform;
        CS_BattleProjectile projectile = Instantiate(_projectilePrefab, parent);
        projectile.SetColor(GetElementColor(element));

        yield return projectile.Fly(start, end, FLIGHT_DURATION);

        Destroy(projectile.gameObject);
    }

    /// <summary>
    /// 属性ごとの弾の色。無属性(たたかう)は白
    /// </summary>
    private static Color GetElementColor(CSE_ElementType element)
    {
        switch (element)
        {
            case CSE_ElementType.Fire: return new Color(1f, 0.4f, 0.15f);
            case CSE_ElementType.Water: return new Color(0.2f, 0.5f, 1f);
            case CSE_ElementType.Grass: return new Color(0.3f, 0.8f, 0.3f);
            case CSE_ElementType.Ice: return new Color(0.6f, 0.9f, 1f);
            case CSE_ElementType.Thunder: return new Color(1f, 0.9f, 0.2f);
            case CSE_ElementType.Earth: return new Color(0.6f, 0.45f, 0.25f);
            case CSE_ElementType.Light: return new Color(1f, 0.95f, 0.7f);
            case CSE_ElementType.Dark: return new Color(0.4f, 0.2f, 0.5f);
            default: return Color.white;
        }
    }
}
