using Google.GenAI.Types;

namespace EverbloomingLab.PiscesCLI.Core
{
    public record ModelConfig(
        string Name,
        int MaxOutputTokens,
        string ResponseMimeType,
        string ThinkingLevel)
    {
        public const string DEFAULT_CONFIG_HEADER_COMMENT =
            """
             # =========================================
             # Auto-generated configuration file
             # PiscesCLI Model Configuration
             #
             # This file controls per-model generation parameters for a specific model alias.
             # Place this file in the model config directory, named after the model alias key
             # (e.g. "3.5flash.toml"). If a file exists for the active model alias, its settings
             # override the corresponding fields in the client config for that model.
             #
             # [name]
             #   The model alias key this config applies to (e.g. "3.5flash", "pro").
             #   This is informational only; the actual routing is determined by the filename.
             #   Leave empty to apply as a generic fallback config.
             #
             # [max_output_tokens]
             #   Maximum number of tokens the model may generate per response.
             #   Default: 65536
             #   This value overrides the client config max_output_tokens for this model.
             #
             # [response_mime_type]
             #   MIME type of the model's response content.
             #   "text/plain"        — plain text output (default)
             #   "application/json"  — structured JSON output
             #   This value overrides the client config response_mime_type for this model.
             #
             # [thinking_level]
             #   Controls the model's internal reasoning/thinking budget.
             #   Must be a valid ThinkingLevel enum name from the Gemini SDK:
             #     ThinkingLevelUnspecified  — use model default (recommended)
             #     ThinkingLevelNone         — disable thinking
             #     ThinkingLevelLow          — minimal thinking budget
             #     ThinkingLevelMedium       — moderate thinking budget
             #     ThinkingLevelHigh         — maximum thinking budget
             #   An invalid value will cause config application to fail and fall back
             #   to the client config for this request.
             # =========================================
            """;

        public static readonly ModelConfig _Default =
            new("",
                65536,
                "text/plain",
                "ThinkingLevelUnspecified"
               );

        public static void AskToWriteDefaultConfig(string filePath)
        {
            if (Tools.GetConfirmation("Do you want to build a default model configuration? Please fill configuration details."))
                TomlConfigSerializer.Save(filePath, _Default, DEFAULT_CONFIG_HEADER_COMMENT);
        }

        public GenerateContentConfig? GetGenerateContentConfig(string systemPrompt)
        {
            try
            {
                return new GenerateContentConfig
                {
                    ThinkingConfig = new ThinkingConfig
                    {
                        ThinkingLevel = Enum.Parse<ThinkingLevel>(ThinkingLevel),
                    },
                    MaxOutputTokens = MaxOutputTokens,
                    ResponseMimeType = ResponseMimeType,
                    SafetySettings = CoreConfig._SafetySetting,
                    SystemInstruction = new Content
                    {
                        Parts = [new Part { Text = systemPrompt }],
                    },
                };
            }
            catch (Exception e)
            {
                Console.WriteLine($"[Core] Error occurred while generating content config from model config '{Name}'. Error: {e.Message}");
            }

            return null;
        }
    }
}