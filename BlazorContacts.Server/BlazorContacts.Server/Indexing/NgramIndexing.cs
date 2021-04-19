using Entities.Models;
using System.IO;
using Lucene.Net.Analysis.NGram;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using System.Collections.Generic;
using Lucene.Net.Util;
using Lucene.Net.QueryParsers.Classic;

namespace BlazorContacts.Server.Indexing
{
    internal class NgramIndexing : IndexingStrategy
    {
        const LuceneVersion AppLuceneVersion = LuceneVersion.LUCENE_48;

        public void BuildIndex(Lucene.Net.Store.Directory azureDirectory, IEnumerable<Contact> contacts)
        {
            using var analyzer = new StandardAnalyzer(AppLuceneVersion);
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

        public Query ToQuery(string searchTerm)
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
    }
}
