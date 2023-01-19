using System.Text;
using Core.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Processor;

public abstract class Processor
{
    private static readonly SemaphoreSlim Semaphore = new(4);

    private static Task Main()
    {
        // Connect to RabbitMQ
        var factory = new ConnectionFactory {HostName = "localhost"};
        IConnection? connection = factory.CreateConnection();
        IModel? channel = connection.CreateModel();
        channel.QueueDeclare("hashes", false, false, false, null);

        // Process messages in parallel using 4 threads
        var consumer = new EventingBasicConsumer(channel);
        using var context = new LocalContext();

        consumer.Received += async (sender, ea) =>
        {
            var hash = Encoding.UTF8.GetString(ea.Body.ToArray());
            await Semaphore.WaitAsync();
            Task.Run(() =>
            {
                try
                {
                    Console.WriteLine($"Thread: {Semaphore.CurrentCount} received {hash}");
                    ProcessHash(hash, context);
                    channel.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                finally
                {
                    Semaphore.Release();
                }
            });
        };


        channel.BasicConsume("hashes", false, consumer);
        Console.WriteLine(" Press [enter] to exit.");
        Console.ReadLine();
        return Task.CompletedTask;
    }

    private static void ProcessHash(string hash, LocalContext context)
    {
        var randomTest = new Random();

        TimeSpan timeSpan = DateTime.Now - DateTime.Now.AddMonths(-1);
        TimeSpan newSpan = new(0, randomTest.Next(0, (int) timeSpan.TotalMinutes), 0);
        DateTime randomDate = DateTime.Now.AddMonths(-1) + newSpan;
        context.Hash.Add(new Hash
        {
            Id = Guid.NewGuid(),
            Date = randomDate,
            Sha1 = hash
        });
        context.SaveChanges();
    }
}