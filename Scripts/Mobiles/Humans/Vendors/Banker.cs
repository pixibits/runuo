using System;
using System.Collections; using System.Collections.Generic;
using Server.Items;
using Server.ContextMenus;
using Server.Misc;
using Server.Network;

namespace Server.Mobiles
{
	public class Banker : BaseVendor
	{
		private ArrayList m_SBInfos = new ArrayList();
		protected override ArrayList SBInfos{ get { return m_SBInfos; } }

		public override NpcGuild NpcGuild{ get{ return NpcGuild.MerchantsGuild; } }

		[Constructable]
		public Banker() : base( "the banker" )
		{
			Job = JobFragment.banker;

			SetStr( 71, 85 );
			SetDex( 66, 80 );
			SetInt( 66, 80 );
			Karma = Utility.RandomMinMax( 13, -45 );

			
			SetSkill( SkillName.Tactics, 45, 67.5 );
			SetSkill( SkillName.MagicResist, 45, 67.5 );
			SetSkill( SkillName.Parry, 45, 67.5 );
			SetSkill( SkillName.Swords, 15, 37.5 );
			SetSkill( SkillName.Macing, 15, 37.5 );
			SetSkill( SkillName.Fencing, 15, 37.5 );
			SetSkill( SkillName.Wrestling, 15, 37.5 );
		}

		public override void InitSBInfo()
		{
			m_SBInfos.Add( new SBBanker() );
		}

		public static int GetBalance( Mobile from )
		{
			Item[] gold, checks;

			return GetBalance( from, out gold, out checks );
		}

		public override bool OnDragDrop(Mobile from, Item dropped)
		{
			if ( dropped is Gold )
			{
				from.BankBox.AddItem( dropped );
				SayTo( from, "Thou hast deposited the gold in thy bank account." );
				return true;
			}
			else
			{
				SayTo( from, "I have no use for this." );
				return false;
			}
		}

		public override void InitBody()
		{
			Female = Utility.RandomBool();
			Body = Female ? 401 : 400;
			Name = NameList.RandomName( Female ? "female" : "male" );
			Hue = Utility.RandomSkinHue();
		}

		public override void InitOutfit()
		{
			Item item = null;
			if ( !Female )
			{
				item = AddRandomHair();
				item.Hue = Utility.RandomHairHue();
				item = AddRandomFacialHair( item.Hue );
				item = new Shirt();
				item.Hue = Utility.RandomNondyedHue();
				AddItem( item );
				item = new ShortPants();
				item.Hue = Utility.RandomNondyedHue();
				AddItem( item );
				item = new Shoes();
				item.Hue = Utility.RandomNeutralHue();
				AddItem( item );
				PackGold( 15, 100 );
			} 
			else 
			{
				item = AddRandomHair();
				item.Hue = Utility.RandomHairHue();
				item = new Shirt();
				item.Hue = Utility.RandomNondyedHue();
				AddItem( item );
				item = new Skirt();
				item.Hue = Utility.RandomNondyedHue();
				AddItem( item );
				item = new Shoes();
				item.Hue = Utility.RandomNeutralHue();
				AddItem( item );
				PackGold( 15, 100 );
			}
		}

		public static int GetBalance( Mobile from, out Item[] gold, out Item[] checks )
		{
			int balance = 0;

			Container bank = from.BankBox;

			if ( bank != null )
			{
				gold = bank.FindItemsByType( typeof( Gold ) );
				checks = bank.FindItemsByType( typeof( BankCheck ) );

				for ( int i = 0; i < gold.Length; ++i )
					balance += gold[i].Amount;

				for ( int i = 0; i < checks.Length; ++i )
					balance += ((BankCheck)checks[i]).Worth;
			}
			else
			{
				gold = checks = new Item[0];
			}

			return balance;
		}

		public static bool Withdraw( Mobile from, int amount )
		{
			Item[] gold, checks;
			int balance = GetBalance( from, out gold, out checks );

			if ( balance < amount )
				return false;

			for ( int i = 0; amount > 0 && i < gold.Length; ++i )
			{
				if ( gold[i].Amount <= amount )
				{
					amount -= gold[i].Amount;
					gold[i].Delete();
				}
				else
				{
					gold[i].Amount -= amount;
					amount = 0;
				}
			}

			for ( int i = 0; amount > 0 && i < checks.Length; ++i )
			{
				BankCheck check = (BankCheck)checks[i];

				if ( check.Worth <= amount )
				{
					amount -= check.Worth;
					check.Delete();
				}
				else
				{
					check.Worth -= amount;
					amount = 0;
				}
			}

			return true;
		}

		public static bool Deposit( Mobile from, int amount )
		{
			BankBox box = from.BankBox;

			Item item = ( amount >= 1000 ? (Item)new BankCheck( amount ) : (Item)new Gold( amount ) );

			if ( box != null && box.TryDropItem( from, item, false ) )
				return true;

			item.Delete();
			return false;
		}

		public Banker( Serial serial ) : base( serial )
		{
		}

		public override bool HandlesOnSpeech( Mobile from )
		{
			if ( from.InRange( this.Location, 12 ) )
				return true;

			return base.HandlesOnSpeech( from );
		}

		public override void OnSpeech( SpeechEventArgs e )
		{
			Console.WriteLine("in banker onspeech keycount: {0}", e.Keywords.Length);
			if ( !e.Handled && e.Mobile.InRange( this.Location, 12 ) )
			{
				for ( int i = 0; i < e.Keywords.Length; ++i )
				{
					int keyword = e.Keywords[i];
                    Console.WriteLine("in banker onspeech keyword: {0}", keyword.ToString("X"));
                    switch ( keyword )
					{
						case 0x0000: // *withdraw*
						{
							e.Handled = true;
							string[] split = e.Speech.Split( ' ' );

							if ( split.Length >= 2 )
							{
								int amount;

								try
								{
									amount = Convert.ToInt32( split[1] );
								}
								catch
								{
									break;
								}

								if ( amount > 60000 )
								{
									this.Say( 500381 ); // Thou canst not withdraw so much at one time!
								}
								else if ( amount > 0 )
								{
									BankBox box = e.Mobile.BankBox;

									if ( box == null || !box.ConsumeTotal( typeof( Gold ), amount ) )
									{
										this.Say( 500384 ); // Ah, art thou trying to fool me? Thou hast not so much gold!
									}
									else
									{
										e.Mobile.AddToBackpack( new Gold( amount ) );

										this.Say( 1010005 ); // Thou hast withdrawn gold from thy account.
									}
								}
							}

							break;
						}
						case 0x0001: // *balance*
						{
							e.Handled = true;
							BankBox box = e.Mobile.BankBox;

							if ( box != null )
							{
								this.Say( 1042759, box.TotalGold.ToString() ); // Thy current bank balance is ~1_AMOUNT~ gold.
							}

							break;
						}
						case 0x0002: // *bank*
						{
							e.Handled = true;

							if ( e.Mobile.Criminal )
							{
								this.Say( 500378 ); // Thou art a criminal and cannot access thy bank box.
								break;
							}

							BankBox box = e.Mobile.BankBox;

							if ( box != null )
								box.Open();

							break;
						}
						/*case 0x0003: // *check*
						{
							e.Handled = true;

							if ( e.Mobile.Criminal )
							{
								this.Say( 500389 ); // I will not do business with a criminal!
								break;
							}

							string[] split = e.Speech.Split( ' ' );

							if ( split.Length >= 2 )
							{
								int amount;

								try
								{
									amount = Convert.ToInt32( split[1] );
								}
								catch
								{
									break;
								}

								if ( amount < 5000 )
								{
									this.Say( 1010006 ); // We cannot create checks for such a paltry amount of gold!
								}
								else if ( amount > 1000000 )
								{
									this.Say( 1010007 ); // Our policies prevent us from creating checks worth that much!
								}
								else
								{
									BankCheck check = new BankCheck( amount );

									BankBox box = e.Mobile.BankBox;

									if ( box == null || !box.TryDropItem( e.Mobile, check, false ) )
									{
										this.Say( 500386 ); // There's not enough room in your bankbox for the check!
										check.Delete();
									}
									else if ( !box.ConsumeTotal( typeof( Gold ), amount ) )
									{
										this.Say( 500384 ); // Ah, art thou trying to fool me? Thou hast not so much gold!
										check.Delete();
									}
									else
									{
										this.Say( 1042673, AffixType.Append, amount.ToString(), "" ); // Into your bank box I have placed a check in the amount of:
									}
								}
							}

							break;
						} // end *check* */
					}
				}
			}

			base.OnSpeech( e );
			if ( e.Handled && this.Home != Point3D.Zero && !this.InRange( this.Home, this.RangeHome+5 ) )
			{
				this.Say( "Please allow me to return to my post so that I might assist thee." );
				this.Location = this.Home;
			}
		}

        public override void AddCustomContextEntries(Mobile from, List<ContextMenus.ContextMenuEntry> list)
		{
			if ( from.Alive )
				list.Add( new OpenBankEntry( from, this ) );

			base.AddCustomContextEntries( from, list );
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}
}
