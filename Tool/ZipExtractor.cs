using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRAUpdaterPlus.Tool
{
    public static class ZipExtractor
    {
        /// <summary>
        /// 异步解压ZIP文件到指定目录，并排除指定文件。
        /// </summary>
        /// <param name="zipPath">ZIP文件路径</param>
        /// <param name="extractPath">解压到的目标目录</param>
        /// <param name="exclude">排除的路径（如 /version.json）</param>
        public static async Task ExtractZipAsync(string zipPath, string extractPath, List<string>? exclude = null)
        {
            if (!File.Exists(zipPath))
                throw new FileNotFoundException($"未找到ZIP文件: {zipPath}");

            // 检查并结束SRA进程
            var sraProcesses = Process.GetProcessesByName("SRA");
            if (sraProcesses.Length > 0)
            {
                LogHelper.Info("检测到正在运行的SRA进程，正在尝试结束...");
                foreach (var process in sraProcesses)
                {
                    try
                    {
                        process.Kill();
                        process.WaitForExit();
                        LogHelper.Info("成功结束SRA进程");
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Error($"结束SRA进程时出错: {ex.Message}");
                        throw new InvalidOperationException("无法结束SRA进程，请手动关闭SRA后重试");
                    }
                }
            }

            if (!Directory.Exists(extractPath))
                Directory.CreateDirectory(extractPath);

            exclude ??= new List<string>();

            using ZipArchive archive = ZipFile.OpenRead(zipPath);

            foreach (var entry in archive.Entries)
            {
                string normalizedPath = "/" + entry.FullName.Replace('\\', '/').TrimStart('/');

                if (exclude.Any(e => string.Equals(e.Trim(), normalizedPath, StringComparison.OrdinalIgnoreCase)))
                    continue;

                string destinationPath = Path.Combine(extractPath, entry.FullName);

                if (string.IsNullOrEmpty(entry.Name))
                {
                    Directory.CreateDirectory(destinationPath);
                    continue;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);

                if (File.Exists(destinationPath))
                    File.Delete(destinationPath);

                using var entryStream = entry.Open();
                using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, useAsync: true);
                await entryStream.CopyToAsync(fileStream);
            }
        }
    }
}
