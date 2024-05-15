using System;
using Server;
using Server.Engines.Craft;

namespace Server.Items
{
	[Flipable( 0x1032, 0x1033 )]
	public class SmoothingPlane : BaseTool
	{
		public override CraftSystem GetCraftInstance()
		{
			return new CarpentrySystem();
		}

		[Constructable]
		public SmoothingPlane() : base( 0x1032 )
		{
			Weight = 1.0;
		}

		[Constructable]
		public SmoothingPlane( int uses ) : base( uses, 0x1032 )
		{
			Weight = 1.0;
		}

		public SmoothingPlane( Serial serial ) : base( serial )
		{
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