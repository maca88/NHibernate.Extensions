using System;
using System.Linq.Expressions;
using Remotion.Linq.Clauses;
using Remotion.Linq.Parsing.Structure.IntermediateModel;

namespace NHibernate.Extensions.Lock
{
    public class LockExpressionNode : ResultOperatorExpressionNodeBase
    {
        private readonly MethodCallExpressionParseInfo _parseInfo;
        private readonly ConstantExpression _data;

        public LockExpressionNode(MethodCallExpressionParseInfo parseInfo, ConstantExpression data)
            : base(parseInfo, null, null)
        {
            _parseInfo = parseInfo;
            _data = data;
        }

        public override Expression Resolve(ParameterExpression inputParameter, Expression expressionToBeResolved, ClauseGenerationContext clauseGenerationContext)
        {
            throw new NotImplementedException();
        }

        protected override ResultOperatorBase CreateResultOperator(ClauseGenerationContext clauseGenerationContext)
        {
            return new LockResultOperator(_parseInfo, _data);
        }
    }
}
