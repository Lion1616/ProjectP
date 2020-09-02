using NeoNetsphere.Network.Data.Chat;
using System;
using System.Collections.Generic;
using System.Text;

namespace NeoNetsphere
{
    internal class ItemStatsManager
    {
        private readonly Player Player;
        public ItemStatsManager(Player player)
        {
            Player = player;
            Clothes = new ClotheStats(player);
            Weapons = new WeaponsStats(player);
            Skill = new SkillStats(player);
        }

        public ClotheStats Clothes { get; }
        public WeaponsStats Weapons { get; }
        public SkillStats Skill { get; }

        public ClotheStats GetClothesStats()
        {
            return Clothes;
        }

        public WeaponsStats GetWeaponsStats()
        {
            return Weapons;
        }

        public SkillStats GetSkillStats()
        {
            return Skill;
        }
    }

    internal abstract class IBaseStats
    {
        public IBaseStats(Player player)
        {
            Player = player;
        }

        public Player Player { get; set; }
    }

    internal class WeaponsStats : IBaseStats
    {
        public WeaponsStats(Player player)
               : base(player)
        {
        }

        public UserDataItemDto[] GetUserDataDto()
        {
            var UserData = new List<UserDataItemDto>();
            for (WeaponSlot i = 0; i < WeaponSlot.None; i++)
            {
                var xx = new UserDataItemDto
                {
                    ItemNumber = Player.CharacterManager.CurrentCharacter.Weapons.GetItem(i)?.ItemNumber ?? 0,
                    PriceType = Player.CharacterManager.CurrentCharacter.Weapons.GetItem(i)?.PriceType ?? 0,
                    Unk2 = 0,
                    Unk3 = 0,
                    Color = Player.CharacterManager.CurrentCharacter.Weapons.GetItem(i)?.Color ?? 0,
                    Effects = Player.CharacterManager.CurrentCharacter.Weapons.GetItem(i)?.GetItemEffectsInt() ?? new uint[0],
                    Unk4 = 0,
                    Unk5 = 0
                };

                UserData.Add(xx);
            }
            return UserData.ToArray();
        }
    }

    internal class SkillStats : IBaseStats
    {
        public SkillStats(Player player)
               : base(player)
        {
        }

        public UserDataItemDto[] GetUserDataDto()
        {
            var UserData = new List<UserDataItemDto>();

            var xx = new UserDataItemDto
            {
                ItemNumber = Player.CharacterManager.CurrentCharacter.Skills.GetItem(SkillSlot.Skill)?.ItemNumber ?? 0,
                PriceType = Player.CharacterManager.CurrentCharacter.Skills.GetItem(SkillSlot.Skill)?.PriceType ?? 0,
                Unk2 = 0,
                Unk3 = 0,
                Color = Player.CharacterManager.CurrentCharacter.Skills.GetItem(SkillSlot.Skill)?.Color ?? 0,
                Effects = Player.CharacterManager.CurrentCharacter.Skills.GetItem(SkillSlot.Skill)?.GetItemEffectsInt() ?? new uint[0],
                Unk4 = 0,
                Unk5 = 0
            };

            UserData.Add(xx);

            return UserData.ToArray();
        }
    }

    internal class ClotheStats : IBaseStats
    {
        public ClotheStats(Player player)
               : base(player)
        {
        }

        public UserDataItemDto[] GetUserDataDto()
        {
            var UserData = new List<UserDataItemDto>();

            for (CostumeSlot i = 0; i < CostumeSlot.Max; i++)
            {
                var xx = new UserDataItemDto
                {
                    ItemNumber = Player.CharacterManager.CurrentCharacter.Costumes.GetItem(i)?.ItemNumber ?? 0,
                    PriceType = Player.CharacterManager.CurrentCharacter.Costumes.GetItem(i)?.PriceType ?? 0,
                    Unk2 = 0,
                    Unk3 = 0,
                    Color = Player.CharacterManager.CurrentCharacter.Costumes.GetItem(i)?.Color ?? 0,
                    Effects = Player.CharacterManager.CurrentCharacter.Costumes.GetItem(i)?.GetItemEffectsInt() ?? new uint[0],
                    Unk4 = 0,
                    Unk5 = 0
                };

                UserData.Add(xx);
            }

            return UserData.ToArray();
        }
    }
}
