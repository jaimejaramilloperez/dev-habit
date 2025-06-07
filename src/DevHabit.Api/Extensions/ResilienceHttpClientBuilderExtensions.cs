using Microsoft.Extensions.Http.Resilience;

namespace DevHabit.Api.Extensions;

public static class ResilienceHttpClientBuilderExtensions
{
    public static IHttpClientBuilder InternalRemoveAllResilienceHandlers(this IHttpClientBuilder builder)
    {
        builder.ConfigureAdditionalHttpMessageHandlers(delegate (IList<DelegatingHandler> handlers, IServiceProvider _)
        {
            for (int num = handlers.Count - 1; num >= 0; num--)
            {
                if (handlers[num] is ResilienceHandler)
                {
                    handlers.RemoveAt(num);
                }
            }
        });

        return builder;
    }
}
