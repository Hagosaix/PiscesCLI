using EverbloomingLab.PiscesCLI.Core;

namespace EverbloomingLab.PiscesCLI.Bulk
{
    public class InlineDataTask(byte[][] images, string[] imageNames) : BulkTask
    {
        protected override IEnumerable<GenerationRequest> GetGenerationRequest()
        {
            for (var i = 0; i < images.Length; i++)
            {
                var rqst = new GenerationRequest(images[i], imageNames[i], i);
                if (rqst.CreateRequestSuccessful) yield return rqst;
            }
        }
    }
}