﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinSolitaireTrain
{
    public partial class WinTrain : Form
    {
        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);

        #region Fields
        ImageCollectionHandler ImageCollection;
        PictureDraw drawer;
        bool placing = false;
        bool sizing = false;
        Point savedLocation;
        Thread keyThread;
        AITrainer ai;
        #endregion

        public WinTrain()
        {
            InitializeComponent();
            ImageCollection = new ImageCollectionHandler();
            ai = new AITrainer(lbType.Items.Cast<string>().ToArray());
            drawer = new PictureDraw(pbCard, this.Font, ai.names);

            pbCard.Click += (s, e) => 
            { 
                if (placing) { 
                    placing = false; 
                    sizing = true; 
                    savedLocation = Cursor.Position; 
                    UpdateStatus("Vælg størrelse"); 
                }
                else if (sizing) { 
                    sizing = false;
                    UpdateStatus("Venter");
                }
                else if (!sizing && !placing) { 
                    placing = true;
                    UpdateStatus("Vælg placering");
                }
            };            
            pbCard.MouseMove += (s, e) => {
                if (placing)
                {
                    drawer.rectangle = new Rectangle(
                        e.Location,
                        new Size(drawer.rectangle.Width, drawer.rectangle.Height)
                    );
                }

                if (sizing)
                {
                    var dx = e.Location.X - drawer.rectangle.X;
                    var dy = e.Location.Y - drawer.rectangle.Y;

                    drawer.rectangle = new Rectangle(
                        drawer.rectangle.X, 
                        drawer.rectangle.Y,

                        dx, 
                        dy
                    );
                }

                if ((placing || sizing) && ImageCollection.HasImages())
                {
                    drawer.Draw(ImageCollection.Get());
                }
            };

            keyThread = new Thread(() =>
            {
                while (true)
                {
                    if (GetAsyncKeyState(0x7B) == -32767)
                    {
                        this.Invoke(new Action(() => {
                            lock (drawer.mutex)
                            {
                                if (lbType.SelectedIndex >= 0)
                                {
                                    drawer.Cards.Add(new Card()
                                    {
                                        Bounds = drawer.rectangle,
                                        Type = lbType.SelectedIndex
                                    });
                                    drawer.rectangle = new Rectangle(0, 0, 1, 1);
                                    placing = true;
                                    sizing = false;
                                }
                            }
                        }));
                    }

                    if (GetAsyncKeyState(0xA6) == -32767)
                    {
                        this.Invoke(new Action(() => {
                            lock (drawer.mutex)
                            {
                                if (drawer.Cards.Count > 0)
                                    drawer.Cards.Remove(drawer.Cards.Last());
                            }
                        }));
                    }

                    Thread.Sleep(1);
                }
            });
            keyThread.Start(); 
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            keyThread.Abort();
        }

        private void testToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var msgBoxResult = MessageBox.Show("Open Default?", "Open", MessageBoxButtons.YesNo);

            if (msgBoxResult == DialogResult.Yes)
            {
                string[] files = Directory.GetFiles(@"C:\Users\hansk\source\repos\SolitaireSolver\SolitaireSolver\bin\Debug\imgs");

                foreach (var file in files)
                {
                    Image image = System.Drawing.Image.FromFile(file);
                    ImageCollection.Add(image);
                }

                OpenImage();

                return;
            }

            using (var fbd = new FolderBrowserDialog())
            {
                var result = fbd.ShowDialog();
                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    string[] files = Directory.GetFiles(fbd.SelectedPath);

                    foreach (var file in files)
                    {
                        Image image = System.Drawing.Image.FromFile(file);
                        ImageCollection.Add(image);
                    }
                }
            }

            OpenImage();
        }

        private void OpenImage()
        {
            drawer.Draw(ImageCollection.Get());
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            if (ImageCollection.Next())
                OpenImage();
            else 
                MessageBox.Show("Færdig");
        }

        private void btnBeginTraining_Click(object sender, EventArgs e)
        {
            ai.BeginTraining();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (lbType.SelectedIndex >= 0)
            {
                var img = (Bitmap)ImageCollection.Get();
                placing = true;
                ai.Save(img, drawer.Cards.ToArray());
                drawer.Cards.Clear();
                UpdateStatus("Gemt");
            } else
            {
                MessageBox.Show("Vælg korttype!");
            }
        }

        private void UpdateStatus(string Status)
        {
            lblStatus.Text = $"Status: {Status}";
        }
    }
}
