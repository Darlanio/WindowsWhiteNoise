using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsWhiteNoise
{
    public partial class MainForm : Form
    {
        private Random _random = new Random();

        // GDI32 used for fallback and is slower than prepared bitmaps.
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        internal static extern bool SetPixel(IntPtr hdc, int X, int Y, uint crColor);

        private Boolean ticking = false;
        private Bitmap[] bitmaps;
        int bitmapCounter = 0;
        int [] lastBitmapCounters;

        public MainForm()
        {
            InitializeComponent();
            SetDoubleBuffered(mainPanel);

            bitmaps = null;
            bitmaps = CreateRandomBitmaps(bitmaps, 180, 1920, 1080);
            lastBitmapCounters = new int[bitmaps.Length];
            for (int i = 0; i < lastBitmapCounters.Length; i++) lastBitmapCounters[i] = i;
            mainTimer.Enabled = true;
        }

        /// <summary>
        /// Picks a new random bitmap and remembers the ones that has been used, avoiding to pick the most recently used.
        /// </summary>
        private void UpdateBitmapCounter()
        {
            int n,r;

            r = _random.Next() % (bitmaps.Length / 2);
            n = lastBitmapCounters[r];
            bitmapCounter = n;
            for(int i=r;i<lastBitmapCounters.Length-1;i++)
            {
                lastBitmapCounters[i] = lastBitmapCounters[i + 1];
            }
            lastBitmapCounters[lastBitmapCounters.Length - 1] = n;
        }

        /// <summary>
        /// Creates specified number of bitmaps with specified dimensions for later use
        /// </summary>
        /// <param name="bms"></param>
        public Bitmap[] CreateRandomBitmaps(Bitmap[] bms, int nob, int maxx, int maxy)
        {
            bms = new Bitmap[nob];
            for(int i=0;i<nob;i++)
            {
                bms[i] = new Bitmap(maxx, maxy);
                using (Graphics gr = Graphics.FromImage(bms[i]))
                {
                    for (int y = 0; y < maxy; y++)
                    {
                        for (int x = 0; x < maxx;)
                        {
                            uint randomBits = (uint)_random.Next();
                            for (int x2 = x + 24; x < x2; x++)
                            {
                                Brush useThisBrush = System.Drawing.Brushes.White;
                                if ((randomBits & (uint)1) == 1)
                                {
                                    useThisBrush = System.Drawing.Brushes.Black;
                                }
                                randomBits = randomBits >> 1;
                                gr.FillRectangle(useThisBrush, x, y, 1, 1);
                            }
                        }
                    }

                }
            }
            return bms;
        }



        // FROM: http://stackoverflow.com/questions/76993/how-to-double-buffer-net-controls-on-a-form
        /// <summary>
        /// SetDoubleBuffered
        /// Makes the window update without flickering - .NET doublebuffer
        /// </summary>
        /// <param name="c"></param>
        public static void SetDoubleBuffered(System.Windows.Forms.Control c)
        {
            //Taxes: Remote Desktop Connection and painting
            //http://blogs.msdn.com/oldnewthing/archive/2006/01/03/508694.aspx
            if (System.Windows.Forms.SystemInformation.TerminalServerSession)
                return;

            System.Reflection.PropertyInfo aProp =
                  typeof(System.Windows.Forms.Control).GetProperty(
                        "DoubleBuffered",
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Instance);

            aProp.SetValue(c, true, null);
        }



        private void MainPanel_Paint(object sender, PaintEventArgs e)
        {
            int renderWidth = (int) e.Graphics.ClipBounds.Width;
            int renderHeight = e.ClipRectangle.Height;

            if (bitmaps == null)
            {
                // Fallback to slow routine if no bitmaps have been created
                uint white = (uint)((255 << 16) | (255 << 8) | (255));
                uint black = (uint)0;
                IntPtr hdc = e.Graphics.GetHdc();
                for (int y = 0; y < renderHeight; y++)
                {
                    for (int x = 0; x < renderWidth;)
                    {
                        // Color pixelColor = GetPixelColor(x, y);
                        // NOTE: GDI colors are BGR, not ARGB.                    
                        //uint colorRef = (uint)((pixelColor.B << 16) | (pixelColor.G << 8) | (pixelColor.R));

                        uint randomBits = (uint)_random.Next();
                        for (int x2 = x + 24; x < x2; x++)
                        {
                            uint colorRef = white;
                            if ((randomBits & (uint)1) == 1)
                            {
                                colorRef = black;
                            }
                            randomBits = randomBits >> 1;
                            SetPixel(hdc, x, y, colorRef);
                        }
                    }
                }
                e.Graphics.ReleaseHdc(hdc);
            } else
            {
                // Fast routine, using premade bitmaps
                e.Graphics.DrawImage(bitmaps[bitmapCounter], 0, 0);
                UpdateBitmapCounter();
            }
            ticking = false; // Paint is done, reenabling the timer
        }

        /// <summary>
        /// Mouse Click Handler - toggles fullscreen mode / window mode
        /// </summary>
        /// <param name="sender">the object sending the mouseclick</param>
        /// <param name="e">MouseEventArgs</param>
        private void MainPanel_MouseClick(object sender, MouseEventArgs e)
        {
            if (this.FormBorderStyle == FormBorderStyle.None)
            {
                this.FormBorderStyle = FormBorderStyle.Sizable;
                this.WindowState = FormWindowState.Normal;
            }
            else
            {
                this.FormBorderStyle = FormBorderStyle.None;
                if (this.WindowState == FormWindowState.Maximized) this.WindowState = FormWindowState.Normal;
                this.WindowState = FormWindowState.Maximized;
            }
        }

        private void MainTimer_Tick(object sender, EventArgs e)
        {
            if(!ticking)
            {
                ticking = true; // When Paint is done, this is set to false
                mainPanel.Refresh();
            }
        }
    }
}
