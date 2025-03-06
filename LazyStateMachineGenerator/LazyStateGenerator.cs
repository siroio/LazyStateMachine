using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.DotnetRuntime.Extensions;

namespace LazyStateMachineGenerator
{
    [Generator]
    internal class LazyStateGenerator : IIncrementalGenerator
    {
        private static readonly (string MethodName, string InterfaceName)[] MethodMappings =
        {
            ("OnEnter", "IOnEnter"),
            ("OnUpdate", "IOnUpdate"),
            ("OnFixedUpdate", "IOnFixedUpdate"),
            ("OnLateUpdate", "IOnLateUpdate"),
            ("OnExit", "IOnExit")
        };

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterPostInitializationOutput(static context =>
            {
                context.AddSource("GenerateLazyStateAttribute.g.cs", """
using System;

namespace LazyStateMachine 
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    internal sealed class GenerateLazyStateAttribute : Attribute
    { }
}
""");
            });

            var source = context.SyntaxProvider.ForAttributeWithMetadataName(
                context,
                "LazyStateMachine.GenerateLazyStateAttribute",
                static (node, token) => true,
                static (context, token) => context)
                .Collect();

            context.RegisterSourceOutput(source, Emit);
        }

        private static void Emit(SourceProductionContext context, ImmutableArray<GeneratorAttributeSyntaxContext> array)
        {
            foreach (var item in array)
            {
                if (item.TargetSymbol is not INamedTypeSymbol classSymbol)
                    continue;

                var className = classSymbol.Name;
                var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
                var baseClazzTypeParameter = new HashSet<string>();
                var implementedInterfaces = new HashSet<string>();

                if (classSymbol.BaseType == null || !classSymbol.BaseType.Name.StartsWith("LazyStateBase"))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor(
                            "LSS0001", // カスタムエラーコード
                            "Invalid Base Class",
                            $"{className} must inherit from LazyStateBase<T> to use the GenerateStateAttribute.",
                            "Usage",
                            DiagnosticSeverity.Error,
                            true),
                        item.Attributes[0]?.ApplicationSyntaxReference?.GetSyntax().GetLocation()));
                    continue;
                }

                for (int i = 0; i < classSymbol.BaseType?.TypeArguments.Length; i++)
                {
                    ITypeSymbol? param = classSymbol.BaseType?.TypeArguments[i];
                    if (param == null) break;
                    baseClazzTypeParameter.Add(param.ToDisplayString());
                }

                // クラス内のメソッドを取得し、対応するインターフェースを特定
                foreach (var method in classSymbol.GetMembers().OfType<IMethodSymbol>())
                {
                    foreach (var (methodName, interfaceName) in MethodMappings)
                    {
                        if (method.Name == methodName)
                        {
                            implementedInterfaces.Add(interfaceName);
                        }
                    }
                }

                if (implementedInterfaces.Count == 0)
                    continue;

                var builder = new SourceBuilder();

                if (classSymbol.BaseType?.ContainingNamespace != null)
                {
                    builder.UsingDirective(classSymbol.BaseType.ContainingNamespace.ToDisplayString());
                    builder.InsertLine();
                }

                void CreateClassScope()
                {
                    using (builder.CreateClassScope($"{className} : LazyStateBase<{string.Join(", ", baseClazzTypeParameter)}>, {string.Join(", ", implementedInterfaces)}", "public partial"))
                    { }
                }

                builder.InsertLine(@"//===== AUTO GENERATE CLASS ======");
                if (!namespaceName.Contains("global namespace"))
                {
                    using (builder.CreateNamespaceScope(namespaceName))
                    {
                        CreateClassScope();
                    }
                }
                else
                {
                    CreateClassScope();
                }

                context.AddSource($"{className}.g.cs", builder.ToString());
            }
        }

    }
}
