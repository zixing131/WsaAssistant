﻿using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace WSATools.Libs
{
    public sealed class AppX
    {
        private static AppX instance;
        public static AppX Instance
        {
            get
            {
                if (instance == null)
                    instance = new AppX();
                return instance;
            }
        }
        private readonly string[] array;
        private Node<string, string, bool> PackageList { get; }
        private AppX()
        {
            array = new string[2];
            switch (RuntimeInformation.ProcessArchitecture)
            {
                case Architecture.Arm:
                case Architecture.Arm64:
                    array[0] = "x86";
                    array[1] = "x64";
                    break;
                case Architecture.X64:
                    array[0] = "arm";
                    array[1] = "x86";
                    break;
            }
            PackageList = new Node<string, string, bool>();
            DownloadManager.Instance.ProgressComplete += DownloadManager_ProgressComplete;
        }
        private void DownloadManager_ProgressComplete(object sender, bool hasError, string filePath)
        {
            if (!hasError)
            {

            }
        }
        public async Task<Node<string, string, bool>> GetFilePath()
        {
            var handler = new HttpClientHandler() { AutomaticDecompression = DecompressionMethods.GZip };
            HttpClient httpClient = new HttpClient(handler);
            var stringContent = new StringContent("type=ProductId&url=9p3395vx91nr&ring=WIS&lang=zh-CN", Encoding.UTF8, "application/x-www-form-urlencoded");
            var respone = await httpClient.PostAsync("https://store.rg-adguard.net/api/GetFiles", stringContent);
            if (respone.StatusCode == HttpStatusCode.OK)
            {
                var html = await respone.Content.ReadAsStringAsync();
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(html);
                var table = doc.DocumentNode.SelectSingleNode("table");
                if (table != null)
                {
                    foreach (var tr in table.SelectNodes("tr"))
                    {
                        var a = tr.SelectSingleNode("td").SelectSingleNode("a");
                        if (a != null)
                        {
                            var key = a.InnerHtml.ToString();
                            if (IsSupport(key))
                            {
                                var value = a.Attributes["href"].Value;
                                PackageList.AddOrUpdate(key, value);
                            }
                        }
                    }
                }
            }
            return PackageList;
        }
        private bool IsSupport(string key)
        {
            if (key.EndsWith("appx", StringComparison.CurrentCultureIgnoreCase) ||
                key.EndsWith("msixbundle", StringComparison.CurrentCultureIgnoreCase))
            {
                var count = 0;
                foreach (var type in array)
                {
                    if (key.Contains(type))
                        count++;
                }
                return count == 0;
            }
            return false;
        }
        public async Task<List<string>> PepairAsync()
        {
            List<string> fileNames = new List<string>();
            try
            {
                foreach (Tuple<string, string, bool> url in PackageList)
                {
                    var path = Path.Combine(Environment.CurrentDirectory, url.Item1);
                    if (File.Exists(path))
                        fileNames.Add(path);
                    else
                    {
                        var data = await DownloadManager.Instance.Create(url.Item2);
                        if (data.CreateStatus)
                            fileNames.Add(data.Package.FileName);
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogError("PepairAsync", ex);
            }
            return fileNames;
        }
    }
}