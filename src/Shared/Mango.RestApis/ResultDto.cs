namespace Mango.RestApis;

public record ResultDto<T>(T? Data, bool IsError = false, string ErrorMessage = default!) where T : class;
