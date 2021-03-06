﻿using System;
using System.CommandLine;
using System.Net.Http;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using TrueColorConsole;

using Xenial.Licensing.Cli;
using Xenial.Licensing.Cli.Commands;
using Xenial.Licensing.Cli.Services;
using Xenial.Licensing.Cli.Services.Default;
using Xenial.Licensing.Cli.Services.Default.Storage;
using Xenial.Licensing.Cli.Services.Queries;
using Xenial.Licensing.Cli.Services.Queries.Default;
using Xenial.Licensing.Cli.XenialLicenseApi;

var host = CreateHostBuilder(args).Build();

using var serviceScope = host.Services.CreateScope();

var services = serviceScope.ServiceProvider;

try
{
    var rootCommand = services.GetRequiredService<XenialRootCommand>();
    return await rootCommand.RootCommand.InvokeAsync(args);
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("Error Occured {0}", ex);
    Console.ResetColor();
    return -1;
}

static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .ConfigureServices((hostContext, services) =>
        {
            services.AddTransient<IUserProfileProvider, DefaultUserProfileProvider>();

            services.AddTransient<ITokenPathProvider, DefaultPathProvider>();
            services.AddTransient<ITokenStorage, TokenFileStorage>();
            services.AddTransient<DefaultRefreshTokenHandler>();
            services.AddTransient<DefaultConfigurationProvider>();
            services.AddTransient<DefaultDiscoveryProvider>();

            services.AddTransient<IDeviceIdProvider, DefaultDeviceIdProvider>();
            services.AddTransient<IUserInfoProvider, DefaultUserInfoProvider>();
            services.AddTransient<ILicenseValidator, DefaultLicenseValidator>();
            services.AddTransient<ILicenseInformationProvider, DefaultLicenseInformationProvider>();

            services.AddTransient<ILicensePublicKeyStorage, AggregateLicensePublicKeyStorage>();
            services.AddTransient<ILicenseStorage, AggregateLicenseStorage>();

            services.AddTransient<ILicenseQuery, DefaultLicenseQuery>();

            services.AddHttpClient<DefaultTokenProvider>();
            services.AddHttpClient(nameof(LicenseClient));
            services.AddHttpClient<DefaultUserInfoProvider>();
            services.AddTransient<ITokenProvider, DefaultTokenProvider>();
            services.AddTransient<IApiClientConfiguration, DefaultApiClientConfiguration>();

            services.AddTransient<ILicenseClient, LicenseClient>(
                s => new LicenseClient(
                    s.GetRequiredService<IApiClientConfiguration>(),
                    s.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(LicenseClient))
            ));

            services.AddSingleton<XenialRootCommand>();

            services.AddXenialCommands();
        });
