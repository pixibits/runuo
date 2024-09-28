using System;
using System.Collections;
using System.Collections.Generic;
using Server;
using Server.Gumps;
using Server.Network;
using Server.Mobiles;
using Server.Items;
using Server.ContextMenus;

namespace Server.Gumps
{
	public class OldResurrectGump
	{
		public static void Configure()
		{			
			PacketHandlers.Register(0x2C, 2, true, new OnPacketReceive(DeathStatusResponse));
		}

		private static void DeathStatusResponse(NetState ns, PacketReader pvSrc)
		{
			Mobile from = ns.Mobile;
			PlayerMobile pm = from as PlayerMobile;

			int action = pvSrc.ReadByte();
			if (action == 1)
			{
				if (pm.Location != pm.DeathLocation)
				{
					pm.SendAsciiMessage("Thou hast wandered too far from the site of thy resurrection!");
					return;
				}
				else if (pm.SpiritCohesion <= 0)
				{
					pm.SendAsciiMessage("Your spirit was too weak to return to corporeal form.");
					return;
				}

				from.PlaySound(0x214);
				from.FixedEffect(0x376A, 10, 16);
				pm.Resurrect();

				pm.SpiritCohesion--;
				switch (pm.SpiritCohesion)
				{
					case 0:
						pm.SendAsciiMessage("Your spirit returns to corporeal form, but is too weak to do so a gain for a while.");
						break;
					case 1:
						pm.SendAsciiMessage("Your spirit barely manages to return to corporeal form.");
						break;
					case 2:
						pm.SendAsciiMessage("With some effort your spirit returns to corporeal form.");
						break;
					case 3:
					default:
						pm.SendAsciiMessage("Your spirit easily returns to corporeal form.");
						break;
				}

				for (int i = 0; i < pm.Skills.Length; i++)
				{
					if (pm.Skills[i].Base > 25.0)
						pm.Skills[i].Base -= Utility.Random(5) + 5;
				}

				if (pm.RawDex > 15)
					pm.RawDex -= pm.RawDex / 15;
				if (pm.RawStr > 15)
					pm.RawStr -= pm.RawStr / 15;
				if (pm.RawInt > 15)
					pm.RawInt -= pm.RawInt / 15;

				pm.Hits = pm.HitsMax / 2;
				pm.Mana = pm.ManaMax / 5;

				from.Send(new MobileStatusExtended(from, ns));
				from.Send(new MobileHits(from));
				from.Send(new MobileMana(from));
				from.Send(new MobileStam(from));
			}
			
		}

		private class ResChoice : Packet
		{
			public ResChoice() : base(0x2C)
			{

				EnsureCapacity(2);

				m_Stream.Write((int)1); // action

			}
		}
	}
}
