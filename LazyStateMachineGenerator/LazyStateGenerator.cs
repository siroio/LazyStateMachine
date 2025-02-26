using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace LazyStateMachineGenerator
{
    public class StateSyntaxReceiver : ISyntaxReceiver
    {
        public List<ClassDeclarationSyntax> CandidateClasses { get; } = new();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            // クラス宣言のみを対象とする
            if (syntaxNode is ClassDeclarationSyntax classDecl)
            {
                // LazyStateBase を継承しているクラスを収集
                if (classDecl.BaseList?.Types.Any(bt => bt.Type.ToString() == "LazyStateBase") == true)
                {
                    CandidateClasses.Add(classDecl);
                }
            }
        }
    }

    [Generator]
    internal class LazyStateGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new StateSyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (context.SyntaxReceiver is not StateSyntaxReceiver receiver)
                return;

            var compilation = context.Compilation;

            foreach (var stateClass in receiver.CandidateClasses)
            {
                var model = compilation.GetSemanticModel(stateClass.SyntaxTree);
                if (model.GetDeclaredSymbol(stateClass) is not INamedTypeSymbol symbol) continue;

                var interfaces = new List<string>();

                if (symbol.GetMembers().OfType<IMethodSymbol>().Any(m => m.Name == "OnEnter"))
                {
                    interfaces.Add("IOnEnter");
                }

                if (symbol.GetMembers().OfType<IMethodSymbol>().Any(m => m.Name == "OnUpdate"))
                {
                    interfaces.Add("IOnUpdate");
                }

                if (symbol.GetMembers().OfType<IMethodSymbol>().Any(m => m.Name == "OnFixedUpdate"))
                {
                    interfaces.Add("IOnFixedUpdate");
                }

                if (symbol.GetMembers().OfType<IMethodSymbol>().Any(m => m.Name == "OnLateUpdate"))
                {
                    interfaces.Add("IOnLateUpdate");
                }

                if (symbol.GetMembers().OfType<IMethodSymbol>().Any(m => m.Name == "OnExit"))
                {
                    interfaces.Add("IOnExit");
                }

                if (interfaces.Count > 0)
                {
                    var source = GeneratePartialClass(symbol, interfaces);
                    context.AddSource($"{symbol.Name}.g.cs", SourceText.From(source, Encoding.UTF8));
                }
            }
        }

        private string GeneratePartialClass(INamedTypeSymbol symbol, List<string> interfaces)
        {
            var namespaceName = symbol.ContainingNamespace.ToString();
            var className = symbol.Name;
            var interfaceList = string.Join(", ", interfaces);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("// Auto-Generated");
            sb.AppendLine("using LazyStateMachine;");
            sb.AppendLine("");
            sb.AppendLine($"namespace {namespaceName}");
            sb.AppendLine("{");
            sb.AppendLine($"    public partial class {className} : {interfaceList}");
            sb.AppendLine("    {  }");
            sb.AppendLine("}");

            return sb.ToString();
        }
    }
}
