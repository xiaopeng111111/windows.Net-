using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace _01服务发现_S
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        public static Socket sockUdpRecv;
        public static Socket sockUdpSend;
        public static byte[] udpRecvDataBuf;
        public static byte[] udpSendDataBuf;
        public static IntPtr main_wnd_handle;
       
        public class Obj_one 
        {
        }
        public static Obj_one ob_1;
        private void Form1_Load(object sender, EventArgs e)
        {
            main_wnd_handle = this.Handle;//窗体句柄初始化
            udpRecvDataBuf = new byte[1024];
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ipeRemoteClient = new IPEndPoint(IPAddress.Any, 9095);
            //初始化一个发送广播和制定端口的网络端口实例
            sockUdpSend = new Socket(AddressFamily.InterNetwork,
                SocketType.Dgram, ProtocolType.Udp);
            //初始化一个Scoket协议，用于发送数据
            //开始进行UDP数据包接收，接收到数据包就进行回复，表示服务器存在
            sockUdpRecv = new Socket(AddressFamily.InterNetwork,
                SocketType.Dgram, ProtocolType.Udp);

            //初始化一个发送广播和制定端口的网络端口实例
            EndPoint iep = new IPEndPoint(IPAddress.Any, 9095);
            //初始化一个发送广播和制定端口的网络端口实例
            sockUdpRecv.Bind(iep);//绑定这个实例
            ob_1 = new Obj_one();
            sockUdpRecv.BeginReceiveFrom(udpRecvDataBuf, 0, 1024,
                SocketFlags.None, ref iep, ReceiveUdpCallback, ob_1);
            
        }
        public static IPEndPoint ipeRemoteClient;
        public static int iUdprecvDataLen;
        public static string strInfo;
        public void ReceiveUdpCallback(IAsyncResult ar)
        {
            try
            {
                EndPoint tempRemoteEP = (EndPoint)ipeRemoteClient;
                iUdprecvDataLen = sockUdpRecv.EndReceiveFrom(ar, ref tempRemoteEP);
                strInfo = Encoding.UTF8.GetString(udpRecvDataBuf, 0, iUdprecvDataLen);
                udpSendDataBuf = new byte[1024];
                ipeRemoteClient = (IPEndPoint)tempRemoteEP;
                IPEndPoint iep = new IPEndPoint(ipeRemoteClient.Address, 9096);
                byte[] databyte = Encoding.UTF8.GetBytes("hello from server");
                sockUdpSend.SendTo(databyte, iep);
                //继续接受UDP数据包
                sockUdpRecv.BeginReceiveFrom(udpRecvDataBuf, 0, 1024,
                SocketFlags.None, ref tempRemoteEP, ReceiveUdpCallback, ob_1);

            }
            catch (SocketException se)
            {
                MessageBox.Show(se.Message);
            }
        }
        
    }
}
