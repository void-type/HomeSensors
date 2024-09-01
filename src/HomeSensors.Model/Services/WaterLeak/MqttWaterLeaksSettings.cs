﻿namespace HomeSensors.Model.Services.WaterLeak;

public class MqttWaterLeaksSettings
{
    public bool IsEnabled { get; init; } = true;
    public int BetweenTicksMinutes { get; init; } = 20;
    public int BetweenNotificationsMinutes { get; init; } = 120;
    public int InactiveDeviceLimitMinutes { get; init; } = 90;
    public MqttWaterLeakSettingsDevice[] Devices { get; init; } = [];
}
