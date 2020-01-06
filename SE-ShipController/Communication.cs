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
        public class Communication
        {

            String listenerId = "23s1d5w4a12s1f3e85s4fs23d1g4e42sf1";




            //对象之间的分隔符
            public static char objectDelimiter = '\n';
            //id与数据之间的分隔符
            public static char idDelimiter = '#';
            //键值对分隔符
            public static char dataDelimiter = '&';
            //键与值分隔符
            public static char valueDelimiter = '^';

            IMyIntergridCommunicationSystem igc;
            IMyBroadcastListener broadcastListener;
            IMyUnicastListener unicastListener;
            IMyProgrammableBlock pb;
            public Communication(IMyProgrammableBlock pb, IMyIntergridCommunicationSystem igc)
            {
                this.igc = igc;
                broadcastListener = igc.RegisterBroadcastListener(listenerId);
                unicastListener = igc.UnicastListener;
                this.pb = pb;
            }
            public Ship init()
            {
                Ship ship = new Ship();
                ship.id = pb.EntityId;
                ship.type = "0";
                ship.action = "NULL";
                igc.SendBroadcastMessage<String>(listenerId, ship.encode());
                if (unicastListener.HasPendingMessage)
                {
                    MyIGCMessage message = unicastListener.AcceptMessage();
                    Ship s = new Ship();
                    s.decode(message.Data.ToString());
                    return s;
                }

                return null;
            }
            /*
             * 是否有待处理消息
             */
            public bool isActive
            {
                get { return unicastListener.HasPendingMessage; }
            }
            /*
             * 取出所有消息
             */
            public List<MyIGCMessage> getIGCMessages()
            {
                List<MyIGCMessage> messages = new List<MyIGCMessage>();
                while (unicastListener.HasPendingMessage)
                {
                    messages.Add(unicastListener.AcceptMessage());
                }
                return messages;
            }
            public void sendMessage(String message, long id)
            {
                igc.SendUnicastMessage<string>(id, "", message);
            }
        }
    }
}
