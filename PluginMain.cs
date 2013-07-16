using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using TShockAPI;
using TShockAPI.DB;
using Terraria;
using Hooks;

namespace Region_Buffs
{
    [APIVersion(1, 12)]
    public class PluginMain : TerrariaPlugin
    {
        private List<int> GloballyBanned;
        private static List<Buffee> Plrs;
        private Thread WorkThread;

        #region Overrides

        public override string Author
        { get { return "Snirk Immington"; } }

        public override string Description
        { get { return "Allows for regionally applies, allowed, or banned buffs!"; } }

        public override string Name
        { get { return "Region Buffs"; } }

        public override Version Version
        { get { return new Version(2, 1, 1); } }

        public PluginMain ( Main game ) : base(game)
        {
            Order = 1;
            Plrs = new List<Buffee>();
            GloballyBanned = new List<int>();
        }

        #endregion

        #region Initialize

        public override void Initialize ( )
        {
            NetHooks.GreetPlayer += OnGreet;
            ServerHooks.Leave += OnLeave;
            GameHooks.PostInitialize += OnPost;
            GameHooks.Initialize += OnInit;
        }

        protected override void Dispose ( bool disposing )
        {
            if (disposing)
            {
                NetHooks.GreetPlayer-= OnGreet;
                ServerHooks.Leave -= OnLeave;
                GameHooks.PostInitialize -= OnPost;
                GameHooks.Initialize -= OnInit;
                try
                {
                    WorkThread.Abort();
                }
                catch  // This shouldn't happen, but just in case.
                {
                }
            }
            base.Dispose(disposing);
        }

        private void OnInit ( )
        {
            Commands.ChatCommands.Add(new Command(Com, "rbuff", "regionbuff", "regionbuffs"));
            // Override /region commands.
            if (ConfigFile.Override)
            {
                Commands.ChatCommands.Add(new Command(Permissions.manageregion, rdef, "rdef") { AllowServer = false });
                Commands.ChatCommands.Add(new Command(Permissions.manageregion, rdel, "rdel"));
                Commands.ChatCommands.Add(new Command(Permissions.manageregion, rprot, "rprot"));
                Commands.ChatCommands.Add(new Command(Permissions.maintenance, rallow, "rallow"));
                Commands.ChatCommands.Add(new Command(Permissions.manageregion, rremove, "rremove"));
                Commands.ChatCommands.Add(new Command(Permissions.manageregion, rallowg, "rallowg"));
                Commands.ChatCommands.Add(new Command(Permissions.manageregion, rremoveg, "rremoveg"));
                Commands.ChatCommands.Add(new Command(Permissions.manageregion, rz, "rz"));
                Commands.ChatCommands.Add(new Command(Permissions.manageregion, rsize, "rsize"));
            }
        }

        private void OnPost ( )
        {
            WorkThread = new Thread(UpdateThread);
            WorkThread.Start();
        }

        #endregion

        #region Hooks

        private void OnGreet ( int who, HandledEventArgs args )
        {
            Plrs.Add(new Buffee(who));
        }

        private void OnLeave ( int who )
        {
            Plrs.RemoveAll(p => p.Index == who);
        }

        private void UpdateThread ( )
        {
            while (true)
            {
                try
                {
                    foreach (var ply in TShock.Players.Where(p => p != null && p.RealPlayer))
                    {
                        List<Region> inRegions = TShock.Regions.InAreaRegion(ply.TileX, ply.TileY);
                        List<BuffProp> AppliedBuffs = new List<BuffProp>();
                        List<BuffProp> AllowedBuffs = new List<BuffProp>();
                        List<BuffProp> BannedBuffs = new List<BuffProp>(GloballyBanned.ConvertAll(b => new BuffProp(b, "global")));

                        var plrbuffs = ply.TPlayer.buffType;

                        Buffee buffee = Plrs.FirstOrDefault(p => p.Index == ply.Index);

                        if (buffee != null)
                        {
                            if (buffee.Notifications == 0)
                            {
                                TShock.Utils.Kick(ply, Region_Buffs.ConfigFile.KickMessage, true, false, null, false);
                            }
                            foreach (var regName in inRegions.ConvertAll(r => r.Name).Where(r => r.ToLower().Contains('|')))
                            {
                                string buffpart = "";
                                string region = "";
                                for (int i = 0; i < regName.Length; i++)
                                {
                                    if (regName[i] == '|')
                                    {
                                        buffpart = regName.Substring(i + 1);
                                        break;
                                    }
                                    region = region + regName[i];
                                }
                                //Console.Write("Region {0} ({1}), buffs: ", region, buffpart);
                                foreach (string buff in buffpart.Split(','))
                                {
                                    // +well@12
                                    uint length = 120; var parts = buff.Split('@');

                                    if (uint.TryParse(parts[1], out length))
                                    {
                                        length = Math.Max(100, length);
                                        //length = Math.Min(3600, length);
                                    }

                                    //Console.Write("Parts: [0] = {0}, [1] = {1}. ", parts[0], parts[1]);
                                    List<int> buffByName = TShock.Utils.GetBuffByName(parts[0].Substring(1));
                                    //Console.WriteLine("Searching {0}... matched: {1}", parts[0].Substring(1), string.Join(", ", TShock.Utils.GetBuffByName(parts[0].Substring(1))));
                                    if (buffByName.Count == 1)
                                    {
                                        switch (parts[0].Trim()[0])
                                        {
                                            case '+':
                                            AppliedBuffs.Add(new BuffProp(buffByName[0], region, length));
                                            break;

                                            case '-':
                                            BannedBuffs.Add(new BuffProp(buffByName[0], region, length));
                                            break;

                                            case '%':
                                            AllowedBuffs.Add(new BuffProp(buffByName[0], region, length));
                                            break;

                                            default: AppliedBuffs.Add(new BuffProp(buffByName[0], region, length)); break;
                                        }
                                    }

                                }
                            }
                            BannedBuffs.RemoveAll(b => AppliedBuffs.Any(a => a.Buff == b.Buff) || AllowedBuffs.Any(a => a.Buff == b.Buff));

                            // I do love List.Distinct() as it has fixed bugs
                            AppliedBuffs = AppliedBuffs.Distinct().ToList();
                            AllowedBuffs = AllowedBuffs.Distinct().ToList();
                            BannedBuffs = BannedBuffs.Distinct().ToList();

                            if (buffee.BuffPreference != 2)
                            {
                                if (buffee.BuffPreference == 1)
                                {
                                    AppliedBuffs.RemoveAll(apl => ( ( apl.Buff > 0x13 ) && ( apl.Buff < 0x19 ) ) || ( ( apl.Buff > 0x1d ) && ( apl.Buff < 40 ) ));
                                }

                                #region applied buffs
                                List<BuffProp> toApply = new List<BuffProp>();
                                foreach (BuffProp pair in AppliedBuffs)
                                {
                                    if (!plrbuffs.Contains(pair.Buff))
                                    {
                                        toApply.Add(pair);
                                    }
                                    ply.SetBuff(pair.Buff, (int)pair.Time, true);
                                }
                                #endregion

                                if (( Region_Buffs.ConfigFile.GreetOnEnter && buffee.GetInfo ) && ( toApply.Count > 0 ))
                                {
                                    ply.SendSuccessMessage("You have been buffed with the {0} buff{1} from the \"{2}\" region{1}!".SFormat(
                                        string.Join(", ", toApply.Select(a => TShock.Utils.GetBuffName(a.Buff))), toApply.Count > 1 ? "s" : "",
                                        string.Join("\", \"", toApply.Select(b => b.Region)), toApply.Count > 1 ? "s":""));
                                    ply.SendInfoMessage("Use /rb notify to stop recieving notifications.");
                                }
                            }

                            if (!ply.Group.HasPermission("usebannedbuff"))
                            {
                                var hadBanned = BannedBuffs.Where<BuffProp>(b => plrbuffs.Contains(b.Buff)).ToList();
                                if (hadBanned.Count > 0)
                                {
                                    bool flag = false;
                                    if (Region_Buffs.ConfigFile.KickAfterCountDown)
                                    {
                                        foreach (BuffProp pair2 in hadBanned)
                                        {
                                            if (!flag)
                                            {
                                                if (Region_Buffs.ConfigFile.CountDownInRegions)
                                                {
                                                    flag = true;
                                                    buffee.Notifications--;
                                                }
                                                else if (pair2.Region == "global")
                                                {
                                                    flag = true;
                                                    buffee.Notifications--;
                                                }
                                            }
                                        }
                                    }
                                    if (!flag)
                                    {
                                        buffee.Notifications = Region_Buffs.ConfigFile.TimesBeforeKick;
                                    }

                                    ply.SetBuff(0x21, 150, true);
                                    ply.SetBuff(0x20, 150, true);
                                    ply.SetBuff(0x17, 150, true);

                                    var buffs = string.Join(", ", hadBanned.Select(c => TShock.Utils.GetBuffName(c.Buff)));

                                    if (Region_Buffs.ConfigFile.WriteToConsole)
                                    {
                                        Console.WriteLine("{0} has been diabled for using {1} buff(s)!", ply.Name, buffs);
                                    }
                                    ply.SendErrorMessage("Your buff{0}: {1}, {2} banned! Please right click the icon{0} to remove {3}!".SFormat(
                                        hadBanned.Count > 1 ? "s" : "", buffs, ( hadBanned.Count > 1 ) ? "are" : "is", ( hadBanned.Count > 1 ) ? "them" : "it"));

                                    if (Region_Buffs.ConfigFile.KickAfterCountDown && flag)
                                    {
                                        ply.SendWarningMessage("You have {0} more seconds of trying to use an illegal buff before you're kicked.".SFormat(new object[] { buffee.Notifications }));
                                    }
                                }
                            }
                        }

                    }
                    Thread.Sleep(0x44c);
                }
                catch (Exception) { }
            }
        }

        #endregion

        #region Commands

        private static List<Region> GetRegions ( string search )
        {
            var ret = new List<Region>();
            foreach (var reg in TShock.Regions.ListAllRegions(Main.worldID.ToString()))
            {
                if (reg.Name.ToLower() == search)
                    return new List<Region>() { reg };

                else if (reg.Name.ToLower().StartsWith(search))
                    ret.Add(reg);
            }
            return ret;
        }

        // This used to be the only command, hence, com.
        private void Com ( CommandArgs com )
        {
            List<string> list = new List<string>();
            if (Region_Buffs.ConfigFile.GreetOnEnter)
            {
                list.Add("/rb notify - toggles the notifications you get when entering buffed regions!");
            }
            if (com.Player.Group.HasPermission("regionbuff"))
            {
                list.Add("/rb bufftype - choose to recieve all, good, or no buffs from buffed regions!");
            }
            if (com.Player.Group.HasPermission(Permissions.maintenance))
            {
                list.Add("/rb reload - reloads the Region Buffs config file!");
            }
            if (com.Player == TSPlayer.Server)
            {
                list.Add("/rb editconfig - opens the config editor window!");
            }
            if (com.Parameters.Count == 0)
            {
                if (list.Count == 1) com.Player.SendErrorMessage("Usage: " + list[0]);

                else if (list.Count > 1)
                {
                    com.Player.SendInfoMessage("You have access to these Region Buffs subcommands:");
                    list.ForEach(i => com.Player.SendInfoMessage(i));
                }
                else com.Player.SendErrorMessage("You do not have access to any Region Buffs subcommands on this server.");
            }
            else // it's ok?
            {
                switch (com.Parameters[0].ToLower())
                {
                    #region Buff type
                    case "bufftype":
                    case "buffs":
                    case "getbuffs":
                    case "gettype":
                    {
                        if (!com.Player.Group.HasPermission("regionbuffs"))
                        {
                            com.Player.SendErrorMessage("You do not have permission to use /rb bufftype!");
                            return;
                        }

                        var buffee2 = Plrs.FirstOrDefault(b => b.Player.Index == com.Player.Index);
                        if (buffee2 == null) return; // never gonna happen

                        if (com.Parameters.Count == 1)
                        {
                            com.Player.SendErrorMessage("Usage: /rb bufftype all|buffs|none - choose which kinds of buffs you get from buffed regions!");
                            return;
                        }

                        switch (com.Parameters[1].ToLower())
                        {
                            case "all":
                            buffee2.BuffPreference = 0;
                            com.Player.SendSuccessMessage("You recieve all buffs from buffed regions!");
                            return;

                            case "buffs":
                            buffee2.BuffPreference = 1;
                            com.Player.SendSuccessMessage("You only recieve buffs from buffed regions!");
                            return;

                            case "none":
                            buffee2.BuffPreference = 2;
                            com.Player.SendSuccessMessage("You are no longer affected by buffed regions!");
                            return;
                        }
                        com.Player.SendErrorMessage("Usage: /rb bufftype all|buffs|none - choose which types of buffs you get from buffed regions!");
                        return;
                    }
                    #endregion

                    #region Notify

                    case "notify":
                    case "notifications":
                    {
                        if (ConfigFile.GreetOnEnter)
                        {
                            var buffee = Plrs.FirstOrDefault(b => b.Index == com.Player.Index);
                            com.Player.SendSuccessMessage("You now are {0}notified upon recieving buffs and debuffs from Buffed Regions!".SFormat(
                                buffee.GetInfo ? "not " : ""));
                            buffee.GetInfo = !buffee.GetInfo;
                        }
                        else com.Player.SendErrorMessage("Region notifications are not toggleable on this server at this time!");

                        return;
                    }

                    #endregion

                    #region reload
                    case "reload":
                    case "reloadconfig":
                    {
                        if (!com.Player.Group.HasPermission(Permissions.maintenance))
                        {
                            com.Player.SendErrorMessage("You do not have permission for /rb reload!");
                            return;
                        }

                        ConfigFile.SetupConfig();
                        com.Player.SendInfoMessage("Config reloaded. Check console for errors.");
                        return;
                    }
                    #endregion
/*
                    #region editconfig
                    case "editconfig":
                    case "openconfig":
                    {
                        if (com.Player == TSPlayer.Server)
                        {
                            TSPlayer.Server.SendInfoMessage("Opening config editor now...");
                            var Form = new Config_Editor();
                            Form.Show(); // open it
                        }
                        else if (com.Player.Group == new SuperAdminGroup())
                        {
                            com.Player.SendErrorMessage("This command must be executed through the console.");
                        }
                        else { com.Player.SendErrorMessage("You do not have permission for /rb openconfig!"); }

                        return;
                    }
                    #endregion
                        */
                    default: com.Player.SendErrorMessage("Invalid subcommand!"); return;
                }
            }
        }

        private void rallow ( CommandArgs com )
        {
            //  /rallow <user> <region>

            if (com.Parameters.Count > 1)
            {
                string playerName = com.Parameters[0];
                string regionName = "";

                for (int i = 1; i < com.Parameters.Count; i++)
                {
                    if (regionName == "")
                    {
                        regionName = com.Parameters[1];
                    }
                    else
                    {
                        regionName = regionName + " " + com.Parameters[i];
                    }
                }

                // Typo but I thought it looked cool so hey I went with it
                var regiosn = GetRegions(regionName.ToLower());

                if (regiosn.Count != 1)
                {
                    com.Player.SendErrorMessage(regiosn.Count + " regions found!");
                    return;
                }

                if (TShock.Users.GetUserByName(playerName) != null)
                {
                    if (TShock.Regions.AddNewUser(regiosn[0].Name, playerName))
                    {
                        com.Player.SendInfoMessage("Added user " + playerName + " to " + regiosn[0].Name);
                    }
                    else com.Player.SendErrorMessage("Region " + regionName + " not found");
                }
                else
                {
                    com.Player.SendErrorMessage("Player " + playerName + " not found");
                }
            }
            else com.Player.SendMessage("Invalid syntax! Proper syntax: /rallow [user name] [region name search] (\"/region allow\" shortcut from Region Buffs 2)", Color.Red); 
        }

        private void rallowg ( CommandArgs com )
        {
            if (com.Parameters.Count > 1)
            {
                string group = com.Parameters[0];
                string regionName = "";

                for (int i = 2; i < com.Parameters.Count; i++)
                {
                    if (regionName == "")
                    {
                        regionName = com.Parameters[1];
                    }
                    else
                    {
                        regionName = regionName + " " + com.Parameters[i];
                    }
                }

                var regions = GetRegions(regionName.ToLower());
                if (regions.Count != 1)
                {
                    com.Player.SendErrorMessage(regions.Count + " regions found!");
                    return;
                }

                if (TShock.Groups.GroupExists(group))
                {
                    if (TShock.Regions.AllowGroup(regions[0].Name, group))
                    {
                        com.Player.SendInfoMessage("Added group " + group + " to " + regions[0].Name);
                    }
                    else com.Player.SendErrorMessage("Region " + regionName + " not found");
                }
                else com.Player.SendErrorMessage("Group " + group + " not found");
            }
            else com.Player.SendErrorMessage("Invalid syntax! Usage: /rallow [group] [search name] (\"/region allow\" from Region Buffs 2)");
        }

        private void rremove ( CommandArgs com )
        {
            if (com.Parameters.Count > 1)
            {
                string playerName = com.Parameters[0];
                string regionName = "";

                for (int i = 2; i < com.Parameters.Count; i++)
                {
                    if (regionName == "")
                    {
                        regionName = com.Parameters[1];
                    }
                    else
                    {
                        regionName = regionName + " " + com.Parameters[i];
                    }
                }

                var regions = GetRegions(regionName.ToLower());
                if (regions.Count != 1)
                {
                    com.Player.SendErrorMessage(regions.Count + " regions matched!");
                    return;
                }

                if (TShock.Users.GetUserByName(playerName) != null)
                {
                    if (TShock.Regions.RemoveUser(regions[0].Name, playerName))
                    {
                        com.Player.SendInfoMessage("Removed user " + playerName + " from " + regions[0].Name);
                    }
                    else com.Player.SendInfoMessage("Region " + regionName + " not found");
                }
                else com.Player.SendErrorMessage("Player " + playerName + " not found");
            }
            else com.Player.SendErrorMessage("Invalid syntax! Proper syntax: /rremove [user name] [region name search] (\"/region remove\" shortcut from Region Buffs 2)");
        }

        private void rremoveg ( CommandArgs com )
        {
            //  /rremoveg player region
            if (com.Parameters.Count > 1)
            {
                string group = com.Parameters[0];
                string regionName = "";

                for (int i = 2; i < com.Parameters.Count; i++)
                {
                    if (regionName == "")
                    {
                        regionName = com.Parameters[1];
                    }
                    else
                    {
                        regionName = regionName + " " + com.Parameters[i];
                    }
                }

                var regions = GetRegions(regionName.ToLower());
                if (regions.Count != 1)
                {
                    com.Player.SendErrorMessage(regions.Count + " regions matched!");
                    return;
                }

                if (TShock.Groups.GroupExists(group))
                {
                    if (TShock.Regions.RemoveGroup(regions[0].Name, group))
                    {
                        com.Player.SendErrorMessage("Removed group " + group + " from " + regions[0].Name);
                    }
                    else com.Player.SendErrorMessage("Region " + regionName + " not found");
                }
                else com.Player.SendErrorMessage("Group " + group + " not found");
                
            }
            else com.Player.SendErrorMessage("Invalid syntax! Usage: /rremoveg [group] [name search] (\"/region removeg\" shortcut from Region Buffs 2)");
        }

        private void rz ( CommandArgs com )
        {
            if (com.Parameters.Count == 2)
            {
                var regions = GetRegions(com.Parameters[0]);
                if (regions.Count != 1)
                {
                    com.Player.SendErrorMessage(regions.Count + " regions found!");
                    return;
                }

                int z = 0;
                if (int.TryParse(com.Parameters[2], out z))
                {
                    if (TShock.Regions.SetZ(regions[0].Name, z))
                        com.Player.SendInfoMessage("Region's z is now " + z);

                    else com.Player.SendErrorMessage("Could not find specified region");
                }
                else com.Player.SendErrorMessage("Invalid syntax! Usage: /rz [name search] [#] (\"/region z\" shortcut from Region Buffs 2)");
            }
            else com.Player.SendErrorMessage("Invalid syntax! Usage: /rz [name search] [#] (\"/region z\" shortcut from Region Buffs 2)");
        }

        private void rprot ( CommandArgs com )
        {
            if (com.Parameters.Count == 2)
            {
                var regions = GetRegions(com.Parameters[0].ToLower());

                if (regions.Count != 1)
                {
                    com.Player.SendErrorMessage(regions.Count + " regions matched!");
                    return;
                }

                if (com.Parameters[1].ToLower() == "true" || com.Parameters[2].ToLower() == "t")
                {
                    if (TShock.Regions.SetRegionState(regions[0].Name, true))
                        com.Player.SendInfoMessage("Protected region " + regions[0].Name);

                    else com.Player.SendErrorMessage("Could not find specified region!");
                }
                else if (com.Parameters[1].ToLower() == "false" || com.Parameters[2].ToLower() == "f")
                {
                    if (TShock.Regions.SetRegionState(regions[0].Name, false))
                        com.Player.SendInfoMessage("Unprotected region " + regions[0].Name);

                    else com.Player.SendErrorMessage("Could not find specified region");
                }
                else com.Player.SendErrorMessage("Invalid syntax! Usage: /rprot [name search] [t/f] (\"/region protect\" shortcut from Region Buffs 2)");
            }
            else com.Player.SendErrorMessage("Invalid syntax! Usage: /rprot [name search] [t/f] (\"/region protect\" shortcut from Region Buffs 2)");
        }

        private void rdef ( CommandArgs com )
        {
            //  /rdef <name>

            if (com.Parameters.Count > 0)
            {
                if (!com.Player.TempPoints.Any(p => p == Point.Zero))
                {
                    string regionName = String.Join(" ", com.Parameters);

                    var x = Math.Min(com.Player.TempPoints[0].X, com.Player.TempPoints[1].X);
                    var y = Math.Min(com.Player.TempPoints[0].Y, com.Player.TempPoints[1].Y);

                    var width = Math.Abs(com.Player.TempPoints[0].X - com.Player.TempPoints[1].X);
                    var height = Math.Abs(com.Player.TempPoints[0].Y - com.Player.TempPoints[1].Y);

                    if (TShock.Regions.AddRegion(x, y, width, height, regionName, com.Player.UserAccountName,
                                                 Main.worldID.ToString()))
                    {
                        com.Player.TempPoints[0] = Point.Zero;
                        com.Player.TempPoints[1] = Point.Zero;
                        com.Player.SendInfoMessage("Set region " + regionName);
                    }
                    else com.Player.SendErrorMessage("Region \"" + regionName + "\" already exists.");
                }
                else com.Player.SendErrorMessage("Points not set up yet! Use /region set [1/2] to set up a region."); 
            }
            else com.Player.SendErrorMessage("Invalid syntax! Usage: /rdef [name] (\"/region define\" shortcut from Region Buffs 2)");
        }

        private void rdel ( CommandArgs com )
        {
            if (com.Parameters.Count > 0)
            {
                string regionName = String.Join(" ", com.Parameters);

                var regions = GetRegions(regionName);

                if (regions.Count != 1)
                {
                    com.Player.SendErrorMessage(regions.Count + " regions matched!");
                    return;
                }

                if (TShock.Regions.DeleteRegion(regions[0].Name))
                    com.Player.SendInfoMessage("Deleted region " + regionName);

                else com.Player.SendErrorMessage("Error with database/could not find specified region.");
            }
            else com.Player.SendMessage("Invalid syntax! Proper syntax: /rdel [name search] (\"/region delete\" shortcut from Region Buffs 2)", Color.Red);
        }

        private void rsize ( CommandArgs com )
        {
            //  /rsize [search] [dir] [amt]
            if (com.Parameters.Count == 3)
            {
                int direction;
                switch (com.Parameters[1])
                {
                    case "u":
                    case "up":
                    {
                        direction = 0;
                        break;
                    }
                    case "r":
                    case "right":
                    {
                        direction = 1;
                        break;
                    }
                    case "d":
                    case "down":
                    {
                        direction = 2;
                        break;
                    }
                    case "l":
                    case "left":
                    {
                        direction = 3;
                        break;
                    }
                    default:
                    {
                        direction = -1;
                        break;
                    }
                }
                int addAmount;
                int.TryParse(com.Parameters[2], out addAmount);

                var regions = GetRegions(com.Parameters[0].ToLower());

                if (regions.Count != 1)
                {
                    com.Player.SendErrorMessage(regions.Count + " regions found!");
                    return;
                }

                if (TShock.Regions.resizeRegion(regions[0].Name, addAmount, direction))
                {
                    com.Player.SendInfoMessage("Region Resized Successfully!");
                    try
                    {
                        TShock.Regions.ReloadAllRegions();
                    }
                    catch (Exception)
                    { com.Player.SendErrorMessage("Database error! Try again!"); }
                }
                else com.Player.SendErrorMessage("Invalid syntax! Proper syntax: /rsize [name search] [u/d/l/r] [amount] (\"/region resize\" shortcut from Region Buffs 2)");
            }
            else com.Player.SendErrorMessage("Invalid syntax! Proper syntax: /rsize [name search] [u/d/l/r] [amount] (\"/region resize\" shortcut from Region Buffs 2)");   
        }

        #endregion
    }
}

