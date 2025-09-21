using System.Data.Common;

class DbInitializer : BackgroundService
{
    private readonly IServiceProvider _sp;
    public DbInitializer(IServiceProvider sp) => _sp = sp;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DbConnection>();
        await db.OpenAsync(stoppingToken);

        var sql = @"
CREATE TABLE IF NOT EXISTS articles (
  id BIGINT PRIMARY KEY AUTO_INCREMENT,
  url VARCHAR(1024) NOT NULL,
  title VARCHAR(512) NULL,
  summary TEXT NULL,
  embedding VARBINARY(8192) NULL,
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";

        await using var cmd = db.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync(stoppingToken);
    }
}