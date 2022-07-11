namespace Bitathon.Models;

public class BitathonConfig
{
    public bool IsRunning { get; set; }
    public long TotalBits { get; set; }
    public long TotalSubs { get; set; }
    public long TotalTier1Subs { get; set; }
    public long TotalTier2Subs { get; set; }
    public long TotalTier3Subs { get; set; }
    public long TotalAddedSeconds { get; set; }
    public long TotalTimeElapsed { get; set; }
    public TimeSpan TimeLeft { get; set; } = TimeSpan.MinValue;
}