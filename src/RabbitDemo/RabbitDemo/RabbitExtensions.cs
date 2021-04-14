using EasyNetQ;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RabbitDemo
{
    public static class RabbitExtensions
    {
        private static string Serialize<T>(T message)
        {
            if (message == null) return string.Empty;
            var code = Type.GetTypeCode(typeof(T));
            switch (code)
            {
                case TypeCode.Empty:
                    return string.Empty;
                case TypeCode.Object:
                    return System.Text.Json.JsonSerializer.Serialize(message);
                case TypeCode.DBNull:
                    return string.Empty;
                case TypeCode.Boolean:
                case TypeCode.Char:
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                case TypeCode.DateTime:
                case TypeCode.String:
                    return message.ToString();
                default:
                    return string.Empty;
            }
        }

        public static Task PublishAsync<T>(this IBus bus, string name, T message)
        {
            return bus.PublishAsync(name, message, ExchangeType.Direct);
        }

        public static Task BroadCastAsync<T>(this IBus bus,string name, T message)
        {
            return bus.PublishAsync(name, message, ExchangeType.Fanout);
        }


        private static async Task PublishAsync<T>(this IBus bus, string name, T message, string exchangeType,IDictionary<string,object> headers=null)
        {
            var value = Serialize(message);
            var queue = await bus.Advanced.QueueDeclareAsync(name);
            var exchange = await bus.Advanced.ExchangeDeclareAsync(name, exchangeType);
            await bus.Advanced.BindAsync(exchange, queue, name, default);
            var buffer = Encoding.UTF8.GetBytes(value);
            await bus.Advanced.PublishAsync(exchange, name, false, new MessageProperties { Headers =headers }, buffer);
        }


        public static async Task<IDisposable> ReceiveAsync(this IBus bus, string queue, Func<IDictionary<string,string>, string, Task> onMessage, CancellationToken cancellationToken = default)
        {
            return await bus.ReceiveAsync(queue, async (body, properties, info) =>
            {
                var headers = new Dictionary<string, string>();
                foreach (var header in properties.Headers)
                {
                    var value = Encoding.UTF8.GetString(header.Value as byte[]);
                    headers.Add(header.Key, value);
                }
                await onMessage(headers, Encoding.UTF8.GetString(body));
                if (!cancellationToken.IsCancellationRequested)
                {
                    await bus.ReceiveAsync(queue, onMessage, cancellationToken);
                }
            }, cancellationToken);
        }

        private static async Task<IDisposable> ReceiveAsync(this IBus bus, string queue, Func<byte[], MessageProperties, MessageReceivedInfo, Task> onMessage, CancellationToken cancellationToken)
        {
            var declareQueue = await bus.Advanced.QueueDeclareAsync(queue, cancellationToken);
            return bus.Advanced.Consume(declareQueue, onMessage);
        }

        public static async Task<IDisposable> SubscribeAsync(this IBus bus,string name, Func<IDictionary<string, string>, string, Task> onMessage, CancellationToken token=default)
        {
            var id = Guid.NewGuid().ToString();
            var exchange = await bus.Advanced.ExchangeDeclareAsync(name, ExchangeType.Fanout);
            var queue = bus.Advanced.QueueDeclare($"{name}-{id}",config=> {
                config.AsExclusive(true);
            });
            bus.Advanced.Bind(exchange, queue, name, token);
            return bus.Advanced.Consume(queue, async (body, properties, info) =>
            {
                var headers = new Dictionary<string, string>();
                foreach (var header in properties.Headers)
                {
                    var value = Encoding.UTF8.GetString(header.Value as byte[]);
                    headers.Add(header.Key, value);
                }
                await onMessage(headers, Encoding.UTF8.GetString(body));
            });

        }
    }
}
