using GirLoader;
using GirModel;

var positional = new List<string>();
var searchDirs = new List<string>();
string? stampFile = null;
for (var i = 0; i < args.Length; i++)
{
    if (args[i] == "--search" && i + 1 < args.Length)
        searchDirs.Add(args[++i]);
    else if (args[i] == "--stamp" && i + 1 < args.Length)
        stampFile = args[++i];
    else
        positional.Add(args[i]);
}

var apiDir = positional.Count > 0
    ? positional[0]
    : Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "build", "subprojects", "frida-core", "src", "api");
apiDir = Path.GetFullPath(apiDir);

var frida = LoadFridaNamespace(apiDir, searchDirs);

if (positional.Count > 2 && positional[1] == "--all")
{
    EmitAll(frida, Path.GetFullPath(positional[2]));
    if (stampFile != null)
        File.WriteAllText(stampFile, "generated");
    return 0;
}

if (positional.Count > 1)
{
    var cls = frida.Classes.First(c => c.Name == positional[1]);
    Console.Write(WrapperEmitter.Emit(cls));
    return 0;
}

Console.WriteLine($"Frida {frida.Version}: {frida.Classes.Count()} classes, {frida.Enumerations.Count()} enums, {frida.Records.Count()} records");
return 0;

static void EmitAll(Namespace frida, string outDir)
{
    var eventsDir = Path.Combine(outDir, "Events");
    var dataDir = Path.Combine(outDir, "Data");
    Directory.CreateDirectory(outDir);
    Directory.CreateDirectory(eventsDir);
    Directory.CreateDirectory(dataDir);

    foreach (var dir in new[] { outDir, eventsDir, dataDir })
        foreach (var stale in Directory.GetFiles(dir, "*.g.cs"))
            File.Delete(stale);

    var options = new HashSet<Class>();
    var wrappers = 0;
    var events = 0;

    foreach (var cls in frida.Classes.Where(ShouldGenerate))
    {
        var custom = CustomMembers.For(cls.Name);
        File.WriteAllText(Path.Combine(outDir, $"Frida{cls.Name}.g.cs"), WrapperEmitter.Emit(cls, options));
        wrappers++;

        foreach (var signal in WrapperEmitter.SupportedSignals(cls).Where(s => !custom.Signals.Contains(s.Name)))
        {
            File.WriteAllText(Path.Combine(eventsDir, $"{cls.Name}{Pascal(signal.Name)}EventArgs.g.cs"), WrapperEmitter.EmitEventArgs(cls, signal));
            events++;
        }
    }

    foreach (var opt in options)
        File.WriteAllText(Path.Combine(dataDir, $"{opt.Name}.g.cs"), WrapperEmitter.EmitDataRecord(opt));

    Console.WriteLine($"Emitted {wrappers} wrappers, {events} event args, {options.Count} option records to {outDir}");
}

static bool ShouldGenerate(Class cls)
    => !WrapperEmitter.IsFridaList(cls)
    && !cls.Name.EndsWith("Options");

static string Pascal(string name)
    => string.Concat(name.Split('_', '-').Where(p => p.Length > 0).Select(p => char.ToUpperInvariant(p[0]) + p[1..]));

static Namespace LoadFridaNamespace(string apiDir, IReadOnlyList<string> searchDirs)
{
    var dirs = new List<string> { apiDir };
    dirs.AddRange(searchDirs.Select(Path.GetFullPath));

    var repoRoot = Path.GetFullPath(Path.Combine(apiDir, "..", "..", "..", "..", ".."));
    var fallbackDir = Path.Combine(repoRoot, "gir.core", "ext", "gir-files", "windows");
    if (Directory.Exists(fallbackDir))
        dirs.Add(fallbackDir);

    var stdout = Console.Out;
    Console.SetOut(Console.Error);
    try
    {
        var resolver = new ChainedRepositoryResolver(dirs.Select(d => (IRepositoryResolver)new DirectoryRepositoryResolver(d)).ToArray());
        var input = resolver.ResolveRepository("Frida-1.0.gir")
            ?? throw new Exception($"Frida-1.0.gir not found in {apiDir}");

        var includeResolver = new IncludeResolver(resolver);
        var loader = new Loader(includeResolver.ResolveInclude);
        var repositories = loader.Load(new[] { input });

        return repositories.Select(r => r.Namespace).First(ns => ns.Name == "Frida");
    }
    finally
    {
        Console.SetOut(stdout);
    }
}
