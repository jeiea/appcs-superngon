using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SuperNGon
{
  public partial class Form1 : Form
  {
    int Side = 6;
    long StartTick;
    PointF Center;

    class PaintCache
    {
      public PointF Center;
      public PointF[] Vetices;
      public Brush[] Brushes;
    }

    PaintCache Memo;

    public Form1()
    {
      ResizeRedraw = true;
      DoubleBuffered = true;
      InitializeComponent();
    }

    protected override void OnResize(EventArgs e)
    {
      Center = new PointF(ClientSize.Width / 2, ClientSize.Height / 2);
      base.OnResize(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
      var g = e.Graphics;
      var tick = DateTime.Now.Ticks - StartTick;

      var main = g.BeginContainer();

      FillBackground(g);

      int insc = Math.Min(Width, Height);
      var top = new PointF(Center.X, Center.Y * 0.8f).Rotate(Math.PI / Side);

      g.EndContainer(main);
    }

    private void FillBackground(Graphics g)
    {
      var r = Math.Sqrt(Math.Pow(Center.X, 2) + Math.Pow(Center.Y, 2));
      var chordInscribedRadius = 1 / Math.Cos(Math.PI / 3) * r;
      var top = new PointF(0, -(float)chordInscribedRadius);
      var angle = 2 * Math.PI / Side;
      var rot = top.Rotate(angle, Center);
      var chordDots = Enumerable.Range(0, Side + 1)
        .Select(i => top.Rotate(angle * i, Center)).ToArray();

      PointF[] GetFan(int c)
      {
        var vs = new List<PointF>();
        for (int i = c; i < Side; i += 2)
        {
          vs.AddRange(new PointF[] {
              Center,
              chordDots[i],
              chordDots[i + 1],
            });
        }
        return vs.ToArray();
      }

      var obsFront = new SolidBrush(new HSBColor(1 / 3f, 1, .9f));
      var briBack = new SolidBrush(new HSBColor(1 / 3f, 1, .7f));
      var drkBack = new SolidBrush(new HSBColor(1 / 3f, 1, .3f));
      g.FillPolygon(briBack, GetFan(0));
      g.FillPolygon(drkBack, GetFan(1));
      if (Side % 2 == 1)
      {
        var midBack = new SolidBrush(new HSBColor(1 / 3f, 1, .5f));
        g.FillPolygon(midBack, GetFan(chordDots.Length - 2));
      }
    }


  }
}
