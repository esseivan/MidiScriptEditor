using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MidiScriptEditor
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			string[] args = Environment.GetCommandLineArgs();
			string arg_file = args.ElementAtOrDefault(1);
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			if (string.IsNullOrEmpty(arg_file))
			{
				Application.Run(new frmMain());
			}
			else
			{
				try
				{
					frmMain.Script(arg_file);
				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.ToString());
					throw;
				}
			}
		}
	}
}
