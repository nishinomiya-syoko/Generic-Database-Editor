using System;
using UnityEngine;
using System.Collections.Generic;

namespace NiShiMiYa.GenericEditor
{
    public static class CSharpTypeToString
    {
        // 预定义支持的Unity常用类型（可扩展）
        // 预定义支持的Unity常用类型（可扩展）
        private static readonly Dictionary<string, Type> UnityCommonTypes = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
    {
        { "Vector2", typeof(Vector2) },
        { "Vector2Int", typeof(Vector2Int) }, // 新增
        { "Vector3", typeof(Vector3) },
        { "Vector3Int", typeof(Vector3Int) }, // 新增
        { "Vector4", typeof(Vector4) },
        { "Color", typeof(Color) },
        { "Rect", typeof(Rect) },
        { "Quaternion", typeof(Quaternion) }
    };
        public static string GetCSharpTypeName(string type)
        {
            var t = GetCSharpType(type);
            return GetCSharpTypeName(t);
        }

        /// <summary>
        /// 获取C#类型名（处理数组、泛型、Unity类型、基础类型）
        /// </summary>
        public static string GetCSharpTypeName(Type t)
        {
            if (t == null) return "object";

            // 处理数组
            if (t.IsArray)
                return $"{GetCSharpTypeName(t.GetElementType())}[]";

            // 处理泛型
            if (t.IsGenericType)
            {
                var genericDef = t.GetGenericTypeDefinition();
                // List<T>
                if (genericDef == typeof(List<>))
                    return $"List<{GetCSharpTypeName(t.GetGenericArguments()[0])}>";
                // Dictionary<K,V>
                if (genericDef == typeof(Dictionary<,>))
                {
                    var args = t.GetGenericArguments();
                    return $"Dictionary<{GetCSharpTypeName(args[0])}, {GetCSharpTypeName(args[1])}>";
                }
            }

            // 处理枚举
            if (t.IsEnum)
                return $"{t.Namespace}.{t.Name}";//changed

            // 基础类型别名替换
            string name = t.Name;
            name = name.Replace("Single", "float")
                      .Replace("Double", "double")
                      .Replace("Int32", "int")
                      .Replace("Int64", "long")
                      .Replace("Boolean", "bool")
                      .Replace("String", "string");

            // Unity内置类型直接返回原名
            if (UnityCommonTypes.ContainsKey(name))
                return name;

            return name;
        }

        // /// <summary>
        // /// 将数据表类型转换为C#类型（扩展支持更多类型+枚举）
        // /// </summary>
        public static Type GetCSharpType(string typeStr)
        {
            typeStr = typeStr.Trim();

            if (typeStr.EndsWith("[]"))
            {
                string inner = typeStr.Substring(0, typeStr.Length - 2);
                Type innerType = GetSimpleType(inner);
                return innerType.MakeArrayType();
            }

            if (typeStr.StartsWith("List<") && typeStr.EndsWith(">"))
            {
                string inner = typeStr.Substring(5, typeStr.Length - 6);
                Type innerType = GetSimpleType(inner);
                return typeof(List<>).MakeGenericType(innerType);
            }

            if (typeStr.StartsWith("Dictionary<") && typeStr.EndsWith(">"))
            {
                string inner = typeStr.Substring(11, typeStr.Length - 12);
                string[] parts = inner.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2)
                    throw new Exception($"Dictionary 格式错误：{typeStr}");

                Type keyType = GetSimpleType(parts[0].Trim());
                Type valType = GetSimpleType(parts[1].Trim());
                return typeof(Dictionary<,>).MakeGenericType(keyType, valType);
            }

            if (typeStr.StartsWith("Array<") && typeStr.EndsWith(">"))
            {
                string inner = typeStr.Substring(6, typeStr.Length - 7);
                Type innerType = GetSimpleType(inner);
                return typeof(List<>).MakeGenericType(innerType);
            }

            return GetSimpleType(typeStr);
        }

        private static Type GetSimpleType(string name)
        {
            string lowerName = name.ToLower().Trim();

            switch (lowerName)
            {
                case "int":
                case "integer":
                    return typeof(int);
                case "long":
                case "int64":
                    return typeof(long);
                case "float":
                case "single":
                    return typeof(float);
                case "double":
                    return typeof(double);
                case "bool":
                case "boolean":
                    return typeof(bool);
                case "string":
                case "str":
                case "text":
                    return typeof(string);
                case "byte":
                    return typeof(byte);
                case "short":
                case "int16":
                    return typeof(short);
                case "uint":
                case "uint32":
                    return typeof(uint);
                case "ulong":
                case "uint64":
                    return typeof(ulong);
                case "vector2":
                case "vec2":
                    return typeof(Vector2);
                case "vector2int":
                    return typeof(Vector2Int);
                case "vector3":
                case "vec3":
                    return typeof(Vector3);
                case "vector3int":
                    return typeof(Vector3Int);
                case "vector4":
                case "vec4":
                    return typeof(Vector4);
                case "color":
                    return typeof(Color);
                case "color32":
                    return typeof(Color32);
                case "quaternion":
                case "quat":
                    return typeof(Quaternion);
                case "rect":
                    return typeof(Rect);
                case "bounds":
                    return typeof(Bounds);
                case "datetime":
                case "date":
                    return typeof(DateTime);
                case "enum":
                    return typeof(int);
                default:
                    var t = Type.GetType($"{name}");
                    if (t != null) return t;

                    t = Type.GetType($"Top.{name}");
                    if (t != null) return t;

                    t = Type.GetType($"UnityEngine.{name}");
                    if (t != null) return t;

                    t = Type.GetType($"System.{name}");
                    if (t != null) return t;


                    throw new Exception($"未知类型：{name}");
            }
        }


        /// <summary>
        /// 将数据表类型转换为C#类型（扩展支持更多类型+枚举）
        /// </summary>
        /// <param name="tableType">TXT中的类型名（如int/Vector3/RoleType）</param>
        /// <param name="enumTypes">已定义的枚举类型列表（用于校验枚举）</param>
        /// <returns></returns>
        public static string GetCSharpTypeName(string tableType, List<Type> enumTypes = null)
        {
            // 先处理基础类型
            switch (tableType.ToLower())
            {
                case "int": return "int";
                case "float": return "float";
                case "double": return "double";
                case "long": return "long";
                case "bool": return "bool";
                case "string": return "string";
                // Unity常用类型
                case "vector2": return "Vector2";
                case "vector2int": return "Vector2Int";
                case "vector3": return "Vector3";
                case "vector3int": return "Vector3Int";
                case "vector4": return "Vector4";
                case "color": return "Color";
                case "rect": return "Rect";
                case "quaternion": return "Quaternion";
            }

            // 处理枚举类型（检查是否为已定义的枚举）
            enumTypes ??= new List<Type>();
            var enumType = enumTypes.Find(t => t.Name.Equals(tableType, StringComparison.OrdinalIgnoreCase));
            if (enumType != null && enumType.IsEnum)
            {
                return enumType.Name;
            }

            // 未识别类型：先尝试当作自定义类型（如枚举）返回原名，而非直接默认string
            Debug.LogWarning($"未识别的类型{tableType}，将作为自定义类型/枚举处理");
            return tableType;
        }


        /// <summary>
        /// 检查类型是否为枚举
        /// </summary>
        public static bool IsEnumType(string typeName, List<Type> enumTypes)
        {
            return enumTypes.Exists(t => t.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 获取项目中所有枚举类型（用于校验TXT中的枚举）
        /// </summary>
        public static List<Type> GetAllEnumTypes()
        {
            List<Type> enumTypes = new List<Type>();
            // 遍历当前程序集所有类型
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (type.IsEnum)
                        {
                            enumTypes.Add(type);
                        }
                    }
                }
                catch (Exception)
                {
                    // 跳过无法访问的程序集
                    continue;
                }
            }
            return enumTypes;
        }
    }
}