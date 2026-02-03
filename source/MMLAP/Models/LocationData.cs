using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMLAP.Models
{
    public class LocationData
    {
        public required int Id { get; set; }
        public required string Category { get; set; }
        public required string Name { get; set; }
        public required ulong Address { get; set; }
        public required int AddressBit { get; set; }
        public required bool IsMissable { get; set; }
        public required LevelData DefaultLevelData { get; set; }
        public required ItemData DefaultItemData { get; set; }
        public string? ChestItemSignatureAddress { get; set; }
        public string? TextBoxStartAddress { get; set; }
        public LocationData(
            int id,
            string category,
            string name,
            ulong address,
            int addressBit,
            bool isMissable,
            LevelData defaultLevelData,
            ItemData defaultItemData,
            string? chestItemSignatureAddress = null,
            string? textBoxStartAddress = null
        )
        {
            Id = id;
            Category = category;
            Name = name;
            Address = address;
            AddressBit = addressBit;
            IsMissable = isMissable;
            DefaultLevelData = defaultLevelData;
            DefaultItemData = defaultItemData;
            ChestItemSignatureAddress = chestItemSignatureAddress;
            TextBoxStartAddress = textBoxStartAddress;
        }
    }
}
