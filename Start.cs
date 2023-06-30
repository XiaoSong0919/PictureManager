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
        struct TemporaryData
        {
            public FileInfo FileInfo;
            public byte[] Data;
        }
        static void Main(string[] args)
        {            
            Console.Out.WriteLineAsync("[INFO]请输入目标路径:");
            InputPath = Console.ReadLine();
            while(!Directory.Exists(InputPath))
            {
                Console.Out.WriteLineAsync("[ERROR]目标路径不存在，请重新输入:");
                InputPath = Console.ReadLine();
            }
            Console.Out.WriteLineAsync($"目标路径:{InputPath}");
            Console.Out.WriteLineAsync("[INFO]请输入输出路径:");
            OutputPath = Console.ReadLine();
            while (!Directory.Exists(OutputPath))
            {
                Console.Out.WriteLineAsync("[ERROR]此路径不存在，请重新输入:");
                OutputPath = Console.ReadLine();
            }
            Console.Out.WriteLineAsync($"输出路径:{OutputPath}");
            Console.Out.WriteLineAsync($"[Debug]正在读取Config...");
            ReadConfig();
            Console.Out.WriteLineAsync($"[Debug]开始执行...");
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
                var FilePath = Picture.FullName;
                var FileNewPath = $"{OutputPath}/Picture/{Year}/{Month.ToString().PadLeft(2, '0')}/P{PictureCount++}{Picture.Extension}";
                if(File.Exists(FileNewPath))//存在同名文件
                {
                    FileInfo ExistsFileInfo = new(FileNewPath);
                    FileStream FileReader = new(FileNewPath, FileMode.Open, FileAccess.Read);
                    byte[] FileData = new byte[ExistsFileInfo.Length];
                    FileReader.Read(FileData, 0, FileData.Length);
                    TemporaryData tmpdata = new();
                    tmpdata.Data = FileData;
                    tmpdata.FileInfo = ExistsFileInfo;
                    File.Delete(FileNewPath);
                    PTemporaryList.Add(FileNewPath,tmpdata);
                    FileReader.Close();
                    Console.Out.WriteLineAsync($"[INFO]已缓存[Picture]");

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
                    FileStream FileWriter = new(FileNewPath, FileMode.Open, FileAccess.Read);//写入到新位置
                    byte[] Data = TemporaryFile.Data;
                    FileWriter.Write(Data,0,Data.Length);
                    FileWriter.Close();
                    PTemporaryList.Remove(FilePath);
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
                var FilePath = Raw.FullName;
                var FileNewPath = $"{OutputPath}/Raw/{Year}/{Month.ToString().PadLeft(2, '0')}//R{RawCount++}{Raw.Extension}";
                if (File.Exists(FileNewPath))//存在同名文件
                {
                    FileInfo ExistsFileInfo = new(FileNewPath);
                    FileStream FileReader = new(FileNewPath, FileMode.Open, FileAccess.Read);
                    byte[] FileData = new byte[ExistsFileInfo.Length];
                    FileReader.Read(FileData, 0, FileData.Length);
                    TemporaryData tmpdata = new();
                    tmpdata.Data = FileData;
                    tmpdata.FileInfo = ExistsFileInfo;
                    File.Delete(FileNewPath);
                    RTemporaryList.Add(FileNewPath, tmpdata);
                    FileReader.Close();
                    Console.Out.WriteLineAsync($"[INFO]已缓存[Raw]");

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
                    FileStream FileWriter = new(FileNewPath, FileMode.Open, FileAccess.Read);//写入到新位置
                    byte[] Data = TemporaryFile.Data;
                    FileWriter.Write(Data, 0, Data.Length);
                    FileWriter.Close();
                    RTemporaryList.Remove(FilePath);
                    Console.Out.WriteLineAsync($"[INFO]已处理[Raw],新位置在\"{FileNewPath}\"");
                    RawList.Remove(Raw);
                }
            });
            PTemporaryList.Clear();
            RTemporaryList.Clear();
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
                    Console.Out.WriteLineAsync($"[Debug]已创建子线程，目标路径:{dir.FullName}");
                }
            }
            FileCount += Filelist.Length;

            foreach( FileInfo file in Filelist )
            {
                if (file.Name == "PictureManager.config" || !(PictureExtension.Contains(file.Extension.ToLower()) || RawExtension.Contains(file.Extension.ToLower())))
                    continue;
                Task<List<FileInfo>> subTask = new(() => 
                {   
                    
                    Console.Out.WriteLineAsync($"[Debug]正在计算Hash，目标:{file.FullName}");
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
                        Console.Out.WriteLineAsync($"[INFO]发现Picture:{file.Name}");
                    }
                    else if (!RawHashList.Contains(FileMD5) && RawExtension.Contains(file.Extension.ToLower()))
                    {
                        DiscverFileList.Add(file);
                        RawHashList.Add(FileMD5);
                        Console.Out.WriteLineAsync($"[INFO]发现Raw:{file.Name}");
                    }
                    else
                        Console.Out.WriteLineAsync("[Debug]已录入文件，Skipping...");
                    return null;
                });
                subTaskList.Add(subTask);
                subTask.Start();
            }
            if(subTaskList.Count != 0)
            {
                Task.WaitAll(subTaskList.ToArray());
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
            Console.Out.WriteLineAsync("[Finished]扫描完毕");
            Console.Out.WriteLineAsync($"[INFO]共发现[Picture]:{PictureCount}");
            Console.Out.WriteLineAsync($"[INFO]共发现[Raw]:{RawCount}");
            FileHandle();
            return null;
        }
        static void SetConfig()
        {
            Console.Out.WriteLineAsync("[INFO]正在写入Config...");
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
                Console.Out.WriteLineAsync($"[Debug]Config有效，正在读取...");
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
                Console.Out.WriteLineAsync($"[Debug]Config读取完毕");
                Thread.Sleep(500);
            }
            else
                Console.Out.WriteLineAsync($"[Debug]Config不存在");
            Thread.Sleep(1000);
        }
    }
}
