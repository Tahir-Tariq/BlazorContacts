using BlazorContacts.Server.Context;
using Entities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorProducts.Server.Context
{
    public class DbInitializer
    {
        public static void Initialize(ContactContext context)
        {
            //context.Database.EnsureCreated();

            // Look for any Contacts.
            if (context.Contacts.Any())
            {
                return;   // DB has been seeded
            }

            var contacts = new List<Contact>();
            for (int i = 1; i <= 50; i++)
            {                

                contacts.Add(
                    new Contact { Name = $"Person {i}", Email = "abc@yahoo.com", Company = "ITN", Id = i, Role = "IT", PhoneNumber = $"+1 555 111 1111{i}" }
                    );
            }

            //var contacts = new Contact[]
            //{
            //    new Contact { Name = "Person 1",  PhoneNumber = "+1 555 111 1111" },
            //    new Contact { Name = "Person 2",  PhoneNumber = "+1 555 222 2222" },
            //    new Contact { Name = "Person 3",  PhoneNumber = "+1 555 333 3333" },
            //    new Contact { Name = "Person 4",  PhoneNumber = "+1 555 444 4444" },
            //    new Contact { Name = "Person 5",  PhoneNumber = "+1 555 555 5555" },
            //};

            foreach (Contact c in contacts)
            {
                context.Contacts.Add(c);
            }

            context.SaveChanges();                       
        }
    }
}
