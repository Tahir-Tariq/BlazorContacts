using Entities.Models;
using Microsoft.Azure.Storage;
using System.Collections.Generic;
using System.Linq;

namespace BlazorContacts.Server.Repository.RepositoryExtensions
{
    public static class RepositoryContactExtensions
    {
        public static IList<Contact> Search(this IQueryable<Contact> Contacts, string searchTearm, LuceneService luceneService)
        {
            if (string.IsNullOrWhiteSpace(searchTearm))
                return Contacts.ToList();

            var lowerCaseSearchTerm = searchTearm.Trim().ToLower();

            return luceneService.Search(lowerCaseSearchTerm);

            //return Contacts.Where(p => p.Name.ToLower().Contains(lowerCaseSearchTerm));
        }
    }
}
