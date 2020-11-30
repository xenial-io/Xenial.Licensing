﻿using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using IdentityModel.Client;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Xenial.Licensing.Cli.Services;
using Xenial.Licensing.Cli.Services.Default;
using Xenial.Licensing.Cli.Utils;

namespace Xenial.Licensing.Cli.Commands
{
    public class LoginCommand : XenialDefaultCommand
    {
        public override IEnumerable<Option> CreateOptions()
            => base.CreateOptions()
            .Concat(new Option[]
            {
                new Option(new[] { "--no-cache" }, "Login will not use cached arguments")
                {
                    Argument = new Argument<bool>()
                },
                new Option(new[] { "--no-store" }, "Login will not store credentials")
                {
                    Argument = new Argument<bool>()
                }
            });

        public bool NoCache { get; set; }
        public bool NoStore { get; set; }
    }

    [XenialCommandHandler("login")]
    public class LoginCommandHandler : XenialCommandHandler<LoginCommand>
    {
        private readonly HttpClient httpClient;
        private readonly IUserProfileProvider userProfileProvider;
        private readonly ILogger<LoginCommandHandler> logger;
        private readonly ITokenStorage tokenStorage;
        private readonly DefaultConfigurationProvider configurationProvider;
        private readonly DefaultDiscoveryProvider discoveryProvider;
        private readonly DefaultRefreshTokenHandler refreshTokenHandler;

        public LoginCommandHandler(
            HttpClient httpClient,
            IUserProfileProvider userProfileProvider,
            ILogger<LoginCommandHandler> logger,
            ITokenStorage tokenStorage,
            DefaultConfigurationProvider configurationProvider,
            DefaultDiscoveryProvider discoveryProvider,
            DefaultRefreshTokenHandler refreshTokenHandler
        )
        {
            this.httpClient = httpClient;
            this.userProfileProvider = userProfileProvider;
            this.logger = logger;
            this.tokenStorage = tokenStorage;
            this.configurationProvider = configurationProvider;
            this.discoveryProvider = discoveryProvider;
            this.refreshTokenHandler = refreshTokenHandler;
        }

        protected async override Task<int> ExecuteCommand(LoginCommand arguments)
        {
            Console.WriteLine("Logging in...");
            if (!arguments.NoCache)
            {
                var userToken = await tokenStorage.LoadUserTokenAsync();
                if (userToken != null)
                {
                    userToken = await refreshTokenHandler.RefreshTokenAsync(userToken);
                    if (userToken != null)
                    {
                        await StoreToken(arguments, userToken);
                        return 0;
                    }
                }
            }

            var disco = await discoveryProvider.FetchDiscoveryDocument();

            var result = await httpClient.RequestDeviceAuthorizationAsync(new DeviceAuthorizationRequest
            {
                Address = disco.DeviceAuthorizationEndpoint,
                ClientId = configurationProvider.ClientId,
                Scope = configurationProvider.Scope
            });

            if (result.IsError)
            {
                Console.WriteLine(result.Error);
                throw new Exception(result.Error);
            }

            Console.WriteLine($"Visit: {result.VerificationUri}");
            Console.WriteLine();
            Console.WriteLine("And enter this code");
            Console.WriteLine("-------------------");
            Console.WriteLine($"-    {result.UserCode}    -");
            Console.WriteLine("-------------------");

            var fetchToken = true;
            var interval = (result.Interval == 0 ? 5 : result.Interval) * 1000;
            var spinner = new ConsoleSpinner();
            while (fetchToken)
            {
                spinner.Turn();
                Console.Write(" Fetching token....");
                var tokenResponse = await httpClient.RequestDeviceTokenAsync(new DeviceTokenRequest
                {
                    Address = disco.TokenEndpoint,
                    ClientId = configurationProvider.ClientId,
                    DeviceCode = result.DeviceCode,
                });

                if (tokenResponse.IsError)
                {
                    if (tokenResponse.Error == "authorization_pending" || tokenResponse.Error == "slow_down")
                    {
                        await Task.Delay(interval);
                    }
                    else
                    {
                        spinner.ClearLine();
                        Console.WriteLine(tokenResponse.Error);
                        throw new Exception(tokenResponse.Error);
                    }
                }
                else
                {
                    var userToken = new UserToken(
                        tokenResponse.AccessToken,
                        tokenResponse.RefreshToken,
                        tokenResponse.IdentityToken,
                        DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn)
                    );

                    spinner.ClearLine();
                    await StoreToken(arguments, userToken);
                    return 0;
                }
            }

            return 0;
        }

        private async Task StoreToken(LoginCommand arguments, UserToken userToken)
        {
            if (arguments.NoStore)
            {
                return;
            }
            await tokenStorage.StoreAsync(userToken);
        }
    }
}
