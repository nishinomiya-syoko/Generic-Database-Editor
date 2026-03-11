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
}