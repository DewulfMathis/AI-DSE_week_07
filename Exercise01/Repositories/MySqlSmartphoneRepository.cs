using System.Data.Common;
using Microsoft.VisualBasic;

public class MySqlSmartphoneRepository : ISmartphoneRepository
{
    private MySqlDataSource _datasource;
    public MySqlSmartphoneRepository(MySqlDataSource dataSource)
    {
        _datasource = dataSource;
    }

    // Smartphones added with AddSmartphone but not yet written to the database.
    private readonly List<Smartphone> _pending = [];

    // Synchronous, like the interface asks: it only remembers the smartphone.
    // The INSERT happens in SaveChangesAsync (same pattern as the CSV repository).
    public void AddSmartphone(Smartphone smartphone)
    {
        _pending.Add(smartphone);
    }

    private const string SelectColumns =
        "SELECT id, brand, type, release_year, start_price, operating_system FROM smartphones";

    public async Task<Smartphone?> GetSmartphoneByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _datasource.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = $"{SelectColumns} WHERE id = @id;";
        command.Parameters.AddWithValue("@id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        // No row with that id: null, like FirstOrDefault in the other repositories.
        return await reader.ReadAsync(cancellationToken) ? MapSmartphone(reader) : null;
    }

    public async Task<List<Smartphone>> GetSmartphonesAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _datasource.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = $"{SelectColumns} ORDER BY id;";

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var smartphones = new List<Smartphone>();
        while (await reader.ReadAsync(cancellationToken))
        {
            smartphones.Add(MapSmartphone(reader));
        }
        return smartphones;
    }

    // One row -> one Smartphone. The column names are the snake_case names of the table.
    private static Smartphone MapSmartphone(MySqlDataReader reader)
    {
        return new Smartphone
        {
            Id = reader.GetInt32("id"),
            Brand = reader.GetString("brand"),
            Type = reader.GetString("type"),
            ReleaseYear = reader.GetInt16("release_year"),
            StartPrice = reader.GetDecimal("start_price"),
            OperatingSystem = reader.GetString("operating_system")
        };
    }

    public Task ImportSmartphones()
    {
        throw new NotImplementedException();
    }

    public async Task ImportSmartphones(List<Smartphone> smartphones, CancellationToken cancellationToken)
    {
        await using var connection = await _datasource.OpenConnectionAsync(cancellationToken);

        // All rows or none: if one insert fails (or the user cancels), nothing stays half-imported.
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            // One command, reused for every row. Only the parameter values change.
            // Parameters instead of string concatenation: no SQL injection, no quoting/decimal-separator issues.
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText =
                """
                INSERT INTO smartphones (brand, type, release_year, start_price, operating_system)
                VALUES (@brand, @type, @releaseYear, @startPrice, @operatingSystem);
                """;

            var brand = command.Parameters.Add("@brand", MySqlDbType.VarChar);
            var type = command.Parameters.Add("@type", MySqlDbType.VarChar);
            var releaseYear = command.Parameters.Add("@releaseYear", MySqlDbType.Int16);
            var startPrice = command.Parameters.Add("@startPrice", MySqlDbType.Decimal);
            var operatingSystem = command.Parameters.Add("@operatingSystem", MySqlDbType.VarChar);

            await command.PrepareAsync(cancellationToken);

            foreach (var smartphone in smartphones)
            {
                brand.Value = smartphone.Brand;
                type.Value = smartphone.Type;
                releaseYear.Value = smartphone.ReleaseYear;
                startPrice.Value = smartphone.StartPrice;
                operatingSystem.Value = smartphone.OperatingSystem;

                await command.ExecuteNonQueryAsync(cancellationToken);

                // id is AUTO_INCREMENT: give the object the id MySQL chose.
                smartphone.Id = (int)command.LastInsertedId;
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            // CancellationToken.None: the rollback must still run when the import was cancelled.
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    // Writes all pending smartphones in one transaction: all of them or none.
    // ImportSmartphones already does exactly that (and sets the Id MySQL chose), so it is reused.
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (_pending.Count == 0)
        {
            return;
        }

        await ImportSmartphones(_pending, cancellationToken);

        // Only cleared after a successful commit: after a failure they can be saved again.
        _pending.Clear();
    }

    // One UPDATE statement: a single statement is atomic by itself, so no explicit transaction is needed.
    public async Task<bool> UpdatePriceAsync(int id, decimal newPrice, CancellationToken cancellationToken = default)
    {
        await using var connection = await _datasource.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE smartphones SET start_price = @startPrice WHERE id = @id;";
        command.Parameters.AddWithValue("@startPrice", newPrice);
        command.Parameters.AddWithValue("@id", id);

        // MySqlConnector counts the rows that were found (not only the ones that changed),
        // so setting the same price again still returns true. 0 rows = no smartphone with that id.
        int rows = await command.ExecuteNonQueryAsync(cancellationToken);
        return rows > 0;
    }

    // One DELETE statement: atomic by itself, no explicit transaction needed.
    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _datasource.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM smartphones WHERE id = @id;";
        command.Parameters.AddWithValue("@id", id);

        // 0 rows deleted = there was no smartphone with that id.
        int rows = await command.ExecuteNonQueryAsync(cancellationToken);
        return rows > 0;
    }
}