using EverbloomingLab.PiscesCLI.Core;
using Client = EverbloomingLab.PiscesCLI.Core.Client;

namespace EverbloomingLab.PiscesCLI.Bulk
{
    public static class BulkRunner
    {
        public static async Task StartBulkTask(Client client, string fileOrDirPath, Func<string, BulkTask?> getBulkTask)
        {
            var bTask = getBulkTask(fileOrDirPath);

            if (bTask is null) return;

            await bTask.StartTask(client);

            if (bTask.State is BulkTask.EState.Completed or BulkTask.EState.CompletedWithError)
                bTask.WriteToCsv(Path.Combine(AppPath._DefaultOutputDirectory, $"bulk_task_{DateTime.Now:yyMMdd_HHmmss}_output.csv"));
        }

        public static string[] GetTextPrompts(string promptsFilePath)
        {
            var prompt = File.ReadAllLines(promptsFilePath);

            Console.WriteLine($"[Bulk] Prompt loaded, total {prompt.Length} lines.");
            return prompt;
        }

        public static (byte[][]? data, string[]? fileName) GetInlineDataPrompts(string dataDirectory)
        {
            var files = Directory.GetFiles(dataDirectory);

            if (files.Length == 0)
            {
                Console.WriteLine($"[Bulk] No files found in directory '{dataDirectory}'.");
                return (null, null);
            }

            // For simplicity, just return the byte data of all images
            var imageDataArray = files.Select(File.ReadAllBytes).ToArray();

            foreach (var file in files)
            {
                Console.WriteLine($"[Bulk] Loaded image data from '{file}'.");
            }

            return (imageDataArray, files);
        }

        public static BulkTask? GetBulkTaskInlineData(string dataDirectory)
        {
            var prompts = GetInlineDataPrompts(dataDirectory);
            if (prompts.data is null || prompts.fileName is null)
            {
                Console.WriteLine("[Bulk] No inline data found.");
                return null;
            }

            return new InlineDataTask(prompts.data, prompts.fileName);
        }

        public static BulkTask? GetBulkTaskTextPrompt(string promptPath)
        {
            var prompts = GetTextPrompts(promptPath);
            if (prompts.Any()) return new TextTask(prompts);

            Console.WriteLine("[Bulk] No prompts found.");
            return null;
        }
    }
}