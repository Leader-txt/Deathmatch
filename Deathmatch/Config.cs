using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Terraria;
using TShockAPI;
using Z.Expressions;

namespace Deathmatch
{
    public class Room
    {
        [JsonProperty("图格坐标x")]
        public int TileX { get; set; }
        [JsonProperty("图格坐标y")]
        public int TileY { get; set; }
        [JsonProperty("长度")]
        public int Width { get; set; }
        [JsonProperty("高度")]
        public int Height { get; set; }
    }
    public class Skill
    {
        //技能名称 弹幕id 图像 生成位置 生成间隔 生成时间间隔 
        //使用冷却时间 使用消耗魔力 触发条件
        [JsonProperty("技能名称")]
        public string Name { get; set; } = "";
        [JsonProperty("弹幕id")]
        public int Id { get; set; } = 1;
        [JsonProperty("伤害")]
        public int Damage { get; set; } = 100;
        [JsonProperty("击退")]
        public float KnockBack { get; set; } = 1;
        [JsonProperty("解析式 自变量为x")]
        public string Func { get; set; } = "x^2";
        //技能触发动作 wasd u-使用物品 p-跳跃
        [JsonProperty("技能触发动作 wasd-上下左右 p-跳跃 u-使用物品")]
        public string Trigger { get; set; } = "";
        [JsonProperty("原点位置x偏移量")]
        public int X { get; set; } = 0;
        [JsonProperty("原点位置y偏移量")]
        public int Y { get; set; } = 0;
        [JsonProperty("自变量取值范围0-此值")]
        public int Legnth { get; set; } = 10;
        [JsonProperty("自变量每次增量")]
        public float Step { get; set; } = 0.1f;
        [JsonProperty("时间间隔 ms")]
        public int TimeSpan { get; set; } = 100;
        [JsonProperty("是否以敌方为攻击对象")]
        public bool Trace { get; set; } = false;
        [JsonProperty("技能冷却时间 ms")]
        public int UseSpan { get; set; } = 2000;
        [JsonProperty("技能消耗魔法值")]
        public int Mana { get; set; } = 10;
        public bool Release(TSPlayer user,TSPlayer anamy)
        {
            if (user.TPlayer.statMana < Mana)
            {
                return false;
            }
            Utils.SendTextAboveHead(user.Index, Name, Color.Red);
            user.TPlayer.statMana -= Mana;
            user.SendData(PacketTypes.PlayerMana, "", user.Index);
            for (float i = 0; i < Legnth; i += Step)
            {
                var pos = user.TPlayer.position + new Vector2(X+i, Eval.Execute<float>(Func, new { x = i })+Y);
                Projectile.NewProjectile(null, pos, Trace ? anamy.TPlayer.position - pos : pos - (new Vector2(X, Y) + user.TPlayer.position), Id, Damage, KnockBack);
                Thread.Sleep(TimeSpan);
            }
            return true;
        }
    }
    public class Character
    {
        [JsonProperty("角色名")]
        public string Name { get; set; }
        [JsonProperty("技能")]
        public Skill[] Skills { get; set; } = new Skill[] { new Skill() };   
    }
    public class Config
    {
        [JsonProperty("房间")]
        public Room[] Rooms { get; set; } = new Room[] { new Room() };
        [JsonProperty("角色")]
        public Character[] Characters { get; set; }=new Character[] { new Character() };
        private const string path = "tshock/Deathmatch.json";
        public void Save()
        {
            File.WriteAllText(path, JsonConvert.SerializeObject(this, Formatting.Indented),Encoding.UTF8);
        }
        public static Config GetConfig()
        {
            if (!File.Exists(path))
                new Config().Save();
            return JsonConvert.DeserializeObject<Config>(File.ReadAllText(path));
        }
    }
}
