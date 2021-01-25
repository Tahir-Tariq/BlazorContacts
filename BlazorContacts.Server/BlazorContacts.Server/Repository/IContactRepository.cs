using BlazorContacts.Server.Paging;
using Entities.Models;
using Entities.RequestParameters;
using System.Threading.Tasks;

namespace BlazorContacts.Server.Repository
{
    public interface IContactRepository
    {
        PagedList<Contact> GetContacts(ContactParameters ContactParameters);
    }
}
