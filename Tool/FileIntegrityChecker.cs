using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using SRAUpdaterPlus.Tool;
using logger = SRAUpdaterPlus.Tool.LogHelper;

namespace SRAUpdaterPlus.Tool
{
    public class FileIntegrityChecker
    {

        public static async Task<List<string>> CheckIntegrityAsync(IProgress<double> progress = null)
        {
            logger.Info("开始文件完整性检查...");
            var corruptedFiles = new List<string>();

            try
            {
                // 下载hash文件
                var hashData = await DownloadHashFileAsync();
                if (hashData == null)
                {
                    logger.Error("无法下载hash文件，跳过完整性检查");
                    return corruptedFiles;
                }

                var totalFiles = hashData.Count;
                var checkedFiles = 0;

                logger.Info($"开始检查{totalFiles}个文件的完整性...");

                foreach (var kvp in hashData)
                {
                    var relativePath = kvp.Key;
                    var expectedHash = kvp.Value;
                    var fullPath = Path.Combine(Parameter.LOCATED_DIR, relativePath);

                    // 如果是 SRAUpdater.exe，则跳过检查
                    if (Path.GetFileName(fullPath).Equals("SRAUpdater.exe", StringComparison.OrdinalIgnoreCase))
                    {
                        logger.Debug($"跳过 SRAUpdater.exe 的完整性检查: {relativePath}");
                    }
                    // 检查文件是否存在
                    else if (!File.Exists(fullPath))
                    {
                        logger.Warn($"文件不存在: {relativePath}");
                        corruptedFiles.Add(relativePath);
                    }
                    else
                    {
                        // 计算文件hash
                        var actualHash = await ComputeFileHashAsync(fullPath);
                        if (!string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase))
                        {
                            logger.Warn($"文件hash不匹配: {relativePath}, 期望: {expectedHash}, 实际: {actualHash}");
                            corruptedFiles.Add(relativePath);
                        }
                        else
                        {
                            logger.Debug($"文件完整性检查通过: {relativePath}");
                        }
                    }

                    checkedFiles++;
                    progress?.Report((double)checkedFiles / totalFiles * 100);
                }

                if (corruptedFiles.Count > 0)
                {
                    logger.Warn($"发现{corruptedFiles.Count}个文件存在问题");
                }
                else
                {
                    logger.Info("所有文件完整性检查通过");
                }

                return corruptedFiles;
            }
            catch (Exception ex)
            {
                logger.Error($"文件完整性检查过程中发生错误: {ex.Message}");
                return corruptedFiles;
            }
        }

        public static async Task<bool> RepairFilesAsync(List<string> corruptedFiles, bool disableSSL, bool useProxy, IProgress<double> progress = null)
        {
            if (corruptedFiles == null || corruptedFiles.Count == 0)
                return true;

            logger.Info($"开始修复{corruptedFiles.Count}个损坏的文件...");

            var repairedCount = 0;
            var totalFiles = corruptedFiles.Count;

            try
            {
                foreach (var relativePath in corruptedFiles)
                {
                    var fullPath = Path.Combine(Parameter.LOCATED_DIR, relativePath);
                    var downloadUrl = $"https://resource.starrailassistant.top/SRA/{relativePath.Replace('\\', '/')}"; // 确保使用正斜杠

                    logger.Info($"正在修复文件: {relativePath}");

                    try
                    {
                        // 如果文件存在，先删除
                        if (File.Exists(fullPath))
                        {
                            File.Delete(fullPath);
                            logger.Debug($"删除损坏的文件: {fullPath}");
                        }

                        // 确保目录存在
                        var directory = Path.GetDirectoryName(fullPath);
                        if (!Directory.Exists(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }

                        // 下载文件
                        await DownloadHelper.DownloadFileAsync(
                            downloadUrl,
                            fullPath,
                            disableSSL: disableSSL,
                            useProxy: useProxy
                        );

                        logger.Info($"文件修复成功: {relativePath}");
                    }
                    catch (Exception ex)
                    {
                        logger.Error($"修复文件失败: {relativePath}, 错误: {ex.Message}");
                        return false;
                    }

                    repairedCount++;
                    progress?.Report((double)repairedCount / totalFiles * 100);
                }

                logger.Info($"成功修复{repairedCount}个文件");
                return true;
            }
            catch (Exception ex)
            {
                logger.Error($"文件修复过程中发生错误: {ex.Message}");
                return false;
            }
        }

        private static async Task<Dictionary<string, string>> DownloadHashFileAsync()
        {
            try
            {
                logger.Info("正在下载hash文件...");
                
                using var httpClient = new HttpClient();
                var response = await httpClient.GetStringAsync(Parameter.HASH_URL);
                
                var hashData = JObject.Parse(response);
                var result = new Dictionary<string, string>();
                
                foreach (var property in hashData.Properties())
                {
                    result[property.Name] = property.Value.ToString();
                }
                
                logger.Info($"成功下载hash文件，包含{result.Count}个文件信息");
                return result;
            }
            catch (Exception ex)
            {
                logger.Error($"下载hash文件失败: {ex.Message}");
                return null;
            }
        }

        private static async Task<string> ComputeFileHashAsync(string filePath)
        {
            using var sha256 = SHA256.Create();
            using var stream = File.OpenRead(filePath);
            var hashBytes = await Task.Run(() => sha256.ComputeHash(stream));
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }
    }
}
