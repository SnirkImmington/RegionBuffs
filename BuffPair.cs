using System;

namespace Region_Buffs
{
    internal class BuffProp
    {
        public BuffProp ( int buff, string region, uint time = 120 )
        {
            Buff = buff; Region = region.Trim();
            Time = time;
        }

        public int Buff { get; set; }

        public string Region { get; set; }

        public uint Time { get; set; }
    }
}

