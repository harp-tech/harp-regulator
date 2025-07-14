namespace Harp.Devices;

// The order of this enum is significant, higher confidence levels must come later!
public enum DeviceConfidence
{
    /// <summary>The device is just a serial port and we have no reason to think it's a Harp device</summary>
    Zero,
    /// <summary>The device looks like it could be a Harp device, but it might not be</summary>
    Low,
    /// <summary>The device is definitely a Harp device</summary>
    High,
}

public static class DeviceConfidenceEx
{
    public static DeviceConfidence PromoteTo(this DeviceConfidence from, DeviceConfidence to)
        => from >= to ? from : to;

    public static DeviceConfidence DemoteTo(this DeviceConfidence from, DeviceConfidence to)
        => from >= to ? to : from;
}
