using IdentityModel;
using IdentityServer4.Models;
using IdentityServer4.Test;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace Osmos.Business.WebApi.Models
{
    public static class IdentityServerConfig
    {
        public static IEnumerable<ApiScope> ApiScopes =>
            new ApiScope[]
            {
                new ApiScope("api")
            };

        public static IEnumerable<IdentityResource> GetIdentityResources()
        {
            return new List<IdentityResource>
            {
                new IdentityResources.OpenId(),
                new IdentityResources.Profile()
            };
        }

        public static IEnumerable<ApiResource> GetApiResources()
        {
            return new List<ApiResource>
                        {
                            new ApiResource("api", "Resource API"){
                                UserClaims = {"role"},
                                ApiSecrets = new List<Secret>{
                                    new Secret("secret".Sha256())
                                },
                                Enabled = true,
                                Scopes = new string[]{"api" }
                            }
                        };
        }

        public static IEnumerable<Client> Clients => _clients;

        public static IEnumerable<Client> _clients = new Client[] { };

        public static string ClientSecret { get; set; }

        public static List<TestUser> Users
        {
            get
            {
                return _users;
            }
        }

        private static List<TestUser> _users = new List<TestUser>();
        public static void Init()
        {
            _users = new List<TestUser>
                {
                    new TestUser
                    {
                        SubjectId = Guid.NewGuid().ToString(),
                        Username = _RandomString(16),
                        Password = _RandomString(32),
                        Claims =
                        {
                            new Claim(JwtClaimTypes.Name, "")
                        }
                    }
                };


            ClientSecret = _RandomString(32);

            _clients = new Client[]
            {
                new Client
                    {
                        ClientId = Guid.NewGuid().ToString(),
                        AllowedGrantTypes = new[] {
                            GrantType.ResourceOwnerPassword,
                            "external"
                        },

                        ClientSecrets =
                        {
                            new Secret(ClientSecret.Sha256())
                        },
                        AllowedScopes = { "api" },
                        //AccessTokenLifetime = _accessTokenLifetime,
                        AccessTokenType = AccessTokenType.Reference,
                        AccessTokenLifetime = 3600 * 24
                    }
            };
        }

        private const string _chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        private static readonly Random _random = new Random();
        private static string _RandomString(int length)
        {
            return new string(
                Enumerable.Repeat(_chars, length)
                          .Select(s => s[_random.Next(s.Length)])
                          .ToArray());
        }
    }

    public class AppIdentityOptions
    {
        public string IssuerUri { get; set; }
        public string Authority { get; set; }
    }
}
