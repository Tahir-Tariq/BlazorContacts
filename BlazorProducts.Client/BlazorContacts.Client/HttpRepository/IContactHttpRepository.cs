using BlazorContacts.Client.Features;
using Entities.Models;
using Entities.RequestParameters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorContacts.Client.HttpRepository
{
    public interface IContactHttpRepository
    {
        Task<PagingResponse<Contact>> GetContacts(ContactParameters ContactParameters);
    }
}
