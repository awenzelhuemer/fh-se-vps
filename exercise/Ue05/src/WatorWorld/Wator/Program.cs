using System;
using System.Windows.Forms;

namespace VPS.Wator {
  static class Program {
    [STAThread]
    static void Main() {

        // -- Optimierungen --
        // TODO Listen für Point
        // TODO Instance of
        // TODO Zweidimensionale Arrays
        // TODO Randomisieren von der Matrix

      Application.EnableVisualStyles();
      Application.SetCompatibleTextRenderingDefault(false);
      Application.Run(new MainForm());
    }
  }
}