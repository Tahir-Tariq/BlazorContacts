using BlazorContacts.Server.Paging;
using Entities.Models;
using Entities.RequestParameters;

namespace BlazorContacts.Server.Services
{
    public interface IContactService
    {
        PagedList<Contact> GetContacts(ContactParameters ContactParameters);
    }
}
