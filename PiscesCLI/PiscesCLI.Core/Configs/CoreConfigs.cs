using Google.GenAI.Types;

namespace EverbloomingLab.PiscesCLI.Core
{
    public record CoreConfig
    {
        public string DefaultModel { get; init; } = string.Empty;
        public Dictionary<string, string> Model_Api_Names { get; init; } = new();
        public Dictionary<string, string> Extension_To_Mime { get; init; } = new();

        public static CoreConfig _Default => _default ??= new CoreConfig
        {
            DefaultModel = "models/gemini-3.5-flash-lite",
            Model_Api_Names = new Dictionary<string, string>
            {
                ["3.1lite"] = "models/gemini-3.1-flash-lite",
                ["3.5lite"] = "models/gemini-3.5-flash-lite",
                ["3.5flash"] = "models/gemini-3.5-flash",
                ["3.6flash"] = "models/gemini-3.6-flash",
                ["pro"] = "models/gemini-3.1-pro-preview",
            },
            Extension_To_Mime = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                // ── text ──
                [".html"] = "text/html",
                [".htm"] = "text/html",
                [".css"] = "text/css",
                [".txt"] = "text/plain",
                [".text"] = "text/plain",
                [".log"] = "text/plain",
                [".md"] = "text/plain",
                [".xml"] = "text/xml",
                [".csv"] = "text/csv",
                [".rtf"] = "text/rtf",
                [".js"] = "text/javascript",
                [".mjs"] = "text/javascript",
                // ── application ──
                [".json"] = "application/json",
                [".jsonl"] = "application/jsonl",
                [".pdf"] = "application/pdf",
                // ── image ──
                [".bmp"] = "image/bmp",
                [".jpg"] = "image/jpeg",
                [".jpeg"] = "image/jpeg",
                [".jpe"] = "image/jpeg",
                [".png"] = "image/png",
                [".webp"] = "image/webp",
                // ── video ──
                [".mp4"] = "video/mp4",
                [".m4v"] = "video/mp4",
                [".mpeg"] = "video/mpeg",
                [".mpg"] = "video/mpg",
                [".mov"] = "video/quicktime",
                [".qt"] = "video/quicktime",
                [".avi"] = "video/avi",
                [".flv"] = "video/x-flv",
                [".webm"] = "video/webm",
                [".wmv"] = "video/wmv",
                [".3gp"] = "video/3gpp",
                [".3gpp"] = "video/3gpp",
            },
        };

        public static readonly List<SafetySetting> _SafetySetting =
        [
            new()
            {
                Category = HarmCategory.HarmCategoryHateSpeech,
                Threshold = HarmBlockThreshold.BlockNone,
            },
            new()
            {
                Category = HarmCategory.HarmCategoryDangerousContent,
                Threshold = HarmBlockThreshold.BlockNone,
            },
            new()
            {
                Category = HarmCategory.HarmCategorySexuallyExplicit,
                Threshold = HarmBlockThreshold.BlockNone,
            },
            new()
            {
                Category = HarmCategory.HarmCategoryHarassment,
                Threshold = HarmBlockThreshold.BlockNone,
            },
        ];

        public const string DEFAULT_CONFIG_HEADER_COMMENT =
            """
             # =========================================
             # Auto-generated configuration file
             # PiscesCLI Core Configuration
             #
             # [default_model]
             #   The fallback model API name used when the client config specifies
             #   an unrecognized or empty model alias.
             #   This must be a full Gemini API model name (e.g. "models/gemini-3.5-flash-lite"),
             #   NOT a short alias key.
             #
             # [model_api_names]
             #   A mapping from short alias keys to full Gemini API model name strings.
             #   Clients reference models by alias key (e.g. "3.5flash", "pro").
             #   The alias key is used in client config [model] field and per-model
             #   config file naming (e.g. "3.5flash.json" under the model conf directory).
             #   If this section is absent or empty, the built-in default mapping is used.
             #   Example:
             #     3.5flash = "models/gemini-3.5-flash"
             #     pro      = "models/gemini-3.1-pro-preview"
             #
             # [extension_to_mime]
             #   A mapping from file extension (including leading dot, case-insensitive)
             #   to MIME type string, used when uploading files to the Gemini Files API.
             #   Extensions not listed here are treated as unsupported and will be rejected
             #   at upload time. If this section is absent or empty, the built-in default
             #   mapping (covering common text, image, video, and document formats) is used.
             #   Example:
             #     .png  = "image/png"
             #     .mp4  = "video/mp4"
             #     .pdf  = "application/pdf"
             # =========================================
            """;

        private static CoreConfig? _default;

        public static void AskToWriteDefaultConfig(string filePath)
        {
            if (Tools.GetConfirmation("Do you want to build a default core configuration? Please fill configuration details."))
                TomlConfigSerializer.Save(filePath, _Default, DEFAULT_CONFIG_HEADER_COMMENT);
        }
    }
}