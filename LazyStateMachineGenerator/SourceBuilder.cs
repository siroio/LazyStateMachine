using System.Runtime.CompilerServices;
using System.Text;

namespace LazyStateMachineGenerator
{
    internal class SourceBuilder
    {
        private readonly StringBuilder sb = new();
        private int indent;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AppendIndent() => sb.Append(' ', indent * 4);

        public void RemoveLast()
        {
            if (sb.Length > 0) sb.Remove(sb.Length - 1, 1);
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

        public void InsertLine(string? code = null)
        {
            sb.Append('\n');
            AppendIndent();
            if (!string.IsNullOrEmpty(code)) sb.Append(code);
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

    internal readonly ref struct ScopeBase
    {
        private readonly SourceBuilder cb;

        public ScopeBase(SourceBuilder cb, string? line)
        {
            this.cb = cb;
            if (!string.IsNullOrEmpty(line)) cb.InsertLine(line);
            cb.BeginBlock();
        }

        public void Dispose() => cb.EndBlock();
    }

    internal readonly ref struct BlockScope
    {
        private readonly ScopeBase scope;
        public BlockScope(SourceBuilder cb) => scope = new ScopeBase(cb, null);
        public BlockScope(SourceBuilder cb, string code) => scope = new ScopeBase(cb, code);
        public void Dispose() => scope.Dispose();
    }

    internal readonly ref struct NamespaceScope
    {
        private readonly ScopeBase scope;

        public NamespaceScope(SourceBuilder cb, string ns) => scope = new ScopeBase(cb, $"namespace {ns}");

        public void Dispose() => scope.Dispose();
    }


    internal readonly ref struct ClassScope
    {
        private readonly ScopeBase scope;

        public ClassScope(SourceBuilder cb, string name, string modifiers = "public")
            => scope = new ScopeBase(cb, $"{modifiers} class {name}");

        public void Dispose() => scope.Dispose();
    }


    internal readonly ref struct MethodScope
    {
        private readonly ScopeBase scope;

        public MethodScope(SourceBuilder cb, string name, string returnType = "void", string modifiers = "public")
            => scope = new ScopeBase(cb, $"{modifiers} {returnType} {name}()");

        public void Dispose() => scope.Dispose();
    }
}
