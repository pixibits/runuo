using System;
using Server;
using Server.Items;
using Server.Misc;

namespace Server.Mobiles
{
	public class TailorGuildmaster : BaseGuildmaster
	{
		public override NpcGuild NpcGuild{ get{ return NpcGuild.TailorsGuild; } }

		[Constructable]
		public TailorGuildmaster() : base( "tailor" )
		{
			Job = JobFragment.tailor;
			Karma = Utility.RandomMinMax( 13, -45 );

			
			SetSkill( SkillName.Tactics, 25, 47.5 );
			SetSkill( SkillName.MagicResist, 25, 47.5 );
			SetSkill( SkillName.Parry, 25, 47.5 );
			SetSkill( SkillName.Swords, 15, 37.5 );
			SetSkill( SkillName.Macing, 15, 37.5 );
			SetSkill( SkillName.Fencing, 15, 37.5 );
			SetSkill( SkillName.Wrestling, 15, 37.5 );
			SetSkill( SkillName.Tailoring, 55, 77.5 );

		}

		public override void InitBody()
		{
			SetStr( 36, 50 );
			SetDex( 46, 60 );
			SetInt( 41, 55 );
			Hue = Utility.RandomSkinHue();
			SpeechHue = Utility.RandomDyedHue();

			Female = Utility.RandomBool();
			Body = Female ? 401 : 400;
			Name = NameList.RandomName( Female ? "female" : "male" );
		}

		public override void InitOutfit()
		{
			Item item = null;
			item = AddRandomHair();
			item.Hue = Utility.RandomHairHue();
			item = AddRandomFacialHair( item.Hue );
			item = new Shirt();
			item.Hue = Utility.RandomNondyedHue();
			AddItem( item );
			item = new ShortPants();
			item.Hue = Utility.RandomNondyedHue();
			AddItem( item );
			item = Utility.RandomBool() ? (Item)new Shoes() : (Item)new Sandals();
			item.Hue = Utility.RandomNeutralHue();
			AddItem( item );
			PackGold( 15, 100 );
		}

		public TailorGuildmaster( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int)0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();
		}
	}
}

