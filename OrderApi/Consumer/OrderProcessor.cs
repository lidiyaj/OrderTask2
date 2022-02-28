using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OrderApi.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OrderApi.Consumer
{
    public class OrderProcessor : BackgroundService
    {
        private readonly ILogger _logger;
        private IConnection _connection;
        private IModel _channel;

        private readonly IServiceScopeFactory _serviceScopeFactory;

        public OrderProcessor(ILoggerFactory loggerFactory, IServiceScopeFactory serviceScopeFactory)
        {
            this._logger = loggerFactory.CreateLogger<OrderProcessor>();
            _serviceScopeFactory = serviceScopeFactory;

            InitRabbitMQ();
        }

        private void InitRabbitMQ()
        {
            var factory = new ConnectionFactory
            {
                HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST"),
                Port = Convert.ToInt32(Environment.GetEnvironmentVariable("RABBITMQ_PORT"))
            };

            // create connection  
            _connection = factory.CreateConnection();

            // create channel  
            _channel = _connection.CreateModel();

            _channel.QueueDeclare("orders", true, false, false, null);

            _connection.ConnectionShutdown += RabbitMQ_ConnectionShutdown;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (ch, ea) =>
            {
                // received message  
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                // handle the received message  
                HandleMessageAsync(message);
                _channel.BasicAck(ea.DeliveryTag, false);
            };

            consumer.Shutdown += OnConsumerShutdown;
            consumer.Registered += OnConsumerRegistered;
            consumer.Unregistered += OnConsumerUnregistered;
            consumer.ConsumerCancelled += OnConsumerConsumerCancelled;

            _channel.BasicConsume("orders", false, consumer);
            return Task.CompletedTask;
        }

        private async Task HandleMessageAsync(string message)
        {
            // we just print this message   
            _logger.LogInformation($"consumer received {message}");

            CartOrder orderMsg = JsonConvert.DeserializeObject<CartOrder>(message);

            _logger.LogInformation($"after received {orderMsg.OrderID}");
            _logger.LogInformation($"Processing order... {orderMsg.OrderID} ");

            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var _context = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

                var order = await _context.Orders.FindAsync(orderMsg.OrderID);
                if (order != null)
                {
                    _logger.LogInformation("Order with OrderId: " + orderMsg.OrderID + " exists.");
                }
                else
                {
                    Order neworder = new()
                    {
                        OrderStatus = nameof(OrderStatus.INITIATED),
                        CustomerID = orderMsg.CustomerID
                    };

                    _context.Orders.Add(neworder);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Order with OrderId: " + neworder.OrderID + " created.");

                    foreach (var product in orderMsg.Products)
                    {
                        var orderItem = new OrderItem
                        {
                            OrderID = order.OrderID,
                            ProductID = product.ProductID,
                            Quantity = product.Quantity,
                            ProductPrice = product.ProductPrice,
                            Total = product.Total
                        };

                        _context.OrderItems.Add(orderItem);
                    }
                    await _context.SaveChangesAsync();
                }
            }
        }

        private void OnConsumerConsumerCancelled(object sender, ConsumerEventArgs e) { }
        private void OnConsumerUnregistered(object sender, ConsumerEventArgs e) { }
        private void OnConsumerRegistered(object sender, ConsumerEventArgs e) { }
        private void OnConsumerShutdown(object sender, ShutdownEventArgs e) { }
        private void RabbitMQ_ConnectionShutdown(object sender, ShutdownEventArgs e) { }

        public override void Dispose()
        {
            _channel.Close();
            _connection.Close();
            base.Dispose();
        }
    }
}
