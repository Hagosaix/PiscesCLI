using Google.GenAI.Types;
using System.Text.Encodings.Web;
using System.Text.Json;
using EverbloomingLab.PiscesCLI.Core;

namespace EverbloomingLab.PiscesCLI.Batch
{
    public class Jsonl : File
    {
        public bool WriteCompleted { get; private set; } = false;

        public Jsonl(string jobLocalName,
                     string filePath) : base(filePath) =>
            JobLocalName = jobLocalName;

        public void CreateInlineTextPrompts(Client client, string[] prompts)
        {
            if (WriteCompleted)
            {
                Console.WriteLine($"[Batch] JSONL file '{LocalFilePath}' has already been written.");
                return;
            }

            var lines = prompts.Select((prompt, i) => GetJsonLine(client, $"{JobLocalName}_prompt_{i}", prompt));

            WriteJsonlFile(lines);
        }

        public void CreateFilePrompts(Client client, IEnumerable<File> fileInfos)
        {
            var fileActive = fileInfos.Where(f => f.State == FileState.Active).ToArray();
            Console.WriteLine($"[Batch] {fileActive.Length} files are active.");

            if (!fileActive.Any()) return;

            var lines = fileActive.Select((file, i) => GetJsonLine(client, $"{JobLocalName}_file_{i}", file.Uri, file.MimeType));
            WriteJsonlFile(lines);
        }

        private void WriteJsonlFile(IEnumerable<object> lines)
        {
            using var fs = new FileStream(LocalFilePath, FileMode.CreateNew, FileAccess.Write);
            using var writer = new StreamWriter(fs);

            foreach (var linObj in lines)
            {
                writer.WriteLine(JsonSerializer.Serialize(linObj, _json_serializer_options));
            }

            Console.WriteLine($"[Batch] JSONL file '{LocalFilePath}' has been written.");

            WriteCompleted = true;
        }

        private static object GetJsonLine(Client client, string trackId, string userPrompt)
        {
            return new
            {
                key = trackId,
                request = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[] { new { text = userPrompt } },
                        },
                    },
                    systemInstruction = new
                    {
                        parts = new[] { new { text = client.Config.SystemPrompt } },
                    },
                    generationConfig = new
                    {
                        maxOutputTokens = client.Config.MaxOutputTokens,
                        responseMimeType = client.Config.ResponseMimeType,
                    },
                    safetySettings = new[]
                    {
                        new { category = "HARM_CATEGORY_HATE_SPEECH", threshold = "BLOCK_NONE" },
                        new { category = "HARM_CATEGORY_DANGEROUS_CONTENT", threshold = "BLOCK_NONE" },
                        new { category = "HARM_CATEGORY_SEXUALLY_EXPLICIT", threshold = "BLOCK_NONE" },
                        new { category = "HARM_CATEGORY_HARASSMENT", threshold = "BLOCK_NONE" },
                    },
                },
            };
        }

        private static object GetJsonLine(Client client, string trackId, string fileUrl, string mimeType)
        {
            return new
            {
                key = trackId,
                request = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new
                                {
                                    fileData = new
                                    {
                                        fileUri = fileUrl,
                                        mimeType = mimeType,
                                    },
                                },
                            },
                        },
                    },
                    systemInstruction = new
                    {
                        parts = new[] { new { text = client.Config.SystemPrompt } },
                    },
                    generationConfig = new
                    {
                        maxOutputTokens = client.Config.MaxOutputTokens,
                        responseMimeType = client.Config.ResponseMimeType,
                    },
                    safetySettings = new[]
                    {
                        new { category = "HARM_CATEGORY_HATE_SPEECH", threshold = "BLOCK_NONE" },
                        new { category = "HARM_CATEGORY_DANGEROUS_CONTENT", threshold = "BLOCK_NONE" },
                        new { category = "HARM_CATEGORY_SEXUALLY_EXPLICIT", threshold = "BLOCK_NONE" },
                        new { category = "HARM_CATEGORY_HARASSMENT", threshold = "BLOCK_NONE" },
                    },
                },
            };
        }

        private static readonly JsonSerializerOptions _json_serializer_options = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
    }
}