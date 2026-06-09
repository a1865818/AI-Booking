using GhedDay.Application.Verticals;
using Microsoft.Extensions.DependencyInjection;

namespace GhedDay.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IVerticalConfigService, VerticalConfigService>();
        return services;
    }
}
