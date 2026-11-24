namespace NPark.Application.Options;

public sealed class ParkingBusinessOptions
{
    public string TimeZoneId { get; set; } = "Africa/Cairo";
}

public sealed class GateOperationOptions
{
    public string CommandTopicTemplate { get; set; } = "sub/network/{controllerId}";
    public string AckTopicFilter { get; set; } = "ack/network/#";
    public int CommandQos { get; set; } = 1;
    public int AckQos { get; set; } = 1;
    public bool Retain { get; set; }
    public int AckTimeoutSeconds { get; set; } = 3;
    public int MaxRetries { get; set; } = 2;
    public int RetryIntervalMilliseconds { get; set; } = 1000;
    public bool EnableAutomaticRetry { get; set; }
    public bool HardwareDeduplicationConfirmed { get; set; }
    public bool RequireAcknowledgment { get; set; } = true;
    public int RecoveryIntervalSeconds { get; set; } = 2;

    public string CommandTopic(int controller) => CommandTopicTemplate.Replace(
        "{controllerId}", controller.ToString(System.Globalization.CultureInfo.InvariantCulture));
}
