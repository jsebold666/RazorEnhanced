using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RazorEnhanced
{
    public class EncodedSpeech
    {
        internal class SpeechEntry : IComparable<SpeechEntry>
        {
            internal short m_KeywordID;
            internal string[] m_Keywords;

            internal SpeechEntry(int idKeyword, string keyword)
            {
                m_KeywordID = (short)idKeyword;
                m_Keywords = keyword.Split(new char[] { '*' });
            }

            public int CompareTo(SpeechEntry entry)
            {
                if (entry == null)
                {
                    return -1;
                }
                if (entry != this)
                {
                    if (m_KeywordID < entry.m_KeywordID)
                    {
                        return -1;
                    }
                    if (m_KeywordID > entry.m_KeywordID)
                    {
                        return 1;
                    }
                }
                return 0;
            }
        }

        private static List<SpeechEntry> m_Speech;

        internal static unsafe void LoadSpeechTable()
        {
            string path = Ultima.Files.GetFilePath("Speech.mul");

            if (!File.Exists(path))
            {
                m_Speech = new List<SpeechEntry>();
            }
            else
            {
                byte[] buffer = new byte[0x400];
                fixed (byte* numRef = buffer)
                {
                    List<SpeechEntry> list = new();
                    FileStream fs = new(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                    int num = 0;
                    while ((num = fs.Read(buffer, 0, 4)) > 0)
                    {
                        int idKeyword = numRef[1] | (numRef[0] << 8);
                        int bytes = numRef[3] | (numRef[2] << 8);
                        if (bytes > 0)
                        {
                            fs.Read(buffer, 0, bytes);
                            list.Add(new SpeechEntry(idKeyword, new string((sbyte*)numRef, 0, bytes)));
                        }
                    }

                    fs.Close();
                    m_Speech = list;
                }
            }
        }

        internal static List<ushort> GetKeywords(string text)
        {
            List<ushort> keynumber = new();

            if (m_Speech == null)
            {
                LoadSpeechTable();
            }

            text = text.ToLower();

            List<SpeechEntry> keywords = new();
            List<SpeechEntry> speech = m_Speech.ToList();
            foreach (SpeechEntry entry in speech)
            {
                if (IsMatch(text, entry.m_Keywords))
                {
                    keywords.Add(entry);
                }
            }
            keywords.Sort();

            bool flag = false;

            int numk = keywords.Count & 15;
            int index = 0;
            while (index < keywords.Count)
            {
                SpeechEntry entry = keywords[index];
                int keywordID = entry.m_KeywordID;

                if (flag)
                {
                    keynumber.Add((byte)(keywordID >> 4));
                    numk = keywordID & 15;
                }
                else
                {
                    keynumber.Add((byte)((numk << 4) | ((keywordID >> 8) & 15)));
                    keynumber.Add((byte)keywordID);
                }

                index++;
                flag = !flag;
            }

            if (!flag)
            {
                keynumber.Add((byte)(numk << 4));
            }

            return keynumber;
        }

        private static bool IsMatch(string input, string[] split)
        {
            int startIndex = 0;

            for (int i = 0; i < split.Length; i++)
            {
                if (split[i].Length > 0)
                {
                    int index = input.IndexOf(split[i], startIndex);
                    if ((index > 0) && (i == 0))
                    {
                        return false;
                    }
                    if (index < 0)
                    {
                        return false;
                    }
                    startIndex = index + split[i].Length;
                }
            }

            return ((split[split.Length - 1].Length <= 0) || (startIndex == input.Length));
        }
    }
}