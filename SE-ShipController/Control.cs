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
        public class Control
        {
            IMyGridTerminalSystem myGridTerminal;
            public Control(IMyRemoteControl rc, IMyGridTerminalSystem myGridTerminal)
            {
                this.myGridTerminal = myGridTerminal;
                FirstTimeSetup(rc);
            }

            public void Save()
            { //SaveCode();
            }
            //矿船不是链接器的名字必须包含   Ejector
            double PrecisionMaxAngularVel = 1; //Maximum Precision Ship Angular Velocity（之前是0.6）
            double RotationalSensitvity = 1; //Gain Applied To Gyros
            double l_ratio = 2.5;
            double s_ratio = 1.4;
            bool ThrustCountOverride = false; //Togglable Override On Thrust Count

            double forward = 40;
            double up = 5;

            //无人机专属lcd
            IMyTextPanel testLcd;
            Vector3D target = new Vector3D();
            int time;
            //0-工蚁
            //1-兵蚁
            int type = 999;
            bool isNew = true;
            bool isAutoPilot = false;
            string name;
            string tips;
            //航线开关
            bool linkLimit = false;

            Vector3D vector3DNull = new Vector3D();

            public void print(string message)
            {
                testLcd?.WritePublicText($"{message}\n", true);
            }
            List<IMySmallGatlingGun> mySmallGatlings = new List<IMySmallGatlingGun>();
            List<IMySmallMissileLauncher> mySmallMissiles = new List<IMySmallMissileLauncher>();
            List<IMyLargeTurretBase> myLargeTurrets = new List<IMyLargeTurretBase>();
            List<IMyTerminalBlock> terminalBlocks = new List<IMyTerminalBlock>();
            Vector3D StoredAsteroidLoc = new Vector3D();
            Vector3D StoredAsteroidCentre = new Vector3D();
            double StoredAsteroidDiameter = new double();
            bool AbleToMine = new bool();
            void moveTo(Vector3D ROID_START, Vector3D ROID_END)
            {
                //Setup Of Common Variables
                Vector3D DronePosition = RC.GetPosition();
                Vector3D Drone_To_Target = Vector3D.Normalize(ROID_END - DronePosition);
                //Generates XYZ Vectors
                Vector3D X_ADD = Vector3D.Normalize(ROID_END - ROID_START);//Characteristic 'Forward' vector
                Vector3D Y_ADD = Vector3D.CalculatePerpendicularVector(X_ADD); //Characteristic 'Left' vector
                Vector3D Z_ADD = Vector3D.Cross(X_ADD, Y_ADD); //Characteristic 'Up' vector

                //设置起点
                Vector3D CurrentVectorStart = ROID_START;
                //设置终点
                Vector3D CurrentVectorEnd = ROID_END; //Accounts for small input
                double dis = Vector3D.Distance(CurrentVectorStart, DronePosition);
                //Inputs To Autopilot Function
                double RollReqt = (float)(0.6 * (Vector3D.Dot(Z_ADD, RC.WorldMatrix.Down)));
                GyroTurn6(X_ADD * 999999999999999999, RotationalSensitvity, GYRO, RC, RollReqt, PrecisionMaxAngularVel);
                if (dis > 20 && isAutoPilot)
                {
                    RC_Manager(CurrentVectorStart, RC, false);
                    if (dis < 30)
                    {
                        isAutoPilot = false;
                    }
                    print("方式：自动驾驶");
                }
                else
                {
                    print("方式：程序控制");
                    Vector_Thrust_Manager(CurrentVectorStart, CurrentVectorEnd, RC.GetPosition(), 1, 0.5, RC);
                }
            }
            void towards(Vector3D ROID_START, Vector3D ROID_CENTRE, double ROID_DIAMETER, double SHIPSIZE, bool Reset)
            {
                //Setup Of Common Variables
                Vector3D DronePosition = RC.GetPosition();
                Vector3D Drone_To_Target = Vector3D.Normalize(ROID_CENTRE - DronePosition);
                //Generates XYZ Vectors
                Vector3D X_ADD = Vector3D.Normalize(ROID_CENTRE - ROID_START);//Characteristic 'Forward' vector
                Vector3D Y_ADD = Vector3D.CalculatePerpendicularVector(X_ADD); //Characteristic 'Left' vector
                Vector3D Z_ADD = Vector3D.Cross(X_ADD, Y_ADD); //Characteristic 'Up' vector
                //Generates Array Of Starting Vectors
                int Steps = (int)((ROID_DIAMETER * 0.3) / SHIPSIZE); //How many horizontal passes of the ship are required to eat the roid
                double StepSize = SHIPSIZE;  //How big are those passes

                //设置起点
                Vector3D CurrentVectorStart = new Vector3D();
                //设置终点
                Vector3D CurrentVectorEnd = CurrentVectorStart + X_ADD * (((ROID_CENTRE - ROID_START).Length() - ROID_DIAMETER / 2) + ROID_DIAMETER * 0.8); //Accounts for small input
                double dis = Vector3D.Distance(CurrentVectorStart, DronePosition);
                //Inputs To Autopilot Function
                double RollReqt = (float)(0.6 * (Vector3D.Dot(Z_ADD, RC.WorldMatrix.Down)));
                GyroTurn6(X_ADD * 999999999999999999, RotationalSensitvity, GYRO, RC, RollReqt, PrecisionMaxAngularVel);
                if (dis > 20 && isAutoPilot)
                {
                    RC_Manager(CurrentVectorStart, RC, false);
                    if (dis < 30)
                    {
                        isAutoPilot = false;
                    }
                    print("方式：自动驾驶");
                }
                else
                {
                    print("方式：程序控制");
                    Vector_Thrust_Manager(CurrentVectorStart, CurrentVectorEnd, RC.GetPosition(), 1, 0.5, RC);
                }
            }
            void start(String MININGSTATUS)
            {
                var DOCKLIST = new List<Vector3D>();
                DOCKLIST.Add(DockPos3);
                DOCKLIST.Add(DockPos2);
                DOCKLIST.Add(DockPos1);
                //If Full Go And Free Dock (stage 1)
                if (MININGSTATUS == "T")
                {
                    DockingIterator(true, DOCKLIST, GYRO, CONNECTOR, RC);
                    print("状态: 返航");
                }
                //If Empty And Docked And Want To Go Mine, Undock (stage 2)
                else if (MININGSTATUS == "S")
                {
                    DockingIterator(false, DOCKLIST, GYRO, CONNECTOR, RC);
                    print("状态: 起航");
                }
            }
            /*
             * 停靠与起航
             */
            Vector3D dockPos1 = new Vector3D(); //Conncetor Location
            Vector3D dockPos2 = new Vector3D(); //Straight Up Location
            Vector3D dockPos3 = new Vector3D(); //Straight Up And Forward Location
            private void DockingIterator(bool Docking, List<Vector3D> COORDINATES, IMyGyro GYRO, IMyShipConnector CONNECTOR, IMyRemoteControl RC)
            {
                if (COORDINATES.Count < 3) { return; }

                int TargetID = 0;
                int CurrentID = 0;
                int iter_er = 0;
                if (Docking == true)
                { TargetID = 1; CurrentID = 0; iter_er = +1; }
                if (Docking == false)
                { TargetID = 0; CurrentID = 1; iter_er = -1; }

                if (Docking == true) { CONNECTOR.Connect(); }
                if (Docking == true && CONNECTOR.IsWorking == false) { CONNECTOR.Enabled = true; }
                if (Docking == false && CONNECTOR.IsWorking == true) { CONNECTOR.Disconnect(); CONNECTOR.Enabled = true; }
                if (CONNECTOR.Status == MyShipConnectorStatus.Connected && Docking == true)
                {
                    for (int j = 0; j < CAF2_THRUST.Count; j++)
                    { (CAF2_THRUST[j] as IMyThrust).Enabled = false; }
                    GYRO.GyroOverride = false;
                    return;
                }
                Vector3D RollOrienter = Vector3D.Normalize(COORDINATES[COORDINATES.Count - 1] - COORDINATES[COORDINATES.Count - 2]);
                Vector3D Connector_Direction = -1 * ReturnConnectorDirection(CONNECTOR, RC);
                double RollReqt = (float)(0.6 * (Vector3D.Dot(RollOrienter, Connector_Direction)));
                //垂直运动在码头
                if (COORD_ID == COORDINATES.Count - 1)
                {
                    Vector3D DockingHeading = Vector3D.Normalize(COORDINATES[COORDINATES.Count - 3] - COORDINATES[COORDINATES.Count - 2]) * 9000000; //Heading
                    GyroTurn6(DockingHeading, RotationalSensitvity, GYRO, RC, RollReqt, PrecisionMaxAngularVel); //Turn to heading
                    if (Vector3D.Dot(RC.WorldMatrix.Forward, Vector3D.Normalize(DockingHeading)) > 0.98) //Error check for small rotational velocity
                    { Vector_Thrust_Manager(COORDINATES[COORD_ID - TargetID], COORDINATES[COORD_ID - CurrentID], CONNECTOR.GetPosition(), 5, 0.7, RC); }  //Thrusts to point
                }

                //在码头上的最后/第一个外部Coord
                else if (COORD_ID == 0)
                {
                    print($"启动自动驾驶\n距离目标:{Vector3D.Distance(COORDINATES[0], RC.GetPosition())}");
                    RC_Manager(COORDINATES[0], RC, false);
                }

                //水平和迭代语句
                else
                {
                    var HEADING = Vector3D.Normalize(COORDINATES[COORD_ID - CurrentID] - COORDINATES[COORD_ID - TargetID]) * 9000000;
                    Vector_Thrust_Manager(COORDINATES[COORD_ID - TargetID], COORDINATES[COORD_ID - CurrentID], CONNECTOR.GetPosition(), 8, 1, RC); //Runs docking sequence
                    GyroTurn6(HEADING, RotationalSensitvity, GYRO, RC, RollReqt, PrecisionMaxAngularVel);
                }

                //逻辑检查和迭代
                if (Docking == false && COORD_ID == 0) { }
                else if ((CONNECTOR.GetPosition() - COORDINATES[COORD_ID - CurrentID]).Length() < 1 || ((RC.GetPosition() - COORDINATES[COORD_ID - CurrentID]).Length() < 10 && COORD_ID == 0))
                {
                    COORD_ID = COORD_ID + iter_er;
                    if (COORD_ID == COORDINATES.Count)
                    { COORD_ID = COORDINATES.Count - 1; }
                    if (COORD_ID < 0)
                    { COORD_ID = 0; }
                }
            }
            //----------==--------=------------=-----------=---------------=------------=-------==--------=------------=-----------=----------


            //Standardised First Time Setup
            /*====================================================================================================================
                    Function: FIRST_TIME_SETUP
                    ---------------------------------------
                    function will: Initiates Systems and initiasing Readouts to LCD
                    Performance Cost:
                   //======================================================================================================================*/
            //SUBCATEGORY STORED BLOCKS
            IMyRemoteControl RC;
            IMyShipConnector CONNECTOR;
            List<IMyLargeTurretBase> DIRECTORS = new List<IMyLargeTurretBase>();
            IMyRadioAntenna RADIO;
            IMyGyro GYRO;
            List<IMyTerminalBlock> CONTROLLERS = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> Cargo = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> DIRECTIONAL_FIRE = new List<IMyTerminalBlock>();  //Directional ship weaponry
            List<IMyOreDetector> oreDetectors = new List<IMyOreDetector>();
            List<IMyRadioAntenna> oreA = new List<IMyRadioAntenna>();
            List<IMyCameraBlock> cameraBlocks = new List<IMyCameraBlock>();
            List<IMyBatteryBlock> batteryBlocks = new List<IMyBatteryBlock>();
            List<IMyRadioAntenna> radioAntennas = new List<IMyRadioAntenna>();
            List<IMyShipDrill> SHIP_DRILLS = new List<IMyShipDrill>();
            IMyCameraBlock cameraBlock;
            MyDetectedEntityInfo asteroid;
            private void FirstTimeSetup(IMyRemoteControl rc)
            {
                try
                {
                    RC1 = rc;
                    myGridTerminal.GetBlocksOfType<IMyOreDetector>(oreDetectors, b => b.CubeGrid == RC1.CubeGrid);
                    myGridTerminal.GetBlocksOfType<IMyCameraBlock>(cameraBlocks, b => b.CubeGrid == RC1.CubeGrid);
                    myGridTerminal.GetBlocksOfType<IMyBatteryBlock>(batteryBlocks, b => b.CubeGrid == RC1.CubeGrid);
                    myGridTerminal.GetBlocksOfType<IMyRadioAntenna>(RadioAntennas, b => b.CubeGrid == RC1.CubeGrid);
                    if (cameraBlocks.Count > 0)
                    {
                        cameraBlock = cameraBlocks[0];
                    }
                    myGridTerminal.GetBlocksOfType(mySmallMissiles, b => b.CubeGrid == RC1.CubeGrid);
                    myGridTerminal.GetBlocksOfType(mySmallGatlings, b => b.CubeGrid == RC1.CubeGrid);
                    myGridTerminal.GetBlocksOfType(myLargeTurrets, b => b.CubeGrid == RC1.CubeGrid);
                }
                catch (Exception e)
                {
                    print(e.Message);
                }
                try
                {
                    List<IMyTerminalBlock> TEMP_CON = new List<IMyTerminalBlock>();
                    myGridTerminal.GetBlocksOfType<IMyShipConnector>(TEMP_CON, b => b.CubeGrid == RC1.CubeGrid && b.CustomName.Contains("Ejector") == false);
                    CONNECTOR1 = TEMP_CON[0] as IMyShipConnector;
                }
                catch { }
                try
                {
                    List<IMyTerminalBlock> TEMP_GYRO = new List<IMyTerminalBlock>();
                    myGridTerminal.GetBlocksOfType<IMyGyro>(TEMP_GYRO, b => b.CubeGrid == RC1.CubeGrid);
                    GYRO = TEMP_GYRO[0] as IMyGyro;
                }
                catch { }

                //Initialising Dedicated Cargo
                try
                {
                    myGridTerminal.GetBlocksOfType<IMyCargoContainer>(Cargo, b => b.CubeGrid == RC1.CubeGrid);
                }
                catch
                { }

                //Gathers Antennae
                try
                {
                    List<IMyTerminalBlock> TEMP = new List<IMyTerminalBlock>();
                    myGridTerminal.GetBlocksOfType<IMyRadioAntenna>(TEMP, b => b.CubeGrid == RC1.CubeGrid);
                    RADIO = TEMP[0] as IMyRadioAntenna;
                    RADIO.SetValue<long>("PBList", RC1.EntityId);
                    RADIO.EnableBroadcasting = true;
                    RADIO.Enabled = true;
                }
                catch { }

                //GathersControllers
                try
                {
                    myGridTerminal.GetBlocksOfType<IMyShipController>(CONTROLLERS, b => b.CubeGrid == RC1.CubeGrid);

                }
                catch { }

                //Gathers Director Turret
                try
                {
                    myGridTerminal.GetBlocksOfType<IMyLargeTurretBase>(DIRECTORS, b => b.CubeGrid == RC1.CubeGrid);
                }
                catch { }

                //Gathers Drills
                try
                {
                    myGridTerminal.GetBlocksOfType<IMyShipDrill>(SHIP_DRILLS, b => b.CubeGrid == RC1.CubeGrid);
                }
                catch { }

                //Gathers Directional Weaponry
                try
                {
                    myGridTerminal.GetBlocksOfType<IMyUserControllableGun>(DIRECTIONAL_FIRE,
                        (block =>
                        (
                            block.GetType().Name == "MySmallMissileLauncher" ||
                            block.GetType().Name == "MySmallGatlingGun" ||
                            block.GetType().Name == "MySmallMissileLauncherReload"
                        )
                        && block.CubeGrid == RC1.CubeGrid)
                    ); //Collects the directional weaponry (in a group)
                }
                catch { }
                //Runs Thruster Setup
                try
                {
                    CollectAndFire2(new Vector3D(), 0, 0, RC1.GetPosition(), RC1);
                    for (int j = 0; j < CAF2_THRUST.Count; j++)
                    { CAF2_THRUST[j].SetValue<float>("Override", 0.0f); CAF2_THRUST[j].ApplyAction("OnOff_On"); }
                }
                catch { }
            }
            IMyShipConnector otherTempConnector;

            void Auto_DockpointDetect()
            {
                if (OtherTempConnector != null)
                {
                    DockPos1 = OtherTempConnector.GetPosition() + OtherTempConnector.WorldMatrix.Forward * (1.5);
                    DockPos2 = OtherTempConnector.GetPosition() + OtherTempConnector.WorldMatrix.Forward * up;
                    //DockPos3 = DockPos2 + RC1.WorldMatrix.Forward * forward;
                    //DockPos3 = DockPos2 + OtherTempConnector.WorldMatrix.Up * forward;
                    string name = OtherTempConnector.CustomName;
                    if (name.Contains("Forward"))
                    {
                        DockPos3 = DockPos2 + RC1.WorldMatrix.Forward * forward;
                    }
                    else if (name.Contains("Backward"))
                    {
                        DockPos3 = DockPos2 + RC1.WorldMatrix.Backward * forward;
                    }
                    else if (name.Contains("Left"))
                    {
                        DockPos3 = DockPos2 + RC1.WorldMatrix.Left * forward;
                    }
                    else if (name.Contains("Right"))
                    {
                        DockPos3 = DockPos2 + RC1.WorldMatrix.Right * forward;
                    }
                    else if (name.Contains("Up"))
                    {
                        DockPos3 = DockPos2 + RC1.WorldMatrix.Up * forward;
                    }
                    else if (name.Contains("Down"))
                    {
                        DockPos3 = DockPos2 + RC1.WorldMatrix.Down * forward;
                    }
                    else
                    {
                        DockPos3 = DockPos2 + RC1.WorldMatrix.Forward * forward;
                    }
                }

            }

            //Primary Generic Functions
            //==========================

            //Use For General Drone Flying:
            void RC_Manager(Vector3D TARGET, IMyRemoteControl RC, bool TURN_ONLY)
            {
                //Uses Rotation Control To Handle Max Rotational Velocity
                //---------------------------------------------------------
                if (RC.GetShipVelocities().AngularVelocity.AbsMax() > PrecisionMaxAngularVel)
                { print("转动速度放缓"); RC.SetAutoPilotEnabled(false); return; }
                //Setup Of Common Variables
                //--------------------------------------------
                Vector3D DronePosition = RC.GetPosition();
                Vector3D Drone_To_Target = Vector3D.Normalize(TARGET - DronePosition);
                //Override Direction Detection
                //-------------------------------
                double To_Target_Angle = Vector3D.Dot(Vector3D.Normalize(RC.GetShipVelocities().LinearVelocity), Drone_To_Target);
                double Ship_Velocity = RC.GetShipVelocities().LinearVelocity.Length();
                //Turn Only: (Will drift ship automatically)
                //--------------------------------------------
                /*List<MyWaypointInfo> way = new List<MyWaypointInfo>();
                        RC.GetWaypointInfo(way);
                        if (way.Count>0)
                        {
                            if (way[0].Coords!= TARGET)
                            {
                                //RC.ApplyAction("AutoPilot_Off");
                                //RC.ClearWaypoints();
                            }
                        }*/

                if (TURN_ONLY)
                {
                    //if (way.Count <1)
                    {
                        RC.ClearWaypoints();
                        GYRO.GyroOverride = false;
                        RC.AddWaypoint(TARGET, "母船起点");
                        RC.FlightMode = FlightMode.OneWay;
                        RC.Direction = Base6Directions.Direction.Forward;
                        RC.ApplyAction("AutoPilot_On");
                        RC.ApplyAction("CollisionAvoidance_Off");
                        RC.ControlThrusters = false;
                    }
                    return;
                }
                //Drift Cancellation Enabled:
                //-----------------------------
                if (To_Target_Angle < 0.4 && Ship_Velocity > 3)
                {
                    //Aim Gyro To Reflected Vector
                    Vector3D DRIFT_VECTOR = Vector3D.Normalize(RC.GetShipVelocities().LinearVelocity);
                    Vector3D REFLECTED_DRIFT_VECTOR = -1 * (Vector3D.Normalize(Vector3D.Reflect(DRIFT_VECTOR, Drone_To_Target)));
                    Vector3D AIMPINGPOS = (-1 * DRIFT_VECTOR * 500) + DronePosition;

                    //if (way.Count < 1 )
                    {
                        RC.ClearWaypoints();
                        GYRO.GyroOverride = false;
                        RC.AddWaypoint(AIMPINGPOS, "AIMPINGPOS");
                        RC.SpeedLimit = 100;
                        RC.FlightMode = FlightMode.OneWay;
                        RC.Direction = Base6Directions.Direction.Forward;
                        RC.ApplyAction("AutoPilot_On");
                        RC.ApplyAction("CollisionAvoidance_Off");
                    }
                }

                //System Standard Operation:
                //---------------------------
                else
                {
                    //Assign To RC, Clear And Refresh Command
                    List<ITerminalAction> action = new List<ITerminalAction>();
                    RC.GetActions(action);
                    RC.ControlThrusters = true;
                    RC.ClearWaypoints();
                    GYRO.GyroOverride = false;
                    RC.AddWaypoint(TARGET, "目标");
                    RC.SpeedLimit = 100;
                    RC.FlightMode = FlightMode.OneWay;
                    RC.Direction = Base6Directions.Direction.Forward;
                    RC.ApplyAction("AutoPilot_On");
                    RC.ApplyAction("DockingMode_Off");
                    RC.ApplyAction("CollisionAvoidance_On");
                }
            }

            //Use For Precise Turning (docking, mining, attacking)
            //----------==--------=------------=-----------=---------------=------------=-----=-----*/
            void GyroTurn6(Vector3D TARGET, double GAIN, IMyGyro GYRO, IMyRemoteControl REF_RC, double ROLLANGLE, double MAXANGULARVELOCITY)
            {
                //确保自动驾驶仪没有功能
                REF_RC.SetAutoPilotEnabled(false);
                //检测前、上 & Pos
                Vector3D ShipForward = REF_RC.WorldMatrix.Forward;
                Vector3D ShipUp = REF_RC.WorldMatrix.Up;
                Vector3D ShipPos = REF_RC.GetPosition();

                //创建和使用逆Quatinion
                Quaternion Quat_Two = Quaternion.CreateFromForwardUp(ShipForward, ShipUp);
                var InvQuat = Quaternion.Inverse(Quat_Two);
                Vector3D DirectionVector = Vector3D.Normalize(TARGET - ShipPos); //RealWorld Target Vector
                Vector3D RCReferenceFrameVector = Vector3D.Transform(DirectionVector, InvQuat); //Target Vector In Terms Of RC Block

                //转换为局部方位和高度
                double ShipForwardAzimuth = 0; double ShipForwardElevation = 0;
                Vector3D.GetAzimuthAndElevation(RCReferenceFrameVector, out ShipForwardAzimuth, out ShipForwardElevation);

                //Does Some Rotations To Provide For any Gyro-Orientation做一些旋转来提供任何旋转方向
                var RC_Matrix = REF_RC.WorldMatrix.GetOrientation();
                var Vector = Vector3.Transform((new Vector3D(ShipForwardElevation, ShipForwardAzimuth, ROLLANGLE)), RC_Matrix); //Converts To World转换为世界
                var TRANS_VECT = Vector3.Transform(Vector, Matrix.Transpose(GYRO.WorldMatrix.GetOrientation()));  //Converts To Gyro Local转换为陀螺仪方位

                //Applies To Scenario适用于场景
                GYRO.Pitch = (float)MathHelper.Clamp((-TRANS_VECT.X * GAIN), -MAXANGULARVELOCITY, MAXANGULARVELOCITY);
                GYRO.Yaw = (float)MathHelper.Clamp(((-TRANS_VECT.Y) * GAIN), -MAXANGULARVELOCITY, MAXANGULARVELOCITY);
                GYRO.Roll = (float)MathHelper.Clamp(((-TRANS_VECT.Z) * GAIN), -MAXANGULARVELOCITY, MAXANGULARVELOCITY);
                GYRO.GyroOverride = true;

                //GYRO.SetValueFloat("Pitch", (float)((TRANS_VECT.X) * GAIN));
                //GYRO.SetValueFloat("Yaw", (float)((-TRANS_VECT.Y) * GAIN));
                //GYRO.SetValueFloat("Roll", (float)((-TRANS_VECT.Z) * GAIN));
            }

            //Use For Precise Thrusting (docking, mining, attacking)
            /*=======================================================================================
                      Function: COLLECT_AND_FIRE
                      ---------------------------------------
                      function will: Collect thrust pointing in a input direction and fire said thrust
                                     towards that point, remember to deset
                    //----------==--------=------------=-----------=---------------=------------=-----=-----*/
            class Thrust_info                   //Basic Information For Axial Thrust
            {
                public double PositiveMaxForce;
                public double NegativeMaxForce;
                public List<IMyThrust> PositiveThrusters;
                public List<IMyThrust> NegativeThrusters;
                public double VCF;
                public Thrust_info(Vector3D DIRECT, IMyGridTerminalSystem GTS, IMyCubeGrid MEGRID)
                {
                    PositiveThrusters = new List<IMyThrust>(); NegativeThrusters = new List<IMyThrust>();
                    List<IMyTerminalBlock> TEMP_RC = new List<IMyTerminalBlock>();
                    GTS.GetBlocksOfType<IMyThrust>(PositiveThrusters, block => Vector3D.Dot(-1 * block.WorldMatrix.Forward, DIRECT) > 0.7 && block.CubeGrid == MEGRID);
                    GTS.GetBlocksOfType<IMyThrust>(NegativeThrusters, block => Vector3D.Dot(block.WorldMatrix.Forward, DIRECT) > 0.7 && block.CubeGrid == MEGRID);
                    double POWER_COUNT = 0;
                    foreach (var item in PositiveThrusters)
                    { POWER_COUNT = POWER_COUNT + item.MaxEffectiveThrust; }
                    PositiveMaxForce = POWER_COUNT;
                    POWER_COUNT = 0;
                    foreach (var item in NegativeThrusters)
                    { POWER_COUNT = POWER_COUNT + item.MaxEffectiveThrust; }
                    NegativeMaxForce = POWER_COUNT;
                }
            }
            Thrust_info CAF2_FORWARD;
            Thrust_info CAF2_UP;
            Thrust_info CAF2_RIGHT;
            List<Thrust_info> CAFTHI = new List<Thrust_info>();

            List<IMyTerminalBlock> CAF2_THRUST = new List<IMyTerminalBlock>();
            bool C_A_F_HASRUN = false;
            double CAF2_BRAKING_COUNT = 99999999;

            double CAF_SHIP_DECELLERATION;                        //Outputs current decelleration
            double CAF_STOPPING_DIST;                             //Outputs current stopping distance
            double CAF_DIST_TO_TARGET;                            //Outputs distance to target

            void CollectAndFire2(Vector3D INPUT_POINT, double INPUT_VELOCITY, double INPUT_MAX_VELOCITY, Vector3D REFPOS, IMyRemoteControl RC)
            {
                //Function Initialisation
                //--------------------------------------------------------------------
                if (C_A_F_HASRUN == false)
                {
                    //Initialise Classes And Basic System
                    CAF2_FORWARD = new Thrust_info(RC.WorldMatrix.Forward, myGridTerminal, RC.CubeGrid);
                    CAF2_UP = new Thrust_info(RC.WorldMatrix.Up, myGridTerminal, RC.CubeGrid);
                    CAF2_RIGHT = new Thrust_info(RC.WorldMatrix.Right, myGridTerminal, RC.CubeGrid);
                    CAFTHI = new List<Thrust_info>() { CAF2_FORWARD, CAF2_UP, CAF2_RIGHT };
                    myGridTerminal.GetBlocksOfType<IMyThrust>(CAF2_THRUST, block => block.CubeGrid == RC.CubeGrid);
                    C_A_F_HASRUN = true;

                    //Initialises Braking Component
                    foreach (var item in CAFTHI)
                    {
                        CAF2_BRAKING_COUNT = (item.PositiveMaxForce < CAF2_BRAKING_COUNT) ? item.PositiveMaxForce : CAF2_BRAKING_COUNT;
                        CAF2_BRAKING_COUNT = (item.NegativeMaxForce < CAF2_BRAKING_COUNT) ? item.PositiveMaxForce : CAF2_BRAKING_COUNT;
                    }
                }
                //Generating Maths To Point and decelleration information etc.
                //--------------------------------------------------------------------
                double SHIPMASS = Convert.ToDouble(RC.CalculateShipMass().PhysicalMass);
                Vector3D INPUT_VECTOR = Vector3D.Normalize(INPUT_POINT - REFPOS);
                double VELOCITY = RC.GetShipSpeed();
                CAF_DIST_TO_TARGET = (REFPOS - INPUT_POINT).Length();
                CAF_SHIP_DECELLERATION = 0.75 * (CAF2_BRAKING_COUNT / SHIPMASS);
                CAF_STOPPING_DIST = (((VELOCITY * VELOCITY) - (INPUT_VELOCITY * INPUT_VELOCITY))) / (2 * CAF_SHIP_DECELLERATION);

                //If Within Stopping Distance Halts Programme
                //--------------------------------------------
                if (!(CAF_DIST_TO_TARGET > (CAF_STOPPING_DIST + 0.25)) || CAF_DIST_TO_TARGET < 0.25 || VELOCITY > INPUT_MAX_VELOCITY)
                { foreach (var thruster in CAF2_THRUST) { (thruster as IMyThrust).ThrustOverride = 0; } return; }
                //dev notes, this is the most major source of discontinuity between theorised system response

                //Reflects Vector To Cancel Orbiting
                //------------------------------------
                Vector3D DRIFT_VECTOR = Vector3D.Normalize(RC.GetShipVelocities().LinearVelocity + RC.WorldMatrix.Forward * 0.00001);
                Vector3D R_DRIFT_VECTOR = -1 * Vector3D.Normalize(Vector3D.Reflect(DRIFT_VECTOR, INPUT_VECTOR));
                R_DRIFT_VECTOR = ((Vector3D.Dot(R_DRIFT_VECTOR, INPUT_VECTOR) < -0.3)) ? 0 * R_DRIFT_VECTOR : R_DRIFT_VECTOR;
                INPUT_VECTOR = Vector3D.Normalize((4 * R_DRIFT_VECTOR) + INPUT_VECTOR);

                //Components Of Input Vector In FUR Axis
                //----------------------------------------
                double F_COMP_IN = Vector_Projection(INPUT_VECTOR, RC.WorldMatrix.Forward);
                double U_COMP_IN = Vector_Projection(INPUT_VECTOR, RC.WorldMatrix.Up);
                double R_COMP_IN = Vector_Projection(INPUT_VECTOR, RC.WorldMatrix.Right);

                //Calculate MAX Allowable in Each Axis & Length
                //-----------------------------------------------
                double F_COMP_MAX = (F_COMP_IN > 0) ? CAF2_FORWARD.PositiveMaxForce : -1 * CAF2_FORWARD.NegativeMaxForce;
                double U_COMP_MAX = (U_COMP_IN > 0) ? CAF2_UP.PositiveMaxForce : -1 * CAF2_UP.NegativeMaxForce;
                double R_COMP_MAX = (R_COMP_IN > 0) ? CAF2_RIGHT.PositiveMaxForce : -1 * CAF2_RIGHT.NegativeMaxForce;
                double MAX_FORCE = Math.Sqrt(F_COMP_MAX * F_COMP_MAX + U_COMP_MAX * U_COMP_MAX + R_COMP_MAX * R_COMP_MAX);

                //Apply Length to Input Components and Calculates Smallest Multiplier
                //--------------------------------------------------------------------
                double F_COMP_PROJ = F_COMP_IN * MAX_FORCE;
                double U_COMP_PROJ = U_COMP_IN * MAX_FORCE;
                double R_COMP_PROJ = R_COMP_IN * MAX_FORCE;
                double MULTIPLIER = 1;
                MULTIPLIER = (F_COMP_MAX / F_COMP_PROJ < MULTIPLIER) ? F_COMP_MAX / F_COMP_PROJ : MULTIPLIER;
                MULTIPLIER = (U_COMP_MAX / U_COMP_PROJ < MULTIPLIER) ? U_COMP_MAX / U_COMP_PROJ : MULTIPLIER;
                MULTIPLIER = (R_COMP_MAX / R_COMP_PROJ < MULTIPLIER) ? R_COMP_MAX / R_COMP_PROJ : MULTIPLIER;

                //Calculate Multiplied Components
                //---------------------------------
                CAF2_FORWARD.VCF = ((F_COMP_PROJ * MULTIPLIER) / F_COMP_MAX) * Math.Sign(F_COMP_MAX);
                CAF2_UP.VCF = ((U_COMP_PROJ * MULTIPLIER) / U_COMP_MAX) * Math.Sign(U_COMP_MAX);
                CAF2_RIGHT.VCF = ((R_COMP_PROJ * MULTIPLIER) / R_COMP_MAX) * Math.Sign(R_COMP_MAX);

                //Runs System Thrust Application
                //----------------------------------
                Dictionary<IMyThrust, float> THRUSTVALUES = new Dictionary<IMyThrust, float>();
                foreach (var thruster in CAF2_THRUST) { THRUSTVALUES.Add((thruster as IMyThrust), 0f); }

                foreach (var THRUSTSYSTM in CAFTHI)
                {
                    List<IMyThrust> POSTHRUST = THRUSTSYSTM.PositiveThrusters;
                    List<IMyThrust> NEGTHRUST = THRUSTSYSTM.NegativeThrusters;
                    if (THRUSTSYSTM.VCF < 0) { POSTHRUST = THRUSTSYSTM.NegativeThrusters; NEGTHRUST = THRUSTSYSTM.PositiveThrusters; }
                    foreach (var thruster in POSTHRUST) { THRUSTVALUES[thruster as IMyThrust] = (float)(Math.Abs(THRUSTSYSTM.VCF)) * (thruster as IMyThrust).MaxThrust; }
                    foreach (var thruster in NEGTHRUST) { THRUSTVALUES[thruster as IMyThrust] = 1; }//(float)0.01001;}
                    foreach (var thruster in THRUSTVALUES) { thruster.Key.ThrustOverride = thruster.Value; } //thruster.Key.ThrustOverride = thruster.Value;
                }
            }
            //----------==--------=------------=-----------=---------------=------------=-------==--------=-----

            //Used For Precise Thrusting Along A Vector (docking, mining, attacking)
            /*====================================================================================================================================
                    Secondary Function: PRECISION MANAGER
                    -----------------------------------------------------
                    Function will: Given two inputs manage vector-based thrusting
                    Inputs: DIRECTION, BLOCK
                    //-=--------------=-----------=-----------=-------------------=-------------------=----------------------=----------------------------*/
            void Vector_Thrust_Manager(Vector3D PM_START, Vector3D PM_TARGET, Vector3D PM_REF, double PR_MAX_VELOCITY, double PREC, IMyRemoteControl RC)
            {
                Vector3D VECTOR = Vector3D.Normalize(PM_START - PM_TARGET);
                Vector3D GOTOPOINT = PM_TARGET + VECTOR * MathHelper.Clamp((((PM_REF - PM_TARGET).Length() - 0.2)), 0, (PM_START - PM_TARGET).Length());
                double DIST_TO_POINT = MathHelper.Clamp((GOTOPOINT - PM_REF).Length(), 0, (PM_START - PM_TARGET).Length());

                if (DIST_TO_POINT > PREC)
                { CollectAndFire2(GOTOPOINT, 0, PR_MAX_VELOCITY * 2, PM_REF, RC); }
                else
                { CollectAndFire2(PM_TARGET, 0, PR_MAX_VELOCITY, PM_REF, RC); }
            }
            //----------==--------=------------=-----------=---------------=------------=-------==--------=------------=-----------=----------
            double Vector_Projection(Vector3D IN, Vector3D Axis)
            {
                double OUT = 0;
                OUT = Vector3D.Dot(IN, Axis) / IN.Length();
                if (OUT + "" == "NaN")
                { OUT = 0; }
                return OUT;
            }
            Vector3D ReturnConnectorDirection(IMyShipConnector CONNECTOR, IMyRemoteControl RC)
            {
                if (CONNECTOR.Orientation.Forward == RC.Orientation.TransformDirection(Base6Directions.Direction.Down))
                {
                    return RC.WorldMatrix.Left;
                }  //Connector is the bottom of ship
                if (CONNECTOR.Orientation.Forward == RC.Orientation.TransformDirection(Base6Directions.Direction.Up))
                {
                    return RC.WorldMatrix.Right;
                }  //Connector is on the top of the ship
                if (CONNECTOR.Orientation.Forward == RC.Orientation.TransformDirection(Base6Directions.Direction.Right))
                {
                    return RC.WorldMatrix.Up;
                }  //Connector is on the left of the ship
                if (CONNECTOR.Orientation.Forward == RC.Orientation.TransformDirection(Base6Directions.Direction.Left))
                {
                    return RC.WorldMatrix.Down;
                }  //Connector is on the right of the ship

                return RC.WorldMatrix.Down;
            }

            static List<string> SavableStrings = new List<string>();
            static List<int> SavableInts = new List<int>();
            static List<Vector3D> SavableVectors = new List<Vector3D>();
            static List<double> SavableDoubles = new List<double>();
            static List<bool> SavableBools = new List<bool>();

            public IMyTextPanel TestLcd
            {
                get
                {
                    return testLcd;
                }

                set
                {
                    testLcd = value;
                }
            }

            public IMyRemoteControl RC1
            {
                get
                {
                    return RC;
                }

                set
                {
                    RC = value;
                }
            }

            public Vector3D Target
            {
                get
                {
                    return target;
                }

                set
                {
                    target = value;
                }
            }

            public MyDetectedEntityInfo Asteroid
            {
                get
                {
                    return asteroid;
                }

                set
                {
                    asteroid = value;
                }
            }

            public int Type
            {
                get
                {
                    return type;
                }

                set
                {
                    type = value;
                }
            }

            public List<IMyLargeTurretBase> MyLargeTurrets
            {
                get
                {
                    return myLargeTurrets;
                }

                set
                {
                    myLargeTurrets = value;
                }
            }

            public List<IMySmallMissileLauncher> MySmallMissiles
            {
                get
                {
                    return mySmallMissiles;
                }

                set
                {
                    mySmallMissiles = value;
                }
            }

            public List<IMySmallGatlingGun> MySmallGatlings
            {
                get
                {
                    return mySmallGatlings;
                }

                set
                {
                    mySmallGatlings = value;
                }
            }

            public IMyShipConnector OtherTempConnector
            {
                get
                {
                    return otherTempConnector;
                }

                set
                {
                    otherTempConnector = value;
                }
            }

            public IMyShipConnector CONNECTOR1
            {
                get
                {
                    return CONNECTOR;
                }

                set
                {
                    CONNECTOR = value;
                }
            }

            public Vector3D DockPos1
            {
                get
                {
                    return dockPos1;
                }

                set
                {
                    dockPos1 = value;
                }
            }

            public Vector3D DockPos2
            {
                get
                {
                    return dockPos2;
                }

                set
                {
                    dockPos2 = value;
                }
            }

            public Vector3D DockPos3
            {
                get
                {
                    return dockPos3;
                }

                set
                {
                    dockPos3 = value;
                }
            }
            int cOORD_ID = 0;
            public int COORD_ID
            {
                get
                {
                    return cOORD_ID;
                }

                set
                {
                    cOORD_ID = value;
                }
            }


            public List<IMyTerminalBlock> TerminalBlocks
            {
                get
                {
                    return terminalBlocks;
                }

                set
                {
                    terminalBlocks = value;
                }
            }


            public List<IMyRadioAntenna> RadioAntennas
            {
                get
                {
                    return radioAntennas;
                }

                set
                {
                    radioAntennas = value;
                }
            }

        }
    }
}
