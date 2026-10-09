namespace Frida;

public class FridaCompilerOptions
{
    public string? ProjectRoot { get; set; }
    public SourceMaps SourceMaps { get; set; } = SourceMaps.Omitted;
    public JsCompression Compression { get; set; } = JsCompression.None;
    public TypeCheckMode TypeCheck { get; set; } = TypeCheckMode.None;
    public OutputFormat OutputFormat { get; set; } = OutputFormat.Unescaped;
    public BundleFormat BundleFormat { get; set; } = BundleFormat.Esm;
    public JsPlatform Platform { get; set; } = JsPlatform.Gum;

    internal BuildOptions ToNative()
    {
        var options = BuildOptions.New();
        if (ProjectRoot != null)
            options.ProjectRoot = ProjectRoot;
        options.SourceMaps = SourceMaps;
        options.Compression = Compression;
        options.TypeCheck = TypeCheck;
        options.OutputFormat = OutputFormat;
        options.BundleFormat = BundleFormat;
        options.Platform = Platform;
        return options;
    }
}
