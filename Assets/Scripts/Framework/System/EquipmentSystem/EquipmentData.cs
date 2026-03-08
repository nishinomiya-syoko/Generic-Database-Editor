using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Top
{
    // 装备数据
    [CreateAssetMenu(fileName = "EquipmentData", menuName = "Top/Equipment Data")]
    public class EquipmentData : ScriptableObject
    {
        [Header("基本信息")]
        public string id;
        public string equipmentName;
        public string description;
        public EquipmentType equipmentType;
        public EquipmentQuality quality;
        public Sprite icon;
        public GameObject modelPrefab;      // 装备模型预制体

        [Header("基础属性")]
        public int attack;                  // 攻击力
        public int defense;                 // 防御力
        public int health;                  // 生命值
        public int mana;                    // 魔法值
        public float attackSpeed;           // 攻击速度
        public float moveSpeed;             // 移动速度

        [Header("技能附加")]
        public string[] attachedSkills;     // 附加技能
        public float skillAttachProbability; // 技能附加概率
        public int requiredLevel;           // 需要等级

        [Header("特殊效果")]
        public string[] specialEffects;     // 特殊效果
        public string setEffectId;          // 套装效果ID

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(id))
                id = Guid.NewGuid().ToString();
        }

        public Color GetQualityColor()
        {
            switch (quality)
            {
                case EquipmentQuality.Common: return Color.white;
                case EquipmentQuality.Rare: return Color.blue;
                case EquipmentQuality.Epic: return Color.magenta;
                case EquipmentQuality.Legendary: return Color.yellow;
                case EquipmentQuality.Mythic: return Color.red;
                default: return Color.white;
            }
        }
    }

    // 玩家装备数据
    [System.Serializable]
    public class PlayerEquipment
    {
        public string equipmentId;
        public int level;                   // 装备等级
        public int enhancementLevel;        // 强化等级
        public string[] attachedSkills;     // 实际附加的技能
        public bool isEquipped;             // 是否已装备
        public DateTime obtainTime;         // 获得时间

        public PlayerEquipment(string equipmentId)
        {
            this.equipmentId = equipmentId;
            this.level = 1;
            this.enhancementLevel = 0;
            this.attachedSkills = new string[0];
            this.isEquipped = false;
            this.obtainTime = DateTime.Now;
        }

        public void Enhance()
        {
            enhancementLevel++;
        }

        public void AttachSkill(string skillId)
        {
            var skillList = new List<string>(attachedSkills);
            if (!skillList.Contains(skillId))
            {
                skillList.Add(skillId);
                attachedSkills = skillList.ToArray();
            }
        }
    }
}