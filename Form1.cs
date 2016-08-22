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
using System.Media;
using Microsoft.Win32;

namespace DenxTouchClient
{
    public partial class Form1 : Form
    {
        Subject<string> felicaInfoStream;//ICカードのIDが流れてくる

        Room room;//設置対象の部屋

        string doshishaPMm = "0120220427674EFF";//学生証のPMm
        string serverUrl = @"http://touch.tsuno.co";

        SoundPlayer se_in;
        SoundPlayer se_out;

        public Form1()
        {

            InitializeComponent();

            //SEの設定
            se_in = new SoundPlayer(Properties.Resources.se_in);
            se_out = new SoundPlayer(Properties.Resources.se_out);

            //タイマーの設定
            timer1.Interval = 500;
            timer1.Enabled = true;

            //利用可能な部屋を登録
            var availavleRoom = new Room[] { Room.box233, Room.box234, Room.box412 };
            //利用可能な部屋をそれぞれ項目として使用可能にする
            foreach (var element in availavleRoom)
            {
                var name = element.DisplayName();
                var tsmi = new ToolStripMenuItem();
                tsmi.Text = name;
                //部屋名クリック時の処理
                tsmi.Click += (s, o) =>
                {
                    room = element;
                    toolStripMenuItem1_SelectRoom.Text = $"選択：{name}";
                    Properties.Settings.Default.PreserveRoomNum = (int)room;
                    Properties.Settings.Default.Save();
                };
                toolStripMenuItem1_SelectRoom.DropDownItems.Add(tsmi);
            }

            //部屋の設定の読み込み
            var proom = Properties.Settings.Default.PreserveRoomNum;
            if (proom == -1)
            {
                //部屋の設定が行われていないとき
                room = Room.None;
                toolStripMenuItem1_SelectRoom.Text = $"選択：なし";
                this.ShowMessage("部屋が設定されていません");
            }
            else
            {
                //部屋の設定が完了しているとき
                room = (Room)proom;
                toolStripMenuItem1_SelectRoom.Text = $"選択：{room.DisplayName()}";
                this.ShowMessage("学生証をかざしてください", "DENX Touch Client");
            }

            //ICカードで読み込んだIDmが流れてくるストリーム
            felicaInfoStream = new Subject<string>();
            felicaInfoStream
                .DistinctUntilChanged()//同じものが流れて来たら弾く
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

            SystemEvents.SessionEnding +=
                new SessionEndingEventHandler(SystemEvents_SessionEnding);
        }

        //ユーザの状態を取得して帰ってきたStatusを処理する
        private async void SendMessage(string CardId)
        {
            var status = await this.GetStatusAsync(CardId);
            if(status == null) { return; } 
            if(status.room != Room.None)
            {
                ShowStatus(status);
            }
            else
            {
                ShowMessage("登録が必要です", "未登録ユーザ");
                System.Diagnostics.Process.Start(status.username);

            }
        }

        //正常にStatusを取得できた場合に行う処理
        private void ShowStatus(Status status)
        {
            var InOut = status.InOrOut ? "入室" : "退室";
            ShowMessage($"{InOut}:{status.username}", "ICカードが読み込まれました");
            if (status.InOrOut)
            {
                se_in.Play();
            }
            else
            {
                se_out.Play();
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
            try
            {
                var client = new System.Net.Http.HttpClient();
                var roomName = room.DisplayName();
                var status = await client.GetStringAsync($"{serverUrl}/touch?cardId={cardId}&place={roomName}");
                dynamic j = JsonConvert.DeserializeObject(status);
                Console.WriteLine(status.ToString());
                if ((bool)j.success)
                {
                    return new Status(
                        RoomExt.DisplayRoom((string)j.place),
                        (string)j.inOrOut == "in" ? true : false,
                        (string)j.username);
                }
                else
                {
                    var urlStr = (string)j.url;
                    return new Status(Room.None, false, $"{serverUrl}{urlStr}");//あとでなんとかする
                }
            }
            catch(HttpRequestException e)
            {
                this.ShowMessage("サーバに繋がりませんでした。管理者に問い合わせてください。");
                return null;
            }
            catch(Exception e)
            {
                this.ShowMessage("レスポンスエラー");
                return null;
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

        //CardIDの取得
            //IDmが流れて来たらfelicaInfoStreamに流す
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

                    //Console.WriteLine(pdataStr);

                    //pMmが学生証のものかどうかをチェック
                    if (pdataStr == doshishaPMm)
                    {
                        felicaInfoStream.OnNext(dataStr);
                    }
                    else
                    {
                        this.ShowMessage("このICカードは学生証ではありません");
                        return;
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
            System.Diagnostics.Process.Start($"{serverUrl}/places");
        }

        private void SystemEvents_SessionEnding(object sender, SessionEndingEventArgs e)
        {
            string s = "";
            if (e.Reason == SessionEndReasons.Logoff)
            {
                s = "ログオフしようとしています。";
            }
            else if (e.Reason == SessionEndReasons.SystemShutdown)
            {
                s = "シャットダウンしようとしています。";
            }
            if (MessageBox.Show($"{s}\n\n{room.DisplayName()}にいるメンバーを全員退出状態にしますか？",
                "終了時アクション", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                //全員退出状態にする
                
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            //イベントを解放する
            //フォームDisposeメソッド内の基本クラスのDisposeメソッド呼び出しの前に
            //記述してもよい
            SystemEvents.SessionEnding -=
                new SessionEndingEventHandler(SystemEvents_SessionEnding);
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
