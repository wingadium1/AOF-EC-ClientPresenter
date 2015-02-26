using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Presenter
{
    public partial class Form1 : Form
    {
        #region Variable

        //defaut address
        string IP = null;
        int PORT = 2505;

        //Threads
        System.Threading.Thread thConnecttoServer;

        //Biến dùng để gửi, nhận dữ liệu
        byte[] inputData = new byte[1024];

        TcpClient _client;
        public TcpClient Client
        {
            get { return _client; }
            set { _client = value; }
        }
        
        private int timeLeft;
        #endregion
        public Form1()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
        }

        private void splitContainer1_Panel2_Paint(object sender, PaintEventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            inputIP();
        }

        private void inputIP()
        {
            InputBoxResult test = InputBox.Show("Input server IP" + "\n" 
                  , "Input server IP", "Default", 100, 0);

            if (test.ReturnCode == DialogResult.OK)
            {
                IP = test.Text;
                // MessageBox.Show(IP);

                thConnecttoServer = new Thread(Connect);
                thConnecttoServer.IsBackground = true;
                thConnecttoServer.Start();
            }
        }

        private delegate void dlgStartTheQuestion();
        private void Connect()
        {
            try
            {

                Client = new TcpClient();
                Client.Connect(IPAddress.Parse(IP), PORT);

                while (true)//Trong khi vẫn còn kết nối
                {
                    //Nhận dữ liệu từ máy chủ
                    try
                    {
                        labelStatus.Text = "Connected to server IP:  " + IP;
                            byte[] dlNhan = new byte[1048576];
                            Client.GetStream().Read(dlNhan, 0, 1048576);

                            // tmp = Encoding.Unicode.GetString(dlNhan);
                            Utility.Message reciveMessage = ByteArrayToMessage(dlNhan);
                            switch (reciveMessage.type)
                            {
                                case (Utility.Message.Type.Quest):
                                    labelQuestion.Text = reciveMessage.x.question;
                                    labelAnswer.Text = "The true answer is : " + reciveMessage.x.ans;
                                    labelAnswer.Visible = false;
                                    if (null != reciveMessage.image)
                                    {
                                        MemoryStream ms = new MemoryStream(reciveMessage.image);
                                        Image returnImage = Image.FromStream(ms);
                                        Bitmap image = null;
                                        image = new Bitmap(returnImage);
                                        pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
                                        pictureBox1.ClientSize = new Size(600, 400);
                                        pictureBox1.Image = (Image)image;
                                    }
                                    {

                                        StartTheQuestion();
                                    }
                                    break;
                                case (Utility.Message.Type.ShowAns):
                                    labelAnswer.Visible = true;
                                    break;
                            }
                        }
                    

                    catch
                    {
                        MessageBox.Show("Lost Connect!!!! + IP");
                        Connect();
                    }
                }
            }
            catch
            {
                MessageBox.Show("Lost Connect!!!!");
                Connect();
            }

        }
        private void SendData(Utility.Message data)
        {
            try
            {
                System.IO.MemoryStream fs = new System.IO.MemoryStream();
                System.Runtime.Serialization.Formatters.Binary.BinaryFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                formatter.Serialize(fs, data);
                byte[] buffer = fs.ToArray();
                Client.GetStream().Write(buffer, 0, buffer.Length);

            }
            catch (Exception er)
            {


            }

        }

        private string LocalIPAddress()
        {
            IPHostEntry host;
            string localIP = "";
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIP = ip.ToString();
                    break;
                }
            }
            return localIP;
        }

        private byte[] MessageToByteArray(Utility.Message message)
        {
            System.IO.MemoryStream fs = new System.IO.MemoryStream();
            System.Runtime.Serialization.Formatters.Binary.BinaryFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            formatter.Serialize(fs, message);
            return fs.ToArray();
        }

        private static Utility.Message ByteArrayToMessage(byte[] arrBytes)
        {
            System.IO.MemoryStream ms = new System.IO.MemoryStream();

            System.Runtime.Serialization.Formatters.Binary.BinaryFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            ms.Write(arrBytes, 0, arrBytes.Length);
            ms.Seek(0, System.IO.SeekOrigin.Begin);
            Utility.Message reciveMessage = (Utility.Message)formatter.Deserialize(ms);
            return reciveMessage;
        }

        delegate void StartQuestionDelegate();
        private void StartTheQuestion()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new StartQuestionDelegate(StartTheQuestion));
            }
            else
            {
                timeLeft = 100;
                labelTimer.Text = "10''00";
                timerCountDown.Interval = 100;
                timerCountDown.Start();
            }
        }

        delegate void SetTextCallback(string text);
        public void SetText(string text)
        {
            // InvokeRequired required compares the thread ID of the
            // calling thread to the thread ID of the creating thread.
            // If these threads are different, it returns true.
            if (this.labelTimer.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetText);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                this.labelTimer.Text = text;
            }
        }

        private void timerCountDown_Tick(object sender, EventArgs e)
        {
            if (timeLeft > 0)
            {
                // Display the new time left 
                // by updating the Time Left label.
                timeLeft -= 1;
                labelTimer.Text = String.Format("{0}''{1}", timeLeft / 10, (timeLeft % 10));
            }
            else
            {
                // If the user ran out of time, stop the timer, show  and fill in the answers.
                timerCountDown.Stop();
                labelTimer.Text = "Time's up!";
                // MessageBox.Show("Time's up");
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            Client.Close();
            thConnecttoServer.Abort();
            Application.Exit();
        }

    }
}
