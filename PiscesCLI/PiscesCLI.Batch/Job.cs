using Google.GenAI.Types;
using System.Text.Json.Serialization;

namespace EverbloomingLab.PiscesCLI.Batch
{
    public class Job
    {
        public string JobLocalName { get; init; }

        [JsonInclude] public string? ServerDisplayName { get; private set; }

        [JsonInclude] public string? ServerName { get; private set; }

        [JsonInclude] public JobState? State { get; private set; }

        public File[]? Files { get; init; }

        [JsonIgnore] public Jsonl? Jsonl { get; set; }

        [JsonConstructor]
        public Job(string jobLocalName, File[]? files = null)
        {
            JobLocalName = jobLocalName;
            Files = files;
            if (Files is null) return;

            foreach (var file in Files)
            {
                file.SetJobLocalName(this);
            }
        }

        public void SetJsonl(Jsonl jsonl)
        {
            Jsonl = jsonl;
        }

        public void SetGeminiBatchJob(BatchJob job)
        {
            ServerDisplayName = job.DisplayName;
            ServerName = job.Name;
            State = job.State;
        }
    }
}