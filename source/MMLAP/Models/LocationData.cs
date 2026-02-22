using static MMLAP.Models.Enums;

namespace MMLAP.Models
{
    public class LocationData(
        int id,
        string name,
        Enums.LocationCategory category,
        LevelData levelData,
        ItemData defaultItemData,
        AddressData checkAddressData,
        byte? checkByteValue,
        bool isMissable = false,
        ulong? chestItemSignatureAddress = null,
        ulong? textBoxStartAddress = null
    )
    {
        public int Id { get; set; } = id;
        public string Name { get; set; } = name;
        public LocationCategory Category { get; set; } = category;
        public LevelData LevelData { get; set; } = levelData;
        public ItemData DefaultItemData { get; set; } = defaultItemData;
        public AddressData CheckAddressData { get; set; } = checkAddressData;
        public byte? CheckByteValue { get; set; } = checkByteValue;
        public bool IsMissable { get; set; } = isMissable;
        public ulong? ChestItemSignatureAddress { get; set; } = chestItemSignatureAddress;
        public ulong? TextBoxStartAddress { get; set; } = textBoxStartAddress;
    }
}
