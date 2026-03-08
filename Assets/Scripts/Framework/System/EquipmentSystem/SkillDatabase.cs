// using UnityEngine;
// using System;
// using System.Collections;
// using System.Collections.Generic;

// namespace Top
// {
//     // 技能数据库
//     [CreateAssetMenu(fileName = "SkillDatabase", menuName = "Top/Skill Database")]
//     public class SkillDatabase : ScriptableObject
//     {
//         public List<SkillData> skills = new List<SkillData>();

//         public SkillData GetSkillData(string skillId)
//         {
//             return skills.Find(s => s.id == skillId);
//         }

//         public List<SkillData> GetSkillsByType(SkillType type)
//         {
//             return skills.FindAll(s => s.skillType == type);
//         }
//     }
// }