using UnityEngine;
using DataCenter;

public class DataLoader : MonoBehaviour
{
     /// <summary>
        /// 初始化数据表（按需加载指定表）
        /// </summary>
        public void InitData()
        {
            // 按需取消注释以加载对应的数据表
            //LoadTable<TowerInfoContainer, TowerInfo>();
            //LoadTable<PlayerInfo1Container, PlayerInfo1>();
            //LoadTable<TestInfoContainer, TestInfo>();
            //LoadTable<BasicDataContainer, BasicData>();
            BinaryDataMgr.Instance.LoadTable<DTBasicData>();
            Debug.Log("数据加载完成");
        }
}