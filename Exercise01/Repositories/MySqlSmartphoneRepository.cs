using System.Data.Common;
using Microsoft.VisualBasic;

public class MySqlSmartphoneRepository : ISmartphoneRepository
{
    private MySqlDataSource _datasource;
    public MySqlSmartphoneRepository(MySqlDataSource dataSource)
    {
        _datasource = dataSource;
    }

    public void AddSmartphone(Smartphone smartphone)
    {
        throw new NotImplementedException();
    }

    public Task<Smartphone?> GetSmartphoneByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<List<Smartphone>> GetSmartphonesAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
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

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}