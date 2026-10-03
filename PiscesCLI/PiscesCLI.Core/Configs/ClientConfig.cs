using Google.GenAI.Types;

namespace EverbloomingLab.PiscesCLI.Core
{
    public record ClientConfig(
        string ApiKey,
        string Model,
        int MaxOutputTokens,
        string ResponseMimeType,
        int ConcurrencyLimit,
        int RpmLimit,
        string SystemPrompt)
    {
        public static readonly ClientConfig _Default =
            new(
                "",
                "",
                65536,
                "text/plain",
                2,
                10,
                ""
               );

        public const string DEFAULT_CONFIG_HEADER_COMMENT =
            """
             # =========================================
             # Auto-generated configuration file
             # PiscesCLI Client Configuration
             #
             # [api_key]
             #   Your Google Gemini API key.
             #   Required. An empty value will cause client boot failure.
             #   Example: "AIzaSy..."
             #
             # [model]
             #   Model alias key as defined in core config (model_api_names).
             #   This is NOT the raw Gemini API model name — it is a short alias
             #   that maps to the actual API name via ClientContext.
             #   If empty or unrecognized, falls back to the core default model.
             #   Example: "flash", "pro" — must match a key in core config.
             #
             # [max_output_tokens]
             #   Maximum number of tokens the model may generate per response.
             #   Default: 65536
             #
             # [response_mime_type]
             #   MIME type of the model's response content.
             #   "text/plain"        — plain text output (default)
             #   "application/json"  — structured JSON output
             #
             # [concurrency_limit]
             #   Maximum number of simultaneous generation requests in flight.
             #   Default: 2. Raise with caution; too high may trigger quota errors.
             #
             # [rpm_limit]
             #   Maximum requests per minute (RPM) sent to the API.
             #   Default: 10. Must not exceed your Gemini API quota tier.
             #
             # [system_prompt]
             #   System-level instruction injected before every generation request.
             #   Use this to define the model's persona, role, or task constraints.
             #   Leave empty for no system instruction.
             # =========================================
            """;

        public static void AskToWriteDefaultConfig(string filePath)
        {
            if (Tools.GetConfirmation("Do you want to build a default client configuration? Please fill configuration details."))
                TomlConfigSerializer.Save(filePath, _Default, DEFAULT_CONFIG_HEADER_COMMENT);
        }

        public GenerateContentConfig GetGenerateContentConfig() =>
            new()
            {
                MaxOutputTokens = MaxOutputTokens,
                ResponseMimeType = ResponseMimeType,
                SafetySettings = CoreConfig._SafetySetting,
                SystemInstruction = new Content
                {
                    Parts = [new Part { Text = SystemPrompt }],
                },
            };
    }
}