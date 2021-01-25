using BlazorContacts.Server.Context.Configuration;
using Entities.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorContacts.Server.Context
{
    public class ContactContext : DbContext
    {
        public ContactContext(DbContextOptions options)
            :base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new ContactConfiguration());
        }

        public DbSet<Contact> Contacts { get; set; }
    }
}
