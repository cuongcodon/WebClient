using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WebClient
{
    public class DownloadUtil
    {
        /// <summary>
        /// Save data to a File.
        /// </summary>
        /// <param name="data">Data to be saved.</param>
        /// <param name="hostName">Name of server responding the data.</param>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="subFolder">Name of subfoler or not.</param>
        public static void SaveFile(byte[] data, string hostName, string fileName, string subFolder = null)
        {
            // Save into release folder
            string s = AppDomain.CurrentDomain.BaseDirectory;
            string path;

            if (subFolder == null)
                path = Path.Combine(s, hostName + "_" + fileName);
            else
            {
                path = Path.Combine(s, hostName + "_" + subFolder);
                Directory.CreateDirectory(path);
                path = Path.Combine(path, fileName);
            }

            using (FileStream fileStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write))
            {
                fileStream.Write(data, 0, data.Length);
            }
        }

        /// <summary>
        /// Download a file from server.
        /// </summary>
        /// <param name="sock">Socket connected to server.</param>
        /// <param name="url">URL of the file.</param>
        /// <param name="close">Whether close the connection after downloading or not.</param>
        /// <param name="folder">Name of folder.</param>
        public static void DownloadFile(Socket sock, string url, bool close, string folder = null)
        {
            string hostName = StringUtil.GetDomainNameFromUrl(url);
            string fileName = StringUtil.GetFileNameFromUrl(url);
            string amendedFileName = Regex.Replace(fileName, @"%20", " ");

            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine($"{amendedFileName} downloading...");
            Console.ForegroundColor = ConsoleColor.White;

            byte[] downloadedFile = SocketUtil.GetDataFromServer(sock, url, close);

            if (downloadedFile != null)
            {
                SaveFile(downloadedFile, hostName, amendedFileName, folder);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Download file {amendedFileName} succeeded!");
                Console.ForegroundColor = ConsoleColor.White;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Download file {amendedFileName} failed!");
                Console.ForegroundColor = ConsoleColor.White;
            }
        }

        /// <summary>
        /// Download file from a folder of server.
        /// </summary>
        /// <param name="sock">Socket connected to server.</param>
        /// <param name="url">URL of the subfolder.</param>
        /// <param name="multiRequests">Whether to multirequest from a single connection or not.</param>
        public static void DownloadFilesFromSubfolder(Socket sock, string url, bool multiRequests)
        {
            string s;
            if (!url.EndsWith("/"))
            {
                s = url;
                url += "/";
            }
            else s = url.Substring(0, url.Length - 1);

            string subFolder = s.Substring(s.LastIndexOf("/") + 1);
            Console.WriteLine($"Downloading file to folder {subFolder}");

            bool close = true;
            if (multiRequests) close = false;
            byte[] data = SocketUtil.GetDataFromServer(sock, url, close);
           
            if (data != null)
            {
                string html = Encoding.ASCII.GetString(data);

                string pattern = @"<a href=""(.+\..+?)"">.+</a>";
                Regex rg = new Regex(pattern);
                MatchCollection matchedStr = rg.Matches(html);
                List<string> fileUrls = new List<string>();
                foreach (Match match in matchedStr)
                {
                    string str = url + match.Groups[1].ToString();
                    fileUrls.Add(str);
                }

                foreach (string file_url in fileUrls)
                {
                    if (multiRequests)
                    {
                        DownloadFile(sock, file_url, close, subFolder);
                    }
                    else
                    {
                        Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        SocketUtil.ConnectToServer(socket, url);
                        DownloadFile(socket, file_url, close, subFolder);
                    }
                }
            }
        }

        /// <summary>
        /// Create a single connection to download.
        /// </summary>
        /// <param name="url">URL of file or folder to download.</param>
        public static void SingleConnection(string url)
        {
            string hostName = StringUtil.GetDomainNameFromUrl(url);
            string fileName = StringUtil.GetFileNameFromUrl(url);
            // Create TCP socket
            Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            sock.ReceiveTimeout = 5000;
            sock.SendTimeout = 1000;

            if (SocketUtil.ConnectToServer(sock, url))
            {
                if (!url.EndsWith(hostName) && fileName.Equals(""))
                {
                    DownloadFilesFromSubfolder(sock, url, true);
                }
                else
                {
                    DownloadFile(sock, url, true);
                }
            }
        }

        /// <summary>
        /// Create multiple connection to download multiple files or folder.
        /// </summary>
        /// <param name="urls">String list contains multiple links of file or foler to download.</param>
        public static void MultiConnection(List<string> urls)
        {
            Parallel.ForEach(urls, (url) =>
            {
                SingleConnection(url);
            });
        }

    }
}
