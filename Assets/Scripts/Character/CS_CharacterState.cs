using UnityEngine;

public class CS_CharacterState
{
    // キャラクターの基礎データ
    private CSO_CharacterData _characterData;

    // キャラクターが死亡しているかどうか
    public bool isDead => _currentHealth <= 0;

    // キャラクター名
    public string characterName => _characterData.characterName;

    // 最大体力
    private int _maxHealth;
    public int maxHealth => _maxHealth;

    // 現在の体力
    private int _currentHealth;
    public int currentHealth => _currentHealth;

    // 現在の攻撃力
    private int _currentAttack;
    public int currentAttack => _currentAttack;

    // 現在の防御力
    private int _currentDefense;
    public int currentDefense => _currentDefense;

    // 現在の速度
    private int _currentSpeed;
    public int currentSpeed => _currentSpeed;

    public CS_CharacterState(CSO_CharacterData data)
    {
        _characterData = data;

        // 体力の初期化
        _maxHealth = _characterData.baseHealth;
        _currentHealth = _maxHealth;

        // 攻撃力の初期化
        _currentAttack = _characterData.baseAttack;

        // 防御力の初期化
        _currentDefense = _characterData.baseDefense;

        // 速度の初期化
        _currentSpeed = _characterData.baseSpeed;
    }

    /// <summary>
    /// ダメージを受ける処理
    /// </summary>
    /// <param name="damage">受けるダメージ量</param>
    public void TakeDamage(int damage)
    {
        // 防御力を考慮した実際のダメージ量を計算
        int effectiveDamage = Mathf.Max(damage - _currentDefense, 1);

        // 乱数を加えてダメージ量を変動させる（例: ±10%の範囲で変動）
        float randomFactor = Random.Range(0.9f, 1.1f);
        effectiveDamage = Mathf.RoundToInt(effectiveDamage * randomFactor);

        // 現在の体力を減少させる
        _currentHealth = Mathf.Max(_currentHealth - effectiveDamage, 0);
        Debug.Log(characterName + "がダメージを受けた: " + effectiveDamage + " 現在の体力: " + _currentHealth);
    }
}
