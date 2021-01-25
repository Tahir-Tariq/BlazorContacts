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
using Lucene.Net.Store.Azure;
using Lucene.Net.QueryParsers.Classic;

namespace BlazorContacts.Server
{
    public class LuceneService
    {
        const LuceneVersion AppLuceneVersion = LuceneVersion.LUCENE_48;

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

        public void BuildIndex(IList<Contact> contacts)
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

        public IList<Contact> Search(string searchTerm, int maxDocs = int.MaxValue)
        { 
            using (var reader = DirectoryReader.Open(_luceneIndexDirectory))
            {
                var searcher = new IndexSearcher(reader);

                Query query = MultiFieldQueryParser.Parse
                (
                    AppLuceneVersion,
                    searchTerm,
                    new string[] { "Id", "Name", "Email", "Email2", "Company", "Role", "PhoneNumber" },
                    new Occur[] { Occur.SHOULD, Occur.SHOULD, Occur.SHOULD, Occur.SHOULD, Occur.SHOULD, Occur.SHOULD, Occur.SHOULD },
                    new StandardAnalyzer(AppLuceneVersion)
                );
                
                ScoreDoc[] hits = searcher.Search(query, maxDocs).ScoreDocs;

                var result = new List<Contact>();
                for (int i = 0; i < hits.Length; i++)
                {
                    int docId = hits[i].Doc;
                    float score = hits[i].Score;
                    Document doc = searcher.Doc(docId);

                    result.Add(new Contact
                    {
                        Id = int.Parse(doc.Get("Id")),
                        Name = doc.Get("Name"),
                        Email = doc.Get("Email"),
                        Company = doc.Get("Company"),
                        Role = doc.Get("Role"),
                        PhoneNumber = doc.Get("PhoneNumber")
                    });
                }
                return result;
            }
        }
    }
}