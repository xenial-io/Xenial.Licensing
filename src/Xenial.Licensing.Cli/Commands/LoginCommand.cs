﻿using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xenial.Licensing.Cli.Commands
{

    public class LoginCommand : XenialDefaultCommand
    {
    }

    [XenialCommandHandler("login")]
    public class LoginCommandHandler : XenialCommandHandler<LoginCommand>
    {
        protected override Task<int> ExecuteCommand(LoginCommand arguments)
        {
            Console.WriteLine("Logging in...");
            return Task.FromResult(0);
        }
    }
}