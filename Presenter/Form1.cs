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
        private string videoFolder = System.IO.Directory.GetCurrentDirectory() + @"\Video";
        //defaut address
        string IP = null;
        int PORT = 2505;

        //Threads
        System.Threading.Thread thConnecttoServer;

        //Biến dùng để gửi, nhận dữ liệu
        byte[] inputData = new byte[1024];

        TcpClient _client;

        private List<Label> listName = new List<Label>();
        private List<Label> listAns = new List<Label>();
        int current = 0;
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
            progressBar1.Minimum = 0;
            progressBar1.Maximum = 100;
            listName.Add(label1);
            listName.Add(label2);
            listName.Add(label3);
            listName.Add(label4);

            listAns.Add(lbAns1);
            listAns.Add(lbAns2);
            listAns.Add(lbAns3);
            listAns.Add(lbAns4);

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
                Console.WriteLine("Fucking bug");
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
                                    String quest = reciveMessage.x.question;
                                    if (quest.Length > 80)
                                    {
                                        try
                                        {
                                            labelQuestion.Text = quest.Substring(0, quest.LastIndexOf(" ", 79));
                                            labelQuestion2.Text = quest.Substring(quest.LastIndexOf(" ", 79));
                                        }
                                        catch (Exception)
                                        {
                                            labelQuestion.Text = quest.Substring(0,79);
                                            labelQuestion2.Text = quest.Substring(79);
                                        }
                                    }
                                    else
                                    {
                                        labelQuestion.Text = quest;
                                        labelQuestion2.Text = "";
                                    }
                                    labelAnswer.Text = reciveMessage.x.ans;
                                    if (null != reciveMessage.image)
                                    {
                                        MemoryStream ms = new MemoryStream(reciveMessage.image);
                                        Image returnImage = Image.FromStream(ms);
                                        Bitmap image = null;
                                        image = new Bitmap(returnImage);
                                        pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
                                        pictureBox1.ClientSize = new Size(600, 400);
                                        pictureBox1.Image = (Image)image;
                                        current = 0;
                                    }
                                    else
                                    {
                                        pictureBox1.Image = null;
                                    }
                                    StartTheQuestion();
                                    if (reciveMessage.recount)
                                    {
                                        Console.WriteLine(reciveMessage.recount.ToString() + reciveMessage.x.questionTime);
                                        timeLeft = reciveMessage.x.questionTime * 10;
                                        
                                    }

                                    break;
                                case (Utility.Message.Type.Ans):
                                    timeLeft = 0;
                                    int time = timeLeft;
                                    labelTimer.Text = String.Format("{0}''{1}", time / 10, (time % 10));
                                    labelTimer.Text = reciveMessage.name + " rang";
                                    break;

                                case (Utility.Message.Type.PlayVideo):
                                    Console.Write(videoFolder + @"\" + reciveMessage.message);
                                    PlayMedia(videoFolder + @"\" + reciveMessage.message);
                                    break;
                                case (Utility.Message.Type.Cnt):
                                        timeLeft = 100;
                                    break;
                                default:
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
                labelTimer.Text = "10''00";
                timerCountDown.Interval = 100;
                timerCountDown.Start();
                progressBar1.Value = 100;
                progressBar1.Step = -1;
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
                progressBar1.PerformStep();
                labelTimer.Text = String.Format("{0}''{1}", timeLeft / 10, (timeLeft % 10));
                label8.Text = String.Format("{0}''{1}", timeLeft / 10, (timeLeft % 10));
            }
            else
            {
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            try
            {
                Client.Close();

                thConnecttoServer.Abort();
            }
            catch (Exception ex)
            {

            }
            finally
            {
                Application.Exit();
            }
        }

        delegate void PlayMediaDelegate(String mediaFile);
        private void PlayMedia(String mediaFile)
        {
            if (this.InvokeRequired)
            {
                PlayMediaDelegate d = new PlayMediaDelegate(PlayMedia);

                this.Invoke(d, new object[] { mediaFile });
            }
            else
            {
                var playForm = new Utility.Form1(mediaFile);
                playForm.Show();

            }
        }

    }
}
