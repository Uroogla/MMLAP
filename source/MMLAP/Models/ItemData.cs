namespace MMLAP.Models
{
    public class ItemData(
        Enums.ItemCategory category,
        string name,
        short? quantity = null,
        byte? itemCode = null,
        AddressData? inventoryAddressData = null,
        bool isFiller = false
    )
    {
        public Enums.ItemCategory Category { get; set; } = category;
        public string Name { get; set; } = name;
        public short? Quantity { get; set; } = quantity;
        public byte? ItemCode { get; set; } = itemCode;
        public AddressData? InventoryAddressData { get; set; } = inventoryAddressData;
        public bool IsFiller { get; set; } = isFiller;
    }
}
