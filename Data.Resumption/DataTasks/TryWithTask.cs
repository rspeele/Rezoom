using System;
using System.Threading.Tasks;

namespace Data.Resumption.DataTasks
{
    internal class TryWithTask<T> : IDataTask<T>
    {
        private readonly IDataTask<T> _bound;
        private readonly Func<Exception, IDataTask<T>> _exceptionHandler;

        public TryWithTask(IDataTask<T> bound, Func<Exception, IDataTask<T>> exceptionHandler)
        {
            _bound = bound;
            _exceptionHandler = exceptionHandler;
        }

        public StepState<T> Step()
        {
            StepState<T> wrapStep;
            try
            {
                wrapStep = _bound.Step();
            }
            catch (Exception stepEx)
            {
                return _exceptionHandler(stepEx).Step();
            }
            wrapStep.Visit
                ( pending =>
                {
                    
                }
                , result => StepState.Result(result));
        }
    }
}
