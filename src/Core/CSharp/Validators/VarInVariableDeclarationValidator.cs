using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using uLearn.Extensions;

namespace uLearn.CSharp.Validators
{
    public class VarInVariableDeclarationValidator : BaseStyleValidator
    {
        public override List<SolutionStyleError> FindErrors(SyntaxTree userSolution, SemanticModel semanticModel)
        {
            return InspectAll<VariableDeclarationSyntax>(userSolution, semanticModel, Inspect).ToList();
        }

        private IEnumerable<SolutionStyleError> Inspect(VariableDeclarationSyntax variableDeclarationSyntax, SemanticModel semanticModel)
        {
            if (!NeedToCheckDeclaration(variableDeclarationSyntax))
                yield break;

            foreach (var variable in variableDeclarationSyntax.Variables)
            {
                if (variable.Initializer == null)
                    yield break;

                var value = variable.Initializer.Value;
                if (!(value is LiteralExpressionSyntax) && !(value is ObjectCreationExpressionSyntax))
                    continue;

                var initializerTypeInfo = semanticModel.GetTypeInfo(variable.Initializer.Value);
                var variableTypeInfo = semanticModel.GetTypeInfo(variableDeclarationSyntax.Type);

                if (variableTypeInfo.IsPrimitive() || variableTypeInfo.Type.SpecialType == SpecialType.System_String)
                    yield break;

                if (Equals(initializerTypeInfo.Type, variableTypeInfo.Type))
                    yield return new SolutionStyleError(variable, "Используйте `var` при инициализации локальной переменной.");
            }
        }

        private bool NeedToCheckDeclaration(VariableDeclarationSyntax variableDeclarationSyntax)
        {
            if (variableDeclarationSyntax.Type.IsVar)
                return false;

            var parent = variableDeclarationSyntax.Parent;
            if (parent is LocalDeclarationStatementSyntax localDeclarationStatment)
                return !localDeclarationStatment.IsConst;

            return variableDeclarationSyntax.Parent is ForStatementSyntax;
        }
    }
}