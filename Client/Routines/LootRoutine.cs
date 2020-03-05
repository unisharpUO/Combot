using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ScriptSDK;
using StealthAPI;
using ScriptSDK.Data;
using ScriptSDK.Engines;
using ScriptSDK.Items;
using XScript.Items;
using XScript.Enumerations;
using XScript.Scripts.Sibble;


namespace Combot
{
    public class LootRoutine : Routines
    {

        public static void StartRoutine(object info)
        {
            Controller.ConsoleMessage("starting loot routine...", ConsoleColor.DarkYellow);

            while (true)
            {
                List<Item> _corpses = new List<Item>();
                _corpses = Item.Find(typeof(Corpse), 0x0, false);

                if (_corpses.Count > 0)
                {
                    foreach (Corpse _corpse in _corpses)
                    {
                        if (_corpse.Distance < 3)
                        {
                            LootCorpse(_corpse);
                            Scanner.Ignore(_corpse.Serial);
                        }
                    }
                }

                Thread.Sleep(2000);
            }
        }

        public static void LootCorpse(Corpse _corpse)
        {
            Controller.ConsoleMessage("Checking corpse: {0}", ConsoleColor.DarkBlue, _corpse.Serial.Value.ToString());
            List<BaseLoot> _toLootList = new List<BaseLoot>();

            _corpse.DoubleClick();
            Thread.Sleep(250);

            List<string> _lootTypes = new List<string>
            {
                "Armor",
                "Weapon",
                "Shield",
                "Jewel"
            };

            LocateGear.Find(_corpse, _lootTypes);

            #region Armor Filter
            foreach (BaseArmor _armor in LocateGear.ArmorList)
            {
                _armor.UpdateLocalizedProperties();

                if (_armor.Cursed || _armor.Antique)
                    continue;

                if (_armor.LootValue.Equals(LootValue.LegendaryArtifact))
                    _toLootList.Add(new BaseLoot(_armor.Serial, "Legendary Armor"));
                else if (_armor.LootValue.Equals(LootValue.MajorArtifact))
                    _toLootList.Add(new BaseLoot(_armor.Serial, "Major Armor"));
                else if (_armor.MaterialType.Equals(ArmorMaterialType.Bone) || _armor.MaterialType.Equals(ArmorMaterialType.Studded))
                    if (_armor.Attributes.LowerManaCost >= 8 && _armor.Attributes.BonusHits >= 5)
                        _toLootList.Add(new BaseLoot(_armor.Serial, "HP Studded/Bone Armor"));
                    else if (_armor.Attributes.BonusStam == 10)
                        _toLootList.Add(new BaseLoot(_armor.Serial, "Stam Studded/Bone Armor"));
            }
            #endregion

            #region Weapon Filter
            foreach (BaseWeapon _weapon in LocateGear.WeaponList)
            {
                _weapon.UpdateLocalizedProperties();

                if (_weapon.Cursed || _weapon.Antique)
                    continue;

                if (_weapon.WeaponAttributes.SplinteringWeapon >= 20)
                    _toLootList.Add(new BaseLoot(_weapon.Serial, "Splintering Weapon"));
                else if (_weapon.LootValue.Equals(LootValue.LegendaryArtifact) || _weapon.LootValue.Equals(LootValue.MajorArtifact))
                    if (_weapon.Attributes.CastSpeed == 0 && _weapon.Attributes.SpellChanneling)
                        _toLootList.Add(new BaseLoot(_weapon.Serial, "Mage Weapon"));
                    else if (_weapon.Attributes.WeaponSpeed >= 30)
                        _toLootList.Add(new BaseLoot(_weapon.Serial, "Warrior Weapon"));
            }
            #endregion

            #region Shield Filter
            foreach (BaseShield _shield in LocateGear.ShieldList)
            {
                _shield.UpdateLocalizedProperties();

                if (_shield.Cursed || _shield.Antique)
                    continue;

                if (!(_shield.LootValue.Equals(LootValue.LegendaryArtifact) || _shield.LootValue.Equals(LootValue.MajorArtifact)))
                    continue;

                if (_shield.Attributes.CastSpeed == 0 && _shield.Attributes.SpellChanneling)
                    _toLootList.Add(new BaseLoot(_shield.Serial, "Mage Shield"));
                else if (_shield.Attributes.WeaponSpeed >= 5 && _shield.Attributes.WeaponDamage >= 5)
                    _toLootList.Add(new BaseLoot(_shield.Serial, "Warrior Shield"));
            }
            #endregion

            #region Jewel Filter
            foreach (BaseJewel _jewel in LocateGear.JewelList)
            {
                _jewel.UpdateLocalizedProperties();

                if (_jewel.Cursed || _jewel.Antique)
                    continue;

                if (_jewel.LootValue.Equals(LootValue.LegendaryArtifact))
                    _toLootList.Add(new BaseLoot(_jewel.Serial, "Legendary Jewel"));
                else if (_jewel.LootValue.Equals(LootValue.MajorArtifact))
                    _toLootList.Add(new BaseLoot(_jewel.Serial, "Major Jewel"));
                else if (_jewel.Attributes.BonusDex == 0
                    && _jewel.Attributes.BonusHits == 0
                    && _jewel.Attributes.BonusInt == 0
                    && _jewel.Attributes.BonusMana == 0
                    && _jewel.Attributes.BonusStam == 0
                    && _jewel.Attributes.BonusStr == 0
                    && _jewel.Attributes.RegenHits == 0
                    && _jewel.Attributes.RegenMana == 0
                    && _jewel.Attributes.RegenStam == 0
                    && _jewel.Attributes.SpellDamage == 0
                    && _jewel.Attributes.CastRecovery == 0
                    && _jewel.Attributes.CastSpeed == 0
                    && _jewel.Attributes.LowerManaCost == 0
                    && _jewel.Attributes.LowerRegCost == 0
                    && _jewel.Attributes.Luck == 0
                    && _jewel.Resistances.Physical == 0
                    && _jewel.Resistances.Fire == 0
                    && _jewel.Resistances.Cold == 0
                    && _jewel.Resistances.Poison == 0
                    && _jewel.Resistances.Energy == 0
                    && _jewel.Attributes.WeaponSpeed >= 5)
                    _toLootList.Add(new BaseLoot(_jewel.Serial, "Clean Dexer Jewel"));
                else if (_jewel.Attributes.BonusDex == 0
                    && _jewel.Attributes.BonusHits == 0
                    && _jewel.Attributes.BonusInt == 0
                    && _jewel.Attributes.BonusMana == 0
                    && _jewel.Attributes.BonusStam == 0
                    && _jewel.Attributes.BonusStr == 0
                    && _jewel.Attributes.RegenHits == 0
                    && _jewel.Attributes.RegenMana == 0
                    && _jewel.Attributes.RegenStam == 0
                    && _jewel.Attributes.SpellDamage >= 15
                    && _jewel.Attributes.Luck == 0
                    && _jewel.Resistances.Physical == 0
                    && _jewel.Resistances.Fire == 0
                    && _jewel.Resistances.Cold == 0
                    && _jewel.Resistances.Poison == 0
                    && _jewel.Resistances.Energy == 0
                    && _jewel.Attributes.AttackChance == 0
                    && _jewel.Attributes.DefendChance == 0
                    && _jewel.Attributes.WeaponSpeed == 0)
                    _toLootList.Add(new BaseLoot(_jewel.Serial, "Clean Mage Jewel"));
            }
            #endregion

            if (_toLootList.Count() > 0)
                LootItems(_toLootList);

        }

        public static void LootItems(List<BaseLoot> _toLootList)
        {
            lock (Controller.UseLock)
            {
                Stealth.Client.CancelTarget();

                foreach (BaseLoot _loot in _toLootList)
                {

                    Stealth.Client.PartySay("Looting Item: " + _loot.Serial.Value + ' ' + _loot.Reason);
                    Stealth.Client.MoveItem(_loot.Serial.Value, 1, Controller.Self.Backpack.Serial.Value, 0, 0, 0);
                    Scanner.Ignore(_loot.Serial);
                    Thread.Sleep(1500);
                }
                _toLootList.Clear();
            }
        }
    }

    [QueryType(typeof(BaseArmor), typeof(BaseJewel), typeof(BaseWeapon))]
    public class BaseEquipment : Item
    {
        public BaseEquipment(Serial serial)
        : base(serial)
        {
        }
    }

    [QueryType(typeof(BaseEquipment))]
    public class BaseLoot : Item
    {
        private string _reason;
        public string Reason
        {
            get { return _reason; }
            set { _reason = value; }
        }

        public BaseLoot(Serial serial)
            : base(serial)
        {
        }

        public BaseLoot(Serial serial, string Reason)
        : base(serial)
        {
            this.Reason = Reason;
        }
    }
}
