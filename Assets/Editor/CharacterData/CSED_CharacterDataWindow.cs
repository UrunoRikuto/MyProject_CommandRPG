using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// CSO_CharacterDataを一覧表示し、ステータスをその場で調整できるエディタウィンドウ
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

    private const float FOLDOUT_WIDTH = 18f;
    private const float RESIZE_HANDLE_WIDTH = 6f;
    private const float MIN_COLUMN_WIDTH = 30f;

    private Vector2 _scrollPosition = Vector2.zero;
    private List<CSO_CharacterData> _characters = new List<CSO_CharacterData>();
    private HashSet<CSO_CharacterData> _expandedCharacters = new HashSet<CSO_CharacterData>();
    private string _filterText = "";

    private float[] _columnWidths = (float[])DEFAULT_COLUMN_WIDTHS.Clone();
    private int _resizingColumn = -1;

    [MenuItem("Tools/Character Data Table")]
    public static void ShowWindow()
    {
        EditorWindow window = GetWindow(typeof(CSED_CharacterDataWindow));
        window.titleContent = new GUIContent("Character Data Table");
        window.minSize = new Vector2(760, 300);
    }

    private void OnEnable()
    {
        RefreshCharacterList();
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

        if (GUILayout.Button("幅リセット", EditorStyles.toolbarButton, GUILayout.Width(80)))
        {
            _columnWidths = (float[])DEFAULT_COLUMN_WIDTHS.Clone();
        }

        GUILayout.Label("フィルター:", GUILayout.Width(60));
        _filterText = EditorGUILayout.TextField(_filterText, EditorStyles.toolbarSearchField, GUILayout.Width(150));

        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField($"{_characters.Count}体", GUILayout.Width(50));

        EditorGUILayout.EndHorizontal();
    }

    private void DrawTableHeader()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("", GUILayout.Width(FOLDOUT_WIDTH));

        for (int i = 0; i < COLUMN_LABELS.Length; i++)
        {
            GUILayout.Label(COLUMN_LABELS[i], EditorStyles.boldLabel, GUILayout.Width(_columnWidths[i]));
            DrawResizeHandle(i);
        }

        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 列見出しの右側に配置するドラッグ用のハンドル。ドラッグ幅を該当列の表示幅に反映する
    /// </summary>
    private void DrawResizeHandle(int columnIndex)
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
                    _resizingColumn = columnIndex;
                    e.Use();
                }
                break;

            case EventType.MouseDrag:
                if (GUIUtility.hotControl == controlId)
                {
                    _columnWidths[columnIndex] = Mathf.Max(MIN_COLUMN_WIDTH, _columnWidths[columnIndex] + e.delta.x);
                    e.Use();
                    Repaint();
                }
                break;

            case EventType.MouseUp:
                if (GUIUtility.hotControl == controlId)
                {
                    GUIUtility.hotControl = 0;
                    _resizingColumn = -1;
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
            DrawSkillList(serializedObject);
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
        bool hasSkills = character.initialSkills.Count > 0;
        bool isExpanded = _expandedCharacters.Contains(character);

        GUI.enabled = hasSkills;
        bool nextExpanded = GUILayout.Toggle(isExpanded, isExpanded ? "▼" : "▶", EditorStyles.miniButton, GUILayout.Width(FOLDOUT_WIDTH));
        GUI.enabled = true;

        if (!hasSkills)
        {
            return;
        }

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
    /// 所持スキルと個別のAI選択重みを表示する
    /// </summary>
    private void DrawSkillList(SerializedObject serializedObject)
    {
        var skillsProp = serializedObject.FindProperty("_initialSkills");
        var weightsProp = serializedObject.FindProperty("_skillWeights");

        EditorGUILayout.Space(2);

        for (int i = 0; i < skillsProp.arraySize; i++)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(FOLDOUT_WIDTH + _columnWidths[(int)Column.Name] * 0.2f);

            var skillProp = skillsProp.GetArrayElementAtIndex(i);
            EditorGUILayout.PropertyField(skillProp, GUIContent.none, GUILayout.Width(_columnWidths[(int)Column.Name]));

            var skill = skillProp.objectReferenceValue as CSO_SkillData;
            string info = skill != null ? $"cost:{skill.cost} rate:{skill.damageRate}" : "未設定";
            EditorGUILayout.LabelField(info, GUILayout.Width(150));

            if (i < weightsProp.arraySize)
            {
                GUILayout.Label("重み", GUILayout.Width(30));
                var weightProp = weightsProp.GetArrayElementAtIndex(i);
                EditorGUILayout.PropertyField(weightProp, GUIContent.none, GUILayout.Width(_columnWidths[(int)Column.AttackWeight]));
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}
