namespace SevenZip.FileManager2.Controls;

/// <summary>
/// Optional backend repair for virtualized row frames. Register in a platform head, not the
/// platform-neutral control assembly. Official MAUI handlers do not need this service.
/// </summary>
public interface IDataTablePlatformLayout
{
    void RefreshRow(Layout row, double width);
}