using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace Picture
{
    internal class FileInfoList : IEnumerable
    {
        List<FileInfo> InternelList = new();
        public int Length
        {
            get
            { return InternelList.Count; }
        }
        public void Add(FileInfo info)
        {
            InternelList.Add(info);
        }
        public void Remove(FileInfo info) 
        {  
            InternelList.Remove(info); 
        }
        public FileInfo this[int index]
        {
            get
            {
                return InternelList[index];
            }
        }
        public void Merge(FileInfoList TargetList)//合并操作
        {
            foreach (FileInfo fileinfo in TargetList)
                InternelList.Add(fileinfo);
        }
        public void Merge(List<FileInfo> TargetList)//合并操作
        {
            foreach (FileInfo fileinfo in TargetList)
                InternelList.Add(fileinfo);
        }
        public IEnumerator GetEnumerator()
        {
            return new FileInfoListEnumerator(this); 
        }
        class FileInfoListEnumerator : IEnumerator
        {
            FileInfoList Member;
            int Index;
            public FileInfoListEnumerator(FileInfoList member)
            {
                this.Member = member;
                this.Index = -1;
            }
            public bool MoveNext()
            {
                if(Index < Member.Length)
                    Index++;
                return Index < Member.Length;

            }
            public object Current
            {
                get 
                { 
                    if(Index == -1 || Index == Member.Length)
                        throw new InvalidOperationException();
                    return Member[Index];
                }
            }
            public void Reset()
            {
                Index = -1;
            }
        }

    }
    internal class main
    {
        static bool MultiThreading = false;//是否启用多线程
        #if DEBUG
        static string Mode = "Debug  ";
        #else
        static string Mode = "Release";
        #endif
        static int PictureCount = 0;
        static int RawCount = 0;

        static string[] PictureExtension = { ".jpg",".png",".jpeg",".webp",".gif",".psd"};
        static string[] RawExtension = { ".raw",".cr2",".cr3",".dng",".arw",".ari"};

        static List<FileInfo> PictureList= new();//已扫描的图片
        static List<FileInfo> RawList= new();//已扫描的原始文件
        static List<string> PictureHashList = new();//Picture Hash列表
        static List<string> RawHashList = new();//Raw Hash列表
        static string InputPath;
        static string OutputPath;
        static System.Timers.Timer ScanFileTimer = new();
        static string Version = "1.1.13";
        struct TemporaryData
        {
            public FileInfo FileInfo;
            public byte[] Data;
        }
        static class Console
        {
            public static void WriteLine()
            {
                System.Console.WriteLine();
            }
            public static void WriteLine(string Text)
            {
                if(Text.Contains("[Finished]"))
                {
                    System.Console.ForegroundColor = ConsoleColor.Green;
                    System.Console.WriteLine(Text);
                    System.Console.ResetColor();
                }
                else if(Text.Contains("[ERROR]"))
                {
                    System.Console.ForegroundColor = ConsoleColor.Red;
                    System.Console.WriteLine(Text);
                    System.Console.ResetColor();
                }
                else if(Text.Contains("[Debug]"))
                {
                    System.Console.ForegroundColor = ConsoleColor.Yellow;
                    System.Console.WriteLine(Text);
                    System.Console.ResetColor();
                }
                else
                    System.Console.WriteLine(Text);

            }
            public static TextWriter Out
            {
                get
                {
                    return System.Console.Out;
                }
            }
            public static string? ReadLine()
            {
                return System.Console.ReadLine();
            }
            public static ConsoleKeyInfo ReadKey()
            {
                return System.Console.ReadKey();
            }
        }
        static void Logo()
        {
            Console.WriteLine("##################################################################################################");
            Console.WriteLine($"                 Picture Manager v{Version}");
            Console.WriteLine($"#                Author       : LeZi                                                             #");
            Console.WriteLine($"#                Framework    : .Net 7.0                                                         #");
            Console.WriteLine($"#                Release Date : 2023/07/04                                                       #");
            Console.WriteLine($"#                Github:      : https://github.com/XiaoSong0919/PictureManager                   #");
            Console.WriteLine($"#                Mode         : {Mode}                                                          #");
            Console.WriteLine("##################################################################################################");
        }
        static void CheckUpdate()
        {
            try
            {
                Console.Out.WriteLineAsync("[INFO]正在检查更新...");
                var Client = new HttpClient();
                Client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.2; WOW64; rv:19.0) Gecko/20100101 Firefox/19.0");
                var GetTask = Client.GetStringAsync("https://api.github.com/repos/XiaoSong0919/PictureManager/releases/latest");
                Task.WaitAll(new Task[] { GetTask });
                var Result = GetTask.Result.Split("\n");
                string LatestVersion = null;
                foreach (var Line in Result)
                {
                    if (Line.Contains("\"tag_name\""))
                        LatestVersion = Line.Split(":")[1].Replace("\"", "").Replace("\",", "").Replace("v", "").TrimStart();
                }
                double _LatestVersion = double.Parse(LatestVersion.Split(".", 3)[0]) + double.Parse(LatestVersion.Split(".", 3)[1]) * 0.1 + double.Parse(LatestVersion.Split(".", 3)[2]) * 0.001;
                double _Version = double.Parse(Version.Split(".", 3)[0]) + double.Parse(Version.Split(".", 3)[1]) * 0.1 + double.Parse(Version.Split(".", 3)[2]) * 0.001;
                if (_LatestVersion > _Version)
                {
                    Console.WriteLine($"[INFO]有可用更新，版本为:v{LatestVersion}");
                    Update();
                }
                else
                    Console.WriteLine($"[INFO]已是最新版本");
            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
                Console.WriteLine("[ERROR]检查更新失败");
                Console.WriteLine("--------------------------------------------------------------------------");
            }
            
        }
        static void Update()
        {
            var API_Url = "https://api.github.com/repos/XiaoSong0919/PictureManager/releases/latest";
            var DLClient = new HttpClient();
            DLClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.2; WOW64; rv:19.0) Gecko/20100101 Firefox/19.0");
            var GetTask = DLClient.GetStringAsync(API_Url);
            var Result = GetTask.Result.Split("\n");
            string DLPath = "";
            foreach(var str in Result)
            {
                if(str.Contains("\"browser_download_url\""))
                {
                    DLPath = $"{Environment.CurrentDirectory}/PictureManager.tmp";
                    if (File.Exists(DLPath))
                        File.Delete(DLPath);
                    Console.WriteLine("[UpdateProcess]正在下载...");
                    var DL_Url = str.Replace("\"browser_download_url\":", "").Replace("\"", "").Replace("\"", "").TrimStart() ;
                    var Response = DLClient.GetAsync(DL_Url);
                    var DLFileLength = Response.Result.Content.Headers.ContentLength;//下载文件总体积                  
                    var DLStream = Response.Result.Content.ReadAsStream();
                    var FileStream = File.Create(DLPath);               
                    DLStream.CopyTo(FileStream);
                    FileStream.Close();
                    DLStream.Close();
                    break;
                }
            }
            Console.WriteLine("[Finished]准备开始更新");
            Thread.Sleep(5000);
            Process.Start(DLPath,"Update");
            Environment.Exit(0);
        }
        static void Main(string[] args)
        {
            foreach(var str in args)
            {
                switch(str)
                {
                    case "Update":
                        Thread.Sleep(500);
                        File.Delete($"{Environment.CurrentDirectory}/PictureManager.exe");
                        File.Copy($"{Environment.CurrentDirectory}/PictureManager.tmp", $"{Environment.CurrentDirectory}/PictureManager.exe");
                        Process.Start($"{Environment.CurrentDirectory}/PictureManager.exe","ReStore");
                        Environment.Exit(1);
                        break;
                    case "ReStore":
                        Thread.Sleep(500);
                        File.Delete($"{Environment.CurrentDirectory}/PictureManager.tmp");
                        break;
                }
            }
            Logo();
            CheckUpdate();
            Console.WriteLine("[INFO]是否启用多线程？");
            Console.WriteLine("y(Yes) n(No) Default : Yes");
            switch(Console.ReadLine())
            {
                case "n":
                    MultiThreading = false;
                    break;
                default:
                    MultiThreading = true;
                    break;
            }
            Console.Out.WriteLineAsync($"[INFO]MultiThreading:{MultiThreading}");
            Console.Out.WriteLineAsync("[INFO]请输入目标路径:");
            InputPath = Console.ReadLine();
            while(!Directory.Exists(InputPath))
            {
                Console.WriteLine("[ERROR]目标路径不存在，请重新输入:");
                InputPath = Console.ReadLine();
            }
            Console.Out.WriteLineAsync($"目标路径:{InputPath}");
            Console.Out.WriteLineAsync("[INFO]请输入输出路径:");
            OutputPath = Console.ReadLine();
            while (!Directory.Exists(OutputPath))
            {
                Console.WriteLine("[ERROR]此路径不存在，请重新输入:");
                OutputPath = Console.ReadLine();
            }
            Console.Out.WriteLineAsync($"输出路径:{OutputPath}");
            Console.Out.WriteLineAsync($"[INFO]正在读取Config...");
            ReadConfig();
            Console.Out.WriteLineAsync($"[INFO]开始整理...");
            ScanFileTimer.Interval = 600;
            ScanFileTimer.Elapsed += DiscoverNewFile();            
            ScanFileTimer.Enabled = true;
            Console.Out.WriteLineAsync("[INFO]Standby");
            //while(true)
            //    Console.ReadKey();


        }      

        static void FileHandle()
        {
            Console.Out.WriteLineAsync("[INFO]准备开始整理Picture...");
            Thread.Sleep(3000);
            //int PictureCount = 0;
            //////////////////////////////////////////////////////////////////
            //////                                  Picture处理
            //////////////////////////////////////////////////////////////////
            Dictionary<long, FileInfo> PictureIndex = new();
            PictureList.ForEach(Picture =>
            {
                var CreationTime = File.GetLastWriteTime(Picture.FullName);
                long Year = CreationTime.Year;
                long Month = CreationTime.Month;
                long Day = CreationTime.Day;
                long Hour = CreationTime.Hour;
                long Minute = CreationTime.Minute;
                long Second = CreationTime.Second;
                long Millisecond = CreationTime.Millisecond;
                long TotalSecond = ((Year * 365 + Month * 30 + Day) * 86400 + (Hour * 3600 + Minute * 60) + Second) * 1000 + Millisecond;
                while (PictureIndex.ContainsKey(TotalSecond))
                    TotalSecond++;
                PictureIndex.Add(TotalSecond, Picture);
            });
            var PictureIndexList = PictureIndex.Keys.ToList();
            PictureIndexList.Sort();
            Dictionary<string,TemporaryData> PTemporaryList = new();
            PictureIndexList.ForEach(i =>
            {
                var Picture = PictureIndex[i];
                var CreationTime = File.GetLastWriteTime(Picture.FullName);
                var Year = CreationTime.Year;
                var Month = CreationTime.Month;
                var FilePath = Picture.FullName.Replace("\\", "/");
                var FileNewPath = $"{OutputPath.Replace("\\","/")}/Picture/{Year}/{Month.ToString().PadLeft(2, '0')}/P{PictureCount++}{Picture.Extension}".Replace("//","/");
                if(File.Exists(FileNewPath))//存在同名文件
                {
                    FileInfo ExistsFileInfo = new(FileNewPath);
                    FileStream FileReader = new(FileNewPath, FileMode.Open, FileAccess.Read);
                    byte[] FileData = new byte[ExistsFileInfo.Length];
                    FileReader.Read(FileData, 0, FileData.Length);
                    TemporaryData tmpdata = new();
                    tmpdata.Data = FileData;
                    tmpdata.FileInfo = ExistsFileInfo;                    
                    PTemporaryList.Add(FileNewPath,tmpdata);
                    FileReader.Close();
                    File.Delete(FileNewPath);
                    Console.Out.WriteLineAsync($"[INFO]已缓存冲突[Picture]");

                }
                if(!PTemporaryList.ContainsKey(FilePath))//文件没有被缓存
                {
                    File.Move(FilePath, FileNewPath);
                    PictureList.Remove(Picture);
                    Console.Out.WriteLineAsync($"[INFO]已处理[Picture],新位置在\"{FileNewPath}\"");
                }
                else
                {
                    TemporaryData TemporaryFile = PTemporaryList[FilePath];
                    FileStream FileWriter = File.Create(FileNewPath, TemporaryFile.Data.Length, FileOptions.WriteThrough);//写入到新位置
                    byte[] Data = TemporaryFile.Data;
                    FileWriter.Write(Data,0,Data.Length);
                    FileWriter.Close();
                    PTemporaryList.Remove(FilePath);
                    FileInfo RawInfo = TemporaryFile.FileInfo;
                    FileInfo NewInfo = new(FileNewPath);
                    NewInfo.CreationTime = RawInfo.CreationTime;
                    NewInfo.LastAccessTime = RawInfo.LastAccessTime;
                    NewInfo.LastWriteTime = RawInfo.LastWriteTime;
                    NewInfo.CreationTimeUtc = RawInfo.CreationTimeUtc;
                    NewInfo.LastAccessTimeUtc = RawInfo.LastAccessTimeUtc;
                    NewInfo.LastWriteTimeUtc = RawInfo.LastWriteTimeUtc;
                    Console.Out.WriteLineAsync($"[INFO]已处理[Picture],新位置在\"{FileNewPath}\"");
                    PictureList.Remove(Picture);
                }


            });
            //////////////////////////////////////////////////////////////////
            //////                                  Raw处理
            //////////////////////////////////////////////////////////////////
            //int RawCount = 0;
            Console.Out.WriteLineAsync("[INFO]准备开始整理Raw...");
            Thread.Sleep(3000);
            Dictionary<long, FileInfo> RawIndex = new();
            RawList.ForEach(Raw =>
            {
                var CreationTime = File.GetLastWriteTime(Raw.FullName);
                long Year = CreationTime.Year;
                long Month = CreationTime.Month;
                long Day = CreationTime.Day;
                long Hour = CreationTime.Hour;
                long Minute = CreationTime.Minute;
                long Second = CreationTime.Second;
                long Millisecond = CreationTime.Millisecond;
                long TotalSecond = ((Year * 365 + Month * 30 + Day) * 86400 + (Hour * 3600 + Minute * 60) + Second) * 1000 + Millisecond;
                while (RawIndex.ContainsKey(TotalSecond))
                    TotalSecond++;
                RawIndex.Add(TotalSecond, Raw);
            });
            var RawIndexList = RawIndex.Keys.ToList();
            Dictionary<string, TemporaryData> RTemporaryList = new();
            RawIndexList.Sort();
            RawIndexList.ForEach(i =>
            {
                var Raw = RawIndex[i];
                var CreationTime = File.GetLastWriteTime(Raw.FullName);
                var Year = CreationTime.Year;
                var Month = CreationTime.Month;
                var FilePath = Raw.FullName.Replace("\\", "/");
                var FileNewPath = $"{OutputPath.Replace("\\", "/")}/Raw/{Year}/{Month.ToString().PadLeft(2, '0')}//R{RawCount++}{Raw.Extension}".Replace("//", "/");
                if (File.Exists(FileNewPath))//存在同名文件
                {
                    FileInfo ExistsFileInfo = new(FileNewPath);
                    FileStream FileReader = new(FileNewPath, FileMode.Open, FileAccess.Read);
                    byte[] FileData = new byte[ExistsFileInfo.Length];
                    FileReader.Read(FileData, 0, FileData.Length);
                    TemporaryData tmpdata = new();
                    tmpdata.Data = FileData;
                    tmpdata.FileInfo = ExistsFileInfo;
                    RTemporaryList.Add(FileNewPath, tmpdata);
                    FileReader.Close(); 
                    File.Delete(FileNewPath);
                    Console.Out.WriteLineAsync($"[INFO]已缓存冲突[Raw]");

                }
                if (!RTemporaryList.ContainsKey(FilePath))//文件没有被缓存
                {
                    File.Move(FilePath, FileNewPath);
                    RawList.Remove(Raw);
                    Console.Out.WriteLineAsync($"[INFO]已处理[Raw],新位置在\"{FileNewPath}\"");
                }
                else
                {
                    TemporaryData TemporaryFile = RTemporaryList[FilePath];
                    FileStream FileWriter = File.Create(FileNewPath, TemporaryFile.Data.Length, FileOptions.WriteThrough);//写入到新位置
                    byte[] Data = TemporaryFile.Data;
                    FileWriter.Write(Data, 0, Data.Length);
                    FileWriter.Close();
                    RTemporaryList.Remove(FilePath);
                    FileInfo RawInfo = TemporaryFile.FileInfo;
                    FileInfo NewInfo = new(FileNewPath);
                    NewInfo.CreationTime = RawInfo.CreationTime;
                    NewInfo.LastAccessTime = RawInfo.LastAccessTime;
                    NewInfo.LastWriteTime = RawInfo.LastWriteTime;
                    NewInfo.CreationTimeUtc = RawInfo.CreationTimeUtc;
                    NewInfo.LastAccessTimeUtc = RawInfo.LastAccessTimeUtc;
                    NewInfo.LastWriteTimeUtc = RawInfo.LastWriteTimeUtc;
                    Console.Out.WriteLineAsync($"[INFO]已处理[Raw],新位置在\"{FileNewPath}\"");
                    RawList.Remove(Raw);
                }
            });
            PTemporaryList.Clear();
            RTemporaryList.Clear();
            SetConfig();
        }
        static string GetHash(string FilePath)
        {
            var info = new FileInfo(FilePath);
            StreamReader Reader = new(FilePath);
            var Stream = Reader.BaseStream;
            byte[] Data = new byte[info.Length];
            Stream.Read(Data, 0, Data.Length);
            var FileHash = Convert.ToBase64String(SHA512.HashData(Data));//计算Hash并转换为string
            Stream.Close();
            Reader.Close();
            Data = null;
            return FileHash;
        }
        //需要返回FileInfo的集合
        static FileInfoList ScanDirectory(string Path)//扫描文件夹
        {
            DirectoryInfo dInfo = new(Path);
            FileInfo[] Filelist = dInfo.GetFiles();//获取当前目录的文件列表
            var Directorylist = dInfo.GetDirectories();//获取当前目录的文件夹列表
            FileInfoList FileInfoList = new();

            //List<FileInfo> DiscverFileList = new();
            List<Task<FileInfoList>> subDirTaskList = new();


            //递归扫描文件夹
            if (Directorylist.Length != 0)
            {
                foreach (var dir in Directorylist)
                {
                    if (MultiThreading)
                    {
                        Task<FileInfoList> subDirTask = new(() => { return ScanDirectory(dir.FullName); });
                        subDirTaskList.Add(subDirTask);
                        subDirTask.Start();
#if DEBUG
                        Console.WriteLine($"[Debug]已创建子线程，目标路径:{dir.FullName}");
#endif
                    }
                    else
                        FileInfoList.Merge(ScanDirectory(dir.FullName));
                }
            }

            List<Task> subTaskList = new();
            //递归文件列表，计算Hash
            foreach (FileInfo file in Filelist)
            {
                //排除条件
                if (file.Name == "PictureManager.config" || !(PictureExtension.Contains(file.Extension.ToLower()) || RawExtension.Contains(file.Extension.ToLower())))
                    continue;
                if (file.Length < 1000000)//跳过小于1M的文件
                    continue;

                if(MultiThreading)
                {
                    Task subTask = new(() =>
                    {
#if DEBUG
                        Console.WriteLine($"[Debug]正在计算Hash，目标:{file.FullName}");
#endif
                        var FileHash = GetHash(file.FullName);//计算Hash并转换为string
                        if (!PictureHashList.Contains(FileHash) && PictureExtension.Contains(file.Extension.ToLower()))//已扫描Picture
                        {
                            FileInfoList.Add(file);
                            PictureHashList.Add(FileHash);
                            Console.Out.WriteLineAsync($"[INFO]发现Picture:{file.Name}");
                        }
                        else if (!RawHashList.Contains(FileHash) && RawExtension.Contains(file.Extension.ToLower()))//已扫描Raw
                        {
                            FileInfoList.Add(file);
                            RawHashList.Add(FileHash);
                            Console.Out.WriteLineAsync($"[INFO]发现Raw:{file.Name}");
                        }
#if DEBUG
                        else
                            Console.WriteLine("[Debug]已录入文件，Skipping...");
#endif
                    });
                    subTaskList.Add(subTask);
                    subTask.Start();
                }
                else
                {
#if DEBUG
                    Console.WriteLine($"[Debug]正在计算Hash，目标:{file.FullName}");
#endif
                    var FileHash = GetHash(file.FullName);
                    if (!PictureHashList.Contains(FileHash) && PictureExtension.Contains(file.Extension.ToLower()))//已扫描Picture
                    {
                        FileInfoList.Add(file);
                        PictureHashList.Add(FileHash);
                        Console.Out.WriteLineAsync($"[INFO]发现Picture:{file.Name}");
                    }
                    else if (!RawHashList.Contains(FileHash) && RawExtension.Contains(file.Extension.ToLower()))//已扫描Raw
                    {
                        FileInfoList.Add(file);
                        RawHashList.Add(FileHash);
                        Console.Out.WriteLineAsync($"[INFO]发现Raw:{file.Name}");
                    }

                    else
                        Console.WriteLine("[Debug]已录入文件，Skipping...");
                }
            }

            if (subTaskList.Count != 0)
                Task.WaitAll(subTaskList.ToArray());

            foreach (var subTask in subDirTaskList)
            {
                if (subTask.Result == null)
                    continue;
                var _FileInfoList = subTask.Result;
                FileInfoList.Merge(_FileInfoList);
            }
            return FileInfoList;


        }
        static ElapsedEventHandler DiscoverNewFile()
        {
            var NewFileList = ScanDirectory(InputPath);
            Directory.CreateDirectory($"{OutputPath}/Picture/");
            Directory.CreateDirectory($"{OutputPath}/Raw/");
            int PictureCount = 0;
            int RawCount = 0;
            foreach (FileInfo file in NewFileList)
            {
                //检查文件夹
                var CreationTime = File.GetLastWriteTime(file.FullName);
                var Year = CreationTime.Year;
                var Month = CreationTime.Month;
                Directory.CreateDirectory($"{OutputPath}/Picture/{Year}");
                Directory.CreateDirectory($"{OutputPath}/Picture/{Year}/{Month.ToString().PadLeft(2, '0')}");
                Directory.CreateDirectory($"{OutputPath}/Raw/{Year}");
                Directory.CreateDirectory($"{OutputPath}/Raw/{Year}/{Month.ToString().PadLeft(2, '0')}");
                
                //筛选
                if (PictureExtension.Contains(file.Extension.ToLower()))
                {
                    PictureCount++;
                    PictureList.Add(file);
                }
                else if (RawExtension.Contains(file.Extension.ToLower()))
                {
                    RawCount++;
                    RawList.Add(file);
                }
            }
            Console.WriteLine("[Finished]扫描完毕");
            Console.Out.WriteLineAsync($"[INFO]共发现[Picture]:{PictureCount}");
            Console.Out.WriteLineAsync($"[INFO]共发现[Raw]:{RawCount}");
            
            FileHandle();
            return null;
        }
        static void SetConfig()
        {
            Console.Out.WriteLineAsync("[INFO]正在写入Config...");
            string ConfigPath = $"{OutputPath}/PictureManager.config";
            string ConfigContent = null;
            ConfigContent += "[PictureHashList]\n";
            PictureHashList.ForEach(Hash =>
            {
                ConfigContent += $"{Hash}\n";
            });
            ConfigContent += "[PictureHashListEnd]\n";
            ConfigContent += "[RawHashList]\n";
            RawHashList.ForEach(Hash =>
            {
                ConfigContent += $"{Hash}\n";
            });
            ConfigContent += "[RawHashListEnd]\n";
            ConfigContent += "[FileCount]\n";
            ConfigContent += $"PictureCount={PictureCount}\n";
            ConfigContent += $"RawCount={RawCount}\n";
            ConfigContent += "[FileCountEnd]";
            File.WriteAllText(ConfigPath, ConfigContent);
        }
        static void ReadConfig()
        {
            string ConfigPath = $"{OutputPath}/PictureManager.config";
            if (File.Exists(ConfigPath))
            {
#if DEBUG
                Console.WriteLine($"[Debug]Config有效，正在读取...");
#endif
                var Contents = File.ReadAllLines(ConfigPath);
                string NowArea = null;
                foreach (var Line in Contents)
                {
                    if (Line == "[PictureHashList]")
                    {
                        NowArea = "PictureHashList";
                        continue;
                    }
                    else if (Line == "[RawHashList]")
                    {
                        NowArea = "RawHashList";
                        continue;
                    }
                    else if (Line == "[FileCount]")
                    {
                        NowArea = "FileCount";
                        continue;
                    }
                    else if (Line.Contains("[PictureHashListEnd]")  || Line.Contains("[RawHashListEnd]"))
                    {
                        NowArea = null;
                        continue;
                    }
                    if (NowArea == "PictureHashList")
                        PictureHashList.Add(Line);
                    else if (NowArea == "RawHashList")
                        RawHashList.Add(Line);
                    else if (NowArea == "FileCount")
                    {
                        if (Line.Contains("Picture"))
                            PictureCount = int.Parse(Line.Replace("PictureCount=", ""));
                        else if (Line.Contains("Raw"))
                            RawCount = int.Parse(Line.Replace("RawCount=", ""));
                    }

                }
                Console.Out.WriteLineAsync($"[INFO]Config读取完毕");
                Thread.Sleep(1000);

            }
            else
            {
                Console.WriteLine($"[Debug]Config不存在");
                Thread.Sleep(1000);
            }


        }
    }
}
