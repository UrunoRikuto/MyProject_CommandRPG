using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 変数の値をリアルタイムで監視するシステム
/// Awake()やStart()で変数を登録するだけで、後はこのシステムが自動管理します
/// </summary>
public class CS_ValueObserver
{
    /// <summary>
    /// 監視対象の変数情報
    /// </summary>
    public class ObservedValue
    {
        /// <summary>変数が属するオブジェクト</summary>
        public GameObject Target { get; set; }

        /// <summary>変数が属するコンポーネント</summary>
        public MonoBehaviour Component { get; set; }

        /// <summary>変数の名前</summary>
        public string VariableName { get; set; }

        /// <summary>変数を取得するデリゲート</summary>
        public Func<object> GetValueFunc { get; set; }

        /// <summary>現在の値</summary>
        public object CurrentValue { get; set; }

        /// <summary>前回の値</summary>
        public object PreviousValue { get; set; }

        /// <summary>登録時刻</summary>
        public DateTime RegistrationTime { get; set; }

        /// <summary>コンポーネントが削除されたか</summary>
        public bool IsComponentDestroyed { get; set; }

        public ObservedValue()
        {
            RegistrationTime = DateTime.Now;
            IsComponentDestroyed = false;
        }
    }

    private static CS_ValueObserver _instance;
    private Dictionary<string, ObservedValue> _observedValues = new Dictionary<string, ObservedValue>();

    /// <summary>
    /// シングルトンインスタンスを取得
    /// </summary>
    public static CS_ValueObserver Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new CS_ValueObserver();
            }
            return _instance;
        }
    }

    /// <summary>
    /// 監視対象の変数を登録
    /// </summary>
    /// <param name="target">対象となるGameObject</param>
    /// <param name="component">変数が属するコンポーネント</param>
    /// <param name="variableName">変数の名前</param>
    /// <param name="getValueFunc">変数の値を取得するデリゲート</param>
    /// <returns>登録ID（後で参照する場合に使用）</returns>
    public string Register(GameObject target, MonoBehaviour component, string variableName, Func<object> getValueFunc)
    {
        if (target == null || component == null || string.IsNullOrEmpty(variableName) || getValueFunc == null)
        {
            Debug.LogWarning("ValueObserver: Invalid registration parameters");
            return null;
        }

        string registrationId = GenerateRegistrationId(target, component, variableName);

        ObservedValue observedValue = new ObservedValue
        {
            Target = target,
            Component = component,
            VariableName = variableName,
            GetValueFunc = getValueFunc
        };

        _observedValues[registrationId] = observedValue;
        UpdateValue(registrationId);

        return registrationId;
    }

    /// <summary>
    /// 監視対象の変数を登録解除
    /// </summary>
    /// <param name="registrationId">登録時に返されたID</param>
    public void Unregister(string registrationId)
    {
        if (_observedValues.ContainsKey(registrationId))
        {
            _observedValues.Remove(registrationId);
        }
    }

    /// <summary>
    /// 監視対象の変数の値を更新
    /// </summary>
    /// <param name="registrationId">登録ID</param>
    private void UpdateValue(string registrationId)
    {
        if (!_observedValues.ContainsKey(registrationId))
            return;

        ObservedValue observed = _observedValues[registrationId];

        // コンポーネントが削除されたかチェック
        if (observed.Component == null || observed.Target == null)
        {
            observed.IsComponentDestroyed = true;
            return;
        }

        observed.IsComponentDestroyed = false;
        observed.PreviousValue = observed.CurrentValue;

        try
        {
            observed.CurrentValue = observed.GetValueFunc.Invoke();
        }
        catch (Exception ex)
        {
            observed.CurrentValue = $"Error: {ex.Message}";
            Debug.LogWarning($"ValueObserver: Error getting value for {registrationId}\n{ex}");
        }
    }

    /// <summary>
    /// すべての監視対象の値を更新
    /// </summary>
    public void UpdateAllValues()
    {
        List<string> keysToRemove = new List<string>();

        foreach (var kvp in _observedValues)
        {
            UpdateValue(kvp.Key);

            // 削除されたコンポーネントはリストから削除
            if (kvp.Value.IsComponentDestroyed)
            {
                keysToRemove.Add(kvp.Key);
            }
        }

        foreach (var key in keysToRemove)
        {
            _observedValues.Remove(key);
        }
    }

    /// <summary>
    /// すべての監視対象の値を取得
    /// </summary>
    public Dictionary<string, ObservedValue> GetAllObservedValues()
    {
        return new Dictionary<string, ObservedValue>(_observedValues);
    }

    /// <summary>
    /// 監視対象の数を取得
    /// </summary>
    public int GetObservedValueCount()
    {
        return _observedValues.Count;
    }

    /// <summary>
    /// 登録IDを生成
    /// </summary>
    private string GenerateRegistrationId(GameObject target, MonoBehaviour component, string variableName)
    {
        return $"{target.name}_{component.GetType().Name}_{variableName}";
    }

    /// <summary>
    /// インスタンスをクリア（エディタの終了時など）
    /// </summary>
    public void Clear()
    {
        _observedValues.Clear();
    }
}
