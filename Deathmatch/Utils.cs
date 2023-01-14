using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TShockAPI;

namespace Deathmatch
{
    public class Utils
    {
        public static void SendTextAboveHead(int who, string text, Color color)
        {
            var plr = TShock.Players[who];
            var data = new MemoryStream();
            using (var wr = new BinaryWriter(data))
            {
                wr.Write((short)0);
                wr.Write((byte)82);
                wr.Write((short)1);
                wr.Write((byte)who);
                wr.Write((byte)0);
                wr.Write(text);
                wr.Write((byte)color.R);
                wr.Write((byte)color.G);
                wr.Write((byte)color.B);
                var length = wr.BaseStream.Length;
                wr.BaseStream.Position = 0;
                wr.Write((short)length);
            }
            plr.SendRawData(data.ToArray());
        }
        public static void SendCombatText(int who, string text, Color color)
        {
            var plr = TShock.Players[who];
            plr.SendData(PacketTypes.CreateCombatTextExtended, text, (int)color.PackedValue, plr.X, plr.Y);
        }
    }
}
