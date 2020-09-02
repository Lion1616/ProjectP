using BlubLib.Serialization;
using NeoNetsphere.Network.Serializers;
using System;

namespace NeoNetsphere.Network.Data.Chat
{
  [BlubContract]
  public class UserDataItemDto
  {
        [BlubMember(0)]
        public ItemNumber ItemNumber { get; set; }

        [BlubMember(1)]
        public ItemPriceType PriceType { get; set; }

        [BlubMember(2)]
        public int Unk2 { get; set; }

        [BlubMember(3)]
        public short Unk3 { get; set; }

        [BlubMember(4)]
        public byte Color { get; set; }

        [BlubMember(5, typeof(ArrayWithIntPrefixSerializer))]
        public uint[] Effects { get; set; }

        [BlubMember(6)]
        public int Unk4 { get; set; }

        [BlubMember(7)]
        public int Unk5 { get; set; }

        public UserDataItemDto()
        {
            Effects = Array.Empty<uint>();
        }
    }
}
