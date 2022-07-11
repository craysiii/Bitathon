using System.Xml.Serialization;
using Bitathon.Models;
using TwitchLib.PubSub;
using TwitchLib.PubSub.Enums;
using TwitchLib.PubSub.Events;

namespace Bitathon.Services;

public class BitathonService
{
    public BitathonConfig? Bitathon { get; private set; }
    private TwitchPubSub PubSub { get; }
    private PeriodicTimer PeriodicTimer { get; } = new(TimeSpan.FromSeconds(1));
    private CancellationToken TimerCancellationToken { get; } = new();
    private Dictionary<SubscriptionPlan, int> TierPrices { get; } = new();
    private ConfigService ConfigService { get; }
    private LoggingService Logger { get; }
    
    private static readonly string ExecutingDirectory = Path.GetDirectoryName(
        System.Reflection.Assembly.GetExecutingAssembly().Location)!;
    private static readonly string BitathonConfigPath = Path.Join(ExecutingDirectory, "bitathon.config.xml");
    private static readonly string TimeLeftPath = Path.Join(ExecutingDirectory, "timeleft.txt");

    public BitathonService(ConfigService configService, LoggingService logger)
    {
        ConfigService = configService;
        Logger = logger;
        
        PubSub = new TwitchPubSub();
        SetTierPrices();
        
        if (File.Exists(BitathonConfigPath))
        {
            LoadBitathonConfig();
            Logger.Log("Loading existing Bitathon");
        }
        else
        {
            CreateBitathonConfig();
            Logger.Log("Creating new Bitathon");
        }
        
        ConfigurePubSub();
        StartBitathonTimer().ConfigureAwait(false);
    }
    
    public void ModifyTimeLeft(int seconds)
    {
        var timeSpan = new TimeSpan(0, 0, seconds);
        Bitathon!.TimeLeft = Bitathon.TimeLeft.Add(timeSpan);
    }
    
    public void StartBitathon()
    {
        var appConfig = ConfigService.GetApplicationConfig();
        PubSub.ListenToBitsEventsV2(appConfig.ChannelId);
        PubSub.ListenToSubscriptions(appConfig.ChannelId);
        PubSub.Connect();
        Bitathon!.IsRunning = true;
        Logger.Log("Bitathon started");
    }

    public void StopBitathon()
    {
        PubSub.Disconnect();
        Bitathon!.IsRunning = false;
        Logger.Log("Bitathon stopped");
    }

    public void ResetBitathon()
    {
        if (Bitathon!.IsRunning)
        {
            PubSub.Disconnect();
            Bitathon!.IsRunning = false;
        }

        File.Delete(BitathonConfigPath);
        CreateBitathonConfig();
        Logger.Log("Bitathon reset");
    }

    private void CreateBitathonConfig()
    {
        var currentConfig = ConfigService.GetApplicationConfig();
        Bitathon = new BitathonConfig
        {
            TimeLeft = currentConfig.InitialStreamCountDown
        };
    }

    private void LoadBitathonConfig()
    {
        var serializer = new XmlSerializer(typeof(BitathonConfig));
        using var fileStream = new FileStream(BitathonConfigPath, FileMode.Open);
        Bitathon = (BitathonConfig) serializer.Deserialize(fileStream)!;
    }

    private void SaveBitathonConfig()
    {
        var serializer = new XmlSerializer(typeof(BitathonConfig));
        using var textWriter = new StreamWriter(BitathonConfigPath);
        serializer.Serialize(textWriter, Bitathon);
    }
    
    private void SetTierPrices()
    {
        var currentConfig = ConfigService.GetApplicationConfig();
        TierPrices[SubscriptionPlan.Prime] = currentConfig.SecondsPerTier1Sub;
        TierPrices[SubscriptionPlan.Tier1] = currentConfig.SecondsPerTier1Sub;
        TierPrices[SubscriptionPlan.Tier2] = currentConfig.SecondsPerTier2Sub;
        TierPrices[SubscriptionPlan.Tier3] = currentConfig.SecondsPerTier3Sub;
    }

    private async Task StartBitathonTimer()
    {
        while (await PeriodicTimer.WaitForNextTickAsync(TimerCancellationToken) &&
               !TimerCancellationToken.IsCancellationRequested)
        {
            TickBitathonTimer();
        }
    }

    private void TickBitathonTimer()
    {
        if (Bitathon is {IsRunning: true})
        {
            TickBitathon(-1);
            Bitathon.TotalTimeElapsed += 1;
            WriteTimeLeftToFile();
        }
        SaveBitathonConfig();
    }

    private void WriteTimeLeftToFile()
    {
        File.WriteAllText(TimeLeftPath, Bitathon!.TimeLeft.ToString());
    }

    private void ConfigurePubSub()
    {
        PubSub.OnPubSubServiceConnected += PubSubOnOnPubSubServiceConnected;
        PubSub.OnPubSubServiceClosed += PubSubOnOnPubSubServiceClosed;
        PubSub.OnPubSubServiceError += PubSubOnOnPubSubServiceError;
        PubSub.OnBitsReceivedV2 += PubSubOnOnBitsReceived;
        PubSub.OnChannelSubscription += PubSubOnOnChannelSubscription;
    }

    private void TickBitathon(int seconds)
    {
        ModifyTimeLeft(seconds);
        
        // Only add to TotalAddedSeconds if its through a bit donation or subscription, otherwise its from the timer
        // counting down
        if (seconds <= 0) return;
        Bitathon!.TotalAddedSeconds += seconds;
        Logger.Log($"Added {seconds} to timer. Total added seconds: {Bitathon.TotalAddedSeconds}");
        SaveBitathonConfig();
    }

    private void PubSubOnOnPubSubServiceConnected(object? sender, EventArgs e)
    {
        var appConfig = ConfigService.GetApplicationConfig();
        PubSub.SendTopics(appConfig.AccessToken);
        Logger.Log("Connected to Twitch PubSub");
    }
    
    private void PubSubOnOnPubSubServiceClosed(object? sender, EventArgs e)
    {
        Logger.Log($"Disconnected from Twitch PubSub");
    }

    private void PubSubOnOnPubSubServiceError(object? sender, OnPubSubServiceErrorArgs e)
    {
        Logger.Log($"Twitch PubSub error: {e.Exception.Message}");
    }

    private void PubSubOnOnBitsReceived(object? sender, OnBitsReceivedV2Args e)
    {
        var currentConfig = ConfigService.GetApplicationConfig();
        var amountToAdd = (int) Math.Round(e.BitsUsed * ((float) currentConfig.SecondsPer100Bits / 100));
        TickBitathon(amountToAdd);
        Logger.Log($"Received {e.BitsUsed} bits from {e.UserName}. Adding {amountToAdd} seconds. Total bits: {Bitathon!.TotalBits}");
        SaveBitathonConfig();
    }
    
    private void PubSubOnOnChannelSubscription(object? sender, OnChannelSubscriptionArgs e)
    {
        Logger.Log($"Received {e.Subscription.SubscriptionPlanName} sub from {e.Subscription.Username}");
        SetTierPrices();
        TickBitathon(TierPrices[e.Subscription.SubscriptionPlan]);

        Bitathon!.TotalSubs += 1;
        switch (e.Subscription.SubscriptionPlan)
        {
            case SubscriptionPlan.Prime:
            case SubscriptionPlan.Tier1:
                Bitathon.TotalTier1Subs += 1;
                break;
            case SubscriptionPlan.Tier2:
                Bitathon.TotalTier2Subs += 1;
                break;
            case SubscriptionPlan.Tier3:
                Bitathon.TotalTier3Subs += 1;
                break;
            case SubscriptionPlan.NotSet:
            default:
                break;
        }
        Logger.Log($"Subs: {Bitathon.TotalSubs} Tier 1: {Bitathon.TotalTier1Subs} Tier 2: {Bitathon.TotalTier2Subs} Tier 3: {Bitathon.TotalTier3Subs}");
        SaveBitathonConfig();
    }
}