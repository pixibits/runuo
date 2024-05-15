using System;
using Server;
using Server.Items;
using Server.Misc;

namespace Server.Mobiles
{
	public class OclloCashual : BaseVendor
	{
		private System.Collections.ArrayList m_SBInfos = new System.Collections.ArrayList();
		protected override System.Collections.ArrayList SBInfos{ get { return m_SBInfos; } }

		public override NpcGuild NpcGuild{ get{ return NpcGuild.MagesGuild; } }
		public override void InitSBInfo()
		{
			m_SBInfos.Add( new SBHolyMage() );
		}

		[Constructable]
		public OclloCashual() : base( "the cashual" )
		{
			Job = JobFragment.cashual;
			Karma = Utility.RandomMinMax( 13, -45 );

			
			SetSkill( SkillName.Tactics, 55, 77.5 );
			SetSkill( SkillName.MagicResist, 65, 87.5 );
			SetSkill( SkillName.Parry, 55, 77.5 );
			SetSkill( SkillName.Swords, 15, 37.5 );
			SetSkill( SkillName.Macing, 15, 37.5 );
			SetSkill( SkillName.Fencing, 15, 37.5 );
			SetSkill( SkillName.Wrestling, 15, 37.5 );
			SetSkill( SkillName.Alchemy, 55, 77.5 );
			SetSkill( SkillName.TasteID, 55, 77.5 );
			SetSkill( SkillName.Magery, 85.1, 100 );
			SetSkill( SkillName.Inscribe, 50.1, 65 );

		}

		public override void InitBody()
		{
			SetStr( 61, 75 );
			SetDex( 71, 85 );
			SetInt( 86, 100 );
			Hue = Utility.RandomSkinHue();
			SpeechHue = Utility.RandomDyedHue();

			Female = Utility.RandomBool();
			Body = Female ? 401 : 400;
			Name = NameList.RandomName( Female ? "female" : "male" );
		}

		public override void InitOutfit()
		{
			Item item = null;
			if ( !Female )
			{
				item = AddRandomHair();
				item.Hue = Utility.RandomHairHue();
				item = AddRandomFacialHair( item.Hue );
				item = new Robe();
				item.Hue = Utility.RandomRedHue();
				AddItem( item );
				item = new Sandals();
				AddItem( item );
				PackGold( 15, 100 );
				LootPack.HighScrolls.Generate( this );
				LootPack.HighScrolls.Generate( this );
			} else {
				item = AddRandomHair();
				item.Hue = Utility.RandomHairHue();
				item = new Robe();
				item.Hue = Utility.RandomRedHue();
				AddItem( item );
				item = new Sandals();
				AddItem( item );
				PackGold( 15, 100 );
				LootPack.HighScrolls.Generate( this );
				LootPack.HighScrolls.Generate( this );
			}
		}

		public OclloCashual( Serial serial ) : base( serial )
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

