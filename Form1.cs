using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;


namespace ServerSocket
{
    public partial class Form1 : Form
    {
        TcpListener server = null;
        TcpClient clientSocket = null;

        string date;
        private Dictionary<TcpClient, string> clientList = new Dictionary<TcpClient, string>();
        private int PORT = 5000;

        public Form1()
        {
            InitializeComponent();
            InitForm();
            serverIP.Text = GetLocalIP();
            serverPort.Text = PORT.ToString();
        }

        private void InitForm()
        {
            serverIP.Text = "";
            serverPort.Text = "";
            richTextBox1.Text = "";
            listBox1.Items.Clear();
            textBox2.Text = "";

        }

        private string GetLocalIP()
        {
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
            //Console.WriteLine(Dns.GetHostName().ToString());
            //Console.WriteLine(host.AddressList.Length.ToString());
            string localIP = string.Empty;

            for(int i=0; i<host.AddressList.Length; i++)
            {
                //Console.WriteLine("host.AddressList[i].ToString() : " + host.AddressList[i].ToString());
                //Console.WriteLine("host.AddressList[i].AddressFamily : " + host.AddressList[i].AddressFamily);
                //Console.WriteLine("AddressFamily.InterNetwork : " + AddressFamily.InterNetwork);
                if (host.AddressList[i].AddressFamily == AddressFamily.InterNetwork)
                {
                    localIP = host.AddressList[i].ToString();
                    break;
                }
            }
            return localIP;
        }

        private void InitSocket()
        {
            server = new TcpListener(IPAddress.Parse(serverIP.Text), int.Parse(serverPort.Text));
            clientSocket = default(TcpClient);
            server.Start();
            DisplayText("서버 구동 시작");

            while (true)
            {
                try
                {
                    clientSocket = server.AcceptTcpClient();
                    if(clientSocket == null)
                    {
                        Console.WriteLine("소켓이 널이여");
                    }

                    NetworkStream stream = clientSocket.GetStream();
                    byte[] buffer = new byte[1024]; // 버퍼
                    int bytes = stream.Read(buffer, 0, buffer.Length);
                    string userName = Encoding.Unicode.GetString(buffer, 0, bytes);
                    userName = userName.Substring(0, userName.IndexOf("$")); // client 사용자 명
                    DisplayText("서버 : [" + userName + "] 접속");

                    clientList.Add(clientSocket, userName);
                    SendMessageAll(userName + " 님이 입장하였습니다.", "", false);
                    SetUserList(userName, "I");

                    HandleClient h_client = new HandleClient(); // 클라이언트 추가
                    h_client.OnReceived += new HandleClient.MessageDisplayHandler(OnReceived);
                    h_client.OnDisconnected += new HandleClient.DisconnectedHandler(h_client_OnDisconnected);
                    h_client.startClient(clientSocket, clientList);

                }
                catch(SocketException se)
                {
                    MessageBox.Show(se.Message);
                    break;

                }catch(Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    break;
                }
            }
            clientSocket.Close();
            server.Stop();
        }

        private void h_client_OnDisconnected(TcpClient clientSocket)
        {
            if (clientList.ContainsKey(clientSocket))
            {
                clientList.Remove(clientSocket);
            }
        }

        private void OnReceived(string message, string userName)
        {
            if (message.Equals("LeaveChat"))
            {
                string displayMessage = "사용자 퇴장 : " + userName;

                DisplayText(displayMessage);
                SendMessageAll("LeaveChat", userName, true);
                SetUserList(userName, "D");
            }
            else
            {
                string displayMessage = "사용자로부터 : " + userName + " : " + message;
                DisplayText(displayMessage); // Server단에 출력
                SendMessageAll(message, userName, true); // 모든 Client에게 전송
            }
        }

        private void SetUserList(string userName, string div)
        {
            try
            {
                if (div.Equals("I"))
                {
                    if (listBox1.InvokeRequired)
                    {
                        listBox1.BeginInvoke((MethodInvoker)delegate
                        {
                            listBox1.Items.Add(userName);                        
                        });
                    }
                }
                else if (div.Equals("D"))
                {
                    if (listBox1.InvokeRequired)
                    {
                        listBox1.BeginInvoke((MethodInvoker)delegate
                        {
                            listBox1.Items.Remove(userName);

                        });
                    }
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
               
            }
        }

        private void SendMessageAll(string message, string userName, bool flag)
        {
            foreach (var pair in clientList)
            {
                date = DateTime.Now.ToString("yyyy.MM.dd. HH:mm:ss"); // 현재 날짜 받기

                TcpClient client = pair.Key as TcpClient;
                NetworkStream stream = client.GetStream();
                byte[] buffer = null;

                if (flag) //개별 메세지
                {
                    if (message.Equals("LeaveChat"))
                        buffer = Encoding.Unicode.GetBytes(userName + " 님이 대화방을 나갔습니다.");
                    else
                        buffer = Encoding.Unicode.GetBytes("[ " + date + " ] " + userName + " : " + message);
                }
                else
                {
                    buffer = Encoding.Unicode.GetBytes(message);
                }
                stream.Write(buffer, 0, buffer.Length); // 버퍼 쓰기
                stream.Flush();
            }
        }

        private void DisplayText(string text)
        {
            richTextBox1.Invoke((MethodInvoker) delegate
            {
                richTextBox1.AppendText(text + "\r\n");
            });
            richTextBox1.Invoke((MethodInvoker)delegate
            {
                richTextBox1.ScrollToCaret();
            });
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Thread server = new Thread(InitSocket);
            server.IsBackground = true;
            server.Start();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string userName = "관리자";
            string message = textBox2.Text.Trim();
            string displayMessage = "[" + userName + "] : " + message;

            DisplayText(displayMessage);
            SendMessageAll(message, userName, true);
            textBox2.Text = "";
        }

        private void textbox2_KeyUp(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.Enter)
            {
                button2_Click(this, e);
            }
        }

        //private void button3_Click(object sender, EventArgs e)
        //{
        //    string userName = "관리자";
        //    string message = "서버 강제 종료";
        //    string displayMessage = "[" + userName + "] : " + message;

        //    DisplayText(displayMessage);
        //    SendMessageAll(message, userName, true);

        //    if(clientSocket != null)
        //    {
        //        clientSocket.Close();
        //    }
        //    server.Stop();
        //}
    }
}
