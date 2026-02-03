using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMLAP.Models
{
    public class ItemData
    {
        public required string Name { get; set; }
        public required string Category { get; set; }
        public byte? ItemCode { get; set; }
        public string? InventoryAddress { get; set; }
        public int? InventoryAddressBitNumber { get; set; }
        public int? Quantity { get; set; }
        public bool IsSellable { get; set; }

        public ItemData(
            string name,
            string category,
            byte? code = null,
            string? address = null,
            int? addressBit = null,
            int? quantity = null,
            bool isSellable = false
        )
        {
            Name = name;
            Category = category;
            ItemCode = code;
            InventoryAddress = address;
            InventoryAddressBitNumber = addressBit;
            Quantity = quantity;
            IsSellable = isSellable;
        }
    }
}
