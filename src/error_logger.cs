using System;
using System.IO;

namespace LLC_Paratranz_Util
{
    internal class Error_logger
    {
        private readonly string logFilePath;
        public Error_logger(string logFilePath)
        {
            FileInfo fileInfo = new(logFilePath);
            if (fileInfo.Exists)
                fileInfo.Delete();
            this.logFilePath = logFilePath;
        }

        public void LogError(string message)
        {
            string logMessage = $"[{DateTime.Now}] {message}{Environment.NewLine}";
            File.AppendAllText(logFilePath, logMessage);
            Console.WriteLine(logMessage);
        }
    }
}
