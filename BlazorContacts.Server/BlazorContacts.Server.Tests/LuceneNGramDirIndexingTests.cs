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
using System.Globalization;
using System.Collections.Generic;
using Lucene.Net.Util;
using Lucene.Net.QueryParsers.Classic;
using Xunit;
using Lucene.Net.Store;

namespace BlazorContacts.Server.Tests
{
    public class LuceneNGramDirFixture : IDisposable
    {
        public StringBuilder Log;
        public Lucene.Net.Store.Directory LuceneDirectory;
        public LuceneVersion AppLuceneVersion = LuceneVersion.LUCENE_48;
        public  Dictionary<int, Contact> ContactsHash = null;

        private FSDirectory _fSDirectory = null;

        public LuceneNGramDirFixture()
        {
            Log = new StringBuilder();
            Log.Insert(0, "*",50).AppendLine();
            ContactsHash = ContactsHash ?? GetTestContacts().ToDictionary(con => con.Id);
            LuceneDirectory = PrepreLocalDirectory();
        }

        private Lucene.Net.Store.Directory PrepreLocalDirectory()
        {
            var fs = FSDirectory.Open("ngramcontactsdir");

            var sw = new Stopwatch();
            sw.Start();
            BuildIndex(fs, ContactsHash.Values);
            sw.Stop();

            var containerSize = fs.ListAll().Select(file => fs.FileLength(file)).Sum();

            Log.AppendLine($"Index container size {containerSize} built in {TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds)} | {sw.ElapsedMilliseconds}");

            _fSDirectory = fs;
            return fs;
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
        
            var indexConfig = new IndexWriterConfig(AppLuceneVersion, new NGramAnalyzer(AppLuceneVersion,2,2));

            using var writer = new IndexWriter(_fSDirectory, indexConfig);
            writer.DeleteAll();
        }
    }

    public class LuceneNGramDirIndexingTests : IClassFixture<LuceneNGramDirFixture>
    {
        LuceneNGramDirFixture Fixture;
        
        public LuceneNGramDirIndexingTests(LuceneNGramDirFixture fixture)
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
        public void TestLocalDirSearch(string searchTerm, int expectedCount)
        {                    
            var query = ToQuery(searchTerm);

            var sw = new Stopwatch();
            var ireader = DirectoryReader.Open(Fixture.LuceneDirectory);
           
            var searcher = new IndexSearcher(ireader);
            sw.Reset();
            sw.Start();
            var topDocs = searcher.Search(query, 100);
            sw.Stop();

            var ids = topDocs.ScoreDocs.Select(hit => int.Parse(searcher.Doc(hit.Doc).Get("Id")));

            Fixture.Log.AppendLine($"Search Term '{searchTerm}' returned in {sw.ElapsedMilliseconds} ms  | Total hits: {topDocs.TotalHits}| Contains: {SearchContains(searchTerm)} | Matching: {MatchSearchContains(searchTerm, ids)} ");            

            Assert.Equal(expectedCount, topDocs.TotalHits);
        }

        private int SearchContains(string searchTerm)
        {
            return  Fixture.ContactsHash.Values.Where(con =>
            con.Email.Contains(searchTerm) || con.Company.Contains(searchTerm)
                    || con.Name.Contains(searchTerm) || con.PhoneNumber.Contains(searchTerm)
                    || con.Role.Contains(searchTerm)
            ).Count();
        }

        private int MatchSearchContains(string searchTerm, IEnumerable<int> ids)
        {
            var c = from conKV in Fixture.ContactsHash
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
