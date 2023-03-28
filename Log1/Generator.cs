using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Log1
{
    [Generator]
    public class Generator : ISourceGenerator
    {
        private struct ClassArgs
        {
            public string ServiceName { get; set; }
            public string GenericArgs { get; set; }
            public string CtorParams { get; set; }
            public string BaseCtorArgs { get; set; }
            public string Methods { get; set; }
        }

        private const string classTemplate = @"using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Log1;
        
public class Log1_{{ServiceName}}Interceptor{{GenericArgs}} : {{ServiceName}}{{GenericArgs}}
{
    private readonly ILogger log1_logger;
    private readonly IConfigurationReader log1_configurationReader;

    public Log1_{{ServiceName}}Interceptor(ILogger log1_logger, IConfigurationReader log1_configurationReader{{CtorParams}})
        : base({{BaseCtorArgs}})
    {
        this.log1_logger = log1_logger;
        this.log1_configurationReader = log1_configurationReader;
    }

{{Methods}}
}";

        private struct MethodArgs
        {
            public string ServiceType { get; set; }
            public string MethodName { get; set; }
            public string GenericArgs { get; set; }
            public string MethodType { get; set; }
            public string LogLevel { get; set; }
            public string MethodParams { get; set; }
            public string BaseMethodArgs { get; set; }
            public string KVPMethodArgs { get; set; }
            public string ReturnAssignment => MethodType == "void" ? "" : "var returnValue = ";
            public string ReturnValue => MethodType == "void" ? "" : ", returnValue";
            public string ReturnStatement => MethodType == "void" ? "" : "return returnValue;";
        }

        private const string methodTemplate = @"    public override {{MethodType}} {{MethodName}}{{GenericArgs}}({{MethodParams}})
    {
        var parameters = new Dictionary<string, object>
        {
{{KVPMethodArgs}}
        };

        var config = log1_configurationReader.ReadConfiguration(""{{ServiceType}}.{{MethodName}}{{GenericArgs}}"");
        var logConditionsMet = config.Matches(parameters);

        if (logConditionsMet)
        {
            // todo: read log level from attribute when generating this call
            log1_logger.LogCall({{LogLevel}}, parameters);
        }

        {{ReturnAssignment}}base.{{MethodName}}{{GenericArgs}}({{BaseMethodArgs}});

        if (logConditionsMet)
        {
            log1_logger.LogReturn({{LogLevel}}{{ReturnValue}});
        }

        {{ReturnStatement}}
    }";

        public void Execute(GeneratorExecutionContext context)
        {
            var methods = GetLog1Methods(context.Compilation);
            foreach (var declaration in methods.GroupBy(g => g.ContainingType, SymbolEqualityComparer.Default))
            {
                var file = BuildClass(declaration.ToImmutableArray());
                context.AddSource($"Log1_{declaration.First().ContainingType.Name}Interceptor.g.cs", file);
            }
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            // No initialization required for this one
#if DEBUG
            if (!Debugger.IsAttached)
            {
                //Debugger.Launch();
            }
#endif
        }

        private static string BuildClass(ImmutableArray<IMethodSymbol> methods)
        {
            var serviceType = methods.First().ContainingType;
            var constructor = serviceType.InstanceConstructors.First();

            var classArgs = new ClassArgs
            {
                ServiceName = serviceType.Name,
                GenericArgs = serviceType.TypeArguments.Any()
                    ? '<' + string.Join(", ", serviceType.TypeArguments.Select(s => s.ToDisplayString())) + '>'
                    : string.Empty,
                CtorParams = constructor.Parameters.Any()
                    ? ", " + string.Join(", ", constructor.Parameters.Select(s => s.Type.ToDisplayString() + " " + s.Name))
                    : string.Empty,
                BaseCtorArgs = string.Join(", ", constructor.Parameters.Select(s => s.Name)),
                Methods = string.Join($"\n\n", methods.Select(BuildMethod))
            };

            return classTemplate
                .Replace("{{ServiceName}}", classArgs.ServiceName)
                .Replace("{{GenericArgs}}", classArgs.GenericArgs)
                .Replace("{{CtorParams}}", classArgs.CtorParams)
                .Replace("{{BaseCtorArgs}}", classArgs.BaseCtorArgs)
                .Replace("{{Methods}}", classArgs.Methods);
        }

        private static string BuildMethod(IMethodSymbol method)
        {
            var log1Attribute = method.GetAttributes().First(f => f.AttributeClass?.Name == "Log1Attribute");
            if (log1Attribute is null)
            {
                return string.Empty;
            }

            var logLevelValue = log1Attribute.NamedArguments.Any(f => f.Key == "LogLevel")
                ? log1Attribute.NamedArguments.First(f => f.Key == "LogLevel").Value.ToCSharpString()
                : null;

            var methodArgs = new MethodArgs
            {
                MethodName = method.Name,
                GenericArgs = method.TypeParameters.Any()
                    ? '<' + string.Join(", ", method.TypeParameters.Select(p => p.Name)) + '>'
                    : string.Empty,
                MethodType = method.ReturnsVoid ? "void" : method.ReturnType.ToDisplayString(),
                ServiceType = method.ContainingType.ToDisplayString(),
                LogLevel = logLevelValue ?? "Microsoft.Extensions.Logging.LogLevel.Information",
                MethodParams = string.Join(", ", method.Parameters.Select(s => s.Type.ToDisplayString() + " " + s.Name)),
                BaseMethodArgs = string.Join(", ", method.Parameters.Select(s => s.Name)),
                KVPMethodArgs = string.Join("", method.Parameters.Select(s => $"            [nameof({s.Name})] = {s.Name},\n"))
            };

            return methodTemplate
                .Replace("{{MethodName}}", methodArgs.MethodName)
                .Replace("{{GenericArgs}}", methodArgs.GenericArgs)
                .Replace("{{MethodType}}", methodArgs.MethodType)
                .Replace("{{ServiceType}}", methodArgs.ServiceType)
                .Replace("{{LogLevel}}", methodArgs.LogLevel)
                .Replace("{{MethodParams}}", methodArgs.MethodParams)
                .Replace("{{BaseMethodArgs}}", methodArgs.BaseMethodArgs)
                .Replace("{{KVPMethodArgs}}", methodArgs.KVPMethodArgs)
                .Replace("{{ReturnValue}}", methodArgs.ReturnValue)
                .Replace("{{ReturnAssignment}}", methodArgs.ReturnAssignment)
                .Replace("{{ReturnStatement}}", methodArgs.ReturnStatement);
        }

        private static ImmutableArray<IMethodSymbol> GetLog1Methods(Compilation compilation)
        {
            IEnumerable<SyntaxNode> allNodes = compilation.SyntaxTrees.SelectMany(s => s.GetRoot().DescendantNodes());
            IEnumerable<MethodDeclarationSyntax> allMethods = allNodes
                .Where(d => d.IsKind(SyntaxKind.MethodDeclaration))
                .OfType<MethodDeclarationSyntax>();

            return allMethods
                .Where(component => component.AttributeLists
                    .SelectMany(s => s.Attributes)
                    .Any(w => w.Name.ToString() == "Log1" || w.Name.ToString() == "Log1Attribute"))
                .Select(s => compilation.GetSemanticModel(s.SyntaxTree).GetDeclaredSymbol(s))
                .Where(w => !(w is null))
                .ToImmutableArray();
        }
    }
}