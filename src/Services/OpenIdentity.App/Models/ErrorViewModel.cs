namespace OpenIdentity.App.Models;

public class ErrorViewModel
{
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    public ErrorMessage? Error { get; set; }
}

public class ErrorMessage
{
    public string? Error { get; set; }
    public string? ErrorDescription { get; set; }
}
