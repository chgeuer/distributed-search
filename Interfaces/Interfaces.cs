namespace Interfaces
{
    using System.Collections.Generic;
    using static BusinessLogic.Logic;

    public interface IBusinessLogicStep<TContext, TItem>
    {
    }

    public class PipelineSteps<TContext, TItem>
    {
        public IEnumerable<IBusinessLogicStep<TContext, TItem>> StreamingSteps { get; set; }

        public IEnumerable<IBusinessLogicStep<TContext, TItem>> FinalSteps { get; set; }
    }

    public interface IBusinessLogicFilterProjection<TContext, TItem> : IBusinessLogicStep<TContext, TItem>
    {
        TItem Map(TContext context, TItem item);
    }

    public interface IBusinessLogicFilterPredicate<TContext, TItem> : IBusinessLogicStep<TContext, TItem>
    {
        bool Matches(TContext context, TItem item);
    }

    public interface IBusinessLogicFilterStatefulPredicate<TContext, TItem> : IBusinessLogicStep<TContext, TItem>
    {
        ReplaceableOption<TItem> BetterMatch(TContext context, TItem item);
    }

    public interface IBusinessLogicOrderStep<TContext, TItem> : IBusinessLogicStep<TContext, TItem>
    {
        IEnumerable<TItem> Order(TContext context, IEnumerable<TItem> items);
    }
}