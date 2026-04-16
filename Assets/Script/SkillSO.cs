using UnityEngine;

// SkillDB用の ScriptableObject
// Note: Skill 本体の定義は `Skill.cs` にある。
[CreateAssetMenu(fileName = "New SkillDB", menuName = "ScriptableObjects/SkillDB")]
public class SkillSO : ScriptableObject
{
 public Skill[] skill;
}
