using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using TShockAPI;

namespace Region_Buffs
{
    internal class ConfigFile
    {
        public static bool CountDownInRegions = false;
        public static string fileString = Path.Combine(TShock.SavePath, "Region Buffs Config.txt");
        public static string[] GlobalBannedBuffs = new string[] { "" };
        public static bool GreetOnEnter = true;
        public static bool KickAfterCountDown = true;
        public static string KickMessage = "You are using a banned buff, please learn how to disable it.";
        public static uint TimesBeforeKick = 30;
        public static bool WriteToConsole = false;
        public static bool Override = true;

        public static void SetupConfig ( )
        {
            if (File.Exists(fileString))
            {
                string[] lines = File.ReadAllLines(fileString);
                bool badSet = false;
                bool badBuff = false;
                for (int i = 0; i < lines.Length; i++)
                {
                    string[] split = lines[i].Split(new char[] { '=' });
                    switch (split[0])
                    {
                        case "Add Region Command Shortcuts":
                        {
                            bool hold;
                            if (bool.TryParse(split[1].ToLower(), out hold))
                                Override = hold;

                            else badSet = true;
                            break;
                        }
                        case "Notify Players When Recieving Buffs":
                        {
                            bool hold;
                            if (bool.TryParse(split[1].ToLower(), out hold))
                                GreetOnEnter = hold;
                            
                            else badSet = true;
                            break;
                        }
                        case "Write Banned Buffs Disables to Console (very spammy)":
                        {
                            bool hold;
                            if (bool.TryParse(split[1].ToLower(), out hold))
                                WriteToConsole = hold;
                            
                            else badSet = true;
                            break;
                        }
                        case "Count Down In Regions (not just globally)":
                        {
                            bool hold;
                            if (bool.TryParse(split[1].ToLower(), out hold))
                                CountDownInRegions = hold;
                            
                            else badSet = true;
                            break;
                        }
                        case "Banned Buff Usage Countdown (seconds)":
                        {
                            uint hold;
                            if (uint.TryParse(split[i].ToLower(), out hold))
                                TimesBeforeKick = hold;
                            
                            else badSet = true;
                            break;
                        }
                        case "Kick After Countdown":
                        {
                            bool hold;
                            if (bool.TryParse(split[i].ToLower(), out hold))
                                KickAfterCountDown = hold;
                            
                            else badSet = true;
                            break;
                        }
                        case "Kick Message":
                        {
                            KickMessage = ( split[1] == "" ) ? "You have used an illegal buff, learn how to turn it off." : split[1];
                            break;
                        }
                        case "Globally Banned Buffs":
                        {
                            GlobalBannedBuffs = split[1].Split(new char[] { ',' });
                            if (( GlobalBannedBuffs.Length != 0 ) && ( GlobalBannedBuffs != null ))
                            {
                                foreach (string str in GlobalBannedBuffs.Where(s => !string.IsNullOrEmpty(s)))
                                {
                                    if (TShock.Utils.GetBuffByName(str).Count != 1)
                                    {
                                        badBuff = true;
                                        //break;
                                    }
                                }
                            }
                            break;
                        }
                    }
                }
                if (badSet)
                {
                    Log.Error("One or more of the settings was written incorrectly in the Region Buff Config.");
                    Console.WriteLine("One or more of the values in the Region Buff Config was written incorrectly, going with those default value(s).");
                }
                if (badBuff)
                {
                    Console.WriteLine("One or more of the buffs in Globally Banned Buffs was written wrong (use names seperated by commas and no spaces)!");
                    Log.Error("Region Buff Config -> Globally Banned Buffs: Written wrong, going with the default of no banned buffs");
                }
            }
            else
            {
                try
                {
                    File.WriteAllLines(fileString, new string[] { 
                        "# Region Buffs Config File: Determines the settings of the plugin.", 
                        "# Please do not remove any lines in the file! You may add as many as you want, so long as they begin with \"#\"", 
                        "# No spaces after the \"=\" and after the commas in the banned buffs list. Write all buff names, not numbers, and use one line.", 
                        "", 
                        "Notify Players When Recieving Buffs=false", 
                        "",
                        "Add Region Command Shortcuts=true",
                        "",
                        "Kick After Coundown=false", 
                        "Banned Buffs Usage Countdown (seconds)=30", 
                        "Count Down in Regions (not just globally)=true", 
                        "Kick Message=You used an illegal buff for too long, learn how to turn it off!", 
                        "", 
                        "Write Banned Buffs Diables to Console (very spammy)=false", 
                        "", 
                        "Globally Banned Buffs=Invisibility,Pet Bunny" });
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error when creating a new Region Buff Config file, check logs for details.");
                    Log.Error(ex.ToString());
                }
            }
        }
    }
}

