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
        services.AddMudServices();
        services.AddMudBlazorSnackbar();
        services.AddSingleton<ConfigService>();

        // Configure BlazorWebView
        blazorWebView.HostPage = "wwwroot\\index.html";
        blazorWebView.Services = services.BuildServiceProvider();
        blazorWebView.RootComponents.Add<App>("#app");
    }
}