using ConsoleAppFramework;
using EverbloomingLab.PiscesCLI.Core;

namespace EverbloomingLab.PiscesCLI.Console
{
    public class PiscesCommandConfig
    {
        [Command("create-configs")]
        public void CreateCoreConfigs()
        {
            CoreConfig.AskToWriteDefaultConfig(AppPath._CoreConfigPath);
            ClientConfig.AskToWriteDefaultConfig(AppPath._DefaultClientConfigPath);
            ModelConfig.AskToWriteDefaultConfig(Path.Combine(AppPath._DefaultModelConfDirectory, "default_model_config.toml"));
        }

        [Command("create-conf")]
        public void CreateCoreConf()
        {
            CreateCoreConfigs();
        }
    }
}