using System.Net;
using Garnet.server;
using Microsoft.Extensions.Configuration;

namespace Minet;

/// <summary>
/// minet.jsonをGarnetServerOptionsに変換するためのクラス
/// </summary>
/// <remarks>
/// minet.jsonのGarnetセクションの設定を読み込み、GarnetServerOptionsのプロパティやフィールドに反映させる。
/// </remarks>
/// <example>
/// minet.jsonの例:
/// <code>
/// {
///   "Garnet": {
///    "Address": "127.0.0.1",
///    "Port": 6379
///   },
///   "SqliteLogPath": "minet_logs.db"
/// }
/// </code>
/// </example>
public class ConfigManager
{
    /// <summary>
    /// デバッグモードかどうかを示すフラグ。trueの場合、設定の読み込み時に詳細な情報をコンソールに出力する。
    /// </summary>
    private bool isDebug = false;

    #region Properties

    /// <summary>
    /// minet.jsonの設定を保持するIConfigurationRoot
    /// </summary>
    public Microsoft.Extensions.Configuration.IConfigurationRoot config { get; set; }

    /// <summary>
    /// GarnetServerOptionsのインスタンス
    /// </summary>
    public GarnetServerOptions options { get; set; }

    /// <summary>
    /// Garnetセクションの設定を取得するためのIConfigurationSection
    /// </summary>
    public IConfigurationSection Garnet => config.GetSection("Garnet");

    /// <summary>
    /// GarnetセクションのAddress設定を取得する。デフォルトは"127.0.0.1"
    /// </summary>
    public string AddressStr => Garnet["Address"] ?? "127.0.0.1";

    /// <summary>
    /// GarnetセクションのPort設定を取得する。デフォルトは6379
    /// </summary>
    public int Port => int.Parse(Garnet["Port"] ?? "6379");

    /// <summary>
    /// minet.jsonのSqliteLogPath設定を取得する。デフォルトは"minet_logs.db"
    /// </summary>
    public string DbPath => config["SqliteLogPath"] ?? "minet_logs.db";

    /// <summary>
    /// Garnetサーバーの実行アドレスを取得する。形式は"Address:Port"。例: "127.0.0.1:6379"
    /// </summary>
    public string RunningAddress => $"{AddressStr}:{Port}";

    #endregion Properties

    public ConfigManager(string configFileName)
    {
        config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(configFileName, optional: false, reloadOnChange: true)
            .Build();

        options = new GarnetServerOptions();
        Garnet.Bind(options);
        Console.WriteLine($"*** update config parameters ***");
        Console.WriteLine(options.ToString());
        // optionsのプロパティとフィールドをGarnetセクションの設定で上書きする
        foreach (var kv in Garnet.AsEnumerable())
        {
            if (isDebug) Console.WriteLine($"{kv.Key}={kv.Value}");
            var key = kv.Key.Split(':').Last(); // "Garnet:Address" -> "Address"
            var prop = options.GetType().GetProperties().FirstOrDefault(p => p.Name == key);
            if (prop != null)
            {
                prop.SetValue(options, Convert.ChangeType(kv.Value, prop.PropertyType));
                Console.WriteLine($"Set property {prop.Name} to {prop.GetValue(options)}");
            }
            else
            {
                var field = options.GetType().GetFields().FirstOrDefault(f => f.Name == key);
                if (field != null)
                {
                    field.SetValue(options, Convert.ChangeType(kv.Value, field.FieldType));
                    Console.WriteLine($"Set field {field.Name} to {field.GetValue(options)}");
                }
            }
        }
        if (isDebug)
        {
            // GarnetServerOptionsのプロパティとフィールドをすべて出力する
            Console.WriteLine($"*** GarnetServerOptions properties ***");
            Console.WriteLine(options.GetType().GetProperties().OrderBy(p => p.Name).Aggregate("", (s, p) => s + $"{p.Name}: {p.GetValue(options)}\n"));
            Console.WriteLine($"*** GarnetServerOptions fields ***");
            Console.WriteLine(options.GetType().GetFields().OrderBy(f => f.Name).Aggregate("", (s, f) => s + $"{f.Name}: {f.GetValue(options)}\n"));
        }

        var ip = IPAddress.Parse(AddressStr);
        options.EndPoints = new[] { new IPEndPoint(ip, Port) };
    }

}