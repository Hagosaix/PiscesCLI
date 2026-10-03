using CsvHelper;
using System.Globalization;
using System.Threading.RateLimiting;
using EverbloomingLab.PiscesCLI.Core;
using Client = EverbloomingLab.PiscesCLI.Core.Client;
using Core_Client = EverbloomingLab.PiscesCLI.Core.Client;

namespace EverbloomingLab.PiscesCLI.Bulk
{
    public abstract class BulkTask
    {
        private readonly List<Task<GenerationResponse>> tasks = [];

        public EState State { get; private set; } = EState.NotStarted;

        public async Task StartTask(Core_Client client)
        {
            // prepare semaphore and rate limiter
            using var semaphore = new SemaphoreSlim(client.Config.ConcurrencyLimit, client.Config.ConcurrencyLimit);
            await using var rpmLimiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
            {
                PermitLimit = client.Config.RpmLimit,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 6,
                QueueLimit = int.MaxValue,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            });

            // run the task
            State = EState.Running;
            var requests = GetGenerationRequest().ToArray();
            if (!requests.Any())
            {
                State = EState.Error;
                Console.WriteLine("[BulkTask] Create Generation request failed.");
                return;
            }

            foreach (var rqst in requests)
            {
                tasks.Add(DispatchAsync(client, rqst, semaphore, rpmLimiter));
                Console.WriteLine($"[BulkTask] #{rqst.TrackId} Request dispatched.");
            }

            await Task.WhenAll(tasks);

            // determine the final state based on the results of the tasks
            var states = tasks.Select(t => t.Result.State).ToArray();

            var anyOk = states.Any(s => s  == GenerationResponse.EState.Completed);
            var anyBad = states.Any(s => s != GenerationResponse.EState.Completed);

            State = (anyOk, anyBad) switch
            {
                (true, false) => EState.Completed,
                (true, true)  => EState.CompletedWithError,
                _             => EState.Error,
            };

            Console.WriteLine($"[BulkTask] Task completed with state: {State}");
        }

        protected abstract IEnumerable<GenerationRequest> GetGenerationRequest();

        private static async Task<GenerationResponse> DispatchAsync(Core_Client client, GenerationRequest rqst, SemaphoreSlim sema, SlidingWindowRateLimiter rpmLimiter)
        {
            await sema.WaitAsync();

            using var lease = await rpmLimiter.AcquireAsync(1);

            Console.WriteLine($"[BulkTask] #{rqst.TrackId} Generating response start...");

            var response = await client.GetGenerateResponseAsync(rqst);

            Console.WriteLine($"[BulkTask] #{rqst.TrackId} Generating response complete. Time taken: {response.TimeTaken.TotalSeconds:F4} seconds");

            sema.Release();

            return response;
        }

        public void WriteToCsv(string filePath)
        {
            using var writer = new StreamWriter(filePath, false, System.Text.Encoding.UTF8);
            using var csvWriter = new CsvWriter(writer, CultureInfo.CurrentCulture);

            Console.WriteLine($"[BulkTask] Start writing results to CSV file...");

            csvWriter.WriteField("TrackID");
            csvWriter.WriteField("TimeTaken");
            csvWriter.WriteField("TokenInput");
            csvWriter.WriteField("TokenOutput");
            csvWriter.WriteField("TokenTotal");
            csvWriter.WriteField("Response");
            csvWriter.NextRecord();

            foreach (var res in tasks.Select(t => t.Result))
            {
                var csvLines = new
                {
                    TrackID = res.TrackId,
                    TimeTaken = res.TimeTaken,
                    TokenInput = res.TokenCount?.Input.ToString()   ?? "N/A",
                    TokenOutput = res.TokenCount?.Output.ToString() ?? "N/A",
                    TokenTaken = res.TokenCount?.Total == 0 ? "Uncountable." : res.TokenCount?.Total.ToString(),
                    Response = res.State               == GenerationResponse.EState.Completed ? res.Response : res.Error ?? string.Empty,
                };

                csvWriter.WriteRecord(csvLines);
                csvWriter.NextRecord();
            }

            Console.WriteLine($"[BulkTask] Writing results to CSV file '{filePath}' completed.");
        }

        public enum EState
        {
            NotStarted,
            Running,
            Completed,
            CompletedWithError,
            Error,
        }
    }
}