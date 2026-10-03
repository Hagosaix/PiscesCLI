using ConsoleAppFramework;
using EverbloomingLab.PiscesCLI.Core;

namespace EverbloomingLab.PiscesCLI.Console
{
    public class PiscesCommandCore
    {
        [Command("connect")]
        public async Task TestConnectionAsync([Argument] string? clientConfigPath)
        {
            if (CoreBootstrapper.GetClientConfig(clientConfigPath, out var clientCfg))
            {
                var client = new Client(clientCfg!);
                if (client.BootSucceeded) await client.TestConnectionAsync();
            }
        }

        [Command("test-gene")]
        public async Task TestGenerationAsync([Argument] string? clientConfigPath)
        {
            if (CoreBootstrapper.GetClientConfig(clientConfigPath, out var clientCfg))
            {
                var client = new Client(clientCfg!);
                if (client.BootSucceeded) await client.TestGenerationAsync();
            }
        }

        [Command("generate")]
        public async Task GenerateAsync([Argument] string? clientConfigPath)
        {
            if (CoreBootstrapper.GetClientConfig(clientConfigPath, out var clientCfg))
            {
                var client = new Client(clientCfg!);
                if (client.BootSucceeded) await client.GenerateFromCliPromptAsync();
            }
        }

        [Command("gene")]
        public Task GeneAsync([Argument] string? clientConfigPath) => GenerateAsync(clientConfigPath);
    }
}