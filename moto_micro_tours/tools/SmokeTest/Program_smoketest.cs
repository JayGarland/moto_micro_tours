// todo: write new smoke tests
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using MySqlConnector;

// Connects to the MySQL database
// Counts rows in all major tables
// Verifies unique indexes exist (TourMotards, PreBookingConversations, etc.)
// Tests the Message XOR constraint (ensures only one conversation ID is set)
// Checks Payment/Payout counts
// Validates seat capacity invariants (SeatsBooked ≤ CapacitySeats)

class Program
{
    static async Task<int> Main(string[] args)
    {
        // locate appsettings.json
        string? configPath = null;
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        for (int i = 0; i < 8 && dir != null; i++)
        {
            var candidate = Path.Combine(dir.FullName, "appsettings.json");
            if (File.Exists(candidate)) { configPath = candidate; break; }
            dir = dir.Parent;
        }
        if (configPath is null) throw new InvalidOperationException("Could not find appsettings.json");

        var json = await File.ReadAllTextAsync(configPath);
        using var doc = JsonDocument.Parse(json);
        var connStr = doc.RootElement.GetProperty("ConnectionStrings").GetProperty("DefaultConnection").GetString();
        if (string.IsNullOrEmpty(connStr)) throw new InvalidOperationException("DefaultConnection not found in appsettings.json");

        Console.WriteLine("Using connection: " + connStr);

        await using var conn = new MySqlConnection(connStr);
        await conn.OpenAsync();

        // Row counts
        var tables = new[] { "AspNetUsers", "Tours", "TourMotards", "PreBookingConversations", "Messages", "Bookings", "Payments", "Payouts", "Reviews" };
        Console.WriteLine("Row counts:");
        foreach (var t in tables)
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT COUNT(*) FROM `{t}`";
            var val = await cmd.ExecuteScalarAsync();
            Console.WriteLine($"{t}: {val}");
        }

        // Index checks
        Console.WriteLine("\nIndex checks:");
        string[] checkTables = { "TourMotards", "PreBookingConversations", "BookingConversations", "Payments", "Payouts" };
        foreach (var t in checkTables)
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT INDEX_NAME, GROUP_CONCAT(COLUMN_NAME ORDER BY SEQ_IN_INDEX) as cols, NON_UNIQUE FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME=@tbl GROUP BY INDEX_NAME";
            cmd.Parameters.AddWithValue("@tbl", t);
            await using var reader = await cmd.ExecuteReaderAsync();
            Console.WriteLine(t + " indexes:");
            while (await reader.ReadAsync())
            {
                Console.WriteLine($"{reader.GetString(0)}: {reader.GetString(1)} (non_unique={reader.GetInt32(2)})");
            }

        }

        // Additional explicit checks for Payments and Payouts
        Console.WriteLine("\n-- START Payments/Payouts index checks --");
        try
        {
            Console.WriteLine("Payments indexes:");
            await using (var cmdPayments = conn.CreateCommand())
            {
                cmdPayments.CommandText = @"SELECT INDEX_NAME, GROUP_CONCAT(COLUMN_NAME ORDER BY SEQ_IN_INDEX) as cols, NON_UNIQUE FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='Payments' GROUP BY INDEX_NAME";
                await using var readerPayments = await cmdPayments.ExecuteReaderAsync();
                while (await readerPayments.ReadAsync()) Console.WriteLine($"{readerPayments.GetString(0)}: {readerPayments.GetString(1)} (non_unique={readerPayments.GetInt32(2)})");
            }
            Console.WriteLine("\nPayouts indexes:");
            await using (var cmdPayouts = conn.CreateCommand())
            {
                cmdPayouts.CommandText = @"SELECT INDEX_NAME, GROUP_CONCAT(COLUMN_NAME ORDER BY SEQ_IN_INDEX) as cols, NON_UNIQUE FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='Payouts' GROUP BY INDEX_NAME";
                await using var readerPayouts = await cmdPayouts.ExecuteReaderAsync();
                while (await readerPayouts.ReadAsync()) Console.WriteLine($"{readerPayouts.GetString(0)}: {readerPayouts.GetString(1)} (non_unique={readerPayouts.GetInt32(2)})");
            }

        }
        catch (Exception ex)
        {
            Console.WriteLine("Error checking payments/payouts indexes: " + ex.Message);
        }

        // Check for CHECK constraints on the Messages table
        Console.WriteLine("\n-- START Message CHECK constraint check --");
        try
        {
            Console.WriteLine("Message table CHECK constraints:");
            await using (var cmdC = conn.CreateCommand())
            {
                cmdC.CommandText = @"SELECT CONSTRAINT_NAME, CHECK_CLAUSE FROM INFORMATION_SCHEMA.CHECK_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND CONSTRAINT_NAME IN (SELECT CONSTRAINT_NAME FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE TABLE_NAME='Messages')";
                await using var readerChecks = await cmdC.ExecuteReaderAsync();
                var foundChecks = false;
                while (await readerChecks.ReadAsync())
                {
                    foundChecks = true;
                    Console.WriteLine($"{readerChecks.GetString(0)}: {readerChecks.GetString(1)}");
                }
                if (!foundChecks) Console.WriteLine("No CHECK constraints found on Messages table.");
            }
        catch (Exception ex)
        {
            Console.WriteLine("Error checking message constraints: " + ex.Message);
        }
        Console.WriteLine("-- END Message CHECK constraint check --");

        // XOR test: try to insert invalid message with both conversation ids set
        Console.WriteLine("\nTesting Message XOR constraint by attempting an insert with both conversation IDs set (rolled back):");
        // Need to get one PreBookingConversation and one BookingConversation ids
        Guid? pbId = null, bcId = null;
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT Id FROM PreBookingConversations LIMIT 1";
            var r = await cmd.ExecuteScalarAsync();
            if (r != null) pbId = Guid.Parse(r.ToString()!);
        }
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT Id FROM BookingConversations LIMIT 1";
            var r = await cmd.ExecuteScalarAsync();
            if (r != null) bcId = Guid.Parse(r.ToString()!);
        }

        if (pbId == null || bcId == null)
        {
            Console.WriteLine("Not enough data to test XOR insert (missing records). Skipping.");
        }
        else
        {
            await using var tx = await conn.BeginTransactionAsync();
            try
            {
                await using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = @"INSERT INTO Messages (Id, PreBookingConversationId, BookingConversationId, SenderUserId, Content, SentAt) VALUES (@id, @pb, @bc, @suser, @content, @sent)";
                cmd.Parameters.AddWithValue("@id", Guid.NewGuid());
                cmd.Parameters.AddWithValue("@pb", pbId);
                cmd.Parameters.AddWithValue("@bc", bcId);
                cmd.Parameters.AddWithValue("@suser", "test@smoketest");
                cmd.Parameters.AddWithValue("@content", "XOR test");
                cmd.Parameters.AddWithValue("@sent", DateTime.UtcNow);
                try
                {
                    var affected = await cmd.ExecuteNonQueryAsync();
                    Console.WriteLine("Insert succeeded (DB did not reject XOR). Rows affected: " + affected);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Insert failed as expected with exception: " + ex.Message);
                }
                await tx.RollbackAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error during XOR test: " + ex.Message);
            }
        }

        // Payments/payouts and seats check
        Console.WriteLine("\nChecking payments/payouts counts and seat invariants:");
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT COUNT(*) FROM Payments";
            Console.WriteLine("Payments: " + await cmd.ExecuteScalarAsync());
        }
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT COUNT(*) FROM Payouts";
            Console.WriteLine("Payouts: " + await cmd.ExecuteScalarAsync());
        }

        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT Id, SeatsBooked, CapacitySeats FROM TourMotards WHERE SeatsBooked > CapacitySeats";
            await using var reader = await cmd.ExecuteReaderAsync();
            var found = false;
            while (await reader.ReadAsync())
            {
                found = true;
                Console.WriteLine($"Inconsistent TourMotard: Id={reader.GetGuid(0)}, SeatsBooked={reader.GetInt32(1)}, CapacitySeats={reader.GetInt32(2)}");
            }
            if (!found) Console.WriteLine("All TourMotard seats are within capacity.");
        }

        Console.WriteLine("\nSmoke tests complete.");
        await conn.CloseAsync();
        return 0;
    }
}
}
