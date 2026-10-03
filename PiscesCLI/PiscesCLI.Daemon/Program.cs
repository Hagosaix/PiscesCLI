namespace EverbloomingLab.PiscesCLI.Daemon
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine($"{DateTime.Now:HH:mm:ss} [Daemon] Starting...");

            var host = new DaemonHost();

            if (host.StartSucceeded)
                await host.RunAsync(11435);
            else
                Console.WriteLine($"{DateTime.Now:HH:mm:ss} [Daemon] Failed to start.");
        }
    }
}