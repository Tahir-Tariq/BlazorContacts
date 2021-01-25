using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorContacts.Server.Context.Configuration
{
    public class ContactConfiguration : IEntityTypeConfiguration<Contact>
    {
        public void Configure(EntityTypeBuilder<Contact> builder)
        {
			builder.HasData
			(
				//Mugs
				new Contact
				{
					Id = 1,
					Name = "panolex",
					Email = "panolex@verizon.net",
					Role = "Chief Executive Officer",
					Company = "verizon",
					PhoneNumber = "123456789"
				},
				new Contact
				{
					Id = 2,
					Name = "pavel",
					Email = "pavel@gmail.com",
					Role = "Employee",
					Company = "gmail",
					PhoneNumber = "234567891"
				},
				new Contact
				{
					Id = 3,
					Name = "cisugrad",
					Email = "cisugrad@yahoo.com",
					Role = "Chief Operating Officer",
					Company = "yahoo",
					PhoneNumber = "345678912"
				}
			);
		}
    }
}
