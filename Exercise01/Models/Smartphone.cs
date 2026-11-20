namespace Exercise01.Models;

public class Smartphone
{
    public int Id { get; set; }
    public string Brand { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int ReleaseYear { get; set; }
    public decimal StartPrice { get; set; }
    public string OperatingSystem { get; set; } = string.Empty;

    public override string ToString()
    {
        return $"{Id,3}  {Brand} {Type} ({ReleaseYear}) - {OperatingSystem} - {StartPrice:C}";
    }
}
