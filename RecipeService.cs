using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RecipeWinFormsApp
{
    public static class RecipeService
    {
        private static readonly string DefaultJsonPath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "recipes.json");

        private static Dictionary<string, List<Recipe>> _cache =
            new Dictionary<string, List<Recipe>>(StringComparer.OrdinalIgnoreCase);

        public static string CurrentRecipeKey { get; set; } = string.Empty;

        static RecipeService()
        {
            Reload();
        }

        public static void Reload(string filePath = null)
        {
            _cache = RecipeJsonStorage.LoadFromJson(filePath ?? DefaultJsonPath);
        }

        public static void UpdateCache(Dictionary<string, List<Recipe>> dict, string currentKey)
        {
            _cache = dict ?? new Dictionary<string, List<Recipe>>(StringComparer.OrdinalIgnoreCase);
            CurrentRecipeKey = currentKey ?? string.Empty;
        }

        public static List<Recipe> GetRecipeList(string recipeKey)
        {
            if (string.IsNullOrWhiteSpace(recipeKey))
                return new List<Recipe>();

            if (_cache.TryGetValue(recipeKey, out List<Recipe> items) && items != null)
            {
                return items.Select(x => new Recipe
                {
                    RecipeName = x.RecipeName,
                    Value = x.Value,
                    Desc = x.Desc
                }).ToList();
            }

            return new List<Recipe>();
        }

        public static List<Recipe> GetCurrentRecipeList()
        {
            return GetRecipeList(CurrentRecipeKey);
        }

        public static string GetValue(string recipeKey, string recipeName, string defaultValue = "")
        {
            var list = GetRecipeList(recipeKey);
            var found = list.FirstOrDefault(x =>
                string.Equals(x.RecipeName, recipeName, StringComparison.OrdinalIgnoreCase));

            return found != null ? found.Value : defaultValue;
        }

        public static int GetIntValue(string recipeKey, string recipeName, int defaultValue = 0)
        {
            string raw = GetValue(recipeKey, recipeName, null);
            return int.TryParse(raw, out int result) ? result : defaultValue;
        }
    }
}