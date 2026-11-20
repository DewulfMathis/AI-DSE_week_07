namespace Exercise01.Repositories;

public class CsvSmartphoneRepository : ISmartphoneRepository
{
    private const string Header = "Id,brand,type,release year,start price,operating system";

    private readonly string _path;
    private List<Smartphone>? _smartphones; // loaded on first use, written by SaveChangesAsync

    // A constructor cannot be async, so it does not read the file. It only remembers the path;
    // LoadAsync reads the file the first time data is asked for (section 5.6).
    public CsvSmartphoneRepository(string path)
    {
        _path = path;
    }

    public async Task<List<Smartphone>> GetSmartphonesAsync(CancellationToken cancellationToken = default)
    {
        _smartphones ??= await LoadAsync(cancellationToken);
        return _smartphones;
    }

    public async Task<Smartphone?> GetSmartphoneByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        List<Smartphone> smartphones = await GetSmartphonesAsync(cancellationToken);
        return smartphones.FirstOrDefault(s => s.Id == id);
    }

    // Synchronous, so it cannot load the file itself: the data has to be loaded already.
    // SmartphoneService.AddSmartphoneAsync makes sure of that by asking for the list first.
    public void AddSmartphone(Smartphone smartphone)
    {
        if (_smartphones == null)
        {
            throw new InvalidOperationException("Load the smartphones with GetSmartphonesAsync before adding one.");
        }

        smartphone.Id = _smartphones.Count == 0 ? 1 : _smartphones.Max(s => s.Id) + 1;
        _smartphones.Add(smartphone);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var lines = new List<string> { Header };
        foreach (var s in await GetSmartphonesAsync(cancellationToken))
        {
            // InvariantCulture: always a dot as decimal separator in the file
            lines.Add(string.Join(",",
                s.Id.ToString(CultureInfo.InvariantCulture),
                s.Brand,
                s.Type,
                s.ReleaseYear.ToString(CultureInfo.InvariantCulture),
                s.StartPrice.ToString(CultureInfo.InvariantCulture),
                s.OperatingSystem));
        }
        await File.WriteAllLinesAsync(_path, lines, cancellationToken);
    }

    private async Task<List<Smartphone>> LoadAsync(CancellationToken cancellationToken)
    {
        var smartphones = new List<Smartphone>();
        // File.ReadAllLinesAsync throws FileNotFoundException: that is for Program.cs to handle.
        string[] lines = await File.ReadAllLinesAsync(_path, cancellationToken);
        foreach (string line in lines.Skip(1)) // skip the header row
        {
            Smartphone? smartphone = ParseLine(line);
            if (smartphone != null)
            {
                smartphones.Add(smartphone);
            }
        }
        return smartphones;
    }

    // Parsing a line waits for nothing, so it stays synchronous.
    // Returns null for a malformed row, so one bad line does not stop the import.
    private static Smartphone? ParseLine(string line)
    {
        string[] parts = line.Split(',');
        if (parts.Length != 6)
        {
            return null;
        }

        if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int id)
            || !int.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int releaseYear)
            || !decimal.TryParse(parts[4], NumberStyles.Number, CultureInfo.InvariantCulture, out decimal startPrice))
        {
            return null;
        }

        return new Smartphone
        {
            Id = id,
            Brand = parts[1].Trim(),
            Type = parts[2].Trim(),
            ReleaseYear = releaseYear,
            StartPrice = startPrice,
            OperatingSystem = parts[5].Trim()
        };
    }

    public Task ImportSmartphones()
    {
        throw new NotImplementedException();
    }

    public Task ImportSmartphones(List<Smartphone> smartphones, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> UpdatePriceAsync(int id, decimal newPrice, CancellationToken cancellationToken = default)
    {
        Smartphone? smartphone = await GetSmartphoneByIdAsync(id, cancellationToken);
        if (smartphone == null)
        {
            return false;
        }

        smartphone.StartPrice = newPrice;
        await SaveChangesAsync(cancellationToken); // rewrites the whole file
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        List<Smartphone> smartphones = await GetSmartphonesAsync(cancellationToken);
        if (smartphones.RemoveAll(s => s.Id == id) == 0)
        {
            return false;
        }

        await SaveChangesAsync(cancellationToken); // rewrites the file without that line
        return true;
    }
}
