using CsvHelper;
using Google.GenAI.Types;
using System.Globalization;
using System.Text.Json;
using File = System.IO.File;
using GeminiClient = Google.GenAI.Client;
using GeminiFile = Google.GenAI.Types.File;

namespace EverbloomingLab.PiscesCLI.Core
{
    public class Client
    {
        public readonly ClientConfig Config;

        private readonly GeminiClient geminiClient;

        private string apiKey => $"{Config.ApiKey[..4]}*{Config.ApiKey[^4..]}";

        public string ModelApiName => ClientContext.GetModelApiName(Config.Model);

        public readonly bool BootSucceeded;

        public Client(ClientConfig config)
        {
            Config = config;

            try
            {
                geminiClient = new GeminiClient(apiKey: Config.ApiKey);
                BootSucceeded = true;
                Console.WriteLine($"[Client] Client created successfully.");
                ShowConfig();
            }
            catch (Exception e)
            {
                geminiClient = null!;
                Console.WriteLine($"[Client] Failed to create client: {e.Message}");
                BootSucceeded = false;
            }
        }

        #region Batch

        public async Task<BatchJob> CreateJobAsync(string sourceFileName, string? jobDisplayName = null)
        {
            Console.WriteLine($"[Client] Creating batch job for file: {sourceFileName}");
            var jobOption = new CreateBatchJobConfig { DisplayName = string.IsNullOrEmpty(jobDisplayName) ? sourceFileName : jobDisplayName };
            var source = new BatchJobSource { FileName = sourceFileName };
            var job = await geminiClient.Batches.CreateAsync(ModelApiName, source, jobOption);
            Console.WriteLine($"[Client] Batch job created: {job.Name}");
            return job;
        }

        public async Task<GeminiFile?> UploadFileAsync(string filePath)
        {
            if (!ClientContext.TryGetMimeType(filePath, out var mimeType))
            {
                Console.WriteLine($"[Client] Failed to upload '{filePath}'. Unsupported file type.");

                return null;
            }

            var displayName = Path.GetFileName(filePath);
            var upCfg = new UploadFileConfig { MimeType = mimeType, DisplayName = displayName };

            GeminiFile? upFile = null;

            Console.WriteLine($"[Client] Uploading file: {filePath}");

            try
            {
                upFile = await geminiClient.Files.UploadAsync(filePath, upCfg);
                Console.WriteLine($"[Client] File '{displayName}' uploaded successfully. Current state: {upFile.State}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"[Client] Failed to upload '{displayName}'. Error: {e.Message}");
            }

            return upFile;
        }

        public async Task FetchBatchJobAsync(string jobServerName)
        {
            Console.WriteLine($"[Client] Fetching batch job: {jobServerName}");
            try
            {
                var job = await geminiClient.Batches.GetAsync(jobServerName);

                var state = job.State?.ToString().ToUpper() ?? "UNKNOWN";
                if (state != JobState.JobStateSucceeded)
                {
                    Console.WriteLine($"""
                                       [Client] Batch job '{jobServerName}' failed or is not yet complete. Current state: {state}" );
                                                Please try again later.
                                       """);
                    return;
                }

                var dest = job.Dest;
                if (dest is not null)
                {
                    var outputFileId = dest.FileName;
                    if (string.IsNullOrEmpty(outputFileId))
                    {
                        Console.WriteLine($"[Client] Get job file failed. Info: {outputFileId}");
                        return;
                    }

                    var outputJsonlPath = AppPath.GetBatchJobOutputJsonlPath(job.DisplayName ?? jobServerName);

                    await geminiClient.Files.DownloadToFileAsync(outputFileId, outputJsonlPath);

                    Console.WriteLine("[Client] Job file download successful. Saved to: " + outputJsonlPath);

                    var outputCsvPath = AppPath.GetBatchJobOutputCsvFilePath(job.DisplayName ?? jobServerName);

                    WriteBatchJobResultJsonlToCsv(outputJsonlPath, outputCsvPath);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"[Client] Failed to fetch batch job '{jobServerName}'. Error: {e.Message}");
            }
        }

        public async Task<GeminiFile?> GetFileAsync(string fileServerName)
        {
            try
            {
                var file = await geminiClient.Files.GetAsync(fileServerName);
                return file;
            }
            catch (Exception e)
            {
                Console.WriteLine($"[Client] Failed to get file '{fileServerName}'. Error: {e.Message}");
                return null;
            }
        }

        #endregion

        #region Core

        public async Task TestConnectionAsync()
        {
            Console.WriteLine("[Client] Start Testing Connection...");

            try
            {
                var response = await geminiClient.Models.ListAsync();
                if (response.Count != 0)
                    Console.WriteLine("[Client] Connection test succeeded.");
                else
                    Console.WriteLine("[Client] Connection test failed: No models returned from the API.");
            }
            catch (Exception e)
            {
                Console.WriteLine($"[Client] Connection test failed: {e.Message}");
            }
        }

        public async Task TestGenerationAsync()
        {
            var testPrompt = "This is a connection test. PLEASE REPLY 'Connection successful.' ONLY, if the connection is successful. NO OTHER WORDS.";

            Console.WriteLine($"[Client] Sending test generation request...");
            var request = new GenerationRequest(testPrompt);
            var response = await GetGenerateResponseAsync(request);

            if (response.Response == "Connection successful.")
                Console.WriteLine($"[Client] Gemini API generation succeeded, model: {ModelApiName}.");
            else
                Console.WriteLine($"""
                                   [Client] Unexpected response from Gemini.
                                            Model: {ModelApiName}
                                            Response: 
                                            {response.Response}
                                            Time taken: {response.TimeTaken.TotalSeconds:F4} seconds
                                   """);
        }

        public async Task GenerateFromCliPromptAsync()
        {
            Console.WriteLine("[Client] Please enter your prompt: (enter '/c' to cancel.)");
            var input = Console.ReadLine();
            if (string.IsNullOrEmpty(input) || input == "/c")
            {
                Console.WriteLine("[Client] Generation cancelled.");
                return;
            }

            Console.WriteLine("[Client] Sending Generation request...");

            var request = new GenerationRequest(input);
            var response = await GetGenerateResponseAsync(request);

            Console.WriteLine($"""
                               [Client] Response: 
                                        {response.Response}
                                        ----
                                        Time taken: {response.TimeTaken.TotalSeconds:F4} seconds
                                        Input Token Count: {(response.TokenCount?.Input == 0 ? "Uncountable." : response.TokenCount?.Input.ToString())}
                                        Output Token Count: {(response.TokenCount?.Output == 0 ? "Uncountable." : response.TokenCount?.Output.ToString())}
                                        Total Token Count: {(response.TokenCount?.Total == 0 ? "Uncountable." : response.TokenCount?.Total.ToString())}
                               """);
        }

        public async Task<GenerationResponse> GetGenerateResponseAsync(GenerationRequest request)
        {
            var genContextCfg =
                ClientContext.TryGetModelConfig(Config.Model, out var modelCfg)                                    // Check if the model config exists for the specified model.
                    ? modelCfg?.GetGenerateContentConfig(Config.SystemPrompt) ?? Config.GetGenerateContentConfig() // Use model-specific config if available, otherwise use default config.
                    : Config.GetGenerateContentConfig();                                                           // Use default config if model config is not found.

            var response = new GenerationResponse(request);

            try
            {
                response.SetWaitingResponse();
                var resFromGoogle = await geminiClient.Models.GenerateContentAsync(ModelApiName, request.Content, genContextCfg);
                response.WriteResponse(GetFinalTextResponse(resFromGoogle));
                response.WriteTokenCount(new TokenCount(
                                                        resFromGoogle?.UsageMetadata?.PromptTokenCount     ?? 0,
                                                        resFromGoogle?.UsageMetadata?.CandidatesTokenCount ?? 0,
                                                        resFromGoogle?.UsageMetadata?.TotalTokenCount      ?? 0
                                                       ));
            }
            catch (Exception e)
            {
                response.WriteError(e.Message);
                Console.WriteLine($"[Client] Error occurred while generating content. Error: {e.Message}");
            }

            return response;
        }

        #endregion

        private void ShowConfig() =>
            Console.WriteLine($"""
                               Configuration Summary:
                               [Core]   Model list: {ClientContext.GetModelListDisplay()}
                                        Default Model: {ModelApiName}
                               [Client] API Key: {apiKey}
                                        Selected Model: {Config.Model}
                                        Response Format: {Config.ResponseMimeType}
                                        Rpm Limit: {Config.RpmLimit}
                                        Concurrency Limit: {Config.ConcurrencyLimit}
                                        System Prompt: {Config.SystemPrompt}
                               """);

        private static string GetFinalTextResponse(GenerateContentResponse response)
        {
            var parts = response.Candidates?[0].Content?.Parts;
            if (parts is null || !parts.Any()) return "[No response received from the model.]";

            var validTextParts = parts
                                .Where(p => p.Thought != true && !string.IsNullOrEmpty(p.Text))
                                .Select(p => p.Text);

            return string.Join("\n", validTextParts);
        }

        private static void WriteBatchJobResultJsonlToCsv(string jsonlFilePath, string csvFilePath)
        {
            using var writer = new StreamWriter(csvFilePath, false, System.Text.Encoding.UTF8);
            using var csvWriter = new CsvWriter(writer, CultureInfo.InvariantCulture);

            csvWriter.WriteField("TrackID");
            csvWriter.WriteField("TokenInput");
            csvWriter.WriteField("TokenOutput");
            csvWriter.WriteField("TokenTotal");
            csvWriter.WriteField("Response");
            csvWriter.WriteField("ExceptionNote");
            csvWriter.NextRecord();

            foreach (var line in File.ReadLines(jsonlFilePath))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                using var doc = JsonDocument.Parse(line);
                var root = doc.RootElement;
                var trackId = root.TryGetProperty("key", out var key) ? key.GetString() : string.Empty;

                if (!root.TryGetProperty("response", out var response))
                {
                    var errMsg = root.TryGetProperty("error", out var e) ? e.ToString() : string.Empty;
                    l_WriteRow(csvWriter, trackId, null, null, null, string.Empty, errMsg);
                    continue;
                }

                int? tokenIn = null, tokenOut = null, tokenTotal = null;
                if (response.TryGetProperty("usageMetadata", out var usageMetadata))
                {
                    tokenIn = usageMetadata.TryGetProperty("promptTokenCount", out var inputToken) ? inputToken.GetInt32() : (int?)null;
                    tokenOut = usageMetadata.TryGetProperty("candidatesTokenCount", out var outputToken) ? outputToken.GetInt32() : (int?)null;
                    tokenTotal = usageMetadata.TryGetProperty("totalTokenCount", out var totalToken) ? totalToken.GetInt32() : (int?)null;
                }

                var candidate = response.GetProperty("candidates")[0];
                var finishReason = candidate.TryGetProperty("finishReason", out var fr) ? fr.GetString() : null;

                var text = string.Empty;
                if (candidate.TryGetProperty("content", out var content) &&
                    content.TryGetProperty("parts", out var parts))
                    text = string.Concat(parts.EnumerateArray()
                                              .Where(pt => pt.TryGetProperty("text", out _))
                                              .Select(pt => pt.GetProperty("text").GetString()));

                l_WriteRow(csvWriter, trackId, tokenIn, tokenOut, tokenTotal, text,
                           finishReason == "STOP" ? string.Empty : finishReason ?? "UNKNOWN");
                continue;

                static void l_WriteRow(CsvWriter csvWriter, string? trackId,
                                       int? tokenInput, int? tokenOutput, int? tokenTotal, string? response, string? note)
                {
                    csvWriter.WriteField(trackId);
                    csvWriter.WriteField(tokenInput?.ToString()  ?? "Uncountable.");
                    csvWriter.WriteField(tokenOutput?.ToString() ?? "Uncountable.");
                    csvWriter.WriteField(tokenTotal?.ToString()  ?? "Uncountable.");
                    csvWriter.WriteField(response);
                    csvWriter.WriteField(note);
                    csvWriter.NextRecord();
                }
            }

            Console.WriteLine($"[Client] Batch job result JSONL file '{jsonlFilePath}' has been converted to CSV file '{csvFilePath}'.");
        }
    }
}