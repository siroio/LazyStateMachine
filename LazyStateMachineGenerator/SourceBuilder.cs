using System.Text;

namespace LazyStateMachineGenerator
{
    internal class SourceBuilder
    {
        private readonly StringBuilder sb = new();
        private int indent = 0;

        public void RemoveLast()
        {
            if (sb.Length > 0)
                sb.Remove(sb.Length - 1, 1);
        }

        public void AddIndent(int indentLevel = 1)
        {
            indent = Math.Max(0, indent + indentLevel);
        }

        public void AddCode(string code)
        {
            sb.Append(code);
        }

        public void AppendFormat(string format, params object[] args)
        {
            sb.AppendFormat(format, args);
        }

        public void InsertLine()
        {
            sb.Append('\n').Append(GetIndentString());
        }

        public void InsertLine(string code)
        {
            InsertLine();
            sb.Append(code);
        }

        public void InsertLines(IEnumerable<string> lines)
        {
            foreach (var line in lines)
                InsertLine(line);
        }

        public void InsertComment(string comment)
        {
            InsertLine($"// {comment}");
        }

        public void UsingDirective(string namespaceName)
        {
            InsertLine($"using {namespaceName};");
        }

        public void BeginBlock()
        {
            InsertLine("{");
            AddIndent();
        }

        public void EndBlock()
        {
            AddIndent(-1);
            InsertLine("}");
        }

        public void Property(string type, string name, string accessModifier = "public", bool autoProperty = true)
        {
            if (autoProperty)
                InsertLine($"{accessModifier} {type} {name} {{ get; set; }}");
            else
                InsertLine($"{accessModifier} {type} {name};");
        }

        public override string ToString() => sb.ToString();

        public void Clear()
        {
            sb.Clear();
            indent = 0;
        }

        private string GetIndentString() => new string(' ', indent * 4);

        public BlockScope CreateBlockScope() => new(this);
        public BlockScope CreateBlockScope(string code) => new(this, code);
        public NamespaceScope CreateNamespaceScope(string name) => new(this, name);
        public ClassScope CreateClassScope(string name, string modifiers = "public") => new(this, name, modifiers);
        public MethodScope CreateMethodScope(string name, string returnType = "void", string modifiers = "public") => new(this, name, returnType, modifiers);
    }

    internal class BlockScope : IDisposable
    {
        private readonly SourceBuilder cb;

        public BlockScope(SourceBuilder codeBuilder, string code)
        {
            cb = codeBuilder;
            cb.InsertLine(code);
            cb.BeginBlock();
        }

        public BlockScope(SourceBuilder codeBuilder)
        {
            cb = codeBuilder;
            cb.BeginBlock();
        }

        public void Dispose() => cb.EndBlock();
    }

    internal class NamespaceScope : IDisposable
    {
        private readonly SourceBuilder cb;

        public NamespaceScope(SourceBuilder codeBuilder, string name)
        {
            cb = codeBuilder;
            cb.InsertLine($"namespace {name}");
            cb.BeginBlock();
        }

        public void Dispose() => cb.EndBlock();
    }

    internal class ClassScope : IDisposable
    {
        private readonly SourceBuilder cb;

        public ClassScope(SourceBuilder codeBuilder, string name, string modifiers = "public")
        {
            cb = codeBuilder;
            cb.InsertLine($"{modifiers} class {name}");
            cb.BeginBlock();
        }

        public void Dispose() => cb.EndBlock();
    }

    internal class MethodScope : IDisposable
    {
        private readonly SourceBuilder cb;

        public MethodScope(SourceBuilder codeBuilder, string name, string returnType = "void", string modifiers = "public")
        {
            cb = codeBuilder;
            cb.InsertLine($"{modifiers} {returnType} {name}()");
            cb.BeginBlock();
        }

        public void Dispose() => cb.EndBlock();
    }
}
