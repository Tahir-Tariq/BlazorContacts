using BlazorContacts.Server.Context;
using BlazorContacts.Server.Paging;
using Entities.Models;
using Entities.RequestParameters;
using System.Collections.Generic;
using System.Linq;

namespace BlazorContacts.Server.Services
{
    public class ContactService : IContactService
    {
        private readonly ContactContext _context;
        private readonly LuceneService _luceneService;
        public ContactService(ContactContext context, LuceneService luceneService)
        {
            _context = context;
            _luceneService = luceneService;
        }

        public PagedList<Contact> GetContacts(ContactParameters contactParameters)
        {
            return contactParameters.HasSearchTerm ?
                     SearchContacts(contactParameters) :
                     GetContactList(contactParameters);
        }
        
        public PagedList<Contact> GetContactList(ContactParameters contactParameters)
        {
            return PagedList<Contact>
               .ToPagedList(_context.Contacts, contactParameters.PageNumber, contactParameters.PageSize);
        }

        public PagedList<Contact> SearchContacts(ContactParameters contactParameters)
        {
            var lowerCaseSearchTerm = contactParameters.SearchTerm.Trim().ToLower();

            return _luceneService.Search(lowerCaseSearchTerm, contactParameters.PageNumber, contactParameters.PageSize);
        }
    }
}
