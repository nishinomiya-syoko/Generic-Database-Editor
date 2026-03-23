using System;
using System.Reflection;
using Unity.VisualScripting;
using UnityEngine;
public static class MethodExtensionForUnity
{
   // 针对 Transform 的扩展方法
   public static T GetOrAddComponent<T>(this Transform origin) where T : Component
   {
       T component = origin.GetComponent<T>();
       if (component == null)
       {
           component = origin.gameObject.AddComponent<T>();
       }
       return component;
   }
    // 针对 GameObject 的扩展方法
    public static T GetOrAddComponent<T>(this GameObject origin) where T : Component
    {
        T component = origin.GetComponent<T>();
        if (component == null)
        {
            component = origin.AddComponent<T>();
        }
        return component;
    }
//    public static Component GetOrAddComponent(this GameObject origin, string componentName)
//     {
//         Type targetType = Type.GetType(componentName);
//         if (targetType == null)
//         {
//             DebugInfo.LogError("未找到类型：" + componentName);
//             return null;
//         }
//         MethodInfo method = typeof(MethodExtensionForUnity)
//             .GetMethod("GetOrAddComponent", BindingFlags.Public | BindingFlags.Static)
//             .MakeGenericMethod(targetType);
//         object component = method.Invoke(null, new object[] { origin });
//         if (component == null)
//         {
//             DebugInfo.LogError("未找到组件：" + componentName);
//             return null;
//         }
//         return origin.GetComponent(targetType);
//     }
}