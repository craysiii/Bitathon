using Bitathon.Services;
using Microsoft.AspNetCore.Components.WebView.WindowsForms;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace Bitathon;

public partial class MainForm : Form
{
    public MainForm()
    {
        InitializeComponent();
        
        // Register services
        var services = new ServiceCollection();
        services.AddWindowsFormsBlazorWebView();
#if DEBUG
        services.AddBlazorWebViewDeveloperTools();
#endif
        services.AddMudServices();
        services.AddMudBlazorSnackbar();
        services.AddSingleton<ConfigService>();
        services.AddSingleton<BitathonService>();
        services.AddSingleton<LoggingService>();
        services.AddScoped<AuthenticationService>();

        // Configure BlazorWebView
        blazorWebView.HostPage = "wwwroot\\index.html";
        blazorWebView.Services = services.BuildServiceProvider();
        blazorWebView.RootComponents.Add<App>("#app");
        
        FormClosed += OnFormClosed;
    }

    private void OnFormClosed(object? sender, FormClosedEventArgs e)
    {
        Environment.Exit(0);
    }
}