namespace MMLAP.Models
{
    public class LocationData
    {
        public string Name { get; set; }
        public string Category { get; set; }
        public LevelData LevelData { get; set; }
        public ItemData DefaultItemData { get; set; }
        public ulong CheckAddress { get; set; }
        public int? CheckBitNumber { get; set; }
        public byte? CheckByteValue { get; set; }
        public bool IsMissable { get; set; }
        public ulong? ChestItemSignatureAddress { get; set; }
        public ulong? TextBoxStartAddress { get; set; }
        public LocationData(
            string name,
            string category,
            LevelData levelData,
            ItemData defaultItemData,
            ulong checkAddress,
            int? checkBitNumber,
            byte? checkByteValue,
            bool isMissable = false,
            ulong? chestItemSignatureAddress = null,
            ulong? textBoxStartAddress = null
        )
        {
            Name = name;
            Category = category;
            LevelData = levelData;
            DefaultItemData = defaultItemData;
            CheckAddress = checkAddress;
            CheckBitNumber = checkBitNumber;
            CheckByteValue = checkByteValue;
            IsMissable = isMissable;
            ChestItemSignatureAddress = chestItemSignatureAddress;
            TextBoxStartAddress = textBoxStartAddress;
        }
    }
}
