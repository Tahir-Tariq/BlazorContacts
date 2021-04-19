using Entities.Models;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using System.Collections.Generic;
using Lucene.Net.Util;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Analysis.Standard;

namespace BlazorContacts.Server.Tests.Indexing
{
    public class SimpleIndexing : IndexingStrategy
    {
        const LuceneVersion AppLuceneVersion = LuceneVersion.LUCENE_48;

        public void BuildIndex(Lucene.Net.Store.Directory azureDirectory, IEnumerable<Contact> contacts)
        {
            using var analyzer = new NGramAnalyzer(AppLuceneVersion, 2, 12);
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

        public Query ToQuery(string searchTerm)
        {
            return MultiFieldQueryParser.Parse
                (
                    AppLuceneVersion,
                    searchTerm,
                    new string[] { "Name", "Email", "Company", "Role", "PhoneNumber" },
                    new Occur[] { Occur.SHOULD, Occur.SHOULD, Occur.SHOULD, Occur.SHOULD, Occur.SHOULD },
                    new StandardAnalyzer(AppLuceneVersion)
                );
        }
    }
}
