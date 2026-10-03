using EverbloomingLab.PiscesCLI.Core;
using Google.GenAI.Types;
using GeminiFile = Google.GenAI.Types.File;

namespace EverbloomingLab.PiscesCLI.Batch
{
    public class File
    {
        public FileState? State { get; protected set; }
        public string Error { get; protected set; }

        public string JobLocalName { get; protected set; }

        public readonly string LocalFilePath;
        public readonly string LocalFileName;
        public readonly string LocalMimeType;

        public string ServerDisplayName { get; private set; }
        public string ServerFileName { get; private set; }
        public string Uri { get; private set; }
        public string MimeType { get; private set; }

        public File(string localFilePath)
        {
            LocalFilePath = localFilePath;
            LocalFileName = Path.GetFileName(localFilePath);
            LocalMimeType = ClientContext.GetMimeType(localFilePath);
        }

        public void SetJobLocalName(Job job) => JobLocalName = job.JobLocalName;

        public void WriteUploadedInfo(GeminiFile file)
        {
            ServerDisplayName = file.DisplayName;
            ServerFileName = file.Name;
            Uri = file.Uri;
            MimeType = file.MimeType;

            State = file.State;
        }

        public void SetState(GeminiFile file)
        {
            State = file.State;
        }

        public void SetState(FileState? state)
        {
            State = state;
        }

        public void SetError(string error)
        {
            State = FileState.Failed;
            Error = error;
        }
    }
}