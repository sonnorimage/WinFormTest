using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace RecipeWinFormsApp
{
    public class Recipe
    {
        public string RecipeName { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Desc { get; set; } = string.Empty;

    }

    public static class RecipeJsonStorage
    {
        public static void SaveToJson(string filePath, Dictionary<string, List<Recipe>> dict)
        {
            string dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string json = JsonConvert.SerializeObject(
                dict ?? new Dictionary<string, List<Recipe>>(),
                Newtonsoft.Json.Formatting.Indented);

            File.WriteAllText(filePath, json, Encoding.UTF8);
        }

        public static Dictionary<string, List<Recipe>> LoadFromJson(string filePath)
        {
            var result = new Dictionary<string, List<Recipe>>(StringComparer.OrdinalIgnoreCase);
            if (!File.Exists(filePath)) return result;

            string json = File.ReadAllText(filePath, Encoding.UTF8);
            if (string.IsNullOrWhiteSpace(json)) return result;

            var loaded = JsonConvert.DeserializeObject<Dictionary<string, List<Recipe>>>(json);
            if (loaded == null) return result;

            foreach (var kvp in loaded)
            {
                if (!string.IsNullOrWhiteSpace(kvp.Key))
                {
                    result[kvp.Key] = kvp.Value ?? new List<Recipe>();
                }
            }
            return result;
        }
    }
}