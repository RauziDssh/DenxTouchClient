using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DenxTouchClient
{
    public enum Room { box233, box234, box412, Lab, None}

    static class RoomExt
    {
        public static string DisplayName(this Room room)
        {
            string[] names = { "box233", "box234", "box412","Lab","None" };
            return names[(int)room];
        }

        public static Room DisplayRoom(string roomName)
        {
            switch (roomName)
            {
                case "box233":
                    return Room.box233;
                case "box234":
                    return Room.box234;
                case "box412":
                    return Room.box412;
                case "Lab":
                    return Room.Lab;
                default:
                    return Room.None;
            }
        }
    }
}
