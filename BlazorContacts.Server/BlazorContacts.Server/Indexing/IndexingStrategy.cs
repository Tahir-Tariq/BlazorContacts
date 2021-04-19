using Entities.Models;
using Lucene.Net.Search;
using System.Collections.Generic;

namespace BlazorContacts.Server.Indexing
{
    public interface IndexingStrategy
    {
        void BuildIndex(Lucene.Net.Store.Directory azureDirectory, IEnumerable<Contact> contacts);

        Query ToQuery(string searchTerm);
    }
}