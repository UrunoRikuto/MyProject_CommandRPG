using UnityEngine;

/// <summary>
/// フィールドで町へ戻る出入口(CS_SceneEntrance)の方向を指す画面端の矢印。
/// 画面中央から見た方向を計算し、その方向の画面の縁(内側に少し余白を取った位置)に
/// 矢印を移動・回転させ続ける、いわゆるオフスクリーンインジケーター方式
/// </summary>
public class CS_TownCompass : MonoBehaviour
{
    [SerializeField] private RectTransform _arrow;

    // CSED_FieldMapGeneratorが設定するCanvasのReference Resolution(1280x720)の半分のサイズに合わせてある。
    // 矢印はこの範囲からEDGE_MARGIN分だけ内側の縁に配置する
    private const float HALF_WIDTH = 640f;
    private const float HALF_HEIGHT = 360f;
    private const float EDGE_MARGIN = 60f;

    private Transform _player;
    private Transform _target;
    private Camera _camera;

    private void Start()
    {
        CS_PlayerMove playerMove = GameObject.FindAnyObjectByType<CS_PlayerMove>();
        _player = playerMove != null ? playerMove.transform : null;

        CS_SceneEntrance entrance = GameObject.FindAnyObjectByType<CS_SceneEntrance>();
        _target = entrance != null ? entrance.transform : null;

        _camera = Camera.main;

        // 町シーンなど、指す対象(町への出入口)が無いシーンでは表示しない
        if (_player == null || _target == null || _camera == null)
        {
            gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        // 出入口自体が画面に映っている間は、矢印を隠す
        if (IsTargetOnScreen())
        {
            _arrow.gameObject.SetActive(false);
            return;
        }
        _arrow.gameObject.SetActive(true);

        Vector2 direction = (Vector2)(_target.position - _player.position);
        if (direction.sqrMagnitude < 0.0001f) return;

        direction.Normalize();

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        // UI_TurnArrow.pngはデフォルトで下向き(-90度)のため、その分を補正する
        _arrow.localEulerAngles = new Vector3(0f, 0f, angle + 90f);

        _arrow.anchoredPosition = ClampToScreenEdge(direction);
    }

    /// <summary>
    /// 出入口がカメラのビューポート内(奥行きも含めて手前側)にあるかどうかを判定する
    /// </summary>
    private bool IsTargetOnScreen()
    {
        Vector3 viewportPoint = _camera.WorldToViewportPoint(_target.position);
        return viewportPoint.z > 0f
            && viewportPoint.x >= 0f && viewportPoint.x <= 1f
            && viewportPoint.y >= 0f && viewportPoint.y <= 1f;
    }

    /// <summary>
    /// 画面中央からdirection方向へ進んだ先が、画面の縁(EDGE_MARGIN分内側)に
    /// ちょうど到達する位置を計算する(矩形とレイの交点を求める、定番のオフスクリーン表示方式)
    /// </summary>
    private Vector2 ClampToScreenEdge(Vector2 direction)
    {
        float maxX = HALF_WIDTH - EDGE_MARGIN;
        float maxY = HALF_HEIGHT - EDGE_MARGIN;

        float tx = Mathf.Abs(direction.x) > 0.0001f ? maxX / Mathf.Abs(direction.x) : float.MaxValue;
        float ty = Mathf.Abs(direction.y) > 0.0001f ? maxY / Mathf.Abs(direction.y) : float.MaxValue;
        float t = Mathf.Min(tx, ty);

        return direction * t;
    }
}
