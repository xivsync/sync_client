using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace XIVSync.Interop;

public static class UILoggingProviderExtensions
{
    public static ILoggingBuilder AddUILogging(this ILoggingBuilder builder, UILoggingProvider? provider = null)
    {
        if (provider != null)
        {
            // Use the provided provider instance
            builder.AddProvider(provider);
        }
        else
        {
            // Create and register a new provider
            var newProvider = new UILoggingProvider();
            builder.Services.AddSingleton(newProvider);
            builder.AddProvider(newProvider);
        }
        return builder;
    }
}

