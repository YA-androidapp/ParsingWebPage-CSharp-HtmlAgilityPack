using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

//TODO: System.Web.Scriptの参照を追加

namespace HapSample
{
    class HapUtil
    {

        /// <summary>
        /// HTTP応答メッセージから、文字コードを判定する
        /// </summary>
        /// <param name="res">HTTP応答メッセージ</param>
        /// <returns>文字エンコード</returns>
        static async Task<Encoding> DetermineEncodingAsync(HttpResponseMessage res)
        {
            // HTTPヘッダのContent-Typeが存在する場合
            string charset = res.Content.Headers.ContentType.CharSet;
            if (!string.IsNullOrWhiteSpace(charset))
            {
                try
                {
                    return Encoding.GetEncoding(charset);
                }
                catch
                {
                }
            }

            // HTML中に文字エンコードが記載されている場合
            string html = null;
            using (var ms = new MemoryStream())
            {
                await res.Content.LoadIntoBufferAsync();
                await res.Content.CopyToAsync(ms);
                ms.Position = 0;
                using (var reader = (new StreamReader(ms, Encoding.UTF8, true)) as TextReader)
                {
                    html = await reader.ReadToEndAsync();
                }
            }

            // metaタグまたはcharset属性が記載されている場合
            var charsetEx = new Regex(@"<[^>]*\bcharset\s*=\s*[""']?(?<charset>\w+)\b", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Singleline);
            Match charsetMatch = charsetEx.Match(html);
            if (charsetMatch.Success)
            {
                try
                {
                    return Encoding.GetEncoding(charsetMatch.Groups["charset"].Value);
                }
                catch
                {
                }
            }

            // 判別できなかった場合、NULLを返し、上の階層でバイト配列をもとに文字エンコードを決定する
            return null;
        }

        /// <summary>
        /// 指定されたページのHTMLソースを取得する
        /// </summary>
        /// <param name="uri">URL文字列</param>
        /// <returns>HTMLソース文字列</returns>
        public static async Task<string> GetSource(string uri)
        {
            string result = string.Empty;

            using (var client = new HttpClient())
            {
                // ユーザーエージェント(Firefox v60のものを指定)
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:60.0) Gecko/20100101 Firefox/60.0");

                // その他オプション
                client.DefaultRequestHeaders.Add("Accept-Language", "ja-JP");
                client.Timeout = TimeSpan.FromSeconds(60d);

                // 読込み
                // GET
                // HttpResponseMessage res = await client.GetAsync(uri);

                // POST
                var param = new Hashtable();
                param["foo"] = 8888;
                param["bar"] = "hoge";
                var serializer = new JavaScriptSerializer();
                var content = new StringContent(serializer.Serialize(param));
                using (HttpResponseMessage res = await client.PostAsync(uri, content))
                {
                    try
                    {
                        // ページ読込み時にエラーが発生した場合、例外を発生させる
                        res.EnsureSuccessStatusCode();

                        Encoding enc = await DetermineEncodingAsync(res);
                        // URLが固定の場合は予め適する文字エンコードを指定しておき高速化
                        // Encoding.GetEncoding("shift_jis") / Encoding.GetEncoding(65001) / Encoding.UTF8 ...

                        if (enc != null)
                        {
                            // 文字エンコードが既知の場合
                            using (var stream = (await res.Content.ReadAsStreamAsync()))
                            using (var reader = (new StreamReader(stream, enc, true)) as TextReader)
                            {
                                // HTMLソースを文字列として返す
                                result = await reader.ReadToEndAsync();
                            }
                        }
                        else
                        {
                            // 文字エンコードが未知の場合
                            byte[] byteData = await client.GetByteArrayAsync(uri);
                            if (byteData != null)
                            {
                                // ReadJEncを使用して文字エンコードを判定する
                                Hnx8.ReadJEnc.CharCode charCode = Hnx8.ReadJEnc.ReadJEnc.JP.GetEncoding(byteData, byteData.Length, out result);
                            }
                        }
                    }
                    catch (HttpRequestException)
                    {
                        if (res.StatusCode == HttpStatusCode.NotFound)
                            Console.WriteLine("Page Not Found");
                    }
                }
            }

            return result;
        }
    }
}

# Copyright (c) 2018 YA-androidapp(https://github.com/YA-androidapp) All rights reserved.
