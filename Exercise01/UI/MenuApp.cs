public class MenuApp
{
    // Properties
    private readonly SmartphoneService _smartphoneService;

    // Constructor
    public MenuApp(SmartphoneService smartphoneService)
    {
        _smartphoneService = smartphoneService;
    }

    // Methods
    public async Task ShowMenu(CancellationToken cancellationToken)
    {
        while (cancellationToken.IsCancellationRequested == false)
        {
            Console.WriteLine("\nMENU:");
            Console.WriteLine("1. Show all smartphones");
            Console.WriteLine("2. Import Smartphones from CSV to MySQL");
            Console.WriteLine("3. Show smartphone by ID");
            Console.WriteLine("4. Add a smartphone");
            Console.WriteLine("5. Update the price of a smartphone");
            Console.WriteLine("6. Delete a smartphone");
            Console.WriteLine("0. Exit");

            Console.Write("\nCHOICE: ");
            string choice = Console.ReadLine();


            if (choice == "1")
            {
                Console.WriteLine("\nAll Smartphones:");
                await ShowAllSmartphones();
            }

            else if (choice == "2")
            {
                await _smartphoneService.ImportSmartphones(cancellationToken);
                Console.WriteLine("Smartphones imported from CSV");
            }

            else if (choice == "3")
            {
                await ShowSmartphoneById(cancellationToken);
            }

            else if (choice == "4")
            {
                await AddSmartphone(cancellationToken);
            }

            else if (choice == "5")
            {
                await UpdatePrice(cancellationToken);
            }

            else if (choice == "6")
            {
                await DeleteSmartphone(cancellationToken);
            }

            else if (choice == "0")
            {
                Console.WriteLine("Bye!");
                Environment.Exit(0);
            }
            else
            {
                choice = "-1";
                Console.WriteLine("Please enter a valid option.");
                continue;
            }
        }
    }

    private async Task ShowSmartphoneById(CancellationToken cancellationToken)
    {
        Console.Write("\nID Smartphone: ");

        // Console.ReadLine gives a string; GetByIdAsync needs an int.
        if (!int.TryParse(Console.ReadLine(), out int searchId))
        {
            Console.WriteLine("Please enter a whole number.");
            return;
        }

        Smartphone? smartphone = await _smartphoneService.GetByIdAsync(searchId, cancellationToken);

        // GetByIdAsync returns null when there is no row with that id.
        if (smartphone == null)
        {
            Console.WriteLine($"No smartphone found with ID {searchId}.");
            return;
        }

        Console.WriteLine($"- {smartphone}");
    }

    private async Task AddSmartphone(CancellationToken cancellationToken)
    {
        Console.WriteLine("\nAdd a smartphone:");

        Console.Write("Brand: ");
        string brand = Console.ReadLine() ?? string.Empty;

        Console.Write("Type: ");
        string type = Console.ReadLine() ?? string.Empty;

        Console.Write("Release year: ");
        if (!int.TryParse(Console.ReadLine(), out int releaseYear))
        {
            Console.WriteLine("The release year must be a whole number.");
            return;
        }

        // Accept both 699.99 and 699,99: the comma becomes a dot, then parse culture-independent.
        Console.Write("Start price: ");
        string priceText = (Console.ReadLine() ?? string.Empty).Replace(',', '.');
        if (!decimal.TryParse(priceText, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal startPrice))
        {
            Console.WriteLine("The start price must be a number, e.g. 699.99.");
            return;
        }

        Console.Write("Operating system: ");
        string operatingSystem = Console.ReadLine() ?? string.Empty;

        try
        {
            Smartphone smartphone = await _smartphoneService.AddSmartphoneAsync(
                brand, type, releaseYear, startPrice, operatingSystem, cancellationToken);

            // AddSmartphoneAsync returns the smartphone with the id MySQL gave it.
            Console.WriteLine($"Added: {smartphone}");
        }
        catch (ArgumentException ex) // the business rules in SmartphoneService.Validate
        {
            // ArgumentOutOfRangeException is an ArgumentException too, so it is caught here as well.
            Console.WriteLine($"Not added: {ex.Message}");
        }
    }

    private async Task UpdatePrice(CancellationToken cancellationToken)
    {
        Console.Write("\nID Smartphone: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        {
            Console.WriteLine("Please enter a whole number.");
            return;
        }

        // Show the current smartphone first, so the user sees which one and the old price.
        Smartphone? smartphone = await _smartphoneService.GetByIdAsync(id, cancellationToken);
        if (smartphone == null)
        {
            Console.WriteLine($"No smartphone found with ID {id}.");
            return;
        }
        Console.WriteLine($"- {smartphone}");

        // Accept both 699.99 and 699,99, like when adding a smartphone.
        Console.Write("New start price: ");
        string priceText = (Console.ReadLine() ?? string.Empty).Replace(',', '.');
        if (!decimal.TryParse(priceText, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal newPrice))
        {
            Console.WriteLine("The start price must be a number, e.g. 699.99.");
            return;
        }

        try
        {
            bool updated = await _smartphoneService.UpdatePriceAsync(id, newPrice, cancellationToken);

            // false: the smartphone was deleted between showing it and updating it.
            Console.WriteLine(updated
                ? $"Price of ID {id} updated to {newPrice:C}."
                : $"No smartphone found with ID {id}.");
        }
        catch (ArgumentOutOfRangeException ex) // price <= 0
        {
            Console.WriteLine($"Not updated: {ex.Message}");
        }
    }

    private async Task DeleteSmartphone(CancellationToken cancellationToken)
    {
        Console.Write("\nID Smartphone: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        {
            Console.WriteLine("Please enter a whole number.");
            return;
        }

        // Show which smartphone will be deleted before asking for confirmation.
        Smartphone? smartphone = await _smartphoneService.GetByIdAsync(id, cancellationToken);
        if (smartphone == null)
        {
            Console.WriteLine($"No smartphone found with ID {id}.");
            return;
        }
        Console.WriteLine($"- {smartphone}");

        // Deleting cannot be undone: ask first.
        Console.Write("Are you sure you want to delete this smartphone? (y/n): ");
        if (!string.Equals(Console.ReadLine()?.Trim(), "y", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("Not deleted.");
            return;
        }

        bool deleted = await _smartphoneService.DeleteAsync(id, cancellationToken);

        // false: someone else deleted it between showing it and confirming.
        Console.WriteLine(deleted
            ? $"Smartphone with ID {id} deleted."
            : $"No smartphone found with ID {id}.");
    }

    private async Task ShowAllSmartphones()
    {
        var smartphones = await _smartphoneService.GetAllAsync();
        foreach (var phone in smartphones)
        {
            Console.WriteLine($"- {phone}");
        }
    }
}