using System;
using System.Collections.Generic;
using System.Text;

namespace Entities.Models
{
    public class Contact
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Company { get; set; }
        public string Role { get; set; }
        public string PhoneNumber { get; set; }      
    }
}
