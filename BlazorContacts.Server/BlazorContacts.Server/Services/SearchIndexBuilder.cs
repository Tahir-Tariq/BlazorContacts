using BlazorContacts.Server.Context;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;

namespace BlazorContacts.Server.Services
{
    public static class SearchIndexBuilder
    {
        public static IHost BuildIndexIfNotExists(this IHost host)
        {
            using (var scope = host.Services.CreateScope())
            {
                try
                {
                    var luceneService = scope.ServiceProvider.GetRequiredService<LuceneService>();

                    if (!luceneService.IsIndexed())
                    {
                        using (var appContext = scope.ServiceProvider.GetRequiredService<ContactContext>())
                        {
                            luceneService.BuildIndex(appContext.Contacts);
                        }
                    }
                }
                catch (Exception ex)
                {
                    var logger = host.Services.GetRequiredService<ILogger>();
                    logger.LogError(ex, "An error occurred creating the DB.");
                }
            }
            return host;
        }
    }
}
