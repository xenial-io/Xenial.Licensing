﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

using Xenial.Licensing.Cli.Services.Queries;
using Xenial.Licensing.Cli.XenialLicenseApi;

using static System.Console;

namespace Xenial.Licensing.Cli.Services.Commands.Default
{
    public class DefaultWelcomeCommand
    {
        private readonly ILicenseQuery licenseQuery;
        private readonly ILicenseClient licenseClient;
        private readonly IDeviceIdProvider deviceIdProvider;
        private readonly IUserInfoProvider userInfoProvider;
        private readonly ILicenseStorage licenseStorage;
        private readonly ILicensePublicKeyStorage publicKeyStorage;
        private readonly ILicenseValidator licenseValidator;
        private readonly ILicenseInformationProvider licenseInformationProvider;

        public DefaultWelcomeCommand(
            ILicenseQuery licenseQuery,
            ILicenseClient licenseClient,
            IDeviceIdProvider deviceIdProvider,
            IUserInfoProvider userInfoProvider,
            ILicenseStorage licenseStorage,
            ILicensePublicKeyStorage publicKeyStorage,
            ILicenseValidator licenseValidator,
            ILicenseInformationProvider licenseInformationProvider
        )
        {
            this.licenseQuery = licenseQuery;
            this.licenseClient = licenseClient;
            this.deviceIdProvider = deviceIdProvider;
            this.userInfoProvider = userInfoProvider;
            this.licenseStorage = licenseStorage;
            this.publicKeyStorage = publicKeyStorage;
            this.licenseValidator = licenseValidator;
            this.licenseInformationProvider = licenseInformationProvider;
        }

        public async Task<int> Execute()
        {
            WriteLine("Fetching user information...");

            var userInfo = await userInfoProvider.GetUserInfoAsync();
            if (userInfo == null)
            {
                WriteLine("ERROR: Cannot fetch user information");
                return -1;
            }

            WriteLine($"Hello {userInfo.UserName}!");
            WriteLine($"Id:    {userInfo.UserId}");
            WriteLine($"Email: {userInfo.Email}");

            WriteLine("Fetching active licenses...");

            var storedLicense = await licenseStorage.FetchAsync();

            WriteLine($"Has stored license...? {!string.IsNullOrEmpty(storedLicense)}");

            var result = await licenseQuery.HasActiveLicense();

            WriteLine($"Has active licenses...? {result}");
            if (string.IsNullOrEmpty(storedLicense) || !result)
            {
                WriteLine($"You don't have a license yet. Do you want to request a trial? Y/n");
                var key = ReadKey();
                if (key.Key == ConsoleKey.Y || key.Key == ConsoleKey.Enter)
                {
                    try
                    {
                        var machineKey = await deviceIdProvider.GetDeviceIdAsync();
                        var trialResult = await licenseClient.LicensesRequestTrialAsync(new InRequestTrialModel(machineKey));
                        WriteLine(FormatXml(trialResult.License));
                        await licenseStorage.StoreAsync(trialResult.License);
                        await publicKeyStorage.StoreAsync(trialResult.PublicKeyName, trialResult.PublicKey);
                    }
                    catch (LicenseApiException<IDictionary<string, object>> ex) when (ex.StatusCode == 400)
                    {
                        WriteLine($"ERROR");
                        foreach (var error in ex.Result)
                        {
                            WriteLine($"{error.Key}: {error.Value}");
                        }
                    }
                    catch (LicenseApiException ex) when (ex.StatusCode == 400 || ex.StatusCode == 500)
                    {
                        WriteLine($"ERROR {ex}");
                    }
                }
            }

            if (await licenseValidator.IsValid())
            {
                WriteLine($"License is valid until {await licenseInformationProvider.IsValidUntil()}");
            }

            return 0;
            static string FormatXml(string xml)
            {
                try
                {
                    var doc = XDocument.Parse(xml);
                    return doc.ToString();
                }
                catch (Exception)
                {
                    // Handle and throw if fatal exception here; don't just ignore them
                    return xml;
                }
            }
        }
    }
}
