// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.



// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Identity.API.MainModule.Consent;

namespace Identity.API.MainModule.Device;

public class DeviceAuthorizationViewModel : ConsentViewModel
{
    public string UserCode { get; set; }
    public bool ConfirmUserCode { get; set; }
}