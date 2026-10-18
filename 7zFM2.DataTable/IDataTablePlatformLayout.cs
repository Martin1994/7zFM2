namespace SevenZip.FileManager2.Controls;

/// <summary>
/// Optional native viewport metrics and backend repairs for virtualized row frames. Register
/// in a platform head, not the platform-neutral control assembly.
/// </summary>
public interface IDataTablePlatformLayout
{
    void RefreshRow(Layout row, double width);

    /// <summary>Available scroll area, excluding non-overlay scrollbar chrome.</summary>
    Size GetViewportSize(ScrollView scroll) => new(scroll.Width, scroll.Height);

    /// <summary>Available row width inside the list, excluding its vertical scrollbar.</summary>
    double GetRowViewportWidth(CollectionView list) => list.Width;

    /// <summary>Counter the ancestor's horizontal scroll without resizing/reloading the list.</summary>
    void SetHorizontalScrollOffset(CollectionView list, double offset) => list.TranslationX = offset;

    /// <summary>Observe native viewport size changes which MAUI SizeChanged does not report.</summary>
    IDisposable? ObserveViewport(ScrollView scroll, CollectionView list, Action changed) => null;
}