using UnityEngine;

namespace SOEditor
{
    /// <summary>
    /// 测试数据生成器
    /// 用于快速生成测试数据
    /// </summary>
    public static class SOTestDataGenerator
    {
        [UnityEditor.MenuItem("Tools/SO Tool/Generate Test Items Data")]
        public static void GenerateTestData()
        {
            if (!UnityEditor.EditorUtility.DisplayDialog("生成测试数据", "这将生成一些测试数据，是否继续？", "确定", "取消"))
            {
                return;
            }
            
            GenerateItems();
            GenerateCharacters();
            GenerateSkills();
            GenerateQuests();
            
            UnityEditor.EditorUtility.DisplayDialog("完成", "测试数据生成完成！", "确定");
            Debug.Log("[SOTestDataGenerator] 测试数据生成完成");
        }
        
        private static void GenerateItems()
        {
            // 武器
            CreateItem("铁剑", "一把普通的铁剑", ItemType.Weapon, 100, 50, 1, 10, 0, 0, 0, 1);
            CreateItem("钢剑", "用优质钢材打造的剑", ItemType.Weapon, 250, 125, 1, 18, 0, 0, 0, 2);
            CreateItem("魔法剑", "蕴含魔法力量的剑", ItemType.Weapon, 500, 250, 1, 25, 0, 0, 10, 3);
            CreateItem("传说之剑", "传说中的神剑", ItemType.Weapon, 2000, 1000, 1, 50, 0, 20, 20, 5);
            
            // 护甲
            CreateItem("皮甲", "用皮革制作的护甲", ItemType.Armor, 80, 40, 1, 0, 5, 10, 0, 1);
            CreateItem("铁甲", "用铁片编织的护甲", ItemType.Armor, 200, 100, 1, 0, 12, 25, 0, 2);
            CreateItem("钢甲", "重型钢制护甲", ItemType.Armor, 450, 225, 1, 0, 20, 40, 0, 3);
            CreateItem("魔法铠甲", "被魔法加持的铠甲", ItemType.Armor, 800, 400, 1, 0, 30, 50, 30, 4);
            
            // 消耗品
            CreateItem("生命药水", "恢复少量生命值", ItemType.Consumable, 20, 10, 99, 0, 0, 20, 0, 1);
            CreateItem("法力药水", "恢复少量法力值", ItemType.Consumable, 20, 10, 99, 0, 0, 0, 20, 1);
            CreateItem("高级生命药水", "恢复大量生命值", ItemType.Consumable, 50, 25, 99, 0, 0, 50, 0, 2);
            CreateItem("高级法力药水", "恢复大量法力值", ItemType.Consumable, 50, 25, 99, 0, 0, 0, 50, 2);
            
            // 材料
            CreateItem("铁矿石", "普通的铁矿石", ItemType.Material, 10, 5, 999, 0, 0, 0, 0, 1);
            CreateItem("金矿石", "珍贵的金矿石", ItemType.Material, 50, 25, 999, 0, 0, 0, 0, 2);
            CreateItem("魔法水晶", "蕴含魔法能量的水晶", ItemType.Material, 100, 50, 999, 0, 0, 0, 0, 3);
            
            Debug.Log("[SOTestDataGenerator] 已生成物品数据");
        }
        
        private static void CreateItem(string name, string desc, ItemType type, int buyPrice, int sellPrice, 
            int maxStack, int atk, int def, int hp, int mp, int rarity)
        {
            ItemSO item = ScriptableObject.CreateInstance<ItemSO>();
            item.GenerateId();
            item.SetDisplayName(name);
            item.SetDescription(desc);
            item.SetItemType(type);
            item.SetBuyPrice(buyPrice);
            item.SetSellPrice(sellPrice);
            item.SetMaxStackSize(maxStack);
            item.SetAttackBonus(atk);
            item.SetDefenseBonus(def);
            item.SetHealthBonus(hp);
            item.SetManaBonus(mp);
            item.SetRarity(rarity);
            item.SetIsEquipable(type == ItemType.Weapon || type == ItemType.Armor);
            item.SetIsUsable(type == ItemType.Consumable);
            
            SODatabase.SaveScriptableObject(item);
        }
        
        private static void GenerateCharacters()
        {
            // 玩家
            CreateCharacter("勇者", "传说中的勇者", CharacterType.Player, 1, 100, 50, 15, 12, 10, 8, 5f, 1f, 2f, false);
            
            // NPC
            CreateCharacter("村长", "村庄的领导者", CharacterType.NPC, 5, 80, 30, 8, 8, 12, 5, 3f, 1f, 2f, false);
            CreateCharacter("商人", "四处旅行的商人", CharacterType.NPC, 3, 60, 40, 6, 10, 14, 4, 4f, 1f, 2f, false);
            CreateCharacter("铁匠", "技艺精湛的铁匠", CharacterType.NPC, 4, 90, 35, 14, 8, 8, 8, 3f, 1f, 2f, false);
            
            // 敌人
            CreateCharacter("史莱姆", "绿色的凝胶状生物", CharacterType.Enemy, 1, 30, 0, 5, 3, 1, 2, 2f, 1f, 1.5f, true, 10, 5);
            CreateCharacter("哥布林", "狡猾的小怪物", CharacterType.Enemy, 2, 45, 0, 8, 4, 2, 3, 3f, 1.2f, 1.5f, true, 15, 8);
            CreateCharacter("兽人", "强壮的兽人战士", CharacterType.Enemy, 3, 80, 0, 15, 8, 3, 5, 4f, 0.8f, 2f, true, 30, 15);
            CreateCharacter("骷髅兵", "复活的骷髅战士", CharacterType.Enemy, 4, 60, 0, 12, 6, 0, 4, 3f, 1f, 2f, true, 25, 12);
            CreateCharacter("暗影刺客", "潜行的刺客", CharacterType.Enemy, 5, 55, 0, 18, 5, 5, 3, 6f, 1.5f, 1.5f, true, 40, 20);
            
            // Boss
            CreateCharacter("哥布林王", "哥布林的首领", CharacterType.Boss, 10, 300, 100, 25, 15, 10, 10, 4f, 1f, 3f, true, 200, 100);
            CreateCharacter("巨龙", "传说中的巨龙", CharacterType.Boss, 20, 1000, 500, 50, 30, 20, 20, 6f, 0.8f, 5f, true, 1000, 500);
            
            Debug.Log("[SOTestDataGenerator] 已生成角色数据");
        }
        
        private static void CreateCharacter(string name, string desc, CharacterType type, int level, int hp, int mp,
            int str, int agi, int intel, int def, float moveSpeed, float atkSpeed, float atkRange, bool hasAI,
            int expReward = 0, int goldReward = 0)
        {
            CharacterSO character = ScriptableObject.CreateInstance<CharacterSO>();
            character.GenerateId();
            character.SetDisplayName(name);
            character.SetDescription(desc);
            character.SetCharacterType(type);
            character.SetLevel(level);
            character.SetMaxHealth(hp);
            character.SetMaxMana(mp);
            character.SetStrength(str);
            character.SetAgility(agi);
            character.SetIntelligence(intel);
            character.SetDefense(def);
            character.SetMoveSpeed(moveSpeed);
            character.SetAttackSpeed(atkSpeed);
            character.SetAttackRange(atkRange);
            character.SetHasAI(hasAI);
            character.SetDetectionRange(10f);
            character.SetChaseRange(15f);
            character.SetExperienceReward(expReward);
            character.SetGoldReward(goldReward);
            
            SODatabase.SaveScriptableObject(character);
        }
        
        private static void GenerateSkills()
        {
            // 主动技能
            CreateSkill("火球术", "发射一个火球攻击敌人", SkillType.Active, DamageType.Fire, 20, 3f, 0.5f, 2f, 8f, 0f,
                25f, 1f, 0f, false, false, false, 0f, false, 0f, 0f, 1, "");
            
            CreateSkill("冰箭", "发射冰箭冻结敌人", SkillType.Active, DamageType.Ice, 15, 2f, 0.3f, 1.5f, 10f, 0f,
                20f, 1f, 0f, false, false, true, 1f, false, 0f, 0f, 1, "");
            
            CreateSkill("闪电链", "释放闪电攻击多个敌人", SkillType.Active, DamageType.Lightning, 30, 5f, 0.8f, 1f, 12f, 5f,
                40f, 1f, 0f, false, false, false, 0f, false, 0f, 0f, 5, "");
            
            CreateSkill("治疗术", "恢复生命值", SkillType.Active, DamageType.True, 25, 4f, 1f, 1f, 0f, 0f,
                0f, 1f, 50f, true, false, false, 0f, false, 0f, 0f, 1, "");
            
            CreateSkill("重击", "强力的物理攻击", SkillType.Active, DamageType.Physical, 10, 4f, 0.5f, 1f, 2f, 0f,
                35f, 1.5f, 0f, false, true, false, 0f, false, 0f, 0f, 3, "");
            
            // 被动技能
            CreateSkill("力量增强", "永久增加力量", SkillType.Passive, DamageType.True, 0, 0f, 0f, 0f, 0f, 0f,
                0f, 1f, 0f, false, false, false, 0f, false, 0f, 0f, 1, "");
            
            CreateSkill("敏捷提升", "永久增加敏捷", SkillType.Passive, DamageType.True, 0, 0f, 0f, 0f, 0f, 0f,
                0f, 1f, 0f, false, false, false, 0f, false, 0f, 0f, 1, "");
            
            // 终极技能
            CreateSkill("陨石术", "召唤陨石攻击大范围敌人", SkillType.Ultimate, DamageType.Fire, 80, 15f, 2f, 3f, 15f, 8f,
                150f, 2f, 0f, false, true, false, 0f, true, 20f, 5f, 10, "火球术");
            
            Debug.Log("[SOTestDataGenerator] 已生成技能数据");
        }
        
        private static void CreateSkill(string name, string desc, SkillType type, DamageType damageType, int manaCost,
            float cooldown, float castTime, float duration, float range, float aoe, float baseDamage, float damageMult,
            float healing, bool isHealing, bool hasKnockback, bool hasStun, float stunDuration, bool hasDot, float dotDamage,
            float dotDuration, int reqLevel, string reqSkill)
        {
            SkillSO skill = ScriptableObject.CreateInstance<SkillSO>();
            skill.GenerateId();
            skill.SetDisplayName(name);
            skill.SetDescription(desc);
            skill.SetSkillType(type);
            skill.SetDamageType(damageType);
            skill.SetManaCost(manaCost);
            skill.SetCooldown(cooldown);
            skill.SetCastTime(castTime);
            skill.SetDuration(duration);
            skill.SetRange(range);
            skill.SetAreaOfEffect(aoe);
            skill.SetBaseDamage(baseDamage);
            skill.SetDamageMultiplier(damageMult);
            skill.SetHealingAmount(healing);
            skill.SetIsHealingSkill(isHealing);
            skill.SetHasKnockback(hasKnockback);
            skill.SetHasStun(hasStun);
            skill.SetStunDuration(stunDuration);
            skill.SetHasDot(hasDot);
            skill.SetDotDamage(dotDamage);
            skill.SetDotDuration(dotDuration);
            skill.SetRequiredLevel(reqLevel);
            skill.SetRequiredSkillId(reqSkill);
            
            SODatabase.SaveScriptableObject(skill);
        }
        
        private static void GenerateQuests()
        {
            // 主线任务
            QuestSO mainQuest1 = CreateQuest("新的开始", "开始你的冒险之旅", QuestType.Main, 1, "", false, 0, "村长", "村长");
            mainQuest1.AddObjective(new QuestObjective
            {
                ObjectiveId = "obj1",
                Type = ObjectiveType.Talk,
                TargetId = "村长",
                Description = "与村长对话",
                RequiredAmount = 1,
                IsOptional = false
            });
            mainQuest1.SetReward(new QuestReward { Experience = 50, Gold = 10 });
            SODatabase.SaveScriptableObject(mainQuest1);
            
            QuestSO mainQuest2 = CreateQuest("清理史莱姆", "村庄周围的史莱姆太多了", QuestType.Main, 1, "", false, 0, "村长", "村长");
            mainQuest2.AddObjective(new QuestObjective
            {
                ObjectiveId = "obj1",
                Type = ObjectiveType.Kill,
                TargetId = "",
                Description = "消灭5只史莱姆",
                RequiredAmount = 5,
                IsOptional = false
            });
            mainQuest2.SetReward(new QuestReward { Experience = 100, Gold = 25 });
            SODatabase.SaveScriptableObject(mainQuest2);
            
            // 支线任务
            QuestSO sideQuest1 = CreateQuest("收集铁矿石", "铁匠需要一些铁矿石", QuestType.Side, 1, "", false, 0, "铁匠", "铁匠");
            sideQuest1.AddObjective(new QuestObjective
            {
                ObjectiveId = "obj1",
                Type = ObjectiveType.Collect,
                TargetId = "",
                Description = "收集10个铁矿石",
                RequiredAmount = 10,
                IsOptional = false
            });
            sideQuest1.SetReward(new QuestReward { Experience = 80, Gold = 30 });
            SODatabase.SaveScriptableObject(sideQuest1);
            
            QuestSO sideQuest2 = CreateQuest("哥布林的威胁", "哥布林在骚扰村庄", QuestType.Side, 2, "", false, 0, "村长", "村长");
            sideQuest2.AddObjective(new QuestObjective
            {
                ObjectiveId = "obj1",
                Type = ObjectiveType.Kill,
                TargetId = "",
                Description = "消灭8只哥布林",
                RequiredAmount = 8,
                IsOptional = false
            });
            sideQuest2.SetReward(new QuestReward { Experience = 150, Gold = 50 });
            SODatabase.SaveScriptableObject(sideQuest2);
            
            // 每日任务
            QuestSO dailyQuest = CreateQuest("日常训练", "完成日常训练任务", QuestType.Daily, 1, "", true, 24, "村长", "村长");
            dailyQuest.AddObjective(new QuestObjective
            {
                ObjectiveId = "obj1",
                Type = ObjectiveType.Kill,
                TargetId = "",
                Description = "消灭10只怪物",
                RequiredAmount = 10,
                IsOptional = false
            });
            dailyQuest.SetReward(new QuestReward { Experience = 200, Gold = 100 });
            SODatabase.SaveScriptableObject(dailyQuest);
            
            Debug.Log("[SOTestDataGenerator] 已生成任务数据");
        }
        
        private static QuestSO CreateQuest(string name, string desc, QuestType type, int reqLevel, string prereq, 
            bool repeatable, int cooldown, string startNpc, string completeNpc)
        {
            QuestSO quest = ScriptableObject.CreateInstance<QuestSO>();
            quest.GenerateId();
            quest.SetDisplayName(name);
            quest.SetDescription(desc);
            quest.SetQuestType(type);
            quest.SetRequiredLevel(reqLevel);
            quest.SetPrerequisiteQuestId(prereq);
            quest.SetIsRepeatable(repeatable);
            quest.SetRepeatCooldownHours(cooldown);
            quest.SetStartNpcId(startNpc);
            quest.SetCompleteNpcId(completeNpc);
            quest.SetHasTimeLimit(false);
            quest.SetTimeLimitMinutes(0);
            
            return quest;
        }
        
        [UnityEditor.MenuItem("Tools/SO Tool/Clear All Data")]
        public static void ClearAllData()
        {
            if (!UnityEditor.EditorUtility.DisplayDialog("清除所有数据", "确定要删除所有数据吗？此操作不可恢复！", "删除", "取消"))
            {
                return;
            }
            
            SODatabase.ClearDatabase();
            UnityEditor.EditorUtility.DisplayDialog("完成", "所有数据已清除", "确定");
            Debug.Log("[SOTestDataGenerator] 所有数据已清除");
        }
    }
}
