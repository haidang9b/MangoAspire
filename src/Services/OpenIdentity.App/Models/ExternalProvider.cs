// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

namespace OpenIdentity.App.Models;

public class ExternalProvider
{
    public string? DisplayName { get; set; }
    public string AuthenticationScheme { get; set; } = string.Empty;
}
