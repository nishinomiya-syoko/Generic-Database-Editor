using System;
using UnityEngine;

namespace SOEditor
{
    /// <summary>
    /// 角色类型枚举
    /// </summary>
    public enum CharacterType
    {
        Player,
        NPC,
        Enemy,
        Boss,
        Companion
    }
    
    /// <summary>
    /// 角色 ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "NewCharacter", menuName = "SO Editor/Character")]
    public class CharacterSO : ScriptableObjectBase
    {
        [Header("角色属性")]
        [SerializeField] private CharacterType _characterType = CharacterType.NPC;
        [SerializeField] private int _level = 1;
        [SerializeField] private int _maxHealth = 100;
        [SerializeField] private int _maxMana = 50;
        [SerializeField] private int _strength = 10;
        [SerializeField] private int _agility = 10;
        [SerializeField] private int _intelligence = 10;
        [SerializeField] private int _defense = 5;
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _attackSpeed = 1f;
        [SerializeField] private float _attackRange = 2f;
        
        [Header("AI 属性")]
        [SerializeField] private bool _hasAI;
        [SerializeField] private float _detectionRange = 10f;
        [SerializeField] private float _chaseRange = 15f;
        
        [Header("掉落")]
        [SerializeField] private int _experienceReward;
        [SerializeField] private int _goldReward;
        
        // 属性访问器
        public CharacterType CharacterType => _characterType;
        public int Level => _level;
        public int MaxHealth => _maxHealth;
        public int MaxMana => _maxMana;
        public int Strength => _strength;
        public int Agility => _agility;
        public int Intelligence => _intelligence;
        public int Defense => _defense;
        public float MoveSpeed => _moveSpeed;
        public float AttackSpeed => _attackSpeed;
        public float AttackRange => _attackRange;
        public bool HasAI => _hasAI;
        public float DetectionRange => _detectionRange;
        public float ChaseRange => _chaseRange;
        public int ExperienceReward => _experienceReward;
        public int GoldReward => _goldReward;
        
        public override string GetSOType() => "Character";
        
        public override object GetSerializableData()
        {
            return new CharacterData
            {
                Id = Id,
                DisplayName = DisplayName,
                Description = Description,
                IconPath = Icon != null ? Icon.name : "",
                CharacterType = _characterType,
                Level = _level,
                MaxHealth = _maxHealth,
                MaxMana = _maxMana,
                Strength = _strength,
                Agility = _agility,
                Intelligence = _intelligence,
                Defense = _defense,
                MoveSpeed = _moveSpeed,
                AttackSpeed = _attackSpeed,
                AttackRange = _attackRange,
                HasAI = _hasAI,
                DetectionRange = _detectionRange,
                ChaseRange = _chaseRange,
                ExperienceReward = _experienceReward,
                GoldReward = _goldReward
            };
        }
        
        public override void LoadFromSerializableData(object data)
        {
            if (data is CharacterData charData)
            {
                SetId(charData.Id);
                SetDisplayName(charData.DisplayName);
                SetDescription(charData.Description);
                _characterType = charData.CharacterType;
                _level = charData.Level;
                _maxHealth = charData.MaxHealth;
                _maxMana = charData.MaxMana;
                _strength = charData.Strength;
                _agility = charData.Agility;
                _intelligence = charData.Intelligence;
                _defense = charData.Defense;
                _moveSpeed = charData.MoveSpeed;
                _attackSpeed = charData.AttackSpeed;
                _attackRange = charData.AttackRange;
                _hasAI = charData.HasAI;
                _detectionRange = charData.DetectionRange;
                _chaseRange = charData.ChaseRange;
                _experienceReward = charData.ExperienceReward;
                _goldReward = charData.GoldReward;
            }
        }
        
        // 设置方法
        public void SetCharacterType(CharacterType type) => _characterType = type;
        public void SetLevel(int level) => _level = level;
        public void SetMaxHealth(int health) => _maxHealth = health;
        public void SetMaxMana(int mana) => _maxMana = mana;
        public void SetStrength(int str) => _strength = str;
        public void SetAgility(int agi) => _agility = agi;
        public void SetIntelligence(int intel) => _intelligence = intel;
        public void SetDefense(int def) => _defense = def;
        public void SetMoveSpeed(float speed) => _moveSpeed = speed;
        public void SetAttackSpeed(float speed) => _attackSpeed = speed;
        public void SetAttackRange(float range) => _attackRange = range;
        public void SetHasAI(bool hasAI) => _hasAI = hasAI;
        public void SetDetectionRange(float range) => _detectionRange = range;
        public void SetChaseRange(float range) => _chaseRange = range;
        public void SetExperienceReward(int exp) => _experienceReward = exp;
        public void SetGoldReward(int gold) => _goldReward = gold;
    }
    
    /// <summary>
    /// 角色序列化数据
    /// </summary>
    [Serializable]
    public class CharacterData
    {
        public string Id;
        public string DisplayName;
        public string Description;
        public string IconPath;
        public CharacterType CharacterType;
        public int Level;
        public int MaxHealth;
        public int MaxMana;
        public int Strength;
        public int Agility;
        public int Intelligence;
        public int Defense;
        public float MoveSpeed;
        public float AttackSpeed;
        public float AttackRange;
        public bool HasAI;
        public float DetectionRange;
        public float ChaseRange;
        public int ExperienceReward;
        public int GoldReward;
    }
}
