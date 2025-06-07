namespace DevHabit.Api.Services;

public sealed class DelayHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        await Task.Delay(10000, cancellationToken);
        return await base.SendAsync(request, cancellationToken);
    }
}
