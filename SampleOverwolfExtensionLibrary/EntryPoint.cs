using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;
using Newtonsoft.Json;

namespace SampleOverwolfExtensionLibrary
{
    public class SampleOverwolfExtension : IDisposable
    {
        private static int m_delay = 100;
        private int m_index = 0;
        private Dictionary<string, Card> m_cards = null;
        private static bool m_firstGame = false;
        private static long m_lastOffset = 0;


        // CHANGE THIS PATH TO THE PATH OF THE LOGFILE
        private static string m_logFileFullPath = @"d:\Program Files (x86)\Hearthstone\Hearthstone_Data\output_log.txt";
        private const string PROJECT_DIRECTORY = @"d:\Dropbox\School Files\Project\DemoExtention\DemoUsingSampleExtension\";


        public SampleOverwolfExtension(int nativeWindowHandle)
        {
        }

        public SampleOverwolfExtension()
        {
        }

        public event Action<object> sampleEvent;
        private void fireSampleEvent()
        {
            if (sampleEvent != null)
            {
                EventArgs e = EventArgs.Empty;
                sampleEvent(e);
            }
        }

        // Fired each time a card is played.
        public event Action<object> CardPlayedEvent;
        private void fireCardPlayedEvent(string msg)
        {
            if (CardPlayedEvent != null)
            {
                CardPlayedEventArgs e = new CardPlayedEventArgs();
                e.CardJSON = msg;
                CardPlayedEvent(e);
            }
        }

        public void init(Action<object> callback)
        {
            if (m_cards == null)
            {
                InitCards(callback);
            }
            BackgroundWorker bw = new BackgroundWorker();
            callback(string.Format("Initialized cards, count: {0}", m_cards.Count));
            bw.DoWork += new DoWorkEventHandler(
            delegate(object o, DoWorkEventArgs args)
            {
                FirstGame = true;
                Regex cardMovementRegex = new Regex(@"\w*(name=(?<name>(.+?(?=id)))).*(cardId=(?<Id>(\w*))).*(zone\ from\ (?<from>((\w*)\s*)*))((\ )*->\ (?<to>(\w*\s*)*))*.*");
                try
                {
                    while (true)
                    {
                        using (FileStream fs = new FileStream(m_logFileFullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            if (FirstGame)
                            {
                                FirstGame = false;
                                m_lastOffset = fs.Length;
                                Thread.Sleep(m_delay);
                                continue;
                            }
                            else
                            {
                                fs.Seek(m_lastOffset, SeekOrigin.Begin);
                                if (fs.Length != m_lastOffset)
                                {
                                    using (StreamReader sr = new StreamReader(fs))
                                    {
                                        string newLine = sr.ReadToEnd();
                                        m_lastOffset = fs.Length;
                                        if (cardMovementRegex.IsMatch(newLine))
                                        {

                                            Match match = cardMovementRegex.Match(newLine);
                                            string id = match.Groups["Id"].Value.Trim();
                                            string name = match.Groups["name"].Value.Trim();
                                            string from = match.Groups["from"].Value.Trim();
                                            string to = match.Groups["to"].Value.Trim();
                                            if (id != "")
                                            {
                                                string output = string.Format("[+] Card Moved - NAME: {0} ID: {1} FROM: {2} TO: {3}", name, id, from, to);
                                                fireCardPlayedEvent(output);
                                                if (m_cards.ContainsKey(id))
                                                {
                                                    fireCardPlayedEvent(JsonConvert.SerializeObject(m_cards[id]));
                                                }
                                            }
                                        }
                                        else
                                        {
                                            Thread.Sleep(m_delay);
                                            continue;
                                        }
                                    }
                                }
                            }
                            Thread.Sleep(m_delay);
                        }
                    }
                }
                catch (Exception e)
                {
                    // Log...
                }
            });
            bw.RunWorkerAsync();
        }

        // Initializes all of the cards from XML.
        private void InitCards(Action<object> callback)
        {
            Card c = null;
            string path = Path.Combine(PROJECT_DIRECTORY, @"Files\XML\enGB.xml");
            string testPath = Path.GetFullPath(@"..\XML\enGB.xml");
            XmlSerializer ser = new XmlSerializer(typeof(Card));
            m_cards = new Dictionary<string, Card>();
            XmlDocument doc = new XmlDocument();
            doc.Load(path);
            XmlElement root = doc.DocumentElement;
            XmlNodeList nodes = root.SelectNodes("Card");

            foreach (XmlNode node in nodes)
            {
                using (StringReader sr = new StringReader(node.OuterXml))
                {
                    try
                    {
                        c = (Card)ser.Deserialize(sr);
                        m_cards.Add(c.CardId, c);
                    }
                    catch (Exception e)
                    {
                        // TODO: Handle bad XML file.
                        if (callback != null)
                        {
                            callback("Error while initializing: " + e.Message);
                        }
                    }
                }
            }
            if (callback != null)
            {
                callback("Initialization complete.");
            }
        }

        public static bool FirstGame
        {
            get
            {
                return m_firstGame;
            }
            set
            {
                m_firstGame = value;
            }
        }

        /// <summary>
        /// Cleans up any resources being used.
        /// </summary>
        /// <skip/>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }

    public class CardPlayedEventArgs : EventArgs
    {
        public string CardJSON { get; set; }
    }
}
