using System.Net;
using Garnet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Minet;

// Minet: Garnet + SQLite ログ + 自動バックアップ

/// <summary>
/// Minetのエントリーポイントクラス。Garnetサーバーを起動し、SQLiteログのバックアップを管理する。
/// </summary>
string configFileName = "minet.json";

Console.WriteLine("Starting Minet on .NET 10 (SQLite Logging & Auto-Backup)...");
var configManager = new ConfigManager(configFileName);

// 1. 起動時チェック: すでに DB ファイルが存在していれば .bk.db に退避
LogBackupManager.HandleStartupBackup(configManager.DbPath);

var cts = new CancellationTokenSource();

// 2. CTRL+C / 終了イベントのハンドリング
Console.CancelKeyPress += (sender, e) =>
{
    Console.WriteLine("\nシャットダウンシグナルを受信しました (CTRL+C)...");
    e.Cancel = true;
    cts.Cancel();
};

try
{
    // 3. SQLiteロガーファクトリの構築
    using var loggerFactory = LoggerFactory.Create(builder =>
    {
        builder.AddProvider(new SqliteLoggerProvider(configManager.DbPath));
        builder.SetMinimumLevel(LogLevel.Information);
    });

    // 5. GarnetServer のコンストラクタ第2引数に loggerFactory を渡す
    using var server = new GarnetServer(configManager.options, loggerFactory);
    server.Start();

    Console.WriteLine($"Minet (Garnet) is running on {configManager.RunningAddress}");
    Console.WriteLine($"Logs DB: {configManager.DbPath}");
    Console.WriteLine("Press CTRL+C to stop.\n");

    await Task.Delay(Timeout.Infinite, cts.Token);
}
catch (TaskCanceledException)
{
    // CTRL+C による正常キャンセル
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Failed to run server: {ex.Message}");
}
finally
{
    // 6. クリーンアップとバックアップ
    GC.Collect();
    GC.WaitForPendingFinalizers();

    LogBackupManager.HandleShutdownBackup(configManager.DbPath);
    Console.WriteLine("Minet を終了しました。");
}
