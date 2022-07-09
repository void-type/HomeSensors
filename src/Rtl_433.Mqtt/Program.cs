using Rtl_433.Mqtt;


IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        var mqttClient = context.Configuration.GetSection("Mqtt").Get<MqttConfiguration>();
        services.AddSingleton<MqttConfiguration>(mqttClient);
        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();
