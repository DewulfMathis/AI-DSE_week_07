using Exercise01.Models;
using Exercise01.Repositories;

namespace Exercise01.Services;

public class SmartphoneService
{
    private readonly ISmartphoneRepository _repository;

    public SmartphoneService(ISmartphoneRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<Smartphone>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        List<Smartphone> smartphones = await _repository.GetSmartphonesAsync(cancellationToken);
        return smartphones
            .OrderBy(s => s.Brand)
            .ThenBy(s => s.Type)
            .ToList();
    }

    // Nothing to do with the result, so no async/await: the task of the repository is passed on as is.
    public Task<Smartphone?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return _repository.GetSmartphoneByIdAsync(id, cancellationToken);
    }

    // Searches brand and type at the same time, case-insensitive, on a part of the text.
    public async Task<List<Smartphone>> SearchAsync(string text, CancellationToken cancellationToken = default)
    {
        List<Smartphone> smartphones = await GetAllAsync(cancellationToken);
        return smartphones
            .Where(s => s.Brand.Contains(text, StringComparison.OrdinalIgnoreCase)
                     || s.Type.Contains(text, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public async Task<Smartphone> AddSmartphoneAsync(string brand, string type, int releaseYear, decimal startPrice,
        string operatingSystem, CancellationToken cancellationToken = default)
    {
        Validate(brand, type, releaseYear, startPrice, operatingSystem);

        var smartphone = new Smartphone
        {
            Brand = brand.Trim(),
            Type = type.Trim(),
            ReleaseYear = releaseYear,
            StartPrice = startPrice,
            OperatingSystem = operatingSystem.Trim()
        };

        // AddSmartphone is synchronous and cannot read the file itself: make sure the data is loaded.
        await _repository.GetSmartphonesAsync(cancellationToken);
        _repository.AddSmartphone(smartphone);
        await _repository.SaveChangesAsync(cancellationToken);
        return smartphone;
    }

    // ---- Exercise 2: LINQ instead of a foreach loop. The answer on smartphones.csv is next to each query.

    // 1. Search by brand, part of the name, case-insensitive.
    //    "sam" -> Samsung Galaxy S21 (799) and Samsung Galaxy S21 (999).
    public async Task<List<Smartphone>> SearchByBrandAsync(string text, CancellationToken cancellationToken = default)
    {
        List<Smartphone> smartphones = await _repository.GetSmartphonesAsync(cancellationToken);
        return smartphones
            .Where(s => s.Brand.Contains(text, StringComparison.OrdinalIgnoreCase))
            .OrderBy(s => s.StartPrice)
            .ToList();
    }

    // 2. The five cheapest, equal prices sorted by brand.
    //    Realme Narzo 30 Pro 249, Vivo V21 499, Apple iPhone 13 699, Google Pixel 6 699, Xiaomi Mi 11 699.
    public async Task<List<Smartphone>> GetFiveCheapestAsync(CancellationToken cancellationToken = default)
    {
        List<Smartphone> smartphones = await _repository.GetSmartphonesAsync(cancellationToken);
        return smartphones
            .OrderBy(s => s.StartPrice)
            .ThenBy(s => s.Brand)
            .Take(5)
            .ToList();
    }

    // 3. The number of smartphones per brand, highest number first.
    //    Samsung 2, then the nine other brands with 1 each, alphabetically
    //    (Apple, Asus, Google, OnePlus, OPPO, Realme, Sony, Vivo, Xiaomi).
    public async Task<List<BrandCount>> CountPerBrandAsync(CancellationToken cancellationToken = default)
    {
        List<Smartphone> smartphones = await _repository.GetSmartphonesAsync(cancellationToken);
        return smartphones
            .GroupBy(s => s.Brand)
            .Select(g => new BrandCount(g.Key, g.Count()))
            .OrderByDescending(b => b.Count)
            .ThenBy(b => b.Brand)
            .ToList();
    }

    // 4. Average price per operating system, rounded to two decimals.
    //    Android 794.00 (ten phones), iOS 699.00 (one phone).
    public async Task<List<OperatingSystemPrice>> AveragePricePerOperatingSystemAsync(CancellationToken cancellationToken = default)
    {
        List<Smartphone> smartphones = await _repository.GetSmartphonesAsync(cancellationToken);
        return smartphones
            .GroupBy(s => s.OperatingSystem)
            .Select(g => new OperatingSystemPrice(g.Key, Math.Round(g.Average(s => s.StartPrice), 2)))
            .OrderBy(o => o.OperatingSystem)
            .ToList();
    }

    // 5a. Is there a smartphone released before the given year?
    //     Before 2015 -> false: the oldest one is the Xiaomi Mi 11 from 2020.
    //     Any on an empty catalogue simply returns false.
    public async Task<bool> AnyReleasedBeforeAsync(int year, CancellationToken cancellationToken = default)
    {
        List<Smartphone> smartphones = await _repository.GetSmartphonesAsync(cancellationToken);
        return smartphones.Any(s => s.ReleaseYear < year);
    }

    // 5b. The newest smartphone, or null for an empty catalogue (FirstOrDefault instead of First,
    //     which would throw InvalidOperationException).
    //     Apple iPhone 13 (2021): ten phones are from 2021, OrderByDescending is stable, so the
    //     first of them in the file wins.
    public async Task<Smartphone?> GetNewestAsync(CancellationToken cancellationToken = default)
    {
        List<Smartphone> smartphones = await _repository.GetSmartphonesAsync(cancellationToken);
        return smartphones
            .OrderByDescending(s => s.ReleaseYear)
            .FirstOrDefault();
    }

    // 6. Browse: one page of the catalogue. Page 1 = ids 1 to 10, page 2 = id 11.
    //
    // This is the method that returns an IEnumerable<T> that is still a query: there is no ToList()
    // at the end. Nothing is sorted or skipped when the method returns; that happens only when the
    // caller loops over the result, and it happens again every time the caller loops over it.
    // The query reads the List<Smartphone> of the repository itself. Once the catalogue can change,
    // that is a risk: a smartphone added after this method returned but before the loop shows up on
    // the page (the page no longer is what was asked for), and adding one while the loop is running
    // throws InvalidOperationException ("Collection was modified"). Program.cs loops over it at once,
    // so here it is fine; ToList() would make it a snapshot.
    public async Task<IEnumerable<Smartphone>> GetPageAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        List<Smartphone> smartphones = await _repository.GetSmartphonesAsync(cancellationToken);
        return smartphones
            .OrderBy(s => s.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize);
    }

    public async Task<int> CountPagesAsync(int pageSize, CancellationToken cancellationToken = default)
    {
        List<Smartphone> smartphones = await _repository.GetSmartphonesAsync(cancellationToken);
        return Math.Max(1, (smartphones.Count + pageSize - 1) / pageSize);
    }

    // The business rules await nothing, so this method stays synchronous.
    private static void Validate(string brand, string type, int releaseYear, decimal startPrice, string operatingSystem)
    {
        if (string.IsNullOrWhiteSpace(brand))
            throw new ArgumentException("A brand is required.", nameof(brand));
        if (string.IsNullOrWhiteSpace(type))
            throw new ArgumentException("A type is required.", nameof(type));
        if (releaseYear < 2000 || releaseYear > DateTime.Now.Year + 1)
            throw new ArgumentOutOfRangeException(nameof(releaseYear), $"The release year must be between 2000 and {DateTime.Now.Year + 1}.");
        if (startPrice <= 0)
            throw new ArgumentOutOfRangeException(nameof(startPrice), "The start price must be positive.");
        if (string.IsNullOrWhiteSpace(operatingSystem))
            throw new ArgumentException("An operating system is required.", nameof(operatingSystem));
        if ((brand + type + operatingSystem).Contains(','))
            throw new ArgumentException("Brand, type and operating system may not contain a comma.");
    }
}
