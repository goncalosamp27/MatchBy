namespace MatchBy.Settings;

public sealed class UploadSettings
{
    public long MaxFileSizeBytes { get; init; } = 50 * 1024 * 1024;
}
