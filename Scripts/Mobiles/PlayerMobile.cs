using System;
using System.Collections; using System.Collections.Generic;
using Server;
using Server.Misc;
using Server.Items;
using Server.Gumps;
using Server.Guilds;
using Server.Multis;
using Server.Engines.Help;
using Server.ContextMenus;
using Server.Network;
using Server.Spells;
using Server.Spells.Fifth;
using Server.Spells.Seventh;
using Server.Targeting;

namespace Server.Mobiles
{
	[Flags]
	public enum PlayerFlag // First 16 bits are reserved for default-distro use, start custom flags at 0x00010000
	{
		None				= 0x00000000,
		Glassblowing		= 0x00000001,
		Masonry				= 0x00000002,
		SandMining			= 0x00000004,
		StoneMining			= 0x00000008,
		ToggleMiningStone	= 0x00000010,
		KarmaLocked			= 0x00000020,
		AutoRenewInsurance	= 0x00000040,
		UseOwnFilter		= 0x00000080,
		PublicMyRunUO		= 0x00000100,
		PagingSquelched		= 0x00000200
	}

	public enum NpcGuild
	{
		None,
		MagesGuild,
		WarriorsGuild,
		ThievesGuild,
		RangersGuild,
		HealersGuild,
		MinersGuild,
		MerchantsGuild,
		TinkersGuild,
		TailorsGuild,
		FishermensGuild,
		BardsGuild,
		BlacksmithsGuild
	}

	public class PlayerMobile : Mobile
	{
		private const int SkillCount = 46;

		private DateTime m_NextNotoUp;
		private int m_Bounty; 

		private class CountAndTimeStamp
		{
			private int m_Count;
			private DateTime m_Stamp;

			public CountAndTimeStamp()
			{
			}

			public DateTime TimeStamp { get{ return m_Stamp; } }
			public int Count 
			{ 
				get { return m_Count; } 
				set	{ m_Count = value; m_Stamp = DateTime.Now; } 
			}
		}

		//private DesignContext m_DesignContext;

		private NpcGuild m_NpcGuild;
		private DateTime m_NpcGuildJoinTime;
		private TimeSpan m_NpcGuildGameTime;
		private PlayerFlag m_Flags;
		private int m_StepsTaken;

		private int m_LastUpdate;
		public int LastUpdate { get { return m_LastUpdate; } set { m_LastUpdate = value; } }

		private DateTime m_LastLogin;
		public DateTime LastLogin { get { return m_LastLogin; } set { m_LastLogin = value; } }

		public int StepsTaken
		{
			get{ return m_StepsTaken; }
			set{ m_StepsTaken = value; }
		}

		[CommandProperty( AccessLevel.GameMaster )] 
		public int Bounty 
		{ 
			get
			{ 
				return m_Bounty; 
			} 
			set
			{ 
				if ( m_Bounty != value )
				{
					if ( m_Bounty < value )
						m_NextBountyDecay = DateTime.Now + TimeSpan.FromDays( 1.0 );
					m_Bounty = value; 
				}
				BountyBoard.Update( this );
			}
		} 

		public override bool ClickTitle
		{
			get
			{
				return false;
			}
		}


		[CommandProperty( AccessLevel.GameMaster )]
		public DateTime NextNotoUp
		{
			get { return m_NextNotoUp; }
			set { m_NextNotoUp = value; }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public NpcGuild NpcGuild
		{
			get{ return m_NpcGuild; }
			set{ m_NpcGuild = value; }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public DateTime NpcGuildJoinTime
		{
			get{ return m_NpcGuildJoinTime; }
			set{ m_NpcGuildJoinTime = value; }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public TimeSpan NpcGuildGameTime
		{
			get{ return m_NpcGuildGameTime; }
			set{ m_NpcGuildGameTime = value; }
		}

		public PlayerFlag Flags
		{
			get{ return m_Flags; }
			set{ m_Flags = value; }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public bool PagingSquelched
		{
			get{ return GetFlag( PlayerFlag.PagingSquelched ); }
			set{ SetFlag( PlayerFlag.PagingSquelched, value ); }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public bool Glassblowing
		{
			get{ return GetFlag( PlayerFlag.Glassblowing ); }
			set{ SetFlag( PlayerFlag.Glassblowing, value ); }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public bool Masonry
		{
			get{ return GetFlag( PlayerFlag.Masonry ); }
			set{ SetFlag( PlayerFlag.Masonry, value ); }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public bool SandMining
		{
			get{ return GetFlag( PlayerFlag.SandMining ); }
			set{ SetFlag( PlayerFlag.SandMining, value ); }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public bool StoneMining
		{
			get{ return GetFlag( PlayerFlag.StoneMining ); }
			set{ SetFlag( PlayerFlag.StoneMining, value ); }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public bool ToggleMiningStone
		{
			get{ return GetFlag( PlayerFlag.ToggleMiningStone ); }
			set{ SetFlag( PlayerFlag.ToggleMiningStone, value ); }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public bool KarmaLocked
		{
			get{ return GetFlag( PlayerFlag.KarmaLocked ); }
			set{ SetFlag( PlayerFlag.KarmaLocked, value ); }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public bool AutoRenewInsurance
		{
			get{ return GetFlag( PlayerFlag.AutoRenewInsurance ); }
			set{ SetFlag( PlayerFlag.AutoRenewInsurance, value ); }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public bool UseOwnFilter
		{
			get{ return GetFlag( PlayerFlag.UseOwnFilter ); }
			set{ SetFlag( PlayerFlag.UseOwnFilter, value ); }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public bool PublicMyRunUO
		{
			get{ return GetFlag( PlayerFlag.PublicMyRunUO ); }
			set{ SetFlag( PlayerFlag.PublicMyRunUO, value ); InvalidateMyRunUO(); }
		}

		public static Direction GetDirection4( Point3D from, Point3D to )
		{
			int dx = from.X - to.X;
			int dy = from.Y - to.Y;

			int rx = dx - dy;
			int ry = dx + dy;

			Direction ret;

			if ( rx >= 0 && ry >= 0 )
				ret = Direction.West;
			else if ( rx >= 0 && ry < 0 )
				ret = Direction.South;
			else if ( rx < 0 && ry < 0 )
				ret = Direction.East;
			else
				ret = Direction.North;

			return ret;
		}

		public override bool OnDroppedItemToWorld( Item item, Point3D location )
		{
			if ( !base.OnDroppedItemToWorld( item, location ) )
				return false;

			BounceInfo bi = item.GetBounce();

			if ( bi != null )
			{
				Type type = item.GetType();

				if ( type.IsDefined( typeof( FurnitureAttribute ), true ) || type.IsDefined( typeof( DynamicFlipingAttribute ), true ) )
				{
					object[] objs = type.GetCustomAttributes( typeof( FlipableAttribute ), true );

					if ( objs != null && objs.Length > 0 )
					{
						FlipableAttribute fp = objs[0] as FlipableAttribute;

						if ( fp != null )
						{
							int[] itemIDs = fp.ItemIDs;

							Point3D oldWorldLoc = bi.m_WorldLoc;
							Point3D newWorldLoc = location;

							if ( oldWorldLoc.X != newWorldLoc.X || oldWorldLoc.Y != newWorldLoc.Y )
							{
								Direction dir = GetDirection4( oldWorldLoc, newWorldLoc );

								if ( itemIDs.Length == 2 )
								{
									switch ( dir )
									{
										case Direction.North:
										case Direction.South: item.ItemID = itemIDs[0]; break;
										case Direction.East:
										case Direction.West: item.ItemID = itemIDs[1]; break;
									}
								}
								else if ( itemIDs.Length == 4 )
								{
									switch ( dir )
									{
										case Direction.South: item.ItemID = itemIDs[0]; break;
										case Direction.East: item.ItemID = itemIDs[1]; break;
										case Direction.North: item.ItemID = itemIDs[2]; break;
										case Direction.West: item.ItemID = itemIDs[3]; break;
									}
								}
							}
						}
					}
				}
			}

			return true;
		}

		public bool GetFlag( PlayerFlag flag )
		{
			return ( (m_Flags & flag) != 0 );
		}

		public void SetFlag( PlayerFlag flag, bool value )
		{
			if ( value )
				m_Flags |= flag;
			else
				m_Flags &= ~flag;
		}

		private static OnPacketReceive m_OldWalkReq;
		public static void Initialize()
		{
			m_OldWalkReq = Server.Network.PacketHandlers.GetHandler( 0x02 ).OnReceive;
			
			Server.Network.PacketHandlers.Register( 0x02,   3,  true, new OnPacketReceive( MovementHandler ) );

			Server.Network.PacketHandlers.Register( 0xAD,   0,  true, new OnPacketReceive( UnicodeSpeech ) );
			
			new MovementController().Start();

			Mobile.AsciiClickMessage = true;
			Mobile.GuildClickMessage = true;
			Mobile.DisableHiddenSelfClick = false;

			EventSink.Login += new LoginEventHandler( OnLogin );
			EventSink.Logout += new LogoutEventHandler( OnLogout );
			EventSink.Connected += new ConnectedEventHandler( EventSink_Connected );
			EventSink.Disconnected += new DisconnectedEventHandler( EventSink_Disconnected );
		}

		public override void OnSkillInvalidated( Skill skill )
		{
			if ( Core.AOS && skill.SkillName == SkillName.MagicResist )
				UpdateResistances();
		}

		public override int GetMaxResistance( ResistanceType type )
		{
			int max = base.GetMaxResistance( type );

			if ( 60 < max && Spells.Fourth.CurseSpell.IsUnderEffect( this ) )
				max = 60;

			return max;
		}

		private int m_LastGlobalLight = -1, m_LastPersonalLight = -1;

		public override void OnNetStateChanged()
		{
			m_LastGlobalLight = -1;
			m_LastPersonalLight = -1;
		}

		public static string[] m_GuildTypes = new string[]{ "", " (Chaos)", " (Order)" };
		public override void OnSingleClick(Mobile from)
		{
			if ( Deleted || ( AccessLevel == AccessLevel.Player && DisableHiddenSelfClick && Hidden && from == this ) )
				return;

			if ( Mobile.GuildClickMessage )
			{
				Server.Guilds.Guild guild = this.Guild as Server.Guilds.Guild;

				if ( guild != null && ( this.DisplayGuildTitle || guild.Type != Server.Guilds.GuildType.Regular ) )
				{
					string title = GuildTitle;
					string type;

					if ( title == null )
						title = "";
					else
						title = title.Trim();

					if ( guild.Type >= 0 && (int)guild.Type < m_GuildTypes.Length )
						type = m_GuildTypes[(int)guild.Type];
					else
						type = "";

					string text = String.Format( title.Length <= 0 ? "[{1}]{2}" : "[{0}, {1}]{2}", title, guild.Abbreviation, type );

					PrivateOverheadMessage( MessageType.Regular, SpeechHue, true, text, from.NetState );
				}
			}

			int hue;

			if ( NameHue != -1 )
				hue = NameHue;
			else if ( AccessLevel > AccessLevel.Player )
				hue = 11;
			else
				hue = Notoriety.GetHue( Notoriety.Compute( from, this ) );

			System.Text.StringBuilder sb = new System.Text.StringBuilder();

			if ( ShowFameTitle && ( Karma >= (int)Noto.LordLady || Karma <= (int)Noto.Dark ) )
				sb.Append( Female ? "Lady " : "Lord " );
			
			sb.Append( Name );

			if ( ClickTitle && Title != null && Title.Length > 0 )
			{
				sb.Append( ' ' );
				sb.Append( Title );
			}

			if ( Frozen || Paralyzed || ( this.Spell != null && this.Spell is Spell && this.Spell.IsCasting && ((Spell)this.Spell).BlocksMovement ) )
				sb.Append( " (frozen)" );

			if ( Blessed )
				sb.Append( " (invulnerable)" );

			PrivateOverheadMessage( MessageType.Label, hue, Mobile.AsciiClickMessage, sb.ToString(), from.NetState );
		}

		public override void ComputeBaseLightLevels( out int global, out int personal )
		{
			global = LightCycle.ComputeLevelFor( this );
			personal = this.LightLevel;
		}

		public override void CheckLightLevels( bool forceResend )
		{
			NetState ns = this.NetState;

			if ( ns == null )
				return;

			int global, personal;

			ComputeLightLevels( out global, out personal );

			if ( !forceResend )
				forceResend = ( global != m_LastGlobalLight || personal != m_LastPersonalLight );

			if ( !forceResend )
				return;

			m_LastGlobalLight = global;
			m_LastPersonalLight = personal;

			ns.Send( GlobalLightLevel.Instantiate( global ) );
			ns.Send( new PersonalLightLevel( this, personal ) );
		}

		public override int GetMinResistance( ResistanceType type )
		{
			int magicResist = (int)(Skills[SkillName.MagicResist].Value * 10);
			int min = int.MinValue;

			if ( magicResist >= 1000 )
				min = 40 + ((magicResist - 1000) / 50);
			else if ( magicResist >= 400 )
				min = (magicResist - 400) / 15;

			if ( min > MaxPlayerResistance )
				min = MaxPlayerResistance;

			int baseMin = base.GetMinResistance( type );

			if ( min < baseMin )
				min = baseMin;

			return min;
		}

		private static void OnLogin( LoginEventArgs e )
		{
			Mobile from = e.Mobile;

			/*
			SacrificeVirtue.CheckAtrophy( from );
			JusticeVirtue.CheckAtrophy( from );
			CompassionVirtue.CheckAtrophy( from );
			*/

			if ( AccountHandler.LockdownLevel > AccessLevel.Player )
			{
				string notice;

				Accounting.Account acct = from.Account as Accounting.Account;

				if ( acct == null || !acct.HasAccess( from.NetState ) )
				{
					if ( from.AccessLevel == AccessLevel.Player )
						notice = "The server is currently under lockdown. No players are allowed to log in at this time.";
					else
						notice = "The server is currently under lockdown. You do not have sufficient access level to connect.";

					Timer.DelayCall( TimeSpan.FromSeconds( 1.0 ), new TimerStateCallback( Disconnect ), from );
				}
				else if ( from.AccessLevel == AccessLevel.Administrator )
				{
					notice = "The server is currently under lockdown. As you are an administrator, you may change this from the [Admin gump.";
				}
				else
				{
					notice = "The server is currently under lockdown. You have sufficient access level to connect.";
				}

				from.SendGump( new NoticeGump( 1060637, 30720, notice, 0xFFC000, 300, 140, null, null ) );
			}
		}

		private bool m_NoDeltaRecursion;

		public void ValidateEquipment()
		{
			if ( m_NoDeltaRecursion || Map == null || Map == Map.Internal )
				return;

			if ( this.Items == null )
				return;

			m_NoDeltaRecursion = true;
			Timer.DelayCall( TimeSpan.Zero, new TimerCallback( ValidateEquipment_Sandbox ) );
		}

		private void ValidateEquipment_Sandbox()
		{
			try
			{
				if ( Map == null || Map == Map.Internal )
					return;

				List<Item> items = this.Items;

				if ( items == null )
					return;

				bool moved = false;

				int str = this.Str;
				int dex = this.Dex;
				int intel = this.Int;

				Mobile from = this;

				for ( int i = items.Count - 1; i >= 0; --i )
				{
					if ( i >= items.Count )
						continue;

					Item item = (Item)items[i];

					if ( item is BaseWeapon )
					{
						BaseWeapon weapon = (BaseWeapon)item;

						bool drop = false;

						if ( dex < weapon.DexRequirement )
							drop = true;
						else if ( str < AOS.Scale( weapon.StrRequirement, 100 - weapon.GetLowerStatReq() ) )
							drop = true;
						else if ( intel < weapon.IntRequirement )
							drop = true;

						if ( drop )
						{
							string name = weapon.Name;

							if ( name == null )
								name = String.Format( "#{0}", weapon.LabelNumber );

							from.SendLocalizedMessage( 1062001, name ); // You can no longer wield your ~1_WEAPON~
							from.AddToBackpack( weapon );
							moved = true;
						}
					}
					else if ( item is BaseArmor )
					{
						BaseArmor armor = (BaseArmor)item;

						bool drop = false;

						if ( !armor.AllowMaleWearer && from.Body.IsMale && from.AccessLevel < AccessLevel.GameMaster )
						{
							drop = true;
						}
						else if ( !armor.AllowFemaleWearer && from.Body.IsFemale && from.AccessLevel < AccessLevel.GameMaster )
						{
							drop = true;
						}
						else
						{
							int strBonus = armor.ComputeStatBonus( StatType.Str ), strReq = armor.ComputeStatReq( StatType.Str );
							int dexBonus = armor.ComputeStatBonus( StatType.Dex ), dexReq = armor.ComputeStatReq( StatType.Dex );
							int intBonus = armor.ComputeStatBonus( StatType.Int ), intReq = armor.ComputeStatReq( StatType.Int );

							if ( dex < dexReq || (dex + dexBonus) < 1 )
								drop = true;
							else if ( str < strReq || (str + strBonus) < 1 )
								drop = true;
							else if ( intel < intReq || (intel + intBonus) < 1 )
								drop = true;
						}

						if ( drop )
						{
							string name = armor.Name;

							if ( name == null )
								name = String.Format( "#{0}", armor.LabelNumber );

							if ( armor is BaseShield )
								from.SendLocalizedMessage( 1062003, name ); // You can no longer equip your ~1_SHIELD~
							else
								from.SendLocalizedMessage( 1062002, name ); // You can no longer wear your ~1_ARMOR~

							from.AddToBackpack( armor );
							moved = true;
						}
					}
				}

				if ( moved )
					from.SendLocalizedMessage( 500647 ); // Some equipment has been moved to your backpack.
			}
			catch ( Exception e )
			{
				Console.WriteLine( e );
			}
			finally
			{
				m_NoDeltaRecursion = false;
			}
		}

		public override void Delta( MobileDelta flag )
		{
			base.Delta( flag );

			if ( (flag & MobileDelta.Stat) != 0 )
				ValidateEquipment();

			if ( (flag & (MobileDelta.Name | MobileDelta.Hue)) != 0 )
				InvalidateMyRunUO();
		}

		private static void Disconnect( object state )
		{
			NetState ns = ((Mobile)state).NetState;

			if ( ns != null )
				ns.Dispose();
		}

		private static void OnLogout( LogoutEventArgs e )
		{
		}

		private static void EventSink_Connected( ConnectedEventArgs e )
		{
			PlayerMobile pm = e.Mobile as PlayerMobile;

			if ( pm != null )
				pm.m_SessionStart = DateTime.Now;
		}

		private static void EventSink_Disconnected( DisconnectedEventArgs e )
		{
			Mobile from = e.Mobile;
			/*DesignContext context = DesignContext.Find( from );

			if ( context != null )
			{
				// Remove design context
				DesignContext.Remove( from );

				// Eject client from house
				from.RevealingAction();
				from.Map = context.Foundation.Map;
				from.Location = context.Foundation.BanLocation;
			}*/

			PlayerMobile pm = e.Mobile as PlayerMobile;

			if ( pm != null )
			{
				pm.m_GameTime += (DateTime.Now - pm.m_SessionStart);

				MovementController.OnDisconnected( pm );
			}
		}

		public override void RevealingAction()
		{
			//if ( m_DesignContext != null )
			//	return;

			Spells.Sixth.InvisibilitySpell.RemoveTimer( this );

			base.RevealingAction();
		}

		public override void OnSubItemAdded( Item item )
		{
			if ( AccessLevel < AccessLevel.GameMaster && item.IsChildOf( this.Backpack ) )
			{
				int maxWeight = WeightOverloading.GetMaxWeight( this );
				int curWeight = Mobile.BodyWeight + this.TotalWeight;

				if ( curWeight > maxWeight )
					this.SendLocalizedMessage( 1019035, true, String.Format( " : {0} / {1}", curWeight, maxWeight ) );
			}
		}

		public override bool CanBeHarmful( Mobile target, bool message, bool ignoreOurBlessedness )
		{
			/*if ( m_DesignContext != null || (target is PlayerMobile && ((PlayerMobile)target).m_DesignContext != null) )
				return false;*/

			if ( (target is BaseVendor && ((BaseVendor)target).IsInvulnerable) || target is PlayerVendor || target is TownCrier )
			{
				if ( message )
				{
					if ( target.Title == null )
						SendAsciiMessage( "{0} the vendor cannot be harmed.", target.Name );
					else
						SendAsciiMessage( "{0} {1} cannot be harmed.", target.Name, target.Title );
				}

				return false;
			}

			return base.CanBeHarmful( target, message, ignoreOurBlessedness );
		}

		public override void OnItemAdded( Item item )
		{
			base.OnItemAdded( item );
			InvalidateMyRunUO();
		}

		public override void OnItemRemoved( Item item )
		{
			base.OnItemRemoved( item );
			InvalidateMyRunUO();
		}

		public override int HitsMax
		{
			get{ return base.Str; }
		}

		public override int StamMax
		{
			get
			{ 
				return base.StamMax + AosAttributes.GetValue( this, AosAttribute.BonusStam ); 
			}
		}

		public override int ManaMax
		{
			get{ return base.ManaMax + AosAttributes.GetValue( this, AosAttribute.BonusMana ); }
		}

		public override bool Move( Direction d )
		{
			NetState ns = this.NetState;

			/*if ( ns != null )
			{
				Gump[] gumps = ns.Gumps;

				for ( int i = 0; i < gumps.Length; ++i )
				{
					if ( gumps[i] is ResurrectGump )
					{
						if ( Alive )
						{
							CloseGump( typeof( ResurrectGump ) );
						}
						else
						{
							SendLocalizedMessage( 500111 ); // You are frozen and cannot move.
							return false;
						}
					}
				}
			}*/

			TimeSpan speed = ComputeMovementSpeed( d );
            Console.WriteLine("Script moving {0}", d);
            if ( !base.Move( d ) )
				return false;

			m_NextMovementTime = DateTime.Now + speed;
			return true;
		}

		protected override bool OnMove( Direction d )
		{
			bool result = base.OnMove( d );

			if ( result && this.Alive )
			{
				BaseMount mount = this.Mount as BaseMount;
				if ( mount != null )
				{
					if ( mount.Stam <= 0 )
					{
						SendLocalizedMessage( 500108 ); // Your mount is too fatigued to move.
						result = false;
					}
					else
					{
						if ( (d&Direction.Running) != 0 )
							mount.OnRunningStep( this );
						else
							mount.OnWalkingStep( this );

						if ( mount.Stam <= 1 || mount.Stam == (int)(mount.StamMax/5) )
							SendLocalizedMessage( 500133 ); // Your mount is very fatigued.
					}
				}
				else if ( this.Stam <= 1 )
				{
					this.Stam = 0;
					SendLocalizedMessage( 500110 ); // You are too fatigued to move.
					result = false;
				}
			}

			return result;
		}

		#region Fastwalk Prevention
		private DateTime m_NextMovementTime;

		public virtual bool UsesFastwalkPrevention{ get{ return ( AccessLevel < AccessLevel.Counselor ) || Mount is BaseMount; } }
		public virtual bool CanMoveNow { get { return !UsesFastwalkPrevention || m_NextMovementTime <= DateTime.Now+TimeSpan.FromMilliseconds( 10 ); } }

		public virtual TimeSpan ComputeMovementSpeed( Direction dir )
		{
			double sec;
			bool running = ( (dir & Direction.Running) != 0 );

			if ( this.Mount is BaseMount )
			{
				sec = ( running ? 0.1 : 0.2 ) * ((BaseMount)this.Mount).GetSpeedScalar( this, dir );

				if ( (dir & Direction.Mask) != (this.Direction & Direction.Mask) )
					sec /= 2.0;
			}
			else 
			{
				sec = ( running ? 0.2 : 0.4 );

				if ( (dir & Direction.Mask) != (this.Direction & Direction.Mask) || m_NextMovementTime+TimeSpan.FromSeconds( 0.5 ) < DateTime.Now )
					sec = 0.1;
			}

			return TimeSpan.FromSeconds( sec );
		}
		
		private static void MovementHandler( NetState ns, PacketReader pvSrc )
		{
			PlayerMobile pm = ns.Mobile as PlayerMobile;

			if ( pm == null )
			{
				m_OldWalkReq( ns, pvSrc );
			}
			else
			{
				Direction dir = (Direction)pvSrc.ReadByte();
				byte seq = pvSrc.ReadByte();
                Console.WriteLine("Script Got dir {0} and seq {1}", dir.ToString(), seq.ToString());
                MovementController.Enqueue( pm, dir, seq );
			}
		}
		
		private Queue m_MoveReqs;
		
		private class MovementController : Timer
		{
			private static ArrayList m_List = new ArrayList();
			private static ArrayList m_Bounced = new ArrayList();

			public static void OnDisconnected( PlayerMobile pm )
			{
				try
				{
					if ( pm.m_MoveReqs != null )
					{
						while ( pm.m_MoveReqs.Count > 0 )
							((Entry)pm.m_MoveReqs.Dequeue()).Free();
					}
				}
				catch
				{
				}
				pm.m_MoveReqs = null;
			}

			public static void Enqueue( PlayerMobile pm, Direction dir, byte seq )
			{
				if ( pm.CanMoveNow && ( pm.m_MoveReqs == null || pm.m_MoveReqs.Count <= 0 ) )
				{
					Process( pm, dir, seq );
				}
				else
				{
					if ( pm.m_MoveReqs == null )
						pm.m_MoveReqs = new Queue();
					
					if ( pm.m_MoveReqs.Count < 25 )
					{
						if ( pm.m_MoveReqs.Count <= 0 )
							m_List.Add( pm );
						pm.m_MoveReqs.Enqueue( Entry.Instanciate( dir, seq ) );
					}
					else
					{
						pm.m_MoveReqs.Clear();
						pm.Send( new MovementRej( seq, pm ) );
					}
				}
			}

			private static void Process( PlayerMobile pm, Direction dir, int seq )
			{
				if ( (pm.NetState.Sequence == 0 && seq != 0) || !pm.Move( dir ) )
				{
					pm.Send( new MovementRej( seq, pm ) );
					pm.NetState.Sequence = 0;

					pm.ClearFastwalkStack();
				}
				else
				{
					++seq;

					if ( seq == 256 )
						seq = 1;

					pm.NetState.Sequence = seq;
				}
			}

			protected override void OnTick()
			{
				ArrayList list = new ArrayList( m_List );
				for ( int i=0;i<list.Count;i++ )
				{
					PlayerMobile pm = list[i] as PlayerMobile;
					if ( pm == null )
						continue;

					m_List.Remove( pm );

					if ( pm.NetState == null || !pm.NetState.Running || pm.m_MoveReqs == null )
						continue;

					while ( pm.CanMoveNow && pm.m_MoveReqs.Count > 0 )
					{
						Entry e = (Entry)pm.m_MoveReqs.Dequeue();
						Process( pm, e.Direction, e.Sequence );
						e.Free();
					}

					if ( pm.m_MoveReqs.Count > 0 )
						m_List.Add( pm );
				}
			}

			public MovementController() : base( TimeSpan.Zero, TimeSpan.Zero )
			{
				Priority = TimerPriority.TenMS;
			}

			private class Entry
			{
				private static Queue m_Free = new Queue();
				public static Entry Instanciate( Direction dir, byte seq )
				{
					Entry e;
					if ( m_Free.Count <= 0 )
						e = new Entry();
					else
						e = (Entry)m_Free.Dequeue();

					if ( m_Free.Count > 1000 )
						m_Free.Clear();

					e.Direction = dir;
					e.Sequence = seq;
					return e;
				}

				public void Free()
				{
					m_Free.Enqueue( this );
				}

				private Entry()
				{
				}

				public Direction Direction;
				public byte Sequence;
			}
		}

		/*
		private class MovementController : Timer
		{
			private static Queue m_Queue = new Queue();
			private static ArrayList m_Bounced = new ArrayList();

			public static void Enqueue( NetState ns, Direction dir, byte seq )
			{
				m_Queue.Enqueue( Entry.Instanciate( ns, dir, seq ) );
			}

			public MovementQueue() : base( TimeSpan.Zero, TimeSpan.Zero )
			{
				Priority = TimerPriority.TenMS;
			}

			protected override void OnTick()
			{
				if ( m_Queue.Count <= 0 )
					return;

				Queue oldQueue = m_Queue;
				m_Queue = new Queue();

				while ( oldQueue.Count > 0 )
				{
					Entry e = (Entry)oldQueue.Dequeue();
					
					if ( e.State == null || !e.State.Running || e.State.Mobile == null )
					{
						e.Free();
						continue;
					}

					Mobile m = e.State.Mobile;
					PlayerMobile pm = m as PlayerMobile;
					if ( pm == null || pm.CanMoveNow )
					{
						int seq = e.Sequence;
						try
						{
							bool alreadyBounced = m_Bounced.Contains( m );
							if ( alreadyBounced || (e.State.Sequence == 0 && seq != 0) || !m.Move( e.Direction ) )
							{
								e.State.Send( new MovementRej( seq, m ) );
								e.State.Sequence = 0;

								m.ClearFastwalkStack();

								if ( !alreadyBounced )
									m_Bounced.Add( m );
							}
							else
							{
								++seq;

								if ( seq == 256 )
									seq = 1;

								e.State.Sequence = seq;
							}
						}
						catch ( Exception ex )
						{
							Server.Misc.CrashGuard.GenerateCrashReport( ex );
						}
						e.Free();
					}
					else 
					{
						try { m_Queue.Enqueue( e ); } 
						catch ( Exception ex ) { Server.Misc.CrashGuard.GenerateCrashReport( ex ); }
					}
				}

				m_Bounced.Clear();
			}

			private class Entry
			{
				private static Queue m_Free = new Queue();
				public static Entry Instanciate( NetState ns, Direction dir, byte seq )
				{
					Entry e;
					if ( m_Free.Count <= 0 )
						e = new Entry();
					else
						e = (Entry)m_Free.Dequeue();

					if ( m_Free.Count > 1000 )
						m_Free.Clear();

					e.State = ns;
					e.Direction = dir;
					e.Sequence = seq;
					return e;
				}

				public void Free()
				{
					m_Free.Enqueue( this );
					State = null;
				}

				private Entry()
				{
				}

				public NetState State;
				public Direction Direction;
				public byte Sequence;
			}
		}*/
		#endregion

		private bool m_LastProtectedMessage;
		private int m_NextProtectionCheck = 10;

		public virtual void RecheckTownProtection()
		{
			m_NextProtectionCheck = 10;

			Regions.GuardedRegion reg = this.Region as Regions.GuardedRegion;
			bool isProtected = ( reg != null && !reg.IsDisabled() );

			if ( isProtected != m_LastProtectedMessage )
			{
				if ( isProtected )
					SendAsciiMessage( "You are now under the protection of Lord British's guards." );//SendLocalizedMessage( 500112 ); // You are now under the protection of the town guards.
				else
					SendAsciiMessage( "You have left the protection of Lord British's guards." ); // You have left the protection of the town guards.

				m_LastProtectedMessage = isProtected;
			}
		}

		public override void MoveToWorld( Point3D loc, Map map )
		{
			base.MoveToWorld( loc, map );

			RecheckTownProtection();
		}

		public override void SetLocation( Point3D loc, bool isTeleport )
		{
			base.SetLocation( loc, isTeleport );

			if ( isTeleport || --m_NextProtectionCheck == 0 )
				RecheckTownProtection();
		}


		public override void GetContextMenuEntries( Mobile from, List<ContextMenuEntry> list )
		{
			base.GetContextMenuEntries( from, list );

			if ( from == this )
			{
				if ( InsuranceEnabled )
				{
					list.Add( new CallbackEntry( 6201, new ContextCallback( ToggleItemInsurance ) ) );

					if ( AutoRenewInsurance )
						list.Add( new CallbackEntry( 6202, new ContextCallback( CancelRenewInventoryInsurance ) ) );
					else
						list.Add( new CallbackEntry( 6200, new ContextCallback( AutoRenewInventoryInsurance ) ) );
				}

				// TODO: Toggle champ titles

				BaseHouse house = BaseHouse.FindHouseAt( this );

				if ( house != null && house.IsAosRules )
					list.Add( new CallbackEntry( 6207, new ContextCallback( LeaveHouse ) ) );

				if ( m_JusticeProtectors.Count > 0 )
					list.Add( new CallbackEntry( 6157, new ContextCallback( CancelProtection ) ) );
			}
		}

		private void CancelProtection()
		{
			for ( int i = 0; i < m_JusticeProtectors.Count; ++i )
			{
				Mobile prot = (Mobile)m_JusticeProtectors[i];

				string args = String.Format( "{0}\t{1}", this.Name, prot.Name );

				prot.SendLocalizedMessage( 1049371, args ); // The protective relationship between ~1_PLAYER1~ and ~2_PLAYER2~ has been ended.
				this.SendLocalizedMessage( 1049371, args ); // The protective relationship between ~1_PLAYER1~ and ~2_PLAYER2~ has been ended.
			}

			m_JusticeProtectors.Clear();
		}

		private void ToggleItemInsurance()
		{
			BeginTarget( -1, false, TargetFlags.None, new TargetCallback( ToggleItemInsurance_Callback ) );
			SendLocalizedMessage( 1060868 ); // Target the item you wish to toggle insurance status on <ESC> to cancel
		}

		private bool CanInsure( Item item )
		{
			if ( item is Container )
				return false;

			if ( item is Spellbook || item is PotionKeg )
				return false;

			if ( item.Stackable )
				return false;

			if ( item.LootType == LootType.Cursed )
				return false;

			if ( item.ItemID == 0x204E ) // death shroud
				return false;

			return true;
		}

		private void ToggleItemInsurance_Callback( Mobile from, object obj )
		{
			Item item = obj as Item;

			if ( item == null || !item.IsChildOf( this ) )
			{
				BeginTarget( -1, false, TargetFlags.None, new TargetCallback( ToggleItemInsurance_Callback ) );
				SendLocalizedMessage( 1060871, "", 0x23 ); // You can only insure items that you have equipped or that are in your backpack
			}
			else if ( item.Insured )
			{
				item.Insured = false;

				SendLocalizedMessage( 1060874, "", 0x35 ); // You cancel the insurance on the item

				BeginTarget( -1, false, TargetFlags.None, new TargetCallback( ToggleItemInsurance_Callback ) );
				SendLocalizedMessage( 1060868, "", 0x23 ); // Target the item you wish to toggle insurance status on <ESC> to cancel
			}
			else if ( !CanInsure( item ) )
			{
				BeginTarget( -1, false, TargetFlags.None, new TargetCallback( ToggleItemInsurance_Callback ) );
				SendLocalizedMessage( 1060869, "", 0x23 ); // You cannot insure that
			}
			else if ( item.LootType == LootType.Blessed || item.LootType == LootType.Newbied || item.BlessedFor == from )
			{
				BeginTarget( -1, false, TargetFlags.None, new TargetCallback( ToggleItemInsurance_Callback ) );
				SendLocalizedMessage( 1060870, "", 0x23 ); // That item is blessed and does not need to be insured
				SendLocalizedMessage( 1060869, "", 0x23 ); // You cannot insure that
			}
			else
			{
				if ( !item.PayedInsurance )
				{
					if ( Banker.Withdraw( from, 600 ) )
					{
						SendLocalizedMessage( 1060398, "600" ); // ~1_AMOUNT~ gold has been withdrawn from your bank box.
						item.PayedInsurance = true;
					}
					else
					{
						SendLocalizedMessage( 1061079, "", 0x23 ); // You lack the funds to purchase the insurance
						return;
					}
				}

				item.Insured = true;

				SendLocalizedMessage( 1060873, "", 0x23 ); // You have insured the item

				BeginTarget( -1, false, TargetFlags.None, new TargetCallback( ToggleItemInsurance_Callback ) );
				SendLocalizedMessage( 1060868, "", 0x23 ); // Target the item you wish to toggle insurance status on <ESC> to cancel
			}
		}

		private void AutoRenewInventoryInsurance()
		{
			SendLocalizedMessage( 1060881, "", 0x23 ); // You have selected to automatically reinsure all insured items upon death
			AutoRenewInsurance = true;
		}

		private void CancelRenewInventoryInsurance()
		{
			SendLocalizedMessage( 1061075, "", 0x23 ); // You have cancelled automatically reinsuring all insured items upon death
			AutoRenewInsurance = false;
		}

		// TODO: Champ titles, toggle

		private void LeaveHouse()
		{
			BaseHouse house = BaseHouse.FindHouseAt( this );

			if ( house != null )
				this.Location = house.BanLocation;
		}

		private delegate void ContextCallback();

		private class CallbackEntry : ContextMenuEntry
		{
			private ContextCallback m_Callback;

			public CallbackEntry( int number, ContextCallback callback ) : this( number, -1, callback )
			{
			}

			public CallbackEntry( int number, int range, ContextCallback callback ) : base( number, range )
			{
				m_Callback = callback;
			}

			public override void OnClick()
			{
				if ( m_Callback != null )
					m_Callback();
			}
		}

		public override void Damage( int amount, Mobile from )
		{
			if ( this.Spell is Spell )
				((Spell)this.Spell).OnCasterHurt( amount );

			if ( from != null && ( this.Combatant == null || this.Combatant.Deleted || !this.Combatant.Alive || !this.Combatant.InRange( this, 15 ) ) )
				this.Combatant = from;

			base.Damage (amount, from);
		}

		public override void OnDamage( int amount, Mobile from, bool willKill )
		{
			WeightOverloading.FatigueOnDamage( this, amount );

			base.OnDamage( amount, from, willKill );
		}

		public static int ComputeSkillTotal( Mobile m )
		{
			int total = 0;

			for ( int i = 0; i < m.Skills.Length; ++i )
				total += m.Skills[i].BaseFixedPoint;

			return ( total / 10 );
		}

		public override void Resurrect()
		{
			bool wasAlive = this.Alive;

			base.Resurrect();

			if ( Alive && !wasAlive )
			{
				MagicDamageAbsorb = 0;
				MeleeDamageAbsorb = 0;

				Criminal = false;
				SkillHandlers.Stealing.ClearFor( this );

				Item deathRobe = null;
				if ( Backpack != null )
				{
					deathRobe = Backpack.FindItemByType( typeof( DeathRobe ) );
				
					if ( deathRobe == null )
						deathRobe = deathRobe = Backpack.FindItemByType( typeof( Robe ) );
				}

				if ( deathRobe == null )
					deathRobe = new DeathRobe();

				if ( !EquipItem( deathRobe ) )
					deathRobe.Delete();
			}
		}

		public override DeathMoveResult GetParentMoveResultFor( Item item )
		{
			if ( item.LootType == LootType.Newbied )
				return DeathMoveResult.MoveToBackpack;
			else
				return base.GetParentMoveResultFor( item );
		}

		public override DeathMoveResult GetInventoryMoveResultFor( Item item )
		{
			if ( item.LootType == LootType.Newbied )
				return DeathMoveResult.MoveToBackpack;
			else
				return base.GetInventoryMoveResultFor( item );
		}

		private Point3D m_DeathLoc;
		public Point3D DeathLocation
		{
			get { return m_DeathLoc; }
		}

		public override void OnDeath( Container c )
		{
			base.OnDeath( c );

			HueMod = -1;
			NameMod = null;
			SavagePaintExpiration = TimeSpan.Zero;

			SetHairMods( -1, -1 );

			PolymorphSpell.StopTimer( this );
			IncognitoSpell.StopTimer( this );
			DisguiseGump.StopTimer( this );

			EndAction( typeof( PolymorphSpell ) );
			EndAction( typeof( IncognitoSpell ) );

			m_PermaFlags.Clear();

			m_DeathLoc = this.Location;

			// if these are sent right away, the client closes them as part of its death stuff.
			// freeze them for a *short* time so they dont move by accident before they get the menus.
			if ( !this.AssumePlayAsGhost )
				this.Freeze( TimeSpan.FromSeconds( 2.25 ) ); 
			ProcDeathMenus();
		}

		private void ProcDeathMenus()
		{
			int karmaGive = 0;
			if ( this.Karma >= (int)Noto.LowNeutral )
				karmaGive = -10 - ( this.Karma > 0 ? this.Karma / 20 : 0 );
			else if ( this.Karma <= (int)Noto.Dastardly )
				karmaGive = 1;
			// else for Dishonorable no gain/loss
			
			if ( karmaGive != 0 )
			{
				ArrayList killers = new ArrayList(); 
				foreach ( AggressorInfo ai in this.Aggressors ) 
				{
					Mobile att = ai.Attacker;
					if ( att.Player )
					{
						if ( karmaGive < 0 )
						{
							if ( ai.CanReportMurder )
							{ 
								if ( !ai.Reported )
								{
									ai.Reported = true; 
									killers.Add( ai.Attacker ); 
								}
							}
							else
							{
								continue;
							}
						}
						
						// only give a red karma if they kill someone with (much) lower karma than them
						// otherwise, take karma away
						int noto = Notoriety.Compute( ai.Attacker, this );
						if ( noto != Notoriety.Ally && noto != Notoriety.Enemy )
						{
							if ( ai.Attacker.Karma > (int)Noto.LordLady && karmaGive < 0 )
								Misc.Titles.AlterNotoriety( ai.Attacker, (int)(karmaGive*1.5) );
							else if ( ai.Attacker.Karma+10 < this.Karma && karmaGive > 0 )
								Misc.Titles.AlterNotoriety( att, -10 );
							else
								Misc.Titles.AlterNotoriety( ai.Attacker, karmaGive );
						}
					}
				} 

				if ( killers != null && killers.Count > 0 )
				{
					m_NextBountyDecay = DateTime.Now + TimeSpan.FromDays( 1.0 );
					ReportMurderer.SetKillers( this, killers );
				}
			} 
			
			Timer.DelayCall( TimeSpan.FromSeconds( 2.00 ), new TimerCallback( GiveDeathMenus ) );
		}
		
		private void GiveDeathMenus()
		{
			if ( this.NetState != null )
				ReportMurderer.SendNext( this );
		}

		private ArrayList m_PermaFlags;
		private ArrayList m_VisList;
		private Hashtable m_AntiMacroTable;
		private TimeSpan m_GameTime;
		private TimeSpan m_ShortTermElapse;
		private TimeSpan m_LongTermElapse;
		private DateTime m_SessionStart;
		private DateTime m_LastEscortTime;
		private DateTime m_NextBountyDecay;
		private SkillName m_Learning = (SkillName)(-1);
		private int m_Cohesion;
		private DateTime m_LastCohesion;
		private bool m_AssumeGhost;

		[CommandProperty( AccessLevel.GameMaster )]
		public DateTime NextBountyDecay
		{
			get
			{
				return m_NextBountyDecay;
			}
			set
			{
				m_NextBountyDecay = value;
			}
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public int SpiritCohesion
		{
			get
			{
				while ( m_Cohesion < 4 && m_LastCohesion+TimeSpan.FromMinutes( 5 ) < DateTime.Now )
				{
					m_Cohesion++;
					m_LastCohesion += TimeSpan.FromMinutes( 5 );
				}

				return m_Cohesion;
			}
			set
			{
				if ( m_Cohesion > value ) // going down?
					m_LastCohesion = DateTime.Now;
				m_Cohesion = value;
			}
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public DateTime LastSpiritCohesion
		{
			get { return m_LastCohesion; }
			set { m_LastCohesion = value; }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public bool AssumePlayAsGhost
		{
			get { return m_AssumeGhost; }
			set { m_AssumeGhost = value; }
		}

		public SkillName Learning
		{
			get{ return m_Learning; }
			set{ m_Learning = value; }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public TimeSpan SavagePaintExpiration
		{
			get
			{
				return TimeSpan.Zero;
			}
			set
			{
			}
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public TimeSpan NextSmithBulkOrder
		{
			get
			{
				return TimeSpan.Zero;
			}
			set
			{
			}
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public TimeSpan NextTailorBulkOrder
		{
			get
			{
				return TimeSpan.Zero;
			}
			set
			{
			}
		}

		public DateTime LastEscortTime
		{
			get{ return m_LastEscortTime; }
			set{ m_LastEscortTime = value; }
		}

		public PlayerMobile()
		{
			m_VisList = new ArrayList();
			m_PermaFlags = new ArrayList();
			m_AntiMacroTable = new Hashtable();
			m_SkillUsageOrder = new byte[PlayerMobile.SkillCount];
			for(int i=0;i<PlayerMobile.SkillCount;i++)
				m_SkillUsageOrder[i] = (byte)i;

			m_GameTime = TimeSpan.Zero;
			m_ShortTermElapse = TimeSpan.FromHours( 8.0 );
			m_LongTermElapse = TimeSpan.FromHours( 40.0 );

			m_NextBountyDecay = DateTime.Now + TimeSpan.FromDays( 1.0 );

			m_JusticeProtectors = new ArrayList();

			InvalidateMyRunUO();

			m_LastCohesion = DateTime.Now - TimeSpan.FromHours( 1.0 );
			m_Cohesion = 4;
		}
		
		public PlayerMobile( Serial s ) : base( s )
		{
			m_VisList = new ArrayList();
			m_AntiMacroTable = new Hashtable();
			InvalidateMyRunUO();
		}

		//public override bool MutateSpeech( ArrayList hears, ref string text, ref object context )
        public override bool MutateSpeech(System.Collections.Generic.List<Mobile> hears, ref string text, ref object context)
		{
			if ( Alive )
				return false;

			if ( Core.AOS )
			{
				for ( int i = 0; i < hears.Count; ++i )
				{
					object o = hears[i];

					if ( o != this && o is Mobile && ((Mobile)o).Skills[SkillName.SpiritSpeak].Value >= 100.0 )
						return false;
				}
			}

			return base.MutateSpeech( hears, ref text, ref context );
		}

		public override ApplyPoisonResult ApplyPoison( Mobile from, Poison poison )
		{
			if ( !Alive )
				return ApplyPoisonResult.Immune;

			return base.ApplyPoison( from, poison );
		}

		public ArrayList VisibilityList
		{
			get{ return m_VisList; }
		}

		public ArrayList PermaFlags
		{
			get{ return m_PermaFlags; }
		}

		public override int Luck{ get{ return AosAttributes.GetValue( this, AosAttribute.Luck ); } }

		public static bool IsGuarded( Mobile m )
		{
			return m.Region is Regions.GuardedRegion && !((Regions.GuardedRegion)m.Region).IsDisabled();
		}

		public override bool IsBeneficialCriminal(Mobile target)
		{
			return false; // heal anyone you want
		}
		
		public static bool CheckShieldOpposition( Mobile from, Mobile target )
		{
			Item fs = from.FindItemOnLayer( Layer.TwoHanded );
			Item ts = target.FindItemOnLayer( Layer.TwoHanded );
			
			return ( fs is OrderShield && ts is ChaosShield ) || ( fs is ChaosShield && ts is OrderShield );
		}

		public override bool IsHarmfulCriminal( Mobile target )
		{
			if ( !target.Player )
			{
				// use generic (Notoriety.Compute) method for pets & creatures
				return base.IsHarmfulCriminal( target );
			}
			else
			{
				if ( target.Criminal || target == this || target.AccessLevel > AccessLevel.Player )
					return false;// always ok to attack criminals, self, and staff

				Guild fromGuild = this.Guild as Guild;
				Guild targetGuild = target.Guild as Guild;

				if ( fromGuild != null && targetGuild != null )
				{
					if ( fromGuild == targetGuild || fromGuild.IsAlly( targetGuild ) || fromGuild.IsEnemy( targetGuild ) )
						return false; // always ok to attack guild stuffs
				}

				if ( NotorietyHandlers.CheckAggressor( this.Aggressors, target ) || NotorietyHandlers.CheckAggressed( this.Aggressed, target ) )
					return false; // always ok to attack aggressors
				
				if ( Server.SkillHandlers.Stealing.AttackOK( this, target ) )
					return false;

				if ( CheckShieldOpposition( this, target ) )
					return false;

				if ( target.Karma <= (int)Noto.Dark )
					return false; // dark/evil/dread are always ok to attack
				else if ( target.Karma <= (int)Noto.Dishonorable ) 
					return IsGuarded( target );// its a criminal action to attack dishonorable and dastardly only while they're in town
				else // Innocent
					return true;
			}
		}

		public bool AntiMacroCheck( Skill skill, object obj )
		{
			if ( obj == null || m_AntiMacroTable == null || this.AccessLevel != AccessLevel.Player )
				return true;

			Hashtable tbl = (Hashtable)m_AntiMacroTable[skill];
			if ( tbl == null )
				m_AntiMacroTable[skill] = tbl = new Hashtable();

			CountAndTimeStamp count = (CountAndTimeStamp)tbl[obj];
			if ( count != null )
			{
				if ( count.TimeStamp + SkillCheck.AntiMacroExpire <= DateTime.Now )
				{
					count.Count = 1;
					return true;
				}
				else
				{
					++count.Count;
					if ( count.Count <= SkillCheck.Allowance )
						return true;
					else
						return false;
				}
			}
			else
			{
				tbl[obj] = count = new CountAndTimeStamp();
				count.Count = 1;
				
				return true;
			}
		}

		private void RevertHair()
		{
			SetHairMods( -1, -1 );
		}

		private byte[] m_SkillUsageOrder;

		public byte[] SkillUsage { get { return m_SkillUsageOrder; } }

		public virtual void OnSkillUsed( SkillName sk )
		{
			if ( m_SkillUsageOrder == null || m_SkillUsageOrder.Length < 1 )
				return;

			int i;
			for(i=0;i<m_SkillUsageOrder.Length-1;i++)
			{
				if ( m_SkillUsageOrder[i] == (byte)sk )
					break;
			}
			for(;i>0;i--)
				m_SkillUsageOrder[i] = m_SkillUsageOrder[i-1];
			m_SkillUsageOrder[0] = (byte)sk;
		}

		public SkillName GetSkillToLower( SkillName raise, int amount )
		{
			byte[] canLower = new byte[3]{255,255,255};
			int cl = 0;
			for( int i=m_SkillUsageOrder.Length-1; i>=0 && cl < canLower.Length; i--)
			{
				if ( m_SkillUsageOrder[i] != (byte)raise && this.Skills[m_SkillUsageOrder[i]].BaseFixedPoint >= amount )
				{
					if ( cl < canLower.Length )
						canLower[cl] = m_SkillUsageOrder[i];
					cl++;
				}
			}
			
			if ( cl > 0 )
			{
				int rnd = Utility.Random( 100 );
				if ( rnd < 5 && Skills.Total >= 600 )
					rnd = 2;
				else if ( rnd < 40 && Skills.Total >= 475 )
					rnd = 1;
				else
					rnd = 0;
				
				return (SkillName)canLower[rnd];
			}
			else
			{
				return raise;
			}
		}
		
		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();

			switch ( version )
			{
				case 18:
				{
					m_NextBountyDecay = reader.ReadDateTime();
					goto case 17;
				}
				case 17:
				{
					m_LastUpdate = reader.ReadInt();
					m_LastLogin = reader.ReadDateTime();

					goto case 16;
				}
				case 16:
				{
					m_NextNotoUp = reader.ReadDateTime();
					m_Cohesion = reader.ReadInt();
					m_LastCohesion = DateTime.Now - reader.ReadTimeSpan();
					m_AssumeGhost = reader.ReadBool();

					int skillCount = reader.ReadByte();
					m_SkillUsageOrder = new byte[skillCount];
					for(int i=0;i<skillCount;i++)
						m_SkillUsageOrder[i] = reader.ReadByte();

					goto case 15;
				}
				case 15:
				{
					m_Bounty = reader.ReadInt();
					
					goto case 14;
				}
				case 14:
				{
					m_CompassionGains = reader.ReadEncodedInt();

					if ( m_CompassionGains > 0 )
						m_NextCompassionDay = reader.ReadDeltaTime();

					goto case 13;
				}
				case 13: // just removed m_PayedInsurance list
				case 12:
				{
					goto case 11;
				}
				case 11:
				{
					if ( version < 13 )
					{
						ArrayList payed = reader.ReadItemList();

						for ( int i = 0; i < payed.Count; ++i )
							((Item)payed[i]).PayedInsurance = true;
					}

					goto case 10;
				}
				case 10:
				{
					if ( reader.ReadBool() )
					{
						m_HairModID = reader.ReadInt();
						m_HairModHue = reader.ReadInt();
						m_BeardModID = reader.ReadInt();
						m_BeardModHue = reader.ReadInt();

						// We cannot call SetHairMods( -1, -1 ) here because the items have not yet loaded
						Timer.DelayCall( TimeSpan.Zero, new TimerCallback( RevertHair ) );
					}

					goto case 9;
				}
				case 9:
				{
					SavagePaintExpiration = reader.ReadTimeSpan();

					if ( SavagePaintExpiration > TimeSpan.Zero )
					{
						BodyMod = ( Female ? 184 : 183 );
						HueMod = 0;
					}

					goto case 8;
				}
				case 8:
				{
					m_NpcGuild = (NpcGuild)reader.ReadInt();
					m_NpcGuildJoinTime = reader.ReadDateTime();
					m_NpcGuildGameTime = reader.ReadTimeSpan();
					goto case 7;
				}
				case 7:
				{
					/*m_PermaFlags =*/ reader.ReadMobileList();
					goto case 6;
				}
				case 6:
				{
					NextTailorBulkOrder = reader.ReadTimeSpan();
					goto case 5;
				}
				case 5:
				{
					NextSmithBulkOrder = reader.ReadTimeSpan();
					goto case 4;
				}
				case 4:
				{
					m_LastJusticeLoss = reader.ReadDeltaTime();
					m_JusticeProtectors = reader.ReadMobileList();
					goto case 3;
				}
				case 3:
				{
					m_LastSacrificeGain = reader.ReadDeltaTime();
					m_LastSacrificeLoss = reader.ReadDeltaTime();
					m_AvailableResurrects = reader.ReadInt();
					goto case 2;
				}
				case 2:
				{
					m_Flags = (PlayerFlag)reader.ReadInt();
					goto case 1;
				}
				case 1:
				{
					m_LongTermElapse = reader.ReadTimeSpan();
					m_ShortTermElapse = reader.ReadTimeSpan();
					m_GameTime = reader.ReadTimeSpan();
					goto case 0;
				}
				case 0:
				{
					break;
				}
			}

			if ( m_PermaFlags == null )
				m_PermaFlags = new ArrayList();

			if ( m_JusticeProtectors == null )
				m_JusticeProtectors = new ArrayList();

			List<Mobile> list = this.Stabled;

			for ( int i = 0; i < list.Count; ++i )
			{
				BaseCreature bc = list[i] as BaseCreature;

				if ( bc != null )
					bc.IsStabled = true;
			}

			if ( m_NextBountyDecay == DateTime.MinValue )
			{
				if ( m_LastLogin != DateTime.MinValue )
					m_NextBountyDecay = m_LastLogin + TimeSpan.FromDays( 1.0 );
			}
			
			while ( m_Bounty > 0 && m_NextBountyDecay < DateTime.Now )
			{
				m_Bounty -= 100;
				m_NextBountyDecay += TimeSpan.FromDays( 1.0 );
			}
			
			if ( m_Bounty <= 0 )
			{
				m_Bounty = 0;
				Kills = 0;
			}

			if ( m_Bounty > 0 && m_Bounty > BountyBoard.LowestBounty )
				BountyBoard.Update( this );

			if ( m_SkillUsageOrder == null )
			{
				m_SkillUsageOrder = new byte[PlayerMobile.SkillCount];
				for(int i=0;i<PlayerMobile.SkillCount;i++)
					m_SkillUsageOrder[i] = (byte)i;
			}
		}
		
		public override void Serialize( GenericWriter writer )
		{
			//cleanup our anti-macro table 
			foreach ( Hashtable t in m_AntiMacroTable.Values )
			{
				ArrayList remove = new ArrayList();
				foreach ( CountAndTimeStamp time in t.Values )
				{
					if ( time.TimeStamp + SkillCheck.AntiMacroExpire <= DateTime.Now )
						remove.Add( time );
				}

				for (int i=0;i<remove.Count;++i)
					t.Remove( remove[i] );
			}

			if ( m_NextBountyDecay != DateTime.MinValue )
			{
				bool update = false;
				while ( m_Bounty > 0 && m_NextBountyDecay < DateTime.Now )
				{
					update = true;
					m_Bounty -= 100;
					m_NextBountyDecay += TimeSpan.FromDays( 1.0 );
				}

				if ( m_Bounty < 0 )
					m_Bounty = 0;

				if ( update )
					BountyBoard.Update( this );
			}

			base.Serialize( writer );
			
			writer.Write( (int) 18 ); // version

			writer.Write( m_NextBountyDecay );

			writer.Write( m_LastUpdate );
			writer.Write( m_LastLogin );

			writer.Write( m_NextNotoUp );
			writer.Write( m_Cohesion );
			writer.Write( (TimeSpan)(DateTime.Now - m_LastCohesion) );
			writer.Write( m_AssumeGhost );
			writer.Write( (byte)m_SkillUsageOrder.Length );
			for(int i=0;i<m_SkillUsageOrder.Length;i++)
				writer.Write( (byte)m_SkillUsageOrder[i] );

         	writer.Write( (int) m_Bounty ); 

			writer.WriteEncodedInt( m_CompassionGains );

			if ( m_CompassionGains > 0 )
				writer.WriteDeltaTime( m_NextCompassionDay );

			bool useMods = ( m_HairModID != -1 || m_BeardModID != -1 );

			writer.Write( useMods );

			if ( useMods )
			{
				writer.Write( (int) m_HairModID );
				writer.Write( (int) m_HairModHue );
				writer.Write( (int) m_BeardModID );
				writer.Write( (int) m_BeardModHue );
			}

			writer.Write( SavagePaintExpiration );

			writer.Write( (int) m_NpcGuild );
			writer.Write( (DateTime) m_NpcGuildJoinTime );
			writer.Write( (TimeSpan) m_NpcGuildGameTime );

			writer.WriteMobileList( m_PermaFlags, true );

			writer.Write( NextTailorBulkOrder );

			writer.Write( NextSmithBulkOrder );

			writer.WriteDeltaTime( m_LastJusticeLoss );
			writer.WriteMobileList( m_JusticeProtectors, true );

			writer.WriteDeltaTime( m_LastSacrificeGain );
			writer.WriteDeltaTime( m_LastSacrificeLoss );
			writer.Write( m_AvailableResurrects );

			writer.Write( (int) m_Flags );

			writer.Write( m_LongTermElapse );
			writer.Write( m_ShortTermElapse );
			writer.Write( this.GameTime );
		}

		public void ResetKillTime()
		{
			m_ShortTermElapse = this.GameTime + TimeSpan.FromHours( 8 );
			m_LongTermElapse = this.GameTime + TimeSpan.FromHours( 40 );
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public TimeSpan GameTime
		{
			get
			{
				if ( NetState != null )
					return m_GameTime + (DateTime.Now - m_SessionStart);
				else
					return m_GameTime;
			}
		}

		public void SetGameTime( TimeSpan v )
		{
			m_GameTime = v;
		}

		public override bool CanSee( Mobile m )
		{
			if ( m.Deleted )
				return false;

			if ( m is PlayerMobile && ((PlayerMobile)m).m_VisList.Contains( this ) )
				return true;
			
			if ( this.AccessLevel >= m.AccessLevel && this.AccessLevel > AccessLevel.Player )
				return true;

			return base.CanSee( m );
		}

		private void AddSpeechItemsFrom( ArrayList list, Container cont )
		{
			for ( int i = 0; i < cont.Items.Count; ++i )
			{
				Item item = (Item)cont.Items[i];

				if ( item.HandlesOnSpeech )
					list.Add( item );

				if ( item is Container )
					AddSpeechItemsFrom( list, (Container)item );
			}
		}

		private class LocationComparer : IComparer
		{
			private IPoint3D m_RelativeTo;

			public LocationComparer( IPoint3D relativeTo )
			{
				m_RelativeTo = relativeTo;
			}

			private int GetDistance( IPoint3D p )
			{
				int x = m_RelativeTo.X - p.X;
				int y = m_RelativeTo.Y - p.Y;
				int z = m_RelativeTo.Z - p.Z;

				x *= 11;
				y *= 11;

				return (x*x) + (y*y) + (z*z);
			}

			public int Compare( object x, object y )
			{
				IPoint3D a = x as IPoint3D;
				IPoint3D b = y as IPoint3D;

				return GetDistance( a ) - GetDistance( b );
			}
		}

		public override void OnSaid( SpeechEventArgs e )
		{
			if ( Squelched )
			{
				this.SendLocalizedMessage( 500168 ); // You can not say anything, you have been squelched.
				e.Blocked = true;
			}
		}

		private static KeywordList m_KeywordList = new KeywordList();
		private static int[] m_EmptyInts = new int[0];

		public static void UnicodeSpeech( NetState state, PacketReader pvSrc )
		{
			Mobile from = state.Mobile;

			MessageType type = (MessageType)pvSrc.ReadByte();
			int hue = pvSrc.ReadInt16();
			pvSrc.ReadInt16(); // font
			string lang = pvSrc.ReadString( 4 );
			string text;

			bool isEncoded = (type & MessageType.Encoded) != 0;
			int[] keywords;

			if ( isEncoded )
			{
				int value = pvSrc.ReadInt16();
				int count = (value & 0xFFF0) >> 4;
				int hold = value & 0xF;

				if ( count < 0 || count > 50 )
					return;

				KeywordList keyList = m_KeywordList;

				for ( int i = 0; i < count; ++i )
				{
					int speechID;

					if ( (i & 1) == 0 )
					{
						hold <<= 8;
						hold |= pvSrc.ReadByte();
						speechID = hold;
						hold = 0;
					}
					else
					{
						value = pvSrc.ReadInt16();
						speechID = (value & 0xFFF0) >> 4;
						hold = value & 0xF;
					}

					if ( !keyList.Contains( speechID ) )
						keyList.Add( speechID );
				}

				text = pvSrc.ReadUTF8StringSafe();

				keywords = keyList.ToArray();
			}
			else
			{
				text = pvSrc.ReadUnicodeStringSafe();

				keywords = m_EmptyInts;
			}

			if ( (int)type == 0x0F )
				text = Server.Commands.CommandSystem.Prefix + text.Trim();
			else
				text = text.Trim();

			if ( text.Length <= 0 || text.Length > 128 )
				return;

			type &= ~MessageType.Encoded;

			if ( !Enum.IsDefined( typeof( MessageType ), type ) )
				type = MessageType.Regular;

			from.Language = lang;
			from.DoSpeech( text, keywords, type, Utility.ClipDyedHue( hue ) );
		}


		public override void DoSpeech( string text, int[] keywords, MessageType type, int hue )
		{
			if ( Deleted || Commands.CommandSystem.Handle( this, text ) )
				return;

			int range = 15;

			switch ( type )
			{
				case MessageType.Regular: SpeechHue = hue; break;
				case MessageType.Emote: EmoteHue = hue; break;
				case MessageType.Whisper: WhisperHue = hue; range = 1; break;
				case MessageType.Yell: YellHue = hue; range = 18; break;
				default: type = MessageType.Regular; break;
			}

			SpeechEventArgs regArgs = new SpeechEventArgs( this, text, type, hue, keywords );

			EventSink.InvokeSpeech( regArgs );
			Region.OnSpeech( regArgs );
			OnSaid( regArgs );

			if ( regArgs.Blocked )
				return;

			text = regArgs.Speech;

			if ( text == null || text.Length == 0 )
				return;

			List<Mobile> hears = new List<Mobile>();
			ArrayList onSpeech = new ArrayList();

			bool needSpeechItem = Alive && Hidden && AccessLevel == AccessLevel.Player;

			if ( Map != null )
			{
				IPooledEnumerable eable = Map.GetObjectsInRange( Location, range );

				foreach ( object o in eable )
				{
					if ( o is Mobile )
					{
						Mobile heard = (Mobile)o;
						
						if ( ( needSpeechItem || heard.CanSee( this ) ) && (NoSpeechLOS || !heard.Player || heard.InLOS( this )) )
						{
							if ( heard.NetState != null )
								hears.Add( heard );

							if ( heard.HandlesOnSpeech( this ) )
								onSpeech.Add( heard );

							for ( int i = 0; i < heard.Items.Count; ++i )
							{
								Item item = (Item)heard.Items[i];

								if ( item.HandlesOnSpeech )
									onSpeech.Add( item );

								if ( item is Container )
									AddSpeechItemsFrom( onSpeech, (Container)item );
							}
						}
					}
					else if ( o is Item )
					{
						if ( ((Item)o).HandlesOnSpeech )
							onSpeech.Add( o );

						if ( o is Container )
							AddSpeechItemsFrom( onSpeech, (Container)o );
					}
				}

				//eable.Free();

				object mutateContext = null;
				string mutatedText = text;
				SpeechEventArgs mutatedArgs = null;

				if ( MutateSpeech( hears, ref mutatedText, ref mutateContext ) )
					mutatedArgs = new SpeechEventArgs( this, mutatedText, type, hue, new int[0] );

				CheckSpeechManifest();

				ProcessDelta();

				SpeechItem si = null;
				
				Packet regp = null, regpSI = null;
				Packet mutp = null, mutpSI = null;

				for ( int i = 0; i < hears.Count; ++i )
				{
					Mobile heard = (Mobile)hears[i];

					if ( mutatedArgs == null || !CheckHearsMutatedSpeech( heard, mutateContext ) )
					{
						heard.OnSpeech( regArgs );

						NetState ns = heard.NetState;

						if ( ns != null )
						{
							if ( needSpeechItem && !heard.CanSee( this ) && this != heard )
							{
								if ( regpSI == null )
								{
									if ( si == null )
										si = SpeechItem.Get( this );
									regpSI = new AsciiMessage( si.Serial, si.ItemID, type, hue, 3, "(hidden)", text );
									regpSI.SetStatic();
								}
								ns.Send( regpSI );
							}
							else
							{
								if ( regp == null )
								{
									regp = new AsciiMessage( Serial, Body, type, hue, 3, Name, text );		
									regp.SetStatic();
								}
								ns.Send( regp );
							}
						}
					}
					else
					{
						heard.OnSpeech( mutatedArgs );

						NetState ns = heard.NetState;

						if ( ns != null )
						{
							if ( needSpeechItem && !heard.CanSee( this ) && this != heard )
							{
								if ( mutpSI == null )
								{
									if ( si == null )
										si = SpeechItem.Get( this );
								
									mutpSI = new AsciiMessage( si.Serial, si.ItemID, type, hue, 3, "(hidden)", mutatedText );
									mutpSI.SetStatic();
								}
								ns.Send( mutpSI );
							}
							else
							{
								if ( mutp == null )
								{
									mutp = new AsciiMessage( Serial, Body, type, hue, 3, Name, mutatedText );
									mutp.SetStatic();
								}
								ns.Send( mutp );
							}
						}
					}
				}

				Packet.Release( ref regp );
				Packet.Release( ref regpSI );
				Packet.Release( ref mutp );
				Packet.Release( ref mutpSI );

				if ( onSpeech.Count > 1 )
					onSpeech.Sort( new LocationComparer( this ) );

				for ( int i = 0; i < onSpeech.Count; ++i )
				{
					object obj = onSpeech[i];

					if ( obj is Mobile )
					{
						Mobile heard = (Mobile)obj;

						if ( mutatedArgs == null || !CheckHearsMutatedSpeech( heard, mutateContext ) )
							heard.OnSpeech( regArgs );
						else
							heard.OnSpeech( mutatedArgs );
					}
					else
					{
						Item item = (Item)obj;

						item.OnSpeech( regArgs );
					}
				}
			}
		}

		public override bool CheckHigherPoison(Mobile from, Poison poison)
		{
			return this.Poison != null;
		}

		public override void OnBeneficialAction( Mobile target, bool isCriminal )
		{
			if ( target != this && !( target is BaseCreature && ((BaseCreature)target).Controled && ((BaseCreature)target).ControlMaster == this ) )
			{
				if ( Notoriety.Compute( this, target ) == Notoriety.Murderer )
					Titles.AlterNotoriety( this, -2, NotoCap.Dastardly );
			}
		}

		public override void OnHarmfulAction( Mobile target, bool isCriminal )
		{
			if ( target != this && !PlayerMobile.CheckAggressors( this, target ) && !PlayerMobile.CheckAggressors( target, this ) )
			{
				IPooledEnumerable eable = GetClientsInRange( 13 );
				Packet p = null;
				foreach ( NetState ns in eable )
				{
					Mobile m = ns.Mobile;
					if ( m != null && m.CanSee( this ) && m != target && m != this )
					{
						if ( p == null )
						{
							p = new AsciiMessage( Serial.MinusOne, -1, MessageType.Regular, 0x3b2, 3, "System", String.Format( "You see {0} attacking {1}!", this.Name, target.Name ) );
							p.SetStatic();
						}
						ns.Send( p );
					}
					//if ( Notoriety.Compute( this, target ) == Notoriety.Innocent )
					//	Titles.AlterNotoriety( this, -1, NotoCap.Dastardly );
				}
				Packet.Release( p );
				eable.Free();

				if ( SkillHandlers.Stealing.AttackOK( this, target ) )
					AggressiveAction( target, false );
			}

			base.OnHarmfulAction( target, isCriminal );
		}

		public static bool CheckAggressors( Mobile from, Mobile target )
		{
			for(int i=0;i<from.Aggressors.Count;i++)
			{
				AggressorInfo ai = (AggressorInfo)from.Aggressors[i];
				if ( ai.Attacker == target && !ai.Expired )
					return true;
			}

			for(int i=0;i<from.Aggressed.Count;i++)
			{
				AggressorInfo ai = (AggressorInfo)from.Aggressed[i];
				if ( ai.Defender == target && !ai.Expired )
					return true;
			}

			return false;
		}

		public override TimeSpan GetLogoutDelay()
		{
			if ( !Server.SkillHandlers.Hiding.CheckCombat( this, 7 ) )
			{
				BedRoll roll = null;
				Campfire fire = null;

				IPooledEnumerable eable = GetItemsInRange( 7 );
				foreach ( Item item in eable )
				{
					if ( item is BedRoll && ( roll == null || GetDistanceToSqrt( roll ) > GetDistanceToSqrt( item ) ) )
						roll = (BedRoll)item;
					else if ( item is Campfire && ( fire == null || GetDistanceToSqrt( fire ) > GetDistanceToSqrt( item ) ) )
						fire = (Campfire)item;
				}
				eable.Free();

				if ( roll != null && fire != null && roll.Unrolled && fire.CanLogout( this ) )
				{
					roll.Roll();
					AddToBackpack( roll );
					
					return TimeSpan.FromSeconds( 15.0 );
				}
			}
			
			return base.GetLogoutDelay();
		}

		public override double ArmorRating
		{
			get
			{
				BaseArmor ar;
				double rating = 0.0;

				ar = NeckArmor as BaseArmor;
				if ( ar != null )
					rating += ar.ArmorRatingScaled;

				ar = HandArmor as BaseArmor;
				if ( ar != null )
					rating += ar.ArmorRatingScaled;

				ar = HeadArmor as BaseArmor;
				if ( ar != null )
					rating += ar.ArmorRatingScaled;

				ar = ArmsArmor as BaseArmor;
				if ( ar != null )
					rating += ar.ArmorRatingScaled;

				ar = LegsArmor as BaseArmor;
				if ( ar != null )
					rating += ar.ArmorRatingScaled;

				ar = ChestArmor as BaseArmor;
				if ( ar != null )
					rating += ar.ArmorRatingScaled;

				ar = ShieldArmor as BaseArmor;
				if ( ar != null )
					rating += ar.ArmorRatingScaled;

				return VirtualArmor + VirtualArmorMod + rating;
			}
		}

		#region MyRunUO Invalidation
		private bool m_ChangedMyRunUO;

		public bool ChangedMyRunUO
		{
			get{ return m_ChangedMyRunUO; }
			set{ m_ChangedMyRunUO = value; }
		}

		public void InvalidateMyRunUO()
		{
			if ( !Deleted && !m_ChangedMyRunUO )
			{
				m_ChangedMyRunUO = true;
				Engines.MyRunUO.MyRunUO.QueueMobileUpdate( this );
			}
		}

		public override void OnKillsChange( int oldValue )
		{
			InvalidateMyRunUO();
		}

		public override void OnGenderChanged( bool oldFemale )
		{
			InvalidateMyRunUO();
		}

		public override void OnGuildChange( Server.Guilds.BaseGuild oldGuild )
		{
			InvalidateMyRunUO();
		}

		public override void OnGuildTitleChange( string oldTitle )
		{
			InvalidateMyRunUO();
		}

		private bool KillVirtueShields( ArrayList items )
		{
			bool killme = false;
			for(int i=0;i<items.Count;i++)
			{
				Item check = (Item)items[i];
				if ( check.Items.Count > 0 )
				{
					killme |= KillVirtueShields( new ArrayList( check.Items ) );
				}
				else if ( check is VirtueShield )
				{
					killme = true;
					check.Delete();
				}
			}

			return killme;
		}

		public override void OnKarmaChange( int oldValue )
		{
			if ( Titles.GetNotoLevel( oldValue ) != Titles.GetNotoLevel( Karma ) )
				Delta( MobileDelta.Noto );

			if ( Karma < (int)Noto.NobleLordLady && oldValue >= (int)Noto.NobleLordLady )
			{
				if ( KillVirtueShields( new ArrayList( this.Items ) ) )
				{
					Kill();
					SendAsciiMessage( "Thou hast strayed from the path of virtue!" );
					FixedParticles( 0x36BD, 20, 10, 5044, EffectLayer.Head );
					PlaySound( 0x307 );
				}
			}
			
			InvalidateMyRunUO();
		}

		public override void OnFameChange( int oldValue )
		{
			InvalidateMyRunUO();
		}

		public override void OnSkillChange( SkillName skill, double oldBase )
		{
			InvalidateMyRunUO();
		}

		public override void OnAccessLevelChanged( AccessLevel oldLevel )
		{
			InvalidateMyRunUO();
		}

		public override void OnRawStatChange( StatType stat, int oldValue )
		{
			InvalidateMyRunUO();
		}

		public override void OnDelete()
		{
			InvalidateMyRunUO();
		}
		#endregion

		#region Enemy of One
		private Type m_EnemyOfOneType;
		private bool m_WaitingForEnemy;

		public Type EnemyOfOneType
		{
			get{ return m_EnemyOfOneType; }
			set
			{
				Type oldType = m_EnemyOfOneType;
				Type newType = value;

				if ( oldType == newType )
					return;

				m_EnemyOfOneType = value;

				DeltaEnemies( oldType, newType );
			}
		}

		public bool WaitingForEnemy
		{
			get{ return m_WaitingForEnemy; }
			set{ m_WaitingForEnemy = value; }
		}

		private void DeltaEnemies( Type oldType, Type newType )
		{
			foreach ( Mobile m in this.GetMobilesInRange( 18 ) )
			{
				Type t = m.GetType();

				if ( t == oldType || t == newType )
					Send( new MobileMoving( m, Notoriety.Compute( this, m ) ) );
			}
		}
		#endregion

		#region Hair and beard mods
		private int m_HairModID = -1, m_HairModHue;
		private int m_BeardModID = -1, m_BeardModHue;

		public void SetHairMods( int hairID, int beardID )
		{
			if ( hairID == -1 )
				InternalRestoreHair( true, ref m_HairModID, ref m_HairModHue );
			else if ( hairID != -2 )
				InternalChangeHair( true, hairID, ref m_HairModID, ref m_HairModHue );

			if ( beardID == -1 )
				InternalRestoreHair( false, ref m_BeardModID, ref m_BeardModHue );
			else if ( beardID != -2 )
				InternalChangeHair( false, beardID, ref m_BeardModID, ref m_BeardModHue );
		}

		private Item CreateHair( bool hair, int id, int hue )
		{
			switch ( id )
			{
				case 0x203B: return new ShortHair( hue );
				case 0x203C: return new LongHair( hue );
				case 0x203D: return new PonyTail( hue );
				case 0x203E: return new LongBeard( hue );
				case 0x203F: return new ShortBeard( hue );
				case 0x2040: return new Goatee( hue );
				case 0x2041: return new Mustache( hue );
				case 0x2044: return new Mohawk( hue );
				case 0x2045: return new PageboyHair( hue );
				case 0x2046: return new BunsHair( hue );
				case 0x2047: return new Afro( hue );
				case 0x2048: return new ReceedingHair( hue );
				case 0x2049: return new TwoPigTails( hue );
				case 0x204A: return new KrisnaHair( hue );
				case 0x204B: return new MediumShortBeard( hue );
				case 0x204C: return new MediumLongBeard( hue );
				case 0x204D: return new Vandyke( hue );
				default:
				{
					Console.WriteLine( "Warning: Unknown hair ID specified: {0}", id );

					if ( hair )
						return new GenericHair( id );
					else
						return new GenericBeard( id );
				}
			}
		}

		private void InternalRestoreHair( bool hair, ref int id, ref int hue )
		{
			if ( id == -1 )
				return;

			Item item = FindItemOnLayer( hair ? Layer.Hair : Layer.FacialHair );

			if ( item != null )
				item.Delete();

			if ( id != 0 )
				AddItem( CreateHair( hair, id, hue ) );

			id = -1;
			hue = 0;
		}

		private void InternalChangeHair( bool hair, int id, ref int storeID, ref int storeHue )
		{
			Item item = FindItemOnLayer( hair ? Layer.Hair : Layer.FacialHair );

			if ( item != null )
			{
				if ( storeID == -1 )
				{
					storeID = item.ItemID;
					storeHue = item.Hue;
				}

				item.Delete();
			}
			else if ( storeID == -1 )
			{
				storeID = 0;
				storeHue = 0;
			}

			if ( id == 0 )
				return;

			AddItem( CreateHair( hair, id, 0 ) );
		}
		#endregion

		#region Virtue stuff
		private DateTime m_LastSacrificeGain;
		private DateTime m_LastSacrificeLoss;
		private int m_AvailableResurrects;

		public DateTime LastSacrificeGain{ get{ return m_LastSacrificeGain; } set{ m_LastSacrificeGain = value; } }
		public DateTime LastSacrificeLoss{ get{ return m_LastSacrificeLoss; } set{ m_LastSacrificeLoss = value; } }
		public int AvailableResurrects{ get{ return m_AvailableResurrects; } set{ m_AvailableResurrects = value; } }

		//private DateTime m_NextJustAward;
		private DateTime m_LastJusticeLoss;
		private ArrayList m_JusticeProtectors;

		public DateTime LastJusticeLoss{ get{ return m_LastJusticeLoss; } set{ m_LastJusticeLoss = value; } }
		public ArrayList JusticeProtectors{ get{ return m_JusticeProtectors; } set{ m_JusticeProtectors = value; } }

		private DateTime m_LastCompassionLoss;
		private DateTime m_NextCompassionDay;
		private int m_CompassionGains;

		public DateTime LastCompassionLoss{ get{ return m_LastCompassionLoss; } set{ m_LastCompassionLoss = value; } }
		public DateTime NextCompassionDay{ get{ return m_NextCompassionDay; } set{ m_NextCompassionDay = value; } }
		public int CompassionGains{ get{ return m_CompassionGains; } set{ m_CompassionGains = value; } }
		#endregion
	}

	public class SpeechItem : Item
	{
		private static Hashtable m_Table = new Hashtable();

		public static SpeechItem Get( Mobile m )
		{
			SpeechItem si = m_Table[m] as SpeechItem;
			if ( si == null )
				m_Table[m] = si = new SpeechItem( m );
			else
				si.ResetDecay();
			si.MoveToWorld( m.Location, m.Map );
			si.ProcessDelta();
			return si;
		}

		private DecayTimer m_DecayTimer;
		private Mobile m_Owner;

		private SpeechItem( Mobile m ) : base( 0x2198 ) // no draw tile
		{
			Movable = false;
			
			m_Owner = m;

			m_DecayTimer = new DecayTimer( this );
			m_DecayTimer.Start();
		}

		public SpeechItem( Serial s ) : base( s )
		{
			m_DecayTimer = new DecayTimer( this );
			m_DecayTimer.Start();
		}

		public void ResetDecay()
		{
			m_DecayTimer.Stop();
			m_DecayTimer.Start();
		}

		public override void Serialize(GenericWriter writer)
		{
			base.Serialize (writer);

			writer.Write( (int)0 ); // version
		}

		public override void Deserialize(GenericReader reader)
		{
			base.Deserialize (reader);

			int version = reader.ReadInt();
		}

		public override void OnSingleClick(Mobile from)
		{
		}

		private class DecayTimer : Timer
		{
			private SpeechItem m_SI;
			public DecayTimer( SpeechItem si ) : base( TimeSpan.FromSeconds( 30.0 )	)
			{
				m_SI = si;
			}

			protected override void OnTick()
			{
				try
				{
					if ( m_SI.m_Owner != null )
						m_Table.Remove( m_SI.m_Owner );
				}
				catch
				{
				}

				m_SI.Delete();
			}
		}
	}
}
