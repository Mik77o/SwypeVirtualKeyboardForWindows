/*
The MIT License (MIT)
 
Copyright (c) 2013 Krishna Bharadwaj <krishna@krishnabharadwaj.info>
 
Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:
 
The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.
 
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/
using System.Collections.Generic;
using System.Linq;

namespace Klawiatura
{
    public class Swipe
    {
        public static string WORDLIST = "wordlist.txt";
        public static string PERSONAL_DICTIONARY = "PersonalDictionary.txt";
        public static List<string> wordsFromDictionary = new List<string>();
        public static List<string> wordsFromPersonalDictionary = new List<string>();

        public static void SetKindOfDictionary(string dictionaryOfKeyboard)
        {
            WORDLIST = dictionaryOfKeyboard;
        }

        internal static bool MatchPathToWordFromDictionary(string path, string word)
        {
            int pathLength = path.Length, wordLength = word.Length, j = 0;
            for (int i = 0; i < pathLength; i++)
            {
                if (path[i] == word[j])
                {
                    j++;
                }
                if (j == wordLength)
                    break;
            }
            if (j < wordLength)
            {
                return false;
            }
            return true;
        }

        public static int GetKeyboardRow(char c)
        {
            List<string> keyboardSystem = new List<string> { "qweęrtyuioóp", "aąsśdfghjklł", "zźżxcćvbnńm" };
            for (int counter = 0; counter < keyboardSystem.Count; counter++)
            {
                int n = keyboardSystem[counter].Length;
                for (int jcounter = 0; jcounter < n; jcounter++)
                {
                    if (keyboardSystem[counter][jcounter] == c)
                    {
                        return counter;
                    }
                }
            }
            return -1;
        }

        public static int[] Compress(int[] s)
        {
            int i = 0, j = 0, n = s.Length;
            while (i < n)
            {
                if (s[i] != s[j])
                {
                    s[++j] = s[i];
                }
                i++;
            }
            return s.Take(j + 1).ToArray();
        }

        public static int GetMinimiumWordLength(string path)
        {
            int pathLength = path.Length;
            int[] rowNumbers = new int[pathLength];
            for (int i = 0; i < pathLength; i++)
            {
                rowNumbers[i] = GetKeyboardRow(path[i]);
            }

            int minimumLength = Compress(rowNumbers).Length - 3;
            if (minimumLength < 0)
            {
                minimumLength = 0;
            }
            return minimumLength;
        }

        public static List<string> GetMostAppropriateSuggestions(string path)
        {
            List<string> suggestonsLevel1 = new List<string>(), suggestionsLevel2 = new List<string>(), suggestionsLevel3 = new List<string>();
            int amountOfWords = wordsFromDictionary.Count;
            int pathLength = path.Length;
            int len;
            //step1
            for (int i = 0; i < amountOfWords; i++)
            {
                len = wordsFromDictionary[i].Length;
                if (len == 0)
                    continue;
                if (path[0] == wordsFromDictionary[i][0] && path[pathLength - 1] == wordsFromDictionary[i][len - 1])
                {
                    suggestonsLevel1.Add(wordsFromDictionary[i]);
                }
            }
            //step2
            amountOfWords = suggestonsLevel1.Count;
            for (int k = 0; k < amountOfWords; k++)
            {
                if (MatchPathToWordFromDictionary(path, suggestonsLevel1[k]))
                {
                    suggestionsLevel2.Add(suggestonsLevel1[k]);
                }
            }
            //step3
            int minLength = GetMinimiumWordLength(path);
            amountOfWords = suggestionsLevel2.Count;
            for (int p = 0; p < amountOfWords; p++)
            {
                if (suggestionsLevel2[p].Length > minLength)
                {
                    suggestionsLevel3.Add(suggestionsLevel2[p]);
                }
            }
            //result
            return suggestionsLevel3;
        }

        public static List<string> GetMostAppropriateSuggestionsFromPersonalDictionary(string path)
        {
            List<string> suggestonsLevel1 = new List<string>(), suggestionsLevel2 = new List<string>(), suggestionsLevel3 = new List<string>();
            int amountOfWords = wordsFromPersonalDictionary.Count;
            int pathLength = path.Length;
            int len;

            //step 1
            for (int i = 0; i < amountOfWords; i++)
            {
                len = wordsFromPersonalDictionary[i].Length;
                if (len == 0)
                    continue;
                if (path[0] == wordsFromPersonalDictionary[i][0] && path[pathLength - 1] == wordsFromPersonalDictionary[i][len - 1])
                {
                    suggestonsLevel1.Add(wordsFromPersonalDictionary[i]);
                }
            }
            //step 2
            amountOfWords = suggestonsLevel1.Count;
            for (int k = 0; k < amountOfWords; k++)
            {
                if (MatchPathToWordFromDictionary(path, suggestonsLevel1[k]))
                {
                    suggestionsLevel2.Add(suggestonsLevel1[k]);
                }
            }
            //step 3
            int minLength = GetMinimiumWordLength(path);
            amountOfWords = suggestionsLevel2.Count;
            for (int p = 0; p < amountOfWords; p++)
            {
                if (suggestionsLevel2[p].Length > minLength)
                {
                    suggestionsLevel3.Add(suggestionsLevel2[p]);
                }
            }
            //result
            return suggestionsLevel3;
        }

        public static void LoadWordsFromDictionary()
        {
            string lineOfText;
            wordsFromDictionary.Clear();
            var fileStream = new System.IO.FileStream(WORDLIST,
                                              System.IO.FileMode.Open,
                                              System.IO.FileAccess.Read,
                                              System.IO.FileShare.ReadWrite);
            var fileDictionary = new System.IO.StreamReader(fileStream, System.Text.Encoding.UTF8, true, 128);

            while ((lineOfText = fileDictionary.ReadLine()) != null)
            {
                wordsFromDictionary.Add(lineOfText.Trim());
            }
        }

        public static void LoadWordsFromPersonalDictionary()
        {
            string lineOfText;
            wordsFromPersonalDictionary.Clear();
            var fileStream = new System.IO.FileStream(PERSONAL_DICTIONARY,
                                              System.IO.FileMode.Open,
                                              System.IO.FileAccess.Read,
                                              System.IO.FileShare.ReadWrite);
            var fileDictionary = new System.IO.StreamReader(fileStream, System.Text.Encoding.UTF8, true, 128);

            while ((lineOfText = fileDictionary.ReadLine()) != null)
            {
                wordsFromPersonalDictionary.Add(lineOfText.Trim());
            }
        }
    }
}