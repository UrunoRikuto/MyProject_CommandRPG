using UnityEngine;

/// <summary>
/// フィールドを徘徊するモンスター。プレイヤーと接触すると戦闘を開始する。
/// エンカウントエリア(CS_EncounterSymbol)とは別の、見える形のエンカウント手段
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class CS_FieldMonster : MonoBehaviour
{
    [Header("この個体が表すエンカウントデータ(地域・段階ごとに1種)")]
    [SerializeField] private CSO_EncounterData _encounterData;

    [Header("移動速度")]
    [SerializeField] private float _moveSpeed = 1.2f;

    [Header("出現地点からの徘徊範囲")]
    [SerializeField] private float _wanderRadius = 3f;

    [Header("方向を選び直す間隔(秒)")]
    [SerializeField] private float _directionChangeInterval = 2f;

    [Header("倒してから再出現するまでの時間(秒)")]
    [SerializeField] private float _respawnTime = 20f;

    [Header("物理衝突用コライダー(壁で止まる)")]
    [SerializeField] private Collider2D _bodyCollider;

    [Header("プレイヤー接触検知用コライダー(トリガー)")]
    [SerializeField] private Collider2D _triggerCollider;

    private Rigidbody2D _rigidbody;
    private SpriteRenderer _spriteRenderer;

    private Vector2 _spawnPosition;
    private Vector2 _direction;
    private float _directionTimer;
    private bool _isDefeated;
    private float _respawnTimer;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        // 見た目はキャラクターアイコンの実寸に合わせて縮小した子オブジェクト(Visual)側にある。
        // 当たり判定(コライダー)はルート側の等倍スケールのまま独立して保つため
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void Start()
    {
        _spawnPosition = _rigidbody.position;

        if (_encounterData != null && _encounterData.enemyDataList.Count > 0)
        {
            _spriteRenderer.sprite = _encounterData.enemyDataList[0].characterIcon;
        }
        else
        {
            Debug.LogWarning($"{name}にエンカウントデータが設定されていません。");
        }

        PickNewDirection();
    }

    private void Update()
    {
        if (_isDefeated)
        {
            _respawnTimer -= Time.deltaTime;
            if (_respawnTimer <= 0f)
            {
                Respawn();
            }
            return;
        }

        _directionTimer -= Time.deltaTime;
        if (_directionTimer <= 0f)
        {
            PickNewDirection();
        }
    }

    private void FixedUpdate()
    {
        if (_isDefeated) return;

        Vector2 nextPosition = _rigidbody.position + _direction * _moveSpeed * Time.fixedDeltaTime;

        // 出現地点から離れすぎたら、出現地点へ戻る方向に切り替える(リーシュ)
        if (Vector2.Distance(nextPosition, _spawnPosition) > _wanderRadius)
        {
            _direction = (_spawnPosition - _rigidbody.position).normalized;
            nextPosition = _rigidbody.position + _direction * _moveSpeed * Time.fixedDeltaTime;
        }

        _rigidbody.MovePosition(nextPosition);
    }

    /// <summary>
    /// ランダムな方向を選び直す。一定確率でその場に立ち止まる(ふらふら歩く動きを出すため)
    /// </summary>
    private void PickNewDirection()
    {
        _directionTimer = _directionChangeInterval + Random.Range(-0.5f, 0.5f);

        if (Random.value < 0.25f)
        {
            _direction = Vector2.zero;
            return;
        }

        float angle = Random.Range(0f, Mathf.PI * 2f);
        _direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (_isDefeated) return;
        if (collision.GetComponent<CS_PlayerMove>() == null) return;
        if (_encounterData == null) return;

        HideForBattle();
        CS_GameManager.Instance.RequestBattle(_encounterData);
    }

    /// <summary>
    /// 戦闘に入る際、見た目と当たり判定を隠す。勝敗・にげる、いずれの結果でも
    /// 一定時間後に同じ場所へRespawnする(戦闘中はFieldEnvironmentごと非アクティブになるため、
    /// このタイマーはフィールドに戻ってから実際に経過した時間だけをカウントする)
    /// </summary>
    private void HideForBattle()
    {
        _isDefeated = true;
        _respawnTimer = _respawnTime;
        _spriteRenderer.enabled = false;
        _bodyCollider.enabled = false;
        _triggerCollider.enabled = false;
    }

    private void Respawn()
    {
        _isDefeated = false;
        _rigidbody.position = _spawnPosition;
        _spriteRenderer.enabled = true;
        _bodyCollider.enabled = true;
        _triggerCollider.enabled = true;
        PickNewDirection();
    }
}
