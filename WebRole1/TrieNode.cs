using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebRole1
{
    public class TrieNode
    {
        public char letter { get; set; }
        public bool lastChar { get; set; }
        public Dictionary<char, TrieNode> edge { get; set; }

        public TrieNode(char c)
        {
            edge = new Dictionary<char, TrieNode>();
            this.letter = c;
            this.lastChar = false;
        }

        public TrieNode getNode(char key)
        {
            TrieNode nextNode;
            if (this.edge.TryGetValue(key, out nextNode))
                return nextNode;
            else
                return null;
        }
    }
}