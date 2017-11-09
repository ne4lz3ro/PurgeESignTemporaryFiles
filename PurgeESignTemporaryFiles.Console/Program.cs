using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace PurgeESignTemporaryFiles.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            var purgeServcie = new PurgeService();
            purgeServcie.RunService();            
        }
    }

    public class PurgeService 
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private Timer _timer = null;
        private string CURRENT_MONTH = $"{DateTime.Today.Year}{DateTime.Today.Month.ToString().PadLeft(2, '0')}";

        public void StartTimer(TimeSpan scheduledRunTime, TimeSpan timeBetweenEachRun)
        {
            double current = DateTime.Now.TimeOfDay.TotalMilliseconds;
            double scheduledTime = scheduledRunTime.TotalMilliseconds;
            double intervalPeriod = timeBetweenEachRun.TotalMilliseconds;
            double firstExecution = current > scheduledTime ? intervalPeriod + (intervalPeriod - current) : scheduledTime - current;
            //TimerCallback callback = new TimerCallback(RunService());
            //logger.Debug(firstExecution);
            //_timer = new Timer(callback, null, Convert.ToInt32(firstExecution), Convert.ToInt32(intervalPeriod));
        }

        public void RunService()
        {
            try
            {
                if (ConfigurationManager.AppSettings["ServiceStatus"] == "On")
                {
                    logger.Debug("e投保刪除檔案開始執行");
                    DeleteProcessLog();
                    DeleteXMLFiles();
                    DeleteSignedPdfFiles();
                    DeleteWSUPLoadFData();
                    DeleteWSUploadFileLog();
                    DeleteZipFileBackup();
                    CheckDiskSpace();
                }
            }
            catch (Exception ex)
            {
                logger.Error($"錯誤訊息:{ex.Message}");
            }

            logger.Debug("e投保刪除檔案執行完成");
        }

        private void CheckDiskSpace()
        {
            DriveInfo info = new DriveInfo("D");
            var formatDivideBy = Math.Pow(1024, 3);
            var availableSpace = info.TotalFreeSpace / formatDivideBy;
            if (availableSpace < 1)
            {
                logger.Fatal($"空間不足，目前磁碟空間剩餘{availableSpace} M");
            }
        }

        public void DeleteProcessLog()
        {
            var filePath = ConfigurationManager.AppSettings["ProcessLogFilePath"].Replace("%currentMonth%", CURRENT_MONTH);
            var reservedDay = int.Parse(ConfigurationManager.AppSettings["ProcessLogReservedDays"]);
            var deleteFiles = 0;
            PurgeFilePathInfo(filePath, reservedDay, out deleteFiles);
            logger.Debug($"刪除{deleteFiles}個檔案，Delete process log finished");

            var checkDay = ConfigurationManager.AppSettings["CheckLastMonthFilesDay"];
            if (DateTime.Today.Day.Equals(int.Parse(checkDay)))
            {
                var lastMonth = $"{DateTime.Today.Year}{(DateTime.Today.Month - 1).ToString().PadLeft(2, '0')}";
                var lastFilePath = ConfigurationManager.AppSettings["ProcessLogFilePath"].Replace("%currentMonth%", lastMonth);
                PurgeFilePathInfo(lastFilePath, -1, out deleteFiles);
                logger.Debug($"刪除上個月{deleteFiles}個檔案，刪除上個月process檔案已完成");
            }
        }

        private void DeleteXMLFiles()
        {
            var filePath = ConfigurationManager.AppSettings["ProcessXMLFilePath"].Replace("%currentMonth%", CURRENT_MONTH);
            var reservedDay = int.Parse(ConfigurationManager.AppSettings["ProcessXMLReservedDays"]);
            var deleteFiles = 0;
            PurgeFilePathInfo(filePath, reservedDay, out deleteFiles);
            logger.Debug($"刪除{deleteFiles}個檔案，Delete XML files finished");

            var checkDay = ConfigurationManager.AppSettings["CheckLastMonthFilesDay"];
            if (DateTime.Today.Day.Equals(int.Parse(checkDay)))
            {
                var lastMonth = $"{DateTime.Today.Year}{(DateTime.Today.Month - 1).ToString().PadLeft(2, '0')}";
                var lastFilePath = ConfigurationManager.AppSettings["ProcessXMLFilePath"].Replace("%currentMonth%", lastMonth);
                PurgeFilePathInfo(lastFilePath, -1, out deleteFiles);
                logger.Debug($"刪除上個月{deleteFiles}個檔案，刪除上個月XML檔案已完成");
            }
        }

        private void DeleteSignedPdfFiles()
        {
            var filePath = ConfigurationManager.AppSettings["SignedPdfFilesFilePath"];
            var reservedDay = int.Parse(ConfigurationManager.AppSettings["SignedPdfFilesReservedDays"]);
            var deleteFiles = 0;
            PurgeFilePathInfo(filePath, reservedDay, out deleteFiles);
            logger.Debug($"刪除{deleteFiles}個檔案，Delete Signed files finished");
        }

        private void DeleteWSUPLoadFData()
        {
            var filePath = ConfigurationManager.AppSettings["WSUPLoadFDataFilePath"];
            var reservedDay = int.Parse(ConfigurationManager.AppSettings["WSUPLoadFDataReservedDays"]);
            var deleteFiles = 0;
            PurgeFilePathInfo(filePath, reservedDay, out deleteFiles);
            logger.Debug($"刪除{deleteFiles}個檔案，Delete WSUPLoadFData finished");
        }

        private void DeleteWSUploadFileLog()
        {
            var filePath = ConfigurationManager.AppSettings["WSUploadFileLogFilePath"];
            var reservedDay = int.Parse(ConfigurationManager.AppSettings["WSUploadFileLogReservedDays"]);
            var deleteFiles = 0;
            PurgeFilePathInfo(filePath, reservedDay, out deleteFiles);
            logger.Debug($"刪除{deleteFiles}個檔案，Delete WSUpload File log finished");
        }

        private void DeleteZipFileBackup()
        {
            var filePath = ConfigurationManager.AppSettings["ZipFileBackupFilePath"].Replace("%currentMonth%", CURRENT_MONTH);
            var reservedDay = int.Parse(ConfigurationManager.AppSettings["ZipFileBackupReservedDays"]);
            var deleteFiles = 0;
            PurgeFilePathInfo(filePath, reservedDay, out deleteFiles);
            logger.Debug($"刪除{deleteFiles}個檔案，刪除影像備份檔案已完成");

            if (DateTime.Today.Day != reservedDay) return;
            var lastMonth = $"{DateTime.Today.Year}{(DateTime.Today.Month - 1).ToString().PadLeft(2, '0')}";
            var lastFilePath = ConfigurationManager.AppSettings["ZipFileBackupFilePath"].Replace("%currentMonth%", lastMonth);
            PurgeFilePathInfo(lastFilePath, -1, out deleteFiles);
            logger.Debug($"刪除上個月{deleteFiles}個檔案，刪除影像備份檔案已完成");
        }

        private void PurgeFilePathInfo(string filePath, int day, out int files)
        {
            logger.Debug($"刪除檔案的目錄路徑: {filePath},保留天數 {day}");
            files = 0;
            try
            {
                if (!Directory.Exists(filePath))
                {
                    Directory.CreateDirectory(filePath);
                }
                string[] fileList;
                string[] directoryList = null;
                if (day.Equals(-1))
                {
                    fileList = Directory.GetFiles(filePath, "*.*", SearchOption.AllDirectories);
                    directoryList = Directory.GetDirectories(filePath, "*.*", SearchOption.AllDirectories);
                }
                else
                {
                    fileList = new DirectoryInfo(filePath).GetFiles("*.*", SearchOption.AllDirectories).Where(f=>f.LastWriteTime < DateTime.Today.AddDays(-day)).Select(f => f.FullName).ToArray();
                }

                if (fileList == null) return;
                files = fileList.Count();
                foreach (string file in fileList)
                {                    
                    File.Delete(file);
                }
                if(directoryList != null)
                {
                    foreach(string directory in directoryList)
                    {
                        if (Directory.Exists(directory))
                        {
                            try
                            {
                                Directory.Delete(directory);
                            }
                            catch(Exception ex)
                            {
                                logger.Error($"Source:{ex.Source}, Messages:{ex.Message}");
                            }
                        }
                    }
                }
            }
            catch (DirectoryNotFoundException dirNotFound)
            {
                Debug.WriteLine(dirNotFound.Message);
                string sEvent;
                sEvent = dirNotFound.Message;
                logger.Error(sEvent);
            }
            catch (Exception ex)
            {
                logger.Error($"Source:{ex.Source}, Messages:{ex.Message}");
            }
        }

    }
}
