using BlazorContacts.Server.Context;
using CsvHelper;
using CsvHelper.Configuration;
using Entities.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace BlazorProducts.Server.Context
{
    public static class DbInitializer
    {
        public static IHost CreateDbIfNotExists(this IHost host)
        {
            using (var scope = host.Services.CreateScope())
            {
                using (var appContext = scope.ServiceProvider.GetRequiredService<ContactContext>())
                {
                    try
                    {
                        Initialize(appContext);
                    }
                    catch (Exception ex)
                    {
                        var logger = scope.ServiceProvider.GetRequiredService<ILogger>();
                        logger.LogError(ex, "An error occurred creating the DB.");
                    }
                }
            }

            return host;
        }

        public static void Initialize(ContactContext context)
        {
            context.Database.EnsureCreated();

            // Look for any Contacts.
            if (context.Contacts.Any())
            {
                return;   // DB has been seeded
            }

            IList<Contact> contacts = GetTestContacts("10000 Records_.csv");

            context.Contacts.AddRange(contacts);

            context.SaveChanges();
        }

        public static IList<Contact> GetTestContacts(string filename)
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
            };
            using (var reader = new StreamReader(filename))
            using (var csv = new CsvReader(reader, config))
            {
                return csv.GetRecords<Contact>().ToList();
            }
        }
    }
}
