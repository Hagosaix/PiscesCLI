namespace EverbloomingLab.PiscesCLI.Console
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            System.Console.WriteLine("[Console] PiscesCLI Console Start...");
            var app = ConsoleAppFramework.ConsoleApp.Create();

            app.Add<PiscesCommandCore>();
            app.Add<PiscesCommandConfig>();
            app.Add<PiscesCommandBulkBatch>();

            await app.RunAsync(args);

            System.Console.WriteLine("[Console] PiscesCLI Console End.");
        }
    }
}