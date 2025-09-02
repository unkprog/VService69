using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using static System.Net.WebRequestMethods;

namespace ASUweb.PublishUtil
{
    internal class Program
    {
        internal static string dv = DateTime.Now.ToString("yyyyMMddHHmmss");



        static void Main(string[] args)
        {
            string path = (args == null || args.Length < 1 ? string.Empty : args[0]);

            if (string.IsNullOrEmpty(path))
                return;

            //string jsonFile = Path.Combine(path, "appsettings.json");
            //if (System.IO.File.Exists(jsonFile))
            //{
            //    string json = System.IO.File.ReadAllText(jsonFile);
            //    JsonObject? data = JsonNode.Parse(json) as JsonObject;
            //    data["AppSettings"]["Version"] = dv;
            //    json = data?.ToString();
            //    System.IO.File.WriteAllText(jsonFile, json);
            //}

            if (Directory.Exists(path))
            {
                string newFileNameError = string.Concat(path, @"\\logfile", dv, ".log");
                using (writerError = new StreamWriter(newFileNameError))
                {
                    ProcessDirectory(path);
                }
            }
        }

        internal static StreamWriter? writerError;

        internal static void WriteError(string message, string trace = "")
        {
            if (writerError == null)
                return;
            try
            {
                writerError?.WriteLine(DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));
                writerError?.WriteLine(message);
                if (!string.IsNullOrEmpty(trace))
                {
                    writerError?.WriteLine("Stack trace:");
                    writerError?.WriteLine(trace);
                }
            }
            catch
            {
            }
        }

        // Process all files in the directory passed in, recurse on any directories
        // that are found, and process the files they contain.
        public static void ProcessDirectory(string targetDirectory)
        {
            //// Process the list of files found in the directory.
            //if (!targetDirectory.EndsWith("\\Scripts")
            //   && !targetDirectory.EndsWith("\\Scripts\\bootstrap5")
            //   && !targetDirectory.EndsWith("\\Scripts\\camera")
            //   && !targetDirectory.EndsWith("\\Scripts\\Data")
            //   && !targetDirectory.Contains("\\Scripts\\lib")
            //   && !targetDirectory.Contains("\\js\\lib\\devextreme")
            //   && !targetDirectory.Contains(@"\App\CityIdeas")
            //   && !targetDirectory.Contains(@"\App\MediaView"))
            {
                string[] fileEntries = Directory.GetFiles(targetDirectory);
                foreach (string fileName in fileEntries)
                    ProcessFile(fileName);
            }

            // Recurse into subdirectories of this directory.
            string[] subdirectoryEntries = Directory.GetDirectories(targetDirectory);
            foreach (string subdirectory in subdirectoryEntries)
                ProcessDirectory(subdirectory);
        }

        // Insert logic for processing found files here.
        public static void ProcessFile(string path)
        {
            FileInfo fileInfo = new FileInfo(path);
            string ext = fileInfo.Extension.ToLower();
            if (ext == ".aspx" || ext == ".html" || ext == ".master" || ext == ".js" || ext == ".cshtml")
                UpdateVersionLinks(fileInfo);
        }


        public static void UpdateVersionLinks(FileInfo fileInfo)
        {
            Console.WriteLine("Processed file '{0}'.", fileInfo.FullName);
            string oldFileName = fileInfo.FullName
                 , newFileName = fileInfo.FullName.Replace(fileInfo.Extension, "") + "_Repl" + fileInfo.Extension;
            using (var input = System.IO.File.OpenText(oldFileName))
            {
                using (var output = new StreamWriter(newFileName, true, new UTF8Encoding(true)))
                {
                    string line;
                    while (null != (line = input.ReadLine()))
                    {
                        // optionally modify line.
                        output.WriteLine(ProcessLine(fileInfo, line));
                    }
                }
            }
            System.IO.File.Delete(oldFileName);
            System.IO.File.Move(newFileName, oldFileName);
        }

        public static string ProcessLine(FileInfo fileInfo, string line)
        {
            string locLine = line.ToLower();
            if (locLine.IndexOf("<link ") > -1)
                return ProcessLineTag(fileInfo, line, "href", ".css");
            else if (locLine.IndexOf("<script ") > -1)
                return ProcessLineTag(fileInfo, line, "src", ".js");
            else if (locLine.LastIndexOf(".js") > -1 && locLine.LastIndexOf(".json") == -1)
                return ProcessLineJsLink(fileInfo, line);
            else if (locLine.Replace(" ", "").LastIndexOf("pageversion:") > -1)
                return ProcessLinePageVersion(fileInfo, line);
            return line;
        }

        internal static int FindQuot(string line, int start = 0)
        {
            int result = line.ToLower().IndexOf("\"", start);
            if (result == -1)
                result = line.ToLower().IndexOf("\'", start);
            return result;
        }

        internal static int FindQuotLast(string line, int start = 0)
        {
            int result = line.ToLower().LastIndexOf("\"", start);
            if (result == -1)
                result = line.ToLower().LastIndexOf("\'", start);
            return result;
        }

        public static string ProcessLinePageVersion(FileInfo fileInfo, string line)
        {
            string result = line;
            int locPV = result.ToLower().LastIndexOf("pageversion");
            if (locPV > -1)
            {
                int locBeg = FindQuot(result, locPV); //result.ToLower().IndexOf("\"", locPV);
                if (locBeg > -1)
                {
                    int locEnd = FindQuot(result, locBeg + 1); //result.ToLower().IndexOf("\"", locBeg);
                    if (locEnd > -1)
                    {
                        result = result.Substring(0, locBeg + 1)
                                      + "?v=" + dv
                                      + result.Substring(locEnd);
                    }
                }
            }
            return result;
        }
        public static string ProcessLineJsLink(FileInfo fileInfo, string line, string ext = ".js")
        {
            string result = line;
            int locLen, locExt, locEnd, locBeg;
            string extLink;

            try
            {
                locLen = result.Length;
                locExt = result.ToLower().LastIndexOf(ext, locLen);
                while (locExt > -1)
                {
                    locEnd = FindQuot(result, locExt); // result.ToLower().IndexOf("\"", locExt);
                    if (locEnd > -1)
                    {
                        locBeg = FindQuotLast(result, locExt); // result.ToLower().LastIndexOf("\"", locExt);
                        if (locBeg > -1)
                        {
                            extLink = result.Substring(locBeg + 1, locEnd - locBeg - 1);
                            result = result.Substring(0, locBeg + 1)
                                       + extLink.Substring(0, extLink.LastIndexOf(ext)) + ext + "?v=" + dv
                                       + result.Substring(locEnd);
                        }
                        locLen = locBeg - 1;
                    }
                    else
                        locLen = locExt - 1;
                    if (locLen > -1)
                        locExt = result.ToLower().LastIndexOf(ext, locLen);
                    else
                        locExt = -1;
                }

            }
            catch (Exception ex)
            {
                WriteError(string.Concat(fileInfo.FullName, Environment.NewLine, ex.Message), ex.StackTrace);
            }
            return result;
        }
        public static string ProcessLineTag(FileInfo fileInfo, string line, string prop, string ext)
        {
            string result = line;

            try
            {
                string locProp = prop + "=\"";
                int lenProp = locProp.Length;
                int locPos = result.ToLower().IndexOf(locProp);
                if (locPos > -1)
                {
                    int locPosEnd = FindQuot(result, locPos + lenProp); //result.ToLower().IndexOf("\"", locPos + lenProp);
                    string extLink = result.Substring(locPos + lenProp, locPosEnd - locPos - lenProp);
                    int locExt = extLink.ToLower().LastIndexOf(ext);
                    if (locExt > -1)
                    {
                        result = result.Substring(0, locPos + lenProp)
                               + extLink.Substring(0, locExt) + ext + "?v=" + dv
                               + result.Substring(locPosEnd);
                    }
                }
            }
            catch (Exception ex)
            {
                WriteError(string.Concat(fileInfo.FullName, Environment.NewLine, ex.Message), ex.StackTrace);
            }
            return result;
        }
    }
}
