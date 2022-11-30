using System;
using System.Collections.Generic;
using System.Text;

namespace WebClient
{
    public class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            //if (args.Length < 2)
            //    DownloadUtil.SingleConnection(args[0]);
            //else
            //{
            //    List<string> ls = new List<string>();
            //    foreach (var item in args)
            //    {
            //        ls.Add(item);
            //    }
            //    DownloadUtil.MultiConnection(ls);
            //}
            List<string> urls = new List<string>();

            while (true)
            {
                Console.Write("Nhập url (exit để thoát): ");
                string s = Console.ReadLine();
                if (s == "exit") break;
                urls.Add(s);
            }
            if (urls.Count < 2)
                DownloadUtil.SingleConnection(urls[0]);
            else
            {

                DownloadUtil.MultiConnection(urls);

            }

            Console.WriteLine("Downloading process has finnished! Press any key to exit!");
            Console.ReadLine();
        }
    }
}