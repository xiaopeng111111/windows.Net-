using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace _01服务发现
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        public static byte[] udpDataSendBuf;
        public static Socket sockUdpSearchSend;
        public static Socket sockUdpSearchRecv;
        public static EndPoint remoteEp;
        public static IPEndPoint RemoteServerIpEP;
        public static Mutex mutSearch;
        public static int iUdprecvDataLen;
        public static EndPoint remoteUdp_ep;
        public static ManualResetEvent mrEvent_GotServer;
        public static string strInfo;
        public static byte[] udpRecvDataBuf;
        public static int recv_data_len;
        public static bool udpSockClosed;
        public static IntPtr main_wnd_handle;
        //动态链接库引入
        [DllImport("User32.dll", EntryPoint = "SendMessage")]
        private static extern int SendMessage(
            IntPtr hWnd,   //handle to destination window
            int Msg,   //message
            int wParam,  //first message parameter
            int lPatam   //second message parameter
            );
        public const int NO_SERVER = 0x520;
        public const int FOUND_SERVER = 0x521;
        public static Socket client_sock;
        public static IPEndPoint ipeServerRemoteIp;
        public static bool bServerExists;
        public static IPAddress LocalHostIPAddress;
        public static string strLocalHAddr;

        private void Form1_Load(object sender, EventArgs e)
        {
            main_wnd_handle = this.Handle;
            //初始时服务器标志位false
            bServerExists = false;
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            //检测可用网卡的方式是网关不为0.0.0.0 掩码不为空
            NetworkInterface[] NetworkInterfaces =
                NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface NetworkInterf in NetworkInterfaces)
            {
                IPInterfaceProperties IPInterfaceProperties
                    = NetworkInterf.GetIPProperties();
                UnicastIPAddressInformationCollection UnicastIPAddressInformationCollection
                    = IPInterfaceProperties.UnicastAddresses;
                foreach (UnicastIPAddressInformation UnicastIPAddressInformation
                    in UnicastIPAddressInformationCollection)
                {
                    if (UnicastIPAddressInformation.Address.AddressFamily ==
                        AddressFamily.InterNetwork)
                    {
                        if (IPInterfaceProperties.GatewayAddresses.Count > 0)
                        {
                            strLocalHAddr = UnicastIPAddressInformation.Address.ToString();
                        }
                    }
                }
            }
            if (strLocalHAddr == null)
            {
                MessageBox.Show("没有网络连接");
            }
            else
            {
                strLocalHAddr = strLocalHAddr.Substring(0, strLocalHAddr.LastIndexOf('.') + 1) + "255";
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            label1.Text = "正在查找服务器……";
            udpDataSendBuf = new byte[1024];
            ThreadStart workStart = new ThreadStart(thrSearchServer);
            Thread workThread = new Thread(workStart);
            workThread.IsBackground = true;
            workThread.Start();
        }
        public static void ReceiveServerCallback(IAsyncResult ar)
        {
            try
            {
                EndPoint tempRemoteEP = (EndPoint)ipeServerRemoteIp;
                if (!udpSockClosed)
                {
                    //关闭Socket对象也将引发此事件，故要进行区分
                    iUdprecvDataLen = sockUdpSearchRecv.EndReceiveFrom(ar, ref tempRemoteEP);
                    ipeServerRemoteIp = (IPEndPoint)tempRemoteEP;
                    strInfo = Encoding.UTF8.GetString(udpRecvDataBuf, 0, recv_data_len);
                    mrEvent_GotServer.Set();
                }
            }
            catch (SocketException se)
            {
                MessageBox.Show(se.Message);
            }
        }
        protected override void DefWndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case NO_SERVER:
                    label1.Text="已经行查找，服务器尚未找到！";
                        break;
                case FOUND_SERVER:
                        label1.Text = string.Format("服务器{0}在线，欢迎你",
                            ipeServerRemoteIp.Address);
                        break;
                default:
                        base.DefWndProc(ref m);
                        break;
            }
        }
        static void thrSearchServer()
        {
            mutSearch = null;
            bool mutExists = true;
            try
            {
                mutSearch = Mutex.OpenExisting("SearchServer by UDP");
            }
            catch (Exception)
            {
                mutExists = false;
            }
            if (mutExists)
            {
                //互斥量已存在，代表此代码线程体正在运行，本线程退出。
                return;
            }
            else
            {
                //互斥量不存在，须创建并获得拥有
                mutSearch = new Mutex(true, "SearchServer by UDP");
            }

            //使用事件对象准备
            mrEvent_GotServer = new ManualResetEvent(false);
            udpSockClosed = false;
            //创建使用的Socket对象初始化一个Socket协议
            sockUdpSearchSend = new Socket(AddressFamily.InterNetwork,
                SocketType.Dgram, ProtocolType.Udp);
            //设置该Socket实例的发送形式
            sockUdpSearchSend.SetSocketOption(SocketOptionLevel.Socket,
                SocketOptionName.Broadcast, 1);
            RemoteServerIpEP = new IPEndPoint(IPAddress.Parse(strLocalHAddr), 9095);
            ipeServerRemoteIp = new IPEndPoint(IPAddress. Any, 0);
            udpRecvDataBuf = new byte[1024];
            remoteEp = new IPEndPoint(IPAddress.Any, 0);
            sockUdpSearchRecv = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint iep = new IPEndPoint(IPAddress.Any, 9096);
            //接收服务端UDP的9096端口
            sockUdpSearchRecv.Bind(iep);
            sockUdpSearchRecv.BeginReceiveFrom(udpRecvDataBuf, 0, 1024, SocketFlags.None, ref remoteEp, ReceiveServerCallback, new object());
            byte[] send_data_buf;
            send_data_buf = new byte[1024];
            int send_data_len;
            try
            {
                bool noUdpReply = true;
                byte[] b_txt;
                b_txt = Encoding.UTF8.GetBytes("are you online?");
                send_data_len = b_txt.Length;
                Buffer.BlockCopy(b_txt, 0, udpDataSendBuf, 0, send_data_len);
                for (int i = 0; (i < 10) && noUdpReply; i++)
                {
                    //发送100个UDP数据包，相当于线程最多运行20秒钟
                    sockUdpSearchSend.SendTo(udpDataSendBuf,
                        send_data_len, SocketFlags.None, RemoteServerIpEP);
                    //等待时间间隔为200毫秒
                    noUdpReply = !mrEvent_GotServer.WaitOne(200);
                }
                //如果没有收到任何回复，则通知窗体没有启动服务，收到回复则显示服务器存在
                if (noUdpReply)
                {
                    SendMessage(main_wnd_handle, NO_SERVER, 100, 100);
                }
                else
                {
                    SendMessage(main_wnd_handle, FOUND_SERVER, 100, 100);
                }
                //关闭Socket对象
                udpSockClosed = true;
                sockUdpSearchSend.Close();
                sockUdpSearchRecv.Close();
            }
            catch (Exception e2)
            {
                MessageBox.Show(e2.Message);
            }
            //线程结束，须释放互斥量
            mutSearch.ReleaseMutex();
            mutSearch.Close();
        }
    }
}
