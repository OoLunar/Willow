using System;
using System.Collections.Generic;
using OoLunar.Willow.Models;

namespace OoLunar.Willow.Payloads
{
    public class WelcomePayload
    {
        public readonly UserModel User;
        public readonly LoginModel? LastSuccessfulLogin;
        public readonly IEnumerable<CommandModel> Commands;

        public WelcomePayload(UserModel user, LoginModel? lastSuccessfulLogin, IEnumerable<CommandModel> commands)
        {
            User = user ?? throw new ArgumentNullException(nameof(user));
            LastSuccessfulLogin = lastSuccessfulLogin;
            Commands = commands;
        }
    }
}
