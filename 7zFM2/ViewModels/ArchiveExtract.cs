using Microsoft.UI.Dispatching;
using System.Diagnostics;

namespace SevenZip.FileManager2.ViewModels;

public partial class ArchiveExtract : ObservableObject
{
    private static readonly TimeSpan UI_REFRESH_RATE = TimeSpan.FromMilliseconds(100);

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private string _progressPercentage = "";

    [ObservableProperty]
    private double _ratio;

    [ObservableProperty]
    private ulong _totalBytes;

    [ObservableProperty]
    private ulong _processedItems;

    [ObservableProperty]
    private string _currentFilePath = "";

    [ObservableProperty]
    private string _mode = "";

    [ObservableProperty]
    private string _status = "";

    public async void Extract(SevenZipInArchive archive, IEnumerable<SevenZipItemNode> nodes)
    {
        Progress = 0;
        TotalBytes = 0;
        ProcessedItems = 0;
        Mode = "Test archive";
        Status = "";
        CurrentFilePath = "";

        // TODO: cancellation
        try
        {
            var callback = new ArchiveExtractCallback(this, archive, UI_REFRESH_RATE);
            await Task.Run(() => archive.Extract(nodes, NAskMode.kTest, callback));

            callback.Flush(true);

            DispatcherQueue.GetForCurrentThread().TryEnqueue(() =>
            {
                CurrentFilePath = "";
                Status = "Completed.";
            });
        }
        catch (Exception ex)
        {
            Status = Mode + " failed. " + ex.Message;
        }
    }
}

class RateControlledSetter<T>
{
    private readonly Action<T> _setter;
    private readonly TimeSpan _interval;
    private DateTime _nextUpdate = DateTime.Now;

    public RateControlledSetter(TimeSpan interval, Action<T> setter)
    {
        _setter = setter;
        _interval = interval;
    }

    public void Set(T value)
    {
        if (_nextUpdate <= DateTime.Now)
        {
            _setter(value);
            _nextUpdate = DateTime.Now + _interval;
        }
    }
}

file class ArchiveExtractCallback : IArchiveExtractCallback, ICompressProgressInfo
{
    private readonly ArchiveExtract _vm;
    private readonly SevenZipInArchive _archive;
    private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread() ?? throw new InvalidOperationException("ArchiveExtractCallback must be constructed in a UI thread.");

    private ulong _items = 0;
    private uint _currentId = 0;
    private long _lastReportedId = -1;
    private ulong _total = 0;
    private ulong _completed = 0;
    private ulong _completedIn = 0;
    private ulong _completedOut = 0;

    private readonly TimeSpan _interval;
    private DateTime _nextUpdate = DateTime.Now;

    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

    public ArchiveExtractCallback(ArchiveExtract vm, SevenZipInArchive archive, TimeSpan refreshInterval)
    {
        _vm = vm;
        _archive = archive;
        _interval = refreshInterval;
        _nextUpdate = DateTime.Now;
    }

    public void Flush(bool force)
    {
        if (_nextUpdate <= DateTime.Now || force)
        {
            _nextUpdate = DateTime.Now + _interval;

            string? currentFile = null;
            if (_currentId != _lastReportedId)
            {
                currentFile = _archive[_currentId].Path;
            }

            _dispatcherQueue.TryEnqueue(() =>
            {
                double progress = ClampRatio((double)_completed / _total);

                _vm.Progress = progress * 100;
                _vm.ProgressPercentage = progress.ToString("P2");
                _vm.Ratio = ClampRatio((double)_completedIn / _completedOut);
                _vm.ProcessedItems = _items;

                double remainingMs = _stopwatch.ElapsedMilliseconds / progress * (1 - progress);
                if (double.IsFinite(remainingMs))
                {
                    TimeSpan remaining = TimeSpan.FromMilliseconds(remainingMs);
                    _vm.Status = $"{FormatTimeSpan(remaining)} remaining...";
                }

                if (currentFile != null)
                {
                    _vm.CurrentFilePath = currentFile;
                }
            });
        }
    }

    private static double ClampRatio(double value)
    {
        if (!double.IsFinite(value))
        {
            return 0;
        }

        return Math.Clamp(value, 0, 1);
    }

    private static string FormatTimeSpan(TimeSpan ts)
    {
        if (ts.TotalDays > 1)
        {
            return $"{ts.TotalDays:f2} days";
        }
        if (ts.TotalHours > 1)
        {
            return $"{ts.TotalHours:f1} hours";
        }
        if (ts.TotalMinutes > 1)
        {
            return $"{ts.TotalMinutes:f1} minutes";
        }
        if (ts.TotalSeconds > 1)
        {
            return $"{ts.TotalSeconds:f0} seconds";
        }

        return "Less than a second";
    }

    public Stream? GetStream(uint index, NAskMode askExtractMode)
    {
        _items++;
        _currentId = index;
        Flush(false);

        if (askExtractMode == NAskMode.kTest)
        {
            return null;
        }
        return new MemoryStream();
    }

    public void PrepareOperation(NAskMode askExtractMode)
    {
    }

    public void SetCompleted(in ulong size)
    {
        _completed = size;
        Flush(false);
    }

    public void SetOperationResult(NOperationResult opRes)
    {
    }

    public void SetRatioInfo(in ulong inSize, in ulong outSize)
    {
        _completedIn = inSize;
        _completedOut = outSize;
        Flush(false);
    }

    public void SetTotal(ulong size)
    {
        _total = size;
        _dispatcherQueue.TryEnqueue(() =>
        {
            _vm.TotalBytes = size;
        });
    }
}
