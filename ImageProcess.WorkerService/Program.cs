using ImageProcess.WorkerService.Consumers;
using MassTransit;

var builder = Host.CreateApplicationBuilder(args);


builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<ResizeImageCommandConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(new Uri(builder.Configuration.GetConnectionString("RabbitMQ")!));


        cfg.ConfigureEndpoints(context);
    });
});


var host = builder.Build();
host.Run();