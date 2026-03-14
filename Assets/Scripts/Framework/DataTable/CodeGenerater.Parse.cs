
// public partial class CodeGenerater
// {
//     private object ParseCellValue(string value, Type targetType)
//     {
//         if (string.IsNullOrEmpty(value) || value.Trim() == "")
//             return GetDefaultValue(targetType);

//         value = value.Trim();

//         try
//         {
//             // 基本数值类型
//             if (targetType == typeof(int)) return int.Parse(value);
//             if (targetType == typeof(long)) return long.Parse(value);
//             if (targetType == typeof(float)) return float.Parse(value);
//             if (targetType == typeof(double)) return double.Parse(value);
//             if (targetType == typeof(bool))
//                 return value.ToLower() == "true" || value == "1" || value.ToLower() == "yes";
//             if (targetType == typeof(string)) return value;
//             if (targetType == typeof(byte)) return byte.Parse(value);
//             if (targetType == typeof(short)) return short.Parse(value);
//             if (targetType == typeof(uint)) return uint.Parse(value);
//             if (targetType == typeof(ulong)) return ulong.Parse(value);

//             // Unity 内置类型
//             if (targetType == typeof(Vector2)) return ParseVector2(value);
//             if (targetType == typeof(Vector3)) return ParseVector3(value);
//             if (targetType == typeof(Vector4)) return ParseVector4(value);
//             if (targetType == typeof(Color)) return ParseColor(value);
//             if (targetType == typeof(Color32)) return ParseColor32(value);
//             if (targetType == typeof(Quaternion)) return ParseQuaternion(value);
//             if (targetType == typeof(Rect)) return ParseRect(value);
//             if (targetType == typeof(Bounds)) return ParseBounds(value);
//             if (targetType == typeof(DateTime)) return ParseDateTime(value);

//             // List<T>
//             if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(List<>))
//             {
//                 Type innerType = targetType.GetGenericArguments()[0];
//                 return ParseCollection(value, innerType, targetType, true);
//             }

//             // Array
//             if (targetType.IsArray)
//             {
//                 Type innerType = targetType.GetElementType();
//                 return ParseCollection(value, innerType, targetType, false);
//             }

//             // Dictionary<K,V>
//             if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
//             {
//                 return ParseDictionary(value, targetType);
//             }

//             // 自定义类 (JSON)
//             if (!targetType.IsPrimitive && targetType != typeof(string) && !targetType.IsEnum)
//             {
//                 return JsonConvert.DeserializeObject(value, targetType);
//             }
//         }
//         catch (Exception ex)
//         {
//             throw new Exception($"解析失败 [{targetType.Name}]: {ex.Message}");
//         }

//         return GetDefaultValue(targetType);
//     }

//     private Vector2 ParseVector2(string value)
//     {
//         return (Vector2)ParseVector(value, 2);
//     }

//     private Vector3 ParseVector3(string value)
//     {
//         return (Vector3)ParseVector(value, 3);
//     }

//     private Vector4 ParseVector4(string value)
//     {
//         return (Vector4)ParseVector(value, 4);
//     }

//     private object ParseVector(string value, int dimensions)
//     {
//         // 支持格式：(1,2,3) 或 {1,2,3} 或 1,2,3 或 JSON
//         value = value.Replace("(", "").Replace(")", "").Replace("{", "").Replace("}", "");
//         string[] parts = value.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

//         float[] nums = new float[dimensions];
//         for (int i = 0; i < dimensions && i < parts.Length; i++)
//         {
//             nums[i] = float.Parse(parts[i].Trim());
//         }

//         if (dimensions == 2) return new Vector2(nums[0], nums[1]);
//         if (dimensions == 3) return new Vector3(nums[0], nums[1], nums[2]);
//         if (dimensions == 4) return new Vector4(nums[0], nums[1], nums[2], nums[3]);

//         return Vector3.zero;
//     }

//     private Color ParseColor(string value)
//     {
//         value = value.Replace("(", "").Replace(")", "").Replace("{", "").Replace("}", "");
//         string[] parts = value.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

//         if (parts.Length >= 3)
//         {
//             float r = float.Parse(parts[0].Trim());
//             float g = float.Parse(parts[1].Trim());
//             float b = float.Parse(parts[2].Trim());
//             float a = parts.Length >= 4 ? float.Parse(parts[3].Trim()) : 1f;
//             return new Color(r, g, b, a);
//         }

//         return Color.white;
//     }

//     private Color32 ParseColor32(string value)
//     {
//         value = value.Replace("(", "").Replace(")", "").Replace("{", "").Replace("}", "");
//         string[] parts = value.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

//         if (parts.Length >= 3)
//         {
//             byte r = byte.Parse(parts[0].Trim());
//             byte g = byte.Parse(parts[1].Trim());
//             byte b = byte.Parse(parts[2].Trim());
//             byte a = parts.Length >= 4 ? byte.Parse(parts[3].Trim()) : (byte)255;
//             return new Color32(r, g, b, a);
//         }

//         return Color.white;
//     }

//     private Quaternion ParseQuaternion(string value)
//     {
//         value = value.Replace("(", "").Replace(")", "").Replace("{", "").Replace("}", "");
//         string[] parts = value.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

//         if (parts.Length >= 4)
//         {
//             return new Quaternion(
//                 float.Parse(parts[0].Trim()),
//                 float.Parse(parts[1].Trim()),
//                 float.Parse(parts[2].Trim()),
//                 float.Parse(parts[3].Trim())
//             );
//         }

//         return Quaternion.identity;
//     }

//     private Rect ParseRect(string value)
//     {
//         value = value.Replace("(", "").Replace(")", "").Replace("{", "").Replace("}", "");
//         string[] parts = value.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

//         if (parts.Length >= 4)
//         {
//             return new Rect(
//                 float.Parse(parts[0].Trim()),
//                 float.Parse(parts[1].Trim()),
//                 float.Parse(parts[2].Trim()),
//                 float.Parse(parts[3].Trim())
//             );
//         }

//         return Rect.zero;
//     }

//     private Bounds ParseBounds(string value)
//     {
//         // 简化处理，建议使用 JSON 格式
//         if (value.Trim().StartsWith("{"))
//         {
//             return JsonConvert.DeserializeObject<Bounds>(value);
//         }
//         return new Bounds(Vector3.zero, Vector3.one);
//     }

//     private DateTime ParseDateTime(string value)
//     {
//         if (DateTime.TryParse(value, out DateTime result))
//             return result;
//         return DateTime.MinValue;
//     }

//     private object ParseCollection(string value, Type innerType, Type collectionType, bool isList)
//     {
//         // JSON 格式：[1,2,3]
//         if (value.Trim().StartsWith("["))
//         {
//             return JsonConvert.DeserializeObject(value, collectionType);
//         }

//         // 分隔符格式：1|2|3 或 1,2,3
//         char separator = value.Contains("|") ? '|' : ',';
//         string[] parts = value.Split(separator, StringSplitOptions.RemoveEmptyEntries);

//         if (isList)
//         {
//             IList list = (IList)Activator.CreateInstance(collectionType);
//             foreach (var part in parts)
//             {
//                 list.Add(ParseCellValue(part.Trim(), innerType));
//             }
//             return list;
//         }
//         else
//         {
//             Array arr = Array.CreateInstance(innerType, parts.Length);
//             for (int i = 0; i < parts.Length; i++)
//             {
//                 arr.SetValue(ParseCellValue(parts[i].Trim(), innerType), i);
//             }
//             return arr;
//         }
//     }

//     private object ParseDictionary(string value, Type dictType)
//     {
//         // 强制 JSON 格式：{"key": value}
//         if (value.Trim().StartsWith("{"))
//         {
//             return JsonConvert.DeserializeObject(value, dictType);
//         }

//         throw new Exception("Dictionary 类型必须使用 JSON 格式，如：{\"key\": 1}");
//     }

//     private object GetDefaultValue(Type t)
//     {
//         if (t.IsValueType)
//             return Activator.CreateInstance(t);
//         return null;
//     }
// }