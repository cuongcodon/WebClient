using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace WebClient
{
    public static class StringUtil
    {
        /// <summary>
        /// Get domain name of server form URL.
        /// </summary>
        /// <param name="url">URL to get domain name.</param>
        /// <returns>Domain name of server.</returns>
        public static string GetDomainNameFromUrl(string url)
        {
            string pattern = @"^(?:https?:\/\/)?(?<domain>(?:www\.)?([^:\/?\n]+))";
            Regex rg = new Regex(pattern);
            return rg.Match(url).Groups["domain"].Value;
        }

        /// <summary>
        /// Get path of file or folder from URL.
        /// </summary>
        /// <param name="url">URL to get path.</param>
        /// <returns>Path of file or folder.</returns>
        public static string GetPathFromUrl(string url)
        {
            string pattern = @"^(?:https?:\/\/)?((?:www\.)?([^:\/?\n]+))(?<path>.+$)";
            Regex rg = new Regex(pattern);
            return rg.Match(url).Groups["path"].Value;
        }

        /// <summary>
        /// Get file name from URL, if a folder then return "".
        /// </summary>
        /// <param name="url">URL to get file name.</param>
        /// <returns>Filename or "" if a folder.</returns>
        public static string GetFileNameFromUrl(string url)
        {
            string hostName = GetDomainNameFromUrl(url);
            string fileName = string.Empty;

            if (url.EndsWith(hostName) || url.EndsWith(hostName + "/"))
                fileName = "index.html";
            else fileName = url.Substring(url.LastIndexOf("/") + 1);
            return fileName;
        }

        /// <summary>
        /// Get header from http response from server.
        /// </summary>
        /// <param name="sock">Socket connected to server.</param>
        /// <returns>Header of http response.</returns>
        public static string GetHeader(Socket sock)
        {
            StringBuilder header = new StringBuilder();
            string s = SocketUtil.ReadLineFromSocket(sock);

            while (!s.Equals("\r\n"))
            {
                header.Append(s);
                s = SocketUtil.ReadLineFromSocket(sock);
            }
            return header.ToString();
        }

    }
}
