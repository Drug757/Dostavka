// --- Client.cs ---

public class Client
{
    public int ClientId { get; set; }
    public string Nickname { get; set; }
    public string Email { get; set; }
    public string Street { get; set; }
    public string Building { get; set; }
    public string Apartment { get; set; }

    public string FullAddress => $"{Street}, д. {Building}" + (string.IsNullOrWhiteSpace(Apartment) ? "" : $", кв. {Apartment}");
}