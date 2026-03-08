// using UnityEngine;
// using System;
// using System.Collections;
// using System.Collections.Generic;

// namespace Top
// {
//     // 技能管理器
//     public class SkillManager : MonoBehaviour
//     {
//         [Header("技能设置")]
//         public SkillDatabase skillDatabase;

//         private Dictionary<string, SkillCooldownData> skillCooldowns = new Dictionary<string, SkillCooldownData>();

//         [System.Serializable]
//         private class SkillCooldownData
//         {
//             public float remainingCooldown;
//             public bool isReady => remainingCooldown <= 0f;

//             public void UpdateCooldown(float deltaTime)
//             {
//                 remainingCooldown = Mathf.Max(0f, remainingCooldown - deltaTime);
//             }

//             public void StartCooldown(float cooldown)
//             {
//                 remainingCooldown = cooldown;
//             }
//         }

//         void Update()
//         {
//             UpdateSkillCooldowns();
//         }

//         private void UpdateSkillCooldowns()
//         {
//             float deltaTime = Time.deltaTime;
//             foreach (var cooldown in skillCooldowns.Values)
//             {
//                 cooldown.UpdateCooldown(deltaTime);
//             }
//         }

//         public bool CanUseSkill(string skillId)
//         {
//             if (skillCooldowns.TryGetValue(skillId, out var cooldownData))
//             {
//                 return cooldownData.isReady;
//             }
//             return true;
//         }

//         public void UseSkill(string skillId, int skillLevel = 1)
//         {
//             var skillData = skillDatabase?.GetSkillData(skillId);
//             if (skillData == null)
//                 return;

//             float cooldown = skillData.GetCooldown(skillLevel);

//             if (!skillCooldowns.ContainsKey(skillId))
//             {
//                 skillCooldowns[skillId] = new SkillCooldownData();
//             }

//             skillCooldowns[skillId].StartCooldown(cooldown);
//         }

//         public float GetSkillCooldownRemaining(string skillId)
//         {
//             if (skillCooldowns.TryGetValue(skillId, out var cooldownData))
//             {
//                 return cooldownData.remainingCooldown;
//             }
//             return 0f;
//         }

//         public float GetSkillCooldownProgress(string skillId)
//         {
//             var skillData = skillDatabase?.GetSkillData(skillId);
//             if (skillData == null)
//                 return 1f;

//             float remaining = GetSkillCooldownRemaining(skillId);
//             return 1f - (remaining / skillData.cooldown);
//         }
//     }
// }