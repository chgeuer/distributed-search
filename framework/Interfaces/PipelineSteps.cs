namespace Mercury.Interfaces
{
    using System.Collections.Generic;

    public class PipelineSteps<TBusinessData, TSearchRequest, TItem>
    {
        public IEnumerable<IBusinessLogicStep<TBusinessData, TSearchRequest, TItem>> StreamingSteps { get; set; }

        public IEnumerable<IBusinessLogicStep<TBusinessData, TSearchRequest, TItem>> FinalSteps { get; set; }
    }
}
