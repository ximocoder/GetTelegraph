using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using CliWrap;

namespace GetTelegraph
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting...");

            var messagesFile = File.ReadAllText(".\\files\\messages.html");

            //HtmlDocument hap = new HtmlDocument();
            //hap.LoadHtml(messagesFile);
            //HtmlNodeCollection nodes = hap
            //    .DocumentNode
            //    .SelectNodes("//div[@class='text']");

            HtmlDocument hap = new HtmlDocument();
            hap.LoadHtml(messagesFile);
            var nodes = hap.DocumentNode.SelectNodes("//a[@href]");         

            var hrefTags = new List<NodeInfo>();

            Console.WriteLine("Getting nodes...");

            try
            {
                GetNodes(nodes, hrefTags);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error getting nodes: " + ex.Message);
            }


            Console.WriteLine("Total of nodes: " + hrefTags.Count.ToString());

            try
            {
                //SaveNodes(hrefTags);
                SaveNodesWithWget(hrefTags);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error saving nodes: " + ex.Message);
            }


            Console.WriteLine("Done!");

        }

        private static void SaveNodes(List<NodeInfo> hrefTags)
        {
            int count = 0;
            foreach (var item in hrefTags)
            {
                count++;
                var path = "C:\\tmp\\blinkist\\" + count + "-" + ReplaceInvalidChars(item.Name) + ".html";

                if (item.CanBeSaved)
                {
                    item.Text = Download(item.Url);
                    Console.WriteLine("Saving " + path);
                    File.WriteAllText(path, item.Text);
                }
            }
        }

        private static void SaveNodesWithWget(List<NodeInfo> hrefTags)
        {
            int count = 0;
            foreach (var item in hrefTags)
            {
                count++;
                var path = "C:\\tmp\\blinkist_wget\\" + count + "-" + ReplaceInvalidChars(item.Name) + ".html";
                var stdOutBuffer = new StringBuilder();
                var stdErrBuffer = new StringBuilder();

                if (item.CanBeSaved)
                {
                    var result = Cli.Wrap("./files/wget.exe")
                        .WithArguments("--adjust-extension --user-agent=\"Mozilla\" -p -k " + item.Url)
                        .WithWorkingDirectory("./files/")
                        .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
                        .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
                        .ExecuteAsync();
                    Console.WriteLine("Saving " + path);
                    //File.WriteAllText(path, item.Text);
                }
            }
        }

        private static void GetNodes(HtmlNodeCollection nodes, List<NodeInfo> hrefTags)
        {
            if (nodes != null)
            {
                foreach (HtmlNode node in nodes)
                {
                    var canBesaved = true;
                    string url = "";
                    try
                    {
                        url = node.OuterHtml.Split("\"")[1];
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Error with InnerHtml " + node.InnerHtml);
                        canBesaved = false;
                    }

                    if (url.Contains("telegra.ph"))
                    {
                        hrefTags.Add(new NodeInfo()
                        {
                            Name = node.InnerText,
                            Url = url,
                            CanBeSaved = canBesaved

                        });
                    }

                }
            }
        }

        static string Download(string url)
        {
            HttpWebRequest Request = (HttpWebRequest)WebRequest.Create(url);
            Request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            Request.Proxy = null;
            Request.Method = "GET";
            using (WebResponse Response = Request.GetResponse())
            {
                using (StreamReader Reader = new StreamReader(Response.GetResponseStream()))
                {
                    return Reader.ReadToEnd();
                }
            }

            return "";
        }

        static string ReplaceInvalidChars(string filename)
        {
            return string.Join("_", filename.Split(Path.GetInvalidFileNameChars()));
        }
    }

    internal class NodeInfo
    {
        public string Name;
        public string Url;
        public string Text { get; internal set; }
        public bool CanBeSaved { get; internal set; }
    }
}
