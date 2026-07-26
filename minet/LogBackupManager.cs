using Microsoft.Data.Sqlite;
namespace Minet;
/// <summary>
/// ログバックアップを管理するクラス
/// </summary>
public static class LogBackupManager
{
    /// <summary>
    /// バックアップディレクトリのパス。現在の作業ディレクトリの "backup" フォルダ内に作成される。
    /// </summary>
    private static readonly string BackupDir = Path.Combine(Directory.GetCurrentDirectory(), "backup");

    /// <summary>
    /// 起動時にログファイルのバックアップを処理するメソッド。指定されたdbPathのログファイルが存在する場合、タイムスタンプ付きのバックアップファイルとしてbackupディレクトリに移動する。
    /// </summary>
    /// <param name="dbPath">バックアップ対象のデータベースファイルのパス</param>
    public static void HandleStartupBackup(string dbPath)
    {
        // バックアップディレクトリが存在しない場合は作成する
        EnsureBackupDirectoryExists();

        // dbPath が存在する場合、バックアップを作成する
        if (File.Exists(dbPath))
        {
            // バックアップファイル名をタイムスタンプ付きで生成する
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var backupFileName = $"{timestamp}.bk.db";
            var destinationPath = Path.Combine(BackupDir, backupFileName);
            SafeMoveFile(dbPath, destinationPath, "起動時異常終了用バックアップ");
        }
        else
        {
            // dbPath がディレクトリの中を指定している場合、ディレクトリまでのパスが存在しない場合は、ディレクトリを作成する。
            var directoryPath = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
                Console.WriteLine($"[Backup] ログディレクトリを作成しました: {directoryPath}");
            }
            else
            {
                Console.WriteLine("[Backup] 前回のログファイルは存在しません。");
            }
        }
    }

    // 終了時（CTRL+C等）の処理: 現在のDBを 日時.db として移動
    /// <summary>
    /// 終了時にログファイルのバックアップを処理するメソッド。
    /// 指定されたdbPathのログファイルが存在する場合、タイムスタンプ付きのバックアップファイルとしてbackupディレクトリに移動する。
    /// </summary>
    /// <param name="dbPath">バックアップ対象のデータベースファイルのパス</param>
    public static void HandleShutdownBackup(string dbPath)
    {
        EnsureBackupDirectoryExists();

        if (File.Exists(dbPath))
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var backupFileName = $"{timestamp}.db";
            var destinationPath = Path.Combine(BackupDir, backupFileName);
            SafeMoveFile(dbPath, destinationPath, "終了時バックアップ");
        }
    }

    /// <summary>
    /// バックアップディレクトリが存在しない場合に作成するメソッド。
    /// </summary>
    private static void EnsureBackupDirectoryExists()
    {
        if (!Directory.Exists(BackupDir))
        {
            Directory.CreateDirectory(BackupDir);
        }
    }

    /// <summary>
    /// 指定されたソースファイルを安全に移動するメソッド。
    /// SQLiteのコネクションプールをクリアしてロックを解除し、リトライ処理付きでMoveを試みる。
    /// </summary>
    /// <param name="sourcePath">移動元のファイルパス</param>
    /// <param name="destinationPath">移動先のファイルパス</param>
    /// <param name="backupType">バックアップの種類（起動時バックアップ、終了時バックアップなど）</param>
    private static void SafeMoveFile(string sourcePath, string destinationPath, string backupType)
    {
        try
        {
            // 1. SQLiteの全コネクションプールを明示的にクリアしてロックを解除
            SqliteConnection.ClearAllPools();

            // 2. リトライ処理付きでMove（MoveがダメならCopy & Delete）
            int retries = 5;
            while (retries > 0)
            {
                try
                {
                    File.Move(sourcePath, destinationPath, overwrite: true);
                    Console.WriteLine($"\n[Backup] {backupType}を完了しました: backup/{Path.GetFileName(destinationPath)}");
                    return;
                }
                catch (IOException)
                {
                    retries--;
                    if (retries == 0) throw;
                    Thread.Sleep(200); // 200ミリ秒待機して再試行
                }
            }
        }
        catch (Exception ex)
        {
            // 3. Move がどうしてもロックで失敗する場合は SafeCopy & Delete にフォールバック
            try
            {
                Console.WriteLine($"\n[Backup Warning] {backupType}で例外発生: {ex.Message}");
                File.Copy(sourcePath, destinationPath, overwrite: true);
                File.Delete(sourcePath);
                Console.WriteLine($"\n[Backup] {backupType}を完了しました(Copy経由): backup/{Path.GetFileName(destinationPath)}");
            }
            catch (Exception innerEx)
            {
                Console.Error.WriteLine($"\n[Backup Error] {backupType}失敗: {innerEx.Message}");
            }
        }
    }
}
