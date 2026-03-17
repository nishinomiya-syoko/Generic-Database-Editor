using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Text;
using System.Linq;
using System.Reflection;
using OfficeOpenXml;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using DataCenter;

namespace Top
{
    // 【修复】添加 Unity 类型自定义转换器
    public class Vector2Converter : JsonConverter<Vector2>
    {
        public override void WriteJson(JsonWriter writer, Vector2 value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("x");
            writer.WriteValue(value.x);
            writer.WritePropertyName("y");
            writer.WriteValue(value.y);
            writer.WriteEndObject();
        }

        public override Vector2 ReadJson(JsonReader reader, Type objectType, Vector2 existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var dict = serializer.Deserialize<Dictionary<string, float>>(reader);
            return new Vector2(dict["x"], dict["y"]);
        }
    }

    public class Vector3Converter : JsonConverter<Vector3>
    {
        public override void WriteJson(JsonWriter writer, Vector3 value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("x");
            writer.WriteValue(value.x);
            writer.WritePropertyName("y");
            writer.WriteValue(value.y);
            writer.WritePropertyName("z");
            writer.WriteValue(value.z);
            writer.WriteEndObject();
        }

        public override Vector3 ReadJson(JsonReader reader, Type objectType, Vector3 existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var dict = serializer.Deserialize<Dictionary<string, float>>(reader);
            return new Vector3(dict["x"], dict["y"], dict["z"]);
        }
    }

    public class Vector4Converter : JsonConverter<Vector4>
    {
        public override void WriteJson(JsonWriter writer, Vector4 value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("x");
            writer.WriteValue(value.x);
            writer.WritePropertyName("y");
            writer.WriteValue(value.y);
            writer.WritePropertyName("z");
            writer.WriteValue(value.z);
            writer.WritePropertyName("w");
            writer.WriteValue(value.w);
            writer.WriteEndObject();
        }

        public override Vector4 ReadJson(JsonReader reader, Type objectType, Vector4 existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var dict = serializer.Deserialize<Dictionary<string, float>>(reader);
            return new Vector4(dict["x"], dict["y"], dict["z"], dict["w"]);
        }
    }

    public class ColorConverter : JsonConverter<Color>
    {
        public override void WriteJson(JsonWriter writer, Color value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("r");
            writer.WriteValue(value.r);
            writer.WritePropertyName("g");
            writer.WriteValue(value.g);
            writer.WritePropertyName("b");
            writer.WriteValue(value.b);
            writer.WritePropertyName("a");
            writer.WriteValue(value.a);
            writer.WriteEndObject();
        }

        public override Color ReadJson(JsonReader reader, Type objectType, Color existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var dict = serializer.Deserialize<Dictionary<string, float>>(reader);
            return new Color(dict["r"], dict["g"], dict["b"], dict["a"]);
        }
    }

    public class QuaternionConverter : JsonConverter<Quaternion>
    {
        public override void WriteJson(JsonWriter writer, Quaternion value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("x");
            writer.WriteValue(value.x);
            writer.WritePropertyName("y");
            writer.WriteValue(value.y);
            writer.WritePropertyName("z");
            writer.WriteValue(value.z);
            writer.WritePropertyName("w");
            writer.WriteValue(value.w);
            writer.WriteEndObject();
        }

        public override Quaternion ReadJson(JsonReader reader, Type objectType, Quaternion existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var dict = serializer.Deserialize<Dictionary<string, float>>(reader);
            return new Quaternion(dict["x"], dict["y"], dict["z"], dict["w"]);
        }
    }

    public class RectConverter : JsonConverter<Rect>
    {
        public override void WriteJson(JsonWriter writer, Rect value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("x");
            writer.WriteValue(value.x);
            writer.WritePropertyName("y");
            writer.WriteValue(value.y);
            writer.WritePropertyName("width");
            writer.WriteValue(value.width);
            writer.WritePropertyName("height");
            writer.WriteValue(value.height);
            writer.WriteEndObject();
        }

        public override Rect ReadJson(JsonReader reader, Type objectType, Rect existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var dict = serializer.Deserialize<Dictionary<string, float>>(reader);
            return new Rect(dict["x"], dict["y"], dict["width"], dict["height"]);
        }
    }

    public class BoundsConverter : JsonConverter<Bounds>
    {
        public override void WriteJson(JsonWriter writer, Bounds value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("center");
            serializer.Serialize(writer, value.center);
            writer.WritePropertyName("size");
            serializer.Serialize(writer, value.size);
            writer.WriteEndObject();
        }

        public override Bounds ReadJson(JsonReader reader, Type objectType, Bounds existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var dict = serializer.Deserialize<Dictionary<string, object>>(reader);
            Vector3 center = JsonConvert.DeserializeObject<Vector3>(dict["center"].ToString());
            Vector3 size = JsonConvert.DeserializeObject<Vector3>(dict["size"].ToString());
            return new Bounds(center, size);
        }
    }
}