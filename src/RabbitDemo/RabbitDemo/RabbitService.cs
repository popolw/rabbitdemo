using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using IRabbit = EasyNetQ.IBus;
using EasyNetQ;

namespace RabbitDemo
{
    public class PublishService : BackgroundService
    {
        private readonly IRabbit _rabbit;
        public PublishService(IRabbit rabbit)
        {
            _rabbit = rabbit;
        }

        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var random = new Random();
            var value = random.Next(1000, 3000);
            var message = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}  {Guid.NewGuid():N}";
            await _rabbit.BroadCastAsync("broadcast", message);
            await Task.Delay(value);
        }
    }

    public class RabbitService : BackgroundService
    {
        private readonly IRabbit _rabbit;
        private readonly ILogger _logger;
        public RabbitService(IRabbit rabbit,ILogger<RabbitService> logger)
        {
            _rabbit = rabbit;
            _logger = logger;
        }

        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _rabbit.SubscribeAsync("broadcast",(headers,message)=> {
                _logger.LogInformation(message);
                return Task.CompletedTask;
            });
        }
    }
}
