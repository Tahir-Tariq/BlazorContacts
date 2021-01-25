using BlazorContacts.Server.Context;
using BlazorContacts.Server.Paging;
using BlazorContacts.Server.Repository.RepositoryExtensions;
using Entities.Models;
using Entities.RequestParameters;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorContacts.Server.Repository
{
    public class ContactRepository : IContactRepository
    {
        private readonly ContactContext _context;
        private readonly LuceneService _luceneService;
        public ContactRepository(ContactContext context, LuceneService luceneService)
        {
            _context = context;
            _luceneService = luceneService;
        }

        public PagedList<Contact> GetContacts(ContactParameters ContactParameters)
        {
            var Contacts = _context.Contacts
                .Search(ContactParameters.SearchTerm, _luceneService);                

            return PagedList<Contact>
                .ToPagedList(Contacts, ContactParameters.PageNumber, ContactParameters.PageSize);
        }
    }
}
