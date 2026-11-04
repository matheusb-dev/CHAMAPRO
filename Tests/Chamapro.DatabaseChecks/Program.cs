using System.Text.Json;
using Chamapro.Api.Data;
using Chamapro.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Npgsql;
using Npgsql.NameTranslation;

var secretsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "Microsoft", "UserSecrets", "chamapro-api-dev", "secrets.json");
using var secrets = JsonDocument.Parse(File.ReadAllText(secretsPath));
var connectionString = secrets.RootElement.GetProperty("ConnectionStrings:DefaultConnection").GetString()!;
var cs = new NpgsqlConnectionStringBuilder(connectionString);
cs.Options = "-c default_transaction_read_only=on";
connectionString = cs.ConnectionString;
if (cs.Host != "localhost" || cs.Port != 5432 || cs.Database != "chamapro_local")
    throw new InvalidOperationException("Este verificador somente lê chamapro_local em localhost:5432.");
var translator = new NpgsqlNullNameTranslator();
var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connectionString, pg =>
{
    pg.MapEnum<TipoUsuario>("tipo_usuario", nameTranslator: translator);
    pg.MapEnum<StatusPedido>("status_pedido", nameTranslator: translator);
    pg.MapEnum<StatusAgendamento>("status_agendamento", nameTranslator: translator);
    pg.MapEnum<StatusVerificacao>("status_verificacao", nameTranslator: translator);
}).Options;
using var db = new AppDbContext(options);
await using var connection = new NpgsqlConnection(connectionString);
await connection.OpenAsync();
string Quote(string name) => "\"" + name.Replace("\"", "\"\"") + "\"";
async Task<string> Scalar(string sql)
{
    await using var cmd = new NpgsqlCommand(sql, connection);
    return Convert.ToString(await cmd.ExecuteScalarAsync())!;
}
var count = 0;
foreach (var entity in db.Model.GetEntityTypes())
{
    var table = entity.GetTableName()!;
    var schema = entity.GetSchema() ?? "public";
    var store = StoreObjectIdentifier.Table(table, entity.GetSchema());
    var columns = entity.GetProperties().Select(p => Quote(p.GetColumnName(store)!));
    // O servidor valida nomes e tipos das colunas usadas pelo modelo sem ler registros privados.
    await using (var cmd = new NpgsqlCommand($"SELECT {string.Join(',', columns)} FROM {Quote(schema)}.{Quote(table)} LIMIT 0", connection))
    await using (var reader = await cmd.ExecuteReaderAsync()) { }
    var expectedPk = string.Join(',', entity.FindPrimaryKey()!.Properties.Select(p => p.GetColumnName(store)));
    var actualPk = await Scalar($"SELECT string_agg(a.attname, ',' ORDER BY k.ord) FROM pg_constraint c CROSS JOIN LATERAL unnest(c.conkey) WITH ORDINALITY k(num,ord) JOIN pg_attribute a ON a.attrelid=c.conrelid AND a.attnum=k.num WHERE c.contype='p' AND c.conrelid='{schema}.{table}'::regclass");
    if (actualPk != expectedPk) throw new Exception($"PK divergente: {table}");
    foreach (var fk in entity.GetForeignKeys())
    {
        var principal = fk.PrincipalEntityType;
        var principalStore = StoreObjectIdentifier.Table(principal.GetTableName()!, principal.GetSchema());
        var localColumns = string.Join(',', fk.Properties.Select(p => p.GetColumnName(store)));
        var remoteColumns = string.Join(',', fk.PrincipalKey.Properties.Select(p => p.GetColumnName(principalStore)));
        var exists = await Scalar($"SELECT count(*) FROM pg_constraint c WHERE c.contype='f' AND c.conrelid='{schema}.{table}'::regclass AND c.confrelid='{principal.GetSchema() ?? "public"}.{principal.GetTableName()}'::regclass AND (SELECT string_agg(a.attname,',' ORDER BY k.ord) FROM unnest(c.conkey) WITH ORDINALITY k(num,ord) JOIN pg_attribute a ON a.attrelid=c.conrelid AND a.attnum=k.num)='{localColumns}' AND (SELECT string_agg(a.attname,',' ORDER BY k.ord) FROM unnest(c.confkey) WITH ORDINALITY k(num,ord) JOIN pg_attribute a ON a.attrelid=c.confrelid AND a.attnum=k.num)='{remoteColumns}'");
        if (exists == "0") throw new Exception($"FK ausente: {table}.{localColumns}");
    }
    var rows = await Scalar($"SELECT count(*) FROM {Quote(schema)}.{Quote(table)}");
    Console.WriteLine($"PASS {table}: colunas, PK, FKs; {rows} registros (somente leitura)");
    count++;
}
foreach (var (name, type) in new[] { ("tipo_usuario", typeof(TipoUsuario)), ("status_pedido", typeof(StatusPedido)), ("status_agendamento", typeof(StatusAgendamento)), ("status_verificacao", typeof(StatusVerificacao)) })
{
    var labels = (await Scalar($"SELECT string_agg(e.enumlabel,',' ORDER BY e.enumlabel) FROM pg_enum e JOIN pg_type t ON t.oid=e.enumtypid WHERE t.typname='{name}'")).Split(',');
    if (!labels.Order().SequenceEqual(Enum.GetNames(type).Order())) throw new Exception($"Enum divergente: {name}");
    Console.WriteLine($"PASS enum {name}");
}
Console.WriteLine($"PASS {count} entidades compatíveis com o catálogo PostgreSQL; consulta somente leitura, sem exigir base vazia.");
