using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;

namespace DenxTouchClient
{
    class RoomManager
    {
        public Room room { get; private set; }
        public Subject<Status> statusStream { get; set; }

        public RoomManager(Room r)
        {
            this.room = r;
            statusStream = new Subject<Status>();
        }

        public void ChangeRoom(Room r)
        {
            this.room = r;
        }

        //ユーザの状態を取得して帰ってきたStatusを処理する
        public async void SendMessage(string CardId)
        {
            try
            {
                var status = await DXTServer.GetStatusAsync(CardId, room);
                if (status == null) { return; }
                if (status.room != Room.None)
                {
                    statusStream.OnNext(status);
                }
                else
                {
                    var f = (Form1)Form1.ActiveForm;
                    f.ShowErrorMessage("未登録ユーザです。登録が必要です。");
                    System.Diagnostics.Process.Start(status.username);
                }
            }
            catch (Exception e)
            {
                var f = (Form1)Form1.ActiveForm;
                f.ShowErrorMessage(e.ToString());
            }
        }
    }

    public class Status
    {
        public Room room { get; private set; }
        public string username { get; private set; }
        public bool InOrOut { get; private set; }

        public Status(Room r, bool io, string t)
        {
            this.room = r;
            this.InOrOut = io;
            this.username = t;
        }
    }
}
