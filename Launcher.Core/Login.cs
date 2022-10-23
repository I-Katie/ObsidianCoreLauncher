using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

//https://wiki.vg/Microsoft_Authentication_Scheme

namespace Launcher.Core
{
    public static class Login
    {
        private static readonly string[] Scopes = new[] { "XboxLive.signin" };

        private const string MSACacheFile = "msa-auth.dat";
        private const string LoginCacheFile = "login.dat";

        private const string XBLAuthenticate = "https://user.auth.xboxlive.com/user/authenticate";
        private const string XSTSAuthorize = "https://xsts.auth.xboxlive.com/xsts/authorize";
        private const string MinecraftAuth = "https://api.minecraftservices.com/authentication/login_with_xbox";
        private const string MinecraftProfile = "https://api.minecraftservices.com/minecraft/profile";

        private static IPublicClientApplication ClientApp;

        public static void Init()
        {
            ClientApp = PublicClientApplicationBuilder.Create(Secrets.AzureApp_ClientId)
                .WithRedirectUri("http://localhost")
                .WithTenantId("consumers") // sign in users with home accounts only
                .Build();

            var cacheHelper = new TokenCacheHelper(Path.Combine(Launcher.BaseDir, MSACacheFile));
            cacheHelper.RegisterCache(ClientApp.UserTokenCache);
        }

        public static async Task LogoutAsync(IPageControl pageControl)
        {
            pageControl.SetPageWait("Logging out...");

            DeleteLoginData();

            var accounts = await ClientApp.GetAccountsAsync();
            await ClientApp.RemoveAsync(accounts.FirstOrDefault());

            pageControl.SetPageLogin();
        }

        public static async Task AutoLoginAsync(IPageControl pageControl)
        {
            //validates Microsoft account token and then either opens the Login page or passes to LoginOtherAsync
            try
            {
                //throw new HttpRequestException(); // uncomment to pretend we're offline !!!!

                var firstAccount = (await ClientApp.GetAccountsAsync()).FirstOrDefault();
                var authResult = await ClientApp.AcquireTokenSilent(Scopes, firstAccount).ExecuteAsync();

                await LoginXboxAndMinecraftAsync(pageControl, authResult);
            }
            catch (MsalUiRequiredException)
            {
                DeleteLoginData(); //if it exists by any chance, get rid of it
                pageControl.SetPageLogin();
            }
            catch (HttpRequestException)
            {
                LoginData loginData = LoadLoginData();
                if (loginData != null)
                {
                    //offer to run offline
                    pageControl.SetPageLoggedIn(loginData, offline: true);
                }
                else
                {
                    await pageControl.ShowErrorPageAsync("Connection error", "An error occured while connecting to the login servers.");
                    //this should not happen under normal circumstances, so just log the user out to fix stuff
                    await LogoutAsync(pageControl);
                }
            }
            catch (LoginException ex)
            {
                await pageControl.ShowErrorPageAsync("Login failed", ex.Message);
                await LogoutAsync(pageControl);
            }
        }

        public static async Task LoginAsync(IPageControl pageControl)
        {
            //logs into Microsoft account and then passes to LoginOtherAsync
            try
            {
                var cts = new CancellationTokenSource();

                pageControl.SetPageWaitWithCancel("Waiting for Microsoft login...", cts);

                var authResult = await ClientApp.AcquireTokenInteractive(Scopes)
                    .WithUseEmbeddedWebView(false)
                    //.WithSystemWebViewOptions(new SystemWebViewOptions { })
                    .WithPrompt(Prompt.SelectAccount)
                    .ExecuteAsync(cts.Token);

                await LoginXboxAndMinecraftAsync(pageControl, authResult);
            }
            catch (MsalServiceException)
            {
                //Back button (maybe something else too)
                pageControl.SetPageLogin();
            }
            catch (HttpRequestException)
            {
                await pageControl.ShowErrorPageAsync("Connection error", "An error occured while connecting to the login servers.");
                pageControl.SetPageLogin();
            }
            catch (LoginException ex)
            {
                await pageControl.ShowErrorPageAsync("Login failed", ex.Message);
                await LogoutAsync(pageControl);
            }
            catch (OperationCanceledException)
            {
                pageControl.SetPageLogin();
            }
        }

        private static async Task LoginXboxAndMinecraftAsync(IPageControl pageControl, AuthenticationResult authResult)
        {
            try
            {
                pageControl.SetPageWait("Logging in to Xbox and Mojang...");


                //XBL - Xbox Live
                //this one succeeds even on a new account that never used xbox live
                XBLResponse xblResponse;
                try
                {
                    var headers = new Dictionary<string, string>
                    {
                        {"x-xbl-contract-version", "1"}
                    };
                    var xblData = new
                    {
                        Properties = new
                        {
                            AuthMethod = "RPS",
                            SiteName = "user.auth.xboxlive.com",
                            RpsTicket = "d=" + authResult.AccessToken
                        },
                        RelyingParty = "http://auth.xboxlive.com",
                        TokenType = "JWT"
                    };
                    xblResponse = await Http4Login.PostObjectAsync<XBLResponse>(XBLAuthenticate, xblData, headers);
                }
                catch (HttpFailureResponseException ex)
                {
                    //(HttpFailureResponseException BadRequest)
                    throw new LoginException($"Xbox Live login error ({ex.StatusCode})");
                }


                //XSTS - Xbox Live security token (for Xbox Live)
                XBLResponse xstsResponseXbox;
                try
                {
                    var headers = new Dictionary<string, string>
                    {
                        {"x-xbl-contract-version", "1"}
                    };
                    var xstsData = new
                    {
                        Properties = new
                        {
                            SandboxId = "RETAIL",
                            UserTokens = new string[]
                            {
                                xblResponse.Token
                            }
                        },
                        RelyingParty = "http://xboxlive.com",
                        TokenType = "JWT"
                    };
                    xstsResponseXbox = await Http4Login.PostObjectAsync<XBLResponse>(XSTSAuthorize, xstsData, headers);
                }
                catch (HttpFailureResponseException ex)
                {
                    //(HttpFailureResponseException BadRequest)
                    //(HttpFailureResponseException Unauthorized) - if it's a new account that doesn't yet have xbox live
                    if (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                        throw new LoginException("Login to Xbox Live failed.");
                    else
                        throw new LoginException($"Xbox STS error ({ex.StatusCode})");
                }


                //XSTS - Xbox Live security token (for Minecraft)
                //this one succeeds even on a new account that never used xbox live
                XBLResponse xstsResponseMC;
                try
                {
                    var xstsData = new
                    {
                        Properties = new
                        {
                            SandboxId = "RETAIL",
                            UserTokens = new string[]
                            {
                                xblResponse.Token
                            }
                        },
                        RelyingParty = "rp://api.minecraftservices.com/",
                        TokenType = "JWT"
                    };
                    xstsResponseMC = await Http4Login.PostObjectAsync<XBLResponse>(XSTSAuthorize, xstsData);
                }
                catch (HttpFailureResponseException ex)
                {
                    //(HttpFailureResponseException BadRequest)
                    //(HttpFailureResponseException Unauthorized) - same as in the previous request
                    if (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                        throw new LoginException("Login to Xbox Live failed.");
                    else
                        throw new LoginException($"Xbox STS error ({ex.StatusCode})");
                }

                //Minecraft
                //this one succeeds even on a new account that has registered with xbox live but doesn't have minecraft
                MinecraftAuthResponse minecraftResponse;
                try
                {
                    var minecraftData = new
                    {
                        identityToken = $"XBL3.0 x={xstsResponseMC.DisplayClaims.Xui[0].Uhs};{xstsResponseMC.Token}"
                    };
                    minecraftResponse = await Http4Login.PostObjectAsync<MinecraftAuthResponse>(MinecraftAuth, minecraftData);
                }
                catch (HttpFailureResponseException ex)
                {
                    //(HttpFailureResponseException BadRequest)
                    throw new LoginException($"MinecraftServices login error ({ex.StatusCode})");
                }


                //Get the profile
                MinecraftProfileResponse profileResponse;
                try
                {
                    var headers = new Dictionary<string, string>
                    {
                        { "Authorization", $"Bearer {minecraftResponse.AccessToken}" }
                    };
                    profileResponse = await Http4Login.GetAsync<MinecraftProfileResponse>(MinecraftProfile, headers);
                }
                catch (HttpFailureResponseException ex)
                {
                    //(HttpFailureResponseException Unauthorized)
                    //(HttpFailureResponseException NotFound) - if the user doesn't own Minecraft
                    if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                        throw new LoginException("You don't own the game.");
                    else
                        throw new LoginException($"MinecraftServices login error ({ex.StatusCode})");
                }

                var loginData = new LoginData
                {
                    PlayerName = profileResponse.Name,
                    Uuid = profileResponse.Id,
                    AccessToken = minecraftResponse.AccessToken,
                    UserType = "msa", //microsoft account
                    ExpiresAt = DateTime.UtcNow + new TimeSpan(0, 0, 1000 * minecraftResponse.ExpiresIn),

                    Xuid = xstsResponseXbox.DisplayClaims.Xui[0].Xid,
                    GamerTag = xstsResponseXbox.DisplayClaims.Xui[0].Gtg
                };
                SaveLoginData(loginData);

                pageControl.SetPageLoggedIn(loginData, offline: false);
            }
            catch (OperationCanceledException)
            {
                await LogoutAsync(pageControl);
            }
        }

        private static void SaveLoginData(LoginData data)
        {
            var json = JsonSerializer.Serialize(data);
            var bytes = Encoding.UTF8.GetBytes(json);
            bytes = Cryptography.Encrypt(bytes);
            GZip.WriteBytesToGZipFile(Path.Combine(Launcher.BaseDir, LoginCacheFile), bytes);
        }

        private static LoginData LoadLoginData()
        {
            try
            {
                var bytes = GZip.ReadBytesFromGZipFile(Path.Combine(Launcher.BaseDir, LoginCacheFile));
                bytes = Cryptography.Decrypt(bytes);
                var json = Encoding.UTF8.GetString(bytes);
                return JsonSerializer.Deserialize<LoginData>(json);
            }
            catch (Exception)
            {
                return null;
            }
        }

        internal static void DeleteLoginData()
        {
            File.Delete(Path.Combine(Launcher.BaseDir, LoginCacheFile));
        }

#pragma warning disable CS0649
        public class XBLResponse //Xbox live response
        {
            [JsonPropertyName("IssueInstant")]
            public DateTime IssueInstant { get; set; }

            [JsonPropertyName("NotAfter")]
            public DateTime NotAfter { get; set; }

            [JsonPropertyName("Token")]
            public string Token { get; set; }

            [JsonPropertyName("DisplayClaims")]
            public XBLDisplayClaims DisplayClaims { get; set; }

            public class XBLDisplayClaims
            {
                [JsonPropertyName("xui")]
                public El[] Xui { get; set; }

                public class El
                {
                    [JsonPropertyName("xid")]
                    public string Xid { get; set; } //xuid (only returned in xstsResponseXbox)

                    [JsonPropertyName("uhs")]
                    public string Uhs { get; set; } //user hash

                    [JsonPropertyName("gtg")]
                    public string Gtg { get; set; } //gamer tag (only returned in xstsResponseXbox)
                }
            }
        }

        public class MinecraftAuthResponse
        {
            [JsonPropertyName("username")]
            public string Username { get; set; }

            //public object[] roles; //no idea what they contain

            [JsonPropertyName("access_token")]
            public string AccessToken { get; set; }

            [JsonPropertyName("token_type")]
            public string TokenType { get; set; }

            [JsonPropertyName("expires_in")]
            public int ExpiresIn { get; set; }
        }

        public class MinecraftProfileResponse
        {
            [JsonPropertyName("id")]
            public string Id { get; set; }

            [JsonPropertyName("name")]
            public string Name { get; set; }

            //there's also data on skins and capes but we don't need it
        }
#pragma warning restore CS0649
    }

    public class LoginData
    {
        //minecraft
        [JsonPropertyName("playername")]
        public string PlayerName { get; set; }

        [JsonPropertyName("uuid")]
        public string Uuid { get; set; }

        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }

        [JsonPropertyName("user_type")]
        public string UserType { get; set; }

        [JsonPropertyName("expiresAt")]
        public DateTime ExpiresAt { get; set; }

        //xbox live
        [JsonPropertyName("xuid")]
        public string Xuid { get; set; }

        [JsonPropertyName("gamerTag")]
        public string GamerTag { get; set; }
    }

    class LoginException : ApplicationException
    {
        public LoginException(string message) : base(message) { }
    }
}
