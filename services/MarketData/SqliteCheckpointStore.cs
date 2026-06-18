using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using QuantInfra.Common.Messaging.Patterns.DealerRouterWithReplay;

namespace QuantInfra.Services.MarketData;

internal sealed class SqliteCheckpointStore :
    IReceiverStateProvider,
    IDisposable
{
    private readonly SqliteConnection _conn;

    private readonly SqliteCommand _upsertCmd;
    private readonly SqliteParameter _pSenderCompId;
    private readonly SqliteParameter _pSessionId;
    private readonly SqliteParameter _pSequence;

    private readonly SqliteCommand _readCmd;
    private readonly SqliteParameter _pReadSenderCompId;

    private SqliteTransaction? _tx;
    private int _pending;
    private readonly int _commitEvery;

    public SqliteCheckpointStore(DisruptorMarketDataServiceConfig disruptorMarketDataServiceConfig)
    {
        _commitEvery = Math.Max(1, disruptorMarketDataServiceConfig.CheckpointDatabaseCommitInverval);

        var cs = new SqliteConnectionStringBuilder
        {
            DataSource = disruptorMarketDataServiceConfig.CheckpointDatabasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared, // OK for single-process, but not required
        }.ToString();

        _conn = new SqliteConnection(cs);
        _conn.Open();

        InitSchema(_conn);

        _tx = _conn.BeginTransaction();

        // UPSERT prepared command (fast)
        _upsertCmd = _conn.CreateCommand();
        _upsertCmd.Transaction = _tx;
        _upsertCmd.CommandText = """
            INSERT INTO checkpoint (sender_comp_id, session_id, sequence_number)
            VALUES ($sender_comp_id, $session_id, $sequence_number)
            ON CONFLICT(sender_comp_id) DO UPDATE SET
                session_id = excluded.session_id,
                sequence_number = excluded.sequence_number;
        """;
        
        _pSenderCompId = _upsertCmd.CreateParameter();
        _pSenderCompId.ParameterName = "$sender_comp_id";
        _upsertCmd.Parameters.Add(_pSenderCompId);

        _pSessionId = _upsertCmd.CreateParameter();
        _pSessionId.ParameterName = "$session_id";
        _upsertCmd.Parameters.Add(_pSessionId);
        
        _pSequence = _upsertCmd.CreateParameter();
        _pSequence.ParameterName = "$sequence_number";
        _upsertCmd.Parameters.Add(_pSequence);

        _upsertCmd.Prepare();

        // READ prepared command
        _readCmd = _conn.CreateCommand();
        _readCmd.CommandText = "SELECT sender_comp_id, session_id, sequence_number FROM checkpoint;";
        _readCmd.Prepare();
    }

    public ReceiverState GetReceiverState()
    {
        using var r = _readCmd.ExecuteReader();
        if (!r.HasRows)
        {
            return new ReceiverState();
        }

        var sessions = new Dictionary<string, UpstreamSession>();
        while (r.Read())
        {
            sessions.Add(r.GetString(0), new UpstreamSession(r.GetInt64(1), r.GetInt64(2)));
        }
        return new ReceiverState { Sessions = sessions };
    }
    
    
    /// <summary>
    /// Very hot path: just binds params and executes prepared statement.
    /// Must be called from the same thread (single connection, single writer).
    /// </summary>
    public void UpdateState(string senderCompId, long sessionId, long sequenceNumber)
    {
        _pSenderCompId.Value = senderCompId;
        _pSessionId.Value = sessionId;
        _pSequence.Value = sequenceNumber;

        _upsertCmd.ExecuteNonQuery();

        _pending++;
        if (_pending >= _commitEvery)
            Commit();
    }

    public void Commit()
    {
        if (_tx is null) return;

        _tx.Commit();
        _tx.Dispose();

        _pending = 0;
        _tx = _conn.BeginTransaction();

        // Keep command bound to the current tx.
        _upsertCmd.Transaction = _tx;
    }

    public void Dispose()
    {
        try { Commit(); } catch { /* best effort */ }
        _upsertCmd.Dispose();
        _readCmd.Dispose();
        _tx?.Dispose();
        _conn.Dispose();
    }

    private static void InitSchema(SqliteConnection conn)
    {
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = """

                PRAGMA journal_mode=WAL;
                PRAGMA synchronous=NORMAL;
                PRAGMA temp_store=MEMORY;
                PRAGMA cache_size=-20000;
                PRAGMA wal_autocheckpoint=1000;

            """;
            cmd.ExecuteNonQuery();
        }

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = """

                CREATE TABLE IF NOT EXISTS checkpoint (
                    sender_comp_id TEXT PRIMARY KEY,
                    session_id INTEGER NOT NULL,
                    sequence_number  INTEGER NOT NULL
                ) WITHOUT ROWID;

            """;
            cmd.ExecuteNonQuery();
        }
    }
}