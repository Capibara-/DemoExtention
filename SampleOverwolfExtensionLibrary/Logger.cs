using System;
using System.Text;
using System.IO;

namespace SampleOverwolfExtensionLibrary
{
    public class Logger : IDisposable
    {
        private string LOG_FILE_PATH = string.Empty;
        private static Logger m_instance = null;
        private static object SYNC_OBJ = new object();
        private StreamWriter m_logger = null;
        private LogFileStream m_fileStream = null;
        private bool m_isDisoposed = false;

        private class LogFileStream : FileStream
        {
            public LogFileStream(string path, FileMode fileMode, FileAccess fileAccess)
                : base(path, fileMode, fileAccess)
            {
            }

            public override void Close()
            {
                base.Close();
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing == true)
                {
                    base.Dispose(disposing);
                }
            }
        }

        private Logger()
        {
            m_fileStream = new LogFileStream(LOG_FILE_PATH, FileMode.Create, FileAccess.ReadWrite);

            m_logger = new StreamWriter(m_fileStream);
        }

        ~Logger()
        {
            Dispose();
        }

        public static Logger Instance
        {
            get
            {
                if (m_instance == null)
                {
                    lock (SYNC_OBJ)
                    {
                        if (m_instance == null)
                        {
                            m_instance = new Logger();
                        }
                    }
                }

                return m_instance;
            }
            set { }
        }

        public void Log(string msg, string header)
        {
            lock (SYNC_OBJ)
            {
                m_logger.WriteLine(string.Format("({0}) [{1}] : {2}", DateTime.Now.ToString(), header, msg));
                m_logger.Flush();
            }
        }

        public void LogError(string msg)
        {
            Log(msg, "ERROR");
        }

        public void Dispose()
        {
            if (m_isDisoposed == false)
            {
                lock (SYNC_OBJ)
                {
                    if (m_isDisoposed == false)
                    {
                        m_isDisoposed = true;

                        m_logger.Dispose();
                        m_logger = null;

                        m_fileStream.Dispose();
                        m_fileStream = null;
                    }
                }
            }
        }
    }
}
