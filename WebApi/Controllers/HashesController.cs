using System.Security.Cryptography;
using System.Text;
using Core.Context;
using Microsoft.AspNetCore.Mvc;
using RabbitMQ.Client;

namespace WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class HashesController : ControllerBase
{
    private readonly IModel _channel;
    private const int BatchSize = 1000;
    private readonly HashesDbContext _appDbContext;

    public HashesController(HashesDbContext db)
    {
        // Connect to RabbitMQ
        var factory = new ConnectionFactory {HostName = "localhost"};
        IConnection? connection = factory.CreateConnection();
        _channel = connection.CreateModel();
        _channel.QueueDeclare("hashes", false, false, false, null);
        _appDbContext = db;
    }

    [HttpPost(Name = "GenerateData")]
    public void Post()
    {
        // Generate 40,000 random SHA1 hashes
        var hashes = Enumerable.Range(0, 40_000)
            .Select(i =>
            {
                using var sha1 = SHA1.Create();
                var inputBytes = Encoding.ASCII.GetBytes(i.ToString());
                var hash = sha1.ComputeHash(inputBytes);
                return BitConverter.ToString(hash).Replace("-", "");
            });

        // Split hashes into batches
        var batches = hashes.Select((x, i) => new {Index = i, Value = x}).GroupBy(x => x.Index / BatchSize)
            .Select(x => x.Select(v => v.Value).ToList());

        // Send batches to RabbitMQ in parallel
        Parallel.ForEach(batches, batch =>
        {
            foreach (var hash in batch)
            {
                _channel.BasicPublish("", "hashes", null, Encoding.UTF8.GetBytes(hash));
            }
        });
    }

    [HttpGet(Name = "GetData")]
    public object Get()
    {
        // Retrieve hashes from database, group by day, and return in JSON format
        var hashes = _appDbContext.Hash
            .GroupBy(x => x.Date.Date)
            .Select(g => new {date = g.Key, count = g.Count()})
            .ToList();

        return new {hashes};
    }
}