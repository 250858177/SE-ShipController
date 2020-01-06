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
    partial class Program : MyGridProgram
    {
        Control con = null;
        Communication MSG = null;
        public static IMyTextSurface textSurface;
        string data;
        public static void print(string s)
        {
            textSurface?.WriteText(s + '\n', true);
        }
        public Program()
        {
            textSurface = Me.GetSurface(0);
            data = Me.CustomData;
            List<IMyRemoteControl> remoteControls = new List<IMyRemoteControl>();
            GridTerminalSystem.GetBlocksOfType<IMyRemoteControl>(remoteControls, g => g.CubeGrid == Me.CubeGrid);
            if (remoteControls.Count != 1)
            {
                Echo("必须有一个远程控制器");
                return;
            }
            if (data != null && data != "")
            {
                my = new Ship();
                my.decode(data);
            }
            con = new Control(remoteControls[0], GridTerminalSystem);
            MSG = new Communication(Me, IGC);
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        public void Save()
        {

        }
        Ship my = null;
        public void Main(string argument, UpdateType updateSource)
        {
            textSurface?.WriteText("", false);
            if (my == null)
            {
                my = MSG.init();
            }
            print($"初始化状态{my != null}");
        }
    }
}
