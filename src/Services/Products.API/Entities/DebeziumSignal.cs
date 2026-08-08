namespace Products.API.Entities;

/// <summary>
/// Debezium's signal table. Inserting a row here is how the connector is asked to do
/// something — this codebase uses it for on-demand incremental snapshots.
/// </summary>
/// <remarks>
/// The schema is dictated by Debezium (<c>id</c>, <c>type</c>, <c>data</c>) and must not be
/// changed. The table is listed in <c>debezium.source.table.include.list</c> because the
/// connector only reads signals from a table it is already capturing.
/// <para>
/// This is the backfill path for history that has aged out of the CDC stream's retention
/// window: it re-emits every row of a table into the log without dropping the replication
/// slot, so unlike the old "delete the volume and re-snapshot" procedure it does not disturb
/// consumers that are already up to date.
/// </para>
/// <para>
/// Rows are written once and never read by this service; the connector consumes them.
/// </para>
/// </remarks>
public class DebeziumSignal
{
    /// <summary>Arbitrary unique id. Debezium fixes the width at 42 characters.</summary>
    public required string Id { get; set; }

    /// <summary>Signal type, e.g. <c>execute-snapshot</c>.</summary>
    public required string Type { get; set; }

    /// <summary>JSON payload whose shape depends on <see cref="Type"/>.</summary>
    public string? Data { get; set; }
}
