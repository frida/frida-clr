using System.Text;
using GirModel;

internal static class WrapperEmitter
{
    public static string Emit(Class cls, ISet<Class>? usedOptions = null)
    {
        usedOptions ??= new HashSet<Class>();
        var custom = CustomMembers.For(cls.Name);
        var lowLevel = cls.Name;
        var field = Field(cls.Name);
        var signals = SupportedSignals(cls).Where(s => !custom.Signals.Contains(s.Name)).ToList();
        var asyncOps = AsyncOperations(cls).ToList();

        var writer = new SourceWriter();
        writer.Line("#nullable enable");
        writer.Blank();
        writer.Line("using Frida.Events;");
        writer.Line("using Frida.Helpers;");
        writer.Blank();
        writer.Line("namespace Frida;");
        writer.Blank();
        writer.Line($"public partial class Frida{cls.Name} : IDisposable");
        writer.Open();

        writer.Line($"private readonly {lowLevel} {field};");
        foreach (var signal in signals)
            writer.Line($"private readonly LazyEvent<{EventArgsType(cls, signal)}> {EventField(signal)};");
        writer.Line("private bool _disposed;");
        writer.Blank();

        EmitConstructor(writer, cls, field, signals);

        var properties = cls.Properties.Where(p => p.Readable && IsExposableProperty(cls, p)).ToList();
        foreach (var property in properties)
            EmitProperty(writer, cls, property, field);
        if (properties.Count > 0)
            writer.Blank();

        foreach (var op in asyncOps)
            EmitAsyncMethod(writer, op, lowLevel, field, custom, usedOptions);

        foreach (var method in SyncMethods(cls, asyncOps))
            EmitSyncMethod(writer, method, field, custom);

        foreach (var options in OptionsUsedBy(asyncOps, custom))
        {
            usedOptions.Add(options);
            EmitOptionsBuilder(writer, options);
        }

        foreach (var signal in signals.Where(s => !custom.Signals.Contains(s.Name)))
            EmitSignalHandler(writer, cls, signal, field);

        EmitDispose(writer, field, signals);

        foreach (var signal in signals)
            EmitSignalEvent(writer, cls, signal);

        writer.Close();
        return writer.ToString();
    }

    private static void EmitConstructor(SourceWriter writer, Class cls, string field, List<Signal> signals)
    {
        writer.Line($"internal Frida{cls.Name}({cls.Name} {Camel(cls.Name)})");
        writer.Open();
        writer.Line($"{field} = {Camel(cls.Name)};");
        foreach (var signal in signals)
        {
            var handler = HandlerName(signal);
            var lowLevelEvent = "On" + Pascal(signal.Name);
            writer.Line($"{EventField(signal)} = new LazyEvent<{EventArgsType(cls, signal)}>(");
            writer.Line($"    _ => {field}.{lowLevelEvent} += {handler},");
            writer.Line($"    _ => {field}.{lowLevelEvent} -= {handler});");
        }
        writer.Close();
        writer.Blank();
    }

    private static void EmitProperty(SourceWriter writer, Class cls, Property property, string field)
    {
        var name = PropertyName(property.Name);
        if (Getter(cls, property) is { } getter && IsForeign(property.AnyType))
        {
            if (IsVariant(property.AnyType))
            {
                writer.Line($"public object? {name} => VariantMarshal.ToObject({field}.{Pascal(getter.Name)}());");
                return;
            }
            var getterType = CsType(getter.ReturnType.AnyType, getter.ReturnType.Nullable);
            writer.Line($"public {getterType} {name} => {field}.{Pascal(getter.Name)}();");
            return;
        }
        var baseType = CsType(property.AnyType, false);
        var type = baseType == "string" ? "string?" : baseType;
        writer.Line($"public {type} {name} => {field}.{Pascal(property.Name)};");
    }

    private static Method? Getter(Class cls, Property property)
        => cls.Methods.FirstOrDefault(m => m.Name == "get_" + property.Name.Replace('-', '_'));

    private static bool IsForeign(AnyType type)
        => type.TryPickT0(out var t, out _) && t is ComplexType c && (c.Namespace.Name == "GLib" || c.Namespace.Name == "Gio");

    private static bool IsVariant(AnyType type)
        => type.TryPickT0(out var t, out _) && t is ComplexType c && c.Namespace.Name == "GLib" && c.Name == "Variant";

    private static void EmitAsyncMethod(SourceWriter writer, Method op, string lowLevel, string field, CustomMembers custom, ISet<Class> usedOptions)
    {
        var name = Pascal(op.Name);
        var realParams = RealParameters(op).ToList();
        if (custom.Methods.Contains(op.Name) || realParams.Any(p => Classify(p) == ParamKind.Unsupported))
            return;

        var result = DescribeResult(op);
        if (result.Kind == ResultKind.Unsupported)
            return;

        var parameters = string.Join(", ", realParams.Select(ParameterDeclaration));
        var innerArgs = BuildInvokeArgs(lowLevel, name, field, realParams);
        var finish = $"{field}.{name}Finish";

        var hasStringParam = realParams.Any(IsStringParameter);
        var begin = hasStringParam
            ? BuildStringBegin(lowLevel, name, field, realParams)
            : $"ready => Internal.{lowLevel}.{name}({innerArgs})";

        var isAsync = result.Kind is ResultKind.Wrapper or ResultKind.List or ResultKind.Variant;
        if (isAsync)
            writer.Line($"public async {result.TaskType} {name}({parameters})");
        else
            writer.Line($"public {result.TaskType} {name}({parameters})");
        writer.Open();

        foreach (var p in realParams.Where(p => Classify(p) == ParamKind.Options))
            writer.Line($"var {Camel(p.Name)}Native = Build{OptionsType(p).Name}({Camel(p.Name)});");
        foreach (var p in realParams.Where(p => Classify(p) == ParamKind.Variant))
            writer.Line($"var {Camel(p.Name)}Variant = VariantMarshal.ToVariant({Camel(p.Name)});");

        switch (result.Kind)
        {
            case ResultKind.Void:
            case ResultKind.Value:
                writer.Line("return FridaRuntime.Invoke(");
                writer.Line($"    {begin},");
                writer.Line($"    {finish});");
                break;
            case ResultKind.Variant:
                writer.Line("var result = await FridaRuntime.Invoke(");
                writer.Line($"    {begin},");
                writer.Line($"    {finish}).ConfigureAwait(false);");
                writer.Line("return VariantMarshal.ToObject(result);");
                break;
            case ResultKind.Wrapper:
                writer.Line($"var result = await FridaRuntime.Invoke(");
                writer.Line($"    {begin},");
                writer.Line($"    {finish}).ConfigureAwait(false);");
                writer.Line(result.Nullable
                    ? $"return result != null ? new {result.ElementType}(result) : null;"
                    : $"return new {result.ElementType}(result);");
                break;
            case ResultKind.List:
                writer.Line($"using var list = await FridaRuntime.Invoke(");
                writer.Line($"    {begin},");
                writer.Line($"    {finish}).ConfigureAwait(false);");
                writer.Blank();
                writer.Line($"var items = new List<{result.ElementType}>();");
                writer.Line("for (var i = 0; i < list.Size(); i++)");
                writer.Line($"    items.Add(new {result.ElementType}(list.Get(i)));");
                writer.Line("return items;");
                break;
        }

        writer.Close();
        writer.Blank();
    }

    private static void EmitSyncMethod(SourceWriter writer, Method method, string field, CustomMembers custom)
    {
        if (custom.Methods.Contains(method.Name))
            return;
        var realParams = RealParameters(method).ToList();
        var returnType = method.ReturnType.AnyType;
        var isVoid = IsVoid(returnType);
        if (realParams.Any(p => Classify(p) != ParamKind.Simple) || (!isVoid && !IsExposableValue(returnType)))
            return;

        var name = Pascal(method.Name);
        var parameters = string.Join(", ", realParams.Select(p => $"{CsType(ParamType(p), p.Nullable)} {Camel(p.Name)}"));
        var args = string.Join(", ", realParams.Select(p => Camel(p.Name)));
        writer.Line($"public {(isVoid ? "void" : CsType(returnType, method.ReturnType.Nullable))} {name}({parameters})");
        writer.Open();
        writer.Line($"{(isVoid ? "" : "return ")}{field}.{name}({args});");
        writer.Close();
        writer.Blank();
    }

    private static void EmitSignalHandler(SourceWriter writer, Class cls, Signal signal, string field)
    {
        var args = signal.Parameters.ToList();
        var signature = args.Count == 0
            ? $"{cls.Name} sender, EventArgs eventArgs"
            : $"{cls.Name} sender, {cls.Name}.{Pascal(signal.Name)}SignalArgs eventArgs";
        writer.Line($"private void {HandlerName(signal)}({signature})");
        writer.Open();
        var construction = args.Count == 0
            ? $"new {EventArgsType(cls, signal)}()"
            : $"new {EventArgsType(cls, signal)}({string.Join(", ", args.Select(SignalArgExpression))})";
        writer.Line($"{EventField(signal)}.InvokeHandlers(this, {construction});");
        writer.Close();
        writer.Blank();
    }

    private static string SignalArgExpression(Parameter arg)
    {
        var source = $"eventArgs.{Pascal(arg.Name)}";
        if (IsVariant(arg.AnyTypeOrVarArgs.AsT0))
            return $"VariantMarshal.ToObject({source})";
        if (!IsWrapperType(arg.AnyTypeOrVarArgs.AsT0, out var wrapper))
            return source;
        return arg.Nullable
            ? $"{source} != null ? new {wrapper}({source}) : null"
            : $"new {wrapper}({source})";
    }

    private static bool IsWrapperType(AnyType type, out string wrapper)
    {
        wrapper = "";
        if (type.TryPickT0(out var t, out _) && t is Class c && c.Namespace.Name == "Frida")
        {
            wrapper = "Frida" + c.Name;
            return true;
        }
        return false;
    }

    public static List<Signal> SupportedSignals(Class cls)
        => cls.Signals.Where(s => s.Introspectable && s.Parameters.All(IsSupportedSignalArg)).ToList();

    private static bool IsSupportedSignalArg(Parameter p)
    {
        var type = p.AnyTypeOrVarArgs.AsT0;
        if (!type.TryPickT0(out var t, out _))
            return false;
        if (t is PrimitiveType || t is Enumeration || IsWrapperType(type, out _))
            return true;
        return t is ComplexType c && (c.Namespace.Name == "GLib" || c.Namespace.Name == "Gio");
    }

    private static void EmitDispose(SourceWriter writer, string field, List<Signal> signals)
    {
        writer.Line("public void Dispose()");
        writer.Open();
        writer.Line("if (_disposed)");
        writer.Open();
        writer.Line("return;");
        writer.Close();
        writer.Blank();
        writer.Line("_disposed = true;");
        foreach (var signal in signals)
            writer.Line($"{EventField(signal)}.Dispose();");
        writer.Line($"{field}.Dispose();");
        writer.Close();
    }

    private static void EmitSignalEvent(SourceWriter writer, Class cls, Signal signal)
    {
        writer.Blank();
        writer.Line($"public event EventHandler<{EventArgsType(cls, signal)}> On{Pascal(signal.Name)}");
        writer.Open();
        writer.Line($"add => {EventField(signal)}.Add(value);");
        writer.Line($"remove => {EventField(signal)}.Remove(value);");
        writer.Close();
    }

    private static string BuildInvokeArgs(string lowLevel, string name, string field, List<Parameter> realParams)
    {
        var args = new List<string> { $"{field}.Handle.DangerousGetHandle()" };
        args.AddRange(realParams.Select(InvokeArgument));
        args.Add("IntPtr.Zero");
        args.Add("ready");
        args.Add("IntPtr.Zero");
        return string.Join(", ", args);
    }

    private static string BuildStringBegin(string lowLevel, string name, string field, List<Parameter> realParams)
    {
        var sb = new StringBuilder();
        sb.Append("ready =>\n");
        sb.Append("            {\n");
        foreach (var p in realParams.Where(IsStringParameter))
            sb.Append($"                using var {Camel(p.Name)}Native = GLib.Internal.NonNullableUtf8StringOwnedHandle.Create({Camel(p.Name)});\n");
        sb.Append($"                Internal.{lowLevel}.{name}({BuildInvokeArgs(lowLevel, name, field, realParams)});\n");
        sb.Append("            }");
        return sb.ToString();
    }

    private static string ParameterDeclaration(Parameter p)
        => Classify(p) switch
        {
            ParamKind.Options => $"Data.{OptionsType(p).Name}? {Camel(p.Name)} = null",
            ParamKind.Variant => $"object? {Camel(p.Name)}",
            _ => $"{CsType(ParamType(p), p.Nullable)} {Camel(p.Name)}",
        };

    private static string InvokeArgument(Parameter p)
        => Classify(p) switch
        {
            ParamKind.String => $"{Camel(p.Name)}Native",
            ParamKind.Options => $"{Camel(p.Name)}Native.Handle.DangerousGetHandle()",
            ParamKind.Handle => $"{Camel(p.Name)}.Handle",
            ParamKind.Variant => $"{Camel(p.Name)}Variant.Handle",
            _ => Camel(p.Name),
        };

    private static IEnumerable<Class> OptionsUsedBy(List<Method> asyncOps, CustomMembers custom)
    {
        var seen = new HashSet<string>();
        foreach (var op in asyncOps.Where(o => !custom.Methods.Contains(o.Name)))
        {
            foreach (var p in RealParameters(op).Where(p => Classify(p) == ParamKind.Options))
            {
                var type = OptionsType(p);
                if (seen.Add(type.Name))
                    yield return type;
            }
        }
    }

    private static void EmitOptionsBuilder(SourceWriter writer, Class options)
    {
        var name = options.Name;
        writer.Line($"private static {name} Build{name}(Data.{name}? options)");
        writer.Open();
        writer.Line($"var result = {name}.New();");
        writer.Line("if (options == null)");
        writer.Open();
        writer.Line("return result;");
        writer.Close();
        writer.Blank();
        foreach (var field in OptionFields(options))
        {
            writer.Line($"if ({field.Guard("options")})");
            writer.Open();
            writer.Line($"result.{field.Name} = {field.Assignment("options")};");
            writer.Close();
        }
        writer.Line("return result;");
        writer.Close();
        writer.Blank();
    }

    private static IEnumerable<Method> AsyncOperations(Class cls)
    {
        var names = cls.Methods.Select(m => m.Name).ToHashSet();
        return cls.Methods.Where(m => names.Contains(m.Name + "_finish"));
    }

    public static bool IsFridaList(Class cls)
    {
        var get = cls.Methods.FirstOrDefault(m => m.Name == "get");
        return cls.Methods.Any(m => m.Name == "size")
            && get != null
            && get.ReturnType.AnyType.TryPickT0(out var t, out _)
            && t is Class e && e.Namespace.Name == "Frida";
    }

    private static string ListElement(Class cls)
    {
        var get = cls.Methods.First(m => m.Name == "get");
        return "Frida" + ((Class)get.ReturnType.AnyType.AsT0).Name;
    }

    private static IEnumerable<Method> SyncMethods(Class cls, List<Method> asyncOps)
    {
        var asyncNames = asyncOps.Select(m => m.Name).ToHashSet();
        var propertyNames = cls.Properties.Select(p => p.Name.Replace('-', '_')).ToHashSet();
        return cls.Methods.Where(m =>
            !m.Name.EndsWith("_finish") &&
            !m.Name.EndsWith("_sync") &&
            !asyncNames.Contains(m.Name) &&
            !m.IsAccessor() &&
            !BacksProperty(m.Name, propertyNames));
    }

    private static bool BacksProperty(string methodName, HashSet<string> propertyNames)
    {
        foreach (var prefix in new[] { "get_", "set_", "is_", "has_" })
        {
            if (methodName.StartsWith(prefix) && propertyNames.Contains(methodName[prefix.Length..]))
                return true;
        }
        return false;
    }

    private static Result DescribeResult(Method op)
    {
        var finish = op.Parent is Class cls
            ? cls.Methods.First(m => m.Name == op.Name + "_finish")
            : throw new Exception($"no finish for {op.Name}");
        var type = finish.ReturnType.AnyType;

        if (IsVoid(type))
            return new Result(ResultKind.Void, "Task", "", false);

        if (type.TryPickT0(out var t, out _) && t is Class cl && cl.Namespace.Name == "Frida")
        {
            if (IsFridaList(cl))
            {
                var element = ListElement(cl);
                return new Result(ResultKind.List, $"Task<IReadOnlyList<{element}>>", element, false);
            }

            var wrapper = "Frida" + cl.Name;
            return new Result(ResultKind.Wrapper, $"Task<{wrapper}{Nullable(finish.ReturnType.Nullable)}>", wrapper, finish.ReturnType.Nullable);
        }

        if (IsVariant(type))
            return new Result(ResultKind.Variant, "Task<object?>", "", true);

        var value = CsType(type, finish.ReturnType.Nullable);
        return new Result(ResultKind.Value, $"Task<{value}>", value, finish.ReturnType.Nullable);
    }

    private static IEnumerable<Parameter> RealParameters(Method method)
        => method.Parameters.Where(p => !IsPlumbing(p));

    private static bool IsPlumbing(Parameter p)
    {
        if (p.Name.StartsWith("_"))
            return true;
        if (ParamType(p).TryPickT0(out var t, out _) && t is ComplexType c && c.Namespace.Name == "Gio")
            return true;
        return false;
    }

    private static bool IsStringParameter(Parameter p)
        => ParamType(p).TryPickT0(out var t, out _) && t is GirModel.String;

    private static bool IsExposableValue(AnyType type)
        => type.TryPickT0(out var t, out _) && (t is PrimitiveType || IsFridaEnum(t));

    private static bool IsExposableProperty(Class cls, Property p)
    {
        if (!p.AnyType.TryPickT0(out var t, out _))
            return false;
        if (t is PrimitiveType || t is Enumeration)
            return true;
        return IsForeign(p.AnyType) && Getter(cls, p) != null;
    }

    private static bool IsFridaEnum(GirModel.Type t)
        => t is Enumeration e && e.Namespace.Name == "Frida";

    private static ParamKind Classify(Parameter p)
    {
        var type = ParamType(p);
        if (!type.TryPickT0(out var t, out _))
            return ParamKind.Unsupported;
        if (t is GirModel.String)
            return ParamKind.String;
        if (t is PrimitiveType || t is Enumeration)
            return ParamKind.Simple;
        if (t is Class c && c.Namespace.Name == "Frida" && c.Name.EndsWith("Options"))
            return ParamKind.Options;
        if (IsVariant(ParamType(p)))
            return ParamKind.Variant;
        if (t is ComplexType f && (f.Namespace.Name == "GLib" || f.Namespace.Name == "Gio") && !p.Nullable)
            return ParamKind.Handle;
        return ParamKind.Unsupported;
    }

    private static Class OptionsType(Parameter p)
        => (Class)ParamType(p).AsT0;

    private static AnyType ParamType(Parameter p)
        => p.AnyTypeOrVarArgs.AsT0;

    private static bool IsVoid(AnyType type)
        => type.TryPickT0(out var t, out _) && t is GirModel.Void;

    private static string CsType(AnyType type, bool nullable)
    {
        if (!type.TryPickT0(out var t, out _))
            return "byte[]";

        var mapped = t switch
        {
            GirModel.Void => "void",
            Bool => "bool",
            GirModel.Byte => "byte",
            SignedByte => "sbyte",
            Short => "short",
            UnsignedShort => "ushort",
            Integer => "int",
            UnsignedInteger => "uint",
            Long => "long",
            UnsignedLong => "ulong",
            GirModel.Double => "double",
            Float => "float",
            GirModel.String => "string",
            Enumeration e when e.Namespace.Name == "Frida" => e.Name,
            Enumeration e => $"{e.Namespace.Name}.{e.Name}",
            ComplexType c when c.Namespace.Name == "Frida" => "Frida" + c.Name,
            ComplexType c => $"{c.Namespace.Name}.{c.Name}",
            _ => "object",
        };
        return mapped + Nullable(nullable && mapped != "void");
    }

    private static string Nullable(bool nullable) => nullable ? "?" : "";

    private static string PropertyName(string girName)
        => girName == "dtype" ? "Type" : Pascal(girName);

    private static string EventArgsType(Class cls, Signal signal)
        => $"{cls.Name}{Pascal(signal.Name)}EventArgs";

    private static string EventField(Signal signal)
        => "_on" + Pascal(signal.Name);

    private static string HandlerName(Signal signal)
        => "Handle" + Pascal(signal.Name);

    private static string Field(string className)
        => "_" + Camel(className);

    private static string Pascal(string name)
        => string.Concat(name.Split('_', '-').Where(p => p.Length > 0).Select(p => char.ToUpperInvariant(p[0]) + p[1..]));

    private static string Camel(string name)
    {
        var pascal = Pascal(name);
        var camel = char.ToLowerInvariant(pascal[0]) + pascal[1..];
        return Keywords.Contains(camel) ? "@" + camel : camel;
    }

    private static readonly HashSet<string> Keywords = new()
    {
        "event", "object", "string", "params", "in", "out", "ref", "lock", "base",
        "default", "new", "this", "namespace", "internal", "class", "struct", "bool",
        "byte", "int", "uint", "long", "ulong", "short", "ushort", "float", "double",
        "void", "operator", "is", "as", "checked", "fixed", "delegate", "interface",
    };

    private static IEnumerable<OptionField> OptionFields(Class options)
    {
        foreach (var property in options.Properties.Where(p => p.Writeable))
        {
            var field = DescribeOptionField(property);
            if (field != null)
                yield return field;
        }
    }

    private static OptionField? DescribeOptionField(Property property)
    {
        var name = Pascal(property.Name);
        var type = property.AnyType;

        if (type.TryPickT1(out var array, out _))
        {
            if (array.AnyType.TryPickT0(out var element, out _) && element is GirModel.String)
                return new OptionField(name, "string[]?", $"x.{name} is {{ Length: > 0 }}", $"x.{name}");
            return null;
        }

        var t = type.AsT0;
        if (t is Enumeration e)
            return new OptionField(name, e.Name + "?", $"x.{name} != null", $"x.{name}.Value");
        if (t is GirModel.String)
            return new OptionField(name, "string?", $"x.{name} != null", $"x.{name}");
        if (t is PrimitiveType)
            return new OptionField(name, CsType(type, true), $"x.{name}.HasValue", $"x.{name}.Value");
        if (t is ComplexType c && c.Name == "Bytes")
            return new OptionField(name, "byte[]?", $"x.{name} != null", $"GLib.Bytes.New(x.{name})");
        return null;
    }

    private enum ResultKind { Void, Value, Wrapper, List, Variant, Unsupported }

    private enum ParamKind { Simple, String, Options, Handle, Variant, Unsupported }

    private record Result(ResultKind Kind, string TaskType, string ElementType, bool Nullable);

    private record OptionField(string Name, string DataType, string GuardExpr, string AssignmentExpr)
    {
        public string Guard(string variable) => GuardExpr.Replace("x.", variable + ".");
        public string Assignment(string variable) => AssignmentExpr.Replace("x.", variable + ".");
    }

    public static string EmitEventArgs(Class cls, Signal signal)
    {
        var args = signal.Parameters.ToList();
        var writer = new SourceWriter();
        writer.Line("#nullable enable");
        writer.Blank();
        writer.Line("namespace Frida.Events;");
        writer.Blank();
        writer.Line($"public class {EventArgsType(cls, signal)} : EventArgs");
        writer.Open();

        if (args.Count > 0)
        {
            var parameters = string.Join(", ", args.Select(a => $"{EventArgType(a)} {Camel(a.Name)}"));
            writer.Line($"public {EventArgsType(cls, signal)}({parameters})");
            writer.Open();
            foreach (var a in args)
                writer.Line($"{Pascal(a.Name)} = {Camel(a.Name)};");
            writer.Close();
            writer.Blank();
            foreach (var a in args)
                writer.Line($"public {EventArgType(a)} {Pascal(a.Name)} {{ get; }}");
        }

        writer.Close();
        return writer.ToString();
    }

    public static string EmitDataRecord(Class options)
    {
        var writer = new SourceWriter();
        writer.Line("#nullable enable");
        writer.Blank();
        writer.Line("namespace Frida.Data;");
        writer.Blank();
        writer.Line($"public class {options.Name}");
        writer.Open();
        foreach (var field in OptionFields(options))
            writer.Line($"public {field.DataType} {field.Name} {{ get; set; }}");
        writer.Close();
        return writer.ToString();
    }

    private static string EventArgType(Parameter arg)
    {
        var type = arg.AnyTypeOrVarArgs.AsT0;
        if (IsVariant(type))
            return "object?";
        return IsWrapperType(type, out var wrapper)
            ? wrapper + Nullable(arg.Nullable)
            : CsType(type, arg.Nullable);
    }
}

internal record CustomMembers(IReadOnlySet<string> Signals, IReadOnlySet<string> Methods)
{
    public static CustomMembers For(string className) => className switch
    {
        _ => new CustomMembers(new HashSet<string>(), new HashSet<string>()),
    };

    public static bool IsFullyCustom(string className) => className switch
    {
        "Script" or "Compiler" => true,
        _ => false,
    };
}
