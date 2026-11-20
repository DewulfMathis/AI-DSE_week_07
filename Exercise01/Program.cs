HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddJsonFile("appsettings.local.json", optional: true);

string? connectionString = builder.Configuration.GetConnectionString("Catalogue");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Connection string 'Catalogue' is missing. Add it to appsettings.local.json.");
}

builder.Services.AddSingleton(new MySqlDataSource(connectionString));


// DEPENDENCY INJECTION

// Repositories: ze delen allen de ISmartphoneRepository, je kan dus makkelijk wisselen.
builder.Services.AddSingleton<ISmartphoneRepository>(_ => new CsvSmartphoneRepository("Data/smartphones.csv"));
// builder.Services.AddSingleton<ISmartphoneRepository>(_ => new InMemorySmartphoneRepository());
builder.Services.AddSingleton<MySqlSmartphoneRepository>();

// Services
builder.Services.AddSingleton<SmartphoneService>();

// MenuApp
builder.Services.AddSingleton<MenuApp>();

using IHost host = builder.Build();
using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (sender, eventArgs) =>
{
    eventArgs.Cancel = true;
    cts.Cancel();
};

using(IServiceScope scope = host.Services.CreateScope())
{
    MenuApp app = scope.ServiceProvider.GetRequiredService<MenuApp>();
    await app.ShowMenu(cts.Token);
}
