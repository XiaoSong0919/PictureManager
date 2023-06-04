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
        static List<string> HashList = new();//Hash列表
        static string InputPath;
        static string OutputPath;
        static System.Timers.Timer ScanFileTimer = new();
        static void Main(string[] args)
        {            
            Console.WriteLine("[INFO]请输入目标路径:");
            InputPath = Console.ReadLine();
            while(!Directory.Exists(InputPath))
            {
                Console.WriteLine("[ERROR]目标路径不存在，请重新输入:");
                InputPath = Console.ReadLine();
            }
            Console.WriteLine($"目标路径:{InputPath}");
            Console.WriteLine("[INFO]请输入输出路径:");
            OutputPath = Console.ReadLine();
            while (!Directory.Exists(OutputPath))
            {
                Console.WriteLine("[ERROR]此路径不存在，请重新输入:");
                OutputPath = Console.ReadLine();
            }
            Console.WriteLine($"输出路径:{OutputPath}");
            Console.WriteLine($"[Debug]正在读取Config...");
            ReadConfig();
            Console.WriteLine($"[Debug]开始执行...");
            ScanFileTimer.Interval = 600;
            ScanFileTimer.Elapsed += DiscoverNewFile();            
            ScanFileTimer.Enabled = true;
            while(true)
                Console.ReadKey();
            

        }
        

        static void FileHandle()
        {
            Task PictureTask = new(() =>
            {
                int PictureCount = 0;
                Dictionary<long, FileInfo> PictureIndex = new() ;
                PictureList.ForEach( Picture =>
                {
                    var CreationTime = File.GetCreationTime(Picture.FullName);
                    long Year = CreationTime.Year;
                    long Month = CreationTime.Month;
                    long Day = CreationTime.Day;
                    long Hour = CreationTime.Hour;
                    long Minute = CreationTime.Minute;
                    long Second = CreationTime.Second;
                    long Millisecond = CreationTime.Millisecond;
                    long TotalSecond = ((Year * 365 + Month * 30 + Day) * 86400 + (Hour * 3600 + Minute * 60) + Second) * 1000 + Millisecond;
                    PictureIndex.Add(TotalSecond, Picture);
                });
                var IndexList = PictureIndex.Keys.ToList();
                IndexList.Sort();
                IndexList.ForEach(i =>
                {
                    var Picture = PictureIndex[i];
                    var CreationTime = File.GetCreationTime(Picture.FullName);
                    var Year = CreationTime.Year;
                    var Month = CreationTime.Month;
                    File.Move(Picture.FullName, $"{OutputPath}/Picture/{Year}/{Month.ToString().PadLeft(2, '0')}/{PictureCount++}{Picture.Extension}");
                    PictureList.Remove(Picture);
                    Console.WriteLine($"[INFO]已处理[Picture],新位置在\"{OutputPath}/Picture/{Year}/{Month.ToString().PadLeft(2, '0')}/{PictureCount}{Picture.Extension}\"");
                });
            });
            PictureTask.Start();
            Task RawTask = new(() =>
            {
                int RawCount = 0;
                Dictionary<long, FileInfo> RawIndex = new();
                RawList.ForEach(Raw =>
                {
                    var CreationTime = File.GetCreationTime(Raw.FullName);
                    long Year = CreationTime.Year;
                    long Month = CreationTime.Month;
                    long Day = CreationTime.Day;
                    long Hour = CreationTime.Hour;
                    long Minute = CreationTime.Minute;
                    long Second = CreationTime.Second;
                    long Millisecond = CreationTime.Millisecond;
                    long TotalSecond = ((Year * 365 + Month * 30 + Day) * 86400 + (Hour * 3600 + Minute * 60) + Second)* 1000 + Millisecond;
                    RawIndex.Add(TotalSecond, Raw);
                });
                var IndexList = RawIndex.Keys.ToList();
                IndexList.Sort();
                IndexList.ForEach(i =>
                {
                    var Raw = RawIndex[i];
                    var CreationTime = File.GetCreationTime(Raw.FullName);
                    var Year = CreationTime.Year;
                    var Month = CreationTime.Month;
                    File.Move(Raw.FullName, $"{OutputPath}/Raw/{Year}/{Month.ToString().PadLeft(2, '0')}/{RawCount++}{Raw.Extension}");
                    PictureList.Remove(Raw);
                    Console.WriteLine($"[INFO]已处理[Raw],新位置在\"{OutputPath}/Raw/{Year}/{Month.ToString().PadLeft(2, '0')}/{RawCount}{Raw.Extension}\"");
                });
            });
            RawTask.Start();
            Task subTask = Task.WhenAny(new Task[2] { PictureTask,RawTask});
            subTask.Wait();
            SetConfig();
            Console.WriteLine("[INFO]Standby");
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
                    Console.WriteLine($"[Debug]已创建子线程，目标路径:{dir.FullName}");
                }
            }
            FileCount += Filelist.Length;

            foreach( FileInfo file in Filelist )
            {
                if (file.Name == "PictureManager.config")
                    continue;
                Task<List<FileInfo>> subTask = new(() => 
                {                    
                    Console.WriteLine($"[Debug]正在计算Hash，目标:{file.FullName}");
                    var FileMD5 = Convert.ToBase64String(MD5.HashData(File.ReadAllBytes(file.FullName)));
                    if (HashList.Contains(FileMD5))
                        Console.WriteLine("[Debug]已录入文件，Skipping...");
                    else if (PictureExtension.Contains(file.Extension.ToLower()) || RawExtension.Contains(file.Extension.ToLower()))
                    {
                        DiscverFileList.Add(file);
                        HashList.Add(FileMD5);
                        Console.WriteLine($"[INFO]发现文件:{file.Name}");
                    }
                    return null;
                });
                subTaskList.Add(subTask);
                subTask.Start();
            }
            var task = Task.WhenAny(subTaskList);
            task.Wait();
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
            foreach (FileInfo file in NewFileList)
            {
                //检查文件夹
                var CreationTime = File.GetCreationTime(file.FullName);
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
            Console.WriteLine($"[INFO]共发现[Picture]:{PictureCount}");
            Console.WriteLine($"[INFO]共发现[Raw]:{RawCount}");
            FileHandle();
            return null;
        }
        static void SetConfig()
        {
            string ConfigPath = $"{OutputPath}/PictureManager.config";
            File.WriteAllText(ConfigPath,"[FileHashList]\n");
            HashList.ForEach(Hash =>
            {
                File.AppendAllText(ConfigPath,$"{Hash}\n");
            });
            File.AppendAllText(ConfigPath, "[FileHashListEnd]\n");
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
                Console.WriteLine($"[Debug]Config有效，正在读取...");
                Thread.Sleep(500);
                var Contents = File.ReadAllLines(ConfigPath);
                string NowArea = null;
                foreach (var Line in Contents)
                {
                    if (Line == "[FileHashList]")
                    {
                        NowArea = "FileHashList";
                        continue;
                    }
                    else if (Line == "[FileHashListEnd]")
                    {
                        NowArea = null;
                        continue;
                    }
                    else if (Line == "[FileCount]")
                    {
                        NowArea = "FileCount";
                        continue;
                    }
                    else if (Line == "[FileCountEnd]")
                    {
                        NowArea = null;
                        continue;
                    }
                    if (NowArea == "FileHashList")
                        HashList.Add(Line);
                    else if (NowArea == "FileCount")
                    {
                        if (Line.Contains("Picture"))
                            PictureCount = int.Parse(Line.Replace("PictureCount=", ""));
                        else if (Line.Contains("Raw"))
                            RawCount = int.Parse(Line.Replace("RawCount=", ""));
                    }

                }
                Console.WriteLine($"[Debug]Config读取完毕");
                Thread.Sleep(500);
            }
            else
                File.Create(ConfigPath);
        }
    }
}
