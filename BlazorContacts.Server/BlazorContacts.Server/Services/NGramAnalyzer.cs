using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.NGram;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Util;

namespace BlazorContacts.Server.Services
{
    public class NGramAnalyzer : Analyzer
    {
        private LuceneVersion _version;
        private int _minGram;
        private int _maxGram;
        public NGramAnalyzer(LuceneVersion version, int minGram, int maxGram)
        {
            _version = version;
            _minGram = minGram;
            _maxGram = maxGram;            
        }

        protected override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
        {            
            Tokenizer source = new StandardTokenizer(_version, reader) { MaxTokenLength = 255 };

            TokenStream filter = new StandardFilter(_version, source);

            filter = new LowerCaseFilter(_version, filter);

            filter = new StopFilter(_version, filter, StandardAnalyzer.STOP_WORDS_SET);

            filter = new NGramTokenFilter(_version, filter, _minGram, _maxGram);            

            return new TokenStreamComponents(source, filter);
        }        
    }
}