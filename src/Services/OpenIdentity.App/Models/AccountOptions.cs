// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

namespace OpenIdentity.App.Models;

public class AccountOptions
{
    public const bool AllowLocalLogin = true;
    public const bool AllowRememberLogin = true;
    public static TimeSpan RememberMeLoginDuration = TimeSpan.FromDays(30);

    public const bool ShowLogoutPrompt = true;
    public const bool AutomaticRedirectAfterSignOut = false;

    public const string InvalidCredentialsErrorMessage = "Invalid username or password";
}
