using UnityEditor;
using UnityEngine;
using System.Linq;

/// <summary>
/// 登録された変数をリアルタイムで表示するエディタウィンドウ
/// </summary>
public class CS_ValueObserverWindow : EditorWindow
{
    private Vector2 _scrollPosition = Vector2.zero;
    private double _lastUpdateTime = 0;
    private const double UPDATE_INTERVAL = 0.1; // 100msごとに更新
    private bool _autoRefresh = true;
    private bool _showOnlyChanged = false;
    private string _filterText = "";
    private bool _showHelp = false;
    private bool _simpleMode = false; // 簡易表示モード

    [MenuItem("Tools/Value Observer")]
    public static void ShowWindow()
    {
        EditorWindow window = GetWindow(typeof(CS_ValueObserverWindow));
        window.titleContent = new GUIContent("Value Observer");
        window.minSize = new Vector2(550, 300);
    }

    private void OnEnable()
    {
        _lastUpdateTime = EditorApplication.timeSinceStartup;
        EditorApplication.update += OnEditorUpdate;
    }

    private void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
    }

    private void OnEditorUpdate()
    {
        if (!_autoRefresh)
            return;

        double currentTime = EditorApplication.timeSinceStartup;
        if (currentTime - _lastUpdateTime >= UPDATE_INTERVAL)
        {
            CS_ValueObserver.Instance.UpdateAllValues();
            _lastUpdateTime = currentTime;
            Repaint();
        }
    }

    private void OnGUI()
    {
        if (_showHelp)
        {
            DrawHelpView();
        }
        else
        {
            DrawToolbar();
            DrawValueList();
        }
    }

    /// <summary>
    /// ツールバーを描画
    /// </summary>
    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        _autoRefresh = GUILayout.Toggle(_autoRefresh, "自動更新", EditorStyles.toolbarButton, GUILayout.Width(100));

        if (GUILayout.Button("手動更新", EditorStyles.toolbarButton, GUILayout.Width(100)))
        {
            CS_ValueObserver.Instance.UpdateAllValues();
            Repaint();
        }

        _showOnlyChanged = GUILayout.Toggle(_showOnlyChanged, "変更のみ表示", EditorStyles.toolbarButton, GUILayout.Width(100));

        // 簡易表示モード切り替え
        string simpleModeLabel = _simpleMode ? "簡易表示🔸" : "詳細表示🔹";
        if (GUILayout.Button(simpleModeLabel, EditorStyles.toolbarButton, GUILayout.Width(100)))
        {
            _simpleMode = !_simpleMode;
        }

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("リセット", EditorStyles.toolbarButton, GUILayout.Width(80)))
        {
            CS_ValueObserver.Instance.Clear();
            Repaint();
        }

        if (GUILayout.Button("?", EditorStyles.toolbarButton, GUILayout.Width(30)))
        {
            _showHelp = true;
        }

        EditorGUILayout.EndHorizontal();

        // フィルター入力
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("フィルター:", GUILayout.Width(50));
        _filterText = EditorGUILayout.TextField(_filterText);
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// ヘルプビューを描画
    /// </summary>
    private void DrawHelpView()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        if (GUILayout.Button("← 戻る", EditorStyles.toolbarButton, GUILayout.Width(60)))
        {
            _showHelp = false;
        }
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

        EditorGUILayout.LabelField("Value Observer - ガイド", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        DrawHelpSection("📌 概要",
                "Value Observer はゲーム実行中の変数の値をリアルタイムで監視するエディタツールです。\n" +
                "デバッグやパフォーマンス調査に活用できます。");

        DrawHelpSection("🚀 基本的な使い方",
                "1. 監視したい変数をコンポーネントに登録\n" +
                "2. Value Observer ウィンドウを開く\n" +
                "3. リアルタイムで値の変化を確認\n\n" +
                "【登録コード例】\n" +
                "void Start() {\n" +
                "  CS_ValueObserver.Instance.Register(\n" +
                "    gameObject,\n" +
                "    this,\n" +
                "    \"speed\",// 表示名\n" +
                "    () => speed  // ラムダ式で値を返す\n" +
                "  );\n" +
                "}");

        DrawHelpSection("📂 ファイル構造",
                "Assets/\n" +
                "    Script/\n" +
                "      CS_ValueObserver.cs        // 監視システム本体\n" +
                "    Editor/\n" +
                "      CS_ValueObserverWindow.cs  // エディタウィンドウ\n");


        DrawHelpSection("📊 ウィンドウの機能",
                "• 自動更新: 自動でリアルタイム更新（推奨：ON）\n" +
                "• 手動更新: 手動更新\n" +
                "• 変更のみ表示: 値が変わったもののみ表示\n" +
                "• 簡易表示/詳細表示: 表示モードの切り替え\n" +
                "• フィルター: 表示名orスクリプト名で検索\n" +
                "• ?: ガイドの表示\n" +
                "• リセット: すべての登録を削除");

        DrawHelpSection("🎨 色の意味",
                "• 黄色: 値が変化した\n" +
                "• 赤色: コンポーネントが削除された\n" +
                "• オレンジ色: 値が null");

        DrawHelpSection("📋 表示モード",
                "• 詳細表示: 詳細な情報を表示（対象、登録時刻、前回の値など）\n" +
                "• 簡易表示: 表示名と値のみを一行で表示（高速確認用）");

        DrawHelpSection("💡 応用例",
                "【複数の値を登録】\n" +
                "Register(gameObject, this, \"speed\", () => speed);\n" +
                "Register(gameObject, this, \"health\", () => health);\n" +
                "Register(gameObject, this, \"ammo\", () => ammo);\n\n" +
                "【プロパティを監視】\n" +
                "Register(gameObject, this, \"Position\", \n" +
                "  () => transform.position);\n\n" +
                "【他のコンポーネント】\n" +
                "Rigidbody rb = GetComponent<Rigidbody>();\n" +
                "Register(gameObject, rb, \"velocity\", \n" +
                "  () => rb.velocity);");

        DrawHelpSection("🔄 監視の解除",
                "通常はエディタを閉じると自動的に登録がクリアされますが、必要に応じて任意のタイミングで解除することも可能です。\n\n" +
                "string registrationId = Register(...);\n" +
                "// 実行中に任意のタイミングで削除する場合\n" +
                "CS_ValueObserver.Instance.Unregister(registrationId);");

        DrawHelpSection("⚠️ 注意点",
                "• 無限ループを避けるため、GetValueFunc は軽量な処理にしてください\n" +
                "• 複数のコンポーネントで同じ変数名を登録しても大丈夫です\n" +
                "• エディタを閉じると登録は自動的にクリアされます\n" +
                "• ゲーム実行中のみ値が更新されます");

        DrawHelpSection("🛠️ トラブルシューティング",
                "Q: 値が更新されない\n" +
                "A: Auto Refresh がONになっているか確認してください。\n" +
                "   または「Refresh Now」で手動更新してください。\n\n" +
                "Q: \"Error: ~\" と表示される\n" +
                "A: GetValueFunc の処理でエラーが発生しています。\n" +
                "   ラムダ式の内容を確認してください。");

        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// ヘルプセクションを描画
    /// </summary>
    private void DrawHelpSection(string title, string content)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        EditorGUILayout.LabelField(content, EditorStyles.wordWrappedLabel);

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();
    }

    /// <summary>
    /// 監視対象の値一覧を描画
    /// </summary>
    private void DrawValueList()
    {
        var observedValues = CS_ValueObserver.Instance.GetAllObservedValues();

        if (observedValues.Count == 0)
        {
            EditorGUILayout.HelpBox("No variables are being observed.\nRegister variables in Awake() or Start() using CS_ValueObserver.Instance.Register()\n\nClick the ? button for help.", MessageType.Info);
            return;
        }

        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

        EditorGUILayout.LabelField($"Observed Variables ({observedValues.Count})", EditorStyles.boldLabel);
        EditorGUILayout.Separator();

        // 簡易表示モードと詳細表示モードの切り替え
        if (_simpleMode)
        {
            DrawSimpleModeList(observedValues);
        }
        else
        {
            DrawDetailedModeList(observedValues);
        }

        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// 簡易表示モードで値一覧を描画
    /// </summary>
    private void DrawSimpleModeList(System.Collections.Generic.Dictionary<string, CS_ValueObserver.ObservedValue> observedValues)
    {
        foreach (var kvp in observedValues.OrderBy(x => x.Key))
        {
            if (!ShouldDisplayValue(kvp.Value, kvp.Key))
                continue;

            DrawSimpleValueEntry(kvp.Key, kvp.Value);
        }
    }

    /// <summary>
    /// 詳細表示モードで値一覧を描画
    /// </summary>
    private void DrawDetailedModeList(System.Collections.Generic.Dictionary<string, CS_ValueObserver.ObservedValue> observedValues)
    {
        foreach (var kvp in observedValues.OrderBy(x => x.Key))
        {
            if (!ShouldDisplayValue(kvp.Value, kvp.Key))
                continue;

            DrawValueEntry(kvp.Key, kvp.Value);
        }
    }

    /// <summary>
    /// 値を表示するかどうかを判定
    /// </summary>
    private bool ShouldDisplayValue(CS_ValueObserver.ObservedValue observedValue, string registrationId)
    {
        // フィルターテキストがある場合はマッチングをチェック
        if (!string.IsNullOrEmpty(_filterText))
        {
            if (!registrationId.ToLower().Contains(_filterText.ToLower()) &&
                !observedValue.VariableName.ToLower().Contains(_filterText.ToLower()))
            {
                return false;
            }
        }

        // 変更があった値のみを表示する場合
        if (_showOnlyChanged)
        {
            return !Equals(observedValue.CurrentValue, observedValue.PreviousValue);
        }

        return true;
    }

    /// <summary>
    /// 簡易表示：表示名と値のみを一行で表示
    /// </summary>
    private void DrawSimpleValueEntry(string registrationId, CS_ValueObserver.ObservedValue observedValue)
    {
        EditorGUILayout.BeginHorizontal();

        // 表示名
        EditorGUILayout.LabelField(observedValue.VariableName, GUILayout.Width(150));

        // 値の表示
        string valueDisplay = FormatValueForDisplay(observedValue.CurrentValue, observedValue.IsComponentDestroyed);
        string valueColor = GetValueColor(observedValue);

        EditorGUILayout.LabelField($"<color={valueColor}>{valueDisplay}</color>", new GUIStyle(EditorStyles.label) { richText = true });

        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 単一の値エントリを描画（詳細表示）
    /// </summary>
    private void DrawValueEntry(string registrationId, CS_ValueObserver.ObservedValue observedValue)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        // ヘッダー（オブジェクト名とコンポーネント名）
        string headerText = $"{observedValue.VariableName} ({observedValue.Component.GetType().Name})";
        EditorGUILayout.LabelField(headerText, EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("オブジェクト:", GUILayout.Width(80));
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.ObjectField(observedValue.Target, typeof(GameObject), true);
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();

        // 値の表示
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("値:", GUILayout.Width(80));

        string valueDisplay = FormatValueForDisplay(observedValue.CurrentValue, observedValue.IsComponentDestroyed);
        string valueColor = GetValueColor(observedValue);

        EditorGUILayout.LabelField($"<color={valueColor}>{valueDisplay}</color>", new GUIStyle(EditorStyles.label) { richText = true });
        EditorGUILayout.EndHorizontal();

        // 値の変化があった場合は表示
        if (!Equals(observedValue.CurrentValue, observedValue.PreviousValue) && observedValue.PreviousValue != null)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Previous:", GUILayout.Width(80));
            string previousDisplay = FormatValueForDisplay(observedValue.PreviousValue, false);
            EditorGUILayout.LabelField(previousDisplay, EditorStyles.label);
            EditorGUILayout.EndHorizontal();
        }

        // 登録時刻
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("登録時刻:", GUILayout.Width(80));
        EditorGUILayout.LabelField(observedValue.RegistrationTime.ToString("HH:mm:ss.fff"), EditorStyles.label);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();
    }

    /// <summary>
    /// 値を表示用にフォーマット
    /// </summary>
    private string FormatValueForDisplay(object value, bool isComponentDestroyed)
    {
        if (isComponentDestroyed)
            return "Component Destroyed";

        if (value == null)
            return "null";

        if (value is string str)
            return $"\"{str}\"";

        if (value is bool b)
            return b ? "true" : "false";

        if (value is float f)
            return f.ToString("F3");

        if (value is double d)
            return d.ToString("F3");

        if (value is Vector3 v3)
            return $"({v3.x:F2}, {v3.y:F2}, {v3.z:F2})";

        if (value is Vector2 v2)
            return $"({v2.x:F2}, {v2.y:F2})";

        if (value is Color c)
            return $"RGBA({c.r:F2}, {c.g:F2}, {c.b:F2}, {c.a:F2})";

        if (value is Object obj)
            return obj.name ?? obj.ToString();

        return value.ToString();
    }

    /// <summary>
    /// 値の状態に応じた色を取得
    /// </summary>
    private string GetValueColor(CS_ValueObserver.ObservedValue observedValue)
    {
        if (observedValue.IsComponentDestroyed)
            return "red";

        if (observedValue.CurrentValue == null)
            return "orange";

        if (!Equals(observedValue.CurrentValue, observedValue.PreviousValue) && observedValue.PreviousValue != null)
            return "yellow";

        return "white";
    }
}
