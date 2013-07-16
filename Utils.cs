using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TShockAPI;
using Terraria;

namespace Region_Buffs
{
    static class Utils
    {
        public static string BuffNameToShort ( string name )
        {
            if (TShock.Utils.GetBuffByName(name).Count == 0)
                return "";

            string ret = name[0].ToString();

            for (int i = 1; i < name.Length; i++)
            {
                if (TShock.Utils.GetBuffByName(ret).Count == 1)
                    return ret;

                else ret += name[i];
            }
            return "";
        }
    }
}
