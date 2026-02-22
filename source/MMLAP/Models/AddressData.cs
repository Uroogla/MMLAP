namespace MMLAP.Models
{
    public class AddressData(
        ulong address,
        int? bitNumber = null,
        int? byteLength = null
        )
    {
        public ulong Address { get; set; } = address;
        public int? BitNumber { get; set; } = bitNumber;
        public int? ByteLength { get; set; } = byteLength;
    }
}