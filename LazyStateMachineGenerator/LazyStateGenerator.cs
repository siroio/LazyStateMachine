using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace LazyStateMachineGenerator
{
    public class StateSyntaxReceiver : ISyntaxReceiver
    {
        public HashSet<ClassDeclarationSyntax> CandidateClasses { get; } = new();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is ClassDeclarationSyntax classDecl && classDecl.BaseList != null)
            {
                foreach (var baseType in classDecl.BaseList.Types)
                {
                    if (baseType.Type is GenericNameSyntax genericName &&
                        genericName.Identifier.Text == "LazyStateBase")
                    {
                        CandidateClasses.Add(classDecl);
                        return; // 1つ見つかったら終了
                    }
                }
            }
        }
    }

    [Generator]
    internal class LazyStateGenerator : ISourceGenerator
    {
        private static readonly (string MethodName, string InterfaceName)[] MethodMappings =
        {
            ("OnEnter", "IOnEnter"),
            ("OnUpdate", "IOnUpdate"),
            ("OnFixedUpdate", "IOnFixedUpdate"),
            ("OnLateUpdate", "IOnLateUpdate"),
            ("OnExit", "IOnExit")
        };

        public void Initialize(GeneratorInitializationContext context) =>
            context.RegisterForSyntaxNotifications(() => new StateSyntaxReceiver());

        public void Execute(GeneratorExecutionContext context)
        {
            if (context.SyntaxReceiver is not StateSyntaxReceiver receiver)
                return;

            var compilation = context.Compilation;

            foreach (var stateClass in receiver.CandidateClasses)
            {
                var model = compilation.GetSemanticModel(stateClass.SyntaxTree);
                if (model.GetDeclaredSymbol(stateClass) is not INamedTypeSymbol symbol)
                    continue;

                var interfaces = DetectInterfaces(symbol);
                if (interfaces.Count > 0)
                {
                    var source = GeneratePartialClass(symbol, interfaces);
                    context.AddSource($"{symbol.Name}.g.cs", SourceText.From(source, Encoding.UTF8));
                }
            }
        }

        private HashSet<string> DetectInterfaces(INamedTypeSymbol symbol)
        {
            var interfaces = new HashSet<string>(StringComparer.Ordinal);

            foreach (var (MethodName, InterfaceName) in MethodMappings)
            {
                foreach (var member in symbol.GetMembers())
                {
                    if (member is IMethodSymbol method &&
                        method.MethodKind == MethodKind.Ordinary &&
                        method.Name == MethodName)
                    {
                        interfaces.Add(InterfaceName);
                        break; // 1つ見つかれば十分
                    }
                }
            }

            return interfaces;
        }

        private string GeneratePartialClass(INamedTypeSymbol symbol, HashSet<string> interfaces)
        {
            var namespaceName = symbol.ContainingNamespace.ToDisplayString();
            var baseClass = symbol.BaseType?.ToDisplayString() ?? "LazyStateBase<T>";

            var sb = new StringBuilder();
            sb.AppendLine("// Auto-Generated");
            sb.AppendLine("using LazyStateMachine;");
            sb.AppendLine();
            sb.Append("namespace ").Append(namespaceName).AppendLine();
            sb.AppendLine("{");
            sb.Append("    public partial class ").Append(symbol.Name)
              .Append(" : ").Append(baseClass);

            if (interfaces.Count > 0)
            {
                sb.Append(", ").Append(string.Join(", ", interfaces));
            }

            sb.AppendLine();
            sb.AppendLine("    { }");
            sb.AppendLine("}");
            return sb.ToString();
        }
    }
}
