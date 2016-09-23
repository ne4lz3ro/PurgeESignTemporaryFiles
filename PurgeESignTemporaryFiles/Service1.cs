using System;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Configuration;
using System.Threading;
using NLog;

namespace PurgeESignTemporaryFiles
{
    public partial class PurgeService : ServiceBase
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private Timer _timer = null;
        private string CURRENT_MONTH = $"{DateTime.Today.Year}{DateTime.Today.Month.ToString().PadLeft(2, '0')}";
        public PurgeService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            logger.Debug("e投保刪除檔案服務開始");
            var scheduledRunTime = ConfigurationManager.AppSettings["ScheduledRunTime"];
            var scheduledRunTimeArrary = scheduledRunTime.Split(':');
            var scheduledRunTimeHours = int.Parse(scheduledRunTimeArrary[0]);
            var scheduledRunTimeMinutes = int.Parse(scheduledRunTimeArrary[1]);
            var scheduledRunTimeSeconds = int.Parse(scheduledRunTimeArrary[2]);

            var timeBetweenEachRun = ConfigurationManager.AppSettings["TimeBetweenEachRun"];
            var timeBetweenEachRunArray = timeBetweenEachRun.Split(':');
            var timeBetweenEachRunHours = int.Parse(timeBetweenEachRunArray[0]);
            var timeBetweenEachRunMinutes = int.Parse(timeBetweenEachRunArray[1]);
            var timeBetweenEachRunSeconds = int.Parse(timeBetweenEachRunArray[2]);

            StartTimer(new TimeSpan(scheduledRunTimeHours, scheduledRunTimeMinutes, scheduledRunTimeSeconds),
                new TimeSpan(timeBetweenEachRunHours, timeBetweenEachRunMinutes, timeBetweenEachRunSeconds));
        }

        protected override void OnStop()
        {
            logger.Debug("e投保刪除檔案服務停止");
        }

        public void StartTimer(TimeSpan scheduledRunTime, TimeSpan timeBetweenEachRun)
        {
            double current = DateTime.Now.TimeOfDay.TotalMilliseconds;
            double scheduledTime = scheduledRunTime.TotalMilliseconds;
            double intervalPeriod = timeBetweenEachRun.TotalMilliseconds;
            double firstExecution = current > scheduledTime ? intervalPeriod + (intervalPeriod - current) : scheduledTime - current;
            TimerCallback callback = new TimerCallback(RunService);
            logger.Debug(firstExecution);
            _timer = new Timer(callback, null, Convert.ToInt32(firstExecution), Convert.ToInt32(intervalPeriod));
        }

        public void RunService(object state)
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
        }

        private void DeleteXMLFiles()
        {
            var filePath = ConfigurationManager.AppSettings["ProcessXMLFilePath"].Replace("%currentMonth%", CURRENT_MONTH);
            var reservedDay = int.Parse(ConfigurationManager.AppSettings["ProcessXMLReservedDays"]);
            var deleteFiles = 0;
            PurgeFilePathInfo(filePath, reservedDay, out deleteFiles);
            logger.Debug($"刪除{deleteFiles}個檔案，Delete XML files finished");
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
        }

        private void PurgeFilePathInfo(string filePath, int day, out int files)
        {
            logger.Debug($"刪除檔案的目錄路徑: {filePath}");
            files = 0;
            try
            {
                if (!Directory.Exists(filePath))
                {
                    Directory.CreateDirectory(filePath);
                }
                var fileList = Directory.GetFiles(filePath, "*.*", SearchOption.AllDirectories).Where(d => new FileInfo(d).LastWriteTime < DateTime.Today.AddDays(-day));
                files = fileList.Count();
                foreach (string file in fileList)
                {
                    File.Delete(file);
                }
            }
            catch (DirectoryNotFoundException dirNotFound)
            {
                Debug.WriteLine(dirNotFound.Message);
                string sEvent;
                sEvent = dirNotFound.Message;
                logger.Error(sEvent);
            }
            catch(Exception ex)
            {
                logger.Error($"Source:{ex.Source}, Messages:{ex.Message}");
            }
        }

    }
}
