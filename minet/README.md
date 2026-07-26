# minet - Garnet + SQLiteログ + 自動バックアップ

## ライセンス

minet本体は [MIT License](../LICENSE) で公開しています。NuGet依存関係を含む
配布物には、[第三者ライセンス通知](../THIRD-PARTY-NOTICES.md) が同梱されます。

## 概要

minetは、Microsoft Garnetキーバリューストアをベースとしたローカルサーバーです。
以下の拡張機能を備えています：

- **SQLiteログ**: すべてのGarnet操作をSQLiteデータベースに記録
- **自動バックアップ**: 起動時・終了時に自動的にデータをバックアップ
- **AOF (Append Only File)**: 永続性のあるデータ書き込み
- **JSON設定ファイル**: `minet.json`による柔軟な設定管理

## 使い方

### ビルド

```bash
dotnet build
```

### 実行

```bash
dotnet run
```

### リリースビルドと実行

```bash
dotnet publish -c Release
./bin/Release/minet/publish/minet
```

### 終了

Ctrl+Cを押してGarnetサーバーを停止します。終了時に自動的にバックアップが作成されます。

## プロジェクト構造

```
minet/
├── minet.csproj              # プロジェクトファイル
├── Program.cs                # エントリーポイント
├── ConfigManager.cs          # 設定管理クラス（minet.json → GarnetServerOptions）
├── LogBackupManager.cs       # ログバックアップ管理クラス（起動時/終了時）
├── SqliteLogger.cs           # SQLiteロガープロバイダ
├── minet.json                # 設定ファイル
├── README.md                 # このREADMEファイル
├── data/                     # Garnetのチェックポイントデータ
├── garnet_data/              # Garnet追加データ
├── logs/                     # SQLiteログデータベース
│   └── minet_logs.db
├── backup/                   # バックアップディレクトリ
│   └── {timestamp}.bk.db     # 起動時バックアップ
│   └── {timestamp}.db        # 終了時バックアップ
└── scripts/                  # ユーティリティスクリプト
```

## 設定ファイル

`minet.json` でサーバーの設定を変更できます：

```json
{
  "Garnet": {
    "Port": 6379,
    "Address": "127.0.0.1",
    "ThreadPoolMinThreads": 16,
    "EnableAOF": true,
    "CheckpointDir": "data",
    "CommitFrequencyMs": 0,
    "PageSizeBits": 27,
    "Recover": true
  },
  "SqliteLogPath": "logs/minet_logs.db"
}
```

### 設定オプション

| キー | 説明 | デフォルト |
|-----|------|-----------|
| `Garnet:Port` |  listening ポート番号 | `6379` |
| `Garnet:Address` | リッスンするアドレス | `127.0.0.1` |
| `Garnet:ThreadPoolMinThreads` | スレッドプールの最小スレッド数 | `16` |
| `Garnet:EnableAOF` | AOF（Append Only File）を有効にする | `true` |
| `Garnet:CheckpointDir` | チェックポイントデータの保存先 | `data` |
| `Garnet:CommitFrequencyMs` | コミット頻度（ミリ秒） | `0` |
| `Garnet:PageSizeBits` | ページサイズビット数 | `27` |
| `Garnet:Recover` | 起動時のリカバリを有効にする | `true` |
| `SqliteLogPath` | SQLiteログデータベースのパス | `logs/minet_logs.db` |

## バックアップ機能

minetは2つのタイミングで自動的にバックアップを作成します：

### 起動時バックアップ
- 既存のSQLiteデータベースファイルが存在する場合、`backup/` ディレクトリに `{timestamp}.bk.db` として退避
- 前回の異常終了に対応するため

### 終了時バックアップ
- Ctrl+C等で正常終了する際、`backup/` ディレクトリに `{timestamp}.db` として保存
- データベースの最新状態を保持

バックアップはファイルロック対策としてリトライ処理とフォールバック機構を持っています。

## ログデータベース

SQLiteには以下の構造でログが記録されます：

```sql
CREATE TABLE ServerLogs (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Timestamp TEXT NOT NULL,
    LogLevel TEXT NOT NULL,
    Category TEXT NOT NULL,
    Message TEXT NOT NULL,
    Exception TEXT
);
```

## 接続方法

minetで起動したGarnetサーバーには、以下の接続情報でアクセスできます：

```
Host: 127.0.0.1
Port: 6379
```

### C# (StackExchange.Redis) の場合

```csharp
using StackExchange.Redis;

var redis = ConnectionMultiplexer.Connect("127.0.0.1:6379");
var db = redis.GetDatabase();

// 値の保存
await db.StringSetAsync("key", "value");

// 値の取得
var value = await db.StringGetAsync("key");
Console.WriteLine(value); // value
```

### TypeScript (ioredis) の場合

```typescript
import { Redis } from 'ioredis';

const redis = new Redis({
  host: '127.0.0.1',
  port: 6379,
});

await redis.set('key', 'value');
const value = await redis.get('key');
console.log(value); // value
```

### curl の場合

```bash
# SETコマンド
echo -e "SET key value" | nc 127.0.0.1 6379

# GETコマンド
echo -e "GET key" | nc 127.0.0.1 6379
```

## 技術スタック

- **.NET 10.0** (net10.0)
- **Microsoft.Garnet** - キーバリューストアエンジン
- **Microsoft.Data.Sqlite** - SQLiteデータベース
- **Microsoft.Extensions.Configuration** - JSON設定読み込み

## LICENSE

MIT