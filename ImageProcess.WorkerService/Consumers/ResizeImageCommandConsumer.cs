using MassTransit;
using Shared.Bus;

namespace ImageProcess.WorkerService.Consumers
{
    internal class ResizeImageCommandConsumer(ILogger<ResizeImageCommandConsumer> logger)
        : IConsumer<ResizeImageCommand>
    {
        public async Task Consume(ConsumeContext<ResizeImageCommand> context)
        {
            await Task.Delay(3000);

            var command = context.Message;

            logger.LogInformation($"Resizing image from {command.ImageUrl} to {command.Width}x{command.Height}");
        }
    }
}