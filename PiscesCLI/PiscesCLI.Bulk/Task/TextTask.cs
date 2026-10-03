using EverbloomingLab.PiscesCLI.Core;

namespace EverbloomingLab.PiscesCLI.Bulk
{
    public class TextTask(string[] prompts) : BulkTask
    {
        protected override IEnumerable<GenerationRequest> GetGenerationRequest()
        {
            for (var i = 0; i < prompts.Length; i++)
            {
                var rqst = new GenerationRequest(prompts[i], i);
                if (rqst.CreateRequestSuccessful) yield return rqst;
            }
        }
    }
}