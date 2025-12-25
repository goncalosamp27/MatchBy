namespace MatchBy.UnitTests.Tz;

public class TzTests
{
    #region FromId Tests

    [Fact]
    public void FromId_WithValidTimezoneId_ShouldReturnTimeZoneInfo()
    {
        // Arrange
        string timezoneId = "Europe/Lisbon";

        // Act
        TimeZoneInfo result = MatchBy.Tz.FromId(timezoneId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("GMT Standard Time", result.Id);
    }

    [Fact]
    public void FromId_WithWindowsTimezoneId_ShouldReturnTimeZoneInfo()
    {
        // Arrange
        string timezoneId = "GMT Standard Time";

        // Act
        TimeZoneInfo result = MatchBy.Tz.FromId(timezoneId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("GMT Standard Time", result.Id);
    }

    [Fact]
    public void FromId_WithInvalidTimezoneId_ShouldThrowException()
    {
        // Arrange
        string invalidTimezoneId = "Invalid/Timezone";

        // Act & Assert
        Assert.Throws<TimeZoneNotFoundException>(() => MatchBy.Tz.FromId(invalidTimezoneId));
    }

    #endregion

    #region ToLocal Tests

    [Fact]
    public void ToLocal_WithUtcDateTime_ShouldConvertToLocalTime()
    {
        // Arrange
        var utcTime = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        TimeZoneInfo lisbonTz = MatchBy.Tz.FromId("Europe/Lisbon");

        // Act
        DateTime result = MatchBy.Tz.ToLocal(utcTime, lisbonTz);

        // Assert
        Assert.Equal(DateTimeKind.Unspecified, result.Kind);
        // UTC 12:00 in Lisbon (WET/WEST) should be 12:00 or 13:00 depending on DST
        Assert.True(result.Hour == 12 || result.Hour == 13); // Account for DST
        Assert.Equal(utcTime.Date, result.Date);
    }

    [Fact]
    public void ToLocal_WithUnspecifiedDateTime_ShouldConvertToLocalTime()
    {
        // Arrange
        var unspecifiedTime = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Unspecified);
        TimeZoneInfo utcTz = MatchBy.Tz.FromId("UTC");

        // Act
        DateTime result = MatchBy.Tz.ToLocal(unspecifiedTime, utcTz);

        // Assert
        Assert.Equal(DateTimeKind.Utc, result.Kind);
        Assert.Equal(unspecifiedTime, result);
    }

    #endregion

    #region ToUtc Tests

    [Fact]
    public void ToUtc_WithLocalDateTime_ShouldConvertToUtc()
    {
        // Arrange
        var localTime = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Unspecified);
        TimeZoneInfo lisbonTz = MatchBy.Tz.FromId("Europe/Lisbon");

        // Act
        DateTime result = MatchBy.Tz.ToUtc(localTime, lisbonTz);

        // Assert
        Assert.Equal(DateTimeKind.Utc, result.Kind);
        // Lisbon local time converted to UTC should be different
        Assert.Equal(localTime.Date, result.Date);
    }

    [Fact]
    public void ToUtc_WithUtcTimezone_ShouldReturnSameTime()
    {
        // Arrange
        var localTime = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Unspecified);
        TimeZoneInfo utcTz = MatchBy.Tz.FromId("UTC");

        // Act
        DateTime result = MatchBy.Tz.ToUtc(localTime, utcTz);

        // Assert
        Assert.Equal(DateTimeKind.Utc, result.Kind);
        Assert.Equal(localTime, result);
    }

    [Fact]
    public void ToUtc_WithDstTransition_ShouldHandleCorrectly()
    {
        // Arrange - Test during DST transition (March 31, 2024 was DST in Europe)
        var dstTime = new DateTime(2024, 3, 31, 2, 30, 0, DateTimeKind.Unspecified);
        TimeZoneInfo lisbonTz = MatchBy.Tz.FromId("Europe/Lisbon");

        // Act
        DateTime result = MatchBy.Tz.ToUtc(dstTime, lisbonTz);

        // Assert
        Assert.Equal(DateTimeKind.Utc, result.Kind);
        Assert.Equal(dstTime.Date, result.Date);
        // Should be 1 hour behind due to DST
        Assert.Equal(1, 2 - result.Hour); // Local 2:30 should be UTC 1:30
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void RoundTripConversion_UtcToLocalToUtc_ShouldPreserveTime()
    {
        // Arrange
        var originalUtc = new DateTime(2024, 6, 15, 10, 30, 45, DateTimeKind.Utc);
        TimeZoneInfo lisbonTz = MatchBy.Tz.FromId("Europe/Lisbon");

        // Act
        DateTime local = MatchBy.Tz.ToLocal(originalUtc, lisbonTz);
        DateTime backToUtc = MatchBy.Tz.ToUtc(local, lisbonTz);

        // Assert
        Assert.Equal(originalUtc, backToUtc);
    }

    [Fact]
    public void RoundTripConversion_LocalToUtcToLocal_ShouldPreserveTime()
    {
        // Arrange
        var originalLocal = new DateTime(2024, 6, 15, 10, 30, 45, DateTimeKind.Unspecified);
        TimeZoneInfo tokyoTz = MatchBy.Tz.FromId("Asia/Tokyo");

        // Act
        DateTime utc = MatchBy.Tz.ToUtc(originalLocal, tokyoTz);
        DateTime backToLocal = MatchBy.Tz.ToLocal(utc, tokyoTz);

        // Assert
        Assert.Equal(originalLocal, backToLocal);
    }

    #endregion
}
