// Windowを作成
// SkillのScriptableObjectを参照して、全スキル数のリストを作成
// リストから任意のスキルを選択(クリック)する
// 起動元のキャラクターに選択したスキルをセットする

#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// スキル選択用のEditorWindow。
/// - `SkillSO`(スキルDB) からスキル配列を読み取り、一覧表示する
/// - 一覧から選択した `Skill` を呼び出し元へ返す（コールバック）
/// 
/// 注意:
/// - このクラスは「選択UI」を提供するだけで、キャラクターへ実際にセットする処理は呼び出し元で行う想定。
///   （例: `Open(skillSo, skill => character.SetSkill(skill));` のように使う）
/// - `#if UNITY_EDITOR` で囲んでいるため、ビルドには含まれない。
/// </summary>
public sealed class MasterySkillEditor : EditorWindow
{
    // ウィンドウのタイトル（GetWindowで表示される）
    private const string WindowTitle = "Mastery Skill Editor";

    // 既定で読み込むスキルDBのパス（ユーザー指定の `Assets/Data/SkillDB.asset`）
    private const string DefaultSkillDbAssetPath = "Assets/Data/SkillDB.asset";

    // スキルDB（ScriptableObject）。InspectorのObjectFieldで差し替え可能
    [SerializeField] private SkillSO _skillDatabase;

    // 起動元へ「選んだSkill」を返すためのコールバック
    private Action<Skill> _onSelected;

    // スクロール位置（スキル数が多い前提）
    private Vector2 _scroll;

    // いま選択中の行インデックス（-1は未選択）
    private int _selectedIndex = -1;

    /// <summary>
    /// 呼び出し元から開くためのAPI。
    /// `SkillSO` と、選択確定時の処理（コールバック）を渡す。
    /// 
    /// `skillDatabase` が null の場合は、既定パス（Assets/Data/SkillDB.asset）から自動ロードを試みる。
    /// </summary>
    public static MasterySkillEditor Open(SkillSO skillDatabase, Action<Skill> onSelected)
    {
        // ユーティリティウィンドウとして生成（モーダル寄りの小ウィンドウ）
        var window = GetWindow<MasterySkillEditor>(true, WindowTitle, true);

        // 最低サイズ（使い勝手のため）
        window.minSize = new Vector2(420, 520);

        // 初期化：コールバックを保持
        window._onSelected = onSelected;

        // 初期化：DBが渡されていなければ、既定のassetをロードしてみる
        window._skillDatabase = skillDatabase != null ? skillDatabase : window.LoadDefaultSkillDbIfNeeded();

        // 初期化：選択状態・スクロール位置をリセット
        window._selectedIndex = -1;
        window._scroll = Vector2.zero;

        // 表示
        window.ShowUtility();
        return window;
    }

    /// <summary>
    /// メニューから単体起動（動作確認用）。
    /// - まず `Assets/Data/SkillDB.asset` を自動で掴みにいく
    /// - 選択したスキル名をログ出力する
    /// </summary>
    [MenuItem("Tools/Mastery Skill Editor")]
    private static void OpenFromMenu()
    {
        Open(null, s =>
        {
            if (s != null) Debug.Log($"[MasterySkillEditor] Selected Skill: {s.strName}");
        });
    }

    /// <summary>
    /// ウィンドウが有効化されたタイミングで呼ばれる。
    /// - まだDBが未設定なら、既定パスのassetをロードする。
    /// </summary>
    private void OnEnable()
    {
        if (_skillDatabase == null)
        {
            _skillDatabase = LoadDefaultSkillDbIfNeeded();
        }
    }

    /// <summary>
    /// 既定パス `Assets/Data/SkillDB.asset` から `SkillSO` を読み込む。
    /// 読み込めない場合は null を返す。
    /// </summary>
    private SkillSO LoadDefaultSkillDbIfNeeded()
    {
        // AssetDatabase はEditor専用。パス直指定でロードする。
        var db = AssetDatabase.LoadAssetAtPath<SkillSO>(DefaultSkillDbAssetPath);
        return db;
    }

    /// <summary>
    /// Unity EditorがGUI描画を行うたびに呼ばれる。
    /// ここで一覧表示・詳細表示・ボタン処理を行う。
    /// </summary>
    private void OnGUI()
    {
        // 冒頭の説明枠
        DrawHeader();

        using (new EditorGUILayout.VerticalScope())
        {
            // `SkillSO` の参照欄（ObjectField）
            // ここで `Assets/Data/SkillDB.asset` をドラッグ＆ドロップして指定できる
            DrawDatabaseField();

            // DB未設定ならここで終了（既定パスに無い/型が違う等）
            if (_skillDatabase == null)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.HelpBox("`SkillSO`(スキルDB) を指定してください。", MessageType.Info);

                    // 既定パスを明示して、ユーザーが状況を把握できるようにする
                    EditorGUILayout.LabelField("既定パス", DefaultSkillDbAssetPath);

                    // ワンクリックで既定DBの再ロードを試せるようにする
                    if (GUILayout.Button("既定の SkillDB.asset を再ロード"))
                    {
                        _skillDatabase = LoadDefaultSkillDbIfNeeded();
                        _selectedIndex = -1;
                    }
                }

                return;
            }

            // DBはあるが配列が空ならここで終了
            if (_skillDatabase.skill == null || _skillDatabase.skill.Length == 0)
            {
                EditorGUILayout.HelpBox("指定された `SkillSO` にスキルが登録されていません。", MessageType.Warning);
                return;
            }

            EditorGUILayout.Space(6);

            // スキル一覧 + 選択中の詳細を描画
            DrawSkillList();

            EditorGUILayout.Space(6);

            // Select / Close ボタンなど
            DrawFooterButtons();
        }
    }

    /// <summary>
    /// 上部の説明UI。
    /// </summary>
    private void DrawHeader()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("スキル選択", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("リストからスキルをクリックして、呼び出し元へ返します。", EditorStyles.wordWrappedLabel);
        }
    }

    /// <summary>
    /// `SkillSO` を割り当てるためのObjectField。
    /// - `Assets/Data/SkillDB.asset` をここで指定可能
    /// - null の場合は、既定パスの自動ロードに任せることもできる
    /// </summary>
    private void DrawDatabaseField()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("Skill Database", GUILayout.Width(100));

            // ユーザーが任意の `SkillSO` を指定できる
            var newDb = (SkillSO)EditorGUILayout.ObjectField(_skillDatabase, typeof(SkillSO), false);

            // 差し替わった場合は選択状態もリセットする（別DBでインデックスがズレるため）
            if (!ReferenceEquals(newDb, _skillDatabase))
            {
                _skillDatabase = newDb;
                _selectedIndex = -1;
            }
        }

        // 既定パスをGUI上にも出しておく（どれを指定すべきか分かりやすくする）
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("Default", GUILayout.Width(100));
            EditorGUILayout.SelectableLabel(DefaultSkillDbAssetPath, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
        }
    }

    /// <summary>
    /// スキル一覧（スクロール）と、選択中スキルの詳細を描画する。
    /// </summary>
    private void DrawSkillList()
    {
        EditorGUILayout.LabelField($"Skills ({_skillDatabase.skill.Length})", EditorStyles.boldLabel);

        // スクロールビュー内で行を描画
        using (var scrollView = new EditorGUILayout.ScrollViewScope(_scroll))
        {
            _scroll = scrollView.scrollPosition;

            for (int i = 0; i < _skillDatabase.skill.Length; i++)
            {
                var skill = _skillDatabase.skill[i];

                // null要素が混ざっていても落ちないようにスキップ
                if (skill == null) continue;

                DrawSkillRow(i, skill);
            }
        }

        // 選択中があれば詳細を描画
        if (_selectedIndex >= 0 && _selectedIndex < _skillDatabase.skill.Length)
        {
            var skill = _skillDatabase.skill[_selectedIndex];
            if (skill != null)
            {
                EditorGUILayout.Space(6);
                DrawSkillDetails(skill);
            }
        }
    }

    /// <summary>
    /// 1行分のボタン（= スキル1件）を描画する。
    /// クリックで選択、ダブルクリックで確定（Selectと同等）にする。
    /// </summary>
    private void DrawSkillRow(int index, Skill skill)
    {
        // 選択中かどうか
        bool isSelected = _selectedIndex == index;

        // 行ボタンの見た目を調整（左寄せ、固定高さ）
        var rowStyle = new GUIStyle(EditorStyles.miniButton)
        {
            alignment = TextAnchor.MiddleLeft,
            fixedHeight = 24
        };

        // 一覧に表示するラベル（表示したい最低限の情報）
        var label = $"{skill.strName}  (MP:{skill.nManaCost})";

        // 選択行は背景色を変えて強調
        var originalBg = GUI.backgroundColor;
        if (isSelected) GUI.backgroundColor = new Color(0.7f, 0.85f, 1.0f);

        // 行クリック処理
        if (GUILayout.Button(label, rowStyle))
        {
            // 選択状態更新
            _selectedIndex = index;

            // クリックカウントが2以上なら「確定」扱いにする（操作を短縮）
            if (Event.current != null && Event.current.clickCount >= 2)
            {
                ConfirmSelection();
            }
        }

        // GUI状態を戻す（他のUIに影響しないように）
        GUI.backgroundColor = originalBg;
    }

    /// <summary>
    /// 選択中スキルの詳細欄を描画する。
    /// </summary>
    private void DrawSkillDetails(Skill skill)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Detail", EditorStyles.boldLabel);

            // 基本情報
            EditorGUILayout.LabelField("Name", skill.strName ?? string.Empty);
            EditorGUILayout.LabelField("MP Cost", skill.nManaCost.ToString());

            // ダメージ情報（Damageがnullの場合もある前提）
            if (skill.damage != null)
            {
                EditorGUILayout.LabelField("Damage Type", skill.damage.eDamageType.ToString());
                EditorGUILayout.LabelField("Damage Amount", skill.damage.nDamageAmount.ToString());
            }

            // 属性情報（Elementがnullの可能性も考慮）
            EditorGUILayout.LabelField("Main Element", skill.eMainElement != null ? skill.eMainElement.eElementType.ToString() : "None");
            EditorGUILayout.LabelField("Sub Element", skill.eSubElement != null ? skill.eSubElement.eElementType.ToString() : "None");

            EditorGUILayout.Space(4);

            // 説明文（複数行）
            EditorGUILayout.LabelField("Description", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.textArea))
            {
                EditorGUILayout.LabelField(skill.strDescription ?? string.Empty, EditorStyles.wordWrappedLabel);
            }
        }
    }

    /// <summary>
    /// 下部の操作ボタン（確定/閉じる）を描画する。
    /// </summary>
    private void DrawFooterButtons()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            // 未選択なら Select を押せないようにする
            GUI.enabled = CanConfirm();

            if (GUILayout.Button("Select", GUILayout.Height(28)))
            {
                ConfirmSelection();
            }

            // GUI.enabled を必ず戻す
            GUI.enabled = true;

            if (GUILayout.Button("Close", GUILayout.Height(28)))
            {
                Close();
            }
        }
    }

    /// <summary>
    /// 「確定」可能か（DBがあり、範囲内のSkillが選択されているか）判定。
    /// </summary>
    private bool CanConfirm()
    {
        return _skillDatabase != null
            && _skillDatabase.skill != null
            && _selectedIndex >= 0
            && _selectedIndex < _skillDatabase.skill.Length
            && _skillDatabase.skill[_selectedIndex] != null;
    }

    /// <summary>
    /// 選択確定。
    /// - コールバックへ `Skill` を渡す
    /// - その後ウィンドウを閉じる
    /// </summary>
    private void ConfirmSelection()
    {
        if (!CanConfirm()) return;

        var selectedSkill = _skillDatabase.skill[_selectedIndex];

        if (selectedSkill == null)
        {
            Debug.LogError("Skill is null!");
            return;
        }

        try
        {
            _onSelected?.Invoke(selectedSkill);
        }
        finally
        {
            Close();
        }
    }
}
#endif