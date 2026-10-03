namespace EverbloomingLab.PiscesCLI.Core
{
    public static class CoreBootstrapper
    {
        public static bool GetClientConfig(string? clientCfgPath, out ClientConfig? clientConfig)
        {
            // verify work dir
            Directory.CreateDirectory(AppPath._DefaultAppDirectory);
            Directory.CreateDirectory(AppPath._DefaultCacheDirectory);
            Directory.CreateDirectory(AppPath._DefaultOutputDirectory);
            Directory.CreateDirectory(AppPath._DefaultModelConfDirectory);

            clientConfig = null;

            if (!File.Exists(clientCfgPath))
            {
                Console.WriteLine($"[Core] Client configuration file not found at: {clientCfgPath}");
                return false;
            }

            // verify core config
            var coreCfg = TomlConfigSerializer.Load<CoreConfig>(AppPath._CoreConfigPath);

            if (coreCfg is null) Console.WriteLine("[Core] Core configuration not found. Loading default configuration...");

            // start core
            ClientContext.Start(coreCfg);

            // verify client config
            var clientCfg = TomlConfigSerializer.Load<ClientConfig>(clientCfgPath);

            if (clientCfg is not null)
            {
                Console.WriteLine($"[Core] Client configuration loaded successfully.");
                clientConfig = clientCfg;
                return true;
            }

            Console.WriteLine($"[Core] Client configuration not found. Exiting...");
            return false;
        }
    }
}