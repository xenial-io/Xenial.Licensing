﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DevExpress.Xpo;

using Xenial.Licensing.Model;
using Xenial.Licensing.Model.Infrastructure;

namespace Xenial.Licensing.Domain.Commands
{
    public record TrialRequestCommand(string UserId, string MachineKey, int? DefaultTrialCooldown, int? DefaultTrialPeriod);
    public record TrialRequestResult(Guid Id, string License, DateTime ExpiresAt);

    public class TrialRequestCommandHandler
    {
        private readonly UnitOfWork unitOfWork;

        public TrialRequestCommandHandler(UnitOfWork unitOfWork)
            => this.unitOfWork = unitOfWork;

        public async Task<TrialRequestResult> ExecuteAsync(TrialRequestCommand command)
        {
            var settings = (await unitOfWork.GetSingletonAsync<LicenseSettings>()) ?? throw new ArgumentNullException(nameof(LicenseSettings));

            if (!command.DefaultTrialCooldown.HasValue)
            {
                command = command with { DefaultTrialCooldown = settings.DefaultTrialCooldown };
            }
            if (!command.DefaultTrialPeriod.HasValue)
            {
                command = command with { DefaultTrialPeriod = settings.DefaultTrialPeriod };
            }

            var cooldownTime = DateTime.Today.AddDays(command.DefaultTrialCooldown.Value * -1);

            var trialRequest = await unitOfWork.Query<TrialRequest>()
                   .Where(trial => trial.UserId == command.UserId && trial.RequestDate > cooldownTime)
                   .OrderByDescending(trial => trial.RequestDate)
                   .FirstOrDefaultAsync();

            if (trialRequest != null)
            {
                var existingTrialResult = await FetchExistingTrial(command);
                if (existingTrialResult != null)
                {
                    return existingTrialResult;
                }
            }

            var trialRequests = await unitOfWork.Query<TrialRequest>()
                .Where(trial => trial.MachineKey == command.MachineKey && trial.RequestDate > cooldownTime)
                .OrderByDescending(trial => trial.RequestDate)
                .Take(2)
                .ToListAsync();

            if (trialRequests.Count >= 2)
            {
                //We allow a second trial with a different email from the same machine
                //We have a third trial request on the same machine with a different email
                throw new InvalidOperationException("You cannot request a new trial, please contact support");
            }

            var newLicense = await RequestTrial(command);
            if (newLicense == null)
            {
                throw new ArgumentNullException(nameof(newLicense));
            }
            return newLicense;
        }

        private async Task<TrialRequestResult> RequestTrial(TrialRequestCommand command)
        {
            var trialRequest = new TrialRequest(unitOfWork)
            {
                MachineKey = command.MachineKey,
                UserId = command.UserId
            };
            var user = await unitOfWork.GetObjectByKeyAsync<CompanyUser>(command.UserId);
            if (user == null)
            {
                user = new CompanyUser(unitOfWork)
                {
                    Id = command.UserId,
                    IsAutogenerated = true,
                    Company = new Company(unitOfWork)
                    {
                        Id = command.UserId,
                        IsAutogenerated = true
                    }
                };
            }

            var license = new License(unitOfWork)
            {
                User = user,
            };

            await unitOfWork.SaveAsync(trialRequest);
            await unitOfWork.SaveAsync(license);
            await unitOfWork.CommitChangesAsync();

            if (!license.ExpiresAt.HasValue)
            {
                throw new ArgumentException($"License must have {nameof(license.ExpiresAt)} set for a trial request.");
            }

            return new TrialRequestResult(license.Id, license.GeneratedLicense.ToString(), license.ExpiresAt.Value);
        }

        private async Task<TrialRequestResult> FetchExistingTrial(TrialRequestCommand command)
        {
            var licenses = await unitOfWork.Query<License>()
               .Where(l =>
                   l.User != null
                   && l.User.Id == command.UserId
                   && l.Type == License.LicenseType.Trial
                   && (l.ExpiresAt.HasValue || l.ExpiresNever)
               )
               .ToListAsync();

            if (licenses.Any(l => l.ExpiresNever))
            {
                var neverExpireTrial = licenses.First(l => l.ExpiresNever);
                return new TrialRequestResult(neverExpireTrial.Id, neverExpireTrial.GeneratedLicense.ToString(), DateTime.MaxValue);
            }
            else
            {
                var expireTrial = licenses
                    .Where(l => l.ExpiresAt.HasValue)
                    .OrderBy(l => l.ExpiresAt.Value)
                    .FirstOrDefault();

                if (expireTrial != null)
                {
                    return new TrialRequestResult(expireTrial.Id, expireTrial.GeneratedLicense.ToString(), expireTrial.ExpiresAt.Value.ToUniversalTime());
                }
            }

            return null;
        }
    }
}
