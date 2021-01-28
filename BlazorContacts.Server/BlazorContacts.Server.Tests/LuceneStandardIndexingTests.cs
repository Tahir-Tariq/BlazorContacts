using CsvHelper;
using CsvHelper.Configuration;
using Entities.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Lucene.Net.Analysis.NGram;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Microsoft.Azure.Storage;
using System.Globalization;
using System.Linq;
using System.Collections.Generic;
using Lucene.Net.Util;
using Lucene.Net.QueryParsers.Classic;
using Xunit;
using Lucene.Net.Store.Azure;
using Lucene.Net.Store;

namespace BlazorContacts.Server.Tests
{
    public class LuceneStandardIndexingTests
    {
        const LuceneVersion AppLuceneVersion = LuceneVersion.LUCENE_48;
        static Dictionary<int, Contact> ContactsHash = null;

        public LuceneStandardIndexingTests()
        {
            ContactsHash = ContactsHash ?? GetTestContacts().ToDictionary(con => con.Id);
        }

        public IList<Contact> GetTestContacts()
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
            };
            using (var reader = new StreamReader("Contacts.csv"))
            using (var csv = new CsvReader(reader, config))
            {
                return csv.GetRecords<Contact>().ToList();
            }
        }

        private void BuildIndex(AzureDirectory azureDirectory, IEnumerable<Contact> contacts)
        {
            var analyzer = new StandardAnalyzer(AppLuceneVersion);
            var indexConfig = new IndexWriterConfig(AppLuceneVersion, analyzer);
            using (var writer = new IndexWriter(azureDirectory, indexConfig))
            {
                foreach (var contact in contacts)
                {
                    Document doc = new Document();
                    doc.Add(new StringField("Id", contact.Id.ToString(), Field.Store.YES));
                    doc.Add(new TextField("Name", contact.Name, Field.Store.YES));
                    doc.Add(new TextField("Email", contact.Email, Field.Store.YES));

                    doc.Add(new StringField("Company", contact.Company, Field.Store.YES));
                    doc.Add(new StringField("Role", contact.Role, Field.Store.YES));
                    doc.Add(new StringField("PhoneNumber", contact.PhoneNumber, Field.Store.YES));

                    doc.Add(new TextField("Name-Terms", new NGramTokenizer(AppLuceneVersion, new StringReader(contact.Name), 2, 12)));

                    doc.Add(new TextField("Email-Terms", new NGramTokenizer(AppLuceneVersion, new StringReader(contact.Email), 2, 12)));

                    doc.Add(new TextField("Company-Terms", new NGramTokenizer(AppLuceneVersion, new StringReader(contact.Company), 2, 12)));

                    doc.Add(new TextField("Role-Terms", new NGramTokenizer(AppLuceneVersion, new StringReader(contact.Role), 2, 12)));

                    doc.Add(new TextField("PhoneNumber-Terms", new NGramTokenizer(AppLuceneVersion, new StringReader(contact.PhoneNumber), 2, 4)));

                    writer.AddDocument(doc);
                }
                writer.Flush(triggerMerge: false, applyAllDeletes: false);
            }
        }

        private Query ToQuery(string searchTerm)
        {
            return MultiFieldQueryParser.Parse
                (
                    AppLuceneVersion,
                    searchTerm,
                    new string[] { "Name", "Email", "Company", "Role", "PhoneNumber", "Name-Terms", "Email-Terms", "Company-Terms", "Role-Terms", "PhoneNumber-Terms" },
                    new Occur[] { Occur.SHOULD, Occur.SHOULD, Occur.SHOULD, Occur.SHOULD, Occur.SHOULD, Occur.SHOULD, Occur.SHOULD, Occur.SHOULD, Occur.SHOULD, Occur.SHOULD },
                    new StandardAnalyzer(AppLuceneVersion)
                );
        }

        [Theory]
        [InlineData("ste", 7)]
        [InlineData("benjamin", 1)]
        [InlineData("reb", 1)]
        [InlineData("yahoo", 13)]
        [InlineData("gmail", 33)]
        [InlineData("225", 6)]
        [InlineData("BrendaRobinson", 1)]        
        public void TestStandardSearch(string searchTerm, int expectedCount)
        {
            var cloudStorageAccount = CloudStorageAccount.DevelopmentStorageAccount;

            const string containerName = "standardsearchcontacts";

            var azureDirectory = new AzureDirectory(cloudStorageAccount, containerName, new RAMDirectory());

            if (!azureDirectory.ListAll().Any())
            {
                BuildIndex(azureDirectory, ContactsHash.Values);
            }

            var query = ToQuery(searchTerm);
            try
            {
                var ireader = DirectoryReader.Open(azureDirectory);
                StringBuilder result = new StringBuilder();
                var searcher = new IndexSearcher(ireader);
                var topDocs = searcher.Search(query, 100);

                List<int> ids = new List<int>();
                foreach (var hit in topDocs.ScoreDocs)
                {
                    var doc = searcher.Doc(hit.Doc);
                    ids.Add(int.Parse(doc.Get("Id")));
                    result.AppendLine(doc.Get("Id"));
                }

                Trace.TraceInformation("Search Term:'{0}' returned these Ids:\n{1}", searchTerm, result.ToString());                

                Assert.Equal(expectedCount, topDocs.TotalHits);
            }          
            finally
            {
                if (azureDirectory.BlobContainer.Exists())
                {
                    azureDirectory.BlobContainer.Delete();
                }
            }
        }

        private int SearchContains(string searchTerm)
        {
            return ContactsHash.Values.Where(con =>
            con.Email.Contains(searchTerm) || con.Company.Contains(searchTerm)
                    || con.Name.Contains(searchTerm) || con.PhoneNumber.Contains(searchTerm)
                    || con.Role.Contains(searchTerm)
            ).Count();            
        }

        private int MatchSearchContains(string searchTerm, IEnumerable<int> ids)
        {
            var c = from conKV in ContactsHash
                    join id in ids on conKV.Key equals id
                    let con = conKV.Value
                    where con.Email.Contains(searchTerm) || con.Company.Contains(searchTerm)
                    || con.Name.Contains(searchTerm) || con.PhoneNumber.Contains(searchTerm)
                    || con.Role.Contains(searchTerm)
                    select 1;

            return c.Sum();
        }
    }
}
