using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace Picture
{
    internal class Start
    {
        static int FileCount = 0;
        static int PictureCount = 0;
        static int RawCount = 0;
        static string[] PictureExtension = { ".jpg",".png",".jpeg",".webp",".gif",".psd"};
        static string[] RawExtension = { ".raw",".cr2",".cr3",".dng"};
        static List<FileInfo> PictureList= new();//已扫描的图片
        static List<FileInfo> RawList= new();//已扫描的原始文件
        static List<string> PictureHashList = new();//Picture Hash列表
        static List<string> RawHashList = new();//Raw Hash列表
        static string InputPath;
        static string OutputPath;
        static System.Timers.Timer ScanFileTimer = new();
        static void Main(string[] args)
        {            
            Console.Out.WriteLine("[INFO]请输入目标路径:");
            InputPath = Console.ReadLine();
            while(!Directory.Exists(InputPath))
            {
                Console.Out.WriteLine("[ERROR]目标路径不存在，请重新输入:");
                InputPath = Console.ReadLine();
            }
            Console.Out.WriteLine($"目标路径:{InputPath}");
            Console.Out.WriteLine("[INFO]请输入输出路径:");
            OutputPath = Console.ReadLine();
            while (!Directory.Exists(OutputPath))
            {
                Console.Out.WriteLine("[ERROR]此路径不存在，请重新输入:");
                OutputPath = Console.ReadLine();
            }
            Console.Out.WriteLine($"输出路径:{OutputPath}");
            Console.Out.WriteLine($"[Debug]正在读取Config...");
            ReadConfig();
            Console.Out.WriteLine($"[Debug]开始执行...");
            ScanFileTimer.Interval = 600;
            ScanFileTimer.Elapsed += DiscoverNewFile();            
            ScanFileTimer.Enabled = true;
            Console.Out.WriteLine("[INFO]Standby");
            //while(true)
            //    Console.ReadKey();


        }
        

        static void FileHandle()
        {
            Console.Out.WriteLine("[INFO]准备开始整理Picture...");
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
            PictureIndexList.ForEach(i =>
            {
                var Picture = PictureIndex[i];
                var CreationTime = File.GetLastWriteTime(Picture.FullName);
                var Year = CreationTime.Year;
                var Month = CreationTime.Month;
                File.Move(Picture.FullName, $"{OutputPath}/Picture/{Year}/{Month.ToString().PadLeft(2, '0')}/P{PictureCount++}{Picture.Extension}");
                PictureList.Remove(Picture);
                Console.Out.WriteLine($"[INFO]已处理[Picture],新位置在\"{OutputPath}/Picture/{Year}/{Month.ToString().PadLeft(2, '0')}/P{PictureCount}{Picture.Extension}\"");
            });
            //////////////////////////////////////////////////////////////////
            //////                                  Raw处理
            //////////////////////////////////////////////////////////////////
            //int RawCount = 0;
            Console.Out.WriteLine("[INFO]准备开始整理Raw...");
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
            RawIndexList.Sort();
            RawIndexList.ForEach(i =>
            {
                var Raw = RawIndex[i];
                var CreationTime = File.GetLastWriteTime(Raw.FullName);
                var Year = CreationTime.Year;
                var Month = CreationTime.Month;
                File.Move(Raw.FullName, $"{OutputPath}/Raw/{Year}/{Month.ToString().PadLeft(2, '0')}/R{RawCount++}{Raw.Extension}");
                PictureList.Remove(Raw);
                Console.Out.WriteLine($"[INFO]已处理[Raw],新位置在\"{OutputPath}/Raw/{Year}/{Month.ToString().PadLeft(2, '0')}/R{RawCount}{Raw.Extension}\"");
            });
            SetConfig();
        }
        static List<FileInfo> ScanDirectory(string Path)//扫描文件夹
        {
            DirectoryInfo dInfo = new(Path);
            FileInfo[] Filelist = dInfo.GetFiles();
            var Directorylist = dInfo.GetDirectories();
            List<FileInfo> DiscverFileList = new();
            List<Task<List<FileInfo>>> subTaskList = new();
            if (Directorylist.Length != 0)
            {
                foreach (var dir in Directorylist)
                {
                    Task<List<FileInfo>> subTask = new(() => { return ScanDirectory(dir.FullName); });
                    subTaskList.Add(subTask);
                    subTask.Start();
                    Console.Out.WriteLine($"[Debug]已创建子线程，目标路径:{dir.FullName}");
                }
            }
            FileCount += Filelist.Length;

            foreach( FileInfo file in Filelist )
            {
                if (file.Name == "PictureManager.config" || !(PictureExtension.Contains(file.Extension.ToLower()) || RawExtension.Contains(file.Extension.ToLower())))
                    continue;
                Task<List<FileInfo>> subTask = new(() => 
                {                    
                    Console.Out.WriteLine($"[Debug]正在计算Hash，目标:{file.FullName}");
                    StreamReader Reader = new(file.FullName);
                    var Stream = Reader.BaseStream;
                    byte[] FileBytes = new byte[Stream.Length];
                    Stream.Read(FileBytes,0,FileBytes.Length);
                    Stream.Close();
                    Reader.Close();
                    var FileMD5 = Convert.ToBase64String(MD5.HashData(FileBytes));
                    if (!PictureHashList.Contains(FileMD5) && PictureExtension.Contains(file.Extension.ToLower()))
                    {
                        DiscverFileList.Add(file);
                        PictureHashList.Add(FileMD5);
                        Console.Out.WriteLine($"[INFO]发现Picture:{file.Name}");
                    }
                    else if (!RawHashList.Contains(FileMD5) && RawExtension.Contains(file.Extension.ToLower()))
                    {
                        DiscverFileList.Add(file);
                        RawHashList.Add(FileMD5);
                        Console.Out.WriteLine($"[INFO]发现Raw:{file.Name}");
                    }
                    else
                        Console.Out.WriteLine("[Debug]已录入文件，Skipping...");
                    return null;
                });
                subTaskList.Add(subTask);
                subTask.Start();
            }
            if(subTaskList.Count != 0)
            {
                var task = Task.WhenAny(subTaskList);
                task.Wait();
            }
            foreach(var subTask in subTaskList)
            {
                if (subTask.Result == null)
                    continue;
                var FileList = subTask.Result;
                foreach(FileInfo file in FileList )
                    DiscverFileList.Add(file);
                
            }
            return DiscverFileList;


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
            Console.Out.WriteLine("[Finished]扫描完毕");
            Console.Out.WriteLine($"[INFO]共发现[Picture]:{PictureCount}");
            Console.Out.WriteLine($"[INFO]共发现[Raw]:{RawCount}");
            FileHandle();
            return null;
        }
        static void SetConfig()
        {
            Console.Out.WriteLine("[INFO]正在写入Config...");
            string ConfigPath = $"{OutputPath}/PictureManager.config";
            File.WriteAllText(ConfigPath,"[PictureHashList]\n");
            PictureHashList.ForEach(Hash =>
            {
                File.AppendAllText(ConfigPath,$"{Hash}\n");
            });
            File.AppendAllText(ConfigPath, "[PictureHashListEnd]\n");
            File.AppendAllText(ConfigPath, "[RawHashList]\n");
            RawHashList.ForEach(Hash =>
            {
                File.AppendAllText(ConfigPath, $"{Hash}\n");
            });
            File.AppendAllText(ConfigPath, "[RawHashListEnd]\n");
            File.AppendAllText(ConfigPath, "[FileCount]\n");
            File.AppendAllText(ConfigPath, $"PictureCount={PictureCount}\n");
            File.AppendAllText(ConfigPath, $"RawCount={RawCount}\n");
            File.AppendAllText(ConfigPath, "[FileCountEnd]");
        }
        static void ReadConfig()
        {
            string ConfigPath = $"{OutputPath}/PictureManager.config";
            if (File.Exists(ConfigPath))
            {
                Console.Out.WriteLine($"[Debug]Config有效，正在读取...");
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
                    else if (Line.Contains("End"))
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
                Console.Out.WriteLine($"[Debug]Config读取完毕");
                Thread.Sleep(500);
            }
            else
                Console.Out.WriteLine($"[Debug]Config不存在");
            Thread.Sleep(1000);
        }
    }
}
