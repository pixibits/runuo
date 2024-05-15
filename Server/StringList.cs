using System;
using System.IO;
using System.Text;
using System.Collections;

namespace Ultima
{
	public class StringList
	{
		private Hashtable m_Table;
		private StringEntry[] m_Entries;
		private string m_Language;

		public StringEntry[] Entries{ get{ return m_Entries; } }
		public Hashtable Table{ get{ return m_Table; } }
		public string Language{ get{ return m_Language; } }

		private static byte[] m_Buffer = new byte[1024];

		public string Format( int num, params object[] args )
		{
			for(int i=0;i<m_Entries.Length;i++)
			{
				if ( m_Entries[i].Number == num )
					return m_Entries[i].Format( args );
			}

			return String.Format( "CliLoc string {0} not found!", num );
		}

		public string SplitFormat( int num, string argstr )
		{
			for(int i=0;i<m_Entries.Length;i++)
			{
				if ( m_Entries[i].Number == num )
					return m_Entries[i].SplitFormat( argstr );
			}

			return String.Format( "CliLoc string {0} not found!", num );
		}

		public StringList( string language )
		{
			m_Language = language;
			m_Table = new Hashtable();


            string path = Server.Core.FindDataFile(String.Format( "cliloc.{0}", language));

			if ( path == null || !File.Exists( path ) )
			{
                Console.WriteLine("ERROR: Failed to find 'cliloc.{0}' datafile", language);
				m_Entries = new StringEntry[0];
				return;
			}

			ArrayList list = new ArrayList();

			using ( BinaryReader bin = new BinaryReader( new FileStream( path, FileMode.Open, FileAccess.Read, FileShare.Read ), Encoding.UTF8 ) )
			{
				bin.ReadInt32();
				bin.ReadInt16();

				try
				{
					while ( true )
					{
						int number = bin.ReadInt32();
						bin.ReadByte();
						int length = bin.ReadInt16();

						if ( length > m_Buffer.Length )
							m_Buffer = new byte[(length + 1023) & ~1023];

						bin.Read( m_Buffer, 0, length );

						try
						{
							string text = Encoding.UTF8.GetString( m_Buffer, 0, length );

							list.Add( new StringEntry( number, text ) );
							m_Table[number] = text;
						}
						catch
						{
						}
					}
				}
				catch ( System.IO.EndOfStreamException )
				{
					// end of file.  stupid C#.
				}
			}

			m_Entries = (StringEntry[])list.ToArray( typeof( StringEntry ) );
		}
	}
}
