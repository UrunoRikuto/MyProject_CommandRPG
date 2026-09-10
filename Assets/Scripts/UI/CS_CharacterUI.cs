using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class CS_CharacterUI : MonoBehaviour
{
    [SerializeField]
    private Image _hpFillImage;

    [SerializeField]
    private Image _mpFillImage;

    [SerializeField]
    private RectTransform _hpBarRect;

    [SerializeField]
    private RectTransform _mpBarRect;

    [SerializeField]
    private RectTransform _turnIndicatorRect;

    [SerializeField]
    [Header("HP/MPバーが1秒あたり動ける割合(0〜1)")]
    private float _barAnimSpeed = 1.0f;

    // 戦闘不能になったキャラクターのアイコンをグレーアウト+半透明にする色と、その変化速度
    private static readonly Color ALIVE_TINT = Color.white;
    private static readonly Color DEAD_TINT = new Color(0.4f, 0.4f, 0.4f, 0.5f);
    private const float TINT_LERP_SPEED = 4f;

    private Image _characterImage;
    public Image characterImage => _characterImage;

    private CS_CharacterState _characterState;
    private Vector2 _turnIndicatorBasePos;

    // 実際に表示中のfillAmount。目標値(現在HP/MP)へ毎フレーム少しずつ近づけることでバーの減少に滑らかさを出す
    private float _displayedHpFill;
    private float _displayedMpFill;
    private bool _fillInitialized;

    private void Awake()
    {
        _characterImage = GetComponent<Image>();
        if (_turnIndicatorRect != null)
        {
            _turnIndicatorBasePos = _turnIndicatorRect.anchoredPosition;
            _turnIndicatorRect.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 行動選択中であることを示すマーカーの表示/非表示を切り替える
    /// </summary>
    public void SetTurnIndicator(bool active)
    {
        if (_turnIndicatorRect != null)
        {
            _turnIndicatorRect.gameObject.SetActive(active);
        }
    }

    public void SetCharacterState(CS_CharacterState characterState)
    {
        _characterState = characterState;
    }

    /// <summary>
    /// 敵側の場合はHP/MPバーをアイコンの下側(画面中央向き)に反転配置する。
    /// 単純に符号反転するとHP/MPの上下関係まで入れ替わってしまうため、
    /// Y座標をHP/MP間で入れ替えたうえで反転し、HPが常に上に来るようにする
    /// </summary>
    public void SetTeamSide(bool isEnemy)
    {
        if (!isEnemy)
        {
            return;
        }

        float hpY = _hpBarRect.anchoredPosition.y;
        float mpY = _mpBarRect.anchoredPosition.y;

        _hpBarRect.anchoredPosition = new Vector2(_hpBarRect.anchoredPosition.x, -mpY);
        _mpBarRect.anchoredPosition = new Vector2(_mpBarRect.anchoredPosition.x, -hpY);
    }

    private void Update()
    {
        if (_characterState == null)
        {
            return;
        }

        float targetHpFill = _characterState.maxHealth > 0
            ? (float)_characterState.currentHealth / _characterState.maxHealth
            : 0f;

        float targetMpFill = _characterState.maxMP > 0
            ? (float)_characterState.currentMP / _characterState.maxMP
            : 0f;

        if (!_fillInitialized)
        {
            // 初回表示時はいきなり0からアニメーションしてしまわないよう、即座に現在値へ合わせる
            _displayedHpFill = targetHpFill;
            _displayedMpFill = targetMpFill;
            _fillInitialized = true;
        }

        _displayedHpFill = Mathf.MoveTowards(_displayedHpFill, targetHpFill, _barAnimSpeed * Time.deltaTime);
        _displayedMpFill = Mathf.MoveTowards(_displayedMpFill, targetMpFill, _barAnimSpeed * Time.deltaTime);

        _hpFillImage.fillAmount = _displayedHpFill;
        _mpFillImage.fillAmount = _displayedMpFill;

        // 戦闘不能になったキャラクターは徐々にグレーアウト+半透明にして一目で分かるようにする
        Color targetTint = _characterState.isDead ? DEAD_TINT : ALIVE_TINT;
        _characterImage.color = Color.Lerp(_characterImage.color, targetTint, TINT_LERP_SPEED * Time.deltaTime);

        if (_turnIndicatorRect != null && _turnIndicatorRect.gameObject.activeSelf)
        {
            float bob = Mathf.Sin(Time.time * 4f) * 6f;
            _turnIndicatorRect.anchoredPosition = _turnIndicatorBasePos + new Vector2(0, bob);
        }
    }
}
