namespace EverbloomingLab.PiscesCLI.Core
{
    public readonly struct TokenCount()
    {
        public readonly int Input;
        public readonly int Output;
        public readonly int Total;

        public TokenCount(int input, int output, int total) : this()
        {
            Input = input;
            Output = output;
            Total = total;
        }
    }

    public class GenerationResponse
    {
        public EState State { get; internal set; } = EState.NotStarted;
        public string? Error { get; internal set; }
        public string? Response { get; internal set; }

        public TokenCount? TokenCount { get; internal set; }
        public readonly GenerationRequest Request;

        public int TrackId => Request.TrackId;

        public TimeSpan TimeTaken { get; private set; } = TimeSpan.Zero;

        private readonly DateTime timeTakenStart = DateTime.Now;

        public GenerationResponse(GenerationRequest request) => Request = request;

        public void SetWaitingResponse()
        {
            State = EState.WaitingResponse;
        }

        public void WriteResponse(string response)
        {
            State = EState.Completed;
            Response = response;

            TimeTaken = DateTime.Now - timeTakenStart;
        }

        public void WriteTokenCount(TokenCount tokenCount)
        {
            TokenCount = tokenCount;
        }

        public void WriteError(string error)
        {
            State = EState.Error;
            Error = error;

            TimeTaken = DateTime.Now - timeTakenStart;
        }

        public enum EState
        {
            NotStarted,
            WaitingResponse,
            Completed,
            Error,
        }
    }
}