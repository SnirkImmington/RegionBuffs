using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Linq;
using System.Text;
using System.IO;

namespace Region_Buffs
{
    [Serializable()]
	class RegionBuffConfig : ISerializable
	{
        public bool Override { get; set; }
        public bool Kick { get; set; }
        public bool CountdownInRegions { get; set; }
        public int[] GloballyBannedBuffs { get; set; }
        public bool Greet { get; set; }
        public string KickMessage { get; set; }
        public UInt16 TimeBeforeKick { get; set; }
        public bool WriteToConsole { get; set; }
        public Macro[] Macros { get; set; }

        //public static string FileNamePath { get { return @"Users\%userprofile%\AppData\Roaming\.tshock\Region Buffs Config.obj"; } }

        public RegionBuffConfig ( ) { }

        public RegionBuffConfig ( SerializationInfo info, StreamingContext con )
        {
            Override = info.GetBoolean("o");
            Kick = info.GetBoolean("k");
            CountdownInRegions = info.GetBoolean("b");
            TimeBeforeKick = info.GetUInt16("i");
            Greet = info.GetBoolean("r");
            KickMessage = info.GetString("m");
            WriteToConsole = info.GetBoolean("c");
            GloballyBannedBuffs = (int[])info.GetValue("g", typeof(int[]));
            Macros = (Macro[])info.GetValue("M", typeof(Macro[]));
        }

        public void GetObjectData ( SerializationInfo info, StreamingContext con )
        {
            info.AddValue("o", Override);
            info.AddValue("k", Kick);
            info.AddValue("b", CountdownInRegions);
            info.AddValue("i", TimeBeforeKick);
            info.AddValue("r", Greet);
            info.AddValue("m", KickMessage);
            info.AddValue("c", WriteToConsole);
            info.AddValue("g", GloballyBannedBuffs);
        }

        public static RegionBuffConfig GetDefault ( )
        {
            return new RegionBuffConfig()
            {
                CountdownInRegions = true,
                Kick = false,
                GloballyBannedBuffs = new int[] { },
                Greet = true,
                KickMessage = "Learn how to disable buffs!",
                Macros = new Macro[] { },
                Override = true,
                TimeBeforeKick = 50,
                WriteToConsole = false
            };
        }

        public static string FileName { get { return Path.Combine(TShockAPI.TShock.SavePath, "Region Buff Config.obj"); } }

        /// <summary>
        /// Sets up config and tells player of success.
        /// </summary>
        /// <exception cref="IOException"></exception>
        public static RegionBuffConfig SetupConfig ( TShockAPI.TSPlayer player )
        {
            var turn = GetDefault();
            if (File.Exists(FileName))
            {
                try
                {
                    turn = Serializer<RegionBuffConfig>.Deserialize(FileName);
                    player.SendSuccessMessage("Set up Region Buffs config!");
                }
                catch
                {
                    player.SendErrorMessage("Failed to read Region Buffs Config file!");
                }
            }
            else
            {
                Serializer<RegionBuffConfig>.Serialize(FileName, turn);
                
            }
            return turn;
        }
	}

    [Serializable()]
    class Macro : ISerializable
    {
        public string Title { get; set; }

        public string Full { get; set; }

        public Macro ( ) { }

        public Macro ( SerializationInfo info, StreamingContext com )
        {
            Title = info.GetString("t"); Full = info.GetString("f");
        }

        public void GetObjectData ( SerializationInfo info, StreamingContext con )
        {
            info.AddValue("t", Title); info.AddValue("f", Full);
        }

        public Macro ( string title, string full )
        {
            Title = title; Full = full;
        }
    }

    class Serializer<T>
    {
        private static Stream stream;
        private static BinaryFormatter formatter = new BinaryFormatter();

        public static void Serialize ( string fileName, T obj )
        {
            stream = File.Open(fileName, FileMode.Create);
            formatter.Serialize(stream, obj);
            stream.Close();
        }

        public static T Deserialize ( string fileName )
        {
            T obj;
            stream = File.Open(fileName, FileMode.Open);
            obj = (T)formatter.Deserialize(stream);
            stream.Close(); return obj;
        }
    }
}
