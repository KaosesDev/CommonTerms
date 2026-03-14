using CommonTermsFunc.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CommonTermsFunc
{
    public class BuildCommon
    {
        private int minWordLength = 3; // Minimum length of words to consider
        private List<FileModel> _fileList;
        // Count single words (unigrams) and contiguous two-word phrases (bigrams).
        Dictionary<string, int> unigramCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, int> bigramCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        // Common words to exclude (stop words). Case-insensitive via comparer.
        HashSet<string> stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                // add more words here
            };

        // Optional phrase list — only phrases composed of 4-character words will be considered below.
        HashSet<string> explicitPhrases = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
            };

        public BuildCommon(List<FileModel> fileList, int minWordLength = 3)
        {
            _fileList = fileList;
            this.minWordLength = minWordLength;
        }

        public Dictionary<string, int> GetUnigramCounts() => unigramCounts;
        
        public Dictionary<string, int> GetBigramCounts() => bigramCounts;

        public void AddWordsToExclude(string word)
        {
            if (!string.IsNullOrWhiteSpace(word))
            {
                stopWords.Add(word.Trim());
            }
        }

        public void AddPhraseToInclude(string phrase)
        {
            if (!string.IsNullOrWhiteSpace(phrase))
            {
                explicitPhrases.Add(phrase.Trim());
            }
        }

        public void Build()
        {
            foreach (var fileModel in _fileList)
            {
                //_logger.LogInformation("File: {FileName}, Cleaned: {FileNameCleaned}, Path: {FilePath}, Extension: {Extension}, Parent Folder: {ParentFolder}",
                //    fileModel.FileName, fileModel.FileNameCleaned, fileModel.FilePath, fileModel.Extension, fileModel.ParentFolder);

                // Normalize and split on whitespace (FileNameCleaned already replaces - and _ with spaces in builder)
                var cleaned = fileModel.FileNameCleaned ?? string.Empty;
                var rawTokens = Regex.Split(cleaned, @"\s+")
                                     .Select(t => t.Trim())
                                     .Where(t => !string.IsNullOrEmpty(t));

                // Remove non-letter/digit characters from tokens, filter by length and exclude stop words
                var tokens = rawTokens
                             .Select(t => Regex.Replace(t, @"[^\p{L}\p{Nd}]", "")) // keep letters & digits
                             .Where(t => !string.IsNullOrEmpty(t))
                             .Where(t => t.Length >= minWordLength) // keep tokens with length >= 3
                             .Where(t => !stopWords.Contains(t)) // exclude common words
                             .ToArray();

                // Count unigrams (filtered tokens)
                foreach (var token in tokens)
                {
                    if (unigramCounts.ContainsKey(token))
                        unigramCounts[token]++;
                    else
                        unigramCounts[token] = 1;
                }

                // Count bigrams (contiguous two-word phrases), both words satisfy filters above
                for (int i = 0; i + 1 < tokens.Length; i++)
                {
                    var bigram = tokens[i] + " " + tokens[i + 1];
                    if (bigramCounts.ContainsKey(bigram))
                        bigramCounts[bigram]++;
                    else
                        bigramCounts[bigram] = 1;
                }

                // Also check explicit multi-word phrase occurrences only if all words in the phrase are 4 chars.
                var lowerCleaned = cleaned.ToLowerInvariant();
                foreach (var phrase in explicitPhrases)
                {
                    var phraseWords = phrase.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (!phraseWords.Any() || !phraseWords.All(w => w.Length >= 3))
                    {
                        // skip phrases that contain words not at least 3 characters long
                        continue;
                    }

                    if (lowerCleaned.Contains(phrase.ToLowerInvariant()))
                    {
                        if (bigramCounts.ContainsKey(phrase))
                            bigramCounts[phrase]++;
                        else
                            bigramCounts[phrase] = 1;
                    }
                }
            }
        }

        private void CountTerms()
        {
            foreach (var file in _fileList)
            {
                string[] words = file.FileNameCleaned.Split(new char[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
                // Count unigrams
                foreach (var word in words)
                {
                    if (!stopWords.Contains(word))
                    {
                        if (unigramCounts.ContainsKey(word))
                            unigramCounts[word]++;
                        else
                            unigramCounts[word] = 1;
                    }
                }
                // Count bigrams
                for (int i = 0; i < words.Length - 1; i++)
                {
                    string bigram = $"{words[i]} {words[i + 1]}";
                    if (!stopWords.Contains(words[i]) && !stopWords.Contains(words[i + 1]) && !explicitPhrases.Contains(bigram))
                    {
                        if (bigramCounts.ContainsKey(bigram))
                            bigramCounts[bigram]++;
                        else
                            bigramCounts[bigram] = 1;
                    }
                }
            }
        }
    }
}
