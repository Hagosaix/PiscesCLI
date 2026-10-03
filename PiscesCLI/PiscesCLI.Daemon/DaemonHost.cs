using System.Net;
using System.Text.Json;
using System.Threading.RateLimiting;
using EverbloomingLab.PiscesCLI.Core;

namespace EverbloomingLab.PiscesCLI.Daemon
{
    public class DaemonHost
    {
        private readonly Client translator;
        private readonly Client explainer;

        private static readonly string _translator_cfg_path = Path.Combine(AppContext.BaseDirectory, "translator.toml");
        private static readonly string _explainer_cfg_path = Path.Combine(AppContext.BaseDirectory, "explainer.toml");

        public bool StartSucceeded;

        public DaemonHost()
        {
            ClientContext.Start(null);
            if (CoreBootstrapper.GetClientConfig(_translator_cfg_path, out var translatorCfg)
             && CoreBootstrapper.GetClientConfig(_explainer_cfg_path, out var explainerCfg))
            {
                translator = new Client(translatorCfg!);
                explainer = new Client(explainerCfg!);
                StartSucceeded = true;
            }
            else
            {
                Console.WriteLine($"{DateTime.Now:HH:mm:ss} [Daemon] Failed to load client configurations.");
                StartSucceeded = false;
            }
        }

        public async Task RunAsync(int port = 11435)
        {
            var listener = new HttpListener();
            listener.Prefixes.Add($"http://localhost:{port}/");
            listener.Start();

            using var semaphore = new SemaphoreSlim(translator.Config.ConcurrencyLimit, translator.Config.ConcurrencyLimit);
            await using var rpmLimiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
            {
                PermitLimit = translator.Config.RpmLimit,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 6,
                QueueLimit = int.MaxValue,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            });

            Console.WriteLine($"{DateTime.Now:HH:mm:ss} [Daemon] Listening on http://localhost:{port}/");

            while (true)
            {
                HttpListenerContext ctx;
                try { ctx = await listener.GetContextAsync(); }
                catch (HttpListenerException exception)
                {
                    Console.WriteLine($"{DateTime.Now:HH:mm:ss} [Daemon] HttpListenerException occurred: {exception.Message}. Stopping listener.");
                    break;
                }

                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var reader = new StreamReader(ctx.Request.InputStream, ctx.Request.ContentEncoding);
                        var requestBody = await reader.ReadToEndAsync();
                        var pathAndQuery = ctx.Request.Url?.PathAndQuery ?? "";

                        Console.WriteLine($"""
                                           {DateTime.Now:HH:mm:ss} [Daemon] Received {ctx.Request.HttpMethod}
                                                                                     {pathAndQuery}
                                                                            Body     {requestBody}         
                                           """);

                        var client = GetRespondClient(pathAndQuery);
                        if (client == null)
                        {
                            await WriteErrorResponse(ctx, "Invalid endpoint.", 404);
                            return;
                        }

                        await semaphore.WaitAsync();

                        try
                        {
                            using var lease = await rpmLimiter.AcquireAsync();

                            if (!lease.IsAcquired)
                            {
                                await WriteErrorResponse(ctx, "Rate limit exceeded.", 429);
                                return;
                            }

                            var body = await GetResponseTranslateBodyAsync(requestBody, client);

                            ctx.Response.StatusCode = (int)HttpStatusCode.OK;
                            ctx.Response.ContentType = "application/json; charset=utf-8"; // 必须是 JSON
                            ctx.Response.ContentLength64 = body.Length;
                            await ctx.Response.OutputStream.WriteAsync(body, 0, body.Length);
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"{DateTime.Now:HH:mm:ss} [Daemon] Error: {e.Message}");

                        await WriteErrorResponse(ctx, $"Internal Error: {e.Message}", 500);
                    }
                    finally
                    {
                        ctx.Response.Close();
                    }
                });
            }

            listener.Stop();
            Console.WriteLine($"{DateTime.Now:HH:mm:ss} [Daemon] Stopped.");
        }

        private async Task WriteErrorResponse(HttpListenerContext ctx, string message, int statusCode)
        {
            try
            {
                var errObj = new { text = message, src = "error" };
                var errBody = JsonSerializer.SerializeToUtf8Bytes(errObj);
                ctx.Response.StatusCode = statusCode;
                ctx.Response.ContentType = "application/json; charset=utf-8";
                ctx.Response.ContentLength64 = errBody.Length;
                await ctx.Response.OutputStream.WriteAsync(errBody, 0, errBody.Length);
            }
            catch
            {
                // Ignore any exceptions while writing the error response
            }
        }

        private Client? GetRespondClient(string cmd) => cmd switch
        {
            "/translate" => translator,
            "/explain"   => explainer,
            _            => null,
        };

        private async Task<byte[]> GetResponseTranslateBodyAsync(string requestBody, Client client)
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            var kissReq = JsonSerializer.Deserialize<KissRequest>(requestBody, options);
            var sourceText = kissReq?.text ?? string.Empty;

            if (string.IsNullOrWhiteSpace(sourceText)) return JsonSerializer.SerializeToUtf8Bytes(new KissResponse(string.Empty, string.Empty));

            Console.WriteLine($"{DateTime.Now:HH:mm:ss} [Daemon] Translating text: {sourceText}");
            var request = new GenerationRequest(sourceText);
            var response = await client.GetGenerateResponseAsync(request);
            var kissRes = new KissResponse(response.Response ?? response.Error ?? "PiscesCLI Error", "auto");

            Console.WriteLine($"""
                               {DateTime.Now:HH:mm:ss} [Daemon] Translation result: {kissRes.text}
                                                                Token in/out/total: {response.TokenCount?.Input}/{response.TokenCount?.Output}/{response.TokenCount?.Total}
                                                                Time taken: {response.TimeTaken} seconds
                               """);

            return JsonSerializer.SerializeToUtf8Bytes(kissRes);
        }
    }

    public record KissRequest(string? text, string from, string to);

    public record KissResponse(string text, string src);
}