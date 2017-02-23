using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using System.IO;
using System.Net;
using System.Threading;
using System.Drawing.Drawing2D;

namespace PLC_Control
{
    public partial class Form1 : Form
    {
        ObjectPLC_KV obj_PLC;

        //public System.Timers.Timer TimePLCProcess_Form1 = new System.Timers.Timer();

        //主畫面用
        public int SpeedRight_MR_30308, SpeedRight_MR_30208, SpeedRight_MR_204, SpeedRight_MR_30307, SpeedRight_MR_30201;

        //工作速度設定用
        public int SpeedRight_MR_202, SpeedRight_MR_203, SpeedRight_MR_201, SpeedRight_DM_90, SpeedRight_DM_95;

        //異常指示燈用
        public int SpeedRight_CR_201, SpeedRight_MR_31701, SpeedRight_MR_32005, SpeedRight_MR_32003, SpeedRight_MR_30000,
            SpeedRight_MR_37604, SpeedRight_MR_31702, SpeedRight_MR_32006, SpeedRight_MR_30202, SpeedRight_MR_30203,
            SpeedRight_MR_32004, SpeedRight_MR_37607;

        //計數設定用
        public int SpeedRight_MR_206, SpeedRight_DM_302, SpeedRight_DM_300;

        //排料速度設定用
        public int SpeedRight_DM_97;

        //畫新按鈕圖示用
        System.Drawing.Drawing2D.GraphicsPath aCircle = new System.Drawing.Drawing2D.GraphicsPath();
        System.Drawing.Drawing2D.GraphicsPath aCircle_Off = new System.Drawing.Drawing2D.GraphicsPath();
        System.Drawing.Drawing2D.GraphicsPath aCircle_On = new System.Drawing.Drawing2D.GraphicsPath();

        //執行緒
        Thread thread;
        public int Thread_Switch = 0;//是否讓執行緒內容執行(0不執行、1執行)
        int Running = 1; //執行緒打開(程式關閉才會清空)

        int AbnormalReturn_Count = 0; //顯示畫面用(異常指示燈使用)
        int Now_Second, Now_Second_Five_Seconds_Interval; //計算秒差用(當下秒數 五秒後)

        public Form1()
        {
            InitializeComponent();
            obj_PLC = new ObjectPLC_KV();
            obj_PLC.axDBCommManager = axDBCommManager1;

            Crawl_PLC_Information();
            lab_Progress.Text = "PLC尚未連線";

            //尚未連線 將斷線及寫入先鎖定
            btnDisConnect.Enabled = false;
            btnSend.Enabled = false;

            //先將所有Panel都隱藏 只留最原始的
            panel_WorkingSpeedSetting.Visible = false;
            panel_Abnormal_Lamp.Visible = false;
            panel_Counter.Visible = false;
            panel_FallSpeed.Visible = false;
            panel_Shutdown.Visible = false;
            panel_CloseProgram.Visible = false;

            //buttonPowerIndicator.Size = new Size(78, 52);108, 56
            //將按鈕變成圓形(關閉)252, 224
            aCircle_Off = new System.Drawing.Drawing2D.GraphicsPath();
            aCircle_Off.AddEllipse(new Rectangle(2, 2, 246, 246));
            aCircle_Off.AddEllipse(new Rectangle(50, 180, 30, 30));

            //將按鈕變成圓形(開啟)
            aCircle_On = new System.Drawing.Drawing2D.GraphicsPath();
            aCircle_On.AddEllipse(new Rectangle(2, 2, 246, 246));
            aCircle_On.AddEllipse(new Rectangle(170, 180, 30, 30));

            aCircle = new System.Drawing.Drawing2D.GraphicsPath();
            aCircle.AddEllipse(new Rectangle(2, 2, 196, 196));

            btn_NylonTrack.Region = new Region(aCircle_Off);
            btn_LubricatingOil.Region = new Region(aCircle_Off);
            btn_VibrationPlate.Region = new Region(aCircle_Off);
            btn_NutsRunway.Region = new Region(aCircle_Off);
            btn_LubricantContinuousAction.Region = new Region(aCircle_Off);

            buttonPowerIndicator.Region = new Region(aCircle);
            buttonMotorOverloadLights.Region = new Region(aCircle);
            buttonRunwayNoNuts.Region = new Region(aCircle);
            buttonAbnormalPressure.Region = new Region(aCircle);
            buttonEmergencyStop.Region = new Region(aCircle);
            buttonLubricatingOil.Region = new Region(aCircle);
            buttonHydraulicMotorOverload.Region = new Region(aCircle);
            buttonNoNylonRoad.Region = new Region(aCircle);
            buttonRangingSensor.Region = new Region(aCircle);
            buttonAbnormalReturn.Region = new Region(aCircle);
            buttonNoPressMadeOfNylon.Region = new Region(aCircle);
            buttonConveyor.Region = new Region(aCircle);

            thread = new Thread(TimerProcessFunc_Form1); //啟動Thread
            thread.Start();

            //程式一開啟就先連線
            if (obj_PLC.doMoniter() == false)
            {
                lab_Progress.Text = "PLC連線失敗";
                //尚未連線 使用者無法使用按鈕
                btn_NylonTrack.Enabled = false;
                btn_LubricatingOil.Enabled = false;
                btn_VibrationPlate.Enabled = false;
                btn_WorkingSpeedSetting.Enabled = false;
                btn_NutsRunway.Enabled = false;
                btn_LubricantContinuousAction.Enabled = false;
                btn_Abnormal_Lamp.Enabled = false;
                btn_Counter.Enabled = false;
                Thread_Switch = 0;//關閉執行緒
                lab_Progress.Text = "PLC連線失敗";
                return;
            }
            //連線成功 將按鈕斷線及寫入開啟 連線則關閉
            btnConnect.Enabled = false;
            btnDisConnect.Enabled = true;
            btnSend.Enabled = true;

            //連線成功 將按鈕提供給使用者使用
            btn_NylonTrack.Enabled = true;
            btn_LubricatingOil.Enabled = true;
            btn_VibrationPlate.Enabled = true;
            btn_WorkingSpeedSetting.Enabled = true;
            btn_NutsRunway.Enabled = true;
            btn_LubricantContinuousAction.Enabled = true;
            btn_Abnormal_Lamp.Enabled = true;
            btn_Counter.Enabled = true;
            Crawl_PLC_Information();
            Now_Second_Five_Seconds_Interval = System.DateTime.Now.AddSeconds(3).Second;//3秒後要更改系統狀態
            lab_Progress.Text = "PLC連線成功";
            Thread_Switch = 1;//開啟執行緒
        }

        public void Crawl_PLC_Information()
        {
            //主畫面用
            SpeedRight_MR_30308 = obj_PLC.doReadDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_MR, "30308"); //讀取MR30308 尼龍跑道偵測
            SpeedRight_MR_30208 = obj_PLC.doReadDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_MR, "30208"); //讀取MR30208 潤滑油偵測
            SpeedRight_MR_204 = obj_PLC.doReadDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_MR, "204"); //讀取MR30208 震動盤偵測
            SpeedRight_MR_30307 = obj_PLC.doReadDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_MR, "30307"); //讀取MR30307 螺帽跑道偵測
            SpeedRight_MR_30201 = obj_PLC.doReadDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_MR, "30201"); //讀取MR30201 潤滑油動作連續*/

            //工作速度設定用
            SpeedRight_MR_202 = obj_PLC.doReadDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_MR, "202"); //讀取MR202 加速
            SpeedRight_MR_203 = obj_PLC.doReadDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_MR, "203"); //讀取MR203 減速
            SpeedRight_MR_201 = obj_PLC.doReadDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_MR, "201"); //讀取MR201 輸入
            SpeedRight_DM_90 = obj_PLC.doReadDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_DM, "90"); //讀取DR90 目前轉速
            SpeedRight_DM_95 = obj_PLC.doReadDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_DM, "95"); //讀取DR95 轉速設定

            //異常指示燈用
            SpeedRight_CR_201 = obj_PLC.doReadDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_CR, "202"); //讀取CR202 電源指示燈
            SpeedRight_MR_31701 = obj_PLC.doReadDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_MR, "31701"); //讀取MR31701 主馬達過載燈
            SpeedRight_MR_32005 = obj_PLC.doReadDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_MR, "32005"); //讀取MR32005 跑道無螺帽燈
            SpeedRight_MR_32003 = obj_PLC.doReadDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_MR, "32003"); //讀取MR32003 壓力造成異常燈
            SpeedRight_MR_30000 = obj_PLC.doReadDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_MR, "30000"); //讀取MR30000 警急停止燈
            SpeedRight_MR_37604 = obj_PLC.doReadDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_MR, "37604"); //讀取MR37604 潤滑油指示燈
            SpeedRight_MR_31702 = obj_PLC.doReadDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_MR, "31702"); //讀取MR31702 油壓馬達過載燈
            SpeedRight_MR_32006 = obj_PLC.doReadDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_MR, "32006"); //讀取MR32006 跑道無尼龍燈
            SpeedRight_MR_30202 = obj_PLC.doReadDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_MR, "30202"); //讀取MR32202 測距senser燈
            SpeedRight_MR_30203 = obj_PLC.doReadDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_MR, "30203"); //讀取MR30203 異常復歸鍵
            SpeedRight_MR_32004 = obj_PLC.doReadDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_MR, "32004"); //讀取MR32004 壓造無尼龍燈
            SpeedRight_MR_37607 = obj_PLC.doReadDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_MR, "37607"); //讀取MR37607 輸送帶燈

            //記數設定用
            SpeedRight_MR_206 = obj_PLC.doReadDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_MR, "206"); //讀取MR206 單批歸零
            SpeedRight_DM_302 = obj_PLC.doReadDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_DM, "302"); //讀取DM302 累計數量
            SpeedRight_DM_300 = obj_PLC.doReadDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_DM, "300"); //讀取DM300 單批數量

            //排料速度設定用
            SpeedRight_DM_97 = obj_PLC.doReadDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_DM, "97"); //讀取DM97 掉落速度

            //計數用
            textBox_CurrentSpeed.Text = SpeedRight_DM_90.ToString();
            textBox_SpeedSetting.Text = SpeedRight_DM_95.ToString();

            Form.CheckForIllegalCrossThreadCalls = false;

            //工作速度設定用
            if (SpeedRight_MR_30308 >= 1)
            {
                btn_NylonTrack.Region = new Region(aCircle_On);
                btn_NylonTrack.BackColor = Color.GreenYellow;
                //Form.CheckForIllegalCrossThreadCalls = false;
                btn_NylonTrack.Text = "尼龍跑道偵測(開)";
            }
            else
            {
                btn_NylonTrack.Region = new Region(aCircle_Off);
                btn_NylonTrack.BackColor = Color.Sienna;
                //Form.CheckForIllegalCrossThreadCalls = false;
                btn_NylonTrack.Text = "尼龍跑道偵測(關)";
            }

            if (SpeedRight_MR_30208 >= 1)
            {
                btn_LubricatingOil.Region = new Region(aCircle_On);
                btn_LubricatingOil.BackColor = Color.GreenYellow;
                //Form.CheckForIllegalCrossThreadCalls = false;
                btn_LubricatingOil.Text = "潤滑油(開)";
            }
            else
            {
                btn_LubricatingOil.Region = new Region(aCircle_Off);
                btn_LubricatingOil.BackColor = Color.Sienna;
                //Form.CheckForIllegalCrossThreadCalls = false;
                btn_LubricatingOil.Text = "潤滑油(關)";
            }

            if (SpeedRight_MR_204 >= 1)
            {
                btn_VibrationPlate.Region = new Region(aCircle_On);
                btn_VibrationPlate.BackColor = Color.GreenYellow;
                //Form.CheckForIllegalCrossThreadCalls = false;
                btn_VibrationPlate.Text = "震動盤(開)";
            }
            else
            {
                btn_VibrationPlate.Region = new Region(aCircle_Off);
                btn_VibrationPlate.BackColor = Color.Sienna;
                //Form.CheckForIllegalCrossThreadCalls = false;
                btn_VibrationPlate.Text = "震動盤(關)";
            }

            if (SpeedRight_MR_30307 >= 1)
            {
                btn_NutsRunway.Region = new Region(aCircle_On);
                btn_NutsRunway.BackColor = Color.GreenYellow;
                //Form.CheckForIllegalCrossThreadCalls = false;
                btn_NutsRunway.Text = "螺帽跑道偵測(開)";
            }
            else
            {
                btn_NutsRunway.Region = new Region(aCircle_Off);
                btn_NutsRunway.BackColor = Color.Sienna;
                //Form.CheckForIllegalCrossThreadCalls = false;
                btn_NutsRunway.Text = "螺帽跑道偵測(關)";
            }

            if (SpeedRight_MR_30201 >= 1)
            {
                btn_LubricantContinuousAction.Region = new Region(aCircle_On);
                btn_LubricantContinuousAction.BackColor = Color.GreenYellow;
                //Form.CheckForIllegalCrossThreadCalls = false;
                btn_LubricantContinuousAction.Text = "潤滑油動作連續(開)";
            }
            else
            {
                btn_LubricantContinuousAction.Region = new Region(aCircle_Off);
                btn_LubricantContinuousAction.BackColor = Color.Sienna;
                //Form.CheckForIllegalCrossThreadCalls = false;
                btn_LubricantContinuousAction.Text = "潤滑油動作連續(關)";
            }

            if (SpeedRight_MR_31701 >= 1)
            {
                buttonMotorOverloadLights.BackColor = Color.GreenYellow;
                //Form.CheckForIllegalCrossThreadCalls = false;
                buttonMotorOverloadLights.Text = "主馬達過載燈(開)";
            }
            else
            {
                buttonMotorOverloadLights.BackColor = Color.Sienna;
                //Form.CheckForIllegalCrossThreadCalls = false;
                buttonMotorOverloadLights.Text = "主馬達過載燈(關)";
            }

            if (SpeedRight_MR_32005 >= 1)
            {
                buttonRunwayNoNuts.BackColor = Color.GreenYellow;
                //Form.CheckForIllegalCrossThreadCalls = false;
                buttonRunwayNoNuts.Text = "跑道無螺帽燈(開)";
            }
            else
            {
                buttonRunwayNoNuts.BackColor = Color.Sienna;
                //Form.CheckForIllegalCrossThreadCalls = false;
                buttonRunwayNoNuts.Text = "跑道無螺帽燈(關)";
            }

            if (SpeedRight_MR_32003 >= 1)
            {
                buttonAbnormalPressure.BackColor = Color.GreenYellow;
                //Form.CheckForIllegalCrossThreadCalls = false;
                buttonAbnormalPressure.Text = "壓力造成異常燈(開)";
            }
            else
            {
                buttonAbnormalPressure.BackColor = Color.Sienna;
                //Form.CheckForIllegalCrossThreadCalls = false;
                buttonAbnormalPressure.Text = "壓力造成異常燈(關)";
            }

            if (SpeedRight_MR_30000 >= 1)//判斷為B接點
            {
                buttonEmergencyStop.BackColor = Color.Sienna;
                //Form.CheckForIllegalCrossThreadCalls = false;
                buttonEmergencyStop.Text = "警急停止燈(關)";                
            }
            else
            {
                buttonEmergencyStop.BackColor = Color.GreenYellow;
                //Form.CheckForIllegalCrossThreadCalls = false;
                buttonEmergencyStop.Text = "警急停止燈(開)";
            }

            if (SpeedRight_MR_37604 >= 1)
            {
                buttonLubricatingOil.BackColor = Color.GreenYellow;
                //Form.CheckForIllegalCrossThreadCalls = false;
                buttonLubricatingOil.Text = "潤滑油指示燈(開)";
            }
            else
            {
                buttonLubricatingOil.BackColor = Color.Sienna;
                //Form.CheckForIllegalCrossThreadCalls = false;
                buttonLubricatingOil.Text = "潤滑油指示燈(關)";
            }

            if (SpeedRight_MR_31702 >= 1)
            {
                buttonHydraulicMotorOverload.BackColor = Color.GreenYellow;
                //Form.CheckForIllegalCrossThreadCalls = false;
                buttonHydraulicMotorOverload.Text = "油壓馬達過載燈(開)";
            }
            else
            {
                buttonHydraulicMotorOverload.BackColor = Color.Sienna;
                //Form.CheckForIllegalCrossThreadCalls = false;
                buttonHydraulicMotorOverload.Text = "油壓馬達過載燈(關)";
            }

            if (SpeedRight_MR_32006 >= 1)
            {
                buttonNoNylonRoad.BackColor = Color.GreenYellow;
                //Form.CheckForIllegalCrossThreadCalls = false;
                buttonNoNylonRoad.Text = "跑道無尼龍燈(開)";
            }
            else
            {
                buttonNoNylonRoad.BackColor = Color.Sienna;
                //Form.CheckForIllegalCrossThreadCalls = false;
                buttonNoNylonRoad.Text = "跑道無尼龍燈(關)";
            }

            if (SpeedRight_MR_30202 >= 1)
            {
                buttonRangingSensor.BackColor = Color.GreenYellow;
                //Form.CheckForIllegalCrossThreadCalls = false;
                buttonRangingSensor.Text = "測距senser燈(開)";
            }
            else
            {
                buttonRangingSensor.BackColor = Color.Sienna;
                //Form.CheckForIllegalCrossThreadCalls = false;
                buttonRangingSensor.Text = "測距senser燈(關)";
            }

            if (SpeedRight_MR_32004 >= 1)
            {
                buttonNoPressMadeOfNylon.BackColor = Color.GreenYellow;
                //Form.CheckForIllegalCrossThreadCalls = false;
                buttonNoPressMadeOfNylon.Text = "壓造無尼龍燈(開)";
            }
            else
            {
                buttonNoPressMadeOfNylon.BackColor = Color.Sienna;
                //Form.CheckForIllegalCrossThreadCalls = false;
                buttonNoPressMadeOfNylon.Text = "壓造無尼龍燈(關)";
            }

            if (SpeedRight_MR_37607 >= 1)
            {
                buttonConveyor.BackColor = Color.GreenYellow;
                //Form.CheckForIllegalCrossThreadCalls = false;
                buttonConveyor.Text = "輸送帶燈(開)";
            }
            else
            {
                buttonConveyor.BackColor = Color.Sienna;
                //Form.CheckForIllegalCrossThreadCalls = false;
                buttonConveyor.Text = "輸送帶燈(關)";
            }

            //計數設定用
            textBox_CountNumber.Text = SpeedRight_DM_302.ToString();
            textBox_SingleLotNumber.Text = SpeedRight_DM_300.ToString();

            //排料速度設定用
            textBox_DropSpeed.Text = SpeedRight_DM_97.ToString();
        }

        public void Form1_Load(object sender, EventArgs e)
        {
            
            /*if (TimePLCProcess_Form1.Enabled == false)//開啟timer
            {
                if (TimePLCProcess_Form1.Interval != 50)
                {
                    TimePLCProcess_Form1.Interval = 50;
                    TimePLCProcess_Form1.Elapsed += new System.Timers.ElapsedEventHandler(TimerProcessFunc_Form1);
                }
                TimePLCProcess_Form1.Enabled = true;
            }*/
        }

        //public void TimerProcessFunc_Form1(object sender, System.EventArgs e)
        public void TimerProcessFunc_Form1()
        {
            while (Running == 1)
            {
                if (Thread_Switch == 0)//尚未連線時
                {
                    Now_Second = System.DateTime.Now.AddSeconds(0).Second;//抓取當下秒數
                    //Console.WriteLine(Now_Second);
                    if (Now_Second_Five_Seconds_Interval > 55)//防止秒數高於55
                    {
                        Now_Second_Five_Seconds_Interval = 0;
                        //Console.WriteLine(Now_Second_Five_Seconds_Interval);
                    }
                    if (Now_Second - Now_Second_Five_Seconds_Interval > 0)//判斷五秒過後要將系統狀態更改
                    {
                        lab_Progress.Text = "PLC尚未連線";
                    }
                }
                else if (Thread_Switch == 1)//連線時
                {
                    Now_Second = System.DateTime.Now.AddSeconds(0).Second;//抓取當下秒數
                    //Console.WriteLine(Now_Second);
                    if (Now_Second_Five_Seconds_Interval > 55)//防止秒數高於55
                    {
                        Now_Second_Five_Seconds_Interval = 0;
                        //Console.WriteLine(Now_Second_Five_Seconds_Interval);
                    }
                    if (Now_Second - Now_Second_Five_Seconds_Interval > 0)//判斷五秒過後要將系統狀態更改
                    {
                        lab_Progress.Text = "PLC連線";
                    }
                    
                    //主畫面用 將數值從PLC讀出來
                    SpeedRight_MR_30308 = obj_PLC.doReadDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_MR, "30308"); //讀取MR30308 尼龍跑道偵測
                    SpeedRight_MR_30208 = obj_PLC.doReadDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_MR, "30208"); //讀取MR30208 潤滑油偵測
                    SpeedRight_MR_204 = obj_PLC.doReadDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_MR, "204");     //讀取MR30208 震動盤偵測
                    SpeedRight_MR_30307 = obj_PLC.doReadDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_MR, "30307"); //讀取MR30307 螺帽跑道偵測
                    SpeedRight_MR_30201 = obj_PLC.doReadDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_MR, "30201"); //讀取MR30201 潤滑油動作連續

                    //工作速度設定用 將數值從PLC讀出來
                    SpeedRight_MR_202 = obj_PLC.doReadDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_MR, "202"); //讀取MR202 加速
                    SpeedRight_MR_203 = obj_PLC.doReadDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_MR, "203"); //讀取MR203 減速
                    SpeedRight_MR_201 = obj_PLC.doReadDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_MR, "201"); //讀取MR201 輸入
                    SpeedRight_DM_90 = obj_PLC.doReadDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_DM, "90"); //讀取DR90 目前轉速
                    //SpeedRight_DM_95 = obj_PLC.doReadDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_DM, "95"); //讀取DM95 轉速設定

                    //異常指示燈用 將數值從PLC讀出來
                    SpeedRight_CR_201 = obj_PLC.doReadDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_CR, "202"); //讀取CR202 電源指示燈
                    SpeedRight_MR_31701 = obj_PLC.doReadDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_MR, "31701"); //讀取MR31701 主馬達過載燈
                    SpeedRight_MR_32005 = obj_PLC.doReadDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_MR, "32005"); //讀取MR32005 跑道無螺帽燈
                    SpeedRight_MR_32003 = obj_PLC.doReadDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_MR, "32003"); //讀取MR32003 壓力造成異常燈
                    SpeedRight_MR_30000 = obj_PLC.doReadDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_MR, "30000"); //讀取MR30000 警急停止燈
                    SpeedRight_MR_37604 = obj_PLC.doReadDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_MR, "37604"); //讀取MR37604 潤滑油指示燈
                    SpeedRight_MR_31702 = obj_PLC.doReadDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_MR, "31702"); //讀取MR31702 油壓馬達過載燈
                    SpeedRight_MR_32006 = obj_PLC.doReadDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_MR, "32006"); //讀取MR32006 跑道無尼龍燈
                    SpeedRight_MR_30202 = obj_PLC.doReadDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_MR, "30202"); //讀取MR30202 測距senser燈
                    SpeedRight_MR_30203 = obj_PLC.doReadDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_MR, "30203"); //讀取MR30203 異常復歸鍵
                    SpeedRight_MR_32004 = obj_PLC.doReadDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_MR, "32004"); //讀取MR32004 壓造無尼龍燈
                    SpeedRight_MR_37607 = obj_PLC.doReadDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_MR, "37607"); //讀取MR37607 輸送帶燈

                    //記數設定用 將數值從PLC讀出來
                    SpeedRight_MR_206 = obj_PLC.doReadDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_MR, "206"); //讀取MR
                    SpeedRight_DM_302 = obj_PLC.doReadDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_DM, "302"); //讀取DM302 累計數量
                    SpeedRight_DM_300 = obj_PLC.doReadDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_DM, "300"); //讀取DM300 單批數量    

                    //排料速度設定用 將數值從PLC讀出來
                    SpeedRight_DM_97 = obj_PLC.doReadDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_DM, "97"); //讀取DM97 掉落速度


                    //計數用
                    Form.CheckForIllegalCrossThreadCalls = false;
                    textBox_CurrentSpeed.Text = SpeedRight_DM_90.ToString();
                    //Form.CheckForIllegalCrossThreadCalls = false;
                    //textBox_SpeedSetting.Text = SpeedRight_DM_95.ToString();

                    //工作速度設定用
                    if (SpeedRight_MR_30308 >= 1)
                    {
                        btn_NylonTrack.Region = new Region(aCircle_On);
                        btn_NylonTrack.BackColor = Color.GreenYellow;
                        //btn_NylonTrack.ForeColor = Color.Black;
                        //Form.CheckForIllegalCrossThreadCalls = false;
                        btn_NylonTrack.Text = "尼龍跑道偵測(開)";
                    }
                    else 
                    {
                        btn_NylonTrack.Region = new Region(aCircle_Off);
                        btn_NylonTrack.BackColor = Color.Sienna;
                        //btn_NylonTrack.ForeColor = Color.White;
                        //Form.CheckForIllegalCrossThreadCalls = false;
                        btn_NylonTrack.Text = "尼龍跑道偵測(關)";
                    }

                    if (SpeedRight_MR_30208 >= 1)
                    {
                        btn_LubricatingOil.Region = new Region(aCircle_On);
                        btn_LubricatingOil.BackColor = Color.GreenYellow;
                        //Form.CheckForIllegalCrossThreadCalls = false;
                        btn_LubricatingOil.Text = "潤滑油(開)";
                    }
                    else
                    {
                        btn_LubricatingOil.Region = new Region(aCircle_Off);
                        btn_LubricatingOil.BackColor = Color.Sienna;
                        //Form.CheckForIllegalCrossThreadCalls = false;
                        btn_LubricatingOil.Text = "潤滑油(關)";
                    }

                    if (SpeedRight_MR_204 >= 1)
                    {
                        btn_VibrationPlate.Region = new Region(aCircle_On);
                        btn_VibrationPlate.BackColor = Color.GreenYellow;
                        //Form.CheckForIllegalCrossThreadCalls = false;
                        btn_VibrationPlate.Text = "震動盤(開)";
                    }
                    else
                    {
                        btn_VibrationPlate.Region = new Region(aCircle_Off);
                        btn_VibrationPlate.BackColor = Color.Sienna;
                        //Form.CheckForIllegalCrossThreadCalls = false;
                        btn_VibrationPlate.Text = "震動盤(關)";
                    }

                    if (SpeedRight_MR_30307 >= 1)
                    {
                        btn_NutsRunway.Region = new Region(aCircle_On);
                        btn_NutsRunway.BackColor = Color.GreenYellow;
                        //Form.CheckForIllegalCrossThreadCalls = false;
                        btn_NutsRunway.Text = "螺帽跑道偵測(開)";
                    }
                    else
                    {
                        btn_NutsRunway.Region = new Region(aCircle_Off);
                        btn_NutsRunway.BackColor = Color.Sienna;
                        //Form.CheckForIllegalCrossThreadCalls = false;
                        btn_NutsRunway.Text = "螺帽跑道偵測(關)";
                    }

                    if (SpeedRight_MR_30201 >= 1)
                    {
                        btn_LubricantContinuousAction.Region = new Region(aCircle_On);
                        btn_LubricantContinuousAction.BackColor = Color.GreenYellow;
                        //Form.CheckForIllegalCrossThreadCalls = false;
                        btn_LubricantContinuousAction.Text = "潤滑油動作連續(開)";
                    }
                    else
                    {
                        btn_LubricantContinuousAction.Region = new Region(aCircle_Off);
                        btn_LubricantContinuousAction.BackColor = Color.Sienna;
                        //Form.CheckForIllegalCrossThreadCalls = false;
                        btn_LubricantContinuousAction.Text = "潤滑油動作連續(關)";
                    }




                    //異常指示燈用
                    if (SpeedRight_MR_31701 >= 1 || SpeedRight_MR_32005 >= 1 || SpeedRight_MR_32003 >= 1 || SpeedRight_MR_30000 < 1 ||
                        SpeedRight_MR_31702 >= 1 || SpeedRight_MR_32006 >= 1 || SpeedRight_MR_30202 >= 1 || SpeedRight_MR_32004 >= 1)
                    {
                        if (AbnormalReturn_Count % 2 == 0)
                        {
                            btn_Abnormal_Lamp.BackColor = Color.Transparent;
                        }
                        else
                        {
                            btn_Abnormal_Lamp.BackColor = Color.GreenYellow;
                        }
                        //Form.CheckForIllegalCrossThreadCalls = false;
                        AbnormalReturn_Count++;
                    }
                    else
                    {
                        btn_Abnormal_Lamp.BackColor = Color.Transparent;
                    }
                    /*if (SpeedRight_CR_201 >= 1)
                    {
                        buttonPowerIndicator.BackColor = Color.GreenYellow;
                        Form.CheckForIllegalCrossThreadCalls = false;
                        buttonPowerIndicator.Text = "電源指示燈(開)";
                    }
                    else
                    {
                        buttonPowerIndicator.BackColor = Color.Sienna;
                        Form.CheckForIllegalCrossThreadCalls = false;
                        buttonPowerIndicator.Text = "電源指示燈(關)";
                    }*/

                    /*SpeedRight_MR_31701 = 1;
                    SpeedRight_MR_32005 = 1; //讀取MR32005 跑道無螺帽燈
                    SpeedRight_MR_32003 = 1; //讀取MR32003 壓力造成異常燈
                    SpeedRight_MR_30000 = 1; //讀取MR30000 警急停止燈
                    SpeedRight_MR_37604 = 1; //讀取MR37604 潤滑油指示燈
                    SpeedRight_MR_31702 = 1; //讀取MR31702 油壓馬達過載燈
                    SpeedRight_MR_32006 = 1; //讀取MR32006 跑道無尼龍燈
                    SpeedRight_MR_30202 = 1;//讀取MR30202 測距senser燈
                    SpeedRight_MR_30203 = 1; //讀取MR30203 異常復歸鍵
                    SpeedRight_MR_32004 = 1;
                    SpeedRight_MR_37607 = 1;*/


                    if (SpeedRight_MR_31701 >= 1)
                    {
                        buttonMotorOverloadLights.BackColor = Color.GreenYellow;
                        //Form.CheckForIllegalCrossThreadCalls = false;
                        buttonMotorOverloadLights.Text = "主馬達過載燈(開)";
                    }
                    else
                    {
                        buttonMotorOverloadLights.BackColor = Color.Sienna;
                        //Form.CheckForIllegalCrossThreadCalls = false;
                        buttonMotorOverloadLights.Text = "主馬達過載燈(關)";
                    }

                    if (SpeedRight_MR_32005 >= 1)
                    {
                        buttonRunwayNoNuts.BackColor = Color.GreenYellow;
                        //Form.CheckForIllegalCrossThreadCalls = false;
                        buttonRunwayNoNuts.Text = "跑道無螺帽燈(開)";
                    }
                    else
                    {
                        buttonRunwayNoNuts.BackColor = Color.Sienna;
                        //Form.CheckForIllegalCrossThreadCalls = false;
                        buttonRunwayNoNuts.Text = "跑道無螺帽燈(關)";
                    }

                    if (SpeedRight_MR_32003 >= 1)
                    {
                        buttonAbnormalPressure.BackColor = Color.GreenYellow;
                        //Form.CheckForIllegalCrossThreadCalls = false;
                        buttonAbnormalPressure.Text = "壓力造成異常燈(開)";
                    }
                    else
                    {
                        buttonAbnormalPressure.BackColor = Color.Sienna;
                        //Form.CheckForIllegalCrossThreadCalls = false;
                        buttonAbnormalPressure.Text = "壓力造成異常燈(關)";
                    }

                    if (SpeedRight_MR_30000 >= 1)//判斷為B接點
                    {
                        buttonEmergencyStop.BackColor = Color.Sienna;
                        //Form.CheckForIllegalCrossThreadCalls = false;
                        buttonEmergencyStop.Text = "警急停止燈(關)";
                    }
                    else
                    {
                        buttonEmergencyStop.BackColor = Color.GreenYellow;
                        //Form.CheckForIllegalCrossThreadCalls = false;
                        buttonEmergencyStop.Text = "警急停止燈(開)";
                    }

                    if (SpeedRight_MR_37604 >= 1)
                    {
                        buttonLubricatingOil.BackColor = Color.GreenYellow;
                        //Form.CheckForIllegalCrossThreadCalls = false;
                        buttonLubricatingOil.Text = "潤滑油指示燈(開)";
                    }
                    else
                    {
                        buttonLubricatingOil.BackColor = Color.Sienna;
                        //Form.CheckForIllegalCrossThreadCalls = false;
                        buttonLubricatingOil.Text = "潤滑油指示燈(關)";
                    }

                    if (SpeedRight_MR_31702 >= 1)
                    {
                        buttonHydraulicMotorOverload.BackColor = Color.GreenYellow;
                        //Form.CheckForIllegalCrossThreadCalls = false;
                        buttonHydraulicMotorOverload.Text = "油壓馬達過載燈(開)";
                    }
                    else
                    {
                        buttonHydraulicMotorOverload.BackColor = Color.Sienna;
                        //Form.CheckForIllegalCrossThreadCalls = false;
                        buttonHydraulicMotorOverload.Text = "油壓馬達過載燈(關)";
                    }

                    if (SpeedRight_MR_32006 >= 1)
                    {
                        buttonNoNylonRoad.BackColor = Color.GreenYellow;
                        //Form.CheckForIllegalCrossThreadCalls = false;
                        buttonNoNylonRoad.Text = "跑道無尼龍燈(開)";
                    }
                    else
                    {
                        buttonNoNylonRoad.BackColor = Color.Sienna;
                        //Form.CheckForIllegalCrossThreadCalls = false;
                        buttonNoNylonRoad.Text = "跑道無尼龍燈(關)";
                    }

                    if (SpeedRight_MR_30202 >= 1)
                    {
                        buttonRangingSensor.BackColor = Color.GreenYellow;
                        //Form.CheckForIllegalCrossThreadCalls = false;
                        buttonRangingSensor.Text = "測距senser燈(開)";
                    }
                    else
                    {
                        buttonRangingSensor.BackColor = Color.Sienna;
                        //Form.CheckForIllegalCrossThreadCalls = false;
                        buttonRangingSensor.Text = "測距senser燈(關)";
                    }

                    if (SpeedRight_MR_32004 >= 1)
                    {
                        buttonNoPressMadeOfNylon.BackColor = Color.GreenYellow;
                        //Form.CheckForIllegalCrossThreadCalls = false;
                        buttonNoPressMadeOfNylon.Text = "壓造無尼龍燈(開)";
                    }
                    else
                    {
                        buttonNoPressMadeOfNylon.BackColor = Color.Sienna;
                        //Form.CheckForIllegalCrossThreadCalls = false;
                        buttonNoPressMadeOfNylon.Text = "壓造無尼龍燈(關)";
                    }

                    if (SpeedRight_MR_37607 >= 1)
                    {
                        buttonConveyor.BackColor = Color.GreenYellow;
                        //Form.CheckForIllegalCrossThreadCalls = false;
                        buttonConveyor.Text = "輸送帶燈(開)";
                    }
                    else
                    {
                        buttonConveyor.BackColor = Color.Sienna;
                        //Form.CheckForIllegalCrossThreadCalls = false;
                        buttonConveyor.Text = "輸送帶燈(關)";
                    }

                    /*if (SpeedRight_MR_30203 >= 1)
                    {
                        buttonAbnormalReturn.BackColor = Color.GreenYellow;
                        Form.CheckForIllegalCrossThreadCalls = false;
                        buttonAbnormalReturn.Text = "異常復歸鍵(開)";
                    }
                    else
                    {
                        buttonAbnormalReturn.BackColor = Color.Sienna;
                        Form.CheckForIllegalCrossThreadCalls = false;
                        buttonAbnormalReturn.Text = "異常復歸鍵(關)";
                    }*/

                    //計數設定用
                    textBox_CountNumber.Text = SpeedRight_DM_302.ToString();
                    textBox_SingleLotNumber.Text = SpeedRight_DM_300.ToString();

                    //排料速度設定用
                    //textBox_DropSpeed.Text = SpeedRight_DM_97.ToString();
                }
                Thread.Sleep(500);
            }
        }

        public void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (obj_PLC.checkConnect(true, false) == true)
            {
                //已連線，進行斷線
                obj_PLC.doDeMoniter();
            }
            else
            {
                //return;
            }
            //關閉執行緒
            Thread_Switch = 0;
            Running = 0;
            thread.Abort();//執行續強制停止

            //關閉程式時將Timer關閉
            //TimePLCProcess_Form1.Enabled = false;
        }

        //PLC連線
        public void btnConnect_Click(object sender, EventArgs e)
        {
            if (obj_PLC.checkConnect(true, false) == true)
            {
                //連線成功 將按鈕斷線及寫入開啟 連線則關閉
                /*btnConnect.Enabled = false;
                btnDisConnect.Enabled = true;
                btnSend.Enabled = true;*/

                //連線成功 將按鈕提供給使用者使用
                btn_NylonTrack.Enabled = true;
                btn_LubricatingOil.Enabled = true;
                btn_VibrationPlate.Enabled = true;
                btn_WorkingSpeedSetting.Enabled = true;
                btn_NutsRunway.Enabled = true;
                btn_LubricantContinuousAction.Enabled = true;
                btn_Abnormal_Lamp.Enabled = true;
                btn_Counter.Enabled = true;
                Crawl_PLC_Information();
                Thread_Switch = 1;//開啟執行緒
                Now_Second_Five_Seconds_Interval = System.DateTime.Now.AddSeconds(5).Second;//3秒後要更改系統狀態
                lab_Progress.Text = "PLC已連線";
                //MessageBox.Show("已連線");
                return;
            }
            else
            {
                //未連線，進行連線
                if (obj_PLC.doMoniter() == false)
                {
                    //Now_Second_Five_Seconds_Interval = System.DateTime.Now.AddSeconds(5).Second;//五秒後要更改系統狀態
                    lab_Progress.Text = "PLC連線失敗";
                    return;
                }
                //連線成功 將按鈕斷線及寫入開啟 連線則關閉
                /*btnConnect.Enabled = false;
                btnDisConnect.Enabled = true;
                btnSend.Enabled = true;*/

                //連線成功 將按鈕提供給使用者使用
                btn_NylonTrack.Enabled = true;
                btn_LubricatingOil.Enabled = true;
                btn_VibrationPlate.Enabled = true;
                btn_WorkingSpeedSetting.Enabled = true;
                btn_NutsRunway.Enabled = true;
                btn_LubricantContinuousAction.Enabled = true;
                btn_Abnormal_Lamp.Enabled = true;
                btn_Counter.Enabled = true;
                Crawl_PLC_Information();
                Now_Second_Five_Seconds_Interval = System.DateTime.Now.AddSeconds(5).Second;//3秒後要更改系統狀態
                lab_Progress.Text = "PLC連線成功";
                Thread_Switch = 1;//開啟執行緒
            }
        }

        //PLC斷線
        public void btnDisConnect_Click(object sender, EventArgs e)
        {
            if (obj_PLC.checkConnect(true, false) == true)
            {
                //已連線，進行斷線
                obj_PLC.doDeMoniter();

                //斷開連線 將按鈕連線開啟 斷線及寫入則關閉
                btnConnect.Enabled = true;
                btnDisConnect.Enabled = false;
                btnSend.Enabled = false;

                //斷開後將所有Panel都隱藏 只留最原始的
                panel_MainScreen.Visible = true;
                panel_WorkingSpeedSetting.Visible = false;
                panel_Abnormal_Lamp.Visible = false;
                panel_Counter.Visible = false;
                panel_FallSpeed.Visible = false;
                panel_Shutdown.Visible = false;

                //尚未連線 使用者無法使用按鈕
                btn_NylonTrack.Enabled = false;
                btn_LubricatingOil.Enabled = false;
                btn_VibrationPlate.Enabled = false;
                btn_WorkingSpeedSetting.Enabled = false;
                btn_NutsRunway.Enabled = false;
                btn_LubricantContinuousAction.Enabled = false;
                btn_Abnormal_Lamp.Enabled = false;
                btn_Counter.Enabled = false;
                lab_Progress.Text = "PLC斷線";
                Thread_Switch = 0; //關閉執行緒
            }
            else
            {
                lab_Progress.Text = "PLC已斷線";
                Thread_Switch = 0; //關閉執行緒
                //MessageBox.Show("已斷線");
                return;
            }
        }

        //沒用到--------Start--------------
        public void btnSend_Click(object sender, EventArgs e)
        {
            //kv - N40dt;
            //寫入跟讀取PLC的範例
            obj_PLC.doWriteDevice(DATABUILDERAXLibLB.DBPlcDevice.DKV3000_MR, "5002", 1);    //寫入MR
            int SpeedRight_MR = obj_PLC.doReadDevice(DATABUILDERAXLibLB.DBPlcDevice.DKV3000_MR, "52"); //讀取MR
            obj_PLC.doWriteDevice(DATABUILDERAXLibLB.DBPlcDevice.DKV3000_DM, "5", 1600);    //寫入DM
            int SpeedRight_DM = obj_PLC.doReadDevice(DATABUILDERAXLibLB.DBPlcDevice.DKV3000_DM, "52"); //讀取DM
        }
        //沒用到---------End---------------

        //尼龍跑道偵測
        public void btn_NylonTrack_Click(object sender, EventArgs e)
        {
            if (btn_NylonTrack.Text == "尼龍跑道偵測(關)")
            {
                btn_NylonTrack.Region = new Region(aCircle_On);
                btn_NylonTrack.BackColor = Color.GreenYellow;
                btn_NylonTrack.Text = "尼龍跑道偵測(開)";
                obj_PLC.doWriteDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_MR, "30308", 1);    //寫入MR尼龍跑道偵測
            }
            else if (btn_NylonTrack.Text == "尼龍跑道偵測(開)")
            {
                btn_NylonTrack.Region = new Region(aCircle_Off);
                btn_NylonTrack.BackColor = Color.Sienna;
                btn_NylonTrack.Text = "尼龍跑道偵測(關)";
                obj_PLC.doWriteDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_MR, "30308", 0);    //寫入MR尼龍跑道偵測
            }
        }

        //潤滑油
        public void btn_LubricatingOil_Click(object sender, EventArgs e)
        {
            if (btn_LubricatingOil.Text == "潤滑油(關)")
            {
                btn_LubricatingOil.Region = new Region(aCircle_On);
                btn_LubricatingOil.BackColor = Color.GreenYellow;
                btn_LubricatingOil.Text = "潤滑油(開)";
                obj_PLC.doWriteDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_MR, "30208", 1);    //寫入MR潤滑油偵測
            }
            else if (btn_LubricatingOil.Text == "潤滑油(開)")
            {
                btn_LubricatingOil.Region = new Region(aCircle_Off);
                btn_LubricatingOil.BackColor = Color.Sienna;
                btn_LubricatingOil.Text = "潤滑油(關)";
                obj_PLC.doWriteDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_MR, "30208", 0);    //寫入MR潤滑油偵測
            }
        }

        //震動盤
        public void btn_VibrationPlate_Click(object sender, EventArgs e)
        {
            if (btn_VibrationPlate.Text == "震動盤(關)")
            {
                btn_VibrationPlate.Region = new Region(aCircle_On);
                btn_VibrationPlate.BackColor = Color.GreenYellow;
                btn_VibrationPlate.Text = "震動盤(開)";
                obj_PLC.doWriteDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_MR, "204", 1);    //寫入MR震動盤偵測
            }
            else if (btn_VibrationPlate.Text == "震動盤(開)")
            {
                btn_VibrationPlate.Region = new Region(aCircle_Off);
                btn_VibrationPlate.BackColor = Color.Sienna;
                btn_VibrationPlate.Text = "震動盤(關)";
                obj_PLC.doWriteDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_MR, "204", 0);    //寫入MR震動盤偵測
            }
        }

        //螺帽跑道偵測
        public void btn_NutsRunway_Click(object sender, EventArgs e)
        {
            if (btn_NutsRunway.Text == "螺帽跑道偵測(關)")
            {
                btn_NutsRunway.Region = new Region(aCircle_On);
                btn_NutsRunway.BackColor = Color.GreenYellow;
                btn_NutsRunway.Text = "螺帽跑道偵測(開)";
                obj_PLC.doWriteDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_MR, "30307", 1);    //寫入MR螺帽跑道偵測
            }
            else if (btn_NutsRunway.Text == "螺帽跑道偵測(開)")
            {
                btn_NutsRunway.Region = new Region(aCircle_Off);
                btn_NutsRunway.BackColor = Color.Sienna;
                btn_NutsRunway.Text = "螺帽跑道偵測(關)";
                obj_PLC.doWriteDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_MR, "30307", 0);    //寫入MR螺帽跑道偵測
            }
        }

        //潤滑油動作連續
        public void btn_LubricantContinuousAction_Click(object sender, EventArgs e)
        {
            if (btn_LubricantContinuousAction.Text == "潤滑油動作連續(關)")
            {
                btn_LubricantContinuousAction.Region = new Region(aCircle_On);
                btn_LubricantContinuousAction.BackColor = Color.GreenYellow;
                btn_LubricantContinuousAction.Text = "潤滑油動作連續(開)";
                obj_PLC.doWriteDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_MR, "30201", 1);    //寫入MR潤滑油動作連續
            }
            else if (btn_LubricantContinuousAction.Text == "潤滑油動作連續(開)")
            {
                btn_LubricantContinuousAction.Region = new Region(aCircle_Off);
                btn_LubricantContinuousAction.BackColor = Color.Sienna;
                btn_LubricantContinuousAction.Text = "潤滑油動作連續(關)";
                obj_PLC.doWriteDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_MR, "30201", 0);    //寫入MR潤滑油動作連續
            }
        }

        //工作速度設定
        public void btn_WorkingSpeedSetting_Click(object sender, EventArgs e)
        {
            panel_MainScreen.Visible = false;
            panel_WorkingSpeedSetting.Visible = true;
        }

        //異常/指示燈
        public void btn_Abnormal_Lamp_Click(object sender, EventArgs e)
        {
            panel_MainScreen.Visible = false;
            panel_Abnormal_Lamp.Visible = true;
        }

        //計數器設定
        public void btn_Counter_Click(object sender, EventArgs e)
        {
            panel_MainScreen.Visible = false;
            panel_Counter.Visible = true;
        }

        //工作速度設定(輸入)
        private void buttonEnter_Click(object sender, EventArgs e)
        {
            //尋找耍按下及放開事件找尋程式
            
        }

        int Input_Speed = 0;
        //輸入速度按鈕放開時寫入數值
        private void buttonEnterMouseUP_Click(object sender, MouseEventArgs e)
        {
            int a;
            if (Input_Speed == 1)
            {
                a = Int32.Parse(textBox_SpeedSetting.Text.ToString());
                if (a > 200)
                {
                    a = 200;
                    Now_Second_Five_Seconds_Interval = System.DateTime.Now.AddSeconds(2).Second;//2秒後要更改系統狀態
                    lab_Progress.Text = "速度無法超過200，資料設定完成";
                }
                else if (a < 1)
                {
                    a = 0;
                    Now_Second_Five_Seconds_Interval = System.DateTime.Now.AddSeconds(2).Second;//2秒後要更改系統狀態
                    lab_Progress.Text = "速度無法小於0，資料設定完成";
                }
                else
                {
                    Now_Second_Five_Seconds_Interval = System.DateTime.Now.AddSeconds(2).Second;//2秒後要更改系統狀態
                    lab_Progress.Text = "資料設定完成";
                }

                textBox_SpeedSetting.Text = a.ToString();
                obj_PLC.doWriteDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_MR, "200", 0);    //寫入MR200 輸入數值讓值改變
                obj_PLC.doWriteDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_MR, "201", 0);    //寫入MR201 輸入數值讓值改變
                textBox_CurrentSpeed.Text = textBox_SpeedSetting.Text;
                
                Input_Speed = 0;
                panel_WorkingSpeedSetting_Button.Visible = true;
                panel_WorkingSpeedSetting_Numerical.Visible = false;
            }
        }

        //輸入速度按下按鈕時
        private void buttonEnterMouseDown_Click(object sender, MouseEventArgs e)
        {
            int a;
            double b;
            try
            {
                a = Int32.Parse(textBox_SpeedSetting.Text.ToString());
                if (a > 200)
                {
                    a = 200;
                }
                else if (a < 1)
                {
                    a = 0;
                }

                obj_PLC.doWriteDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_DM, "90", a);    //寫入DR90 目前轉速
                obj_PLC.doWriteDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_DM, "95", a);    //寫入DR90 目前轉速
                Input_Speed = 1;
            }
            catch
            {
                b = Math.Floor(Double.Parse(textBox_SpeedSetting.Text.ToString()));
                if (b > 200)
                {
                    b = 200;
                }
                else if (b < 1)
                {
                    b = 0;
                }
                textBox_SpeedSetting.Text = b.ToString();
                Now_Second_Five_Seconds_Interval = System.DateTime.Now.AddSeconds(2).Second;//2秒後要更改系統狀態
                lab_Progress.Text = "請輸入整數，資料並未寫入，請重新輸入 \n且系統自動將小數點去除";
                //MessageBox.Show("請輸入整數，資料並未寫入，請重新輸入");
            }
        }

        //工作速度設定(加速)
        private void buttonSpeedAcceleration_Click(object sender, EventArgs e)
        {
            int a;
            double b;
            try
            {
                a = Int32.Parse(textBox_SpeedSetting.Text.ToString());
                a++;
                if (a > 200)
                {
                    a = 200;
                }
                textBox_SpeedSetting.Text = a.ToString();
                //lab_Progress.Text = "資料輸入完成";
            }
            catch
            {
                b = Math.Floor(Double.Parse(textBox_SpeedSetting.Text.ToString()));
                if (b > 200)
                {
                    b = 200;
                }
                textBox_SpeedSetting.Text = b.ToString();
                Now_Second_Five_Seconds_Interval = System.DateTime.Now.AddSeconds(2).Second;//2秒後要更改系統狀態
                lab_Progress.Text = "請輸入整數";
                //MessageBox.Show("請輸入整數");
            }
        }

        //工作速度設定(減速)
        private void buttonSpeedReducer_Click(object sender, EventArgs e)
        {
            int a;
            double b;
            try
            {
                a = Int32.Parse(textBox_SpeedSetting.Text.ToString());
                a--;
                if (a < 1)
                {
                    a = 0;
                }
                textBox_SpeedSetting.Text = a.ToString();
            }
            catch
            {
                b = Math.Floor(Double.Parse(textBox_SpeedSetting.Text.ToString()));
                if (b > 200)
                {
                    b = 200;
                }
                textBox_SpeedSetting.Text = b.ToString();
                Now_Second_Five_Seconds_Interval = System.DateTime.Now.AddSeconds(2).Second;//2秒後要更改系統狀態
                lab_Progress.Text = "請輸入整數";
                //MessageBox.Show("請輸入整數");
            }
        }

        //排料速度設定
        private void buttonDischargeVelocity_Click(object sender, EventArgs e)
        {
            panel_WorkingSpeedSetting.Visible = false;
            panel_FallSpeed.Visible = true;
        }

        /*private void buttonPowerIndicator_Click(object sender, EventArgs e)
        {
            if (buttonPowerIndicator.Text == "電源指示燈(關)")
            {
                buttonPowerIndicator.BackColor = Color.Red;
                buttonPowerIndicator.Text = "電源指示燈(開)";
                obj_PLC.doWriteDevice(DATABUILDERAXLibLB.DBPlcDevice.DKV3000_CR, "202", 1);    //寫入DM電源指示燈
            }
            else if (buttonPowerIndicator.Text == "電源指示燈(開)")
            {
                buttonPowerIndicator.BackColor = Color.Transparent;
                buttonPowerIndicator.Text = "電源指示燈(關)";
                obj_PLC.doWriteDevice(DATABUILDERAXLibLB.DBPlcDevice.DKV3000_CR, "202", 0);    //寫入DM電源指示燈
            }
        }

        private void buttonMotorOverloadLights_Click(object sender, EventArgs e)
        {
            if (buttonMotorOverloadLights.Text == "主馬達過載燈(關)")
            {
                buttonMotorOverloadLights.BackColor = Color.Red;
                buttonMotorOverloadLights.Text = "主馬達過載燈(開)";
                obj_PLC.doWriteDevice(DATABUILDERAXLibLB.DBPlcDevice.DKV3000_MR, "31701", 1);    //寫入MR主馬達過載燈
            }
            else if (buttonMotorOverloadLights.Text == "主馬達過載燈(開)")
            {
                buttonMotorOverloadLights.BackColor = Color.Transparent;
                buttonMotorOverloadLights.Text = "主馬達過載燈(關)";
                obj_PLC.doWriteDevice(DATABUILDERAXLibLB.DBPlcDevice.DKV3000_MR, "31701", 0);    //寫入MR主馬達過載燈
            }
        }

        private void buttonRunwayNoNuts_Click(object sender, EventArgs e)
        {
            if (buttonRunwayNoNuts.Text == "跑道無螺帽燈(關)")
            {
                buttonRunwayNoNuts.BackColor = Color.Red;
                buttonRunwayNoNuts.Text = "跑道無螺帽燈(開)";
                obj_PLC.doWriteDevice(DATABUILDERAXLibLB.DBPlcDevice.DKV3000_MR, "32005", 1);    //寫入MR跑道無螺帽燈
            }
            else if (buttonRunwayNoNuts.Text == "跑道無螺帽燈(開)")
            {
                buttonRunwayNoNuts.BackColor = Color.Transparent;
                buttonRunwayNoNuts.Text = "跑道無螺帽燈(關)";
                obj_PLC.doWriteDevice(DATABUILDERAXLibLB.DBPlcDevice.DKV3000_MR, "32005", 0);    //寫入MR跑道無螺帽燈
            }
        }

        private void buttonAbnormalPressure_Click(object sender, EventArgs e)
        {
            if (buttonAbnormalPressure.Text == "壓力造成異常燈(關)")
            {
                buttonAbnormalPressure.BackColor = Color.Red;
                buttonAbnormalPressure.Text = "壓力造成異常燈(開)";
                obj_PLC.doWriteDevice(DATABUILDERAXLibLB.DBPlcDevice.DKV3000_MR, "35300", 1);    //寫入MR壓力造成異常燈
            }
            else if (buttonAbnormalPressure.Text == "壓力造成異常燈(開)")
            {
                buttonAbnormalPressure.BackColor = Color.Transparent;
                buttonAbnormalPressure.Text = "壓力造成異常燈(關)";
                obj_PLC.doWriteDevice(DATABUILDERAXLibLB.DBPlcDevice.DKV3000_MR, "35300", 0);    //寫入MR壓力造成異常燈
            }
        }

        private void buttonEmergencyStop_Click(object sender, EventArgs e)
        {
            if (buttonEmergencyStop.Text == "警急停止燈(關)")
            {
                buttonEmergencyStop.BackColor = Color.Red;
                buttonEmergencyStop.Text = "警急停止燈(開)";
                obj_PLC.doWriteDevice(DATABUILDERAXLibLB.DBPlcDevice.DKV3000_MR, "30000", 1);    //寫入MR警急停止燈
            }
            else if (buttonEmergencyStop.Text == "警急停止燈(開)")
            {
                buttonEmergencyStop.BackColor = Color.Transparent;
                buttonEmergencyStop.Text = "警急停止燈(關)";
                obj_PLC.doWriteDevice(DATABUILDERAXLibLB.DBPlcDevice.DKV3000_MR, "30000", 0);    //寫入MR警急停止燈
            }
        }

        private void buttonLubricatingOil_Click(object sender, EventArgs e)
        {
            if (buttonLubricatingOil.Text == "潤滑油指示燈(關)")
            {
                buttonLubricatingOil.BackColor = Color.Red;
                buttonLubricatingOil.Text = "潤滑油指示燈(開)";
                obj_PLC.doWriteDevice(DATABUILDERAXLibLB.DBPlcDevice.DKV3000_MR, "37604", 1);    //寫入MR潤滑油指示燈
            }
            else if (buttonLubricatingOil.Text == "潤滑油指示燈(開)")
            {
                buttonLubricatingOil.BackColor = Color.Transparent;
                buttonLubricatingOil.Text = "潤滑油指示燈(關)";
                obj_PLC.doWriteDevice(DATABUILDERAXLibLB.DBPlcDevice.DKV3000_MR, "37604", 0);    //寫入MR潤滑油指示燈
            }
        }

        private void buttonHydraulicMotorOverload_Click(object sender, EventArgs e)
        {
            if (buttonHydraulicMotorOverload.Text == "油壓馬達過載燈(關)")
            {
                buttonHydraulicMotorOverload.BackColor = Color.Red;
                buttonHydraulicMotorOverload.Text = "油壓馬達過載燈(開)";
                obj_PLC.doWriteDevice(DATABUILDERAXLibLB.DBPlcDevice.DKV3000_MR, "31702", 1);    //寫入MR油壓馬達過載燈
            }
            else if (buttonHydraulicMotorOverload.Text == "油壓馬達過載燈(開)")
            {
                buttonHydraulicMotorOverload.BackColor = Color.Transparent;
                buttonHydraulicMotorOverload.Text = "油壓馬達過載燈(關)";
                obj_PLC.doWriteDevice(DATABUILDERAXLibLB.DBPlcDevice.DKV3000_MR, "31702", 0);    //寫入MR油壓馬達過載燈
            }
        }

        private void buttonNoNylonRoad_Click(object sender, EventArgs e)
        {
            if (buttonNoNylonRoad.Text == "跑道無尼龍燈(關)")
            {
                buttonNoNylonRoad.BackColor = Color.Red;
                buttonNoNylonRoad.Text = "跑道無尼龍燈(開)";
                obj_PLC.doWriteDevice(DATABUILDERAXLibLB.DBPlcDevice.DKV3000_MR, "32006", 1);    //寫入MR跑道無尼龍燈

            }
            else if (buttonNoNylonRoad.Text == "跑道無尼龍燈(開)")
            {
                buttonNoNylonRoad.BackColor = Color.Transparent;
                buttonNoNylonRoad.Text = "跑道無尼龍燈(關)";
                obj_PLC.doWriteDevice(DATABUILDERAXLibLB.DBPlcDevice.DKV3000_MR, "32006", 0);    //寫入MR跑道無尼龍燈
            }
        }

        private void buttonRangingSensor_Click(object sender, EventArgs e)
        {
            if (buttonRangingSensor.Text == "測距senser燈(關)")
            {
                buttonRangingSensor.BackColor = Color.Red;
                buttonRangingSensor.Text = "測距senser燈(開)";
                obj_PLC.doWriteDevice(DATABUILDERAXLibLB.DBPlcDevice.DKV3000_MR, "32202", 1);    //寫入MR測距senser燈
            }
            else if (buttonRangingSensor.Text == "測距senser燈(開)")
            {
                buttonRangingSensor.BackColor = Color.Transparent;
                buttonRangingSensor.Text = "測距senser燈(關)";
                obj_PLC.doWriteDevice(DATABUILDERAXLibLB.DBPlcDevice.DKV3000_MR, "32202", 0);    //寫入MR測距senser燈
            }
        }*/

        private void buttonAbnormalReturn_Click(object sender, EventArgs e)
        {
            if (SpeedRight_MR_31701 >= 1)//主馬達過載燈
            {
                //SpeedRight_MR_31701 = 0;
                obj_PLC.doWriteDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_MR, "31701", 0);    //寫入MR主馬達過載燈
            }
            if (SpeedRight_MR_32005 >= 1)//跑道無螺帽燈
            {
                //SpeedRight_MR_32005 = 0;
                obj_PLC.doWriteDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_MR, "32005", 0);    //寫入MR跑道無螺帽燈
            }
            if (SpeedRight_MR_32003 >= 1)//壓力造成異常燈
            {
                //SpeedRight_MR_32003 = 0;
                obj_PLC.doWriteDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_MR, "32003", 0);    //寫入MR壓力造成異常燈
            }
            if (SpeedRight_MR_30000 >= 1)//判斷為B接點  警急停止燈
            {
                //SpeedRight_MR_30000 = 0;
                obj_PLC.doWriteDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_MR, "30000", 0);    //寫入MR警急停止燈
            }
            if (SpeedRight_MR_37604 >= 1)//潤滑油指示燈
            {
                //SpeedRight_MR_37604 = 0;
                obj_PLC.doWriteDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_MR, "37604", 0);    //寫入MR潤滑油指示燈
            }
            if (SpeedRight_MR_31702 >= 1)//油壓馬達過載燈
            {
                //SpeedRight_MR_31702 = 0;
                obj_PLC.doWriteDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_MR, "31702", 0);    //寫入MR油壓馬達過載燈
            }
            if (SpeedRight_MR_32006 >= 1)//跑道無尼龍燈
            {
                //SpeedRight_MR_32006 = 0;
                obj_PLC.doWriteDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_MR, "32006", 0);    //寫入MR跑道無尼龍燈
            }
            if (SpeedRight_MR_30202 >= 1)//測距senser燈
            {
                //SpeedRight_MR_30202 = 0;
                obj_PLC.doWriteDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_MR, "30202", 0);    //寫入MR測距senser燈
            }
            if (SpeedRight_MR_32004 >= 1)//壓造無尼龍燈
            {
                //SpeedRight_MR_32004 = 0;
                obj_PLC.doWriteDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_MR, "32004", 0);    //寫入MR壓造無尼龍燈
            }
            if (SpeedRight_MR_37607 >= 1)//輸送帶燈
            {
                //SpeedRight_MR_37607 = 0;
                obj_PLC.doWriteDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_MR, "37607", 0);    //寫入MR輸送帶燈
            }

            Now_Second_Five_Seconds_Interval = System.DateTime.Now.AddSeconds(2).Second;//2秒後要更改系統狀態
            lab_Progress.Text = "異常復歸";
            AbnormalReturn_Count = 0;
        }

        //返回主畫面(在工作速度設定時)
        private void buttonBackToMainScreen_WorkingSpeed_Click(object sender, EventArgs e)
        {
            panel_MainScreen.Visible = true;
            panel_WorkingSpeedSetting.Visible = false;
        }

        //返回主畫面(在異常/指示燈時)
        private void buttonBackToMainScreen_AbnormalIndication_Click(object sender, EventArgs e)
        {
            panel_MainScreen.Visible = true;
            panel_Abnormal_Lamp.Visible = false;
        }

        //返回主畫面(在計數器設定時)
        private void BackToMainScreen_Counter_Click(object sender, EventArgs e)
        {
            panel_MainScreen.Visible = true;
            panel_Counter.Visible = false;
        }

        //排料速度設定(輸入)
        private void buttonEnter_Drop_Click(object sender, EventArgs e)
        {
            int a;
            double b;
            try
            {
                a = Int32.Parse(textBox_DropSpeed.Text.ToString());
                if (a > 200)
                {
                    a = 200;
                    textBox_DropSpeed.Text = a.ToString();
                }
                else if (a < 1)
                {
                    a = 0;
                    textBox_DropSpeed.Text = a.ToString();
                }
                obj_PLC.doWriteDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_DM, "97", a);    //寫入DR97 掉落速度
                Now_Second_Five_Seconds_Interval = System.DateTime.Now.AddSeconds(2).Second;//2秒後要更改系統狀態
                lab_Progress.Text = "資料設定完成";
            }
            catch
            {
                b = Math.Floor(Double.Parse(textBox_DropSpeed.Text.ToString()));
                if (b > 200)
                {
                    b = 200;
                }
                else if (b < 1)
                {
                    b = 0;
                }
                textBox_DropSpeed.Text = b.ToString();
                Now_Second_Five_Seconds_Interval = System.DateTime.Now.AddSeconds(2).Second;//2秒後要更改系統狀態
                lab_Progress.Text = "請輸入整數，資料並未寫入，請重新輸入 \n且系統自動將小數點去除";
                //MessageBox.Show("請輸入整數，資料並未寫入，請重新輸入");
            }
        }

        //排料速度設定(加速)
        private void button_add_Click(object sender, EventArgs e)
        {
            int a;
            double b;
            try
            {
                a = Int32.Parse(textBox_DropSpeed.Text.ToString());
                a++;
                if (a > 200)
                {
                    a = 200;
                }
                textBox_DropSpeed.Text = a.ToString();
            }
            catch
            {
                b = Math.Floor(Double.Parse(textBox_DropSpeed.Text.ToString()));
                if (b > 200)
                {
                    b = 200;
                }
                textBox_DropSpeed.Text = b.ToString();
                Now_Second_Five_Seconds_Interval = System.DateTime.Now.AddSeconds(2).Second;//2秒後要更改系統狀態
                lab_Progress.Text = "請輸入整數";
                //MessageBox.Show("請輸入整數");
            }
        }

        //排料速度設定(減速)
        private void button_subtract_Click(object sender, EventArgs e)
        {
            int a;
            double b;
            try
            {
                a = Int32.Parse(textBox_DropSpeed.Text.ToString());
                a--;
                if (a < 1)
                {
                    a = 0;
                }
                textBox_DropSpeed.Text = a.ToString();
            }
            catch
            {
                b = Math.Floor(Double.Parse(textBox_DropSpeed.Text.ToString()));
                if (b > 200)
                {
                    b = 200;
                }
                textBox_DropSpeed.Text = b.ToString();
                Now_Second_Five_Seconds_Interval = System.DateTime.Now.AddSeconds(2).Second;//2秒後要更改系統狀態
                lab_Progress.Text = "請輸入整數";
                //MessageBox.Show("請輸入整數");
            }
        }

        //計數器單批歸零
        private void buttonSingleLotZero_Click(object sender, EventArgs e)
        {
            obj_PLC.doWriteDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_DM, "300", 0);    //寫入DM300 單批歸零
            obj_PLC.doWriteDevice(DATABUILDERAXLibLB.DBPlcDevice.DKVNano_DM, "301", 0);    //寫入DM301 單批數量
            textBox_SingleLotNumber.Text = 0.ToString();
            Now_Second_Five_Seconds_Interval = System.DateTime.Now.AddSeconds(2).Second;//2秒後要更改系統狀態
            lab_Progress.Text = "資料設定完成";
        }

        //排料速度設定回到工作速度設定
        private void buttonBackToWorkingSpeedSetting_Click(object sender, EventArgs e)
        {
            panel_WorkingSpeedSetting.Visible = true;
            panel_FallSpeed.Visible = false;
        }

        //排料速度設定回到主畫面
        private void buttonBackToMainScreen_FallSpeed_Click(object sender, EventArgs e)
        {
            panel_MainScreen.Visible = true;
            panel_FallSpeed.Visible = false;
        }

        //string value;
        //關閉程式
        private void btn_Close_Click(object sender, EventArgs e)
        {
            /*if (Form1.InputBox("確定要關閉程式？", "請輸入密碼:", ref value) == DialogResult.OK)
            {
                if (value == "0126")
                {
                    //MessageBox.Show("密碼正確，修改成功");*/
                    this.Close();
                    /*return;
                }
                else
                {
                    MessageBox.Show("密碼錯誤，修改失敗");
                    return;
                }
            }*/
        }

        //關機指令
        private void btn_Shutdown_Click(object sender, EventArgs e)
        {
            panel_WorkingSpeedSetting.Visible = false;
            panel_Abnormal_Lamp.Visible = false;
            panel_Counter.Visible = false;
            panel_FallSpeed.Visible = false;
            panel_MainScreen.Visible = false;
            panel_Shutdown.Visible = true;
            panel_CloseProgram.Visible = false;

            /*//關機
            System.Diagnostics.Process.Start("C:\\WINDOWS\\system32\\shutdown.exe", "-f -s -t 0");

            //登出
            System.Diagnostics.Process.Start("C:\\WINDOWS\\system32\\shutdown.exe", "-l");

            //休眠
            System.Diagnostics.Process.Start("C:\\WINDOWS\\system32\\rundll32.exe", "powrprof.dll,SetSuspendState");

            //重新開機
            System.Diagnostics.Process.Start("C:\\WINDOWS\\system32\\shutdown.exe", "-f -r -t 0");*/
        }

        //關機
        private void btn_Shutdown_close_Click(object sender, EventArgs e)
        {
            /*if (Form1.InputBox("確定要關機？", "請輸入密碼:", ref value) == DialogResult.OK)
            {
                if (value == "0126")
                {*/
                    //MessageBox.Show("密碼正確，修改成功");
                    //TimePLCProcess_Form1.Enabled = false;
                    System.Diagnostics.Process.Start("C:\\WINDOWS\\system32\\shutdown.exe", "-f -s -t 0");
                    this.Close();
                    /*return;
                }
                else
                {
                    MessageBox.Show("密碼錯誤，修改失敗");
                    return;
                }
            }*/
        }

        //重新開機
        private void btn_Reboot_Click(object sender, EventArgs e)
        {
            /*if (Form1.InputBox("確定要重新開機？", "請輸入密碼:", ref value) == DialogResult.OK)
            {
                if (value == "0126")
                {*/
                    //MessageBox.Show("密碼正確，修改成功");
                    //TimePLCProcess_Form1.Enabled = false;
                    System.Diagnostics.Process.Start("C:\\WINDOWS\\system32\\shutdown.exe", "-f -r -t 0");
                    this.Close();
                    /*return;
                }
                else
                {
                    MessageBox.Show("密碼錯誤，修改失敗");
                    return;
                }
            }*/
        }

        //關閉程式用-------------------Start-------------------
        private void btn_Cancel_Click(object sender, EventArgs e)
        {
            panel_MainScreen.Visible = true;
            panel_Shutdown.Visible = false;
        }

        private void lab_HideButton_DoubleClick(object sender, EventArgs e)
        {
            textBox_Password.Text = "";
            panel_CloseProgram.Visible = true;
        }

        private void btn_CancelClose_Click(object sender, EventArgs e)
        {
            panel_CloseProgram.Visible = false;
        }

        private void btn_OK_Close_Click(object sender, EventArgs e)
        {
            string Password = textBox_Password.Text;
            if (Password == "0126")
            {
                this.Close();
            }
            else
            {
                Now_Second_Five_Seconds_Interval = System.DateTime.Now.AddSeconds(2).Second;//2秒後要更改系統狀態
                lab_Progress.Text = "密碼錯誤";
            }
        }

        private void btn_1_Click(object sender, EventArgs e)
        {
            string Password = textBox_Password.Text;
            textBox_Password.Text = Password + "1";
        }

        private void btn_2_Click(object sender, EventArgs e)
        {
            string Password = textBox_Password.Text;
            textBox_Password.Text = Password + "2";
        }

        private void btn_3_Click(object sender, EventArgs e)
        {
            string Password = textBox_Password.Text;
            textBox_Password.Text = Password + "3";
        }

        private void btn_4_Click(object sender, EventArgs e)
        {
            string Password = textBox_Password.Text;
            textBox_Password.Text = Password + "4";
        }

        private void btn_5_Click(object sender, EventArgs e)
        {
            string Password = textBox_Password.Text;
            textBox_Password.Text = Password + "5";
        }

        private void btn_6_Click(object sender, EventArgs e)
        {
            string Password = textBox_Password.Text;
            textBox_Password.Text = Password + "6";
        }

        private void btn__7_Click(object sender, EventArgs e)
        {
            string Password = textBox_Password.Text;
            textBox_Password.Text = Password + "7";
        }

        private void btn_8_Click(object sender, EventArgs e)
        {
            string Password = textBox_Password.Text;
            textBox_Password.Text = Password + "8";
        }

        private void btn_9_Click(object sender, EventArgs e)
        {
            string Password = textBox_Password.Text;
            textBox_Password.Text = Password + "9";
        }

        private void btn_0_Click(object sender, EventArgs e)
        {
            string Password = textBox_Password.Text;
            textBox_Password.Text = Password + "0";
        }

        private void btn_Delete_Click(object sender, EventArgs e)
        {
            textBox_Password.Text = "";
        }

        private void btn_Return_Click(object sender, EventArgs e)
        {
            if (textBox_Password.Text.Length - 1 >= 0)
            {
                textBox_Password.Text = textBox_Password.Text.Substring(0, textBox_Password.Text.Length - 1);
            }
        }
        //關閉程式用--------------------End--------------------


        //修改速度用-------------------Start-------------------
        private void textBox_SpeedSetting_Click(object sender, EventArgs e)
        {
            panel_WorkingSpeedSetting_Button.Visible = false;
            panel_WorkingSpeedSetting_Numerical.Visible = true;
        }

        private void btn_WorkingSpeedSetting_1_Click(object sender, EventArgs e)
        {
            string Password = textBox_SpeedSetting.Text;
            textBox_SpeedSetting.Text = Password + "1";
        }

        private void btn_WorkingSpeedSetting_2_Click(object sender, EventArgs e)
        {
            string Password = textBox_SpeedSetting.Text;
            textBox_SpeedSetting.Text = Password + "2";
        }

        private void btn_WorkingSpeedSetting_3_Click(object sender, EventArgs e)
        {
            string Password = textBox_SpeedSetting.Text;
            textBox_SpeedSetting.Text = Password + "3";
        }

        private void btn_WorkingSpeedSetting_4_Click(object sender, EventArgs e)
        {
            string Password = textBox_SpeedSetting.Text;
            textBox_SpeedSetting.Text = Password + "4";
        }

        private void btn_WorkingSpeedSetting_5_Click(object sender, EventArgs e)
        {
            string Password = textBox_SpeedSetting.Text;
            textBox_SpeedSetting.Text = Password + "5";
        }

        private void btn_WorkingSpeedSetting_6_Click(object sender, EventArgs e)
        {
            string Password = textBox_SpeedSetting.Text;
            textBox_SpeedSetting.Text = Password + "6";
        }

        private void btn_WorkingSpeedSetting_7_Click(object sender, EventArgs e)
        {
            string Password = textBox_SpeedSetting.Text;
            textBox_SpeedSetting.Text = Password + "7";
        }

        private void btn_WorkingSpeedSetting_8_Click(object sender, EventArgs e)
        {
            string Password = textBox_SpeedSetting.Text;
            textBox_SpeedSetting.Text = Password + "8";
        }

        private void btn_WorkingSpeedSetting_9_Click(object sender, EventArgs e)
        {
            string Password = textBox_SpeedSetting.Text;
            textBox_SpeedSetting.Text = Password + "9";
        }

        private void btn_WorkingSpeedSetting_0_Click(object sender, EventArgs e)
        {
            string Password = textBox_SpeedSetting.Text;
            textBox_SpeedSetting.Text = Password + "0";
        }

        private void btn_WorkingSpeedSetting_Delete_Click(object sender, EventArgs e)
        {
            textBox_SpeedSetting.Text = "";
        }

        private void btn_WorkingSpeedSetting_Modify_Click(object sender, EventArgs e)
        {
            if (textBox_SpeedSetting.Text.Length - 1 >= 0)
            {
                textBox_SpeedSetting.Text = textBox_SpeedSetting.Text.Substring(0, textBox_SpeedSetting.Text.Length - 1);
            }
        }

        private void btn_WorkingSpeedSetting_Return_Click(object sender, EventArgs e)
        {
            panel_WorkingSpeedSetting_Button.Visible = true;
            panel_WorkingSpeedSetting_Numerical.Visible = false;
        }
        //修改速度用--------------------End--------------------




        //產生可輸入之MessageBox
        /*public static DialogResult InputBox(string title, string promptText, ref string value)
        {
            Form form = new Form();
            Label label = new Label();
            TextBox textBox = new TextBox();
            Button buttonOk = new Button();
            Button buttonCancel = new Button();
            value = "";
            form.Text = title;
            label.Text = promptText;
            textBox.Text = value;

            buttonOk.Text = "OK";
            buttonCancel.Text = "Cancel";
            buttonOk.DialogResult = DialogResult.OK;
            buttonCancel.DialogResult = DialogResult.Cancel;

            label.SetBounds(9, 20, 372, 13);
            textBox.SetBounds(12, 36, 372, 20);
            buttonOk.SetBounds(228, 72, 75, 23);
            buttonCancel.SetBounds(309, 72, 75, 23);

            label.AutoSize = true;
            textBox.Anchor = textBox.Anchor | AnchorStyles.Right;
            buttonOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            form.ClientSize = new Size(396, 107);
            form.Controls.AddRange(new Control[] { label, textBox, buttonOk, buttonCancel });
            form.ClientSize = new Size(Math.Max(300, label.Right + 10), form.ClientSize.Height);
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.StartPosition = FormStartPosition.CenterScreen;
            form.MinimizeBox = false;
            form.MaximizeBox = false;
            form.AcceptButton = buttonOk;
            form.CancelButton = buttonCancel;

            DialogResult dialogResult = form.ShowDialog();
            value = textBox.Text;
            return dialogResult;
        }*/
    }
}