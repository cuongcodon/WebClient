using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace WebClient
{
    public class SocketUtil
    {
        /// <summary>
        /// Connect a socket client to a server.
        /// </summary>
        /// <param name="sock">Socket to connect to server.</param>
        /// <param name="url">URL to take domain name of server.</param>
        /// <returns>True if connect success else false.</returns>
        public static bool ConnectToServer(Socket sock, string url)
        {
            IPAddress ipaddr = null;
            string hostName = StringUtil.GetDomainNameFromUrl(url);
            Console.WriteLine($"Connecting to {hostName}...");

            try
            {
                // Get IP of server by using DNS
                ipaddr = Dns.GetHostEntry(hostName).AddressList[0];
                try
                {
                    // Connect to server at port 80
                    sock.Connect(new IPEndPoint(ipaddr, 80));
                }
                catch (SocketException)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Connect to {hostName} failed!");
                    Console.ForegroundColor = ConsoleColor.White;
                }
            }
            catch (Exception)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Connect to {hostName} failed! Wrong servername!");
                Console.ForegroundColor = ConsoleColor.White;
                return false;
            }

            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine($"Connect to {hostName} succeeded!");
            Console.ForegroundColor = ConsoleColor.White;
            return true;
        }

        /// <summary>
        /// Send request to a server.
        /// </summary>
        /// <param name="sock">Socket connected to server.</param>
        /// <param name="url">URL to take filepath and hostname to make request header.</param>
        public static void SendRequest(Socket sock, string url)
        {
            string path = StringUtil.GetPathFromUrl(url);
            string hostName = StringUtil.GetDomainNameFromUrl(url);

            if (url.EndsWith(hostName) || url.EndsWith(hostName + "/"))
            {
                path = "/";
            }
            // Request message
            string request = $"GET {path} HTTP/1.1\r\nHost: {hostName}\r\nConnection: Keep-Alive\r\n\r\n";
            try
            {
                sock.Send(Encoding.ASCII.GetBytes(request));
            }
            catch(SocketException sockEx)
            {
                throw sockEx;
            }    
        }

        /// <summary>
        /// Receive data from server.
        /// </summary>
        /// <param name="s">Socket connected to server.</param>
        /// <param name="size">Number of bytes to receive.</param>
        /// <returns>Byte array contains data.</returns>
        public static byte[] ReceiveData(Socket s, int size)
        {
            byte[] data = new byte[size];
            int total = 0;
            int dataLeft = size;
            int recv;

            while (total < size)
            {
                try
                {
                    recv = s.Receive(data, total, dataLeft, SocketFlags.None);
                }
                catch(SocketException sockEx)
                { 
                    throw sockEx; 
                }
                if (recv == 0)
                {
                    break;
                }
                total += recv;
                dataLeft -= recv;
            }

            return data;
        }

        /// <summary>
        /// Read a line from http response message from server.
        /// </summary>
        /// <param name="socket">Socket connected to server.</param>
        /// <param name="inclEndline">Whether to include the endline or not.</param>
        /// <returns>A line taken from http response message.</returns>
        public static string ReadLineFromSocket(Socket socket, bool inclEndline = true)
        {
            byte[] buff = new byte[1];
            StringBuilder line = new StringBuilder();
            char c = '\0', last = '\0';
            int recv = 0;

            while (true)
            {
                try
                {
                    recv = socket.Receive(buff, 1, SocketFlags.None);
                }
                catch(SocketException sockEx)
                { 
                    throw sockEx; 
                }
                if (recv == 0) break;
                c = Encoding.ASCII.GetChars(buff, 0, recv)[0];
                if (last == '\r' && c == '\n')
                {
                    if (inclEndline) line.Append(c);
                    else line.Remove(line.Length - 1, 1);
                    break;
                }
                line.Append(c);
                last = c;
            }

            return line.ToString();
        }

        /// <summary>
        /// Get the data part from http response message from server.
        /// </summary>
        /// <param name="sock">Socket connected to server.</param>
        /// <param name="url">URL to take info to send request to server.</param>
        /// <param name="close">Whether to close the connection or not.</param>
        /// <returns>Byte array contains data taken from server.</returns>
        public static byte[] GetDataFromServer(Socket sock, string url, bool close)
        {
            byte[] downloadedData = null;

            try
            {
                SendRequest(sock, url);
                string header = StringUtil.GetHeader(sock);
                string statusLine = header.Substring(0, header.IndexOf("\r\n"));
                int statusCode = int.Parse(statusLine.Split()[1]);

                if (statusCode == 200)
                {
                    if (header.Contains("Transfer-Encoding: chunked"))
                    {
                        int total = 0;
                        List<byte> data = new List<byte>();

                        while (true)
                        {
                            string line = ReadLineFromSocket(sock);
                            int chunkSize = int.Parse(line.Split(';')[0], System.Globalization.NumberStyles.HexNumber);
                            if (chunkSize == 0) break;
                            data.AddRange(ReceiveData(sock, chunkSize));
                            total += chunkSize;
                            ReceiveData(sock, 2);
                        }
                        downloadedData = new byte[data.Count];
                        data.CopyTo(downloadedData, 0);
                    }
                    else if (header.Contains("Content-Length:"))
                    {
                        int contentLength = 0;
                        string[] headers = header.Split('\n');
                        foreach (string h in headers)
                        {
                            if (h.Contains("Content-Length:"))
                            {
                                contentLength = int.Parse(h.Split()[1]);
                                break;
                            }
                        }
                        downloadedData = ReceiveData(sock, contentLength);
                    }
                }
                else if (statusCode == 301)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"301 URL {url} Moved Permanently!");
                }
                else if (statusCode == 400)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"400 URL {url} Bad Request!");
                }
                else if (statusCode == 403)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"403 URL {url} Forbidden!");
                }
                else if (statusCode == 404)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"404 URL {url} Not Found!");
                }
                else if (statusCode == 503)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"503 URL {url} Service Unavailable!");
                }
            }
            catch (SocketException)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Something went wrong when receiving data!");
            }

            Console.ForegroundColor = ConsoleColor.White;
            if (close)
                sock.Close();

            return downloadedData;
        }

    }
}
