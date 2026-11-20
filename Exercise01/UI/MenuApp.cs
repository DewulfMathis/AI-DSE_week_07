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
            Console.WriteLine("0. Exit");

            Console.Write("\nCHOICE: ");
            string choice = Console.ReadLine();


            if (choice == "1")
            {
                await ShowAllSmartphones();
            }

            if (choice == "2")
            {
                await _smartphoneService.ImportSmartphones(cancellationToken);
                Console.WriteLine("Smartphones imported from CSV");
            }

            if (choice == "0")
            {
                Environment.Exit(0);
            }
        }
    }

    private async Task ShowAllSmartphones()
    {
        var smartphones = await _smartphoneService.GetAllAsync();
        foreach (var phone in smartphones)
        {
            Console.WriteLine(phone.Type);
        }
    }
}