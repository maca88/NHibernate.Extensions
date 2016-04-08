using System;
using System.Linq.Expressions;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.StreamedData;
using Remotion.Linq.Parsing.Structure.IntermediateModel;

namespace NHibernate.Extensions.Lock
{
    public class LockResultOperator : ResultOperatorBase
    {
        public MethodCallExpressionParseInfo ParseInfo { get; private set; }
        public ConstantExpression Data { get; private set; }

        public LockResultOperator(MethodCallExpressionParseInfo parseInfo, ConstantExpression data)
        {
            ParseInfo = parseInfo;
            Data = data;
        }

        public override IStreamedData ExecuteInMemory(IStreamedData input)
        {
            throw new NotImplementedException();
        }

        public override IStreamedDataInfo GetOutputDataInfo(IStreamedDataInfo inputInfo)
        {
            return inputInfo;
        }

        public override ResultOperatorBase Clone(CloneContext cloneContext)
        {
            throw new NotImplementedException();
        }

        public override void TransformExpressions(Func<Expression, Expression> transformation)
        {
        }
    }
}
