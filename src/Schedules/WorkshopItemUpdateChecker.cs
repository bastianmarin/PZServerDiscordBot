﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static partial class Schedules
{
    public static void WorkshopItemUpdateChecker(List<object> args)
    {
        if(!ServerUtility.IsServerRunning())
        {
            Logger.WriteLog(string.Format("[{0}][Workshop Item Update Checker Schedule] Server is not running. Skipping...", Logger.GetLoggingDate()));
            return;
        }

        ScheduleItem serverRestartSchedule = Scheduler.GetItem("ServerRestart");
        ScheduleItem serverRestartAnnouncer = Scheduler.GetItem("ServerRestartAnnouncer");

        if(serverRestartSchedule == null)
        {
            Logger.WriteLog(string.Format("[{0}][Workshop Item Update Checker Schedule] serverRebootSchedule is null.", Logger.GetLoggingDate()));
            return;
        }
        else if(serverRestartSchedule.NextExecuteTime.Subtract(DateTime.Now).TotalMilliseconds <= Application.botSettings.ServerScheduleSettings.WorkshopItemUpdateRestartTimer)
        {
            Logger.WriteLog(string.Format("[{0}][Workshop Item Update Checker Schedule] Upcoming restart detected. Skipping...", Logger.GetLoggingDate()));
            return;
        }

        if(serverRestartAnnouncer == null)
        {
            Logger.WriteLog(string.Format("[{0}][Workshop Item Update Checker Schedule] serverRestartAnnouncer is null.", Logger.GetLoggingDate()));
            return;
        }

        string configFilePath = ServerUtility.GetServerConfigIniFilePath();
        if(string.IsNullOrEmpty(configFilePath))
        {
            Logger.WriteLog(string.Format("[{0}][Workshop Item Update Checker Schedule] configFilePath is null or empty.", Logger.GetLoggingDate()));
            return;
        }

        IniParser.IniData iniData = IniParser.Parse(configFilePath);
        if(iniData == null)
        {
            Logger.WriteLog(string.Format("[{0}][Workshop Item Update Checker Schedule] iniData is null.", Logger.GetLoggingDate()));
            return;
        }

        string[] workshopIdList = iniData.GetValue("WorkshopItems").Split(';');
        var fetchDetails = Task.Run(async () => await SteamWebAPI.GetWorkshopItemDetails(workshopIdList));
        var itemDetails  = fetchDetails.Result;

        foreach(var item in itemDetails)
        {
            var updateDate = DateTimeOffset.FromUnixTimeSeconds(item.TimeUpdated);

            if(updateDate > Application.startTime)
            {
                var  publicChannel    = BotUtility.Discord.GetTextChannelById(Application.botSettings.PublicChannelId);
                var  logChannel       = BotUtility.Discord.GetTextChannelById(Application.botSettings.LogChannelId);
                uint restartInMinutes = Application.botSettings.ServerScheduleSettings.WorkshopItemUpdateRestartTimer / (60 * 1000);

                if(logChannel != null)
                {
                    logChannel.SendMessageAsync("**[Workshop Mod Update Checker]** A workshop mod update has been detected. Preparing to restart server in "+restartInMinutes.ToString()+" minute(s).");
                }
                else
                {
                    Logger.WriteLog(string.Format("[{0}][Workshop Item Update Checker Schedule] logChannel is null.", Logger.GetLoggingDate()));
                    return;
                }

                if(publicChannel != null)
                {
                    publicChannel.SendMessageAsync("**[Workshop Mod Update Checker]** A workshop mod update has been detected. Server will be restarted in "+restartInMinutes.ToString()+" minute(s).");
                }
                else
                {
                    Logger.WriteLog(string.Format("[{0}][Workshop Item Update Checker Schedule] publicChannel is null.", Logger.GetLoggingDate()));
                    return;
                }

                ServerUtility.Commands.ServerMsg("Workshop mod update has been detected. Server will be restarted in "+restartInMinutes.ToString()+" minute(s).");
                serverRestartSchedule.UpdateInterval(Application.botSettings.ServerScheduleSettings.WorkshopItemUpdateRestartTimer);
                Application.startTime = DateTime.UtcNow.AddMinutes(restartInMinutes);
                break;
            }
        }
    }
}
