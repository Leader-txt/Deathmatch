using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace Deathmatch
{
    [ApiVersion(2, 1)]
    public class MainPlugin : TerrariaPlugin
    {
        public override string Name => "Deathmatch";
        public override string Description => "死斗";
        public override string Author => "Leader";
        public override Version Version => new Version(1, 0, 0, 0);
        public static List<(int room, int p1,int p2)> Game { get; set; } = new List<(int room,int p1,int p2)>();
        public static Dictionary<int,string> Action { get; set; } = new Dictionary<int,string>();
        public static Config Config { get; set; }
        public static Dictionary<int, int> Character { get; set; } = new Dictionary<int, int>();
        public static Dictionary<int,Dictionary<int ,DateTime>> Freezing { get; set; }= new Dictionary<int,Dictionary<int ,DateTime>>();
        public MainPlugin(Main game) : base(game)
        {
            Config = Config.GetConfig();
            foreach (var cha in Config.Characters)
            {
                var temp = cha.Skills.ToList();
                temp.Sort((x, y) => x.Trigger.Length.CompareTo(y.Trigger.Length));
                cha.Skills = temp.ToArray();
            }
        }

        public override void Initialize()
        {
            Commands.ChatCommands.Add(new Command("", main, "deathmatch", "dth"));
            ServerApi.Hooks.ServerJoin.Register(this, OnServerJoin);
            GetDataHandlers.PlayerUpdate.Register(OnPlayerUpdate);
            ServerApi.Hooks.ServerLeave.Register(this, OnServerLeave);
            GetDataHandlers.KillMe.Register(OnKillMe);
        }

        private void OnServerLeave(LeaveEventArgs args)
        {
            Character.Remove(args.Who);
            Freezing.Remove(args.Who);
            Action.Remove(args.Who);
        }

        private void OnKillMe(object sender, GetDataHandlers.KillMeEventArgs e)
        {
            bool inGame = false;
            var game = Game[0];
            foreach (var g in Game)
            {
                if (g.p1 == e.Player.Index || g.p2 == e.Player.Index)
                {
                    inGame = true;
                    game = g;
                    return;
                }
            }
            if (inGame)
            {
                Game.Remove(game);
                e.Player.Disconnect("Game over, you died!");
                TShock.Players[game.p1 == e.Player.Index ? game.p2 : game.p1].Disconnect("Game over , you win!");
            }
        }

        private void OnPlayerUpdate(object sender, GetDataHandlers.PlayerUpdateEventArgs e)
        {
            bool inGame = false;
            var room = Config.Rooms[0];
            foreach (var g in Game)
            {
                if (g.p1 == e.Player.Index || g.p2 == e.Player.Index)
                {
                    inGame = true;
                    room = Config.Rooms[g.room];
                    return;
                }
            }
            if (inGame)
            {
                if(e.Player.TileX>room.TileX+room.Width/2||
                    e.Player.TileX<room.TileX-room.Width/2||
                    e.Player.TileY<room.TileY||
                    e.Player.TileY > room.TileY + room.Height)
                {
                    e.Handled = true;
                    e.Player.SendErrorMessage("禁止超出房间边界！");
                }
                bool add = false;
                if (e.Control.MoveUp)
                {
                    Action[e.Player.Index] += "w";
                    add = true;
                }
                if (e.Control.MoveLeft)
                {
                    Action[e.Player.Index] += "a";
                    add = true;
                }
                if (e.Control.MoveDown)
                {
                    Action[e.Player.Index] += "s";
                    add = true;
                }
                if (e.Control.MoveRight)
                {
                    Action[e.Player.Index] += "d";
                    add = true;
                }
                if (e.Control.IsUsingItem)
                {
                    Action[e.Player.Index] += "u";
                    add = true;
                }
                if (e.Control.Jump)
                {
                    Action[e.Player.Index] += "p";
                    add = true;
                }
                if (!add)
                {
                    return;
                }
                var cha = Config.Characters[Character[e.Player.Index]];
                if (Action[e.Player.Index].Length >= cha.Skills[0].Trigger.Length)
                {
                    var ski = cha.Skills.ToList().FindIndex(x => x.Trigger.StartsWith(Action[e.Player.Index]));
                    if (ski != -1)
                    {
                        if (Freezing[e.Player.Index].ContainsKey(ski))
                        {
                            if (Freezing[e.Player.Index][ski] > DateTime.Now)
                            {
                                return;
                            }
                            else
                            {
                                Freezing[e.Player.Index].Remove(ski);
                            }
                        }
                        var skill = cha.Skills[ski];
                        var game = Game.Find(x => x.p1 == e.Player.Index || x.p2 == e.Player.Index);
                        new Thread(() =>
                        {
                            if (skill.Release(e.Player, TShock.Players[game.p1 == e.Player.Index ? game.p2 : game.p1]))
                            {
                                Freezing[e.Player.Index].Add(ski, DateTime.Now + new TimeSpan(skill.UseSpan * 10 * 1000));
                            }
                        })
                        { IsBackground = true }.Start();
                    }
                    else
                    {
                        Action[e.Player.Index] = Action[e.Player.Index].Remove(0, 1);
                    }
                }
            }
            else
            {
                e.Handled = true;
            }
        }

        private void main(CommandArgs args)
        {
            if(args.Parameters.Count == 0)
            {
                args.Player.SendInfoMessage("/dm list，列出所有可选角色");
                args.Player.SendInfoMessage("/dm choose 角色id，选择角色");
                if (args.Player.HasPermission("deathmatch.admin"))
                {
                    args.Player.SendInfoMessage("/dm reload，重载配置文件");
                }
                return;
            }
            switch (args.Parameters[0])
            {
                case "reload":
                    {
                        if (!args.Player.HasPermission("deathmatch.admin"))
                        {
                            args.Player.SendInfoMessage("您无权执行此命令！");
                            return;
                        }
                        Config=Config.GetConfig();
                        foreach (var cha in Config.Characters)
                        {
                            var temp = cha.Skills.ToList();
                            temp.Sort((x, y) => x.Trigger.Length.CompareTo(y.Trigger.Length));
                            cha.Skills = temp.ToArray();
                        }
                        args.Player.SendSuccessMessage("配置文件重载成功！");
                    }
                    break;
                case "choose":
                    {
                        int id=int.Parse(args.Parameters[1]);
                        if (Character.ContainsKey(args.Player.Index))
                        {
                            args.Player.SendErrorMessage("不可重复选择角色！");
                            return;
                        }
                        if (id < 0 || id >= Config.Characters.Length)
                        {
                            args.Player.SendErrorMessage("请输入正确数值！");
                            return;
                        }
                        var temp = Game.Select(x => x.p1).ToList();
                        temp.AddRange(Game.Select(x => x.p2).ToList());
                        var list = Character.ToList().FindAll(x => !temp.Contains(x.Key)).Select(x => x.Key);
                        Character.Add(args.Player.Index, id);
                        Action.Add(args.Player.Index, "");
                        Freezing.Add(args.Player.Index, new Dictionary<int, DateTime>());
                        if (list.Count() == 0)
                        {
                            args.Player.SendSuccessMessage("角色选择成功，请等待玩家加入");
                        }
                        else
                        {
                            for(int i = 0; i < Config.Rooms.Count(); i++)
                            {
                                if (!Game.Select(x => x.room).Contains(i))
                                {
                                    args.Player.SendSuccessMessage("匹配成功，正在传送至房间！");
                                    TShock.Players[list.ToArray()[0]].SendSuccessMessage("匹配成功，正在传送至房间");
                                    Game.Add((i, list.ToArray()[0], args.Player.Index));
                                    args.Player.Teleport(Config.Rooms[i].TileX * 16, Config.Rooms[i].TileY * 16);
                                    break;
                                }
                            }
                        }
                    }
                    break;
                case "list":
                    {
                        int i = 0;
                        foreach (var p in Config.Characters)
                        {
                            args.Player.SendInfoMessage($"id:{i} 名称：{p.Name}");
                            i++;
                        }
                    }
                    break;
            }
        }

        private void OnServerJoin(JoinEventArgs args)
        {
            if (TShock.Utils.GetActivePlayerCount() >= Config.Rooms.Count() * 2)
            {
                TShock.Players[args.Who].Disconnect("人数已满，无可用房间！");
                return;
            }
        }
    }
}
