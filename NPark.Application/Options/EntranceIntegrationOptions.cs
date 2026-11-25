namespace NPark.Application.Options;

public sealed class LprCaptureOptions
{
    public bool Enabled { get; set; } = true;
    public int RetentionDays { get; set; } = 3;
    public int ArchiveCheckIntervalMinutes { get; set; } = 60;
    public int ArchiveBatchSize { get; set; } = 1000;
    public int? MaxMatchingAgeSeconds { get; set; }
}

public sealed class SubscriberButtonLockOptions
{
    public bool Enabled { get; set; }
    public int[] SupportedControllerIds { get; set; } = [];
    public int DuplicateWindowSeconds { get; set; } = 60;

    public bool Supports(int controllerId) => Enabled && SupportedControllerIds.Contains(controllerId);
}

public sealed class AutomaticVisitorEntryOptions
{
    public bool Enabled { get; set; }
    public Guid? SystemUserId { get; set; }
    public int[] SupportedControllerIds { get; set; } = [];

    public bool Supports(int controllerId) => Enabled && SupportedControllerIds.Contains(controllerId);
}
