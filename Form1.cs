using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using FelicaLib;
using System.Reactive.Subjects;
using System.Reactive.Linq;
using System.Net.Http;
using Newtonsoft.Json;

namespace DenxTouchClient
{
    public partial class Form1 : Form
    {
        Subject<string> felicaInfoStream;

        public Form1()
        {
            InitializeComponent();

            timer1.Interval = 500;
            timer1.Enabled = true;

            felicaInfoStream = new Subject<string>();

            felicaInfoStream
                .Where(v => v != null)
                .DistinctUntilChanged()
                .Subscribe(v => 
                    {
                        SendMessage(v);
                    }
                );

            ShowMessage("学生証をかざしてください","DENX Touch Client");
        }

        private async void SendMessage(string CardId)
        {
            var status = await this.GetStatusAsync(CardId);
            if(status.room != Room.Absent)
            {
                ShowMessage(status.text,"ICカードが読み込まれました");
            }
            else
            {
                ShowMessage("登録が必要です", "未登録ユーザ");
                System.Diagnostics.Process.Start(status.text);
            }
        }

        private void ShowMessage(string message,string title)
        {
            notifyIcon1.BalloonTipTitle = title;
            notifyIcon1.BalloonTipText = message;
            notifyIcon1.ShowBalloonTip(200);
        }

        private void ShowMessage(string message)
        {
            notifyIcon1.BalloonTipTitle = "通知";
            notifyIcon1.BalloonTipText = message;
            notifyIcon1.ShowBalloonTip(200);
        }

        public async Task<Status> GetStatusAsync(string cardId)
        {
            var client = new System.Net.Http.HttpClient();
            var s = await client.GetStringAsync("http://192.168.0.131:3000/cardId?cardId=" + cardId);
            dynamic j = JsonConvert.DeserializeObject(s);
            if((bool)j.success)
            {
                return new Status(true, (string)j.twitterId);
            }
            else
            {
                var urlStr = (string)j.url;
                return new Status(false, urlStr);
            }
        }

        private void toolStripMenuItem_Exit_Click(object sender, EventArgs e)
        {
            notifyIcon1.Visible = false;
            Application.Exit();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            try
            {
                using (FelicaLib.Felica f = new FelicaLib.Felica())
                {

                    f.Polling(0xFFFF);
                    byte[] data = f.IDm();
                    byte[] pdata = f.PMm();

                    String dataStr = "";
                    for (int i = 0; i < data.Length; i++)
                    {
                        dataStr += data[i].ToString("X2");
                    }

                    String pdataStr = "";
                    for (int i = 0; i < pdata.Length; i++)
                    {
                        pdataStr += pdata[i].ToString("X2");
                    }

                    felicaInfoStream.OnNext(dataStr);

                }
            }
            catch (Exception ex)
            {
                //felicaInfoStream.OnError(ex);
                //Console.WriteLine("Error");
            }
        }
    }

    public enum Room { K233, K234, I412, Lab, Absent }

    public class Status
    {
        public Room room { get; private set; }
        public string text { get; private set; }

        public Status(bool room, string text)
        {
            this.room =
                room ? Room.K233 : Room.Absent;
            this.text = text;
        }
    }
}
