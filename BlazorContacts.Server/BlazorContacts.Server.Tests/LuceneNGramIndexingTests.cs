using CsvHelper;
using CsvHelper.Configuration;
using Entities.Models;
using System;
using System.Linq;
using System.Diagnostics;
using System.IO;
using System.Text;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Microsoft.Azure.Storage;
using System.Globalization;
using System.Collections.Generic;
using Lucene.Net.Util;
using Lucene.Net.QueryParsers.Classic;
using Xunit;
using Lucene.Net.Store.Azure;
using Lucene.Net.Store;

namespace BlazorContacts.Server.Tests
{
    public class LuceneNGramFixture : IDisposable
    {
        public StringBuilder Log;
        public Lucene.Net.Store.Directory LuceneDirectory;
        public LuceneVersion AppLuceneVersion = LuceneVersion.LUCENE_48;
        public LuceneNGramFixture()
        {
            Log = new StringBuilder();
            Log.Insert(0, "*",50);
            LuceneDirectory =  PrepreDirectory();
        }

        private Lucene.Net.Store.Directory PrepreDirectory()
        {
            var cloudStorageAccount = CloudStorageAccount.DevelopmentStorageAccount;

            const string containerName = "ngramcontacts";

            var azureDirectory = new AzureDirectory(cloudStorageAccount, containerName, new RAMDirectory());

            if (azureDirectory.BlobContainer.Exists())
                azureDirectory.BlobContainer.Delete();

            var sw = new Stopwatch();
            sw.Start();
            BuildIndex(azureDirectory, GetTestContacts());
            sw.Stop();

            var containerSize = azureDirectory.ListAll().Select(file => azureDirectory.FileLength(file)).Sum();

            Log.AppendLine($"Index container size {containerSize} built in {TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds)} | {sw.ElapsedMilliseconds}");

            return azureDirectory;
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

        private void BuildIndex(Lucene.Net.Store.Directory azureDirectory, IEnumerable<Contact> contacts)
        {
            var analyzer = new NGramAnalyzer(AppLuceneVersion, 2, 6);
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

                    writer.AddDocument(doc);
                }
                writer.Flush(triggerMerge: false, applyAllDeletes: false);
            }
        }

        public void Dispose()
        {
            Debug.WriteLine(Log.ToString());
        }
    }

    public class LuceneNGramIndexingTests : Xunit.IClassFixture<LuceneNGramFixture>
    {
        LuceneNGramFixture Fixture;
        
        public LuceneNGramIndexingTests(LuceneNGramFixture fixture)
        {
            Fixture = fixture;
        }                  

        private Query ToQuery(string searchTerm)
        {
            return MultiFieldQueryParser.Parse
                (
                    Fixture.AppLuceneVersion,
                    searchTerm,
                    new string[] { "Name", "Email", "Company", "Role", "PhoneNumber" },
                    new Occur[] { Occur.SHOULD, Occur.SHOULD, Occur.SHOULD, Occur.SHOULD, Occur.SHOULD },
                    new NGramAnalyzer(Fixture.AppLuceneVersion, 2, 6)
                );
        }

        [Theory]
        [InlineData("ste", 16)]
        [InlineData("benjamin", 41)]
        [InlineData("reb", 21)]
        [InlineData("yahoo", 38)]
        [InlineData("henderson", 63)]
        [InlineData("gmail", 59)]        
        [InlineData("BrendaRobinson", 76)]
        public void TestNGramSearch(string searchTerm, int expectedCount)
        {                    
            var query = ToQuery(searchTerm);

            var sw = new Stopwatch();
            var ireader = DirectoryReader.Open(Fixture.LuceneDirectory);
            StringBuilder result = new StringBuilder();
            var searcher = new IndexSearcher(ireader);
            sw.Reset();
            sw.Start();
            var topDocs = searcher.Search(query, 100);
            sw.Stop();

            Fixture.Log.AppendLine($"Search Term '{searchTerm}' returned in {TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds)} | {sw.ElapsedMilliseconds}");

            Trace.TraceInformation("Search Term:'{0}' returned these Ids:\n{1}", searchTerm, result.ToString());

            List<int> ids = new List<int>();
            foreach (var hit in topDocs.ScoreDocs)
            {
                var doc = searcher.Doc(hit.Doc);
                ids.Add(int.Parse(doc.Get("Id")));
                result.AppendLine(doc.Get("Name"));
            }

            Assert.Equal(expectedCount, topDocs.TotalHits);
        }        
    }
}
