﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace ServerLogParsers
{
    public static class PerkLog
    {
        public class UserPerkData
        {
            public string Username;
            public ulong  SteamId;
            public string LogDate;
            public Dictionary<string, object> Perks = new Dictionary<string, object>();
        }

        public static Regex regex = new Regex(@"\[(.*?)]\ \[(\d+)\]\[(.*?)\]\[.*?\]\[(.*?)\]");

        public static string GetContent(int nthFile=0)
        {
            string zomboidLogDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)+"\\Zomboid\\Logs\\";

            if(!Directory.Exists(zomboidLogDirectory)) return string.Empty;

            List<FileInfo> perkLogFiles    = new List<FileInfo>();
            List<FileInfo> sortedDirectory = new DirectoryInfo(zomboidLogDirectory)
                                             .GetFiles()
                                             .OrderBy(file => file.LastWriteTime)
                                             .ToList();

            foreach(FileInfo _fileInfo in sortedDirectory)
            {
                if(_fileInfo.Name.Contains("PerkLog"))
                    perkLogFiles.Add(_fileInfo);
            }

            if(perkLogFiles.Count == 0
            || nthFile > perkLogFiles.Count - 1) return string.Empty;

            FileInfo     fileInfo     = perkLogFiles[nthFile];
            FileStream   fileStream   = fileInfo.OpenRead();
            StreamReader streamReader = new StreamReader(fileStream);

            string fileContent = streamReader.ReadToEnd();

            streamReader.Close();
            return fileContent;
        }

        public static Dictionary<string, UserPerkData> Parse(int nthFile=0)
        {
            string logContent = GetContent(nthFile);

            if(logContent == string.Empty) return null;

            
            Dictionary<string, UserPerkData> userPerkDataList = new Dictionary<string, UserPerkData>();
            string[] logLines = logContent.Split('\n').Reverse().ToArray();

            foreach(string line in logLines)
            {
                var regexMatch = regex.Match(line);

                if(!regexMatch.Success
                || !regexMatch.Groups[4].Value.Contains("Cooking=")) continue;

                if(userPerkDataList.ContainsKey(regexMatch.Groups[3].Value))
                    continue;

                UserPerkData userPerkData = new UserPerkData();
                userPerkData.LogDate      = regexMatch.Groups[1].Value;
                userPerkData.SteamId      = Convert.ToUInt64(regexMatch.Groups[2].Value);
                userPerkData.Username     = regexMatch.Groups[3].Value;

                string[] perks = regexMatch.Groups[4].Value.Split(',').Select(elem => elem.Trim()).ToArray();

                foreach(string perk in perks)
                {
                    string[] perkPair = perk.Split('=');
                    userPerkData.Perks.Add(perkPair[0], Convert.ToInt32(perkPair[1]));
                }

                userPerkDataList.Add(userPerkData.Username, userPerkData);
            }

            return userPerkDataList;
        }
    }
}