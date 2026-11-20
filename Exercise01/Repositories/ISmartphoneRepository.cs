namespace Exercise01.Repositories;

public interface ISmartphoneRepository
{
    Task<List<Smartphone>> GetSmartphonesAsync(CancellationToken cancellationToken = default);
    Task<Smartphone?> GetSmartphoneByIdAsync(int id, CancellationToken cancellationToken = default);

    // AddSmartphone stays synchronous: it only puts an object in a list in memory. There is no file,
    // no network, nothing to wait for. Making it async would only add a Task the caller has to await
    // for nothing. The waiting happens in SaveChangesAsync, which writes the file.
    void AddSmartphone(Smartphone smartphone);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
    Task ImportSmartphones(List<Smartphone> smartphones, CancellationToken cancellationToken = default);

    // Changes only the start price. Returns false when there is no smartphone with that id.
    Task<bool> UpdatePriceAsync(int id, decimal newPrice, CancellationToken cancellationToken = default);

    // Removes the smartphone. Returns false when there is no smartphone with that id.
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
