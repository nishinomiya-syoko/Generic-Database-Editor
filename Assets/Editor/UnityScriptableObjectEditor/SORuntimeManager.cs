using System;
using System.Collections.Generic;
using UnityEngine;

namespace SOEditor
{
    /// <summary>
    /// 运行时 ScriptableObject 管理器
    /// 在游戏启动时加载所有 SO 数据
    /// </summary>
    public class SORuntimeManager : MonoBehaviour
    {
        [Header("设置")]
        [SerializeField] private bool _loadOnStart = true;
        [SerializeField] private bool _dontDestroyOnLoad = true;
        
        // 存储所有加载的 ScriptableObject
        private Dictionary<string, ItemSO> _items = new Dictionary<string, ItemSO>();
        private Dictionary<string, CharacterSO> _characters = new Dictionary<string, CharacterSO>();
        private Dictionary<string, SkillSO> _skills = new Dictionary<string, SkillSO>();
        private Dictionary<string, QuestSO> _quests = new Dictionary<string, QuestSO>();
        
        // 单例实例
        private static SORuntimeManager _instance;
        public static SORuntimeManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<SORuntimeManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("SORuntimeManager");
                        _instance = go.AddComponent<SORuntimeManager>();
                    }
                }
                return _instance;
            }
        }
        
        // 事件
        public event Action OnDatabaseLoaded;
        public event Action<string> OnTypeLoaded;
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            
            if (_dontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }
        }
        
        private void Start()
        {
            if (_loadOnStart)
            {
                LoadAllDatabases();
            }
        }
        
        /// <summary>
        /// 加载所有数据库
        /// </summary>
        public void LoadAllDatabases()
        {
            Debug.Log("[SORuntimeManager] 开始加载数据库...");
            
            LoadItemDatabase();
            LoadCharacterDatabase();
            LoadSkillDatabase();
            LoadQuestDatabase();
            
            OnDatabaseLoaded?.Invoke();
            
            Debug.Log("[SORuntimeManager] 数据库加载完成");
        }
        
        /// <summary>
        /// 加载物品数据库
        /// </summary>
        public void LoadItemDatabase()
        {
            _items.Clear();
            List<ItemData> itemDataList = SODatabase.LoadAllData<ItemData>("Item");
            
            foreach (ItemData data in itemDataList)
            {
                ItemSO item = ScriptableObject.CreateInstance<ItemSO>();
                item.LoadFromSerializableData(data);
                _items[item.Id] = item;
            }
            
            Debug.Log($"[SORuntimeManager] 已加载 {_items.Count} 个物品");
            OnTypeLoaded?.Invoke("Item");
        }
        
        /// <summary>
        /// 加载角色数据库
        /// </summary>
        public void LoadCharacterDatabase()
        {
            _characters.Clear();
            List<CharacterData> charDataList = SODatabase.LoadAllData<CharacterData>("Character");
            
            foreach (CharacterData data in charDataList)
            {
                CharacterSO character = ScriptableObject.CreateInstance<CharacterSO>();
                character.LoadFromSerializableData(data);
                _characters[character.Id] = character;
            }
            
            Debug.Log($"[SORuntimeManager] 已加载 {_characters.Count} 个角色");
            OnTypeLoaded?.Invoke("Character");
        }
        
        /// <summary>
        /// 加载技能数据库
        /// </summary>
        public void LoadSkillDatabase()
        {
            _skills.Clear();
            List<SkillData> skillDataList = SODatabase.LoadAllData<SkillData>("Skill");
            
            foreach (SkillData data in skillDataList)
            {
                SkillSO skill = ScriptableObject.CreateInstance<SkillSO>();
                skill.LoadFromSerializableData(data);
                _skills[skill.Id] = skill;
            }
            
            Debug.Log($"[SORuntimeManager] 已加载 {_skills.Count} 个技能");
            OnTypeLoaded?.Invoke("Skill");
        }
        
        /// <summary>
        /// 加载任务数据库
        /// </summary>
        public void LoadQuestDatabase()
        {
            _quests.Clear();
            List<QuestData> questDataList = SODatabase.LoadAllData<QuestData>("Quest");
            
            foreach (QuestData data in questDataList)
            {
                QuestSO quest = ScriptableObject.CreateInstance<QuestSO>();
                quest.LoadFromSerializableData(data);
                _quests[quest.Id] = quest;
            }
            
            Debug.Log($"[SORuntimeManager] 已加载 {_quests.Count} 个任务");
            OnTypeLoaded?.Invoke("Quest");
        }
        
        #region 获取方法
        
        /// <summary>
        /// 获取物品
        /// </summary>
        public ItemSO GetItem(string id)
        {
            _items.TryGetValue(id, out ItemSO item);
            return item;
        }
        
        /// <summary>
        /// 获取所有物品
        /// </summary>
        public List<ItemSO> GetAllItems()
        {
            return new List<ItemSO>(_items.Values);
        }
        
        /// <summary>
        /// 按类型获取物品
        /// </summary>
        public List<ItemSO> GetItemsByType(ItemType type)
        {
            List<ItemSO> result = new List<ItemSO>();
            foreach (var item in _items.Values)
            {
                if (item.ItemType == type)
                {
                    result.Add(item);
                }
            }
            return result;
        }
        
        /// <summary>
        /// 获取角色
        /// </summary>
        public CharacterSO GetCharacter(string id)
        {
            _characters.TryGetValue(id, out CharacterSO character);
            return character;
        }
        
        /// <summary>
        /// 获取所有角色
        /// </summary>
        public List<CharacterSO> GetAllCharacters()
        {
            return new List<CharacterSO>(_characters.Values);
        }
        
        /// <summary>
        /// 按类型获取角色
        /// </summary>
        public List<CharacterSO> GetCharactersByType(CharacterType type)
        {
            List<CharacterSO> result = new List<CharacterSO>();
            foreach (var character in _characters.Values)
            {
                if (character.CharacterType == type)
                {
                    result.Add(character);
                }
            }
            return result;
        }
        
        /// <summary>
        /// 获取技能
        /// </summary>
        public SkillSO GetSkill(string id)
        {
            _skills.TryGetValue(id, out SkillSO skill);
            return skill;
        }
        
        /// <summary>
        /// 获取所有技能
        /// </summary>
        public List<SkillSO> GetAllSkills()
        {
            return new List<SkillSO>(_skills.Values);
        }
        
        /// <summary>
        /// 按类型获取技能
        /// </summary>
        public List<SkillSO> GetSkillsByType(SkillType type)
        {
            List<SkillSO> result = new List<SkillSO>();
            foreach (var skill in _skills.Values)
            {
                if (skill.SkillType == type)
                {
                    result.Add(skill);
                }
            }
            return result;
        }
        
        /// <summary>
        /// 获取任务
        /// </summary>
        public QuestSO GetQuest(string id)
        {
            _quests.TryGetValue(id, out QuestSO quest);
            return quest;
        }
        
        /// <summary>
        /// 获取所有任务
        /// </summary>
        public List<QuestSO> GetAllQuests()
        {
            return new List<QuestSO>(_quests.Values);
        }
        
        /// <summary>
        /// 按类型获取任务
        /// </summary>
        public List<QuestSO> GetQuestsByType(QuestType type)
        {
            List<QuestSO> result = new List<QuestSO>();
            foreach (var quest in _quests.Values)
            {
                if (quest.QuestType == type)
                {
                    result.Add(quest);
                }
            }
            return result;
        }
        
        #endregion
        
        #region 搜索方法
        
        /// <summary>
        /// 搜索物品（按名称）
        /// </summary>
        public List<ItemSO> SearchItems(string searchTerm)
        {
            List<ItemSO> result = new List<ItemSO>();
            string term = searchTerm.ToLower();
            
            foreach (var item in _items.Values)
            {
                if (item.DisplayName.ToLower().Contains(term) ||
                    item.Description.ToLower().Contains(term))
                {
                    result.Add(item);
                }
            }
            return result;
        }
        
        /// <summary>
        /// 搜索角色
        /// </summary>
        public List<CharacterSO> SearchCharacters(string searchTerm)
        {
            List<CharacterSO> result = new List<CharacterSO>();
            string term = searchTerm.ToLower();
            
            foreach (var character in _characters.Values)
            {
                if (character.DisplayName.ToLower().Contains(term) ||
                    character.Description.ToLower().Contains(term))
                {
                    result.Add(character);
                }
            }
            return result;
        }
        
        /// <summary>
        /// 搜索技能
        /// </summary>
        public List<SkillSO> SearchSkills(string searchTerm)
        {
            List<SkillSO> result = new List<SkillSO>();
            string term = searchTerm.ToLower();
            
            foreach (var skill in _skills.Values)
            {
                if (skill.DisplayName.ToLower().Contains(term) ||
                    skill.Description.ToLower().Contains(term))
                {
                    result.Add(skill);
                }
            }
            return result;
        }
        
        /// <summary>
        /// 搜索任务
        /// </summary>
        public List<QuestSO> SearchQuests(string searchTerm)
        {
            List<QuestSO> result = new List<QuestSO>();
            string term = searchTerm.ToLower();
            
            foreach (var quest in _quests.Values)
            {
                if (quest.DisplayName.ToLower().Contains(term) ||
                    quest.Description.ToLower().Contains(term))
                {
                    result.Add(quest);
                }
            }
            return result;
        }
        
        #endregion
        
        /// <summary>
        /// 重新加载指定类型的数据库
        /// </summary>
        public void ReloadDatabase(string soType)
        {
            switch (soType)
            {
                case "Item":
                    LoadItemDatabase();
                    break;
                case "Character":
                    LoadCharacterDatabase();
                    break;
                case "Skill":
                    LoadSkillDatabase();
                    break;
                case "Quest":
                    LoadQuestDatabase();
                    break;
            }
        }
        
        /// <summary>
        /// 获取加载统计信息
        /// </summary>
        public string GetLoadStatistics()
        {
            return $"物品: {_items.Count}, 角色: {_characters.Count}, 技能: {_skills.Count}, 任务: {_quests.Count}";
        }
    }
}
