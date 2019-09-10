using System;
using System.Text;
using MarukoLib.Logging;
using Newtonsoft.Json;

namespace MarukoLib.Persistence
{

    public static class JsonUtils
    {
        
        private static readonly Logger Logger = Logger.GetLogger(typeof(JsonUtils));

        public static readonly Encoding DefaultEncoding = Encoding.UTF8;

        public static JsonSerializerSettings PrettyFormat = new JsonSerializerSettings {Formatting = Formatting.Indented};

        public static T DeserializeFromFile<T>(string file, JsonSerializerSettings settings = null, Encoding encoding = null)
        {
            TryDeserializeFromFile<T>(file, out var result, settings, encoding);
            return result;
        }

        public static bool TryDeserializeFromFile<T>(string file, out T result, JsonSerializerSettings settings = null, Encoding encoding = null)
        {
            if (System.IO.File.Exists(file))
            {
                try
                {
                    result = Deserialize<T>(System.IO.File.ReadAllText(file, encoding ?? DefaultEncoding), settings);
                    return true;
                }
                catch (Exception e)
                {
                    Logger.Error("TryDeserializeFromFile", e, "file", file);
                }
            }
            result = default;
            return false;
        }

        public static object DeserializeFromFile(string file, Type type, JsonSerializerSettings settings = null, Encoding encoding = null)
        {
            TryDeserializeFromFile(file, type, out var result, settings, encoding);
            return result;
        }

        public static bool TryDeserializeFromFile(string file, Type type, out object result, JsonSerializerSettings settings = null, Encoding encoding = null)
        {
            if (System.IO.File.Exists(file))
            {
                try
                {
                    result = Deserialize(System.IO.File.ReadAllText(file, encoding ?? DefaultEncoding), type, settings);
                    return true;
                }
                catch (Exception e)
                {
                    Logger.Error("TryDeserializeFromFile", e, "file", file);
                }
            }
            result = default;
            return false;
        }

        public static T Deserialize<T>(string json, JsonSerializerSettings settings = null) =>
            settings == null ? JsonConvert.DeserializeObject<T>(json) : JsonConvert.DeserializeObject<T>(json, settings);

        public static object Deserialize(string json, Type type, JsonSerializerSettings settings = null) =>
            settings == null ? JsonConvert.DeserializeObject(json, type) : JsonConvert.DeserializeObject(json, type, settings);

        public static bool SerializeToFile(string file, object val, JsonSerializerSettings settings = null, Encoding encoding = null)
        {
            try
            {
                System.IO.File.WriteAllText(file, Serialize(val, settings), encoding ?? DefaultEncoding);
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("SerializeToFile", e, "file", file, "val", val);
                return false;
            }
        }

        public static string Serialize(object val, JsonSerializerSettings settings = null) => settings == null ? JsonConvert.SerializeObject(val) : JsonConvert.SerializeObject(val, settings);

        #region Extention Methods

        public static bool JsonSerializeToFile(this object val, string file, JsonSerializerSettings settings = null, Encoding encoding = null) => SerializeToFile(file, val, settings, encoding);

        public static string JsonSerializeAsString(this object val, JsonSerializerSettings settings = null) => Serialize(val, settings);

        #endregion

    }
}
