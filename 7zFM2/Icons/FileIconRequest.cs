namespace SevenZip.FileManager2.Icons;

/// <summary>Describes an icon, not a UI image. A null path denotes a virtual/archive item.</summary>
public sealed record FileIconRequest(FileIconKind Kind, string Name, string? FileSystemPath = null);

public enum FileIconKind
{
    File,
    Directory,
    Drive,
    Computer,
}