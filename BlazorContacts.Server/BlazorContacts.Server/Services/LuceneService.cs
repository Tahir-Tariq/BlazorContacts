using Entities.Models;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using System.Collections.Generic;
using Microsoft.Azure.Storage;
using System;
using System.Linq;
using Lucene.Net.Store.Azure;
using BlazorContacts.Server.Paging;
using BlazorContacts.Server.Indexing;

namespace BlazorContacts.Server.Services
{
    public class LuceneService
    {        
        private int MaxSearchRecords = 1000;// just an arbitrary number
        private Directory _azureDirectory;
        private IndexingStrategy _indexer;
        public LuceneService(CloudStorageAccount storageAccount, string catalog, IndexingStrategy indexingStrategy)
        {
            storageAccount = storageAccount ?? throw new ArgumentNullException(nameof(storageAccount));
            _indexer = indexingStrategy ?? throw new ArgumentNullException(nameof(indexingStrategy));

            if (string.IsNullOrWhiteSpace(catalog))
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            _azureDirectory = new AzureDirectory(storageAccount, catalog, new RAMDirectory());            
        }

        public bool IsIndexed()
            => _azureDirectory.ListAll().Any();

        public void BuildIndex(IEnumerable<Contact> contacts)
            => _indexer.BuildIndex(_azureDirectory, contacts);

        private Query ToQuery(string searchTerm)
            => _indexer.ToQuery(searchTerm);        

        public PagedList<Contact> Search(string searchTerm, int pageNumber, int pageSize)
        { 
            using (var reader = DirectoryReader.Open(_azureDirectory))
            {
                var searcher = new IndexSearcher(reader);

                var query = ToQuery(searchTerm);

                var collector = TopScoreDocCollector.Create(MaxSearchRecords, true);

                int startIndex = (pageNumber - 1) * pageSize;

                searcher.Search(query, collector);

                TopDocs topDocs = collector.GetTopDocs(startIndex, pageSize);

                var contacts = ToContacts(searcher, topDocs);                
               
                return new PagedList<Contact>(contacts.ToList(), topDocs.TotalHits, pageNumber, pageSize);
            }
        }

        private IEnumerable<Contact> ToContacts(IndexSearcher searcher, TopDocs topDocs)
            => topDocs.ScoreDocs.Select(hit => ToModel(searcher.Doc(hit.Doc)));

        private Contact ToModel(Document doc)
        {
            return new Contact
            {
                Id = int.Parse(doc.Get("Id")),
                Name = doc.Get("Name"),
                Email = doc.Get("Email"),
                Company = doc.Get("Company"),
                Role = doc.Get("Role"),
                PhoneNumber = doc.Get("PhoneNumber")
            };
        }
    }
}
