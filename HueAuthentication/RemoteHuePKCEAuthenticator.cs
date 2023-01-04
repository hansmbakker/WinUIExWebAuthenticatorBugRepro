using IdentityModel;
using IdentityModel.Client;
using Microsoft.Extensions.Logging.Abstractions;
using OAuth2Test;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;

namespace RemoteHuePKCEAuthentication
{
    public class RemoteHuePKCEAuthenticator
    {
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly CryptoHelper _cryptoHelper;
        private readonly CryptoHelper.Pkce _pkce;

        public RemoteHuePKCEAuthenticator(string clientId, string clientSecret)
        {
            _clientId = clientId;
            _clientSecret = clientSecret;
            _cryptoHelper = new CryptoHelper(new NullLogger<CryptoHelper>());
            _pkce = _cryptoHelper.CreatePkceData();
        }

        public string BuildAuthorizeUrl()
        {
            //var state = _cryptoHelper.CreateState(100);
            var requestUrl = new RequestUrl("https://api.meethue.com/v2/oauth2/authorize");
            var authorizeUrl = requestUrl.CreateAuthorizeUrl(
                _clientId,
                "code",
                scope: null,
                //redirectUri: redirectUri,
                //state: state,
                codeChallenge: _pkce.CodeChallenge,
                codeChallengeMethod: OidcConstants.CodeChallengeMethods.Sha256);
            return authorizeUrl;
        }

        public async Task<TokenResponse> GetAccessTokenAsync(string code)
        {
            var content = new Dictionary<string, string>{
                { "grant_type", "authorization_code"},
                { "code", code},
                { "code_verifier", _pkce.CodeVerifier }
            };
            return await AuthenticateAsync(content);
        }

        public async Task<TokenResponse> RefreshTokenAsync(string refreshToken)
        {
            var content = new Dictionary<string, string>{
                { "grant_type", "refresh_token"},
                { "refresh_token", refreshToken}
            };
            return await AuthenticateAsync(content);
        }

        private async Task<TokenResponse> AuthenticateAsync(Dictionary<string, string> content)
        {
            var client = new HttpClient();
            var canaryTokenResponse = await client.PostAsync("https://api.meethue.com/v2/oauth2/token", new FormUrlEncodedContent(content));

            var responseString = canaryTokenResponse.Headers.WwwAuthenticate
                .ToString().Replace("Digest ", string.Empty);
            var matches = Regex.Match(responseString, "realm=\"(.*)\",nonce=\"(.*)\"");
            var realm = matches.Groups[1].Value;
            var nonce = matches.Groups[2].Value;

            var path = "/v2/oauth2/token";
            var digestResponse = GetDigestResponse(HttpMethod.Post, path, realm, nonce);

            client.DefaultRequestHeaders.Authorization =
                   new AuthenticationHeaderValue("Digest",
                   $"username=\"{_clientId}\", {responseString}, uri=\"{path}\", response=\"{digestResponse}\"");

            var tokenResponse = await client.PostAsync("https://api.meethue.com/v2/oauth2/token", new FormUrlEncodedContent(content));
            var token = await ProtocolResponse.FromHttpResponseAsync<TokenResponse>(tokenResponse);
            return token;
        }

        private string GetDigestResponse(HttpMethod method, string path, string realm, string nonce)
        {
            var HASH1 = MD5($"{_clientId}:{realm}:{_clientSecret}");
            var HASH2 = MD5($"{method}:{path}");
            var response = MD5($"{HASH1}:{nonce}:{HASH2}");
            return response;
        }

        public static string MD5(string input)
        {
            // Use input string to calculate MD5 hash
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // ToLowerInvariant is required because ToHexString
                // returns uppercase which is incompatible with
                // what Philips Hue expects
                return Convert.ToHexString(hashBytes).ToLowerInvariant(); // .NET 5 +
            }
        }
    }
}
