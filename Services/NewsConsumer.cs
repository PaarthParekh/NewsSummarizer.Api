using Confluent.Kafka;
using System.Data.Common;

class NewsConsumer : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly KafkaSettings _ks;

    public NewsConsumer(IServiceProvider sp, KafkaSettings ks)
    {
        _sp = sp;
        _ks = ks;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(() =>
        {
            var cfg = new ConsumerConfig
            {
                BootstrapServers = _ks.BootstrapServers,
                GroupId = _ks.GroupId,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = true
            };

            using var consumer = new ConsumerBuilder<Ignore, string>(cfg).Build();
            consumer.Subscribe(_ks.Topic);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var cr = consumer.Consume(stoppingToken);
                    var url = cr.Message.Value;

                    using var scope = _sp.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<DbConnection>();
                    db.Open();

                    using var cmd = db.CreateCommand();
                    cmd.CommandText = "INSERT INTO articles (url) VALUES (@url)";
                    var p = cmd.CreateParameter(); p.ParameterName = "@url"; p.Value = url;
                    cmd.Parameters.Add(p);
                    cmd.ExecuteNonQuery();
                }
                catch (OperationCanceledException) { /* shutting down */ }
                catch (Exception ex)
                {
                    Console.WriteLine($"Consumer error: {ex.Message}");
                    Thread.Sleep(1000);
                }
            }

            consumer.Close();
        }, stoppingToken);
    }
}