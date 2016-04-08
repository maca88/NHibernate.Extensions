using NHibernate.Linq;
using NHibernate.Linq.Visitors;
using NHibernate.Linq.Visitors.ResultOperatorProcessors;
using NHibernate.Param;

namespace NHibernate.Extensions.Lock
{
    public class ProcessLock : IResultOperatorProcessor<LockResultOperator>
    {
        public void Process(LockResultOperator resultOperator, QueryModelVisitor queryModelVisitor, IntermediateHqlTree tree)
        {
            var parameters = queryModelVisitor.VisitorParameters;

            switch (resultOperator.ParseInfo.ParsedExpression.Method.Name)
            {
                case "Lock":
                    NamedParameter parameterName;
                    queryModelVisitor.VisitorParameters.ConstantToParameterMap.TryGetValue(resultOperator.Data,
                                                                                           out parameterName);
                    if (parameterName != null)
                    {
                        tree.AddAdditionalCriteria((q, p) => q.SetLockMode(null, (LockMode)p[parameterName.Name].Item1));
                    }
                    else
                    {
                        tree.AddAdditionalCriteria((q, p) => q.SetLockMode(null, (LockMode)resultOperator.Data.Value));
                    }
                    break;
            }
        }
    }
}
