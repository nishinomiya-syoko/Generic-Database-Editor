using UnityEngine;

namespace SOEditor
{
    /// <summary>
    /// ScriptableObject 系统使用示例
    /// </summary>
    public class SOExampleUsage : MonoBehaviour
    {
        [Header("测试物品ID")]
        [SerializeField] private string _testItemId = "ITEM001";
        
        [Header("测试角色ID")]
        [SerializeField] private string _testCharacterId = "CHAR001";
        
        [Header("测试技能ID")]
        [SerializeField] private string _testSkillId = "SKILL001";
        
        [Header("测试任务ID")]
        [SerializeField] private string _testQuestId = "QUEST001";
        
        private void Start()
        {
            Debug.Log("=== SO Example Usage ===");
            
            // 确保运行时管理器已加载
            if (SORuntimeManager.Instance == null)
            {
                Debug.LogError("SORuntimeManager 未找到！请确保场景中有 SORuntimeManager 组件。");
                return;
            }
            
            // 示例 1: 获取物品
            Example_GetItem();
            
            // 示例 2: 获取角色
            Example_GetCharacter();
            
            // 示例 3: 获取技能
            Example_GetSkill();
            
            // 示例 4: 获取任务
            Example_GetQuest();
            
            // 示例 5: 搜索
            Example_Search();
            
            // 示例 6: 获取列表
            Example_GetLists();
            
            // 显示加载统计
            Debug.Log($"加载统计: {SORuntimeManager.Instance.GetLoadStatistics()}");
        }
        
        /// <summary>
        /// 示例: 获取物品
        /// </summary>
        private void Example_GetItem()
        {
            Debug.Log("--- 获取物品示例 ---");
            
            ItemSO item = SORuntimeManager.Instance.GetItem(_testItemId);
            
            if (item != null)
            {
                Debug.Log($"物品名称: {item.DisplayName}");
                Debug.Log($"物品类型: {item.ItemType}");
                Debug.Log($"购买价格: {item.BuyPrice}");
                Debug.Log($"出售价格: {item.SellPrice}");
                Debug.Log($"最大堆叠: {item.MaxStackSize}");
                Debug.Log($"稀有度: {item.Rarity}");
            }
            else
            {
                Debug.LogWarning($"未找到物品: {_testItemId}");
            }
        }
        
        /// <summary>
        /// 示例: 获取角色
        /// </summary>
        private void Example_GetCharacter()
        {
            Debug.Log("--- 获取角色示例 ---");
            
            CharacterSO character = SORuntimeManager.Instance.GetCharacter(_testCharacterId);
            
            if (character != null)
            {
                Debug.Log($"角色名称: {character.DisplayName}");
                Debug.Log($"角色类型: {character.CharacterType}");
                Debug.Log($"等级: {character.Level}");
                Debug.Log($"生命值: {character.MaxHealth}");
                Debug.Log($"法力值: {character.MaxMana}");
                Debug.Log($"力量: {character.Strength}");
                Debug.Log($"敏捷: {character.Agility}");
                Debug.Log($"智力: {character.Intelligence}");
            }
            else
            {
                Debug.LogWarning($"未找到角色: {_testCharacterId}");
            }
        }
        
        /// <summary>
        /// 示例: 获取技能
        /// </summary>
        private void Example_GetSkill()
        {
            Debug.Log("--- 获取技能示例 ---");
            
            SkillSO skill = SORuntimeManager.Instance.GetSkill(_testSkillId);
            
            if (skill != null)
            {
                Debug.Log($"技能名称: {skill.DisplayName}");
                Debug.Log($"技能类型: {skill.SkillType}");
                Debug.Log($"伤害类型: {skill.DamageType}");
                Debug.Log($"法力消耗: {skill.ManaCost}");
                Debug.Log($"冷却时间: {skill.Cooldown}");
                Debug.Log($"基础伤害: {skill.BaseDamage}");
                Debug.Log($"范围: {skill.Range}");
            }
            else
            {
                Debug.LogWarning($"未找到技能: {_testSkillId}");
            }
        }
        
        /// <summary>
        /// 示例: 获取任务
        /// </summary>
        private void Example_GetQuest()
        {
            Debug.Log("--- 获取任务示例 ---");
            
            QuestSO quest = SORuntimeManager.Instance.GetQuest(_testQuestId);
            
            if (quest != null)
            {
                Debug.Log($"任务名称: {quest.DisplayName}");
                Debug.Log($"任务类型: {quest.QuestType}");
                Debug.Log($"需求等级: {quest.RequiredLevel}");
                Debug.Log($"是否可重复: {quest.IsRepeatable}");
                
                if (quest.Objectives != null)
                {
                    Debug.Log($"任务目标数量: {quest.Objectives.Count}");
                    foreach (var objective in quest.Objectives)
                    {
                        Debug.Log($"  - {objective.Description} ({objective.CurrentAmount}/{objective.RequiredAmount})");
                    }
                }
                
                if (quest.Reward != null)
                {
                    Debug.Log($"经验奖励: {quest.Reward.Experience}");
                    Debug.Log($"金币奖励: {quest.Reward.Gold}");
                }
            }
            else
            {
                Debug.LogWarning($"未找到任务: {_testQuestId}");
            }
        }
        
        /// <summary>
        /// 示例: 搜索
        /// </summary>
        private void Example_Search()
        {
            Debug.Log("--- 搜索示例 ---");
            
            // 搜索物品
            var items = SORuntimeManager.Instance.SearchItems("剑");
            Debug.Log($"搜索 '剑' 找到 {items.Count} 个物品");
            
            // 搜索角色
            var characters = SORuntimeManager.Instance.SearchCharacters("哥布林");
            Debug.Log($"搜索 '哥布林' 找到 {characters.Count} 个角色");
            
            // 搜索技能
            var skills = SORuntimeManager.Instance.SearchSkills("火球");
            Debug.Log($"搜索 '火球' 找到 {skills.Count} 个技能");
        }
        
        /// <summary>
        /// 示例: 获取列表
        /// </summary>
        private void Example_GetLists()
        {
            Debug.Log("--- 获取列表示例 ---");
            
            // 获取所有物品
            var allItems = SORuntimeManager.Instance.GetAllItems();
            Debug.Log($"总物品数: {allItems.Count}");
            
            // 获取武器类物品
            var weapons = SORuntimeManager.Instance.GetItemsByType(ItemType.Weapon);
            Debug.Log($"武器数量: {weapons.Count}");
            
            // 获取所有角色
            var allCharacters = SORuntimeManager.Instance.GetAllCharacters();
            Debug.Log($"总角色数: {allCharacters.Count}");
            
            // 获取敌人类角色
            var enemies = SORuntimeManager.Instance.GetCharactersByType(CharacterType.Enemy);
            Debug.Log($"敌人数量: {enemies.Count}");
            
            // 获取所有技能
            var allSkills = SORuntimeManager.Instance.GetAllSkills();
            Debug.Log($"总技能数: {allSkills.Count}");
            
            // 获取主动技能
            var activeSkills = SORuntimeManager.Instance.GetSkillsByType(SkillType.Active);
            Debug.Log($"主动技能数量: {activeSkills.Count}");
            
            // 获取所有任务
            var allQuests = SORuntimeManager.Instance.GetAllQuests();
            Debug.Log($"总任务数: {allQuests.Count}");
            
            // 获取主线任务
            var mainQuests = SORuntimeManager.Instance.GetQuestsByType(QuestType.Main);
            Debug.Log($"主线任务数量: {mainQuests.Count}");
        }
        
        /// <summary>
        /// 示例: 通过代码创建物品（运行时）
        /// </summary>
        private void Example_CreateItemAtRuntime()
        {
            Debug.Log("--- 运行时创建物品示例 ---");
            
            // 创建新物品
            ItemSO newItem = ScriptableObject.CreateInstance<ItemSO>();
            newItem.GenerateId();
            newItem.SetDisplayName("测试物品");
            newItem.SetDescription("这是一个测试物品");
            newItem.SetItemType(ItemType.Consumable);
            newItem.SetBuyPrice(100);
            newItem.SetSellPrice(50);
            newItem.SetMaxStackSize(99);
            newItem.SetRarity(3);
            
            // 保存到数据库
            if (SODatabase.SaveScriptableObject(newItem))
            {
                Debug.Log($"物品创建成功: {newItem.Id}");
            }
            
            // 注意：运行时创建的物品不会自动加载到 SORuntimeManager
            // 需要手动重新加载数据库或添加到管理器
        }
        
        /// <summary>
        /// 示例: 重新加载数据库
        /// </summary>
        private void Example_ReloadDatabase()
        {
            Debug.Log("--- 重新加载数据库示例 ---");
            
            // 重新加载物品数据库
            SORuntimeManager.Instance.ReloadDatabase("Item");
            
            Debug.Log("物品数据库已重新加载");
        }
    }
}
