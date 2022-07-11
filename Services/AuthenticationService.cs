using System.Net;
using Microsoft.AspNetCore.WebUtilities;
using TwitchLib.Api;

namespace Bitathon.Services;

public class AuthenticationService
{
    private TwitchAPI Api { get; }
    
    private const string ClientId = "ouvlks3ys2rk5nwwaj7oyjk1ado1nk";
    private const string RedirectUri = "http://localhost:3690";
    private const string BaseUrl = "https://id.twitch.tv/oauth2/authorize";
    private LoggingService Logger { get; }
    
    public AuthenticationService(LoggingService logger)
    {
        Logger = logger;
        Api = new TwitchAPI
        {
            Settings =
            {
                ClientId = ClientId
            }
        };
    }

    public string GetAuthenticationUrl()
    {
        
        var queryParams = new Dictionary<string, string>
        {
            { "response_type", "token id_token" },
            { "client_id", ClientId },
            { "redirect_uri", RedirectUri },
            { "scope", "openid bits:read channel:read:subscriptions" }
        };
        var completeUrl = new Uri(QueryHelpers.AddQueryString(BaseUrl, queryParams)).ToString();
        Logger.Log($"Generated authentication Url: {completeUrl}");
        return completeUrl;
    }

    public async Task<(string, string)> CompleteAuthentication()
    {
        // Receive Fragment from Twitch
        using var listener = new HttpListener();
        listener.Prefixes.Add($"{RedirectUri}/");
        listener.Start();
        Logger.Log("HTTP listener started");
        var context = await listener.GetContextAsync();
        var response = context.Response;
        var responseString = await File.ReadAllTextAsync(@"wwwroot\auth.html");
        var buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
        response.ContentLength64 = buffer.Length;
        var output = response.OutputStream;
        await output.WriteAsync(buffer);
        Logger.Log("Sent authentication page");
        output.Close();
        // Receive QueryString from Javascript
        context = await listener.GetContextAsync();
        var request = context.Request;
        listener.Stop();
        Logger.Log("HTTP listening stopped");

        // Grab out vars
        var accessToken = request.QueryString["access_token"]!;
        Api.Settings.AccessToken = accessToken;
        var user = (await Api.Helix.Users.GetUsersAsync()).Users[0];
        Logger.Log($"Access token retrieved. User Id: {user.Id} Access Token starts with: {accessToken[..3]}");

        return (accessToken, user.Id);
    }
}