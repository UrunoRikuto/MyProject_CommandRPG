using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// CSO_CharacterDataを一覧表示し、ステータス・所持スキルをその場で調整できるエディタウィンドウ
/// </summary>
public class CSED_CharacterDataWindow : EditorWindow
{
    private enum Column
    {
        Name,
        Health,
        MP,
        Attack,
        Defense,
        Speed,
        AttackWeight,
        SkillCount,
    }

    private static readonly string[] COLUMN_LABELS = { "名前", "HP", "MP", "ATK", "DEF", "SPD", "攻撃重み", "スキル" };
    private static readonly float[] DEFAULT_COLUMN_WIDTHS = { 110f, 45f, 45f, 45f, 45f, 45f, 55f, 55f };

    // スキル詳細欄の列幅: [0]名前 [1]コスト [2]倍率
    private static readonly float[] DEFAULT_SKILL_DETAIL_WIDTHS = { 90f, 40f, 40f };

    private const string DEFAULT_CHARACTER_FOLDER = "Assets/Data/Character";
    private const string PREF_PREFIX = "CSED_CharacterDataWindow.";
    private const float FOLDOUT_WIDTH = 18f;
    private const float RESIZE_HANDLE_WIDTH = 6f;
    private const float MIN_COLUMN_WIDTH = 30f;

    private Vector2 _scrollPosition = Vector2.zero;
    private List<CSO_CharacterData> _characters = new List<CSO_CharacterData>();
    private HashSet<CSO_CharacterData> _expandedCharacters = new HashSet<CSO_CharacterData>();
    private string _filterText = "";

    private float[] _columnWidths = (float[])DEFAULT_COLUMN_WIDTHS.Clone();
    private float[] _skillDetailWidths = (float[])DEFAULT_SKILL_DETAIL_WIDTHS.Clone();

    [MenuItem("Tools/Character Data Table")]
    public static void ShowWindow()
    {
        EditorWindow window = GetWindow(typeof(CSED_CharacterDataWindow));
        window.titleContent = new GUIContent("Character Data Table");
        window.minSize = new Vector2(820, 300);
    }

    private void OnEnable()
    {
        LoadColumnWidths();
        RefreshCharacterList();
    }

    private void OnDisable()
    {
        SaveColumnWidths();
    }

    /// <summary>
    /// 保存済みの列幅をEditorPrefsから読み込む。未保存の場合はデフォルト幅のまま
    /// </summary>
    private void LoadColumnWidths()
    {
        for (int i = 0; i < _columnWidths.Length; i++)
        {
            _columnWidths[i] = EditorPrefs.GetFloat(PREF_PREFIX + "Column" + i, DEFAULT_COLUMN_WIDTHS[i]);
        }

        for (int i = 0; i < _skillDetailWidths.Length; i++)
        {
            _skillDetailWidths[i] = EditorPrefs.GetFloat(PREF_PREFIX + "SkillDetail" + i, DEFAULT_SKILL_DETAIL_WIDTHS[i]);
        }
    }

    /// <summary>
    /// 現在の列幅をEditorPrefsに保存する。Unity再起動後も引き継がれる
    /// </summary>
    private void SaveColumnWidths()
    {
        for (int i = 0; i < _columnWidths.Length; i++)
        {
            EditorPrefs.SetFloat(PREF_PREFIX + "Column" + i, _columnWidths[i]);
        }

        for (int i = 0; i < _skillDetailWidths.Length; i++)
        {
            EditorPrefs.SetFloat(PREF_PREFIX + "SkillDetail" + i, _skillDetailWidths[i]);
        }
    }

    /// <summary>
    /// プロジェクト内のCSO_CharacterDataをすべて読み込み直す
    /// </summary>
    private void RefreshCharacterList()
    {
        _characters.Clear();

        string[] guids = AssetDatabase.FindAssets("t:CSO_CharacterData");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            CSO_CharacterData data = AssetDatabase.LoadAssetAtPath<CSO_CharacterData>(path);
            if (data != null)
            {
                _characters.Add(data);
            }
        }

        _characters = _characters.OrderBy(c => c.characterName).ToList();
    }

    private void OnGUI()
    {
        DrawToolbar();
        DrawTableHeader();
        DrawTableBody();
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        if (GUILayout.Button("更新", EditorStyles.toolbarButton, GUILayout.Width(80)))
        {
            RefreshCharacterList();
        }

        if (GUILayout.Button("＋ 新規キャラクター", EditorStyles.toolbarButton, GUILayout.Width(130)))
        {
            CreateNewCharacter();
        }

        if (GUILayout.Button("幅を保存", EditorStyles.toolbarButton, GUILayout.Width(80)))
        {
            SaveColumnWidths();
        }

        if (GUILayout.Button("幅リセット", EditorStyles.toolbarButton, GUILayout.Width(80)))
        {
            _columnWidths = (float[])DEFAULT_COLUMN_WIDTHS.Clone();
            _skillDetailWidths = (float[])DEFAULT_SKILL_DETAIL_WIDTHS.Clone();
        }

        GUILayout.Label("フィルター:", GUILayout.Width(60));
        _filterText = EditorGUILayout.TextField(_filterText, EditorStyles.toolbarSearchField, GUILayout.Width(150));

        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField($"{_characters.Count}体", GUILayout.Width(50));

        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 新規のCSO_CharacterDataアセットを保存先を選ばせて作成する
    /// </summary>
    private void CreateNewCharacter()
    {
        string folder = AssetDatabase.IsValidFolder(DEFAULT_CHARACTER_FOLDER) ? DEFAULT_CHARACTER_FOLDER : "Assets";
        string path = EditorUtility.SaveFilePanelInProject("新規キャラクター作成", "DB_Char_New", "asset", "保存先を選択してください", folder);
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        var newCharacter = ScriptableObject.CreateInstance<CSO_CharacterData>();
        AssetDatabase.CreateAsset(newCharacter, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        RefreshCharacterList();
        EditorGUIUtility.PingObject(newCharacter);
        Selection.activeObject = newCharacter;
    }

    private void DrawTableHeader()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("", GUILayout.Width(FOLDOUT_WIDTH));

        for (int i = 0; i < COLUMN_LABELS.Length; i++)
        {
            GUILayout.Label(COLUMN_LABELS[i], EditorStyles.boldLabel, GUILayout.Width(_columnWidths[i]));
            DrawResizeHandle(_columnWidths, i);
        }

        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// フィールドの右側に配置するドラッグ用のハンドル。ドラッグ幅をwidths[index]に反映する
    /// </summary>
    private void DrawResizeHandle(float[] widths, int index)
    {
        Rect handleRect = GUILayoutUtility.GetRect(RESIZE_HANDLE_WIDTH, EditorGUIUtility.singleLineHeight, GUILayout.Width(RESIZE_HANDLE_WIDTH));
        EditorGUIUtility.AddCursorRect(handleRect, MouseCursor.ResizeHorizontal);

        int controlId = GUIUtility.GetControlID(FocusType.Passive, handleRect);
        Event e = Event.current;

        switch (e.GetTypeForControl(controlId))
        {
            case EventType.MouseDown:
                if (handleRect.Contains(e.mousePosition))
                {
                    GUIUtility.hotControl = controlId;
                    e.Use();
                }
                break;

            case EventType.MouseDrag:
                if (GUIUtility.hotControl == controlId)
                {
                    widths[index] = Mathf.Max(MIN_COLUMN_WIDTH, widths[index] + e.delta.x);
                    e.Use();
                    Repaint();
                }
                break;

            case EventType.MouseUp:
                if (GUIUtility.hotControl == controlId)
                {
                    GUIUtility.hotControl = 0;
                    e.Use();
                }
                break;
        }
    }

    private void DrawTableBody()
    {
        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

        if (_characters.Count == 0)
        {
            EditorGUILayout.HelpBox("CSO_CharacterDataアセットが見つかりません。", MessageType.Info);
        }

        foreach (CSO_CharacterData character in _characters)
        {
            if (character == null || !MatchesFilter(character))
            {
                continue;
            }

            DrawCharacterRow(character);
        }

        EditorGUILayout.EndScrollView();
    }

    private bool MatchesFilter(CSO_CharacterData character)
    {
        if (string.IsNullOrEmpty(_filterText))
        {
            return true;
        }

        string filter = _filterText.ToLower();
        return character.characterName.ToLower().Contains(filter) || character.name.ToLower().Contains(filter);
    }

    private void DrawCharacterRow(CSO_CharacterData character)
    {
        var serializedObject = new SerializedObject(character);
        serializedObject.Update();

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        DrawStatRow(character, serializedObject);
        if (_expandedCharacters.Contains(character))
        {
            DrawSkillList(character, serializedObject);
        }
        EditorGUILayout.EndVertical();

        if (serializedObject.ApplyModifiedProperties())
        {
            EditorUtility.SetDirty(character);
        }
    }

    private void DrawStatRow(CSO_CharacterData character, SerializedObject serializedObject)
    {
        EditorGUILayout.BeginHorizontal();

        DrawFoldoutButton(character);

        if (GUILayout.Button(character.name, EditorStyles.linkLabel, GUILayout.Width(_columnWidths[(int)Column.Name])))
        {
            EditorGUIUtility.PingObject(character);
            Selection.activeObject = character;
        }
        GUILayout.Space(RESIZE_HANDLE_WIDTH);

        DrawStatProperty(serializedObject, "_baseHealth", Column.Health);
        DrawStatProperty(serializedObject, "_baseMP", Column.MP);
        DrawStatProperty(serializedObject, "_baseAttack", Column.Attack);
        DrawStatProperty(serializedObject, "_baseDefense", Column.Defense);
        DrawStatProperty(serializedObject, "_baseSpeed", Column.Speed);

        var attackWeightProp = serializedObject.FindProperty("_attackWeight");
        EditorGUILayout.PropertyField(attackWeightProp, GUIContent.none, GUILayout.Width(_columnWidths[(int)Column.AttackWeight]));
        GUILayout.Space(RESIZE_HANDLE_WIDTH);

        EditorGUILayout.LabelField($"{character.initialSkills.Count}個", GUILayout.Width(_columnWidths[(int)Column.SkillCount]));

        EditorGUILayout.EndHorizontal();
    }

    private void DrawFoldoutButton(CSO_CharacterData character)
    {
        bool isExpanded = _expandedCharacters.Contains(character);
        bool nextExpanded = GUILayout.Toggle(isExpanded, isExpanded ? "▼" : "▶", EditorStyles.miniButton, GUILayout.Width(FOLDOUT_WIDTH));

        if (nextExpanded)
        {
            _expandedCharacters.Add(character);
        }
        else
        {
            _expandedCharacters.Remove(character);
        }
    }

    private void DrawStatProperty(SerializedObject serializedObject, string propertyName, Column column)
    {
        var property = serializedObject.FindProperty(propertyName);
        EditorGUILayout.PropertyField(property, GUIContent.none, GUILayout.Width(_columnWidths[(int)column]));
        GUILayout.Space(RESIZE_HANDLE_WIDTH);
    }

    /// <summary>
    /// 所持スキルの一覧を表示する。スキル自体の名前/コスト/倍率、AI選択重みを直接編集でき、
    /// スロットの追加・削除、新規スキルアセットの作成もここから行える
    /// </summary>
    private void DrawSkillList(CSO_CharacterData character, SerializedObject serializedObject)
    {
        var skillsProp = serializedObject.FindProperty("_initialSkills");
        var weightsProp = serializedObject.FindProperty("_skillWeights");

        EditorGUILayout.Space(2);

        int removeIndex = -1;
        for (int i = 0; i < skillsProp.arraySize; i++)
        {
            if (DrawSkillRow(character, skillsProp, weightsProp, i))
            {
                removeIndex = i;
            }
        }

        if (removeIndex >= 0)
        {
            RemoveSkillAt(skillsProp, weightsProp, removeIndex);
        }

        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(FOLDOUT_WIDTH + _columnWidths[(int)Column.Name] * 0.2f);
        if (GUILayout.Button("＋ スキル追加", EditorStyles.miniButton, GUILayout.Width(100)))
        {
            AddSkillSlot(skillsProp, weightsProp);
        }
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// スキル1枠分を描画する。削除ボタンが押されたらtrueを返す
    /// </summary>
    private bool DrawSkillRow(CSO_CharacterData character, SerializedProperty skillsProp, SerializedProperty weightsProp, int index)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(FOLDOUT_WIDTH + _columnWidths[(int)Column.Name] * 0.2f);

        var skillProp = skillsProp.GetArrayElementAtIndex(index);
        EditorGUILayout.PropertyField(skillProp, GUIContent.none, GUILayout.Width(_columnWidths[(int)Column.Name]));

        if (GUILayout.Button("新規", EditorStyles.miniButton, GUILayout.Width(40)))
        {
            CreateNewSkillAsset(character, skillProp);
        }

        var skill = skillProp.objectReferenceValue as CSO_SkillData;
        if (skill != null)
        {
            DrawSkillDetailFields(skill);
        }
        else
        {
            EditorGUILayout.LabelField("未設定", GUILayout.Width(230));
        }

        if (index < weightsProp.arraySize)
        {
            GUILayout.Label("重み", GUILayout.Width(30));
            var weightProp = weightsProp.GetArrayElementAtIndex(index);
            EditorGUILayout.PropertyField(weightProp, GUIContent.none, GUILayout.Width(_columnWidths[(int)Column.AttackWeight]));
        }

        bool removeRequested = GUILayout.Button("－", EditorStyles.miniButton, GUILayout.Width(24));

        EditorGUILayout.EndHorizontal();
        return removeRequested;
    }

    /// <summary>
    /// 参照先のCSO_SkillData自体の名前・コスト・倍率をその場で編集する
    /// </summary>
    private void DrawSkillDetailFields(CSO_SkillData skill)
    {
        var skillSerializedObject = new SerializedObject(skill);
        skillSerializedObject.Update();

        var nameProp = skillSerializedObject.FindProperty("_skillName");
        var costProp = skillSerializedObject.FindProperty("_cost");
        var rateProp = skillSerializedObject.FindProperty("_damageRate");

        GUILayout.Label("名前", GUILayout.Width(28));
        EditorGUILayout.PropertyField(nameProp, GUIContent.none, GUILayout.Width(_skillDetailWidths[0]));
        DrawResizeHandle(_skillDetailWidths, 0);

        GUILayout.Label("コスト", GUILayout.Width(34));
        EditorGUILayout.PropertyField(costProp, GUIContent.none, GUILayout.Width(_skillDetailWidths[1]));
        DrawResizeHandle(_skillDetailWidths, 1);

        GUILayout.Label("倍率", GUILayout.Width(28));
        EditorGUILayout.PropertyField(rateProp, GUIContent.none, GUILayout.Width(_skillDetailWidths[2]));
        DrawResizeHandle(_skillDetailWidths, 2);

        if (skillSerializedObject.ApplyModifiedProperties())
        {
            EditorUtility.SetDirty(skill);
        }
    }

    /// <summary>
    /// キャラクターと同じフォルダに新規CSO_SkillDataアセットを作成し、そのスロットへ割り当てる
    /// </summary>
    private void CreateNewSkillAsset(CSO_CharacterData character, SerializedProperty skillProp)
    {
        string characterFolder = Path.GetDirectoryName(AssetDatabase.GetAssetPath(character));
        string defaultName = $"DB_Skill_{character.characterName}_New";
        string path = EditorUtility.SaveFilePanelInProject("新規スキル作成", defaultName, "asset", "保存先を選択してください", characterFolder);
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        var newSkill = ScriptableObject.CreateInstance<CSO_SkillData>();
        AssetDatabase.CreateAsset(newSkill, path);
        AssetDatabase.SaveAssets();

        skillProp.objectReferenceValue = newSkill;
    }

    private void AddSkillSlot(SerializedProperty skillsProp, SerializedProperty weightsProp)
    {
        skillsProp.InsertArrayElementAtIndex(skillsProp.arraySize);
        skillsProp.GetArrayElementAtIndex(skillsProp.arraySize - 1).objectReferenceValue = null;

        weightsProp.InsertArrayElementAtIndex(weightsProp.arraySize);
        weightsProp.GetArrayElementAtIndex(weightsProp.arraySize - 1).floatValue = 1f;
    }

    /// <summary>
    /// オブジェクト参照要素は1回のDeleteではnull化されるだけなので、先にnullへ落としてから削除する
    /// </summary>
    private void RemoveSkillAt(SerializedProperty skillsProp, SerializedProperty weightsProp, int index)
    {
        if (skillsProp.GetArrayElementAtIndex(index).objectReferenceValue != null)
        {
            skillsProp.GetArrayElementAtIndex(index).objectReferenceValue = null;
        }
        skillsProp.DeleteArrayElementAtIndex(index);

        if (index < weightsProp.arraySize)
        {
            weightsProp.DeleteArrayElementAtIndex(index);
        }
    }
}
