using Exercise01.Models;

namespace Exercise01.Repositories;

// The interface is asynchronous even though this implementation never waits, because the interface
// describes every possible storage - a file, a database, a web API - and those do have to wait.
public class InMemorySmartphoneRepository : ISmartphoneRepository
{
    private readonly List<Smartphone> _smartphones = new()
    {
        new Smartphone { Id = 1, Brand = "Apple", Type = "iPhone 13", ReleaseYear = 2021, StartPrice = 699m, OperatingSystem = "iOS" },
        new Smartphone { Id = 2, Brand = "Samsung", Type = "Galaxy S21", ReleaseYear = 2021, StartPrice = 799m, OperatingSystem = "Android" },
        new Smartphone { Id = 3, Brand = "Google", Type = "Pixel 6", ReleaseYear = 2021, StartPrice = 699m, OperatingSystem = "Android" },
        new Smartphone { Id = 4, Brand = "Fairphone", Type = "5", ReleaseYear = 2023, StartPrice = 579.50m, OperatingSystem = "Android" }
    };

    // Not marked async: there is nothing to await. Task.FromResult wraps a value that is already
    // there in a finished task.
    public Task<List<Smartphone>> GetSmartphonesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_smartphones);
    }

    public Task<Smartphone?> GetSmartphoneByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_smartphones.FirstOrDefault(s => s.Id == id));
    }

    public void AddSmartphone(Smartphone smartphone)
    {
        smartphone.Id = _smartphones.Count == 0 ? 1 : _smartphones.Max(s => s.Id) + 1;
        _smartphones.Add(smartphone);
    }

    // The list is the storage: nothing left to save. Task.CompletedTask is a task that is already done.
    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
