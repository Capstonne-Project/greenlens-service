using Greenlens.Api.Filters;
using Microsoft.AspNetCore.Mvc;

namespace Greenlens.Api.Extensions;

public static class IdempotencyServiceExtensions
{
    public static IServiceCollection AddGreenlensIdempotency(this IServiceCollection services)
    {
        services.AddScoped<IdempotencyActionFilter>();
        services.Configure<MvcOptions>(options =>
        {
            options.Filters.AddService<IdempotencyActionFilter>();
        });

        return services;
    }
}
