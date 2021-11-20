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
	public partial class Form1 : Form
	{
		private string path = string.Empty;

		public enum Notes
		{
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

		public static byte GetNote(Notes note)
		{
			return (byte)note;
		}

		public Form1()
		{
			InitializeComponent();

			ofd.InitialDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
			ofd.Filter = sfd.Filter = "Midi files (*.mid;*.midi)|*.mid;*.midi|All files (*.*)|*.*";
		}

		private void EditNote(NoteVoiceMidiEvent ev)
		{
			byte newNote = ev.Note;
			switch (ev.Note)
			{
				// Orange Long Note - Kick
				case 96:
					newNote = GetNote(Notes.Kick);
					break;

				// Red Pad Lane 1 - Normal Snare/Ghost Note Snare
				case 97:
					newNote = GetNote(Notes.Snare);
					break;

				// Yellow Cymbal Lane 2 - Crash/Splash/Bell
				case 98:
					newNote = GetNote(Notes.Crash);
					break;

				// Blue Cymbal Lane 4 - China/Ride/Bell/Crash/Splash
				case 99:
					newNote = GetNote(Notes.China);
					break;

				// Green Cymbal Lane 6 - Crash/China/Splash/Bell
				case 100:
					newNote = GetNote(Notes.Ride);
					break;

				// Yellow Pad Lane 3 - Tom 1
				case 110:
					newNote = GetNote(Notes.LowMidTom);
					break;

				// Blue Pad Lane 5 - Tom 2
				case 111:
					newNote = GetNote(Notes.LowTom);
					break;

				// Green Pad Lane 7 - Tom 3
				case 112:
					newNote = GetNote(Notes.LowFloorTom);
					break;

				default:
					break;
			}
			if (ev.Note != newNote)
				ev.Note = newNote;
		}

		private void Script(string savePath)
		{
			try
			{
				// Load file
				MidiSequence sequence;
				using (Stream inputStream = File.OpenRead(path))
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
				using (Stream outputStream = File.OpenWrite(savePath))
				{
					newSequence.Save(outputStream);
				}

				Process.Start(Path.GetDirectoryName(savePath));
				MessageBox.Show("Success.\nSaved to " + savePath, "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
				if (string.IsNullOrEmpty(sfd.FileName))
				{
					sfd.InitialDirectory = Path.GetDirectoryName(path);
					sfd.FileName = Path.GetFileNameWithoutExtension(path) + ".edited" + Path.GetExtension(path);
				}

				if (sfd.ShowDialog() == DialogResult.OK)
				{
					string savePath = sfd.FileName;
					Console.WriteLine("Save path loaded to : " + savePath);
					Script(savePath);
				}
			}
		}

		private void btnExecute_Click(object sender, EventArgs e)
		{
			Execute();
		}
	}
}
