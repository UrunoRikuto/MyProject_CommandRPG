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

        // 速度の初期化
        _currentSpeed = _characterData.baseSpeed;
    }

    /// <summary>
    /// ダメージを受ける処理
    /// </summary>
    /// <param name="damage">受けるダメージ量</param>
    public void TakeDamage(int damage)
    {
        _currentHealth = Mathf.Max(_currentHealth - damage, 0);
    }
}
