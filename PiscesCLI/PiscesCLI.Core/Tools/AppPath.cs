namespace EverbloomingLab.PiscesCLI.Core
{
    public static class AppPath
    {
        public static readonly string _StartTimestamp = $"{DateTime.Now:yyMMdd_HHmmss}";

        public static readonly string _DefaultAppDirectory = AppContext.BaseDirectory;

        public static readonly string _DefaultCacheDirectory = Path.Combine(_DefaultAppDirectory, "cache");

        public static readonly string _DefaultOutputDirectory = Path.Combine(_DefaultAppDirectory, "output");

        public static readonly string _DefaultModelConfDirectory = Path.Combine(_DefaultAppDirectory, "model_conf");

        public static readonly string _CoreConfigPath = Path.Combine(_DefaultAppDirectory, "core_config.toml");

        public static readonly string _DefaultClientConfigPath = Path.Combine(_DefaultAppDirectory, "client_config.toml");

        public static string GetBatchJobUploadJsonlPath(string localJobName)
        {
            var fixedName = localJobName.Replace("\\", "_").Replace("/", "_");
            return Path.Combine(_DefaultCacheDirectory, $"batch_job_{fixedName}_{_StartTimestamp}_upload.jsonl");
        }

        public static string GetBatchJobOutputJsonlPath(string localJobName)
        {
            var fixedName = localJobName.Replace("\\", "_").Replace("/", "_");
            return Path.Combine(_DefaultOutputDirectory, $"batch_job_{fixedName}_{_StartTimestamp}_output.jsonl");
        }

        public static string GetBatchJobObjectJsonFilePath(string localJobName)
        {
            var fixedName = localJobName.Replace("\\", "_").Replace("/", "_");
            return Path.Combine(_DefaultCacheDirectory, $"batch_job_{fixedName}_{_StartTimestamp}_object.json");
        }

        public static string GetBatchJobOutputCsvFilePath(string localJobName)
        {
            var fixedName = localJobName.Replace("\\", "_").Replace("/", "_");
            return Path.Combine(_DefaultOutputDirectory, $"batch_job_{fixedName}_{_StartTimestamp}_output.csv");
        }
    }
}