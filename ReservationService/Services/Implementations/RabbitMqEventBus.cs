using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using ReservationService.Common.Events;
using ReservationService.Configuration;
using System.Text;
using System.Text.Json;

namespace ReservationService.Services.Implementations
{
    public class RabbitMqEventBus : IEventBus, IDisposable
    {
        private readonly IConnection _connection;
        private readonly IChannel _channel;
        private readonly RabbitMqSettings _settings;

        public RabbitMqEventBus(IOptions<RabbitMqSettings> options)
        {
            _settings = options.Value;

            var factory = new ConnectionFactory
            {
                HostName = _settings.Host,
                UserName = _settings.User,
                Password = _settings.Pass,
                VirtualHost = _settings.VirtualHost
            };

            _connection = factory.CreateConnectionAsync().Result;

            _channel = _connection.CreateChannelAsync().Result;

            _channel.ExchangeDeclareAsync(
                exchange: _settings.Exchange,
                type: ExchangeType.Topic,
                durable: true
            ).Wait();
        }

        public async Task PublishAsync<T>(T @event, CancellationToken ct = default)
            where T : IIntegrationEvent
        {
            var message = JsonSerializer.Serialize(@event);
            var body = Encoding.UTF8.GetBytes(message);

            await _channel.BasicPublishAsync(
                exchange: _settings.Exchange,
                routingKey: typeof(T).Name,
                body: body,
                cancellationToken: ct
            );
        }

        public void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
        }
    }
}
