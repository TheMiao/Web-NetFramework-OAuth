﻿using System;
using System.Configuration;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using Microsoft.Owin.Security.Notifications;
using Owin;
using System.IdentityModel.Claims;
using Web_NetFramework.TokenStorage;
using Microsoft.Identity.Client;
using System.Web;

[assembly: OwinStartup(typeof(Web_NetFramework.Startup))]

namespace Web_NetFramework
{
    public class Startup
    {
        private static string ClientID = ConfigurationManager.AppSettings["ClientID"];
        private static string ClientSecret = ConfigurationManager.AppSettings["ClientSecret"];
        private static string RedirectUri = ConfigurationManager.AppSettings["RedirectUri"];
        private static string Tenant = ConfigurationManager.AppSettings["Tenant"];
        private static string EndPointUri = ConfigurationManager.AppSettings["EndPointUri"];
        private static string EndPointVersion = ConfigurationManager.AppSettings["EndPointVersion"];

        private static string GraphScopes = ConfigurationManager.AppSettings["GraphScope"];


        public void Configuration(IAppBuilder app)
        {
            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);
            app.UseCookieAuthentication(new CookieAuthenticationOptions());

            app.UseOpenIdConnectAuthentication(
                new OpenIdConnectAuthenticationOptions
                {
                    ClientId = ClientID,
                    Authority = EndPointUri + Tenant + "/" + EndPointVersion,
                    PostLogoutRedirectUri = RedirectUri,
                    RedirectUri = RedirectUri,
                    Scope = "openid email profile offline_access " + GraphScopes,
                    //ResponseType = OpenIdConnectResponseType.IdToken,
                    TokenValidationParameters = new TokenValidationParameters()
                    {
                        ValidateIssuer = false
                    },
                    Notifications = new OpenIdConnectAuthenticationNotifications
                    {
                        AuthenticationFailed = OnAuthenticationFailedAsync,
                        AuthorizationCodeReceived = AuthorizationCodeReceivedAsync
                    }
                });
        }
        /// <summary>
        /// 认证失败
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private Task OnAuthenticationFailedAsync(AuthenticationFailedNotification<OpenIdConnectMessage,OpenIdConnectAuthenticationOptions> context)
        {
            context.HandleResponse();
            context.Response.Redirect("/?errormessage=" + context.Exception.Message);
            return Task.FromResult(0);
        }
        /// <summary>
        /// 接收到Code
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private async Task AuthorizationCodeReceivedAsync(AuthorizationCodeReceivedNotification context)
        {
            var code = context.Code;
            var signedInUserID = context.AuthenticationTicket.Identity.FindFirst(ClaimTypes.NameIdentifier).Value;
            TokenCache tokenCache = new TokenSessionCache(signedInUserID, 
                context.OwinContext.Environment["System.Web.HttpContextBase"] as HttpContextBase).GetMsalCacheInstance();
            ConfidentialClientApplication confidential = new ConfidentialClientApplication(
                ClientID,
                RedirectUri,
                new ClientCredential(ClientSecret),
                tokenCache,
                null);
            string[] scopes = GraphScopes.Split(new char[] { ' ' });
            AuthenticationResult result = await confidential.AcquireTokenByAuthorizationCodeAsync(code, scopes);
        }
    }
}
