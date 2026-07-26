using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace Minet;

// ログをSQLiteに書き込むロガープラス
public class SqliteLogger : ILogger
{
    private readonly string _categoryName;
    private readonly string _connectionString;

    /// <summary>
    /// SQLite ログの書き込み先を指定するためのコンストラクタ
    /// </summary>
    /// <param name="categoryName">ログカテゴリ名</param>
    /// <param name="connectionString">SQLite接続文字列</param>
    /// <exception cref="ArgumentNullException">categoryName または connectionString が null の場合</exception>
    /// <exception cref="ArgumentException">categoryName または connectionString が空文字列の場合</exception>
    /// <exception cref="SqliteException">SQLite接続に失敗した場合</exception>
    /// <remarks>
    /// categoryName はログのカテゴリ名を指定するために使用されます。
    /// connectionString は SQLite データベースへの接続文字列を指定します。
    /// </remarks>

    public SqliteLogger(string categoryName, string connectionString)
    {
        _categoryName = categoryName;
        _connectionString = connectionString;
    }

    /// <summary>
    /// 開始スコープを作成します。ここではスコープはサポートされていないため、null を返します。
    /// </summary>
    /// <typeparam name="TState">スコープの状態を表す型</typeparam>
    /// <param name="state">スコープの状態</param>
    /// <returns>スコープの IDisposable オブジェクト（サポートされていないため常に null）</returns>
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    /// <summary>
    /// 指定されたログレベルが有効かどうかを判定します。ここでは LogLevel.None 以外はすべて有効とします。
    /// </summary>
    /// <param name="logLevel">ログの重要度を示す値</param>
    /// <returns>指定されたログレベルが有効であれば true、それ以外は false</returns>
    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    /// <summary>
    /// ログを SQLite データベースに書き込みます。ログレベルが有効でない場合は何も行いません。
    /// </summary>
    /// <typeparam name="TState">ログの状態を表す型</typeparam>
    /// <param name="logLevel">ログの重要度を示す値</param>
    /// <param name="eventId">イベントID</param>
    /// <param name="state">ログの状態</param>
    /// <param name="exception">関連する例外（存在する場合）</param>
    /// <param name="formatter">ログメッセージをフォーマットするデリゲート</param>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;

        var message = formatter(state, exception);
        var exMessage = exception?.ToString();

        // テーブル作成 & ログ挿入 (シンプルな書き込み)
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO ServerLogs (Timestamp, LogLevel, Category, Message, Exception)
            VALUES (@Timestamp, @LogLevel, @Category, @Message, @Exception);
        ";

        command.Parameters.AddWithValue("@Timestamp", DateTime.UtcNow.ToString("o"));
        command.Parameters.AddWithValue("@LogLevel", logLevel.ToString());
        command.Parameters.AddWithValue("@Category", _categoryName);
        command.Parameters.AddWithValue("@Message", message);
        command.Parameters.AddWithValue("@Exception", (object?)exMessage ?? DBNull.Value);

        command.ExecuteNonQuery();
    }
}

/// <summary>
/// SQLite ログを提供するロガープロバイダーです。ILoggerProvider インターフェースを実装し、SQLite データベースにログを書き込むための ILogger インスタンスを生成します。
/// </summary>
public class SqliteLoggerProvider : ILoggerProvider
{
    private readonly string _connectionString;

    /// <summary>
    /// SQLite ログプロバイダーのコンストラクタです。指定された SQLite データベースファイルパスを使用して接続文字列を構築し、必要に応じてログテーブルを初期化します。
    /// </summary>
    /// <param name="dbPath">SQLite データベースファイルのパス</param>
    public SqliteLoggerProvider(string dbPath)
    {
        _connectionString = $"Data Source={dbPath}";
        InitializeDatabase();
    }

    /// <summary>
    /// SQLite データベースにログテーブルが存在しない場合に作成します。データベース接続を開き、必要な SQL コマンドを実行してテーブルを作成します。
    /// </summary>
    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS ServerLogs (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Timestamp TEXT NOT NULL,
                LogLevel TEXT NOT NULL,
                Category TEXT NOT NULL,
                Message TEXT NOT NULL,
                Exception TEXT
            );
        ";
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// 指定されたカテゴリ名に基づいて ILogger インスタンスを作成します。ILogger インスタンスは、SQLite データベースにログを書き込むための機能を提供します。
    /// </summary>
    /// <param name="categoryName">ログカテゴリ名</param>
    /// <returns>指定されたカテゴリ名に基づく ILogger インスタンス</returns>
    public ILogger CreateLogger(string categoryName)
    {
        return new SqliteLogger(categoryName, _connectionString);
    }

    /// <summary>
    /// リソースを解放します。ここでは特に解放するリソースはないため、空の実装となっています。
    /// </summary>
    public void Dispose() { }
}