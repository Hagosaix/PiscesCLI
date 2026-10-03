namespace EverbloomingLab.PiscesCLI.Core
{
    public static class ClientContext
    {
        private static string _default_model;
        private static Dictionary<string, string> _model_api_names;
        private static Dictionary<string, string> _extension_to_mime;
        private static readonly Dictionary<string, ModelConfig> _model_config = new();

        public static bool _Started;

        public static void Start(CoreConfig? config)
        {
            if (_Started)
            {
                Console.WriteLine("[Core] Core already started.");
                return;
            }

            _default_model = string.IsNullOrEmpty(config?.DefaultModel) ? CoreConfig._Default.DefaultModel : config.DefaultModel;

            _model_api_names = config?.Model_Api_Names.Any() is true ? new Dictionary<string, string>(config.Model_Api_Names) : CoreConfig._Default.Model_Api_Names;

            _extension_to_mime = config?.Extension_To_Mime.Any() is true
                ? new Dictionary<string, string>(config.Extension_To_Mime, StringComparer.OrdinalIgnoreCase)
                : CoreConfig._Default.Extension_To_Mime;

            LoadModelConfig();

            _Started = true;

            Console.WriteLine("[Core] Core started.");
        }

        private static void LoadModelConfig()
        {
            foreach (var key in _model_api_names.Keys)
            {
                var modelConfigPath = Path.Combine(AppPath._DefaultModelConfDirectory, $"{key}.json");

                if (File.Exists(modelConfigPath))
                {
                    var conf = ReadModelConfig(modelConfigPath);
                    if (conf is null) continue;
                    _model_config.Add(key, conf);
                }
            }
        }

        public static string GetModelListDisplay() =>
            string.Join(" || ", _model_api_names.Select(kv => $"{kv.Key}: {kv.Value}"));

        public static string GetModelApiName(string model) => _model_api_names.GetValueOrDefault(model, _default_model);

        public static bool TryGetModelConfig(string modelName, out ModelConfig? modelConfig) => _model_config.TryGetValue(modelName, out modelConfig);

        public static string GetMimeType(string filePath)
        {
            var ext = Path.GetExtension(filePath);
            if (_extension_to_mime.TryGetValue(ext, out var mime))
                return mime;
            throw new NotSupportedException($"[Core] Gemini did not support file type: \"{ext}\" ({Path.GetFileName(filePath)})");
        }

        public static bool TryGetMimeType(string filePath, out string mimeType) => _extension_to_mime.TryGetValue(Path.GetExtension(filePath), out mimeType!);

        public static bool IsSupported(string filePath) =>
            _extension_to_mime.ContainsKey(Path.GetExtension(filePath));

        private static ModelConfig? ReadModelConfig(string modelNamePath) => TomlConfigSerializer.Load<ModelConfig>(modelNamePath);
    }
}