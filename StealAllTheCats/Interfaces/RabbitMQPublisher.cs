
namespace StealAllTheCats.Interfaces
{
    using Microsoft.AspNetCore.Connections;
    using Microsoft.Extensions.Configuration;
    using Newtonsoft.Json;
    using RabbitMQ.Client;
    using StealAllTheCats.Models;
    using System.Text;

    public class RabbitMQPublisher : IRabbitMQPublisher
    {
        private readonly IConfiguration _config;

        public RabbitMQPublisher(IConfiguration config)
        {
            _config = config;
        }

        public async void Publish(FetchCatsCommand command)
        {
            var factory = new ConnectionFactory()
            {
                HostName = _config["RabbitMQ:Host"],
                UserName = _config["RabbitMQ:Username"],
                Password = _config["RabbitMQ:Password"]
            };

            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(queue: _config["RabbitMQ:Queue"],
                                 durable: true,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);

            var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(command));

            var props = new BasicProperties();
            props.Persistent = true; // save messages

            await channel.BasicPublishAsync(exchange: "",
                                 routingKey: _config["RabbitMQ:Queue"],
                                 false,
                                 basicProperties: props,
                                 body: body);
        }
    }

}
