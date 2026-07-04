namespace ActionOrbit.App.Services.Actions;

public sealed record ActionExecutionResult(bool Succeeded, string Message)
{
    public static ActionExecutionResult Success(string message = "OK") => new(true, message);
    public static ActionExecutionResult Failure(string message) => new(false, message);
}
