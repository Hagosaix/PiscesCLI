using System.Text.Json;
using Tomlyn;

namespace EverbloomingLab.PiscesCLI.Core
{
    public static class TomlConfigSerializer
    {
        private static readonly TomlSerializerOptions _options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = true,
        };

        public static T? Load<T>(string? filePath) where T : class
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return null;

            var toml = TomlSerializer.Deserialize<T>(File.ReadAllText(filePath), _options);
            return toml;
        }

        public static void Save<T>(string filePath, T config, string headerComment = "") where T : class
        {
            var toml = TomlSerializer.Serialize(config, _options);

            if (!string.IsNullOrEmpty(headerComment))
                toml = $"""
                        {headerComment}
                        {toml}
                        """;

            File.WriteAllText(filePath, toml);
            Console.WriteLine($"[Core] Toml configuration '{config}' saved to {filePath}");
        }
    }
}