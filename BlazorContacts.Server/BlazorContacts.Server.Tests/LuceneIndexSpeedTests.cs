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
using Microsoft.Azure.Storage;
using System.Globalization;
using System.Collections.Generic;
using Lucene.Net.Util;
using Xunit;
using Lucene.Net.Store.Azure;
using Lucene.Net.Store;
using BlazorContacts.Server.Tests.Indexing;

namespace BlazorContacts.Server.Tests
{
    public class LuceneIndexSpeedTestFixture : IDisposable
    {
        public StringBuilder Log;

        public RAMDirectory RAMDirectory;
        public FSDirectory FSDirectory;
        public AzureDirectory AzureDirectory;       

        public LuceneVersion AppLuceneVersion = LuceneVersion.LUCENE_48;
        public  Dictionary<int, Contact> ContactsHash = null;
        //public IndexingStrategy IndexingStrategy { get; } = new NgramIndexing();
        public IndexingStrategy IndexingStrategy { get; private set; } = new SimpleIndexing();
        private string _testDirectory = "Test";

        public LuceneIndexSpeedTestFixture()
        {
            Log = new StringBuilder();
            Log.Insert(0, "*",50).AppendLine();
            ContactsHash = ContactsHash ?? GetTestContacts().ToDictionary(con => con.Id);                    
        }

        private AzureDirectory PrepreAzureDirectory()
        {
            var cloudStorageAccount = CloudStorageAccount.DevelopmentStorageAccount;

            const string containerName = "ngramcontacts3";

            var azureDirectory = new AzureDirectory(cloudStorageAccount, containerName, new RAMDirectory());

            if (azureDirectory.BlobContainer.Exists())
                azureDirectory.BlobContainer.Delete();

            var sw = new Stopwatch();
            sw.Start();
            BuildIndex(azureDirectory, ContactsHash.Values);
            sw.Stop();

            var containerSize = azureDirectory.ListAll().Select(file => azureDirectory.FileLength(file)).Sum();

            Log.AppendLine("# AzureDirectory - Indexes built with only columns");

            Log.AppendLine($"Index container size {containerSize} built in {TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds)} | {sw.ElapsedMilliseconds}");

            return azureDirectory;
        }

        private RAMDirectory PrepreRAMDirectory()
        {
            var fs = new RAMDirectory();

            var sw = new Stopwatch();
            sw.Start();
            BuildIndex(fs, ContactsHash.Values);
            sw.Stop();

            var containerSize = fs.ListAll().Select(file => fs.FileLength(file)).Sum();

            Log.AppendLine("# RAMDirectory - Indexes built with only columns");

            Log.AppendLine($"Index container size {containerSize} built in {TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds)} | {sw.ElapsedMilliseconds}");

            return fs;
        }

        private FSDirectory PrepreLocalDirectory()
        {
            var fs = FSDirectory.Open(_testDirectory +"/ngramcontactsdir");

            var sw = new Stopwatch();
            sw.Start();
            BuildIndex(fs, ContactsHash.Values);
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
            //using (var reader = new StreamReader("10000 Records_.csv"))// @"C:\Users\nayyab\Desktop\Lucene project\10000-Records\10000 Records_.csv"))//10000 Records_.csv//Contacts.csv
            using (var reader = new StreamReader("Contacts.csv"))
            using (var csv = new CsvReader(reader, config))
            {
                return csv.GetRecords<Contact>().ToList();
            }
        }

        private void BuildIndex(Lucene.Net.Store.Directory azureDirectory, IEnumerable<Contact> contacts)
            => IndexingStrategy.BuildIndex(azureDirectory, contacts);


        public void PrepareIndexes(IndexingStrategy indexingStrategy)
        {
            this.IndexingStrategy = indexingStrategy;

            Clean();

            RAMDirectory = PrepreRAMDirectory();
            FSDirectory = PrepreLocalDirectory();
            AzureDirectory = PrepreAzureDirectory();            
        }

        private void Clean()
        {            
            this.FSDirectory?.Dispose();
            this.RAMDirectory?.Dispose();
            this.AzureDirectory?.BlobContainer.Delete();
            try
            {
                if (System.IO.Directory.Exists(_testDirectory))
                    System.IO.Directory.Delete(_testDirectory, true);
            }
            catch (Exception)
            {
            }
        }

        public void Dispose()
        {
            File.WriteAllText("LuceneIndexSpeedTests.log", Log.ToString());

            Debug.WriteLine(Log.ToString());

            Clean();
        }
    }

    public class LuceneIndexSpeedTests : IClassFixture<LuceneIndexSpeedTestFixture>
    {
        LuceneIndexSpeedTestFixture Fixture;
        
        public LuceneIndexSpeedTests(LuceneIndexSpeedTestFixture fixture)
        {
            Fixture = fixture;
        }

        private Query ToQuery(string searchTerm) => Fixture.IndexingStrategy.ToQuery(searchTerm);        

        [Fact]
        public void TestLocalDirSearch()
        {
            string[] searchTerms = new string[] { "BrendaRobinson", "gmail", "henderson", "yahoo", "reb", "benjamin", "ste" };

            Search(searchTerms, new SimpleIndexing());
            Search(searchTerms, new NgramIndexing());

            Assert.True(true);
        }

        private void Search(string[] searchTerms, IndexingStrategy indexing)
        {
            Fixture.Log.AppendLine("# " + indexing.GetType().Name);
            Fixture.PrepareIndexes(indexing);

            Fixture.Log.AppendLine("|Search Term|Type |  Speed (ms) |Total hits| Contains| Matching | Acuracy | ");
            Fixture.Log.AppendLine("|:---| :---| :---|:---| :---| :---| :---| ");

            var sw = new Stopwatch();
            foreach (var searchTerm in searchTerms)
            {                
                var query = ToQuery(searchTerm);

                BuildStats(searchTerm, Fixture.RAMDirectory, query, sw);
                BuildStats(searchTerm, Fixture.FSDirectory, query, sw);
                BuildStats(searchTerm, Fixture.AzureDirectory, query, sw);                
            }
        }

        private void BuildStats(string searchTerm, Lucene.Net.Store.Directory directory, Query query, Stopwatch sw)
        {
            sw.Reset();
            sw.Start();

            var ireader = DirectoryReader.Open(directory);
            var searcher = new IndexSearcher(ireader);        
            var topDocs = searcher.Search(query, 100);
            sw.Stop();

            var ids = topDocs.ScoreDocs.Select(hit => int.Parse(searcher.Doc(hit.Doc).Get("Id")));

            var contains = SearchContains(searchTerm);
            var matching = MatchSearchContains(searchTerm, ids);
            var mp = contains == 0 ? 0 : matching * 100 / contains;
            
            Fixture.Log.AppendLine($"|{searchTerm}|{directory.GetType().Name}|{sw.ElapsedMilliseconds}  | {topDocs.TotalHits}| {contains} | {matching} |{mp} ");
        }


        private int SearchContains(string searchTerm)
        {
            return Fixture.ContactsHash.Values.Where(con =>
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


