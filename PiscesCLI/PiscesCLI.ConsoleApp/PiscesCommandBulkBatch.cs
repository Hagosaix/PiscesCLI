using ConsoleAppFramework;
using EverbloomingLab.PiscesCLI.Batch;
using EverbloomingLab.PiscesCLI.Bulk;
using EverbloomingLab.PiscesCLI.Core;
using File = System.IO.File;

namespace EverbloomingLab.PiscesCLI.Console
{
    public class PiscesCommandBulkBatch
    {
        [Command("bulk-text")]
        public async Task SendBulkText([Argument] string? clientCfgPath, [Argument] string textPromptFilePath)
        {
            if (!File.Exists(textPromptFilePath))
            {
                System.Console.WriteLine("[Console] Bulk text prompt file not found.");
            }
            else
            {
                if (CoreBootstrapper.GetClientConfig(clientCfgPath, out var clientCfg))
                {
                    var client = new Client(clientCfg!);
                    if (client.BootSucceeded) await BulkRunner.StartBulkTask(client, textPromptFilePath, BulkRunner.GetBulkTaskTextPrompt);
                }
            }
        }

        [Command("bulk-file")]
        public async Task SendBulkFile([Argument] string? clientCfgPath, [Argument] string fileDir)
        {
            if (!Directory.Exists(fileDir))
            {
                System.Console.WriteLine("[Console] Bulk prompt file directory not found.");
            }
            else
            {
                if (CoreBootstrapper.GetClientConfig(clientCfgPath, out var clientCfg))
                {
                    var client = new Client(clientCfg!);
                    if (client.BootSucceeded) await BulkRunner.StartBulkTask(client, fileDir, BulkRunner.GetBulkTaskInlineData);
                }
            }
        }

        [Command("batch-text")]
        public async Task BatchText([Argument] string? clientCfgPath, [Argument] string textPromptFilePath, [Argument] string jobLocalName = "")
        {
            if (!File.Exists(textPromptFilePath))
            {
                System.Console.WriteLine("[Console] Batch text prompt file not found.");
            }
            else
            {
                if (CoreBootstrapper.GetClientConfig(clientCfgPath, out var clientCfg))
                {
                    var client = new Client(clientCfg!);
                    if (client.BootSucceeded) await BatchRunner.SubmitBatchJobTextAsync(client, textPromptFilePath, jobLocalName);
                }
            }
        }

        [Command("fetch")]
        public async Task FetchBatchJob([Argument] string clientCfgPath, [Argument] string jobServerName)
        {
            if (!CoreBootstrapper.GetClientConfig(clientCfgPath, out var clientCfg))
            {
                System.Console.WriteLine("[Console] Client configuration not found.");
                return;
            }

            var client = new Client(clientCfg!);
            if (client.BootSucceeded) await client.FetchBatchJobAsync(jobServerName);
        }

        [Command("batch-file")]
        public async Task BatchFile([Argument] string clientCfgPath, [Argument] string fileDir, [Argument] string jobLocalName = "")
        {
            if (!Directory.Exists(fileDir))
            {
                System.Console.WriteLine("[Console] Batch prompt file directory not found.");
            }
            else
            {
                if (CoreBootstrapper.GetClientConfig(clientCfgPath, out var clientCfg))
                {
                    var client = new Client(clientCfg!);
                    if (client.BootSucceeded) await BatchRunner.SubmitBatchJobFileAsync(client, fileDir, jobLocalName);
                }
            }
        }
    }
}