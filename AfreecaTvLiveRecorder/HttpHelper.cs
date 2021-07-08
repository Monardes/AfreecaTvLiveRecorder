﻿using System;
using System.Net;
using System.Net.Http;
using System.Text;

public class HttpHelper
{
    public static string Get(string url, string userAgent = null, string cookie = null, int timeout = 0, string proxy = null)
    {
        string result = string.Empty;

        HttpClientHandler httpClientHandler = new HttpClientHandler();

        httpClientHandler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

        httpClientHandler.UseCookies = false;

        if (!string.IsNullOrEmpty(proxy))
        {
            httpClientHandler.Proxy = new WebProxy(proxy);
        }

        using (var client = new HttpClient(httpClientHandler))
        {
            if (timeout > 0)
            {
                client.Timeout = TimeSpan.FromSeconds(timeout);
            }

            if (!string.IsNullOrEmpty(userAgent))
            {
                client.DefaultRequestHeaders.Add("User-Agent", userAgent);
            }

            if (!string.IsNullOrEmpty(cookie))
            {
                client.DefaultRequestHeaders.Add("Cookie", cookie);
            }

            var request = new HttpRequestMessage(HttpMethod.Get, url);

            HttpResponseMessage httpResponseMessage = client.SendAsync(request).Result;

            if (httpResponseMessage.IsSuccessStatusCode)
            {
                result = httpResponseMessage.Content.ReadAsStringAsync().Result;
            }
        }

        return result;
    }

    public static string Post(string url, string postData, string userAgent = null, string cookie = null, int timeout = 0, string proxy = null)
    {
        string result = string.Empty;

        HttpClientHandler httpClientHandler = new HttpClientHandler();

        httpClientHandler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

        httpClientHandler.UseCookies = false;

        if (!string.IsNullOrEmpty(proxy))
        {
            httpClientHandler.Proxy = new WebProxy(proxy);
        }

        using (var client = new HttpClient(httpClientHandler))
        {
            if (timeout > 0)
            {
                client.Timeout = TimeSpan.FromSeconds(timeout);
            }

            if (!string.IsNullOrEmpty(userAgent))
            {
                client.DefaultRequestHeaders.Add("User-Agent", userAgent);
            }

            if (!string.IsNullOrEmpty(cookie))
            {
                client.DefaultRequestHeaders.Add("Cookie", cookie);
            }

            var request = new HttpRequestMessage(HttpMethod.Post, url);

            request.Content = new StringContent(postData, Encoding.UTF8, "application/x-www-form-urlencoded");

            HttpResponseMessage httpResponseMessage = client.SendAsync(request).Result;

            if (httpResponseMessage.IsSuccessStatusCode)
            {
                result = httpResponseMessage.Content.ReadAsStringAsync().Result;
            }
        }

        return result;
    }
}
