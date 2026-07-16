using TelcoLab.Silo;

var builder = Host.CreateApplicationBuilder(args);

builder.UseOrleans(silo =>
{
    silo.UseLocalhostClustering();
    silo.AddMemoryGrainStorage("subscriptionStore");
});

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
