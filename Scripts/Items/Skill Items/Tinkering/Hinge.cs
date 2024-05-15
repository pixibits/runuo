using System;
using Server;

namespace Server.Items
{
	[Flipable( 0x1055, 0x1056 )]
	public class Hinge : BaseItem
	{
		[Constructable]
		public Hinge() : this( 1 )
		{
		}

		[Constructable]
		public Hinge( int amount ) : base( 0x1055 )
		{
			Stackable = true;
			Amount = amount;
			Weight = 1.0;
		}

		public Hinge( Serial serial ) : base( serial )
		{
		}

		public override Item Dupe( int amount )
		{
			return base.Dupe( new Hinge(), amount );
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