using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Spectre.Console;
using SRAUpdaterPlus.Tool;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Enumeration;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using logger = SRAUpdaterPlus.Tool.LogHelper;
using msgbox = iNKORE.UI.WPF.Modern.Controls.MessageBox;
using Path = System.IO.Path;

namespace SRAUpdaterPlus
{
    public partial class MainWindow : Window
    {
        private string downUrl;
        private string proxy;
        private int timeout;
        private bool disableProxy;
        private bool forceUpdate;
        private bool disableSSL;
        private bool fileIntegrityCheck;
        private bool useOfficialSoftware;
        private bool isDetailVisible = false;
        private double originalHeight;

        // 无参数构造函数（给 XAML 用）
        public MainWindow() : this(null, null, 10, false, false, false, false, false)
        {
        }

        // 实际用来传参的构造函数
        public MainWindow(
            string downUrl,
            string proxy,
            int timeout,
            bool disableProxy,
            bool forceUpdate,
            bool disableSSL,
            bool fileIntegrityCheck,
            bool useOfficialSoftware)
        {
            InitializeComponent();

            // 将参数存储到字段中
            this.downUrl = downUrl;
            this.proxy = proxy;
            this.timeout = timeout;
            this.disableProxy = disableProxy;
            this.forceUpdate = forceUpdate;
            this.disableSSL = disableSSL;
            this.fileIntegrityCheck = fileIntegrityCheck;
            this.useOfficialSoftware = useOfficialSoftware;

            originalHeight = this.Height;

            LogHelper.LogAdded += log =>
            {
                Dispatcher.Invoke(() =>
                {
                    LogTextBox.AppendText(log + Environment.NewLine);
                    LogTextBox.ScrollToEnd();
                });
            };
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (useOfficialSoftware)
                {
                    await Migration();
                    return;
                }
                if (downUrl != null)
                {
                    string FileName = DownloadHelper.GetFileNameFromUrl(downUrl);
                    string targetPath = Path.Combine(Parameter.LOCATED_DIR, FileName);
                    await DownloadAsync(downUrl, targetPath, disableSSL, FileName);

                }
                if (fileIntegrityCheck)
                {
                    if (!await CheckAndRepairFileIntegrity())
                        return;
                }
                else
                {
                    await InitializeUpdate();
                    if (!await DownloadVersionFile())
                        return;
                    if (!await CheckAndInstallIfNeeded())
                        return;
                    if (await CheckAndPerformUpdate() == 1)
                    {
                        progressLabel.Content = "无可用更新。";
                        await Task.Delay(500);
                        Application.Current.Shutdown();
                    }
                }

            }
            catch (Exception ex)
            {
                logger.Error($"更新过程中发生错误: {ex.Message}");
                progressLabel.Content = "更新过程中发生错误，请查看详细信息。";
                showProcess(-1, true, false);
            }
        }

        private async Task Migration()
        {
            progressLabel.Content = "正在进行准备...";

            await Task.Delay(500);
            if (Parameter.LOCATED_DIR != AppDomain.CurrentDomain.BaseDirectory)
            {
                await DownloadVersionFile();
                var remotePath = Path.Combine(Parameter.SAVE_PATH, "version.json");
                var remoteInfo = await GetVersionInfo(remotePath);

                progressLabel.Content = $"该程序将为您进行最后一次更新...";
                await Task.Delay(300);
                logger.Info("进行资源下载。");
                string download_url = Parameter.TARGET_URL.Replace("{0}", remoteInfo.Version);
                if (!await DownloadAsync(download_url, Path.Combine(Parameter.SAVE_PATH, "sra.zip"), disableSSL, $"StarRailAssistant_v{remoteInfo.Version}.zip"))
                    return;
                progressLabel.Content = "正在解压缩文件...";
                showProcess(-1, false, false);
                if (!await ExtractZip(Parameter.LOCATED_DIR, Parameter.LOCATED_DIR, new List<string> { }))
                    return;
                File.Delete(Path.Combine(Parameter.SAVE_PATH, "sra.zip"));
                File.Delete(Path.Combine(Parameter.SAVE_PATH, "version.json"));
                progressLabel.Content = "感谢使用SRAUpdaterPlus，重启电脑后，你将不会看到SRAUpdaterPlus的身影。";
                if (await WaitForUserConfirmationAsync("完成"))
                {
                    Application.Current.Shutdown();
                }
            }
            else
            {
                // 获取自身路径
                string currentExe = Process.GetCurrentProcess().MainModule.FileName;
                string exeName = Path.GetFileName(currentExe);

                // 构建临时目录路径
                string tempDir = Path.Combine(Path.GetTempPath(), "SRAU_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tempDir);
                string tempExePath = Path.Combine(tempDir, exeName);

                // 复制自身到临时目录
                LogHelper.Debug($"当前：{currentExe};目标：{tempExePath}");
                File.Copy(currentExe, tempExePath, true);
                LogHelper.Info(Directory.GetParent(currentExe).FullName + "\\SRAUpdaterPlus.dll");
                if (File.Exists(Directory.GetParent(currentExe).FullName + "\\SRAUpdaterPlus.dll"))
                {

                    File.Copy(Directory.GetParent(currentExe).FullName + "\\SRAUpdaterPlus.dll", Path.Combine(tempDir, "SRAUpdaterPlus.dll"), true);
                }
                if (File.Exists(Directory.GetParent(currentExe).FullName + "\\SRAUpdaterPlus.deps.json"))
                {

                    File.Copy(Directory.GetParent(currentExe).FullName + "\\SRAUpdaterPlus.deps.json", Path.Combine(tempDir, "SRAUpdaterPlus.deps.json"), true);
                }

                // 参数
                string arguments = "";
                if (!string.IsNullOrEmpty(proxy))
                    arguments += $" --use-proxy \"{proxy}\"";

                if (disableProxy)
                    arguments += " --disable-proxy";

                if (disableSSL)
                    arguments += " --disable-SSL";

                if (forceUpdate)
                    arguments += " --force-update";

                if (timeout > 0)
                    arguments += $" --timeout {timeout}";

                arguments += $" -SRAL-Disable {Parameter.LOCATED_DIR}";

                // 启动复制后的程序，并带上参数
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = tempExePath,
                    Arguments = arguments,
                    UseShellExecute = true
                };
                Process.Start(psi);
                Environment.Exit(0);
            }
        }

        private async Task InitializeUpdate()
        {
            progressLabel.Content = "正在开始更新...";
            await Task.Delay(500);
            logger.Info("正在获取更新信息...");
            await Task.Delay(300);
        }

        private async Task<bool> DownloadVersionFile()
        {
            if (File.Exists(Path.Combine(Parameter.SAVE_PATH, "version.json")))
            {
                File.Delete(Path.Combine(Parameter.SAVE_PATH, "version.json"));
                logger.Warn("删除了旧的远程版本信息文件。");
            }
            if (!await DownloadAsync(Parameter.VERSION_REMOTE_URL,
                Path.Combine(Parameter.SAVE_PATH, "version.json"),
                disableSSL, "version.json"))
            {
                progressLabel.Content = "在获取更新信息时发生了错误，点击详细信息按钮来查看问题。";
                showProcess(-1, true, false);
                return false;
            }
            return true;
        }

        private async Task<bool> CheckAndInstallIfNeeded()
        {
            if (File.Exists(Path.Combine(Parameter.LOCATED_DIR, "SRA.exe")))
                return true;

            progressLabel.Content = "SRA似乎不存在，请确认是否进行安装。";
            if (!await WaitForUserConfirmationAsync("安装"))
                return false;

            var versionInfo = await GetVersionInfo(Path.Combine(Parameter.SAVE_PATH, "version.json"));
            if (!await DownloadAndExtractUpdate(versionInfo.Version, "安装"))
                return false;

            return true;
        }

        private async Task<bool> CheckAndRepairFileIntegrity()
        {
            try
            {
                logger.Info("开始进行文件完整性检查...");
                progressLabel.Content = "正在检查文件完整性...";
                showProcess(0, false, false);

                var progress = new Progress<double>(p =>
                {
                    progressLabel.Content = $"正在检查文件完整性，进度：{p:0.00}%";
                    showProcess((int)p, false, false);
                });

                var corruptedFiles = await FileIntegrityChecker.CheckIntegrityAsync(progress);

                if (corruptedFiles.Count == 0)
                {
                    progressLabel.Content = "文件完整性检查通过。";
                    showProcess(100, false, false);
                    logger.Info("所有文件完整性检查通过。");
                    return true;
                }

                logger.Warn($"发现{corruptedFiles.Count}个文件存在问题，询问用户是否修复。");
                progressLabel.Content = $"发现{corruptedFiles.Count}个文件存在问题，请确认是否进行修复。";
                showProcess(-1, false, true);

                if (await WaitForUserConfirmationAsync("修复"))
                {
                    logger.Info("开始修复损坏的文件...");
                    progressLabel.Content = "正在修复损坏的文件...";
                    showProcess(0, false, false);

                    var repairProgress = new Progress<double>(p =>
                    {
                        progressLabel.Content = $"正在修复文件，进度：{p:0.00}%";
                        showProcess((int)p, false, false);
                    });

                    bool repairSuccess = await FileIntegrityChecker.RepairFilesAsync(corruptedFiles, disableSSL, !disableProxy, repairProgress);

                    if (repairSuccess)
                    {
                        progressLabel.Content = "文件修复完成。";
                        showProcess(100, false, false);
                        logger.Info("文件修复成功完成。");
                        return true;
                    }
                    else
                    {
                        progressLabel.Content = "文件修复失败，请查看详细信息。";
                        showProcess(-1, true, false);
                        logger.Error("文件修复失败。");
                        return false;
                    }
                }
                if (await WaitForUserConfirmationAsync("完成"))
                {
                    Application.Current.Shutdown();
                }
                return false;
            }
            catch (Exception ex)
            {
                logger.Error($"文件完整性检查过程中发生错误: {ex.Message}");
                progressLabel.Content = "文件完整性检查过程中发生错误，请查看详细信息。";
                showProcess(-1, true, false);
                return false;
            }
        }

        private async Task<int> CheckAndPerformUpdate()
        {
            var (needUpdate, versionInfo) = await CheckUpdateRequired();
            if (!needUpdate && !forceUpdate)
                return 1;
            if (!forceUpdate)
            {
                logger.Info($"发现最新版本{versionInfo.Version}高于当前版本。");
                progressLabel.Content = $"发现新版本{versionInfo.Version}，请点击详细信息查看更多信息。";
                logger.Info($"内容信息：\n{versionInfo.Announcement}");
                if (await WaitForUserConfirmationAsync("更新"))
                {
                    if (await DownloadAndExtractUpdate(versionInfo.Version, "更新"))
                        return 0;
                }
                return -1;
            }
            else
            {
                logger.Info($"发现版本{versionInfo.Version}，将强制更新");
                progressLabel.Content = $"发现版本{versionInfo.Version}。请查看详细信息。";
                logger.Info($"内容信息：\n{versionInfo.Announcement}");
                if (await WaitForUserConfirmationAsync("更新"))
                {
                    if (await DownloadAndExtractUpdate(versionInfo.Version, "更新"))
                        return 0;
                }
                return -1;
            }

        }

        private async Task<(bool NeedUpdate, VersionInfo Info)> CheckUpdateRequired()
        {
            var remotePath = Path.Combine(Parameter.SAVE_PATH, "version.json");
            var localPath = Path.Combine(Parameter.LOCATED_DIR, "version.json");

            var remoteInfo = await GetVersionInfo(remotePath);
            var localInfo = await GetVersionInfo(localPath);

            progressLabel.Content = $"程序：当前版本：{localInfo.Version}，最新版本：{remoteInfo.Version}...";
            await Task.Delay(300);
            progressLabel.Content = $"资源：当前版本：{localInfo.ResourceVersion}，最新版本：{remoteInfo.ResourceVersion}...";
            await Task.Delay(300);

            return (remoteInfo.Version.CompareTo(localInfo.Version) == 1, remoteInfo);
        }

        private async Task<bool> DownloadAndExtractUpdate(string version, string operation)
        {
            string download_url = string.Format(Parameter.TARGET_URL, version);
            string target_path = Path.Combine(Parameter.SAVE_PATH, "sra.zip");
            logger.Debug($"下载链接：{download_url}，保存路径：{target_path}");

            if (!await DownloadAsync(download_url, target_path, disableSSL, $"StarRailAssistant_v{version}.zip"))
                return false;

            progressLabel.Content = "正在解压缩文件...";
            showProcess(-1, false, false);
            if (!await ExtractZip(target_path, Parameter.LOCATED_DIR, new List<string> { "/SRAUpdater.exe" }))
                return false;

            progressLabel.Content = $"{operation}成功。";
            CancelBtn.IsEnabled = false;
            for (int i = 0; i < 101; i += 5)
            {
                showProcess(i, false, false);
                await Task.Delay(10);
            }

            File.Delete(target_path);
            File.Delete(Path.Combine(Parameter.SAVE_PATH, "version.json"));

            if (await WaitForUserConfirmationAsync("完成"))
            {
                Application.Current.Shutdown();
            }
            return true;
        }

        private async Task<VersionInfo> GetVersionInfo(string path)
        {
            var json = await File.ReadAllTextAsync(path);
            var obj = JObject.Parse(json);

            string announcement = obj["announcement"]?.ToString() ?? "";
            var announcementLines = announcement.Split("\n")
                .Select(line => line.Replace("####", "").Replace("###", "").Replace("##", "").Trim())
                .Where(line => !string.IsNullOrEmpty(line));

            return new VersionInfo
            {
                Version = obj["version"].ToString(),
                ResourceVersion = obj["resource_version"].ToString(),
                Announcement = string.Join("\n", announcementLines)
            };
        }

        private class VersionInfo
        {
            public string Version { get; set; }
            public string ResourceVersion { get; set; }
            public string Announcement { get; set; }
            public string RemoteVersion { get; set; } // 添加 RemoteVersion 属性
        }

        static async Task<bool> ExtractZip(string Path, string TargetPath, List<string> Exclude)
        {
            try
            {
                await ZipExtractor.ExtractZipAsync(Path, TargetPath, Exclude);
                logger.Info($"解压缩文件成功，已保存到：{TargetPath}，排除了{Exclude.Count}个文件。");
                return true;
            }
            catch (Exception ex)
            {
                logger.Error($"解压缩文件失败，原因：{ex.Message}");
                return false;
            }
        }

        private readonly Queue<TaskCompletionSource<bool>> _actionQueue = new();
        private readonly object _lock = new();

        public Task<bool> WaitForUserConfirmationAsync(string information)
        {
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            lock (_lock)
            {
                _actionQueue.Enqueue(tcs);
                Application.Current.Dispatcher.Invoke(() => {
                    ActionBtn.IsEnabled = true;
                    ActionBtn.Content = information;
                });
            }

            return tcs.Task;
        }

        // 下载
        private async Task<bool> DownloadAsync(string url, string targetPath, bool disableSSL, string fileName = "")
        {
            var progress = new Progress<double>(p =>
            {
                progressLabel.Content = $"正在下载文件{fileName}，下载进度：{p:0.00}%";
                showProcess((int)p, false, false);
            });

            var cts = new CancellationTokenSource();

            try
            {
                await DownloadHelper.DownloadFileAsync(
                    url,
                    targetPath,
                    disableSSL: disableSSL,
                    progress: progress,
                    cancellationToken: cts.Token,
                    timeout: timeout,
                    useProxy: !disableProxy
                );

                progressLabel.Content = $"文件{fileName}下载完成，正在进行下一步...";
                showProcess(-1, false, true);
                logger.Info($"文件{fileName}下载完成。");
                return true;

            }
            catch (Exception ex)
            {
                progressLabel.Content = $"文件{fileName}下载失败...";
                showProcess(-1, true, false);
                logger.Error($"文件{fileName}下载失败，原因：{ex.Message}");
                return false;
            }
        }

        private void showProcess(int value = -1, bool showError = false, bool showPause = true)
        {
            if (value < 0)
            {
                progressBar.Visibility = Visibility.Collapsed;
                progressBar.Value = 0;
                statusBar.Visibility = Visibility.Visible;
                statusBar.ShowError = false;
                statusBar.ShowPaused = false;
                if (showError)
                {
                    statusBar.ShowError = true;
                }
                if (showPause)
                {
                    statusBar.ShowPaused = true;
                }
            }
            else
            {
                progressBar.Visibility = Visibility.Visible;
                progressBar.Value = value;
                statusBar.Visibility = Visibility.Collapsed;
                statusBar.ShowError = false;
                statusBar.ShowPaused = false;
            }
        }

        private void ShowDetailBtn_Click(object sender, RoutedEventArgs e)
        {
            if (isDetailVisible)
            {
                LogTextBox.Visibility = Visibility.Collapsed;
                this.Height = originalHeight;
                ShowDetailBtn.Content = "详细信息";
            }
            else
            {
                LogTextBox.Visibility = Visibility.Visible;
                this.Height = originalHeight + LogTextBox.Height + 10; // 20 是间距估计
                ShowDetailBtn.Content = "隐藏信息";
            }

            isDetailVisible = !isDetailVisible;
        }

        private void ActionBtn_Click(object sender, RoutedEventArgs e)
        {
            TaskCompletionSource<bool>? nextTcs = null;

            lock (_lock)
            {
                if (_actionQueue.Count > 0)
                    nextTcs = _actionQueue.Dequeue();

                if (_actionQueue.Count == 0)
                {
                    ActionBtn.IsEnabled = false;
                    ActionBtn.Content = "执行操作";
                }
            }

            nextTcs?.SetResult(true); // 唤醒等待者
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }

}