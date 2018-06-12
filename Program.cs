using System;
using System.Linq;
using System.Threading.Tasks;

//TODO: NuGet
// Install-Package HtmlAgilityPack -Version 1.8.4
// Install-Package ReadJEnc -Version 1.3.1.2

namespace HapSample
{
    class Program
    {
        static void Main(string[] args)
        {
            // WebページからHTMLソース文字列を取得する
            string url = "http://www.example.net";
            Task<string> sourceTask = HapUtil.GetSource(url);
            sourceTask.Wait();
            string source = sourceTask.Result;
            // Console.WriteLine(source);

            if (!string.IsNullOrEmpty(source))
            {
                // HtmlDocumentに、Webページから取得したHTMLソース文字列をセットする
                var htmlDoc = new HtmlAgilityPack.HtmlDocument();
                htmlDoc.LoadHtml(source);

                // XPathで指定したリンクの一覧を取得して、URLとタイトルを匿名オブジェクトのコレクションにセットする
                var topics = htmlDoc.DocumentNode.SelectNodes(@"//div[@class=""topicsindex""]");
                if (topics != null)
                {
                    topics = htmlDoc.DocumentNode.SelectNodes(@"//div[@class=""topicsindex""]/ul");
                    if (topics != null)
                    {
                        topics = htmlDoc.DocumentNode.SelectNodes(@"//div[@class=""topicsindex""]/ul/li/a");
                        if (topics != null)
                        {
                            var links = topics.Select(a => new
                            {
                                Url = a.Attributes["href"].Value.Trim(),
                                Title = a.InnerText.Trim(),
                            });

                            // 結果を表示する
                            Console.WriteLine("要素先頭5件（全{0}件）", links.Count());
                            foreach (var a in links.Take(5))
                            {
                                Console.WriteLine("{0} : {1}", a.Title, a.Url);
                            }
                        }
                    }
                }
            }

            Console.ReadKey();
        }
    }
}

# Copyright (c) 2018 YA-androidapp(https://github.com/YA-androidapp) All rights reserved.
