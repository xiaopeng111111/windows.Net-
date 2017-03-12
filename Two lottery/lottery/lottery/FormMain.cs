using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using DotNetSpeech;
namespace lottery
{
    public partial class FormMain : Form
    {
        public FormMain()
        {
            InitializeComponent();
        }
        public static ManualResetEvent runLottery_e;
        public static ManualResetEvent terminate_e;
        public static AutoResetEvent newLot_e;
        public static ManualResetEvent[] me_cap;
        public const int SHOW_SELECT = 0x500;
        public static IntPtr main_whandle;
        [DllImport("User32.dll", EntryPoint = "SendMessage")]
        private static extern int SendMessage(
            IntPtr hWnd,  //handle to destination window
            int Msg,  //message
            int wParam,  // first message parameter
            int IParam  //second message parameter
            );
        [DllImport("Kernel32.dll")]
        static extern int GetTickCount();


        private static int numIndex;
        void RandDataShow()
        {
            while (!terminate_e.WaitOne(INTERVAL))
            {
                if (runLottery_e.WaitOne(1))
                {
                    //抽奖中，从抽剩的数中选一个数据并通知窗体进行显示
                    LuckNum = (int)alRemains[numIndex];
                    if (numIndex == alRemains.Count - 1)
                    {
                        numIndex = 0;
                        RandData();
                    }
                    else
                    {
                        numIndex++;
                    }
                    SendMessage(main_whandle, SHOW_SELECT, 0, 0);
                }
                if (newLot_e.WaitOne(1))
                {
                    //生成下组待选项
                    RandData();
                }
            }
        }


        private static Random ran;
        private Control PreControl;
        private Control CurControl;
        private static int LuckNum;
        protected override void DefWndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case SHOW_SELECT:
                    PreControl.BackColor = Color.FromArgb(255, 240, 240, 240);
                    label1.Text = string.Format("{0}台---{1}号", LuckNum / 10 + 1, LuckNum % 10 + 1);
                    CurControl = (Control)alButtons[LuckNum / 10];
                    CurControl.BackColor = Color.Blue;
                    PreControl = CurControl;
                    break;
                default:
                    base.DefWndProc(ref m);
                    break;
            }
        }




        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SetWindowsHookEx(int hookType,
            HookProc callback, IntPtr hMod, uint dwThreadId);
        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);
        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        public static extern int CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr IParam);
        private bool RemoveLocalKeyboardHook()
        {
            if (hLocalKeyboardHook != IntPtr.Zero)
            {
                //Unhook the mouse hook
                if (!UnhookWindowsHookEx(hLocalKeyboardHook))
                    return false;
                hLocalKeyboardHook = IntPtr.Zero;
            }
            return true;
        }





        public int HC_ACTION = 0;
        public const int KEY_UP = 1 << 30;
        public int KeyboardProc(int nCode, IntPtr wParam, IntPtr IParam)
        {
            if (nCode == HC_ACTION)
            {
                //Get the virtual key code from wParam
                Keys vkCode = (Keys)wParam;
                //判断键是按下还是抬起，只取抬起一次
                if ((((UInt32)IParam) & KEY_UP) != 0)
                {
                    if (runLottery_e.WaitOne(1))
                    {
                        runLottery_e.Reset();   //播放抽中音
                    }
                    else
                    {
                        runLottery_e.Set();   //播放正在抽奖音
                        newLot_e.Set();
                    }
                }
            }
            //Pass the hook information to the next hook procedure in chain
            return CallNextHookEx(hLocalKeyboardHook, nCode, wParam, IParam);
        }




        public delegate int HookProc(int nCode, IntPtr wParam, IntPtr IParam);
        //Handle to the local keyboard hook procedure
        public IntPtr hLocalKeyboardHook = IntPtr.Zero;
        public HookProc localKeyboardHookCallback = null;
        public const int WH_KEYBOARD = 2;
        [DllImport("kernel32.dll")]
        internal static extern uint GetCurrentThreadId();
        private bool SetLocalKeyboardHook()
        {
            //Create an instance of HookProc.
            localKeyboardHookCallback = new HookProc(this.KeyboardProc);
            hLocalKeyboardHook = SetWindowsHookEx(
                WH_KEYBOARD,
                localKeyboardHookCallback,
                IntPtr.Zero,
                GetCurrentThreadId());
            return hLocalKeyboardHook != IntPtr.Zero;
        }

        public static ArrayList alButtons;
        public static ArrayList alRemains;
        public static ArrayList alRandoms;
        //一毫秒为单位的抽奖滚动间隔，值越小，刷新越快
        public const int INTERVAL = 500;
        public static void RandData()
        {
            alRandoms.Clear();
            int index = 0;
            int totainum = alRandoms.Count;
            for (int i = 1; i < totainum; i++)
            {
                index = ran.Next() % alRemains.Count;
                alRandoms.Add(alRemains[index]);
                alRemains.RemoveAt(index);
            }
            alRandoms.Add(alRemains[0]);
            for (int j = 0; j < alRandoms.Count; j++)
            {
                alRemains.Add(alRandoms[j]);
            }
        }
        private void FormMain_Load(object sender, EventArgs e)
        {
            alButtons = new ArrayList();
            alRemains = new ArrayList();
            alRandoms = new ArrayList();
            //添加四十个按钮控件，代表四十张，前二张台不抽
            for (int i = 0; i < 40; i++)
            {
                int x, y;
                //布局为每行6张台，可重设  
                y = i / 6;
                x = i % 6;
                Button pbox = new Button();
                pbox.Width = 100;
                pbox.Height = 50;
                pbox.Font = new Font("微软雅黑", 16);
                pbox.BackColor = Color.FromArgb(255, 240, 240, 240);
                //创建一个坐标，用来给新的按钮定位
                System.Drawing.Point p = new Point(30 + 120 * x, 170 + y * 75);
                pbox.Location = p;//把按钮的位置与刚创建的坐标绑定在一起
                pbox.Text = (i + 1).ToString() + "号台";
                pbox.Parent = this;
                alButtons.Add(pbox);
            }
            //添加事件对象
            runLottery_e = new ManualResetEvent(false);
            terminate_e = new ManualResetEvent(false);
            newLot_e = new AutoResetEvent(false);
            me_cap = new ManualResetEvent[2];
            me_cap[0] = runLottery_e;
            me_cap[1] = terminate_e;

            main_whandle = this.Handle;
            for (int ii = 0; ii < 400; ii++)
            {
                //共四百个数
                alRemains.Add(ii);
            }
            PreControl = (Control)alButtons[0];
            ran = new Random(GetTickCount());
            RandData();
            //启动抽奖提醒
            Thread workThread = new Thread(new ThreadStart(RandDataShow));
            workThread.IsBackground = true;
            workThread.Start();
            //开始本地键盘Hook
            SetLocalKeyboardHook();

        }

       

        private void FormMain_FormClosing_1(object sender, FormClosingEventArgs e)
        {
            RemoveLocalKeyboardHook();
            terminate_e.Set();
        }
    }
}
