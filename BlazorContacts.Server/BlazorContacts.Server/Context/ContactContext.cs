using BlazorContacts.Server.Context.Configuration;
using Entities.Models;
using Microsoft.EntityFrameworkCore;

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
