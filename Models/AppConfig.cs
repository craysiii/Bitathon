namespace Bitathon.Models;

public class AppConfig
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public TimeSpan InitialStreamCountDown { get; set; } = new(8, 0, 0);
    public int SecondsPer100Bits { get; set; } = 60;
    public int SecondsPerTier1Sub { get; set; } = 300;
    public int SecondsPerTier2Sub { get; set; } = 600;
    public int SecondsPerTier3Sub { get; set; } = 1500;

    public AppConfig Clone()
    {
        return new AppConfig
        {
            ClientId = ClientId,
            ClientSecret = ClientSecret,
            InitialStreamCountDown = InitialStreamCountDown,
            SecondsPer100Bits = SecondsPer100Bits,
            SecondsPerTier1Sub = SecondsPerTier1Sub,
            SecondsPerTier2Sub = SecondsPerTier2Sub,
            SecondsPerTier3Sub = SecondsPerTier3Sub
        };
    }
}