using CsvHelper;
using CsvHelper.Configuration;
using Entities.Models;
using System;
using System.Linq;
using System.Diagnostics;
using System.IO;
using System.Text;
using Lucene.Net.Index;
using Lucene.Net.Search;
using System.Globalization;
using System.Collections.Generic;
using Lucene.Net.Util;
using Xunit;
using Lucene.Net.Store;
using BlazorContacts.Server.Tests.Indexing;
using Microsoft.Azure.Storage;
using Lucene.Net.Store.Azure;

namespace BlazorContacts.Server.Tests
{
    public class LuceneNGramDirFixture : IDisposable
    {
        public StringBuilder Log;
        public Lucene.Net.Store.Directory LocalDirectory;
        public AzureDirectory AzureDirectory;
        public LuceneVersion AppLuceneVersion = LuceneVersion.LUCENE_48;
        public  Dictionary<int, Contact> ContactsHash = null;
        //public IndexingStrategy IndexingStrategy { get; } = new SimpleIndexing();
        public IndexingStrategy IndexingStrategy { get; } = new NgramIndexing();

        public LuceneNGramDirFixture()
        {
            Log = new StringBuilder();
            Log.Insert(0, "*",50).AppendLine();
            ContactsHash = ContactsHash ?? GetTestContacts().ToDictionary(con => con.Id);
            //LocalDirectory = PrepreLocalDirectory();

            Log.AppendLine("|Search Term| Speed (ms) |Total hits| Contains| Matching | Acuracy | ");
            Log.AppendLine("|:---| :---|:---| :---| :---| :---| ");
            AzureDirectory = PrepreAzureDirectory();
        }

        private AzureDirectory PrepreAzureDirectory()
        {
            var cloudStorageAccount = CloudStorageAccount.DevelopmentStorageAccount;

            const string containerName = "ngramcontacts";

            var azureDirectory = new AzureDirectory(cloudStorageAccount, containerName, new RAMDirectory());

            if (azureDirectory.BlobContainer.Exists())
                azureDirectory.BlobContainer.Delete();

            var sw = new Stopwatch();
            sw.Start();
            IndexingStrategy.BuildIndex(azureDirectory, ContactsHash.Values);
            sw.Stop();

            var containerSize = azureDirectory.ListAll().Select(file => azureDirectory.FileLength(file)).Sum();

            Log.AppendLine($"Index container size {containerSize} built in {TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds)} | {sw.ElapsedMilliseconds}");         
            return azureDirectory;
        }

        private Lucene.Net.Store.Directory PrepreLocalDirectory()
        {
            string dir = "simple-ngramcontactsdir2";

            if (System.IO.Directory.Exists(dir))
                System.IO.Directory.Delete(dir, true);

            var fs = FSDirectory.Open(dir);

            var sw = new Stopwatch();
            sw.Start();
            IndexingStrategy.BuildIndex(fs, ContactsHash.Values);
            sw.Stop();

            var containerSize = fs.ListAll().Select(file => fs.FileLength(file)).Sum();

            Log.AppendLine("# LocalDirectory - Indexes built with only columns");

            Log.AppendLine($"Index container size {containerSize} built in {TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds)} | {sw.ElapsedMilliseconds}");

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

        public void Dispose()
        {
            Debug.WriteLine(Log.ToString());
            AzureDirectory?.BlobContainer.Delete();
        }
    }

    public class LuceneNGramDirIndexingTests : IClassFixture<LuceneNGramDirFixture>
    {
        LuceneNGramDirFixture Fixture;
        
        public LuceneNGramDirIndexingTests(LuceneNGramDirFixture fixture)
        {
            Fixture = fixture;
        }                       

        [Theory]
        [InlineData("ste")]
        [InlineData("benjamin")]
        [InlineData("reb")]
        [InlineData("yahoo")]
        [InlineData("henderson")]
        [InlineData("gmail")]        
        [InlineData("BrendaRobinson")]
        public void TestLocalDirSearch(string searchTerm)
        {                    
            var query =  Fixture.IndexingStrategy.ToQuery(searchTerm);

            var sw = new Stopwatch();
            var ireader = DirectoryReader.Open(Fixture.AzureDirectory);
           
            var searcher = new IndexSearcher(ireader);
            sw.Reset();
            sw.Start();
            var topDocs = searcher.Search(query, 100);
            sw.Stop();

            var ids = topDocs.ScoreDocs.Select(hit => int.Parse(searcher.Doc(hit.Doc).Get("Id")));

            var contains = SearchContains(searchTerm);
            var matching = MatchSearchContains(searchTerm, ids);

            Fixture.Log.AppendLine($"|{searchTerm}|{sw.ElapsedMilliseconds}  | {topDocs.TotalHits}| {contains} | {matching}| {matching * 100 / contains} ");
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
