using TShockAPI;

namespace Region_Buffs
{
    internal class Buffee
    {

        /// <summary>
        /// 0 = all, 1 = buffs, 2 = none, 3 = debuffs
        /// </summary>
        public byte BuffPreference { get; set; }

        /// <summary>
        /// Whether the player wants to recieve messages
        /// </summary>
        public bool GetInfo { get; set; }

        /// <summary>
        /// TSPlayer index
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Number of times they've been told to stop.
        /// </summary>
        public uint Notifications { get; set; }

        /// <summary>
        /// The TShock player.
        /// </summary>
        public TSPlayer Player { get; set; }

        /// <summary>
        /// The Terraria player.
        /// </summary>
        public Terraria.Player TPlayer { get; set; }

        public Buffee ( int index )
        {
            Index = index;
            BuffPreference = 0;
            if (Region_Buffs.ConfigFile.GreetOnEnter) GetInfo = true;
            Notifications = Region_Buffs.ConfigFile.TimesBeforeKick;
        }
    }

}
