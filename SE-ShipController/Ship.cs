using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRage;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        public class Ship
        {
            //数据转发中心id
            public long controlId;
            //自己的id
            public long id;
            public string name;
            //类型
            public string type;
            //动作
            public string action;
            //当前位置
            public Vector3D location = new Vector3D();
            //生命计时器
            public int life;
            //功能扩展字段(兼容升级版Ship对象)
            public string extension;


            public string encode()
            {
                return $"{id}{Communication.idDelimiter}controlId{Communication.valueDelimiter}{controlId}{Communication.dataDelimiter}"
                    + $"type{Communication.valueDelimiter}{type}{Communication.dataDelimiter}"
                    + $"action{Communication.valueDelimiter}{action}{Communication.dataDelimiter}"
                    + $"location{Communication.valueDelimiter}{location.X},{location.Y},{location.Z}{Communication.dataDelimiter}"
                    + $"life{Communication.valueDelimiter}{life}{extension}";
            }
            public void decode(string code)
            {
                if (code == null || code == "") return;
                string[] strs = code.Split(Communication.idDelimiter);
                print(strs.Length + "");
                string[] data = strs[1].Split(Communication.dataDelimiter);
                foreach (string s in data)
                {
                    string[] kv = s.Split(Communication.valueDelimiter);
                    string[] xyz = null;
                    switch (kv[0])
                    {
                        case "controlId": if (kv[1] != "") controlId = long.Parse(kv[1]); break;
                        case "type": if (kv[1] != "") type = kv[1]; break;
                        case "action": if (kv[1] != "") action = kv[1]; break;
                        case "location": if (kv[1] != "") xyz = kv[1].Split(','); location = new Vector3D(double.Parse(xyz[0]), double.Parse(xyz[1]), double.Parse(xyz[2])); break;
                        case "life": if (kv[1] != "") life = int.Parse(kv[1]); break;
                        default: extension += $"{Communication.dataDelimiter}{kv[0]}{Communication.valueDelimiter}{kv[1]}"; break;
                    }
                }
            }
            public static string encodeList(List<Ship> ships)
            {
                string code = "";
                foreach (Ship s in ships)
                {
                    code += s.encode();
                }
                return code;
            }
            public static List<Ship> decodeList(string code)
            {
                List<Ship> ships = new List<Ship>();
                if (code == null || code == "") return ships;
                string[] strs = code.Split(Communication.objectDelimiter);
                foreach (string s in strs)
                {
                    print(s);
                    Ship ship = new Ship();
                    ship.decode(s);
                    ships.Add(ship);
                }
                return ships;
            }
        }
    }
}
