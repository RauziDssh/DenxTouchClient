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
        Subject<string> felicaInfoStream;//ICカードのIDが流れてくる

        Room room;//設置対象の部屋

        string doshishaPMm = "";//同志社大学の学生証のPMm

        public Form1()
        {
            InitializeComponent();

            timer1.Interval = 500;
            timer1.Enabled = true;

            var availavleRoom = new Room[] { Room.box233, Room.box234, Room.box412 };
            foreach (var element in availavleRoom)
            {
                var name = element.DisplayName();
                var tsmi = new ToolStripMenuItem();
                tsmi.Text = name;
                tsmi.Click += (s, o) =>
                {
                    room = element;
                    toolStripMenuItem1_SelectRoom.Text = $"選択：{name}";
                    Properties.Settings.Default.PreserveRoomNum = (int)room;
                    Properties.Settings.Default.Save();
                };
                toolStripMenuItem1_SelectRoom.DropDownItems.Add(tsmi);
            }

            var proom = Properties.Settings.Default.PreserveRoomNum;
            if (proom == -1)
            {
                room = Room.None;
                toolStripMenuItem1_SelectRoom.Text = $"選択：なし";
            }
            else
            {
                room = (Room)proom;
                toolStripMenuItem1_SelectRoom.Text = $"選択：{room.DisplayName()}";
            }

            felicaInfoStream = new Subject<string>();
            felicaInfoStream
                .DistinctUntilChanged()
                .Where(v => v != null)
                .Do(v => Console.WriteLine(v))
                .Repeat()
                .Subscribe(
                    v => 
                    {
                        if(room == Room.None) { this.ShowMessage("部屋が設定されていません");return; }
                        SendMessage(v);
                    }
                );

            ShowMessage("学生証をかざしてください","DENX Touch Client");
        }

        private async void SendMessage(string CardId)
        {
            var status = await this.GetStatusAsync(CardId);
            if(status.room != Room.None)
            {
                var InOut = status.InOrOut ? "入室" : "退室";
                ShowMessage($"{InOut}:{status.username}","ICカードが読み込まれました");
            }
            else
            {
                ShowMessage("登録が必要です", "未登録ユーザ");
                System.Diagnostics.Process.Start(status.username);
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

        //APIを叩いて各値をStatusにまとめて返す(falseのときの返し方については要再検討)
        public async Task<Status> GetStatusAsync(string cardId)
        {
            var client = new System.Net.Http.HttpClient();
            var roomName = room.DisplayName();
            var status = await client.GetStringAsync($"http://localhost:3000/touch?cardId={cardId}&place={roomName}");
            dynamic j = JsonConvert.DeserializeObject(status);
            if((bool)j.success)
            {
                return new Status(
                    RoomExt.DisplayRoom((string)j.place),
                    (string)j.inOrOut == "in"?true:false,
                    (string)j.twitterId);
            }
            else
            {
                var urlStr = (string)j.url;
                return new Status(Room.None,false,$"http://localhost:3000{urlStr}");//あとでなんとかする
            }
        }

        //終了ボタン押し下時の動作
            //今後確認ダイアログみたいなのを表示するようにする
        private void toolStripMenuItem_Exit_Click(object sender, EventArgs e)
        {
            notifyIcon1.Visible = false;
            Application.Exit();
        }

        //一定間隔ごとにPaSoriに値を求めに行く
            //例外のメッセージの文字化けがひどいのでなんとかしないといけない
        private void timer1_Tick(object sender, EventArgs e)
        {
            GetCardId();
        }

        public void GetCardId()
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

                    Console.WriteLine(pdataStr);

                    if (pdataStr == doshishaPMm)
                    {
                        felicaInfoStream.OnNext(dataStr);
                    }
                }
            }
            catch (FelicaNotLoadedExeption e)
            {
                felicaInfoStream.OnNext(null);
            }
            catch (FelicaNotConnectedExeption e)
            {
                this.ShowMessage("ICカードリーダーを正しく接続してください");
            }
            catch (Exception e)
            {

            }
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("http://localhost:3000/places");
        }
    }

    public class Status
    {
        public Room room { get; private set; }
        public string username { get; private set; }
        public bool InOrOut { get; private set; }

        public Status(Room r,bool io ,string t)
        {
            this.room = r;
            this.InOrOut = io;
            this.username = t;
        }
    }
}
