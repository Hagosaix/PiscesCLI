using Google.GenAI.Types;

namespace EverbloomingLab.PiscesCLI.Core
{
    public class GenerationRequest
    {
        private readonly string userPrompt;
        private readonly byte[] inlineData;
        private readonly string inlineDataFileName;

        public readonly int TrackId;
        public readonly Content Content;

        public readonly bool CreateRequestSuccessful;

        public GenerationRequest(string userPrompt, int trackId = 0)
        {
            this.userPrompt = userPrompt;
            TrackId = trackId;

            Content = new Content
            {
                Role = "user",
                Parts = [Part.FromText(userPrompt)],
            };

            CreateRequestSuccessful = true;
        }

        public GenerationRequest(byte[] inlineData, string inlineDataFileName, int trackId = 0)
        {
            this.inlineData = inlineData;
            this.inlineDataFileName = inlineDataFileName;
            TrackId = trackId;

            if (ClientContext.TryGetMimeType(inlineDataFileName, out var mimeType))
            {
                Content = new Content
                {
                    Role = "user",
                    Parts = [Part.FromBytes(inlineData, mimeType)],
                };
                CreateRequestSuccessful = true;
            }
            else
            {
                Console.WriteLine($"[Client] Failed to create request for inline data '{inlineDataFileName}'. Unsupported file type.");
                CreateRequestSuccessful = false;
            }
        }
    }
}