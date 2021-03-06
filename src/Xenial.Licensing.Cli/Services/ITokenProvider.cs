﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xenial.Licensing.Cli.Services
{
    public record UserToken(string AccessToken, string RefreshToken, string IdToken, DateTime ExpiresAt);

    public interface ITokenProvider
    {
        Task<UserToken> RefreshTokenAsync();
    }
}
