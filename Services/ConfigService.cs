using System.Xml.Serialization;
using Bitathon.Models;

namespace Bitathon.Services;

public class ConfigService
{
    private AppConfig AppConfig { get; set; } = new();
    private AppConfig AppConfigDto { get; set; }
    
    private static readonly string ExecutingDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)!;
    private static readonly string ApplicationConfigPath = Path.Join(ExecutingDirectory, "app.config.xml");

    public ConfigService()
    {
        if (File.Exists(ApplicationConfigPath))
        {
            var serializer = new XmlSerializer(typeof(AppConfig));
            using var fileStream = new FileStream(ApplicationConfigPath, FileMode.Open);
            AppConfig = (AppConfig) serializer.Deserialize(fileStream)!;
        }
        AppConfigDto = AppConfig.Clone();
    }
    public AppConfig GetApplicationConfig()
    {
        return AppConfig;
    }

    public AppConfig GetApplicationConfigDto()
    {
        AppConfigDto = AppConfig.Clone();
        return AppConfigDto;
    }

    public void SaveApplicationConfig()
    {
        AppConfig = AppConfigDto.Clone();
        var serializer = new XmlSerializer(typeof(AppConfig));
        using var textWriter = new StreamWriter(ApplicationConfigPath);
        serializer.Serialize(textWriter, AppConfig);
    }
}