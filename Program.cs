﻿﻿using DOOM;
using DOOM.WAD;
using System.Drawing;
using System.Windows.Forms;


public static class Program
{
    [STAThread]
    public static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        Application.Run(new Renderer());
    }

}
