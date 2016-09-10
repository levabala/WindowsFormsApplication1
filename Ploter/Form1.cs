﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using Microsoft.FSharp.Core;

namespace Ploter
{
    public partial class Form1 : Form
    {
        Camera cam;
        Matrix m;
        List<PointF> points = new List<PointF>();
        const float bufferPerc = 0.01f; //buffer in percentage       
        float max, min;
        public Form1()
        {
            InitializeComponent();                        
            MouseWheel += Form1_MouseWheel;            
        }

        private void Begin()
        {
            cam = new Camera(new PointF(0f, min), new PointF(points.Count, max), points);
            cam.detectPointsToDraw();

            m = new Matrix();

            //invert axis Y
            m.Scale(1, -1);
            m.Translate(0, -ClientSize.Height);

            //resizing for camera size
            m.Scale(ClientSize.Width / cam.width, ClientSize.Height / cam.height);
        }

        private void Form1_MouseWheel(object sender, MouseEventArgs e)
        {            
            PointF pos = DataPoint(e.Location);
            bool inXscale = e.Location.Y < 50;
            bool inYscale = e.Location.X < 50;
            float z = e.Delta > 0 ? 1.1f : 1.0f / 1.1f;
            float kx = z;
            float ky = z;
            if (ModifierKeys.HasFlag(Keys.Control) || inXscale) ky = 1; //!(m.Elements[1] > -1e-5 && m.Elements[1] < -0.05) 
            if (ModifierKeys.HasFlag(Keys.Shift) || inYscale) kx = 1; //!(m.Elements[0] > 0.05 && m.Elements[0] < 1000)
            PointF po = DataPoint(e.Location);
            m.Translate(po.X, po.Y);
            m.Scale(kx, ky);
            m.Translate(-po.X, -po.Y);

            cam.MoveTo(DataPoint(new PointF(0f, 0f)), DataPoint(new PointF(ClientSize.Width, ClientSize.Height)));
            cam.detectPointsToDraw();

            Invalidate();
        }

        private PointF DataPoint(PointF scr)
        {
            Matrix mr = m.Clone();
            mr.Invert();
            PointF[] po = new PointF[] { new PointF(scr.X, scr.Y) };
            mr.TransformPoints(po);
            return po[0];
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Int64[] F5 = Parser.parseLM(@"D:\work\test_6000_fast.raw", 242);
            max = 0f;
            min = F5[1] - F5[0];
            float cr = 0f;
            for (int i = 1; i < F5.Length / 2; i += 2)
            {
                //deltaF5[i / 2] = F5[i] - F5[i - 1];
                cr = F5[i] - F5[i - 1];
                if (cr < 10000)                                 //for debug
                {
                    points.Add(new PointF((i - 1) / 2, cr));
                    if (cr > max) max = cr;
                    else if (cr < min) min = cr;
                }
            }
            Begin();
            Invalidate();
        }

        Pen myPen = new Pen(Color.Green, 1);        
        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            //Text = cam.ToString();            
            myPen.LineJoin = LineJoin.Bevel;
            Graphics g = e.Graphics;            
            g.DrawString(
                m.Elements.Select(a => a.ToString() + "\n").Aggregate((a,b)=>a+b),
                this.Font, Brushes.Red, 50, 50
                );
            g.Transform = m;

            if (cam.toDraw.Count == 0) return; 

            List<PointF> toDraw = new List<PointF>();
            int step = (int)Math.Ceiling((double)(cam.toDraw.Count / ClientSize.Width));
            if (step == 0) step = 1;
            Text = cam.toDraw.Count.ToString() + "/" + ClientSize.Width.ToString() + " = " + step.ToString();

            for (int i = 0; i < cam.toDraw.Count-step; i += step)
            {
                toDraw.Add(points[cam.toDraw[i]]);
                toDraw.Add(new PointF(points[cam.toDraw[i+1]].X, points[cam.toDraw[i]].Y));
            }
                

            g.DrawLines(myPen, toDraw.ToArray());            
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            float dx = 0f;
            float dy = 0f;

            float deltaX = 10;
            float deltaY = 50000;
            switch (e.KeyCode)
            {
                case Keys.Left:
                    {
                        dx = -deltaX;
                        break;
                    }
                case Keys.Right:
                    {
                        dx = deltaX;
                        break;
                    }
                case Keys.Up:
                    {
                        dy = deltaY;
                        break;
                    }
                case Keys.Down:
                    {
                        dy = -deltaY;
                        break;
                    }
                case Keys.R:
                    {
                        Begin();
                        Invalidate();
                        break;
                    }
            }
            cam.Move(dx, dy);
            m.Translate(-dx, -dy);
            cam.detectPointsToDraw();
            Invalidate();
        }

        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            //Text = cam.offsetX.ToString() + " " + cam.offsetY.ToString();
        }

        private void Form1_ResizeEnd(object sender, EventArgs e)
        {
            /*m = new Matrix();

            //invert axis Y
            m.Scale(1, -1);
            m.Translate(0, -ClientSize.Height);

            //resizing for camera size
            m.Scale(ClientSize.Width / cam.width, ClientSize.Height / cam.height);

            m.Translate(-cam.offsetX, -cam.offsetY);*/

            Invalidate();
        }
    }
}