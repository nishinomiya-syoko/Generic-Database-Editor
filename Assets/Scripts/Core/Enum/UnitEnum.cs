namespace Top
{
    // 单位类型枚举
    public enum UnitType
    {
        Unit,
        Infantry,//步兵
        // Cavalry,//骑兵
        Ranged,//远程
        Siege,//攻城
        Flying,//飞行
        Hero,//英雄

        Building,
        Defense,
        Resource,
        Wall,

        Any,
    }

    // 单位状态枚举
    public enum UnitState
    {
        Idle,
        Moving,
        Attacking,
        Dead,
        Training,
        //建筑
        Building,
        Working,
    }
    // 建筑类型枚举
    public enum BuildingFunction
    {
        Resource,
        Defense,
        Military,
        Technology,
        Alchemy,
    }

    // 建筑状态枚举
    public enum BuildingState
    {
        Idle,
        Building,
        Upgrading,
        Working,
        Destroyed
    }
}