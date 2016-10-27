/*
 * Created by SharpDevelop.
 * User: suloku
 * Date: 18/10/2015
 * Time: 9:17
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace XYORAS_Safari_Mirage_Tool
{
	/// <summary>
	/// Description of MainForm.
	/// </summary>
	public partial class MainForm : Form
	{
		public MainForm()
		{
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();
			this.Size = new Size(755, 270);
			
			//
			// TODO: Add constructor code after the InitializeComponent() call.
			//
		}
		int game = 0; //1 XY, 2 ORAS
		string linkfile;
		byte[] savebuffer_XY = new byte[0x65600];
		byte[] savebuffer_ORAS = new byte[0x76000];
		byte[] linkbuffer = new byte[2631];

		//adapted from Gocario's PHBank (www.github.com/gocario/phbank)
		public static byte[] ccitt16(byte[] data)
		// --------------------------------------------------
		{
			int len = data.Length;
			UInt16 crc = 0xFFFF;
		
			for (UInt32 i = 0; i < len; i++)
			{
				crc ^= ((UInt16)((data[i] << 8)&0x0000FFFF));
		
				for (UInt32 j = 0; j < 0x8; j++)
				{
					if ((crc & 0x8000) > 0)
						crc = (UInt16)(((UInt16)((crc << 1)&0x0000FFFF ) ^ 0x1021) &0x0000FFFF);
					else
						crc <<= 1;
				}
			}
		
			return BitConverter.GetBytes(crc);
		}

		/// <summary>
		/// Reads data into a complete array, throwing an EndOfStreamException
		/// if the stream runs out of data first, or if an IOException
		/// naturally occurs.
		/// </summary>
		/// <param name="stream">The stream to read data from</param>
		/// <param name="data">The array to read bytes into. The array
		/// will be completely filled from the stream, so an appropriate
		/// size must be given.</param>
		public static void ReadWholeArray (Stream stream, byte[] data)
		{
		    int offset=0;
		    int remaining = data.Length;
		    while (remaining > 0)
		    {
		        int read = stream.Read(data, offset, remaining);
		        if (read <= 0)
		            throw new EndOfStreamException 
		                (String.Format("End of stream reached with {0} bytes left to read", remaining));
		        remaining -= read;
		        offset += read;
		    }
		}
		int[] mirage_slots = new int[10];
		int pss_slot;
		private void Read_data()
		{
	            System.IO.FileStream saveFile;
	            saveFile = new FileStream(savegamename.Text, FileMode.Open);
	            if (saveFile.Length != 0x65600 && saveFile.Length != 0x76000 ){
	            	savegamename.Text = "";
	            	MessageBox.Show("Invalid file length", "Error");
	            	return;
	            }
	            if (saveFile.Length == 0x65600){
	            	game = 1;
	            	currgame.Text = "X/Y";
		            ReadWholeArray(saveFile, savebuffer_XY);
		            saveFile.Close();
		            unlock_safari.Enabled = true;
		            orasbox.Enabled = false;
	            }else if (saveFile.Length == 0x76000){
	            	game = 2;
	            	currgame.Text = "OR/AS";
		            ReadWholeArray(saveFile, savebuffer_ORAS);
		            //Populate ORAS fields
		            tid_lo.Value = savebuffer_ORAS[0x14000];
					tid_hi.Value = savebuffer_ORAS[0x14000+1];
					update_tid();
					mdv0.Value = savebuffer_ORAS[0x1600];
					mdv1.Value = savebuffer_ORAS[0x1601];
					mdv2.Value = savebuffer_ORAS[0x1602];
					mdv3.Value = savebuffer_ORAS[0x1603];
					update_mdv();
					pss_slot = 0;
					int i = 0;
					for(i=0;i<10;i++)
					{
						mirage_slots[i]=savebuffer_ORAS[0x307D4+(i*4)];
					}
					update_mirages();
					savebuffer_ORAS.Skip(0x20FFF).Take(0xA47).ToArray().CopyTo(linkbuffer, 0);
		            saveFile.Close();
		            unlock_safari.Enabled = false;
		            orasbox.Enabled = true;
	            }
		}
		private void Get_save_data()
        {
            OpenFileDialog openFD = new OpenFileDialog();
            //openFD.InitialDirectory = "c:\\";
            openFD.Filter = "VI gen save data|main|All Files (*.*)|*.*";
            if (openFD.ShowDialog() == DialogResult.OK)
            {
                #region filename
                savegamename.Text = openFD.FileName;
                #endregion
                Read_data();

            }
            
        }
		private void Save_data()
		{	if (savegamename.Text.Length < 1) return;
            SaveFileDialog saveFD = new SaveFileDialog();
            //saveFD.InitialDirectory = "c:\\";
            saveFD.Filter = "VI gen save data|main|All Files (*.*)|*.*";
            if (saveFD.ShowDialog() == DialogResult.OK)
            {
	            System.IO.FileStream saveFile;
	            saveFile = new FileStream(saveFD.FileName, FileMode.Create);            
	            //Write file
	            if (game == 1){
	            	saveFile.Write(savebuffer_XY, 0, savebuffer_XY.Length);
	            }else if (game == 2){
	            	saveFile.Write(savebuffer_ORAS, 0, savebuffer_ORAS.Length);
	            }
	            saveFile.Close();
	            MessageBox.Show("File Saved.", "Save file");
            }
		}
		private void Dump_link_data()
		{	
			if (savegamename.Text.Length < 1) return;
            SaveFileDialog saveFD = new SaveFileDialog();
            //saveFD.InitialDirectory = "c:\\";
            saveFD.Filter = "Pokémon Link Data|*.bin|All Files (*.*)|*.*";
            if (saveFD.ShowDialog() == DialogResult.OK)
            {
	            System.IO.FileStream saveFile;
	            saveFile = new FileStream(saveFD.FileName, FileMode.Create);            
	            //Write file
	            if (game == 1){
	            	saveFile.Write(savebuffer_XY, 0x1FFFF, 0xA47);
	            }else if (game == 2) {
	            	saveFile.Write(savebuffer_ORAS, 0x20FFF, 0xA47);
	            }
	            
	            saveFile.Close();
	            MessageBox.Show("Pokémon Link data dumped to:\r"+saveFD.FileName+".");
            }
		}
		private void Read_link_data()
		{
	            System.IO.FileStream saveFile;
	            saveFile = new FileStream(linkfile, FileMode.Open);
	            if (saveFile.Length != 0xA47){
	            	//linkfile = "";
	            	MessageBox.Show("Invalid file length", "Error");
	            	return;
	            }
	            ReadWholeArray(saveFile, linkbuffer);
	            saveFile.Close();
	            InjectNsave();
		}
		private void Get_link_data()
        {
            OpenFileDialog openFD = new OpenFileDialog();
            //openFD.InitialDirectory = "c:\\";
            openFD.Filter = "Pokémon Link Data|*.bin|All Files (*.*)|*.*";
            if (openFD.ShowDialog() == DialogResult.OK)
            {
                #region filename
                linkfile = openFD.FileName;
                #endregion
                Read_link_data();
            }
            
        }
		private void InjectNsave()
		{

			if (game == 1){
				//Unlock safaris
				  int i;
				  for (i=1;i<101;i++)
				  {
				    if( savebuffer_XY[0x1E7FF+(0x15*i)] != 0x00 )
				        savebuffer_XY[0x1E7FF+(0x15*i)] = 0x3D;
				  }
				//Get full block for checksum calculation
				byte[] friendblockbuffer = new byte[0x834];
				Array.Copy(savebuffer_XY, 0x1E800, friendblockbuffer, 0, 0x834);
				byte[] tablecrcsum = new byte[2];
				tablecrcsum = ccitt16(friendblockbuffer);
				//Put new checksum in savefile
				Array.Copy(tablecrcsum, 0, savebuffer_XY, 0x65582, 2);
			}
			else if (game == 2){
				//Put link data in save
				Array.Copy(linkbuffer, 0, savebuffer_ORAS, 0x20FFF, 0xA47);
				
				//Get full block for checksum calculation
					savebuffer_ORAS[0x14000] = (byte) tid_lo.Value;
					savebuffer_ORAS[0x14000+1] = (byte) tid_hi.Value;
				
					byte[] tidblockbuffer = new byte[0x170];
					Array.Copy(savebuffer_ORAS, 0x14000, tidblockbuffer, 0, 0x170);
					byte[] tablecrcsum = new byte[2];
					tablecrcsum = ccitt16(tidblockbuffer);
					//Put new checksum in savefile
					Array.Copy(tablecrcsum, 0, savebuffer_ORAS, 0x75EA2, 2);
				
				//MDV
					savebuffer_ORAS[0x1600] = (byte) mdv0.Value;
					savebuffer_ORAS[0x1600+1] = (byte) mdv1.Value;
					savebuffer_ORAS[0x1600+2] = (byte) mdv2.Value;
					savebuffer_ORAS[0x1600+3] = (byte) mdv3.Value;
				
					byte[] mdvblockbuffer = new byte[0x4];
					Array.Copy(savebuffer_ORAS, 0x1600, mdvblockbuffer, 0, 0x4);
					tablecrcsum = ccitt16(mdvblockbuffer);
					//Put new checksum in savefile
					Array.Copy(tablecrcsum, 0, savebuffer_ORAS, 0x75E42, 2);
				
				//PSS
					int i = 0;
					for(i=0;i<10;i++)
					{
						savebuffer_ORAS[0x307D4+(i*4)] = (byte)mirage_slots[i];
					}
				
					byte[] pssblockbuffer = new byte[0x78B0];
					Array.Copy(savebuffer_ORAS, 0x2B600, pssblockbuffer, 0, 0x78B0);
					tablecrcsum = ccitt16(pssblockbuffer);
					//Put new checksum in savefile
					Array.Copy(tablecrcsum, 0, savebuffer_ORAS, 0x75FD2, 2);
			}
			//Write Data
			Save_data();
		}
		void LoadsaveClick(object sender, EventArgs e)
		{
			Get_save_data();
		}
		void Dump_butClick(object sender, EventArgs e)
		{
			if (savegamename.Text.Length < 1) return;
			Dump_link_data();
		}
		void SavegamenameTextChanged(object sender, EventArgs e)
		{
			if (savegamename.Text.Length > 0){
				if (game == 1)
					unlock_safari.Enabled = true;
			}else{
				unlock_safari.Enabled = false;
			}

		}
		void Inject_butClick(object sender, EventArgs e)
		{
			Get_link_data();
		}
		void Mdv_advancedClick(object sender, EventArgs e)
		{
			mdv_box.Visible ^= true;
			if (mdv_box.Visible != true)
				this.Size = new Size(755, 270);
			else
				this.Size = new Size(755, 394);
		}
		void GroupBox1Enter(object sender, EventArgs e)
		{
	
		}
		void InfobutClick(object sender, EventArgs e)
		{
			panel_info.Visible = true;
			this.Size = new Size(755, 532);
		}
		void Oras_saveClick(object sender, EventArgs e)
		{
			
			InjectNsave();
		}
		void Unlock_safariClick(object sender, EventArgs e)
		{
			
			InjectNsave();
		}
		void update_tid()
		{
			u16.Text = ((UInt16)((UInt16)tid_lo.Value|((UInt16)tid_hi.Value<<8))).ToString("00000")+"   (u16)\n0x"+ ((UInt16)((UInt16)tid_lo.Value|((UInt16)tid_hi.Value<<8))).ToString("X4")+"  (hex) ";
		}
		void Tid_hiValueChanged(object sender, EventArgs e)
		{
			update_tid();
		}
		void Tid_loValueChanged(object sender, EventArgs e)
		{
			update_tid();
		}
		void update_mdv()
		{
			u32.Text = ((UInt32)((UInt32)mdv0.Value|((UInt32)mdv1.Value<<8)|((UInt32)mdv2.Value<<16)|((UInt32)mdv3.Value<<24))).ToString("0000000000")+"  (u32)\n0x"+ ((UInt32)((UInt32)mdv0.Value|((UInt32)mdv1.Value<<8)|((UInt32)mdv2.Value<<16)|((UInt32)mdv3.Value<<24))).ToString("X8")+"  (hex) ";
		}
		void Mdv0ValueChanged(object sender, EventArgs e)
		{
			update_mdv();
		}
		void Mdv1ValueChanged(object sender, EventArgs e)
		{
			update_mdv();
		}
		void Mdv2ValueChanged(object sender, EventArgs e)
		{
			update_mdv();
		}
		void Mdv3ValueChanged(object sender, EventArgs e)
		{
			update_mdv();
		}
		
		void update_mirages()
		{
			spot0.Text=mirage_slots[0].ToString("00");
			spot1.Text=mirage_slots[1].ToString("00");
			spot2.Text=mirage_slots[2].ToString("00");
			spot3.Text=mirage_slots[3].ToString("00");
			spot4.Text=mirage_slots[4].ToString("00");
			spot5.Text=mirage_slots[5].ToString("00");
			spot6.Text=mirage_slots[6].ToString("00");
			spot7.Text=mirage_slots[7].ToString("00");
			spot8.Text=mirage_slots[8].ToString("00");
			spot9.Text=mirage_slots[9].ToString("00");
			comboBox2.SelectedIndex = mirage_slots[(int)pss_slot];
		}
		void ComboBox2SelectedIndexChanged(object sender, EventArgs e)
		{
			mirage_slots[(int)pss_slot]=comboBox2.SelectedIndex;
			update_mirages();
		}

		void Close_infoClick(object sender, EventArgs e)
		{
			panel_info.Visible = false;
			if (mdv_box.Visible != true)
				this.Size = new Size(755, 270);
			else
				this.Size = new Size(755, 394);
		}
		void update_radio()
		{
			if(but0.Checked == true)
				pss_slot=0;
			else if (but1.Checked == true)
				pss_slot=1;
			else if (but2.Checked == true)
				pss_slot=2;
			else if (but3.Checked == true)
				pss_slot=3;
			else if (but4.Checked == true)
				pss_slot=4;
			else if (but5.Checked == true)
				pss_slot=5;
			else if (but6.Checked == true)
				pss_slot=6;
			else if (but7.Checked == true)
				pss_slot=7;
			else if (but8.Checked == true)
				pss_slot=8;
			else if (but9.Checked == true)
				pss_slot=9;
			else
				pss_slot=0;
			
			comboBox2.SelectedIndex = mirage_slots[(int)pss_slot];
		}
		void But0CheckedChanged(object sender, EventArgs e)
		{
			update_radio();
		}
		void But1CheckedChanged(object sender, EventArgs e)
		{
			update_radio();
		}
		void But2CheckedChanged(object sender, EventArgs e)
		{
			update_radio();
		}
		void But3CheckedChanged(object sender, EventArgs e)
		{
			update_radio();
		}
		void But4CheckedChanged(object sender, EventArgs e)
		{
			update_radio();
		}
		void But5CheckedChanged(object sender, EventArgs e)
		{
			update_radio();
		}
		void But6CheckedChanged(object sender, EventArgs e)
		{
			update_radio();
		}
		void But7CheckedChanged(object sender, EventArgs e)
		{
			update_radio();
		}
		void But8CheckedChanged(object sender, EventArgs e)
		{
			update_radio();
		}
		void But9CheckedChanged(object sender, EventArgs e)
		{
			update_radio();
		}


	}
}
