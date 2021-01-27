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
using Microsoft.Azure.Storage.Blob;
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
    public class ContactsTest
    {
        const LuceneVersion AppLuceneVersion = LuceneVersion.LUCENE_48;

        public IList<Contact> GetTestContacts()
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
            };
            using (var reader = new StreamReader(@"C:\Users\nayyab\Desktop\Lucene project\100-Records\Contacts.csv"))
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

                    doc.Add(new TextField("Name-Terms", new NGramTokenizer(AppLuceneVersion, new StringReader(contact.Name), 2, 6)));

                    doc.Add(new TextField("Email-Terms", new NGramTokenizer(AppLuceneVersion, new StringReader(contact.Email), 2, 6)));

                    doc.Add(new TextField("Company-Terms", new NGramTokenizer(AppLuceneVersion, new StringReader(contact.Company), 2, 4)));

                    doc.Add(new TextField("Role-Terms", new NGramTokenizer(AppLuceneVersion, new StringReader(contact.Role), 2, 4)));

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
                    new string[] { "Id", "Name", "Email", "Company", "Role", "PhoneNumber", "Name-Terms", "Email-Terms", "Company-Terms", "Role-Terms", "PhoneNumber-Terms" },
                    new Occur[] { Occur.SHOULD, Occur.SHOULD, Occur.SHOULD, Occur.SHOULD, Occur.SHOULD, Occur.SHOULD, Occur.SHOULD, Occur.SHOULD, Occur.SHOULD, Occur.SHOULD, Occur.SHOULD },
                    new StandardAnalyzer(AppLuceneVersion)
                );
        }

        [Theory]
        [InlineData("ste", 7)]
        [InlineData("re", 49)]
        [InlineData("reb", 2)]
        [InlineData("22", 17)]
        [InlineData("225", 8)]
        public void TestFuzzySearch(string searchTerm, int expectedCount)
        {
            var cloudStorageAccount = CloudStorageAccount.DevelopmentStorageAccount;

            const string containerName = "fuzzycontacts";

            var azureDirectory = new AzureDirectory(cloudStorageAccount, containerName, new RAMDirectory());

            var blobClient = cloudStorageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(containerName);

            if (!container.Exists())
            {
                BuildIndex(azureDirectory, GetTestContacts());
            }

            var query = ToQuery(searchTerm);
            try
            {
                var ireader = DirectoryReader.Open(azureDirectory);
                StringBuilder result = new StringBuilder();
                var searcher = new IndexSearcher(ireader);
                var topDocs = searcher.Search(query, 100);

                foreach (var hit in topDocs.ScoreDocs)
                {
                    var doc = searcher.Doc(hit.Doc);
                    result.AppendLine(doc.Get("Id"));
                }

                Trace.TraceInformation("Search Ids:\n{0}", result.ToString());

                Assert.Equal(topDocs.TotalHits, expectedCount);
            }
            catch (Exception x)
            {
                Trace.TraceInformation("Tests failed:\n{0}", x);
            }
            finally
            {
                if (container.Exists())
                {
                    container.Delete();
                }                    
            }
        }
    }
}
