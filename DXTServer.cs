using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net.Http;

namespace DenxTouchClient
{
    static class DXTServer
    {
        public const string serverUrl = @"http://touch.tsuno.co";

        //APIを叩いて各値をStatusにまとめて返す(falseのときの返し方については要再検討)
        public static async Task<Status> GetStatusAsync(string cardId,Room room)
        {
            try
            {
                var client = new System.Net.Http.HttpClient();
                var roomName = room.DisplayName();
                var status = await client.GetStringAsync($"{serverUrl}/touch?cardId={cardId}&place={roomName}");
                
                dynamic j = JsonConvert.DeserializeObject(status);
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
                throw new HttpRequestException();
            }
            catch(Exception e)
            {
                throw new Exception("Responce Error");
            }
        }

        //全員退出状態にするAPIを叩く
        public static async void OutAll(Room room)
        {
            var client = new System.Net.Http.HttpClient();
            var status = await client.GetStringAsync($"{serverUrl}/outAll?&place={room.DisplayName()}");
        }
        
    }
}
