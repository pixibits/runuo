/***************************************************************************
 *                             PacketHandlers.cs
 *                            -------------------
 *   begin                : May 1, 2002
 *   copyright            : (C) The RunUO Software Team
 *   email                : info@runuo.com
 *
 *   $Id$
 *
 ***************************************************************************/

/***************************************************************************
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 ***************************************************************************/

using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Server.Accounting;
using Server.Gumps;
using Server.Targeting;
using Server.Items;
using Server.Menus;
using Server.Mobiles;
using Server.Movement;
using Server.Prompts;
using Server.HuePickers;
using Server.ContextMenus;
using Server.Diagnostics;
using CV = Server.ClientVersion;

namespace Server.Network
{
	public enum MessageType
	{
		Regular = 0x00,
		System = 0x01,
		Emote = 0x02,
		Label = 0x06,
		Focus = 0x07,
		Whisper = 0x08,
		Yell = 0x09,
		Spell = 0x0A,

		Guild = 0x0D,
		Alliance = 0x0E,
		Command = 0x0F,

		Encoded = 0xC0
	}

	public static class PacketHandlers
	{
		private static PacketHandler[] m_Handlers;
		private static PacketHandler[] m_6017Handlers;

		private static PacketHandler[] m_ExtendedHandlersLow;
		private static Dictionary<int, PacketHandler> m_ExtendedHandlersHigh;

		private static EncodedPacketHandler[] m_EncodedHandlersLow;
		private static Dictionary<int, EncodedPacketHandler> m_EncodedHandlersHigh;

		public static PacketHandler[] Handlers
		{
			get{ return m_Handlers; }
		}

		static PacketHandlers()
		{
			m_Handlers = new PacketHandler[0x100];
			m_6017Handlers = new PacketHandler[0x100];

			m_ExtendedHandlersLow = new PacketHandler[0x100];
			m_ExtendedHandlersHigh = new Dictionary<int, PacketHandler>();

			m_EncodedHandlersLow = new EncodedPacketHandler[0x100];
			m_EncodedHandlersHigh = new Dictionary<int, EncodedPacketHandler>();

			Register( 0x00, 104, false, new OnPacketReceive( CreateCharacter ) );
			Register( 0x01,   5, false, new OnPacketReceive( Disconnect ) );
			Register( 0x02,   3,  true, new OnPacketReceive( MovementReq_1_25_35 ) );
			Register( 0x03,   0,  true, new OnPacketReceive( AsciiSpeech ) );
			Register( 0x04,   2,  true, new OnPacketReceive( GodModeRequest ) );
			Register( 0x05,   5,  true, new OnPacketReceive( AttackReq ) );
			Register( 0x06,   5,  true, new OnPacketReceive( UseReq ) );
			Register( 0x07,   7,  true, new OnPacketReceive( LiftReq ) );
			Register( 0x08,  14,  true, new OnPacketReceive( DropReq ) );
			Register( 0x09,   5,  true, new OnPacketReceive( LookReq ) );
			Register( 0x0A,  11,  true, new OnPacketReceive( Edit ) );
			Register( 0x12,   0,  true, new OnPacketReceive( TextCommand ) );
			Register( 0x13,  10,  true, new OnPacketReceive( EquipReq ) );
			Register( 0x14,   6,  true, new OnPacketReceive( ChangeZ ) );
			Register( 0x22,   3,  true, new OnPacketReceive( Resynchronize ) );
			Register( 0x2C,   2,  true, new OnPacketReceive( DeathStatusResponse ) );
			Register( 0x34,  10,  true, new OnPacketReceive( MobileQuery ) );
			Register( 0x3A,   0,  true, new OnPacketReceive( ChangeSkillLock ) );
			Register( 0x3B,   0,  true, new OnPacketReceive( VendorBuyReply ) );
			Register( 0x47,  11,  true, new OnPacketReceive( NewTerrain ) );
			Register( 0x48,  73,  true, new OnPacketReceive( NewAnimData ) );
			Register( 0x58, 106,  true, new OnPacketReceive( NewRegion ) );
            Register( 0x5D,  73, false, new OnPacketReceive( PlayCharacter ) );
            Register( 0x5D,  73, false, new OnPacketReceive( PlayCharacter_1_25_35 ));
            Register( 0x61,   9,  true, new OnPacketReceive( DeleteStatic ) );
			Register( 0x6C,  19,  true, new OnPacketReceive( TargetResponse ) );
			Register( 0x6F,   0,  true, new OnPacketReceive( SecureTrade ) );
			Register( 0x72,   5,  true, new OnPacketReceive( SetWarMode ) );
			Register( 0x73,   2, false, new OnPacketReceive( PingReq ) );
			Register( 0x75,  35,  true, new OnPacketReceive( RenameRequest ) );
			Register( 0x79,   9,  true, new OnPacketReceive( ResourceQuery ) );
			Register( 0x7E,   2,  true, new OnPacketReceive( GodviewQuery ) );
			Register( 0x7D,  13,  true, new OnPacketReceive( MenuResponse ) );
			Register( 0x80,  62, false, new OnPacketReceive( AccountLogin ) );
			Register( 0x83,  39, false, new OnPacketReceive( DeleteCharacter ) );
			Register( 0x91,  65, false, new OnPacketReceive( GameLogin ) );
			Register( 0x95,   9,  true, new OnPacketReceive( HuePickerResponse ) );
			Register( 0x96,   0,  true, new OnPacketReceive( GameCentralMoniter ) );
			Register( 0x98,   0,  true, new OnPacketReceive( MobileNameRequest ) );
			Register( 0x9A,   0,  true, new OnPacketReceive( AsciiPromptResponse ) );
			Register( 0x9B, 258,  true, new OnPacketReceive( HelpRequest ) );
			Register( 0x9D,  51,  true, new OnPacketReceive( GMSingle ) );
			Register( 0x9F,   0,  true, new OnPacketReceive( VendorSellReply ) );
			Register( 0xA0,   3, false, new OnPacketReceive( PlayServer ) );
			Register( 0xA4, 149, false, new OnPacketReceive( SystemInfo ) );
			//Register( 0xA7,   4,  true, new OnPacketReceive( RequestScrollWindow ) );
			Register( 0xAD,   0,  true, new OnPacketReceive( UnicodeSpeech ) );
			Register( 0xB1,   0,  true, new OnPacketReceive( DisplayGumpResponse ) );
			Register( 0xB5,  64,  true, new OnPacketReceive( ChatRequest ) );
			Register( 0xB6,   9,  true, new OnPacketReceive( ObjectHelpRequest ) );
			Register( 0xB8,   0,  true, new OnPacketReceive( ProfileReq ) );
			Register( 0xBB,   9, false, new OnPacketReceive( AccountID ) );
			Register( 0xBD,   0, false, new OnPacketReceive( ClientVersion ) );
			Register( 0xBE,   0,  true, new OnPacketReceive( AssistVersion ) );
			Register( 0xBF,   0,  true, new OnPacketReceive( ExtendedCommand ) );
			Register( 0xC2,   0,  true, new OnPacketReceive( UnicodePromptResponse ) );
			Register( 0xC8,   2,  true, new OnPacketReceive( SetUpdateRange ) );
			Register( 0xC9,   6,  true, new OnPacketReceive( TripTime ) );
			Register( 0xCA,   6,  true, new OnPacketReceive( UTripTime ) );
			Register( 0xCF,   0, false, new OnPacketReceive( AccountLogin ) );
			Register( 0xD0,   0,  true, new OnPacketReceive( ConfigurationFile ) );
			Register( 0xD1,   2,  true, new OnPacketReceive( LogoutReq ) );
			Register( 0xD6,   0,  true, new OnPacketReceive( BatchQueryProperties ) );
			Register( 0xD7,   0,  true, new OnPacketReceive( EncodedCommand ) );
			Register( 0xE1,   0, false, new OnPacketReceive( ClientType ) );
			Register( 0xEF,  21, false, new OnPacketReceive( LoginServerSeed ) );

			Register6017( 0x08, 15, true, new OnPacketReceive( DropReq6017 ) );

			RegisterExtended( 0x05, false, new OnPacketReceive( ScreenSize ) );
			RegisterExtended( 0x06,  true, new OnPacketReceive( PartyMessage ) );
			RegisterExtended( 0x07,  true, new OnPacketReceive( QuestArrow ) );
			RegisterExtended( 0x09,  true, new OnPacketReceive( DisarmRequest ) );
			RegisterExtended( 0x0A,  true, new OnPacketReceive( StunRequest ) );
			RegisterExtended( 0x0B, false, new OnPacketReceive( Language ) );
			RegisterExtended( 0x0C,  true, new OnPacketReceive( CloseStatus ) );
			RegisterExtended( 0x0E,  true, new OnPacketReceive( Animate ) );
			RegisterExtended( 0x0F, false, new OnPacketReceive( Empty ) ); // What's this?
			RegisterExtended( 0x10,  true, new OnPacketReceive( QueryProperties ) );
			RegisterExtended( 0x13,  true, new OnPacketReceive( ContextMenuRequest ) );
			RegisterExtended( 0x15,  true, new OnPacketReceive( ContextMenuResponse ) );
			RegisterExtended( 0x1A,  true, new OnPacketReceive( StatLockChange ) );
			RegisterExtended( 0x1C,  true, new OnPacketReceive( CastSpell ) );
			RegisterExtended( 0x24, false, new OnPacketReceive( UnhandledBF ) );

			RegisterEncoded( 0x19, true, new OnEncodedPacketReceive( SetAbility ) );
			RegisterEncoded( 0x28, true, new OnEncodedPacketReceive( GuildGumpRequest ) );

			RegisterEncoded( 0x32, true, new OnEncodedPacketReceive( QuestGumpRequest ) );
		}

		public static void Register( int packetID, int length, bool ingame, OnPacketReceive onReceive )
		{
			m_Handlers[packetID] = new PacketHandler( packetID, length, ingame, onReceive );

			if ( m_6017Handlers[packetID] == null )
				m_6017Handlers[packetID] = new PacketHandler( packetID, length, ingame, onReceive );
		}

		public static PacketHandler GetHandler( int packetID )
		{
			return m_Handlers[packetID];
		}

		public static void Register6017( int packetID, int length, bool ingame, OnPacketReceive onReceive )
		{
			m_6017Handlers[packetID] = new PacketHandler( packetID, length, ingame, onReceive );
		}

		public static PacketHandler Get6017Handler( int packetID )
		{
			return m_6017Handlers[packetID];
		}

		public static void RegisterExtended( int packetID, bool ingame, OnPacketReceive onReceive )
		{
			if ( packetID >= 0 && packetID < 0x100 )
				m_ExtendedHandlersLow[packetID] = new PacketHandler( packetID, 0, ingame, onReceive );
			else
				m_ExtendedHandlersHigh[packetID] = new PacketHandler( packetID, 0, ingame, onReceive );
		}

		public static PacketHandler GetExtendedHandler( int packetID )
		{
			if ( packetID >= 0 && packetID < 0x100 )
				return m_ExtendedHandlersLow[packetID];
			else
			{
				PacketHandler handler;
				m_ExtendedHandlersHigh.TryGetValue( packetID, out handler );
				return handler;
			}
		}

		public static void RemoveExtendedHandler( int packetID )
		{
			if ( packetID >= 0 && packetID < 0x100 )
				m_ExtendedHandlersLow[packetID] = null;
			else
				m_ExtendedHandlersHigh.Remove( packetID );
		}

		public static void RegisterEncoded( int packetID, bool ingame, OnEncodedPacketReceive onReceive )
		{
			if ( packetID >= 0 && packetID < 0x100 )
				m_EncodedHandlersLow[packetID] = new EncodedPacketHandler( packetID, ingame, onReceive );
			else
				m_EncodedHandlersHigh[packetID] = new EncodedPacketHandler( packetID, ingame, onReceive );
		}

		public static EncodedPacketHandler GetEncodedHandler( int packetID )
		{
			if ( packetID >= 0 && packetID < 0x100 )
				return m_EncodedHandlersLow[packetID];
			else
			{
				EncodedPacketHandler handler;
				m_EncodedHandlersHigh.TryGetValue( packetID, out handler );
				return handler;
			}
		}

		public static void RemoveEncodedHandler( int packetID )
		{
			if ( packetID >= 0 && packetID < 0x100 )
				m_EncodedHandlersLow[packetID] = null;
			else
				m_EncodedHandlersHigh.Remove( packetID );
		}

		public static void RegisterThrottler( int packetID, ThrottlePacketCallback t )
		{
			PacketHandler ph = GetHandler( packetID );

			if ( ph != null )
				ph.ThrottleCallback = t;

			ph = Get6017Handler( packetID );

			if ( ph != null )
				ph.ThrottleCallback = t;
		}

		private static void UnhandledBF( NetState state, PacketReader pvSrc )
		{
		}

		public static void Empty( NetState state, PacketReader pvSrc )
		{
		}

		public static void SetAbility( NetState state, IEntity e, EncodedReader reader )
		{
			EventSink.InvokeSetAbility( new SetAbilityEventArgs( state.Mobile, reader.ReadInt32() ) );
		}

		public static void GuildGumpRequest( NetState state, IEntity e, EncodedReader reader )
		{
			EventSink.InvokeGuildGumpRequest( new GuildGumpRequestArgs( state.Mobile ) );
		}

		public static void QuestGumpRequest( NetState state, IEntity e, EncodedReader reader )
		{
			EventSink.InvokeQuestGumpRequest( new QuestGumpRequestArgs( state.Mobile ) );
		}

		public static void EncodedCommand( NetState state, PacketReader pvSrc )
		{
			IEntity e = World.FindEntity( pvSrc.ReadInt32() );
			int packetID = pvSrc.ReadUInt16();

			EncodedPacketHandler ph = GetEncodedHandler( packetID );

			if ( ph != null )
			{
				if ( ph.Ingame && state.Mobile == null )
				{
					Console.WriteLine( "Client: {0}: Sent ingame packet (0xD7x{1:X2}) before having been attached to a mobile", state, packetID );
					state.Dispose();
				}
				else if ( ph.Ingame && state.Mobile.Deleted )
				{
					state.Dispose();
				}
				else
				{
					ph.OnReceive( state, e, new EncodedReader( pvSrc ) );
				}
			}
			else
			{
				pvSrc.Trace( state );
			}
		}

		public static void RenameRequest( NetState state, PacketReader pvSrc )
		{
			Mobile from = state.Mobile;
			Mobile targ = World.FindMobile( pvSrc.ReadInt32() );

			if ( targ != null )
				EventSink.InvokeRenameRequest( new RenameRequestEventArgs( from, targ, pvSrc.ReadStringSafe() ) );
		}

		public static void ChatRequest( NetState state, PacketReader pvSrc )
		{
			EventSink.InvokeChatRequest( new ChatRequestEventArgs( state.Mobile ) );
		}

		public static void SecureTrade( NetState state, PacketReader pvSrc )
		{
			switch ( pvSrc.ReadByte() )
			{
				case 1: // Cancel
				{
					Serial serial = pvSrc.ReadInt32();

					SecureTradeContainer cont = World.FindItem( serial ) as SecureTradeContainer;

					if ( cont != null && cont.Trade != null && (cont.Trade.From.Mobile == state.Mobile || cont.Trade.To.Mobile == state.Mobile) )
						cont.Trade.Cancel();

					break;
				}
				case 2: // Check
				{
					Serial serial = pvSrc.ReadInt32();

					SecureTradeContainer cont = World.FindItem( serial ) as SecureTradeContainer;

					if ( cont != null )
					{
						SecureTrade trade = cont.Trade;

						bool value = ( pvSrc.ReadInt32() != 0 );

						if ( trade != null && trade.From.Mobile == state.Mobile )
						{
							trade.From.Accepted = value;
							trade.Update();
						}
						else if ( trade != null && trade.To.Mobile == state.Mobile )
						{
							trade.To.Accepted = value;
							trade.Update();
						}
					}

					break;
				}
			}
		}

		public static void VendorBuyReply( NetState state, PacketReader pvSrc )
		{
			pvSrc.Seek( 1, SeekOrigin.Begin );

			int msgSize = pvSrc.ReadUInt16();
			Mobile vendor = World.FindMobile( pvSrc.ReadInt32() );
			byte flag = pvSrc.ReadByte();

			if ( vendor == null )
			{
				return;
			}
			else if ( vendor.Deleted || !Utility.RangeCheck( vendor.Location, state.Mobile.Location, 10 ) )
			{
				state.Send( new EndVendorBuy( vendor ) );
				return;
			}

			if ( flag == 0x02 )
			{
				msgSize -= 1+2+4+1;

				if ( (msgSize / 7) > 100 )
					return;

				List<BuyItemResponse> buyList = new List<BuyItemResponse>( msgSize / 7 );
				for ( ;msgSize>0;msgSize-=7)
				{
					byte layer = pvSrc.ReadByte();
					Serial serial = pvSrc.ReadInt32();
					int amount = pvSrc.ReadInt16();
				
					buyList.Add( new BuyItemResponse( serial, amount ) );
				}

				if ( buyList.Count > 0 )
				{
					IVendor v = vendor as IVendor;

					if ( v != null && v.OnBuyItems( state.Mobile, buyList ) )
						state.Send( new EndVendorBuy( vendor ) );
				}
			}
			else
			{
				state.Send( new EndVendorBuy( vendor ) );
			}
		}

		public static void VendorSellReply( NetState state, PacketReader pvSrc )
		{
			Serial serial = pvSrc.ReadInt32();
			Mobile vendor = World.FindMobile( serial );

			if ( vendor == null )
			{
				return;
			}
			else if ( vendor.Deleted || !Utility.RangeCheck( vendor.Location, state.Mobile.Location, 10 ) )
			{
				state.Send( new EndVendorSell( vendor ) );
				return;
			}

			int count = pvSrc.ReadUInt16();
			if ( count < 100 && pvSrc.Size == (1+2+4+2+(count*6)) )
			{
				List<SellItemResponse> sellList = new List<SellItemResponse>( count );

				for (int i=0;i<count;i++)
				{
					Item item = World.FindItem( pvSrc.ReadInt32() );
					int Amount = pvSrc.ReadInt16();

					if ( item != null && Amount > 0 )
						sellList.Add(  new SellItemResponse( item, Amount ) );
				}

				if ( sellList.Count > 0 )
				{
					IVendor v = vendor as IVendor;

					if ( v != null && v.OnSellItems( state.Mobile, sellList ) )
						state.Send( new EndVendorSell( vendor ) );
				}
			}
		}

		public static void DeleteCharacter( NetState state, PacketReader pvSrc )
		{
			pvSrc.Seek( 30, SeekOrigin.Current );
			int index = pvSrc.ReadInt32();

			EventSink.InvokeDeleteRequest( new DeleteRequestEventArgs( state, index ) );
		}

		public static void ResourceQuery( NetState state, PacketReader pvSrc )
		{
			if ( VerifyGC( state ) )
			{
			}
		}

		public static void GameCentralMoniter( NetState state, PacketReader pvSrc )
		{
			if ( VerifyGC( state ) )
			{
				int type = pvSrc.ReadByte();
				int num1 = pvSrc.ReadInt32();

				Console.WriteLine( "God Client: {0}: Game central moniter", state );
				Console.WriteLine( " - Type: {0}", type );
				Console.WriteLine( " - Number: {0}", num1 );

				pvSrc.Trace( state );
			}
		}

		public static void GodviewQuery( NetState state, PacketReader pvSrc )
		{
			if ( VerifyGC( state ) )
			{
				Console.WriteLine( "God Client: {0}: Godview query 0x{1:X}", state, pvSrc.ReadByte() );
			}
		}

		public static void GMSingle( NetState state, PacketReader pvSrc )
		{
			if ( VerifyGC( state ) )
				pvSrc.Trace( state );
		}

		public static void DeathStatusResponse( NetState state, PacketReader pvSrc )
		{
			// Ignored
		}

		public static void ObjectHelpRequest( NetState state, PacketReader pvSrc )
		{
			Mobile from = state.Mobile;

			Serial serial = pvSrc.ReadInt32();
			int unk = pvSrc.ReadByte();
			string lang = pvSrc.ReadString( 3 );

			if ( serial.IsItem )
			{
				Item item = World.FindItem( serial );

				if ( item != null && from.Map == item.Map && Utility.InUpdateRange( item.GetWorldLocation(), from.Location ) && from.CanSee( item ) )
					item.OnHelpRequest( from );
			}
			else if ( serial.IsMobile )
			{
				Mobile m = World.FindMobile( serial );

				if ( m != null && from.Map == m.Map && Utility.InUpdateRange( m.Location, from.Location ) && from.CanSee( m ) )
					m.OnHelpRequest( m );
			}
		}

		public static void MobileNameRequest( NetState state, PacketReader pvSrc )
		{
			Mobile m = World.FindMobile( pvSrc.ReadInt32() );

			if ( m != null && Utility.InUpdateRange( state.Mobile, m ) && state.Mobile.CanSee( m ) )
				state.Send( new MobileName( m ) );
		}

		public static void RequestScrollWindow( NetState state, PacketReader pvSrc )
		{
			int lastTip = pvSrc.ReadInt16();
			int type = pvSrc.ReadByte();
		}

		public static void AttackReq( NetState state, PacketReader pvSrc )
		{
			Mobile from = state.Mobile;
			Mobile m = World.FindMobile( pvSrc.ReadInt32() );

			if ( m != null )
				from.Attack( m );
		}

		public static void HuePickerResponse( NetState state, PacketReader pvSrc ) {
			int serial = pvSrc.ReadInt32();
			int value = pvSrc.ReadInt16();
			int hue = pvSrc.ReadInt16() & 0x3FFF;

			hue = Utility.ClipDyedHue( hue );

			foreach ( HuePicker huePicker in state.HuePickers ) {
				if ( huePicker.Serial == serial ) {
					state.RemoveHuePicker( huePicker );

					huePicker.OnResponse( hue );

					break;
				}
			}
		}

		public static void TripTime( NetState state, PacketReader pvSrc )
		{
			int unk1 = pvSrc.ReadByte();
			int unk2 = pvSrc.ReadInt32();

			state.Send( new TripTimeResponse( unk1 ) );
		}

		public static void UTripTime( NetState state, PacketReader pvSrc )
		{
			int unk1 = pvSrc.ReadByte();
			int unk2 = pvSrc.ReadInt32();

			state.Send( new UTripTimeResponse( unk1 ) );
		}

		public static void ChangeZ( NetState state, PacketReader pvSrc )
		{
			if ( VerifyGC( state ) )
			{
				int x = pvSrc.ReadInt16();
				int y = pvSrc.ReadInt16();
				int z = pvSrc.ReadSByte();

				Console.WriteLine( "God Client: {0}: Change Z ({1}, {2}, {3})", state, x, y, z );
			}
		}

		public static void SystemInfo( NetState state, PacketReader pvSrc )
		{
			int v1 = pvSrc.ReadByte();
			int v2 = pvSrc.ReadUInt16();
			int v3 = pvSrc.ReadByte();
			string s1 = pvSrc.ReadString( 32 );
			string s2 = pvSrc.ReadString( 32 );
			string s3 = pvSrc.ReadString( 32 );
			string s4 = pvSrc.ReadString( 32 );
			int v4 = pvSrc.ReadUInt16();
			int v5 = pvSrc.ReadUInt16();
			int v6 = pvSrc.ReadInt32();
			int v7 = pvSrc.ReadInt32();
			int v8 = pvSrc.ReadInt32();
		}

		public static void Edit( NetState state, PacketReader pvSrc )
		{
			if ( VerifyGC( state ) )
			{
				int type = pvSrc.ReadByte(); // 10 = static, 7 = npc, 4 = dynamic
				int x = pvSrc.ReadInt16();
				int y = pvSrc.ReadInt16();
				int id = pvSrc.ReadInt16();
				int z = pvSrc.ReadSByte();
				int hue = pvSrc.ReadUInt16();

				Console.WriteLine( "God Client: {0}: Edit {6} ({1}, {2}, {3}) 0x{4:X} (0x{5:X})", state, x, y, z, id, hue, type );
			}
		}

		public static void DeleteStatic( NetState state, PacketReader pvSrc )
		{
			if ( VerifyGC( state ) )
			{
				int x = pvSrc.ReadInt16();
				int y = pvSrc.ReadInt16();
				int z = pvSrc.ReadInt16();
				int id = pvSrc.ReadUInt16();

				Console.WriteLine( "God Client: {0}: Delete Static ({1}, {2}, {3}) 0x{4:X}", state, x, y, z, id );
			}
		}

		public static void NewAnimData( NetState state, PacketReader pvSrc )
		{
			if ( VerifyGC( state ) )
			{
				Console.WriteLine( "God Client: {0}: New tile animation", state );

				pvSrc.Trace( state );
			}
		}

		public static void NewTerrain( NetState state, PacketReader pvSrc )
		{
			if ( VerifyGC( state ) )
			{
				int x = pvSrc.ReadInt16();
				int y = pvSrc.ReadInt16();
				int id = pvSrc.ReadUInt16();
				int width = pvSrc.ReadInt16();
				int height = pvSrc.ReadInt16();

				Console.WriteLine( "God Client: {0}: New Terrain ({1}, {2})+({3}, {4}) 0x{5:X4}", state, x, y, width, height, id );
			}
		}

		public static void NewRegion( NetState state, PacketReader pvSrc )
		{
			if ( VerifyGC( state ) )
			{
				string name = pvSrc.ReadString( 40 );
				int unk = pvSrc.ReadInt32();
				int x = pvSrc.ReadInt16();
				int y = pvSrc.ReadInt16();
				int width = pvSrc.ReadInt16();
				int height = pvSrc.ReadInt16();
				int zStart = pvSrc.ReadInt16();
				int zEnd = pvSrc.ReadInt16();
				string desc = pvSrc.ReadString( 40 );
				int soundFX = pvSrc.ReadInt16();
				int music = pvSrc.ReadInt16();
				int nightFX = pvSrc.ReadInt16();
				int dungeon = pvSrc.ReadByte();
				int light = pvSrc.ReadInt16();

				Console.WriteLine( "God Client: {0}: New Region '{1}' ('{2}')", state, name, desc );
			}
		}

		public static void AccountID( NetState state, PacketReader pvSrc )
		{
		}

		public static bool VerifyGC( NetState state )
		{
			if ( state.Mobile == null || state.Mobile.AccessLevel <= AccessLevel.Counselor )
			{
				if ( state.Running )
					Console.WriteLine( "Warning: {0}: Player using godclient, disconnecting", state );

				state.Dispose();
				return false;
			}
			else
			{
				return true;
			}
		}

		public static void TextCommand( NetState state, PacketReader pvSrc )
		{
			int type = pvSrc.ReadByte();
			string command = pvSrc.ReadString();

			Mobile m = state.Mobile;

			switch ( type )
			{
				case 0x00: // Go
				{
					if ( VerifyGC( state ) )
					{
						try
						{
							string[] split = command.Split( ' ' );

							int x = Utility.ToInt32( split[0] );
							int y = Utility.ToInt32( split[1] );

							int z;

							if ( split.Length >= 3 )
								z = Utility.ToInt32( split[2] );
							else if ( m.Map != null )
								z = m.Map.GetAverageZ( x, y );
							else
								z = 0;

							m.Location = new Point3D( x, y, z );
						}
						catch
						{
						}
					}

					break;
				}
				case 0xC7: // Animate
				{
					EventSink.InvokeAnimateRequest( new AnimateRequestEventArgs( m, command ) );

					break;
				}
				case 0x24: // Use skill
				{
					int skillIndex;

					if ( !int.TryParse( command.Split( ' ' )[0], out skillIndex ) )
						break;

					Skills.UseSkill( m, skillIndex );

					break;
				}
				case 0x43: // Open spellbook
				{
					int booktype;

					if ( !int.TryParse( command, out booktype ) )
						booktype = 1;

					EventSink.InvokeOpenSpellbookRequest( new OpenSpellbookRequestEventArgs( m, booktype ) );

					break;
				}
				case 0x27: // Cast spell from book
				{
					string[] split = command.Split( ' ' );

					if ( split.Length > 0 )
					{
						int spellID = Utility.ToInt32( split[0] ) - 1;
						int serial = split.Length > 1 ? Utility.ToInt32( split[1] ) : -1;

						EventSink.InvokeCastSpellRequest( new CastSpellRequestEventArgs( m, spellID, World.FindItem( serial ) ) );
					}

					break;
				}
				case 0x58: // Open door
				{
					EventSink.InvokeOpenDoorMacroUsed( new OpenDoorMacroEventArgs( m ) );

					break;
				}
				case 0x56: // Cast spell from macro
				{
					int spellID = Utility.ToInt32( command ) - 1;

					EventSink.InvokeCastSpellRequest( new CastSpellRequestEventArgs( m, spellID, null ) );

					break;
				}
				case 0xF4: // Invoke virtues from macro
				{
					int virtueID = Utility.ToInt32( command ) - 1;

					EventSink.InvokeVirtueMacroRequest( new VirtueMacroRequestEventArgs( m, virtueID ) );

					break;
				}
				default:
				{
					Console.WriteLine( "Client: {0}: Unknown text-command type 0x{1:X2}: {2}", state, type, command );
					break;
				}
			}
		}

		public static void GodModeRequest( NetState state, PacketReader pvSrc )
		{
			if ( VerifyGC( state ) )
			{
				state.Send( new GodModeReply( pvSrc.ReadBoolean() ) );
			}
		}

		public static void AsciiPromptResponse( NetState state, PacketReader pvSrc )
		{
			int serial = pvSrc.ReadInt32();
			int prompt = pvSrc.ReadInt32();
			int type = pvSrc.ReadInt32();
			string text = pvSrc.ReadStringSafe();

			if ( text.Length > 128 )
				return;

			Mobile from = state.Mobile;
			Prompt p = from.Prompt;

			if ( p != null && p.Serial == serial && p.Serial == prompt )
			{
				from.Prompt = null;

				if ( type == 0 )
					p.OnCancel( from );
				else
					p.OnResponse( from, text );
			}
		}

		public static void UnicodePromptResponse( NetState state, PacketReader pvSrc )
		{
			int serial = pvSrc.ReadInt32();
			int prompt = pvSrc.ReadInt32();
			int type = pvSrc.ReadInt32();
			string lang = pvSrc.ReadString( 4 );
			string text = pvSrc.ReadUnicodeStringLESafe();

			if ( text.Length > 128 )
				return;

			Mobile from = state.Mobile;
			Prompt p = from.Prompt;

			if ( p != null && p.Serial == serial && p.Serial == prompt )
			{
				from.Prompt = null;

				if ( type == 0 )
					p.OnCancel( from );
				else
					p.OnResponse( from, text );
			}
		}

		public static void MenuResponse( NetState state, PacketReader pvSrc ) {
			int serial = pvSrc.ReadInt32();
			int menuID = pvSrc.ReadInt16(); // unused in our implementation
			int index = pvSrc.ReadInt16();
			int itemID = pvSrc.ReadInt16();
			int hue = pvSrc.ReadInt16();

			index -= 1; // convert from 1-based to 0-based

			foreach ( IMenu menu in state.Menus ) {
				if ( menu.Serial == serial ) {
					state.RemoveMenu( menu );

					if ( index >= 0 && index < menu.EntryLength ) {
						menu.OnResponse( state, index );
					} else {
						menu.OnCancel( state );
					}

					break;
				}
			}
		}

		public static void ProfileReq( NetState state, PacketReader pvSrc )
		{
			int type = pvSrc.ReadByte();
			Serial serial = pvSrc.ReadInt32();

			Mobile beholder = state.Mobile;
			Mobile beheld = World.FindMobile( serial );

			if ( beheld == null )
				return;

			switch ( type )
			{
				case 0x00: // display request
				{
					EventSink.InvokeProfileRequest( new ProfileRequestEventArgs( beholder, beheld ) );

					break;
				}
				case 0x01: // edit request
				{
					pvSrc.ReadInt16(); // Skip
					int length = pvSrc.ReadUInt16();

					if ( length > 511 )
						return;

					string text = pvSrc.ReadUnicodeString( length );

					EventSink.InvokeChangeProfileRequest( new ChangeProfileRequestEventArgs( beholder, beheld, text ) );

					break;
				}
			}
		}

		public static void Disconnect( NetState state, PacketReader pvSrc )
		{
			int minusOne = pvSrc.ReadInt32();
		}

		public static void LiftReq( NetState state, PacketReader pvSrc )
		{
			Serial serial = pvSrc.ReadInt32();
			int amount = pvSrc.ReadUInt16();
			Item item = World.FindItem( serial );

			bool rejected;
			LRReason reject;

			state.Mobile.Lift( item, amount, out rejected, out reject );
		}

		public static void EquipReq( NetState state, PacketReader pvSrc )
		{
			Mobile from = state.Mobile;
			Item item = from.Holding;

			bool valid = ( item != null && item.HeldBy == from && item.Map == Map.Internal );

			from.Holding = null;

			if ( !valid ) {
				return;
			}

			pvSrc.Seek( 5, SeekOrigin.Current );
			Mobile to = World.FindMobile( pvSrc.ReadInt32() );

			if ( to == null )
				to = from;

			if ( !to.AllowEquipFrom( from ) || !to.EquipItem( item ) )
				item.Bounce( from );

			item.ClearBounce();
		}

		public static void DropReq( NetState state, PacketReader pvSrc )
		{
			pvSrc.ReadInt32(); // serial, ignored
			int x = pvSrc.ReadInt16();
			int y = pvSrc.ReadInt16();
			int z = pvSrc.ReadSByte();
			Serial dest = pvSrc.ReadInt32();

			Point3D loc = new Point3D( x, y, z );

			Mobile from = state.Mobile;

			if ( dest.IsMobile )
				from.Drop( World.FindMobile( dest ), loc );
			else if ( dest.IsItem )
				from.Drop( World.FindItem( dest ), loc );
			else
				from.Drop( loc );
		}

		public static void DropReq6017( NetState state, PacketReader pvSrc )
		{
			pvSrc.ReadInt32(); // serial, ignored
			int x = pvSrc.ReadInt16();
			int y = pvSrc.ReadInt16();
			int z = pvSrc.ReadSByte();
			pvSrc.ReadByte(); // Grid Location?
			Serial dest = pvSrc.ReadInt32();

			Point3D loc = new Point3D( x, y, z );

			Mobile from = state.Mobile;

			if ( dest.IsMobile )
				from.Drop( World.FindMobile( dest ), loc );
			else if ( dest.IsItem )
				from.Drop( World.FindItem( dest ), loc );
			else
				from.Drop( loc );
		}

		public static void ConfigurationFile( NetState state, PacketReader pvSrc )
		{
		}

		public static void LogoutReq( NetState state, PacketReader pvSrc )
		{
			state.Send( new LogoutAck() );
		}

		public static void ChangeSkillLock( NetState state, PacketReader pvSrc )
		{
			Skill s = state.Mobile.Skills[pvSrc.ReadInt16()];

			if ( s != null )
				s.SetLockNoRelay( (SkillLock)pvSrc.ReadByte() );
		}

		public static void HelpRequest( NetState state, PacketReader pvSrc )
		{
			EventSink.InvokeHelpRequest( new HelpRequestEventArgs( state.Mobile ) );
		}

		public static void TargetResponse( NetState state, PacketReader pvSrc )
		{
			int type = pvSrc.ReadByte();
			int targetID = pvSrc.ReadInt32();
			int flags = pvSrc.ReadByte();
			Serial serial = pvSrc.ReadInt32();
			int x = pvSrc.ReadInt16(), y = pvSrc.ReadInt16(), z = pvSrc.ReadInt16();
			int graphic = pvSrc.ReadUInt16();

			if ( targetID == unchecked( (int) 0xDEADBEEF ) )
				return;

			Mobile from = state.Mobile;

			Target t = from.Target;

			if ( t != null )
			{
				TargetProfile prof = TargetProfile.Acquire( t.GetType() );

				if ( prof != null ) {
					prof.Start();
				}

				try {
					if ( x == -1 && y == -1 && !serial.IsValid )
					{
						// User pressed escape
						t.Cancel( from, TargetCancelType.Canceled );
					}
					else
					{
						object toTarget;

						if ( type == 1 )
						{
							if ( graphic == 0 )
							{
								toTarget = new LandTarget( new Point3D( x, y, z ), from.Map );
							}
							else
							{
								Map map = from.Map;

								if ( map == null || map == Map.Internal )
								{
									t.Cancel( from, TargetCancelType.Canceled );
									return;
								}
								else
								{
									StaticTile[] tiles = map.Tiles.GetStaticTiles( x, y, !t.DisallowMultis );

									bool valid = false;

									if ( state.HighSeas ) {
										ItemData id = TileData.ItemTable[graphic&TileData.MaxItemValue];
										if ( id.Surface ) {
											z -= id.Height;
										}
									}

									for ( int i = 0; !valid && i < tiles.Length; ++i )
									{
										if ( tiles[i].Z == z && tiles[i].ID == graphic )
											valid = true;
									}

									if ( !valid )
									{
										t.Cancel( from, TargetCancelType.Canceled );
										return;
									}
									else
									{
										toTarget = new StaticTarget( new Point3D( x, y, z ), graphic );
									}
								}
							}
						}
						else if ( serial.IsMobile )
						{
							toTarget = World.FindMobile( serial );
						}
						else if ( serial.IsItem )
						{
							toTarget = World.FindItem( serial );
						}
						else
						{
							t.Cancel( from, TargetCancelType.Canceled );
							return;
						}

						t.Invoke( from, toTarget );
					}
				} finally {
					if ( prof != null ) {
						prof.Finish();
					}
				}
			}
		}

		public static void DisplayGumpResponse( NetState state, PacketReader pvSrc ) {
			int serial = pvSrc.ReadInt32();
			int typeID = pvSrc.ReadInt32();
			int buttonID = pvSrc.ReadInt32();

			foreach ( Gump gump in state.Gumps ) {
				if ( gump.Serial == serial && gump.TypeID == typeID ) {
					int switchCount = pvSrc.ReadInt32();

					if ( switchCount < 0 || switchCount > gump.m_Switches ) {
						state.WriteConsole( "Invalid gump response, disconnecting..." );
						state.Dispose();
						return;
					}

					int[] switches = new int[switchCount];

					for ( int j = 0; j < switches.Length; ++j )
						switches[j] = pvSrc.ReadInt32();

					int textCount = pvSrc.ReadInt32();

					if ( textCount < 0 || textCount > gump.m_TextEntries ) {
						state.WriteConsole( "Invalid gump response, disconnecting..." );
						state.Dispose();
						return;
					}

					TextRelay[] textEntries = new TextRelay[textCount];

					for ( int j = 0; j < textEntries.Length; ++j ) {
						int entryID = pvSrc.ReadUInt16();
						int textLength = pvSrc.ReadUInt16();

						if ( textLength > 239 ) {
							state.WriteConsole( "Invalid gump response, disconnecting..." );
							state.Dispose();
							return;
						}

						string text = pvSrc.ReadUnicodeStringSafe( textLength );
						textEntries[j] = new TextRelay( entryID, text );
					}

					state.RemoveGump( gump );

					GumpProfile prof = GumpProfile.Acquire( gump.GetType() );

					if ( prof != null ) {
						prof.Start();
					}

					gump.OnResponse( state, new RelayInfo( buttonID, switches, textEntries ) );

					if ( prof != null ) {
						prof.Finish();
					}

					return;
				}
			}

			if ( typeID == 461 ) { // Virtue gump
				int switchCount = pvSrc.ReadInt32();

				if ( buttonID == 1 && switchCount > 0 ) {
					Mobile beheld = World.FindMobile( pvSrc.ReadInt32() );

					if ( beheld != null ) {
						EventSink.InvokeVirtueGumpRequest( new VirtueGumpRequestEventArgs( state.Mobile, beheld ) );
					}
				} else {
					Mobile beheld = World.FindMobile( serial );

					if ( beheld != null ) {
						EventSink.InvokeVirtueItemRequest( new VirtueItemRequestEventArgs( state.Mobile, beheld, buttonID ) );
					}
				}
			}
		}

		public static void SetWarMode( NetState state, PacketReader pvSrc )
		{
			state.Mobile.DelayChangeWarmode( pvSrc.ReadBoolean() );
		}

		public static void Resynchronize( NetState state, PacketReader pvSrc )
		{
			Mobile m = state.Mobile;

			if ( state.StygianAbyss ) {
				state.Send( new MobileUpdate( m ) );
				state.Send( new MobileIncoming( m, m ) );
			} else {
				state.Send( new MobileUpdateOld( m ) );
				state.Send( new MobileIncomingOld( m, m ) );
			}

			m.SendEverything();

			state.Sequence = 0;

			m.ClearFastwalkStack();
		}

		private static int[] m_EmptyInts = new int[0];
		private static KeywordList m_AsciiKeywordList = new KeywordList();
		public static void AsciiSpeech( NetState state, PacketReader pvSrc )
		{
			Mobile from = state.Mobile;
			int[] keywords;
			MessageType type = (MessageType)pvSrc.ReadByte();
			int hue = pvSrc.ReadInt16();
			pvSrc.ReadInt16(); // font
			string text = pvSrc.ReadStringSafe().Trim();

			if ( text.Length <= 0 || text.Length > 128 )
				return;

			if ( !Enum.IsDefined( typeof( MessageType ), type ) )
				type = MessageType.Regular;

			KeywordList keyList = m_AsciiKeywordList;
			if (text.Contains("withdraw"))
				keyList.Add(0x0000);
			if (text.Contains("withdrawl"))
				keyList.Add(0x0000);
			if (text.Contains("balance"))
				keyList.Add(0x0001);
			if (text.Contains("statement"))
				keyList.Add(0x0001);
			if (text.Contains("bank"))
				keyList.Add(0x0002);
			if (text.Contains("check"))
				keyList.Add(0x0003);
			if (text.Contains("join"))
				keyList.Add(0x0004);
			if (text.Contains("member"))
				keyList.Add(0x0004);
			if (text.Contains("quit"))
				keyList.Add(0x0005);
			if (text.Contains("resign"))
				keyList.Add(0x0005);
			if (text.Contains("guild"))
				keyList.Add(0x0006);
			if (text.Contains("guilds"))
				keyList.Add(0x0006);
			if (text.Contains("guard"))
				keyList.Add(0x0007);
			if (text.Contains("guards"))
				keyList.Add(0x0007);
			if (text.Contains("stable"))
				keyList.Add(0x0008);
			if (text.Contains("claim"))
				keyList.Add(0x0009);
			if (text.Contains("job"))
				keyList.Add(0x000A);
			if (text.Contains("dock"))
				keyList.Add(0x000B);
			if (text.Contains("servant"))
				keyList.Add(0x000C);
			if (text.Contains("serve"))
				keyList.Add(0x000C);
			if (text.Contains("slave"))
				keyList.Add(0x000C);
			if (text.Contains("agreed"))
				keyList.Add(0x000D);
			if (text.Contains("quest"))
				keyList.Add(0x000E);
			if (text.Contains("hunt"))
				keyList.Add(0x000F);
			if (text.Contains("track"))
				keyList.Add(0x000F);
			if (text.Contains("tracking"))
				keyList.Add(0x000F);
			if (text.Contains("dragon"))
				keyList.Add(0x0010);
			if (text.Contains("nay"))
				keyList.Add(0x0011);
			if (text.Contains("no"))
				keyList.Add(0x0011);
			if (text.Contains("nope"))
				keyList.Add(0x0011);
			if (text.Contains("aye"))
				keyList.Add(0x0012);
			if (text.Contains("yea"))
				keyList.Add(0x0012);
			if (text.Contains("yes"))
				keyList.Add(0x0012);
			if (text.Contains("yup"))
				keyList.Add(0x0012);
			if (text.Contains("slay"))
				keyList.Add(0x0013);
			if (text.Contains("hammer"))
				keyList.Add(0x0014);
			if (text.Contains("longsword"))
				keyList.Add(0x0015);
			if (text.Contains("sword"))
				keyList.Add(0x0015);
			if (text.Contains("assistance"))
				keyList.Add(0x0016);
			if (text.Contains("help"))
				keyList.Add(0x0016);
			if (text.Contains("enchanted"))
				keyList.Add(0x0017);
			if (text.Contains("glass"))
				keyList.Add(0x0017);
			if (text.Contains("make"))
				keyList.Add(0x0018);
			if (text.Contains("addtime"))
				keyList.Add(0x0019);
			if (text.Contains("gettime"))
				keyList.Add(0x001A);
			if (text.Contains("hint"))
				keyList.Add(0x001B);
			if (text.Contains("test"))
				keyList.Add(0x001C);
			if (text.Contains("destination"))
				keyList.Add(0x001D);
			if (text.Contains("i will take thee"))
				keyList.Add(0x001E);
			if (text.Contains("disguise"))
				keyList.Add(0x001F);
			if (text.Contains("virtue guard"))
				keyList.Add(0x0020);
			if (text.Contains("order shield"))
				keyList.Add(0x0021);
			if (text.Contains("chaos shield"))
				keyList.Add(0x0022);
			if (text.Contains("i wish to lock this down"))
				keyList.Add(0x0023);
			if (text.Contains("i wish to release this"))
				keyList.Add(0x0024);
			if (text.Contains("i wish to secure this"))
				keyList.Add(0x0025);
			if (text.Contains("i wish to unsecure this"))
				keyList.Add(0x0026);
			if (text.Contains("i wish to place a strongbox"))
				keyList.Add(0x0027);
			if (text.Contains("i wish to place a trash barrel"))
				keyList.Add(0x0028);
			if (text.Contains("showthelist"))
				keyList.Add(0x0029);
			if (text.Contains("i resign from my guild"))
				keyList.Add(0x002A);
			if (text.Contains("set"))
				keyList.Add(0x002B);
			if (text.Contains("raise"))
				keyList.Add(0x002C);
			if (text.Contains("drop"))
				keyList.Add(0x002D);
			if (text.Contains("abracadabra"))
				keyList.Add(0x002E);
			if (text.Contains("shazam"))
				keyList.Add(0x002F);
			if (text.Contains("news"))
				keyList.Add(0x0030);
			if (text.Contains("i honor thee"))
				keyList.Add(0x0031);
			if (text.Contains("i must consider my sins"))
				keyList.Add(0x0032);
			if (text.Contains("remove thyself"))
				keyList.Add(0x0033);
			if (text.Contains("i ban thee"))
				keyList.Add(0x0034);
			if (text.Contains("i renounce my young player status"))
				keyList.Add(0x0035);
			if (text.Contains("stop"))
				keyList.Add(0x0036);
			if (text.Contains("resetme"))
				keyList.Add(0x0037);
			if (text.Contains("appraise"))
				keyList.Add(0x0038);
			if (text.Contains("reset"))
				keyList.Add(0x0039);
			if (text.Contains("hint"))
				keyList.Add(0x003A);
			if (text.Contains("greetings"))
				keyList.Add(0x003B);
			if (text.Contains("hail"))
				keyList.Add(0x003B);
			if (text.Contains("hello"))
				keyList.Add(0x003B);
			if (text.Contains("hey"))
				keyList.Add(0x003B);
			if (text.Contains("yo"))
				keyList.Add(0x003B);
			if (text.Contains("vendor buy"))
				keyList.Add(0x003C);
			if (text.Contains("vendor purchase"))
				keyList.Add(0x003C);
			if (text.Contains("vendor browse"))
				keyList.Add(0x003D);
			if (text.Contains("vendor look"))
				keyList.Add(0x003D);
			if (text.Contains("vendor view"))
				keyList.Add(0x003D);
			if (text.Contains("vendor collect"))
				keyList.Add(0x003E);
			if (text.Contains("vendor get"))
				keyList.Add(0x003E);
			if (text.Contains("vendor gold"))
				keyList.Add(0x003E);
			if (text.Contains("vendor info"))
				keyList.Add(0x003F);
			if (text.Contains("vendor status"))
				keyList.Add(0x003F);
			if (text.Contains("vendor dismiss"))
				keyList.Add(0x0040);
			if (text.Contains("vendor replace"))
				keyList.Add(0x0040);
			if (text.Contains("vendor cycle"))
				keyList.Add(0x0041);
			if (text.Contains("set name"))
				keyList.Add(0x0042);
			if (text.Contains("remove name"))
				keyList.Add(0x0043);
			if (text.Contains("name"))
				keyList.Add(0x0044);
			if (text.Contains("forward"))
				keyList.Add(0x0045);
			if (text.Equals("back"))
				keyList.Add(0x0046);
			if (text.Equals("backward"))
				keyList.Add(0x0046);
			if (text.Contains("backwards"))
				keyList.Add(0x0046);
			if (text.Contains("drift left"))
				keyList.Add(0x0047);
			if (text.Equals("left"))
				keyList.Add(0x0047);
			if (text.Contains("drift right"))
				keyList.Add(0x0048);
			if (text.Equals("right"))
				keyList.Add(0x0048);
			if (text.Contains("starboard"))
				keyList.Add(0x0049);
			if (text.Contains("port"))
				keyList.Add(0x004A);
			if (text.Contains("forward left"))
				keyList.Add(0x004B);
			if (text.Contains("forward right"))
				keyList.Add(0x004C);
			if (text.Contains("back left"))
				keyList.Add(0x004D);
			if (text.Contains("backward left"))
				keyList.Add(0x004D);
			if (text.Contains("backwards left"))
				keyList.Add(0x004D);
			if (text.Contains("back right"))
				keyList.Add(0x004E);
			if (text.Contains("backward right"))
				keyList.Add(0x004E);
			if (text.Contains("backwards right"))
				keyList.Add(0x004E);
			if (text.Contains("stop"))
				keyList.Add(0x004F);
			if (text.Contains("slow left"))
				keyList.Add(0x0050);
			if (text.Contains("slow right"))
				keyList.Add(0x0051);
			if (text.Contains("slow forward"))
				keyList.Add(0x0052);
			if (text.Contains("slow back"))
				keyList.Add(0x0053);
			if (text.Contains("slow backward"))
				keyList.Add(0x0053);
			if (text.Contains("slow backwards"))
				keyList.Add(0x0053);
			if (text.Contains("slow forward left"))
				keyList.Add(0x0054);
			if (text.Contains("slow forward right"))
				keyList.Add(0x0055);
			if (text.Contains("slow back right"))
				keyList.Add(0x0056);
			if (text.Contains("slow backward right"))
				keyList.Add(0x0056);
			if (text.Contains("slow backwards right"))
				keyList.Add(0x0056);
			if (text.Contains("slow back left"))
				keyList.Add(0x0057);
			if (text.Contains("slow backward left"))
				keyList.Add(0x0057);
			if (text.Contains("slow backwards left"))
				keyList.Add(0x0057);
			if (text.Contains("left one"))
				keyList.Add(0x0058);
			if (text.Contains("one left"))
				keyList.Add(0x0058);
			if (text.Contains("one right"))
				keyList.Add(0x0059);
			if (text.Contains("right one"))
				keyList.Add(0x0059);
			if (text.Contains("forward one"))
				keyList.Add(0x005A);
			if (text.Contains("one forward"))
				keyList.Add(0x005A);
			if (text.Contains("back one"))
				keyList.Add(0x005B);
			if (text.Contains("backward one"))
				keyList.Add(0x005B);
			if (text.Contains("backwards one"))
				keyList.Add(0x005B);
			if (text.Contains("one back"))
				keyList.Add(0x005B);
			if (text.Contains("one backward"))
				keyList.Add(0x005B);
			if (text.Contains("one backwards"))
				keyList.Add(0x005B);
			if (text.Contains("forward left one"))
				keyList.Add(0x005C);
			if (text.Contains("one forward left"))
				keyList.Add(0x005C);
			if (text.Contains("forward right one"))
				keyList.Add(0x005D);
			if (text.Contains("one forward right"))
				keyList.Add(0x005D);
			if (text.Contains("back right one"))
				keyList.Add(0x005E);
			if (text.Contains("backward right one"))
				keyList.Add(0x005E);
			if (text.Contains("backwards right one"))
				keyList.Add(0x005E);
			if (text.Contains("one back right"))
				keyList.Add(0x005E);
			if (text.Contains("one backward right"))
				keyList.Add(0x005E);
			if (text.Contains("one backwards right"))
				keyList.Add(0x005E);
			if (text.Contains("back left one"))
				keyList.Add(0x005F);
			if (text.Contains("backward left one"))
				keyList.Add(0x005F);
			if (text.Contains("backwards left one"))
				keyList.Add(0x005F);
			if (text.Contains("one back left"))
				keyList.Add(0x005F);
			if (text.Contains("one backward left"))
				keyList.Add(0x005F);
			if (text.Contains("one backwards left"))
				keyList.Add(0x005F);
			if (text.Contains("nav"))
				keyList.Add(0x0060);
			if (text.Contains("start"))
				keyList.Add(0x0061);
			if (text.Contains("continue"))
				keyList.Add(0x0062);
			if (text.Contains("goto"))
				keyList.Add(0x0063);
			if (text.Contains("single"))
				keyList.Add(0x0064);
			if (text.Contains("turn right"))
				keyList.Add(0x0065);
			if (text.Contains("turn left"))
				keyList.Add(0x0066);
			if (text.Contains("come about"))
				keyList.Add(0x0067);
			if (text.Contains("turn around"))
				keyList.Add(0x0067);
			if (text.Contains("unfurl sail"))
				keyList.Add(0x0068);
			if (text.Contains("furl sail"))
				keyList.Add(0x0069);
			if (text.Contains("drop anchor"))
				keyList.Add(0x006A);
			if (text.Contains("lower anchor"))
				keyList.Add(0x006A);
			if (text.Contains("hoist anchor"))
				keyList.Add(0x006B);
			if (text.Contains("lift anchor"))
				keyList.Add(0x006B);
			if (text.Contains("raise anchor"))
				keyList.Add(0x006B);
			if (text.EndsWith("train"))
				keyList.Add(0x006C);
			if (text.Contains("train battle"))
				keyList.Add(0x006D);
			if (text.Contains("train defense"))
				keyList.Add(0x006D);
			if (text.Contains("train parry"))
				keyList.Add(0x006D);
			if (text.Contains("train parrying"))
				keyList.Add(0x006D);
			if (text.Contains("train aid"))
				keyList.Add(0x006E);
			if (text.Contains("train first"))
				keyList.Add(0x006E);
			if (text.Contains("train heal"))
				keyList.Add(0x006E);
			if (text.Contains("train healing"))
				keyList.Add(0x006E);
			if (text.Contains("train medicine"))
				keyList.Add(0x006E);
			if (text.Contains("train hide"))
				keyList.Add(0x006F);
			if (text.Contains("train hiding"))
				keyList.Add(0x006F);
			if (text.Contains("train steal"))
				keyList.Add(0x0070);
			if (text.Contains("train stealing"))
				keyList.Add(0x0070);
			if (text.Contains("train alchemy"))
				keyList.Add(0x0071);
			if (text.Contains("train animal"))
				keyList.Add(0x0072);
			if (text.Contains("train lore"))
				keyList.Add(0x0072);
			if (text.Contains("train appraise"))
				keyList.Add(0x0073);
			if (text.Contains("train appraising"))
				keyList.Add(0x0073);
			if (text.Contains("train identification"))
				keyList.Add(0x0073);
			if (text.Contains("train identify"))
				keyList.Add(0x0073);
			if (text.Contains("train identifying"))
				keyList.Add(0x0073);
			if (text.Contains("train item"))
				keyList.Add(0x0073);
			if (text.Contains("train arms"))
				keyList.Add(0x0074);
			if (text.Contains("train armslore"))
				keyList.Add(0x0074);
			if (text.Contains("train beg"))
				keyList.Add(0x0075);
			if (text.Contains("train begging"))
				keyList.Add(0x0075);
			if (text.Contains("train blacksmith"))
				keyList.Add(0x0076);
			if (text.Contains("train blacksmithing"))
				keyList.Add(0x0076);
			if (text.Contains("train blacksmithy"))
				keyList.Add(0x0076);
			if (text.Contains("train smith"))
				keyList.Add(0x0076);
			if (text.Contains("train smithing"))
				keyList.Add(0x0076);
			if (text.Contains("train arrow"))
				keyList.Add(0x0077);
			if (text.Contains("train bow"))
				keyList.Add(0x0077);
			if (text.Contains("train bowcraft"))
				keyList.Add(0x0077);
			if (text.Contains("train bower"))
				keyList.Add(0x0077);
			if (text.Contains("train fletcher"))
				keyList.Add(0x0077);
			if (text.Contains("train fletching"))
				keyList.Add(0x0077);
			if (text.Contains("train calm"))
				keyList.Add(0x0078);
			if (text.Contains("train peace"))
				keyList.Add(0x0078);
			if (text.Contains("train peacemaking"))
				keyList.Add(0x0078);
			if (text.Contains("train camp"))
				keyList.Add(0x0079);
			if (text.Contains("train camping"))
				keyList.Add(0x0079);
			if (text.Contains("train carpentry"))
				keyList.Add(0x007A);
			if (text.Contains("train woodwork"))
				keyList.Add(0x007A);
			if (text.Contains("train woodworking"))
				keyList.Add(0x007A);
			if (text.Contains("train cartography"))
				keyList.Add(0x007B);
			if (text.Contains("train map"))
				keyList.Add(0x007B);
			if (text.Contains("train mapmaking"))
				keyList.Add(0x007B);
			if (text.Contains("train cook"))
				keyList.Add(0x007C);
			if (text.Contains("train cooking"))
				keyList.Add(0x007C);
			if (text.Contains("train detect"))
				keyList.Add(0x007D);
			if (text.Contains("train detecting hidden"))
				keyList.Add(0x007D);
			if (text.Contains("train hidden"))
				keyList.Add(0x007D);
			if (text.Contains("train entice"))
				keyList.Add(0x007E);
			if (text.Contains("train enticement"))
				keyList.Add(0x007E);
			if (text.Contains("train enticing"))
				keyList.Add(0x007E);
			if (text.Contains("train evaluate"))
				keyList.Add(0x007F);
			if (text.Contains("train evaluating"))
				keyList.Add(0x007F);
			if (text.Contains("train intelligence"))
				keyList.Add(0x007F);
			if (text.Contains("train fish"))
				keyList.Add(0x0080);
			if (text.Contains("train fishing"))
				keyList.Add(0x0080);
			if (text.Contains("train incite"))
				keyList.Add(0x0081);
			if (text.Contains("train provocation"))
				keyList.Add(0x0081);
			if (text.Contains("train provoke"))
				keyList.Add(0x0081);
			if (text.Contains("train provoking"))
				keyList.Add(0x0081);
			if (text.Contains("train lock"))
				keyList.Add(0x0082);
			if (text.Contains("train lockpicking"))
				keyList.Add(0x0082);
			if (text.Contains("train locks"))
				keyList.Add(0x0082);
			if (text.Contains("train pick"))
				keyList.Add(0x0082);
			if (text.Contains("train picking"))
				keyList.Add(0x0082);
			if (text.Contains("train mage"))
				keyList.Add(0x0083);
			if (text.Contains("train magery"))
				keyList.Add(0x0083);
			if (text.Contains("train magic"))
				keyList.Add(0x0083);
			if (text.Contains("train sorcery"))
				keyList.Add(0x0083);
			if (text.Contains("train wizard"))
				keyList.Add(0x0083);
			if (text.Contains("train wizardry"))
				keyList.Add(0x0083);
			if (text.Contains("train resist"))
				keyList.Add(0x0084);
			if (text.Contains("train resisting"))
				keyList.Add(0x0084);
			if (text.Contains("train spells"))
				keyList.Add(0x0084);
			if (text.Contains("train battle"))
				keyList.Add(0x0085);
			if (text.Contains("train fight"))
				keyList.Add(0x0085);
			if (text.Contains("train fighting"))
				keyList.Add(0x0085);
			if (text.Contains("train tactic"))
				keyList.Add(0x0085);
			if (text.Contains("train tactics"))
				keyList.Add(0x0085);
			if (text.Contains("train peek"))
				keyList.Add(0x0086);
			if (text.Contains("train peeking"))
				keyList.Add(0x0086);
			if (text.Contains("train snoop"))
				keyList.Add(0x0086);
			if (text.Contains("train snooping"))
				keyList.Add(0x0086);
			if (text.Contains("train disarm"))
				keyList.Add(0x0087);
			if (text.Contains("train disarming"))
				keyList.Add(0x0087);
			if (text.Contains("train remove"))
				keyList.Add(0x0087);
			if (text.Contains("train removing"))
				keyList.Add(0x0087);
			if (text.Contains("train instrument"))
				keyList.Add(0x0088);
			if (text.Contains("train music"))
				keyList.Add(0x0088);
			if (text.Contains("train musician"))
				keyList.Add(0x0088);
			if (text.Contains("train musicianship"))
				keyList.Add(0x0088);
			if (text.Contains("train play"))
				keyList.Add(0x0088);
			if (text.Contains("train playing"))
				keyList.Add(0x0088);
			if (text.Contains("train poison"))
				keyList.Add(0x0089);
			if (text.Contains("train poisoning"))
				keyList.Add(0x0089);
			if (text.Contains("train archer"))
				keyList.Add(0x008A);
			if (text.Contains("train archery"))
				keyList.Add(0x008A);
			if (text.Contains("train missile"))
				keyList.Add(0x008A);
			if (text.Contains("train missiles"))
				keyList.Add(0x008A);
			if (text.Contains("train ranged"))
				keyList.Add(0x008A);
			if (text.Contains("train shoot"))
				keyList.Add(0x008A);
			if (text.Contains("train shooting"))
				keyList.Add(0x008A);
			if (text.Contains("train ghost"))
				keyList.Add(0x008B);
			if (text.Contains("train seance"))
				keyList.Add(0x008B);
			if (text.Contains("train spirit"))
				keyList.Add(0x008B);
			if (text.Contains("train spiritualism"))
				keyList.Add(0x008B);
			if (text.Contains("train clothier"))
				keyList.Add(0x008C);
			if (text.Contains("train tailor"))
				keyList.Add(0x008C);
			if (text.Contains("train tailoring"))
				keyList.Add(0x008C);
			if (text.Contains("train tame"))
				keyList.Add(0x008D);
			if (text.Contains("train taming"))
				keyList.Add(0x008D);
			if (text.Contains("train taste"))
				keyList.Add(0x008E);
			if (text.Contains("train tasting"))
				keyList.Add(0x008E);
			if (text.Contains("train tinker"))
				keyList.Add(0x008F);
			if (text.Contains("train tinkering"))
				keyList.Add(0x008F);
			if (text.Contains("train vet"))
				keyList.Add(0x0090);
			if (text.Contains("train veterinarian"))
				keyList.Add(0x0090);
			if (text.Contains("train veterinary"))
				keyList.Add(0x0090);
			if (text.Contains("train forensic"))
				keyList.Add(0x0091);
			if (text.Contains("train forensics"))
				keyList.Add(0x0091);
			if (text.Contains("train herd"))
				keyList.Add(0x0092);
			if (text.Contains("train herding"))
				keyList.Add(0x0092);
			if (text.Contains("train hunt"))
				keyList.Add(0x0093);
			if (text.Contains("train hunting"))
				keyList.Add(0x0093);
			if (text.Contains("train track"))
				keyList.Add(0x0093);
			if (text.Contains("train tracking"))
				keyList.Add(0x0093);
			if (text.Contains("train stealth"))
				keyList.Add(0x0094);
			if (text.Contains("train inscribe"))
				keyList.Add(0x0095);
			if (text.Contains("train inscribing"))
				keyList.Add(0x0095);
			if (text.Contains("train inscription"))
				keyList.Add(0x0095);
			if (text.Contains("train scroll"))
				keyList.Add(0x0095);
			if (text.Contains("train blade"))
				keyList.Add(0x0096);
			if (text.Contains("train blades"))
				keyList.Add(0x0096);
			if (text.Contains("train sword"))
				keyList.Add(0x0096);
			if (text.Contains("train swords"))
				keyList.Add(0x0096);
			if (text.Contains("train swordsman"))
				keyList.Add(0x0096);
			if (text.Contains("train swordsmanship"))
				keyList.Add(0x0096);
			if (text.Contains("train club"))
				keyList.Add(0x0097);
			if (text.Contains("train clubs"))
				keyList.Add(0x0097);
			if (text.Contains("train mace"))
				keyList.Add(0x0097);
			if (text.Contains("train maces"))
				keyList.Add(0x0097);
			if (text.Contains("train dagger"))
				keyList.Add(0x0098);
			if (text.Contains("train daggers"))
				keyList.Add(0x0098);
			if (text.Contains("train fence"))
				keyList.Add(0x0098);
			if (text.Contains("train fencing"))
				keyList.Add(0x0098);
			if (text.Contains("train spear"))
				keyList.Add(0x0098);
			if (text.Contains("train hand"))
				keyList.Add(0x0099);
			if (text.Contains("train wrestle"))
				keyList.Add(0x0099);
			if (text.Contains("train wrestling"))
				keyList.Add(0x0099);
			if (text.Contains("train lumberjack"))
				keyList.Add(0x009A);
			if (text.Contains("train lumberjacking"))
				keyList.Add(0x009A);
			if (text.Contains("train woodcutting"))
				keyList.Add(0x009A);
			if (text.Contains("train mine"))
				keyList.Add(0x009B);
			if (text.Contains("train mining"))
				keyList.Add(0x009B);
			if (text.Contains("train smelt"))
				keyList.Add(0x009B);
			if (text.Contains("train meditate"))
				keyList.Add(0x009C);
			if (text.Contains("train meditation"))
				keyList.Add(0x009C);
			if (text.Contains("move"))
				keyList.Add(0x009D);
			if (text.Contains("time"))
				keyList.Add(0x009E);
			if (text.Contains("where is the shrine"))
				keyList.Add(0x009F);
			if (text.Contains("where is abbey"))
				keyList.Add(0x00A0);
			if (text.Contains("where is empath"))
				keyList.Add(0x00A0);
			if (text.Contains("where is britain"))
				keyList.Add(0x00A1);
			if (text.Contains("where is buccaneer"))
				keyList.Add(0x00A2);
			if (text.Contains("where is den"))
				keyList.Add(0x00A2);
			if (text.Contains("where is jhelom"))
				keyList.Add(0x00A3);
			if (text.Contains("where is magincia"))
				keyList.Add(0x00A4);
			if (text.Contains("where is vesper"))
				keyList.Add(0x00A5);
			if (text.Contains("where is minoc"))
				keyList.Add(0x00A6);
			if (text.Contains("where is moonglow"))
				keyList.Add(0x00A7);
			if (text.Contains("where is nujel"))
				keyList.Add(0x00A8);
			if (text.Contains("where is ocllo"))
				keyList.Add(0x00A9);
			if (text.Contains("where is serpent"))
				keyList.Add(0x00AA);
			if (text.Contains("where is skara"))
				keyList.Add(0x00AB);
			if (text.Contains("where is trinsic"))
				keyList.Add(0x00AC);
			if (text.Contains("where is yew"))
				keyList.Add(0x00AD);
			if (text.Contains("where is cove"))
				keyList.Add(0x00AE);
			if (text.Contains("where is the woodworker"))
				keyList.Add(0x00AF);
			if (text.Contains("where is the alchemist"))
				keyList.Add(0x00B0);
			if (text.Contains("where is the animal"))
				keyList.Add(0x00B1);
			if (text.Contains("where is the armorer"))
				keyList.Add(0x00B2);
			if (text.Contains("where is the artisan"))
				keyList.Add(0x00B3);
			if (text.Contains("where is the baker"))
				keyList.Add(0x00B4);
			if (text.Contains("where is the bank"))
				keyList.Add(0x00B5);
			if (text.Contains("where is the bard"))
				keyList.Add(0x00B6);
			if (text.Contains("where is the bath"))
				keyList.Add(0x00B7);
			if (text.Contains("where is the beekeeper"))
				keyList.Add(0x00B8);
			if (text.Contains("where is the smith"))
				keyList.Add(0x00B9);
			if (text.Contains("where is the blackthorn"))
				keyList.Add(0x00BA);
			if (text.Contains("where is the bowyer"))
				keyList.Add(0x00BB);
			if (text.Contains("where is the butcher"))
				keyList.Add(0x00BC);
			if (text.Contains("where is the carpenter"))
				keyList.Add(0x00BD);
			if (text.Contains("where is the casino"))
				keyList.Add(0x00BE);
			if (text.Contains("where is the cemetery"))
				keyList.Add(0x00BF);
			if (text.Contains("where is the clothier"))
				keyList.Add(0x00C0);
			if (text.Contains("where is the cobbler"))
				keyList.Add(0x00C1);
			if (text.Contains("where is the court"))
				keyList.Add(0x00C2);
			if (text.Contains("where is the customs"))
				keyList.Add(0x00C3);
			if (text.Contains("where is the dock"))
				keyList.Add(0x00C4);
			if (text.Contains("where is the duel"))
				keyList.Add(0x00C5);
			if (text.Contains("where is the farm"))
				keyList.Add(0x00C6);
			if (text.Contains("where is the fish"))
				keyList.Add(0x00C7);
			if (text.Contains("where is the glassblower"))
				keyList.Add(0x00C8);
			if (text.Contains("where is the gypsy"))
				keyList.Add(0x00C9);
			if (text.Contains("where is the healer"))
				keyList.Add(0x00CA);
			if (text.Contains("where is the herbalist"))
				keyList.Add(0x00CB);
			if (text.Contains("where is the hostel"))
				keyList.Add(0x00CC);
			if (text.Contains("where is the inn"))
				keyList.Add(0x00CC);
			if (text.Contains("where is the jail"))
				keyList.Add(0x00CD);
			if (text.Contains("where is the jeweler"))
				keyList.Add(0x00CE);
			if (text.Contains("where is the castle"))
				keyList.Add(0x00CF);
			if (text.Contains("where is the library"))
				keyList.Add(0x00D0);
			if (text.Contains("where is the lighthouse"))
				keyList.Add(0x00D1);
			if (text.Contains("where is the mage"))
				keyList.Add(0x00D2);
			if (text.Contains("where is the magic"))
				keyList.Add(0x00D3);
			if (text.Contains("where is the merchant"))
				keyList.Add(0x00D4);
			if (text.Contains("where is the mill"))
				keyList.Add(0x00D5);
			if (text.Contains("where is the observatory"))
				keyList.Add(0x00D6);
			if (text.Contains("where is the painter"))
				keyList.Add(0x00D7);
			if (text.Contains("where is the paladin"))
				keyList.Add(0x00D8);
			if (text.Contains("where is the provisioner"))
				keyList.Add(0x00D9);
			if (text.Contains("where is the shop"))
				keyList.Add(0x00D9);
			if (text.Contains("where is the ship"))
				keyList.Add(0x00DA);
			if (text.Contains("where is the stable"))
				keyList.Add(0x00DB);
			if (text.Contains("where is the tanner"))
				keyList.Add(0x00DC);
			if (text.Contains("where is the bard"))
				keyList.Add(0x00DD);
			if (text.Contains("where is the pub"))
				keyList.Add(0x00DD);
			if (text.Contains("where is the tavern"))
				keyList.Add(0x00DD);
			if (text.Contains("where is the temple"))
				keyList.Add(0x00DE);
			if (text.Contains("where is the theater"))
				keyList.Add(0x00DF);
			if (text.Contains("where is the tinker"))
				keyList.Add(0x00E0);
			if (text.Contains("where is the vet"))
				keyList.Add(0x00E1);
			if (text.Contains("where is the veterinarian"))
				keyList.Add(0x00E1);
			if (text.Contains("where is the weapon"))
				keyList.Add(0x00E2);
			if (text.Contains("where is the trainer"))
				keyList.Add(0x00E3);
			if (text.Contains("i wish to access the city treasury"))
				keyList.Add(0x00E4);
			if (text.Contains("i wish to resign as finance minister"))
				keyList.Add(0x00E5);
			if (text.Contains("orders"))
				keyList.Add(0x00E6);
			if (text.Contains("patrol"))
				keyList.Add(0x00E7);
			if (text.Contains("follow"))
				keyList.Add(0x00E8);
			if (text.Contains("what is my faction term status"))
				keyList.Add(0x00E9);
			if (text.Contains("message faction"))
				keyList.Add(0x00EA);
			if (text.Contains("message all"))
				keyList.Add(0x00EB);
			if (text.Contains("showscore"))
				keyList.Add(0x00EC);
			if (text.Contains("i am sheriff"))
				keyList.Add(0x00ED);
			if (text.Contains("i wish to resign as sheriff"))
				keyList.Add(0x00EE);
			if (text.Contains("you are fired"))
				keyList.Add(0x00EF);
			if (text.Contains("need help"))
				keyList.Add(0x00F0);
			if (text.Contains("hiring"))
				keyList.Add(0x00F1);
			if (text.Contains("hire me"))
				keyList.Add(0x00F2);
			if (text.Contains("a job"))
				keyList.Add(0x00F3);
			if (text.Contains("i resign from my quest"))
				keyList.Add(0x00F4);
			if (text.Contains("enter tutorial"))
				keyList.Add(0x00F5);
			if (text.Contains("enter haven"))
				keyList.Add(0x00F6);
			if (text.Contains("skill"))
				keyList.Add(0x00F7);
			if (text.Contains("job"))
				keyList.Add(0x00F8);
			if (text.Contains("occupation"))
				keyList.Add(0x00F8);
			if (text.Contains("profession"))
				keyList.Add(0x00F8);
			if (text.Contains("what do you do"))
				keyList.Add(0x00F8);
			if (text.Contains("trainer"))
				keyList.Add(0x00F9);
			if (text.Contains("bye"))
				keyList.Add(0x00FA);
			if (text.Contains("farewell"))
				keyList.Add(0x00FA);
			if (text.Contains("see ya"))
				keyList.Add(0x00FA);
			if (text.Contains("thou need"))
				keyList.Add(0x00FB);
			if (text.Contains("thou needs"))
				keyList.Add(0x00FB);
			if (text.Contains("thou want"))
				keyList.Add(0x00FB);
			if (text.Contains("you need"))
				keyList.Add(0x00FB);
			if (text.Contains("you require"))
				keyList.Add(0x00FB);
			if (text.Contains("you want"))
				keyList.Add(0x00FB);
			if (text.Contains("are you well"))
				keyList.Add(0x00FC);
			if (text.Contains("art thou well"))
				keyList.Add(0x00FC);
			if (text.Contains("how are you"))
				keyList.Add(0x00FC);
			if (text.Contains("how art thou"))
				keyList.Add(0x00FC);
			if (text.Contains("colony"))
				keyList.Add(0x00FD);
			if (text.Contains("thou sale"))
				keyList.Add(0x00FE);
			if (text.Contains("thou sell"))
				keyList.Add(0x00FE);
			if (text.Contains("you sale"))
				keyList.Add(0x00FE);
			if (text.Contains("you sell"))
				keyList.Add(0x00FE);
			if (text.Contains("purchase"))
				keyList.Add(0x00FF);
			if (text.Contains("sale"))
				keyList.Add(0x00FF);
			if (text.Contains("you buy"))
				keyList.Add(0x00FF);
			if (text.Contains("you purchase"))
				keyList.Add(0x00FF);
			if (text.Contains("crypt"))
				keyList.Add(0x0100);
			if (text.Contains("graves"))
				keyList.Add(0x0100);
			if (text.Contains("graveyard"))
				keyList.Add(0x0100);
			if (text.Contains("skeleton"))
				keyList.Add(0x0100);
			if (text.Contains("undead"))
				keyList.Add(0x0100);
			if (text.Contains("sweet dreams"))
				keyList.Add(0x0101);
			if (text.Contains("river"))
				keyList.Add(0x0102);
			if (text.Contains("king"))
				keyList.Add(0x0103);
			if (text.Contains("lb"))
				keyList.Add(0x0103);
			if (text.Contains("lord british"))
				keyList.Add(0x0103);
			if (text.Contains("ruler"))
				keyList.Add(0x0103);
			if (text.Contains("wayfareres inn"))
				keyList.Add(0x0104);
			if (text.Contains("wayfarer's inn"))
				keyList.Add(0x0104);
			if (text.Contains("moat"))
				keyList.Add(0x0105);
			if (text.Contains("narrows neck"))
				keyList.Add(0x0106);
			if (text.Contains("narrows"))
				keyList.Add(0x0106);
			if (text.Contains("neck"))
				keyList.Add(0x0106);
			if (text.Contains("poor gate"))
				keyList.Add(0x0107);
			if (text.Contains("brittany river"))
				keyList.Add(0x0108);
			if (text.Contains("blue boar"))
				keyList.Add(0x0109);
			if (text.Contains("cat's lair"))
				keyList.Add(0x010A);
			if (text.Contains("salty dog"))
				keyList.Add(0x010B);
			if (text.Contains("unicorn horn"))
				keyList.Add(0x010C);
			if (text.Contains("mechanism"))
				keyList.Add(0x010D);
			if (text.Contains("main gate"))
				keyList.Add(0x010E);
			if (text.Contains("northside"))
				keyList.Add(0x010F);
			if (text.Contains("oaken oar"))
				keyList.Add(0x0110);
			if (text.Contains("ocean"))
				keyList.Add(0x0111);
			if (text.Contains("waterfront"))
				keyList.Add(0x0111);
			if (text.Contains("old keep"))
				keyList.Add(0x0112);
			if (text.Contains("gate"))
				keyList.Add(0x0113);
			if (text.Contains("guard house"))
				keyList.Add(0x0114);
			if (text.Contains("guardhouse"))
				keyList.Add(0x0114);
			if (text.Contains("heal"))
				keyList.Add(0x0115);
			if (text.Contains("orc"))
				keyList.Add(0x0116);
			if (text.Contains("wall"))
				keyList.Add(0x0117);
			if (text.Contains("incantations"))
				keyList.Add(0x0118);
			if (text.Contains("bridge"))
				keyList.Add(0x0119);
			if (text.Contains("mage's bridge"))
				keyList.Add(0x011A);
			if (text.Contains("cypress bridge"))
				keyList.Add(0x011B);
			if (text.Contains("northern bridge"))
				keyList.Add(0x011C);
			if (text.Contains("great northern"))
				keyList.Add(0x011D);
			if (text.Contains("great bridge"))
				keyList.Add(0x011E);
			if (text.Contains("gung-farmer"))
				keyList.Add(0x011F);
			if (text.Contains("gung farmer"))
				keyList.Add(0x0120);
			if (text.Contains("river's gate"))
				keyList.Add(0x0121);
			if (text.Contains("lost"))
				keyList.Add(0x0122);
			if (text.Contains("where am i"))
				keyList.Add(0x0122);
			if (text.Contains("sage advice"))
				keyList.Add(0x0123);
			if (text.Contains("sorcerer's delight"))
				keyList.Add(0x0124);
			if (text.Contains("dummies"))
				keyList.Add(0x0125);
			if (text.Contains("dummy"))
				keyList.Add(0x0125);
			if (text.Contains("training dummies"))
				keyList.Add(0x0125);
			if (text.Contains("training dummy"))
				keyList.Add(0x0125);
			if (text.Contains("weather"))
				keyList.Add(0x0126);
			if (text.Contains("bedroll"))
				keyList.Add(0x0127);
			if (text.Contains("dupre"))
				keyList.Add(0x0128);
			if (text.Contains("thou live"))
				keyList.Add(0x0129);
			if (text.Contains("what city are you from"))
				keyList.Add(0x0129);
			if (text.Contains("what town are you from"))
				keyList.Add(0x0129);
			if (text.Contains("where thou from"))
				keyList.Add(0x0129);
			if (text.Contains("where thou live"))
				keyList.Add(0x0129);
			if (text.Contains("where you from"))
				keyList.Add(0x0129);
			if (text.Contains("where you live"))
				keyList.Add(0x0129);
			if (text.Contains("you live"))
				keyList.Add(0x0129);
			if (text.Contains("earn money"))
				keyList.Add(0x012A);
			if (text.Contains("get money"))
				keyList.Add(0x012A);
			if (text.Contains("make money"))
				keyList.Add(0x012A);
			if (text.Contains("where thou work"))
				keyList.Add(0x012B);
			if (text.Contains("where you work"))
				keyList.Add(0x012B);
			if (text.Contains("how quit"))
				keyList.Add(0x012C);
			if (text.Contains("log off"))
				keyList.Add(0x012C);
			if (text.Contains("logoff"))
				keyList.Add(0x012C);
			if (text.Contains("lolo"))
				keyList.Add(0x012D);
			if (text.Contains("camp"))
				keyList.Add(0x012E);
			if (text.Contains("covetous"))
				keyList.Add(0x012F);
			if (text.Contains("deceit"))
				keyList.Add(0x012F);
			if (text.Contains("despise"))
				keyList.Add(0x012F);
			if (text.Contains("destard"))
				keyList.Add(0x012F);
			if (text.Contains("hythloth"))
				keyList.Add(0x012F);
			if (text.Contains("shame"))
				keyList.Add(0x012F);
			if (text.Contains("wrong"))
				keyList.Add(0x012F);
			if (text.Contains("shamino"))
				keyList.Add(0x0130);
			if (text.Contains("knights"))
				keyList.Add(0x0131);
			if (text.Contains("order of the silver serpent"))
				keyList.Add(0x0131);
			if (text.Contains("moons"))
				keyList.Add(0x0132);
			if (text.Contains("compassion"))
				keyList.Add(0x0133);
			if (text.Contains("courage"))
				keyList.Add(0x0133);
			if (text.Contains("honesty"))
				keyList.Add(0x0133);
			if (text.Contains("honor"))
				keyList.Add(0x0133);
			if (text.Contains("humility"))
				keyList.Add(0x0133);
			if (text.Contains("justice"))
				keyList.Add(0x0133);
			if (text.Contains("love"))
				keyList.Add(0x0133);
			if (text.Contains("sacrifice"))
				keyList.Add(0x0133);
			if (text.Contains("shrine"))
				keyList.Add(0x0133);
			if (text.Contains("spirituality"))
				keyList.Add(0x0133);
			if (text.Contains("truth"))
				keyList.Add(0x0133);
			if (text.Contains("valor"))
				keyList.Add(0x0133);
			if (text.Contains("virtue"))
				keyList.Add(0x0133);
			if (text.Contains("gold"))
				keyList.Add(0x0134);
			if (text.Contains("treasure"))
				keyList.Add(0x0134);
			if (text.Contains("kindling"))
				keyList.Add(0x0135);
			if (text.Contains("moongates"))
				keyList.Add(0x0136);
			if (text.Contains("robere"))
				keyList.Add(0x0137);
			if (text.Contains("lord robere"))
				keyList.Add(0x0138);
			if (text.Contains("concerns"))
				keyList.Add(0x0139);
			if (text.Contains("troubles"))
				keyList.Add(0x0139);
			if (text.Contains("avatar"))
				keyList.Add(0x013A);
			if (text.Contains("axe"))
				keyList.Add(0x013B);
			if (text.Contains("hammer"))
				keyList.Add(0x013B);
			if (text.Contains("mace"))
				keyList.Add(0x013B);
			if (text.Contains("sword"))
				keyList.Add(0x013B);
			if (text.Contains("weapon"))
				keyList.Add(0x013B);
			if (text.Contains("city hall"))
				keyList.Add(0x013C);
			if (text.Contains("town hall"))
				keyList.Add(0x013C);
			if (text.Contains("house of parliment"))
				keyList.Add(0x013D);
			if (text.Contains("town square"))
				keyList.Add(0x013E);
			if (text.Contains("new magincia"))
				keyList.Add(0x013F);
			if (text.Contains("merchant guild"))
				keyList.Add(0x0140);
			if (text.Contains("miner guild"))
				keyList.Add(0x0141);
			if (text.Contains("pirate"))
				keyList.Add(0x0142);
			if (text.Contains("zoo"))
				keyList.Add(0x0143);
			if (text.Contains("lycaeum"))
				keyList.Add(0x0144);
			if (text.Contains("scholar"))
				keyList.Add(0x0145);
			if (text.Contains("reagent"))
				keyList.Add(0x0146);
			if (text.Contains("teleporter"))
				keyList.Add(0x0147);
			if (text.Contains("gadget"))
				keyList.Add(0x0148);
			if (text.Contains("silver bow"))
				keyList.Add(0x0149);
			if (text.Contains("mystical spirit"))
				keyList.Add(0x014A);
			if (text.Contains("engineers guild"))
				keyList.Add(0x014B);
			if (text.Contains("sea guild"))
				keyList.Add(0x014C);
			if (text.Contains("vendor sell"))
				keyList.Add(0x014D);
			if (text.Contains("i wish to cross"))
				keyList.Add(0x014E);
			if (text.Contains("where is the miner"))
				keyList.Add(0x014F);
			if (text.Contains("where is the woodsman"))
				keyList.Add(0x0150);
			if (text.Contains("where is the mapmaker"))
				keyList.Add(0x0151);
			if (text.Contains("where is the thief"))
				keyList.Add(0x0152);
			if (text.Contains("where is the tailor"))
				keyList.Add(0x0153);
			if (text.Contains("train anatomy"))
				keyList.Add(0x0154);
			if (text.Contains("come"))
				keyList.Add(0x0155);
			if (text.Contains("drop"))
				keyList.Add(0x0156);
			if (text.Contains("fetch"))
				keyList.Add(0x0157);
			if (text.Contains("get"))
				keyList.Add(0x0158);
			if (text.Contains("bring"))
				keyList.Add(0x0159);
			if (text.Contains("follow"))
				keyList.Add(0x015A);
			if (text.Contains("friend"))
				keyList.Add(0x015B);
			if (text.Contains("guard"))
				keyList.Add(0x015C);
			if (text.Contains("kill"))
				keyList.Add(0x015D);
			if (text.Contains("attack"))
				keyList.Add(0x015E);
			if (text.Contains("patrol"))
				keyList.Add(0x015F);
			if (text.Contains("report"))
				keyList.Add(0x0160);
			if (text.Contains("stop"))
				keyList.Add(0x0161);
			if (text.Contains("hire"))
				keyList.Add(0x0162);
			if (text.Contains("follow me"))
				keyList.Add(0x0163);
			if (text.Contains("all come"))
				keyList.Add(0x0164);
			if (text.Contains("all follow"))
				keyList.Add(0x0165);
			if (text.Contains("all guard"))
				keyList.Add(0x0166);
			if (text.Contains("all stop"))
				keyList.Add(0x0167);
			if (text.Contains("all kill"))
				keyList.Add(0x0168);
			if (text.Contains("all attack"))
				keyList.Add(0x0169);
			if (text.Contains("all report"))
				keyList.Add(0x016A);
			if (text.Contains("all guard me"))
				keyList.Add(0x016B);
			if (text.Contains("all follow me"))
				keyList.Add(0x016C);
			if (text.Contains("release"))
				keyList.Add(0x016D);
			if (text.Contains("transfer"))
				keyList.Add(0x016E);
			if (text.Contains("stay"))
				keyList.Add(0x016F);
			if (text.Contains("all stay"))
				keyList.Add(0x0170);
			if (text.Contains("buy"))
				keyList.Add(0x0171);
			if (text.Contains("browse"))
				keyList.Add(0x0172);
			if (text.Contains("look"))
				keyList.Add(0x0172);
			if (text.Contains("view"))
				keyList.Add(0x0172);
			if (text.Contains("collect"))
				keyList.Add(0x0173);
			if (text.Contains("info"))
				keyList.Add(0x0174);
			if (text.Contains("status"))
				keyList.Add(0x0174);
			if (text.Contains("dismiss"))
				keyList.Add(0x0175);
			if (text.Contains("replace"))
				keyList.Add(0x0175);
			if (text.Contains("cycle"))
				keyList.Add(0x0176);
			if (text.Contains("sell"))
				keyList.Add(0x0177);
			if (text.Contains("i honor your leadership"))
				keyList.Add(0x0178);
			keywords = keyList.ToArray();
			//from.DoSpeech( text, m_EmptyInts, type, Utility.ClipDyedHue( hue ) );
			from.DoSpeech(text, keywords, type, Utility.ClipDyedHue(hue));
		}

		private static KeywordList m_KeywordList = new KeywordList();
		
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

			text = text.Trim();

			if ( text.Length <= 0 || text.Length > 128 )
				return;

			type &= ~MessageType.Encoded;

			if ( !Enum.IsDefined( typeof( MessageType ), type ) )
				type = MessageType.Regular;

			from.Language = lang;
			from.DoSpeech( text, keywords, type, Utility.ClipDyedHue( hue ) );
		}

		public static void UseReq( NetState state, PacketReader pvSrc )
		{
			Mobile from = state.Mobile;

			if ( from.AccessLevel >= AccessLevel.Counselor || DateTime.Now >= from.NextActionTime )
			{
				int value = pvSrc.ReadInt32();

				if ( (value & ~0x7FFFFFFF) != 0 )
				{
					from.OnPaperdollRequest();
				}
				else
				{
					Serial s = value;

					if ( s.IsMobile )
					{
						Mobile m = World.FindMobile( s );

						if ( m != null && !m.Deleted )
							from.Use( m );
					}
					else if ( s.IsItem )
					{
						Item item = World.FindItem( s );

						if ( item != null && !item.Deleted )
							from.Use( item );
					}
				}

				from.NextActionTime = DateTime.Now + TimeSpan.FromSeconds( 0.5 );
			}
			else
			{
				from.SendActionMessage();
			}
		}

		private static bool m_SingleClickProps;

		public static bool SingleClickProps
		{
			get{ return m_SingleClickProps; }
			set{ m_SingleClickProps = value; }
		}

		public static void LookReq( NetState state, PacketReader pvSrc )
		{
			Mobile from = state.Mobile;

			Serial s = pvSrc.ReadInt32();

			if ( s.IsMobile )
			{
				Mobile m = World.FindMobile( s );

				if ( m != null && from.CanSee( m ) && Utility.InUpdateRange( from, m ) )
				{
					if ( m_SingleClickProps )
					{
						m.OnAosSingleClick( from );
					}
					else
					{
						if ( from.Region.OnSingleClick( from, m ) )
							m.OnSingleClick( from );
					}
				}
			}
			else if ( s.IsItem )
			{
				Item item = World.FindItem( s );

				if ( item != null && !item.Deleted && from.CanSee( item ) && Utility.InUpdateRange( from.Location, item.GetWorldLocation() ) )
				{
					if ( m_SingleClickProps )
					{
						item.OnAosSingleClick( from );
					}
					else if ( from.Region.OnSingleClick( from, item ) )
					{
						if ( item.Parent is Item )
							((Item)item.Parent).OnSingleClickContained( from, item );

						item.OnSingleClick( from );
					}
				}
			}
		}

		public static void PingReq( NetState state, PacketReader pvSrc )
		{
			state.Send( PingAck.Instantiate( pvSrc.ReadByte() ) );
		}

		public static void SetUpdateRange( NetState state, PacketReader pvSrc )
		{
			state.Send( ChangeUpdateRange.Instantiate( 18 ) );
		}

		private const int BadFood = unchecked( (int)0xBAADF00D );
		private const int BadUOTD = unchecked( (int)0xFFCEFFCE );

		public static void MovementReq( NetState state, PacketReader pvSrc )
		{
			Direction dir = (Direction)pvSrc.ReadByte();
			int seq = pvSrc.ReadByte();
			int key = pvSrc.ReadInt32();

			Mobile m = state.Mobile;

			if ( (state.Sequence == 0 && seq != 0) || !m.Move( dir ) )
			{
				state.Send( new MovementRej( seq, m ) );
				state.Sequence = 0;

				m.ClearFastwalkStack();
			}
			else
			{
				++seq;

				if ( seq == 256 )
					seq = 1;

				state.Sequence = seq;
			}
		}


        public static void MovementReq_1_25_35(NetState state, PacketReader pvSrc)
        {
            Direction dir = (Direction)pvSrc.ReadByte();
            int seq = pvSrc.ReadByte();
			Console.WriteLine("Got dir {0} and seq {1}", dir.ToString(), seq.ToString());
            //int key = pvSrc.ReadInt32();

            Mobile m = state.Mobile;

            if ( (state.Sequence == 0 && seq != 0) ||  !m.Move(dir))
            {
                state.Send(new MovementRej(seq, m));
                state.Sequence = 0;

                m.ClearFastwalkStack();
            }
            else
            {
                ++seq;

                if (seq == 256)
                    seq = 1;

                state.Sequence = seq;
				//state.Send(MovementAck.Instantiate(seq, m));
            }
        }

        public static int[] m_ValidAnimations = new int[]
			{
				6, 21, 32, 33,
				100, 101, 102,
				103, 104, 105,
				106, 107, 108,
				109, 110, 111,
				112, 113, 114,
				115, 116, 117,
				118, 119, 120,
				121, 123, 124,
				125, 126, 127,
				128
			};

		public static int[] ValidAnimations{ get{ return m_ValidAnimations; } set{ m_ValidAnimations = value; } }

		public static void Animate( NetState state, PacketReader pvSrc )
		{
			Mobile from = state.Mobile;
			int action = pvSrc.ReadInt32();

			bool ok = false;

			for ( int i = 0; !ok && i < m_ValidAnimations.Length; ++i )
				ok = ( action == m_ValidAnimations[i] );

			if ( from != null && ok && from.Alive && from.Body.IsHuman && !from.Mounted )
				from.Animate( action, 7, 1, true, false, 0 );
		}

		public static void QuestArrow( NetState state, PacketReader pvSrc )
		{
			bool rightClick = pvSrc.ReadBoolean();
			Mobile from = state.Mobile;

			if ( from != null && from.QuestArrow != null )
				from.QuestArrow.OnClick( rightClick );
		}

		public static void ExtendedCommand( NetState state, PacketReader pvSrc )
		{
			int packetID = pvSrc.ReadUInt16();

			PacketHandler ph = GetExtendedHandler( packetID );

			if ( ph != null )
			{
				if ( ph.Ingame && state.Mobile == null )
				{
					Console.WriteLine( "Client: {0}: Sent ingame packet (0xBFx{1:X2}) before having been attached to a mobile", state, packetID );
					state.Dispose();
				}
				else if ( ph.Ingame && state.Mobile.Deleted )
				{
					state.Dispose();
				}
				else
				{
					ph.OnReceive( state, pvSrc );
				}
			}
			else
			{
				pvSrc.Trace( state );
			}
		}

		public static void CastSpell( NetState state, PacketReader pvSrc )
		{
			Mobile from = state.Mobile;

			if ( from == null )
				return;

			Item spellbook = null;

			if ( pvSrc.ReadInt16() == 1 )
				spellbook = World.FindItem( pvSrc.ReadInt32() );

			int spellID = pvSrc.ReadInt16() - 1;

			EventSink.InvokeCastSpellRequest( new CastSpellRequestEventArgs( from, spellID, spellbook ) );
		}

		public static void BatchQueryProperties( NetState state, PacketReader pvSrc )
		{
			if ( !ObjectPropertyList.Enabled )
				return;

			Mobile from = state.Mobile;

			int length = pvSrc.Size-3;

			if ( length < 0 || (length%4) != 0 )
				return;

			int count = length/4;

			for ( int i = 0; i < count; ++i )
			{
				Serial s = pvSrc.ReadInt32();

				if ( s.IsMobile )
				{
					Mobile m = World.FindMobile( s );

					if ( m != null && from.CanSee( m ) && Utility.InUpdateRange( from, m ) )
						m.SendPropertiesTo( from );
				}
				else if ( s.IsItem )
				{
					Item item = World.FindItem( s );

					if ( item != null && !item.Deleted && from.CanSee( item ) && Utility.InUpdateRange( from.Location, item.GetWorldLocation() ) )
						item.SendPropertiesTo( from );
				}
			}
		}

		public static void QueryProperties( NetState state, PacketReader pvSrc )
		{
			if ( !ObjectPropertyList.Enabled )
				return;

			Mobile from = state.Mobile;

			Serial s = pvSrc.ReadInt32();

			if ( s.IsMobile )
			{
				Mobile m = World.FindMobile( s );

				if ( m != null && from.CanSee( m ) && Utility.InUpdateRange( from, m ) )
					m.SendPropertiesTo( from );
			}
			else if ( s.IsItem )
			{
				Item item = World.FindItem( s );

				if ( item != null && !item.Deleted && from.CanSee( item ) && Utility.InUpdateRange( from.Location, item.GetWorldLocation() ) )
					item.SendPropertiesTo( from );
			}
		}

		public static void PartyMessage( NetState state, PacketReader pvSrc )
		{
			if ( state.Mobile == null )
				return;

			switch ( pvSrc.ReadByte() )
			{
				case 0x01: PartyMessage_AddMember( state, pvSrc ); break;
				case 0x02: PartyMessage_RemoveMember( state, pvSrc ); break;
				case 0x03: PartyMessage_PrivateMessage( state, pvSrc ); break;
				case 0x04: PartyMessage_PublicMessage( state, pvSrc ); break;
				case 0x06: PartyMessage_SetCanLoot( state, pvSrc ); break;
				case 0x08: PartyMessage_Accept( state, pvSrc ); break;
				case 0x09: PartyMessage_Decline( state, pvSrc ); break;
				default: pvSrc.Trace( state ); break;
			}
		}

		public static void PartyMessage_AddMember( NetState state, PacketReader pvSrc )
		{
			if ( PartyCommands.Handler != null )
				PartyCommands.Handler.OnAdd( state.Mobile );
		}

		public static void PartyMessage_RemoveMember( NetState state, PacketReader pvSrc )
		{
			if ( PartyCommands.Handler != null )
				PartyCommands.Handler.OnRemove( state.Mobile, World.FindMobile( pvSrc.ReadInt32() ) );
		}

		public static void PartyMessage_PrivateMessage( NetState state, PacketReader pvSrc )
		{
			if ( PartyCommands.Handler != null )
				PartyCommands.Handler.OnPrivateMessage( state.Mobile, World.FindMobile( pvSrc.ReadInt32() ), pvSrc.ReadUnicodeStringSafe() );
		}

		public static void PartyMessage_PublicMessage( NetState state, PacketReader pvSrc )
		{
			if ( PartyCommands.Handler != null )
				PartyCommands.Handler.OnPublicMessage( state.Mobile, pvSrc.ReadUnicodeStringSafe() );
		}

		public static void PartyMessage_SetCanLoot( NetState state, PacketReader pvSrc )
		{
			if ( PartyCommands.Handler != null )
				PartyCommands.Handler.OnSetCanLoot( state.Mobile, pvSrc.ReadBoolean() );
		}

		public static void PartyMessage_Accept( NetState state, PacketReader pvSrc )
		{
			if ( PartyCommands.Handler != null )
				PartyCommands.Handler.OnAccept( state.Mobile, World.FindMobile( pvSrc.ReadInt32() ) );
		}

		public static void PartyMessage_Decline( NetState state, PacketReader pvSrc )
		{
			if ( PartyCommands.Handler != null )
				PartyCommands.Handler.OnDecline( state.Mobile, World.FindMobile( pvSrc.ReadInt32() ) );
		}

		public static void StunRequest( NetState state, PacketReader pvSrc )
		{
			EventSink.InvokeStunRequest( new StunRequestEventArgs( state.Mobile ) );
		}

		public static void DisarmRequest( NetState state, PacketReader pvSrc )
		{
			EventSink.InvokeDisarmRequest( new DisarmRequestEventArgs( state.Mobile ) );
		}

		public static void StatLockChange( NetState state, PacketReader pvSrc )
		{
			int stat = pvSrc.ReadByte();
			int lockValue = pvSrc.ReadByte();

			if ( lockValue > 2 ) lockValue = 0;

			Mobile m = state.Mobile;

			if ( m != null )
			{
				switch ( stat )
				{
					case 0: m.StrLock = (StatLockType)lockValue; break;
					case 1: m.DexLock = (StatLockType)lockValue; break;
					case 2: m.IntLock = (StatLockType)lockValue; break;
				}
			}
		}

		public static void ScreenSize( NetState state, PacketReader pvSrc )
		{
			int width = pvSrc.ReadInt32();
			int unk = pvSrc.ReadInt32();
		}

		public static void ContextMenuResponse( NetState state, PacketReader pvSrc )
		{
			Mobile from = state.Mobile;

			if ( from != null )
			{
				ContextMenu menu = from.ContextMenu;

				from.ContextMenu = null;

				if ( menu != null && from != null && from == menu.From )
				{
					IEntity entity = World.FindEntity( pvSrc.ReadInt32() );

					if ( entity != null && entity == menu.Target && from.CanSee( entity ) )
					{
						Point3D p;

						if ( entity is Mobile )
							p = entity.Location;
						else if ( entity is Item )
							p = ((Item)entity).GetWorldLocation();
						else
							return;

						int index = pvSrc.ReadUInt16();

						if ( index >= 0 && index < menu.Entries.Length )
						{
							ContextMenuEntry e = menu.Entries[index];

							int range = e.Range;

							if ( range == -1 )
								range = 18;

							if ( e.Enabled && from.InRange( p, range ) )
								e.OnClick();
						}
					}
				}
			}
		}

		public static void ContextMenuRequest( NetState state, PacketReader pvSrc )
		{
			Mobile from = state.Mobile;
			IEntity target = World.FindEntity( pvSrc.ReadInt32() );

			if ( from != null && target != null && from.Map == target.Map && from.CanSee( target ) )
			{
				if ( target is Mobile && !Utility.InUpdateRange( from.Location, target.Location ) )
					return;
				else if ( target is Item && !Utility.InUpdateRange( from.Location, ((Item)target).GetWorldLocation() ) )
					return;

				if ( !from.CheckContextMenuDisplay( target ) )
					return;

				ContextMenu c = new ContextMenu( from, target );

				if ( c.Entries.Length > 0 )
				{
					if ( target is Item )
					{
						object root = ((Item)target).RootParent;

						if ( root is Mobile && root != from && ((Mobile)root).AccessLevel >= from.AccessLevel )
						{
							for ( int i = 0; i < c.Entries.Length; ++i )
							{
								if ( !c.Entries[i].NonLocalUse )
									c.Entries[i].Enabled = false;
							}
						}
					}

					from.ContextMenu = c;
				}
			}
		}

		public static void CloseStatus( NetState state, PacketReader pvSrc )
		{
			Serial serial = pvSrc.ReadInt32();
		}

		public static void Language( NetState state, PacketReader pvSrc )
		{
			string lang = pvSrc.ReadString( 4 );

			if ( state.Mobile != null )
				state.Mobile.Language = lang;
		}

		public static void AssistVersion( NetState state, PacketReader pvSrc )
		{
			int unk = pvSrc.ReadInt32();
			string av = pvSrc.ReadString();
		}

		public static void ClientVersion( NetState state, PacketReader pvSrc )
		{
			CV version = state.Version = new CV( pvSrc.ReadString() );

			EventSink.InvokeClientVersionReceived( new ClientVersionReceivedArgs( state, version ) );
		}

		public static void ClientType( NetState state, PacketReader pvSrc )
		{
			pvSrc.ReadUInt16();

			int type = pvSrc.ReadUInt16();
			CV version = state.Version = new CV( pvSrc.ReadString() );

			//EventSink.InvokeClientVersionReceived( new ClientVersionReceivedArgs( state, version ) );//todo
		}

		public static void MobileQuery( NetState state, PacketReader pvSrc )
		{
			Mobile from = state.Mobile;

			pvSrc.ReadInt32(); // 0xEDEDEDED
			int type = pvSrc.ReadByte();
			Mobile m = World.FindMobile( pvSrc.ReadInt32() );

			if ( m != null )
			{
				switch ( type )
				{
					case 0x00: // Unknown, sent by godclient
					{
						if ( VerifyGC( state ) )
							Console.WriteLine( "God Client: {0}: Query 0x{1:X2} on {2} '{3}'", state, type, m.Serial, m.Name );

						break;
					}
					case 0x04: // Stats
					{
						m.OnStatsQuery( from );
						break;
					}
					case 0x05:
					{
						m.OnSkillsQuery( from );
						break;
					}
					default:
					{
						pvSrc.Trace( state );
						break;
					}
				}
			}
		}

		private class LoginTimer : Timer
		{
			private NetState m_State;
			private Mobile m_Mobile;

			public LoginTimer( NetState state, Mobile m ) : base( TimeSpan.FromSeconds( 1.0 ), TimeSpan.FromSeconds( 1.0 ) )
			{
				m_State = state;
				m_Mobile = m;
			}

			protected override void OnTick()
			{
				if ( m_State == null )
					Stop();
				if ( m_State.Version != null )
				{
					m_State.BlockAllPackets = false;
					DoLogin( m_State, m_Mobile );
					Stop();
				}
			}
		}

		public static void PlayCharacter( NetState state, PacketReader pvSrc )
		{
			pvSrc.ReadInt32(); // 0xEDEDEDED

			string name = pvSrc.ReadString( 30 );

			pvSrc.Seek( 2, SeekOrigin.Current );
			int flags = pvSrc.ReadInt32();
			pvSrc.Seek( 24, SeekOrigin.Current );

			int charSlot = pvSrc.ReadInt32();
			int clientIP = pvSrc.ReadInt32();

			IAccount a = state.Account;

			if ( a == null || charSlot < 0 || charSlot >= a.Length )
			{
				state.Dispose();
			}
			else
			{
				Mobile m = a[charSlot];

				// Check if anyone is using this account
				for ( int i = 0; i < a.Length; ++i )
				{
					Mobile check = a[i];

					if ( check != null && check.Map != Map.Internal && check != m )
					{
						Console.WriteLine( "Login: {0}: Account in use", state );
						state.Send( new PopupMessage( PMMessage.CharInWorld ) );
						return;
					}
				}

				if ( m == null )
				{
					state.Dispose();
				}
				else
				{
					if ( m.NetState != null )
						m.NetState.Dispose();

					NetState.ProcessDisposedQueue();

					state.Send( new ClientVersionReq() );

					state.BlockAllPackets = true;

					state.Flags = (ClientFlags)flags;

					state.Mobile = m;
					m.NetState = state;

					new LoginTimer( state, m ).Start();
				}
			}
		}

        public static void PlayCharacter_1_25_35(NetState state, PacketReader pvSrc)
        {
            pvSrc.ReadInt32(); // 0xEDEDEDED

            string name = pvSrc.ReadString(30);

            pvSrc.Seek(2, SeekOrigin.Current);
            int flags = pvSrc.ReadInt32();
            pvSrc.Seek(24, SeekOrigin.Current);

            int charSlot = pvSrc.ReadInt32();
            int clientIP = pvSrc.ReadInt32();

            IAccount a = state.Account;

            if (a == null || charSlot < 0 || charSlot >= a.Length)
            {
                state.Dispose();
            }
            else
            {
                Mobile m = a[charSlot];

                // Check if anyone is using this account
                for (int i = 0; i < a.Length; ++i)
                {
                    Mobile check = a[i];

                    if (check != null && check.Map != Map.Internal && check != m)
                    {
                        Console.WriteLine("Login: {0}: Account in use", state);
                        state.Send(new PopupMessage(PMMessage.CharInWorld));
                        return;
                    }
                }

                if (m == null)
                {
                    state.Dispose();
                }
                else
                {
                    if (m.NetState != null)
                        m.NetState.Dispose();

                    NetState.ProcessDisposedQueue();
					
					//1.25.35 Does not support this packet
                    //state.Send(new ClientVersionReq());

                    state.BlockAllPackets = true;

                    state.Flags = (ClientFlags)flags;

                    state.Mobile = m;
                    m.NetState = state;

                    new LoginTimer(state, m).Start();

                    //Just set the client version as if we received it
                    CV version = state.Version = new CV("1.25.35");

                    EventSink.InvokeClientVersionReceived(new ClientVersionReceivedArgs(state, version));
                }
            }
        }

        public static void DoLogin( NetState state, Mobile m )
		{
			state.Send( new LoginConfirm( m ) );

			if ( m.Map != null )
				state.Send( new MapChange( m ) );

			state.Send( new MapPatches() );

			state.Send( SeasonChange.Instantiate( m.GetSeason(), true ) );

			state.Send( SupportedFeatures.Instantiate( state ) );

			state.Sequence = 0;

			if ( state.StygianAbyss ) {
				state.Send( new MobileUpdate( m ) );
				state.Send( new MobileUpdate( m ) );

				m.CheckLightLevels( true );

				state.Send( new MobileUpdate( m ) );

				state.Send( new MobileIncoming( m, m ) );
				//state.Send( new MobileAttributes( m ) );
				state.Send( new MobileStatus( m, m ) );
				state.Send( Server.Network.SetWarMode.Instantiate( m.Warmode ) );

				m.SendEverything();

				state.Send( SupportedFeatures.Instantiate( state ) );
				state.Send( new MobileUpdate( m ) );
				//state.Send( new MobileAttributes( m ) );
				state.Send( new MobileStatus( m, m ) );
				state.Send( Server.Network.SetWarMode.Instantiate( m.Warmode ) );
				state.Send( new MobileIncoming( m, m ) );
			} else {
				state.Send( new MobileUpdateOld( m ) );
				state.Send( new MobileUpdateOld( m ) );

				m.CheckLightLevels( true );

				state.Send( new MobileUpdateOld( m ) );

				state.Send( new MobileIncomingOld( m, m ) );
				//state.Send( new MobileAttributes( m ) );
				state.Send( new MobileStatus( m, m ) );
				state.Send( Server.Network.SetWarMode.Instantiate( m.Warmode ) );

				m.SendEverything();

				state.Send( SupportedFeatures.Instantiate( state ) );
				state.Send( new MobileUpdateOld( m ) );
				//state.Send( new MobileAttributes( m ) );
				state.Send( new MobileStatus( m, m ) );
				state.Send( Server.Network.SetWarMode.Instantiate( m.Warmode ) );
				state.Send( new MobileIncomingOld( m, m ) );
			}

			state.Send( LoginComplete.Instance );
			state.Send( new CurrentTime() );
			state.Send( SeasonChange.Instantiate( m.GetSeason(), true ) );
			state.Send( new MapChange( m ) );

			EventSink.InvokeLogin( new LoginEventArgs( m ) );

			m.ClearFastwalkStack();
		}

		public static void CreateCharacter( NetState state, PacketReader pvSrc )
		{
			Console.WriteLine("New Char Creation");
			int unk1 = pvSrc.ReadInt32();
			int unk2 = pvSrc.ReadInt32();
			int unk3 = pvSrc.ReadByte();
			string name = pvSrc.ReadString( 30 );
			Console.WriteLine("Name: {0}", name);
			pvSrc.Seek( 2, SeekOrigin.Current );
			int flags = pvSrc.ReadInt32();
			Console.WriteLine("Client Flag: (0x{0:X})", flags);
			pvSrc.Seek( 8, SeekOrigin.Current );
			int prof = pvSrc.ReadByte();
			Console.WriteLine("Prof: (0x{0:X})", prof);
			pvSrc.Seek( 15, SeekOrigin.Current );

			//bool female = pvSrc.ReadBoolean();

			int genderRace = pvSrc.ReadByte();
			Console.WriteLine("Sex: (0x{0:X})", genderRace);
			int str = pvSrc.ReadByte();
			Console.WriteLine("Str: {0} - (0x{0:X})", str);
			int dex = pvSrc.ReadByte();
			Console.WriteLine("Dex: {0} - (0x{0:X})", dex);
			int intl= pvSrc.ReadByte();
			Console.WriteLine("Int: {0} - (0x{0:X})", intl);
			int is1 = pvSrc.ReadByte();
			int vs1 = pvSrc.ReadByte();
			int is2 = pvSrc.ReadByte();
			int vs2 = pvSrc.ReadByte();
			int is3 = pvSrc.ReadByte();
			int vs3 = pvSrc.ReadByte();
			int hue = pvSrc.ReadUInt16();
			int hairVal = pvSrc.ReadInt16();
			int hairHue = pvSrc.ReadInt16();
			int hairValf= pvSrc.ReadInt16();
			int hairHuef= pvSrc.ReadInt16();
			pvSrc.ReadByte();
			int cityIndex = pvSrc.ReadByte();
			int charSlot = pvSrc.ReadInt32();
			int clientIP = pvSrc.ReadInt32();
			int shirtHue = pvSrc.ReadInt16();
			int pantsHue = pvSrc.ReadInt16();

			/*
			Pre-7.0.0.0:
			0x00, 0x01 -> Human Male, Human Female
			0x02, 0x03 -> Elf Male, Elf Female

			Post-7.0.0.0:
			0x00, 0x01
			0x02, 0x03 -> Human Male, Human Female
			0x04, 0x05 -> Elf Male, Elf Female
			0x05, 0x06 -> Gargoyle Male, Gargoyle Female
			*/

			bool female = ((genderRace % 2) != 0);

			Race race = null;

			if ( state.StygianAbyss ) {
				byte raceID = (byte)(genderRace < 4 ? 0 : ((genderRace / 2) - 1));
				race = Race.Races[raceID];
			} else {
				race = Race.Races[(byte)(genderRace / 2)];
			}

			if( race == null )
				race = Race.DefaultRace;

			CityInfo[] info = state.CityInfo;
			IAccount a = state.Account;

			if ( info == null || a == null || cityIndex < 0 || cityIndex >= info.Length )
			{
				state.Dispose();
			}
			else
			{
				// Check if anyone is using this account
				for ( int i = 0; i < a.Length; ++i )
				{
					Mobile check = a[i];

					if ( check != null && check.Map != Map.Internal )
					{
						Console.WriteLine( "Login: {0}: Account in use", state );
						state.Send( new PopupMessage( PMMessage.CharInWorld ) );
						return;
					}
				}

				state.Flags = (ClientFlags)flags;

				CharacterCreatedEventArgs args = new CharacterCreatedEventArgs(
					state, a,
					name, female, hue,
					str, dex, intl,
					info[cityIndex],
					new SkillNameValue[3]
					{
						new SkillNameValue( (SkillName)is1, vs1 ),
						new SkillNameValue( (SkillName)is2, vs2 ),
						new SkillNameValue( (SkillName)is3, vs3 ),
					},
					shirtHue, pantsHue,
					hairVal, hairHue,
					hairValf, hairHuef,
					prof,
					race
					);

                //state.Send( new ClientVersionReq() );
                CV version = state.Version = new CV("1.25.35");

                EventSink.InvokeClientVersionReceived(new ClientVersionReceivedArgs(state, version));

                state.BlockAllPackets = true;

				EventSink.InvokeCharacterCreated( args );

				Mobile m = args.Mobile;

				if ( m != null )
				{
					state.Mobile = m;
					m.NetState = state;
					new LoginTimer( state, m ).Start();
				}
				else
				{
					state.BlockAllPackets = false;
					state.Dispose();
				}
			}
		}

		private static bool m_ClientVerification = true;

		public static bool ClientVerification
		{
			get{ return m_ClientVerification; }
			set{ m_ClientVerification = value; }
		}

		internal struct AuthIDPersistence {
			public DateTime Age;
			public ClientVersion Version;

			public AuthIDPersistence( ClientVersion v ) {
				Age = DateTime.Now;
				Version = v;
			}
		}

		private const int m_AuthIDWindowSize = 128;
		private static Dictionary<int, AuthIDPersistence> m_AuthIDWindow = new Dictionary<int, AuthIDPersistence>( m_AuthIDWindowSize );

		private static int GenerateAuthID( NetState state )
		{			
			if ( m_AuthIDWindow.Count == m_AuthIDWindowSize ) {
				int oldestID = 0;
				DateTime oldest = DateTime.MaxValue;

				foreach ( KeyValuePair<int, AuthIDPersistence> kvp in m_AuthIDWindow ) {
					if ( kvp.Value.Age < oldest ) {
						oldestID = kvp.Key;
						oldest = kvp.Value.Age;
					}
				}

				m_AuthIDWindow.Remove( oldestID );
			}
			
			int authID;

			do {
				authID = Utility.Random( 1, int.MaxValue - 1 );

				if ( Utility.RandomBool() )
					authID |= 1<<31;
			} while ( m_AuthIDWindow.ContainsKey( authID ) );

			m_AuthIDWindow[authID] = new AuthIDPersistence( state.Version );
			
			return authID;
		}

		public static void GameLogin( NetState state, PacketReader pvSrc )
		{
			if ( state.SentFirstPacket )
			{
				state.Dispose();
				return;
			}

			state.SentFirstPacket = true;

			int authID = pvSrc.ReadInt32();

			if ( m_AuthIDWindow.ContainsKey( authID ) ) {
				AuthIDPersistence ap = m_AuthIDWindow[authID];
				m_AuthIDWindow.Remove( authID );

				state.Version = ap.Version;
			} else if ( m_ClientVerification ) {
				Console.WriteLine( "Login: {0}: Invalid client detected, disconnecting", state );
				state.Dispose();
				return;
			}
			
			if ( state.m_AuthID != 0 && authID != state.m_AuthID )
			{
				Console.WriteLine( "Login: {0}: Invalid client detected, disconnecting", state );
				state.Dispose();
				return;
			}
			else if ( state.m_AuthID == 0 && authID != state.m_Seed )
			{
				Console.WriteLine( "Login: {0}: Invalid client detected, disconnecting", state );
				state.Dispose();
				return;
			}

			string username = pvSrc.ReadString( 30 );
			string password = pvSrc.ReadString( 30 );

			GameLoginEventArgs e = new GameLoginEventArgs( state, username, password );

			EventSink.InvokeGameLogin( e );

			if ( e.Accepted )
			{
				state.CityInfo = e.CityInfo;
				state.CompressionEnabled = true;

				state.Send( SupportedFeatures.Instantiate( state ) );

				state.Send( new CharacterList( state.Account, state.CityInfo ) );
			}
			else
			{
				state.Dispose();
			}
		}

		public static void PlayServer( NetState state, PacketReader pvSrc )
		{
			int index = pvSrc.ReadInt16();
			ServerInfo[] info = state.ServerInfo;
			IAccount a = state.Account;

			if ( info == null || a == null || index < 0 || index >= info.Length )
			{
				state.Dispose();
			}
			else
			{
				ServerInfo si = info[index];

				state.m_AuthID = PlayServerAck.m_AuthID = GenerateAuthID( state );

				state.SentFirstPacket = false;
				state.Send( new PlayServerAck( si ) );
			}
		}

		public static void LoginServerSeed( NetState state, PacketReader pvSrc )
		{
			state.m_Seed = pvSrc.ReadInt32();
			state.Seeded = true;

			if ( state.m_Seed == 0 )
			{
				Console.WriteLine("Login: {0}: Invalid client detected, disconnecting", state);
				state.Dispose();
				return;
			}

			int clientMaj = pvSrc.ReadInt32();
			int clientMin = pvSrc.ReadInt32();
			int clientRev = pvSrc.ReadInt32();
			int clientPat = pvSrc.ReadInt32();

			state.Version = new ClientVersion( clientMaj, clientMin, clientRev, clientPat );
		}

		public static void AccountLogin( NetState state, PacketReader pvSrc )
		{
			if ( state.SentFirstPacket )
			{
				state.Dispose();
				return;
			}

			state.SentFirstPacket = true;

			string username = pvSrc.ReadString( 30 );
			string password = pvSrc.ReadString( 30 );

			AccountLoginEventArgs e = new AccountLoginEventArgs( state, username, password );

			EventSink.InvokeAccountLogin( e );

			if ( e.Accepted )
				AccountLogin_ReplyAck( state );
			else
				AccountLogin_ReplyRej( state, e.RejectReason );
		}

		public static void AccountLogin_ReplyAck( NetState state )
		{
			ServerListEventArgs e = new ServerListEventArgs( state, state.Account );

			EventSink.InvokeServerList( e );

			if ( e.Rejected )
			{
				state.Account = null;
				state.Send( new AccountLoginRej( ALRReason.BadComm ) );
				state.Dispose();
			}
			else
			{
				ServerInfo[] info = e.Servers.ToArray();

				state.ServerInfo = info;

				state.Send( new AccountLoginAck( info ) );
			}
		}

		public static void AccountLogin_ReplyRej( NetState state, ALRReason reason )
		{
			state.Send( new AccountLoginRej( reason ) );
			state.Dispose();
		}
	}
}