namespace OwlCore.Kubo.Models;

internal class MfsFileStatData
{
    public int? Blocks { get; set; }
    public ulong? CumulativeSize { get; set; }
    public bool? Local { get; set; }
    public ulong? SizeLocal { get; set; }
    public bool? WithLocality { get; set; }
    public string? Hash { get; set; }
    public string? Name { get; set; }
    public string? Type { get; set; }
    public ulong? Size { get; set; }
}