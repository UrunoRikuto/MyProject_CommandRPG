using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 攻撃者から対象へ、ベジェ曲線を描いて飛んでいく弾のエフェクト
/// </summary>
[RequireComponent(typeof(Image))]
public class CS_BattleProjectile : MonoBehaviour
{
    private RectTransform _rectTransform;
    private Image _image;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _image = GetComponent<Image>();
    }

    public void SetColor(Color color)
    {
        _image.color = color;
    }

    /// <summary>
    /// startからendまで、中間点を持ち上げた2次ベジェ曲線に沿って移動する
    /// </summary>
    public IEnumerator Fly(Vector3 start, Vector3 end, float duration)
    {
        Vector3 control = ComputeControlPoint(start, end);
        _rectTransform.position = start;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            _rectTransform.position = QuadraticBezier(start, control, end, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        _rectTransform.position = end;
    }

    /// <summary>
    /// 弧を描かせるため、始点と終点の中間点を距離に応じた高さだけ持ち上げた点を制御点にする
    /// </summary>
    private static Vector3 ComputeControlPoint(Vector3 start, Vector3 end)
    {
        Vector3 mid = (start + end) * 0.5f;
        float arcHeight = Vector3.Distance(start, end) * 0.35f;
        return mid + Vector3.up * arcHeight;
    }

    private static Vector3 QuadraticBezier(Vector3 p0, Vector3 control, Vector3 p2, float t)
    {
        float u = 1f - t;
        return u * u * p0 + 2f * u * t * control + t * t * p2;
    }
}
