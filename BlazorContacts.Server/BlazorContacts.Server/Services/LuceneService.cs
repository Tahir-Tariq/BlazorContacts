﻿using Entities.Models;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using System.Collections.Generic;
using Microsoft.Azure.Storage;
using System;
using System.Linq;
using Lucene.Net.Store.Azure;
using Lucene.Net.QueryParsers.Classic;
using BlazorContacts.Server.Paging;

namespace BlazorContacts.Server.Services
{
    public class LuceneService
    {
        private const LuceneVersion AppLuceneVersion = LuceneVersion.LUCENE_48;
        private int MaxSearchRecords = 1000;// just an arbitrary number
        private Directory _luceneIndexDirectory;

        public LuceneService(CloudStorageAccount storageAccount, string catalog)
        {
            storageAccount = storageAccount ?? throw new ArgumentNullException(nameof(storageAccount));

            if (string.IsNullOrWhiteSpace(catalog))
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            _luceneIndexDirectory = new AzureDirectory(storageAccount, catalog, new RAMDirectory());
        }

        public bool IsIndexed()
            => _luceneIndexDirectory.ListAll().Any();
       
        public void BuildIndex(IEnumerable<Contact> contacts)
        {
            var analyzer = new StandardAnalyzer(AppLuceneVersion);
            var indexConfig = new IndexWriterConfig(AppLuceneVersion, analyzer);
            using (var writer = new IndexWriter(_luceneIndexDirectory, indexConfig))
            {
                foreach (var contact in contacts)
                {
                    Document doc = new Document();
                    doc.Add(new StringField("Id", contact.Id.ToString(), Field.Store.YES));
                    doc.Add(new TextField("Name", contact.Name, Field.Store.YES));
                    doc.Add(new StringField("Email", contact.Email, Field.Store.YES));
                    doc.Add(new TextField("Email2", contact.Email, Field.Store.YES));
                    doc.Add(new StringField("Company", contact.Company, Field.Store.YES));
                    doc.Add(new StringField("Role", contact.Role, Field.Store.YES));
                    doc.Add(new StringField("PhoneNumber", contact.PhoneNumber, Field.Store.YES));

                    writer.AddDocument(doc);
                }
                writer.Flush(triggerMerge: false, applyAllDeletes: false);
            }                        
        }

        public PagedList<Contact> Search(string searchTerm, int pageNumber, int pageSize)
        { 
            using (var reader = DirectoryReader.Open(_luceneIndexDirectory))
            {
                var searcher = new IndexSearcher(reader);

                var query = ToQuery(searchTerm);

                var collector = TopScoreDocCollector.Create(MaxSearchRecords, true);

                int startIndex = (pageNumber - 1) * pageSize;

                searcher.Search(query, collector);

                TopDocs topDocs = collector.GetTopDocs(startIndex, pageSize);


                //ScoreDoc[] hits = searcher.Search(query, int.MaxValue).ScoreDocs;

                //int startIndex = (pageNumber - 1) * pageSize;
                //int endIndex = pageNumber * pageSize;
                //endIndex = hits.Length < endIndex ? hits.Length : endIndex;

                //for (int i = startIndex; i < endIndex; i++)
                //{
                //    luceneDocuments.Add(searcher.Doc(scoreDocs[i].doc));
                //}

                var items = new List<Contact>();
                foreach (var hits in topDocs.ScoreDocs)
                {
                    Document doc = searcher.Doc(hits.Doc);
                    items.Add(ToModel(doc));
                }
               
                return new PagedList<Contact>(items, topDocs.TotalHits, pageNumber, pageSize);
            }
        }

        private Query ToQuery(string searchTerm)
        {
            return MultiFieldQueryParser.Parse
                (
                    AppLuceneVersion,
                    searchTerm,
                    new string[] { "Id", "Name", "Email", "Email2", "Company", "Role", "PhoneNumber" },
                    new Occur[] { Occur.SHOULD, Occur.SHOULD, Occur.SHOULD, Occur.SHOULD, Occur.SHOULD, Occur.SHOULD, Occur.SHOULD },
                    new StandardAnalyzer(AppLuceneVersion)
                );
        }

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