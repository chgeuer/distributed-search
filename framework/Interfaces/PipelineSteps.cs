namespace Mercury.Interfaces
{
    using System.Collections.Generic;

    public class PipelineSteps<TContext, TItem>
    {
        public IEnumerable<IBusinessLogicStep<TContext, TItem>> StreamingSteps { get; set; }

        public IEnumerable<IBusinessLogicStep<TContext, TItem>> FinalSteps { get; set; }
    }
}
