using MidiSharp;
using MidiSharp.Events;
using MidiSharp.Events.Voice;
using MidiSharp.Events.Voice.Note;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


/* Todo : 
 * Supprimer les notes parasites pour low-mid tom et low-tom (crash et china)
 * Faire une interface pour modifier les notes
 * Ne pas demaner ou sauvegarder pour gagner du temps
 * Garder le bouton load
 * 
 * Résumé : Bouton load ; Interface sélection notes ; Bouton exécuter
 * 
 * 
 */

namespace MidiScriptEditor
{
	public partial class frmMain : Form
	{
		private string path = string.Empty;
		public static Dictionary<byte, Notes> Config;
		public Dictionary<byte, string> Comments;
		private bool pauseCheckEdit = true;

		public enum Notes
		{
			Undefined = 0,

			Kick = 36,
			BassDrum = 35,
			Rimshot = 37,
			SideStick = 37,
			Snare = 38,
			LowFloorTom = 41,
			ClosedHiHat = 42,
			HighFloorTom = 43,
			PedalHiHat = 44,
			LowTom = 45,
			OpenHiHat = 46,
			LowMidTom = 47,
			HighMidTom = 48,
			Crash = 49,
			HighTom = 50,
			Ride = 51,
			China = 52,
			RideBell = 53,
			Tambourine = 54,
			Splash = 55,
			Cowbell = 56,
		}

		public static byte GetNote(byte note)
		{
			if (Config == null) PopulateConfig();
			if (Config.ContainsKey(note))
			{
				Notes newNote = Config[note];
				if (Enum.IsDefined(typeof(Notes), newNote) && newNote != Notes.Undefined)
					return (byte)newNote;
			}
			return note;
		}

		private static void PopulateConfig()
		{
			Config = new Dictionary<byte, Notes>()
			{
				{96, Notes.Kick }, // Orange Long Note - Kick
				{97, Notes.Snare }, // Red Pad Lane 1 - Normal Snare/Ghost Note Snare
				{98, Notes.Crash }, // Yellow Cymbal Lane 2 - Crash/Splash/Bell
				{99, Notes.China }, // Blue Cymbal Lane 4 - China/Ride/Bell/Crash/Splash
				{100, Notes.Ride }, // Green Cymbal Lane 6 - Crash/China/Splash/Bell
				{110, Notes.LowMidTom }, // Yellow Pad Lane 3 - Tom 1
				{111, Notes.LowTom }, // Blue Pad Lane 5 - Tom 2
				{112, Notes.LowFloorTom }, // Green Pad Lane 7 - Tom 3
			};
		}


		private void InitConfig()
		{
			PopulateConfig();
			Comments = new Dictionary<byte, string>()
			{
				{96, "Orange Long Note - Kick"},
				{97, "Red Pad Lane 1 - Normal Snare/Ghost Note Snare"},
				{98, "Yellow Cymbal Lane 2 - Crash/Splash/Bell"},
				{99, "Blue Cymbal Lane 4 - China/Ride/Bell/Crash/Splash"},
				{100, "Green Cymbal Lane 6 - Crash/China/Splash/Bell"},
				{110, "Yellow Pad Lane 3 - Tom 1"},
				{111, "Blue Pad Lane 5 - Tom 2" },
				{112, "Green Pad Lane 7 - Tom 3"},
			};
			comboBox1.DataSource = Enum.GetNames(typeof(Notes));
			listBox1.DataSource = Config.Select((kv) => kv.Key).ToList();
		}

		private void RefreshDataSource()
		{
			listBox1.DataSource = null;
			listBox1.DataSource = Config.Select((kv) => kv.Key).ToList();
		}

		public frmMain()
		{
			InitializeComponent();
			InitConfig();
			ofd.InitialDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
			ofd.Filter = sfd.Filter = "Midi files (*.mid;*.midi)|*.mid;*.midi|All files (*.*)|*.*";
		}

		private static void EditNote(NoteVoiceMidiEvent ev)
		{
			ev.Note = GetNote(ev.Note);
		}

		public static void Script(string readPath, string savePath = null)
		{
			try
			{
				// Load file
				MidiSequence sequence;
				using (Stream inputStream = File.OpenRead(readPath))
				{
					sequence = MidiSequence.Open(inputStream);
				}

				// Keep first 2 sequences
				MidiSequence newSequence = new MidiSequence(Format.One, sequence.Division);
				newSequence.Tracks.Add(sequence.Tracks[0]);
				newSequence.Tracks.Add(sequence.Tracks[1]);

				// Change tempo of all notes
				MidiTrack track = newSequence.Tracks[1];

				foreach (var ev in track.Events)
				{
					// Edit to channel 10
					if (ev is VoiceMidiEvent)
						((VoiceMidiEvent)ev).Channel = 9;

					if (ev is NoteVoiceMidiEvent)
					{
						// Edit note level
						EditNote((NoteVoiceMidiEvent)ev);
					}
				}


				// Save file
				savePath = savePath ?? Path.Combine(Path.GetDirectoryName(readPath), Path.GetFileNameWithoutExtension(readPath) + "_edited.mid");

				using (Stream outputStream = File.OpenWrite(savePath))
				{
					newSequence.Save(outputStream);
				}

				MessageBox.Show("Success.\nSaved to " + savePath, "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
				Process.Start(Path.GetDirectoryName(savePath));
			}
			catch (Exception exc)
			{
				Console.Error.WriteLine("Error: {0}", exc.Message);
				MessageBox.Show("Error: \n" + exc.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void LoadFile()
		{
			if (ofd.ShowDialog() == DialogResult.OK)
			{
				path = ofd.FileName;
				Console.WriteLine("Path loaded : " + path);
				sfd.FileName = string.Empty;
			}
		}

		private void btnLoadFile_Click(object sender, EventArgs e)
		{
			LoadFile();
		}

		private void Execute()
		{
			if (string.IsNullOrEmpty(path) || !File.Exists(path))
			{
				MessageBox.Show("File not loaded properly !", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			else
			{
				// Load save path
				//if (string.IsNullOrEmpty(sfd.FileName))
				//{
				//	sfd.InitialDirectory = Path.GetDirectoryName(path);
				//	sfd.FileName = Path.GetFileNameWithoutExtension(path) + ".edited" + Path.GetExtension(path);
				//}

				//if (sfd.ShowDialog() == DialogResult.OK)
				//{
				//	string savePath = sfd.FileName;
				//	Console.WriteLine("Save path loaded to : " + savePath);
				//	Script(savePath);
				//}


				Script(path);
			}
		}

		private void btnExecute_Click(object sender, EventArgs e)
		{
			Execute();
		}

		private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (listBox1.SelectedIndex == -1)
			{
				label1.Text = string.Empty;
				comboBox1.SelectedIndex = -1;
				return;
			}

			pauseCheckEdit = true;

			byte selConfig = (byte)listBox1.SelectedItem;
			Notes selConfigNote = Config[selConfig];
			comboBox1.SelectedItem = selConfigNote.ToString();
			label1.Text = Comments[selConfig];

			pauseCheckEdit = false;
		}

		private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (pauseCheckEdit)
				return;

			if (comboBox1.SelectedIndex == -1)
				return;


			byte selConfig = (byte)listBox1.SelectedItem;
			if (!Enum.TryParse(comboBox1.SelectedItem.ToString(), out Notes selNote))
				return;

			Config[selConfig] = selNote;
		}

		private void button3_Click(object sender, EventArgs e)
		{
			Config[(byte)numericUpDown1.Value] = Notes.Undefined;
			Comments[(byte)numericUpDown1.Value] = "Custom value";
			RefreshDataSource();
		}

		private void button4_Click(object sender, EventArgs e)
		{
			Config.Remove((byte)numericUpDown1.Value);
			Comments.Remove((byte)numericUpDown1.Value);
			RefreshDataSource();
		}

		private void frmMain_Load(object sender, EventArgs e)
		{

		}
	}
}
