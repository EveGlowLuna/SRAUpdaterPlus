using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using SRAUpdaterPlus.Tool;

namespace SRAUpdaterPlus.Tool
{
    public static class DownloadHelper
    {
        /// <summary>
        /// 从URL中获取文件名
        /// </summary>
        /// <param name="url">下载链接</param>
        /// <returns>文件名，如果无法获取则返回null</returns>
        public static string? GetFileNameFromUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return null;

            try
            {
                // 尝试从URL路径中获取文件名
                var uri = new Uri(url);
                var path = uri.AbsolutePath;
                var fileName = Path.GetFileName(path);
                
                if (!string.IsNullOrEmpty(fileName))
                    return fileName;
                
                // 如果URL中没有文件名，尝试从Content-Disposition头获取
                using var client = new HttpClient();
                var response = client.Send(new HttpRequestMessage(HttpMethod.Head, url));
                
                if (response.Content.Headers.ContentDisposition != null)
                {
                    return response.Content.Headers.ContentDisposition.FileName
                        ?.Trim('"') // 移除可能的引号
                        ?? null;
                }
                
                return null;
            }
            catch
            {
                return null;
            }
        }
        /// How to use?
        /*
         private async Task StartDownloadAsync()
        {
            string url = "https://example.com/file.zip";
            string path = "downloaded_file.zip";

            var progress = new Progress<double>(p =>
            {
                Console.WriteLine($"下载进度：{p:0.00}%");
            });

            var cts = new CancellationTokenSource();

            try
            {
                await DownloadHelper.DownloadFileAsync(
                    url,
                    path,
                    disableSSL: true,
                    progress: progress,
                    cancellationToken: cts.Token
                );

                MessageBox.Show("下载完成！");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"下载失败：{ex.Message}");
            }
        }
         */
        /// <summary>
        /// 下载文件（支持任意类型，支持断点续传）
        /// </summary>
        /// <param name="url">下载链接</param>
        /// <param name="savePath">保存路径</param>
        /// <param name="disableSSL">是否禁用 SSL 验证（必须明确传入）</param>
        /// <param name="progress">可选的进度报告器（返回 0-100）</param>
        /// <param name="cancellationToken">可选取消令牌</param>
        /// <param name="timeout">超时时间（秒），默认为 100 秒</param>
        public static async Task DownloadFileAsync(
            string url,
            string savePath,
            bool? disableSSL,
            IProgress<double>? progress = null,
            CancellationToken cancellationToken = default,
            int? timeout = null,
            bool useProxy = true)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("必须提供下载链接。", nameof(url));

            List<string> urlsToTry = new List<string>();
            if (useProxy && (url.StartsWith("https://github.com") || url.StartsWith("https://raw.githubusercontent.com")))
            {
                urlsToTry = ProxyConverter.ConvertToProxy(url);
                if (urlsToTry == null || urlsToTry.Count == 0)
                {
                    urlsToTry.Add(url);
                }
            }
            else
            {
                urlsToTry.Add(url);
            }

            if (disableSSL == null)
                throw new ArgumentException("必须明确指定是否禁用 SSL 验证。", nameof(disableSSL));

            if (disableSSL == true)
            {
                ServicePointManager.ServerCertificateValidationCallback +=
                    (_, _, _, _) => true;
            }

            // 对于 version.json 或小文件，直接全量下载，不使用断点续传
            bool isVersionFile = url.EndsWith("version.json", StringComparison.OrdinalIgnoreCase);
            if (isVersionFile)
            {
                foreach (var tryUrl in urlsToTry)
            {
                try
                {
                    await DownloadWithoutResumeAsync(tryUrl, savePath, progress, cancellationToken, timeout);
                    return;
                }
                catch { }
            }
            await DownloadWithoutResumeAsync(url, savePath, progress, cancellationToken, timeout);
                return;
            }

            using var handler = new HttpClientHandler();
            using var client = new HttpClient(handler);

            if (timeout.HasValue)
            {
                client.Timeout = TimeSpan.FromSeconds(timeout.Value);
            }

            // 设置自定义请求头
            foreach (var header in Parameter.DOWNLOAD_HEADERS)
            {
                if (!client.DefaultRequestHeaders.Contains(header.Key))
                    client.DefaultRequestHeaders.Add(header.Key, header.Value);
            }

            long existingLength = 0;
            if (File.Exists(savePath))
            {
                var fileInfo = new FileInfo(savePath);
                existingLength = fileInfo.Length;
                client.DefaultRequestHeaders.Range = new System.Net.Http.Headers.RangeHeaderValue(existingLength, null);
            }

            HttpResponseMessage? response = null;
            try
            {
                response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                // 如果服务器返回416，说明本地文件比服务器还大或已完整，删除本地文件并重新下载
                if (response.StatusCode == HttpStatusCode.RequestedRangeNotSatisfiable)
                {
                    response.Dispose();
                    if (File.Exists(savePath))
                        File.Delete(savePath);
                    // 重新全量下载
                    await DownloadWithoutResumeAsync(url, savePath, progress, cancellationToken, timeout);
                    return;
                }
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.RequestedRangeNotSatisfiable)
            {
                if (File.Exists(savePath))
                    File.Delete(savePath);
                await DownloadWithoutResumeAsync(url, savePath, progress, cancellationToken, timeout);
                return;
            }

            long? totalBytes = response.Content.Headers.ContentLength + existingLength;
            var canResume = response.StatusCode == HttpStatusCode.PartialContent;

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var fileStream = new FileStream(savePath, canResume ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.None);

            var buffer = new byte[8192];
            long totalRead = existingLength;
            int bytesRead;

            while ((bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                totalRead += bytesRead;

                if (totalBytes.HasValue && progress != null)
                {
                    double percent = (double)totalRead / totalBytes.Value * 100;
                    progress.Report(percent);
                }
            }
        }

        // 新增：无断点续传的全量下载方法
        private static async Task DownloadWithoutResumeAsync(
            string url,
            string savePath,
            IProgress<double>? progress,
            CancellationToken cancellationToken,
            int? timeout = null)
        {
            using var client = new HttpClient();
            if (timeout.HasValue)
            {
                client.Timeout = TimeSpan.FromSeconds(timeout.Value);
            }

            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength;
            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var fileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None);

            var buffer = new byte[8192];
            long totalRead = 0;
            int bytesRead;

            while ((bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                totalRead += bytesRead;

                if (totalBytes.HasValue && progress != null)
                {
                    double percent = (double)totalRead / totalBytes.Value * 100;
                    progress.Report(percent);
                }
            }
        }
    }
}
