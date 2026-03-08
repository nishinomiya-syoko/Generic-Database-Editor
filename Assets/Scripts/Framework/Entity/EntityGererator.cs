// using UnityEngine;
// using System.Linq;

// public class EntityGenerator : MonoSingleton<EntityGenerator>
// {
//     public EntityData[] entityDatas;
//     /// <summary>
//     /// 生成实体
//     /// </summary>
//     /// <param name="entityId"></param>
//     /// <param name="position"></param>
//     /// <returns></returns>
//     public GameObject Generate(int entityId, Vector3 position)
//     {
//         EntityData entityData = entityDatas.FirstOrDefault(x => x.id == entityId);
//         if (entityData != null)
//         {
//             GameObject instance = Instantiate(entityData.prefab, position, Quaternion.identity);
//             return instance;
//         }
//         else
//         {
//             Debug.LogError($"未找到ID为 {entityId} 的实体数据");
//         }
//         return null;
//     }
// }
// [System.Serializable]
// public class EntityData
// {
//     public string name;
//     public int id;
//     public GameObject prefab;
// }