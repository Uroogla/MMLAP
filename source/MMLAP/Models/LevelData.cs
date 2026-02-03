using System.Collections.Generic;

namespace MMLAP.Models
{
    public class LevelData
    {
        public required string AreaName { get; set; }
        public required string RoomName { get; set; }
        public required byte AreaCode { get; set; }
        public required byte RoomCode { get; set; }

        public LevelData(
            string areaName,
            string roomName,
            byte areaCode,
            byte roomCode
        )
        {
            AreaName = areaName;
            RoomName = roomName;
            AreaCode = areaCode;
            RoomCode = roomCode;
        }
    }
}
