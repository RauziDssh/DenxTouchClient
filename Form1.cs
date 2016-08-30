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
using System.Media;
using Microsoft.Win32;
using System.Reactive;
using Codeplex.Reactive;

namespace DenxTouchClient
{
    public partial class Form1 : Form
    {

        private SoundPlayer se_in { get; set; }
        private SoundPlayer se_out { get; set; }
        private RoomManager roomManager { get; set; }

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
                    //部屋が変更されたときの処理
                    InitializeRoom(element);

                    toolStripMenuItem1_SelectRoom.Text = $"選択：{name}";
                    Properties.Settings.Default.PreserveRoomNum = (int)element;
                    Properties.Settings.Default.Save();
                };
                toolStripMenuItem1_SelectRoom.DropDownItems.Add(tsmi);
            }

            //部屋の設定の読み込み
            var proom = Properties.Settings.Default.PreserveRoomNum;
            if (proom == -1)
            {
                toolStripMenuItem1_SelectRoom.Text = $"選択：なし";
                this.ShowErrorMessage("部屋が設定されていません");
            }
            else
            {
                //部屋の設定が完了しているとき
                roomManager = InitializeRoom((Room)proom);

                toolStripMenuItem1_SelectRoom.Text = $"選択：{roomManager.room.DisplayName()}";
                this.ShowMessage("学生証をかざしてください", "DENX Touch Client");
            }

            //ICカードで読み込んだIDmが流れてくるストリーム
            FelicaInputManager.felicaInfoStream
                .Where(_ => roomManager != null)
                .DistinctUntilChanged()//同じものが流れて来たら弾く
                .Where(v => v != null)
                .Do(v => Console.WriteLine(v))
                .Repeat()
                .Subscribe(
                    v => 
                    {
                        if(roomManager == null) { this.ShowErrorMessage("部屋が設定されていません");return; }
                        roomManager.SendMessage(v);
                    }
                    ,e => { ShowErrorMessage(e.ToString()); }
                );

            SystemEvents.SessionEnding +=
                new SessionEndingEventHandler(SystemEvents_SessionEnding);
        }

        private RoomManager InitializeRoom(Room r)
        {
            roomManager = new RoomManager(r,this);
            roomManager.statusStream
                .DistinctUntilChanged()
                .Subscribe(v =>
                {
                    var InOut = v.InOrOut ? "入室" : "退室";
                    ShowMessage($"{InOut}:{v.username}", "ICカードが読み込まれました");
                    if (v.InOrOut)
                    {
                        se_in.Play();
                    }
                    else
                    {
                        se_out.Play();
                    }
                }
                );
            return roomManager;
        }

        private void ShowMessage(string message,string title)
        {
            notifyIcon1.BalloonTipTitle = title;
            notifyIcon1.BalloonTipText = message;
            notifyIcon1.ShowBalloonTip(200);
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
            FelicaInputManager.pool();
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start($"{DXTServer.serverUrl}/places");
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
            if (MessageBox.Show($"{s}\n\n{roomManager.room.DisplayName()}にいるメンバーを全員退出状態にしますか？",
                "DenxTouchClient", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                //全員退出状態にする
                DXTServer.OutAll(roomManager.room);
                Application.Exit();
            }
            else
            {
                Application.Exit();
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

        public void ShowErrorMessage(string message)
        {
            notifyIcon1.BalloonTipTitle = "エラー";
            notifyIcon1.BalloonTipText = message;
            notifyIcon1.ShowBalloonTip(200);
        }

        private void toolStripMenuItem_info_Click(object sender, EventArgs e)
        {
            MessageBox.Show($"author:@Rauziii\nverison:{Application.ProductVersion}",
                "Information",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
    }
}
