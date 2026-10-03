using Google.GenAI.Types;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.RateLimiting;
using EverbloomingLab.PiscesCLI.Core;
using Client = EverbloomingLab.PiscesCLI.Core.Client;
using Core_Client = EverbloomingLab.PiscesCLI.Core.Client;
using SysFile = System.IO.File;

namespace EverbloomingLab.PiscesCLI.Batch
{
    public static class BatchRunner
    {
        #region Public

        public static async Task SubmitBatchJobTextAsync(Core_Client client, string promptsFilePath, string? jobLocalName = null)
        {
            if (string.IsNullOrEmpty(jobLocalName)) jobLocalName = $"batch_{DateTime.Now:yyMMdd_HHmmss}";

            // read prompts from file
            var prompts = await SysFile.ReadAllLinesAsync(promptsFilePath);

            Console.WriteLine($"[Batch] Read {prompts.Length} prompts from file: {promptsFilePath}");

            // create job
            var job = new Job(jobLocalName);

            // create jsonl file
            var concLimiter = CreateSemaphoreAndRateLimiter(client);
            var jsonl = await CreateJsonlAsync(client, jobLocalName, jl =>
            {
                jl.CreateInlineTextPrompts(client, prompts);
            }, concLimiter.sema, concLimiter.rpm);

            job.SetJsonl(jsonl);

            await SubmitJobAsync(job, client);
        }

        public static async Task SubmitBatchJobFileAsync(Core_Client client, string pathDir, string? jobLocalName = null)
        {
            if (string.IsNullOrEmpty(jobLocalName)) jobLocalName = $"batch_{DateTime.Now:yyMMdd_HHmmss}";

            // find files from directory
            var files = Directory.GetFiles(pathDir).Select(f => new File(f)).ToArray();

            Console.WriteLine($"[Batch] Read {files.Length} files from directory: {pathDir}");

            var job = new Job(jobLocalName, files);

            try
            {
                WriteJobFile(job);

                var concLimiter = CreateSemaphoreAndRateLimiter(client);

                var upFileSuccessful = await UploadPromptFilesAsync(client, files, concLimiter.sema, concLimiter.rpm);

                if (!upFileSuccessful)
                {
                    WriteJobFile(job);
                    Console.WriteLine($"[Batch] Failed to upload prompt files. Please try again later with job file: {AppPath.GetBatchJobObjectJsonFilePath(job.JobLocalName)}");
                    return;
                }

                var jsonl = await CreateJsonlAsync(client, jobLocalName, jl =>
                {
                    jl.CreateFilePrompts(client, files);
                }, concLimiter.sema, concLimiter.rpm);

                job.SetJsonl(jsonl);

                await SubmitJobAsync(job, client);
            }
            catch (Exception e)
            {
                Console.WriteLine($"[Batch] Error occurred: {e.Message}");
            }
            finally
            {
                WriteJobFile(job);
            }
        }

        public static async Task ContinueFileBatchJobAsync(Core_Client client, string jobFilePath)
        {
            var job = JsonSerializer.Deserialize<Job>(await SysFile.ReadAllTextAsync(jobFilePath));

            if (job is null)
            {
                Console.WriteLine($"[Batch] Failed to deserialize job file: {jobFilePath}");
                return;
            }

            if (job.Files is null || job.Files.Length == 0)
            {
                Console.WriteLine($"[Batch] No files found in job file: {jobFilePath}");
                return;
            }

            var concLimiter = CreateSemaphoreAndRateLimiter(client);

            await CheckAndReuploadFileAsync(client, job, concLimiter.sema, concLimiter.rpm);


            if (await CheckUploadedFileActive(client, job.Files, concLimiter.sema, concLimiter.rpm))
            {
                var jsonl = await CreateJsonlAsync(client, job.JobLocalName, jl =>
                {
                    jl.CreateFilePrompts(client, job.Files);
                }, concLimiter.sema, concLimiter.rpm);

                job.SetJsonl(jsonl);
                await SubmitJobAsync(job, client);
            }
            else
            {
                Console.WriteLine($"[Batch] Failed to upload files. Please try again later with job file: {AppPath.GetBatchJobObjectJsonFilePath(job.JobLocalName)}");
                WriteJobFile(job);
            }
        }

        #endregion


        private static async Task SubmitJobAsync(Job job, Core_Client client)
        {
            if (job.Jsonl!.State == FileState.Active)
            {
                var geminiJob = await client.CreateJobAsync(job.Jsonl.ServerFileName, job.JobLocalName);
                job.SetGeminiBatchJob(geminiJob);
                Console.WriteLine($"[Batch] Job created: {job.ServerDisplayName} ({job.ServerName})");
            }
            else
            {
                Console.WriteLine($"[Batch] Job create failed, please retry later.");
            }

            WriteJobFile(job);
        }

        #region UploadFiles

        private static async Task CheckAndReuploadFileAsync(Core_Client client, Job job, SemaphoreSlim sema, SlidingWindowRateLimiter rpmLimiter)
        {
            Console.WriteLine($"[Batch] Starting file check and re-upload for job: {job.JobLocalName}");
            var tasks = job.Files!.Select(async f =>
            {
                Google.GenAI.Types.File? gFile = null;
                await sema.WaitAsync();

                try
                {
                    using var _ = await rpmLimiter.AcquireAsync();
                    gFile = await client.GetFileAsync(f.ServerFileName);
                    if (gFile is not null) f.WriteUploadedInfo(gFile);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"[Batch] Error checking file state for {f.ServerFileName}: {e.Message}");
                }
                finally
                {
                    sema.Release();
                }

                if (gFile is null || gFile.State == FileState.Failed || gFile.State == FileState.StateUnspecified)
                {
                    Console.WriteLine($"[Batch] File upload failed or file state is unspecified: {f.ServerFileName}. Re-uploading...");
                    await DispatchUploadFileAsync(client, f, sema, rpmLimiter);
                }
            });

            await Task.WhenAll(tasks);
        }

        private static async Task DispatchUploadFileAsync(Core_Client client, File file, SemaphoreSlim sema, SlidingWindowRateLimiter rpmLimiter)
        {
            await sema.WaitAsync();

            try
            {
                using var _ = await rpmLimiter.AcquireAsync();
                var uFile = await client.UploadFileAsync(file.LocalFilePath);
                if (uFile is null)
                {
                    file.SetError($"Failed to upload file. Server returned null.");
                    return;
                }

                file.WriteUploadedInfo(uFile);
                Console.WriteLine($"[Batch] File uploaded: {file.LocalFilePath}, current state: {uFile.State}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"[Batch] Error dispatching upload for file: {file.LocalFilePath}, Exception: {e.Message}");
                file.SetError($"Error dispatching upload: {e.Message}");
            }
            finally
            {
                sema.Release();
            }
        }

        private static async Task<bool> UploadPromptFilesAsync(Core_Client client, File[] files, SemaphoreSlim sema, SlidingWindowRateLimiter rpmLimiter)
        {
            var tasks = new List<Task>();

            Console.WriteLine($"[Batch] Uploading {files.Length} files...");

            foreach (var file in files)
            {
                tasks.Add(DispatchUploadFileAsync(client, file, sema, rpmLimiter));
            }

            await Task.WhenAll(tasks);

            return await CheckUploadedFileActive(client, files, sema, rpmLimiter);
        }

        private static async Task<bool> CheckUploadedFileActive(Core_Client client, File[] files, SemaphoreSlim sema, SlidingWindowRateLimiter rpmLimiter)
        {
            var fileNotActive = files.Where(f => f.State != FileState.Active).ToArray();
            if (!fileNotActive.Any()) return true; // all files are active
            Console.WriteLine("[Batch] Some files are not yet active.");
            var wTasks = fileNotActive.Select(async f => await WaitFileActiveAsync(client, f, sema, rpmLimiter)).ToArray();
            await Task.WhenAll(wTasks);
            var wTaskFailed = wTasks.Where(t => !t.Result);
            if (!wTaskFailed.Any()) return true; // all files are active

            Console.WriteLine($"[Batch] Some files failed to become active.");
            return false; // some files failed to become active
        }

        private static async Task<bool> WaitFileActiveAsync(Core_Client client, File file, SemaphoreSlim sema, SlidingWindowRateLimiter rpmLimiter)
        {
            if (file.State == FileState.Failed)
            {
                Console.WriteLine($"[Batch] File has failed: {file}");
                return false;
            }

            Console.WriteLine($"[Batch] Waiting for file to become active: {file}");

            var timeout = TimeSpan.FromMinutes(1);
            var stopwatch = Stopwatch.StartNew();

            while (stopwatch.Elapsed < timeout)
            {
                await sema.WaitAsync();

                try
                {
                    using var _ = await rpmLimiter.AcquireAsync();
                    var upFile = await client.GetFileAsync(file.ServerFileName);

                    if (upFile is not null)
                    {
                        file.WriteUploadedInfo(upFile);
                        if (upFile.State == FileState.Failed) return false;
                        if (upFile.State == FileState.Active)
                        {
                            Console.WriteLine($"[Batch] File is now active: {file}");
                            return true;
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"[Batch] Error waiting for file to become active: {file.LocalFilePath}, Exception: {e.Message}");
                }
                finally
                {
                    sema.Release();
                }

                await Task.Delay(10000);
            }

            Console.WriteLine($"[Batch] Timeout waiting for file to become active: {file.LocalFilePath}");
            return false; // timeout
        }

        #endregion

        private static async Task<Jsonl> CreateJsonlAsync(Core_Client client, string jobLocalName, Action<Jsonl> action, SemaphoreSlim sema, SlidingWindowRateLimiter rpmLimiter)
        {
            // create jsonl file
            var jsonl = new Jsonl(jobLocalName, AppPath.GetBatchJobUploadJsonlPath(jobLocalName));

            action.Invoke(jsonl);

            // create job
            var file = await client.UploadFileAsync(jsonl.LocalFilePath);

            if (file is null)
            {
                Console.WriteLine("[Batch] Failed to upload Jsonl file.");
                jsonl.SetError("Failed to upload Jsonl file.");
                return jsonl;
            }

            // wait file active
            jsonl.WriteUploadedInfo(file);

            // check file state
            if (jsonl.State == FileState.Active) return jsonl;

            if (jsonl.State == FileState.Processing)
            {
                Console.WriteLine($"[Batch] JSONL file is not yet active: {jsonl.ServerFileName}");
                var waitSuccess = await WaitFileActiveAsync(client, jsonl, sema, rpmLimiter);
                if (waitSuccess) return jsonl;                                                                        // file is now active
                Console.WriteLine($"[Batch] Failed to wait for JSONL file to become active: {jsonl.ServerFileName}"); // file is still not active
            }

            jsonl.SetError($"JSONL file is not active. Maybe the server is busy or there was an error during processing.");

            return jsonl;
        }

        private static (SemaphoreSlim sema, SlidingWindowRateLimiter rpm) CreateSemaphoreAndRateLimiter(Core_Client client)
        {
            var semaphore = new SemaphoreSlim(client.Config.ConcurrencyLimit, client.Config.ConcurrencyLimit);
            var rpmLimiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
            {
                PermitLimit = client.Config.RpmLimit,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 6,
                QueueLimit = int.MaxValue,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            });
            return (semaphore, rpmLimiter);
        }

        private static void WriteJobFile(Job job)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            SysFile.WriteAllText(AppPath.GetBatchJobObjectJsonFilePath(job.JobLocalName), JsonSerializer.Serialize(job, options));
            Console.WriteLine($"[Batch] Job file written: {AppPath.GetBatchJobObjectJsonFilePath(job.JobLocalName)}");
        }
    }
}