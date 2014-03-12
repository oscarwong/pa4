using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace WebRole1
{
    public class Trie
    {
        public TrieNode root;

        public Trie()
        {
            root = new TrieNode(' ');
        }

        public void insertWord(string word)
        {
            int wordLength = word.Length;
            int currentIndex = 0;
            TrieNode currentNode = root;
            foreach (char c in word)
            {
                if (!currentNode.edge.ContainsKey(c))
                {
                    TrieNode newNode = new TrieNode(c);
                    currentNode.edge.Add(c, newNode);
                    currentNode = newNode;
                    currentIndex++;
                    if (currentIndex == wordLength)
                        currentNode.lastChar = true;
                    else
                        continue;
                }
                else
                {
                    currentIndex++;
                    currentNode = currentNode.getNode(c);
                    if (currentIndex == wordLength)
                        currentNode.lastChar = true;
                }
            }
        }

        public List<string> searchPrefix(string prefix)
        {
            TrieNode currentNode = root;
            List<string> words = new List<string>();
            currentNode = findRoot(prefix);
            if (currentNode == null)
            {
                words.Add("No matching words.");
                return words;
            }
            if (currentNode.lastChar)
                words.Add(prefix);
            StringBuilder sb = new StringBuilder();
            sb.Append(prefix);
            return getWords(currentNode, sb, prefix, words);
        }

        private List<string> getWords(TrieNode currentNode, StringBuilder sb, string prefix, List<string> words)
        {
            foreach (var pair in currentNode.edge)
            {
                if (!pair.Value.lastChar)
                {
                    sb.Append(pair.Key);
                    getWords(pair.Value, sb, prefix, words);
                    sb.Remove(sb.ToString().Length - 1, 1);
                }
                else if (pair.Value.lastChar && pair.Value.edge.Count == 0)
                {
                    sb.Append(pair.Key);
                    if (words.Count < 10)
                        words.Add(sb.ToString());
                    sb.Remove(sb.ToString().Length - 1, 1);
                }
                else
                {
                    sb.Append(pair.Key);
                    if (pair.Value.lastChar)
                    {
                        if (words.Count < 10)
                            words.Add(sb.ToString());
                    }
                    getWords(pair.Value, sb, prefix, words);
                    sb.Remove(sb.ToString().Length - 1, 1);
                }
            }
            return words;
        }

        private TrieNode findRoot(string prefix)
        {
            TrieNode currentNode = root;
            foreach (char c in prefix)
            {
                if (currentNode.edge.ContainsKey(c))
                    currentNode = currentNode.getNode(c);
                else
                    return null;
            }
            return currentNode;
        }
    }
}