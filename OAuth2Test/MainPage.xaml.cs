using IdentityModel;
using IdentityModel.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RemoteHuePKCEAuthentication;
using System.Buffers.Text;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using static IdentityModel.OidcConstants;

namespace OAuth2Test;

public partial class MainPage : ContentPage
{
    int count = 0;
    private readonly RemoteHuePKCEAuthenticator _remoteHuePKCEAuthenticator;

    public MainPage()
    {
        InitializeComponent();

        var clientId = "tGEWSfWqkA95Boo9E28RHA57dsjvaGaG";
        var clientSecret = "f5uUneJ8v1PYeGpA";

        _remoteHuePKCEAuthenticator = new RemoteHuePKCEAuthenticator(clientId, clientSecret);
    }

    private async void OnHueConnectClicked(object sender, EventArgs e)
    {
        var redirectUri = "bugrepro://huecallback";
        var callbackUri = new Uri(redirectUri);

        var authorizeUrl = _remoteHuePKCEAuthenticator.BuildAuthorizeUrl();

        _ = Navigation.PushModalAsync(new ContentPage()
        {
            Content = new Label() { Text = $"Waiting for sign in in your browser\n Username: 'nice-ferret@example.com'\n Password: Black-Capybara-83" }
        });

#if WINDOWS
        var result = await WinUIEx.WebAuthenticator.AuthenticateAsync(new Uri(authorizeUrl), callbackUri);
#else
        var result = await WebAuthenticator.AuthenticateAsync(new Uri(authorizeUrl), callbackUri);
#endif
        var token = await _remoteHuePKCEAuthenticator.GetAccessTokenAsync(result.Properties["code"]);


        if (Navigation.ModalStack.Count > 0)
            _ = Navigation.PopModalAsync();
        await DisplayAlert("Success!", "Signed in", "OK");
    }
}

