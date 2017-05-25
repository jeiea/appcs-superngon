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
    float CursorAngle;
    List<Queue<float>> Obstacles;

    public Form1()
    {
      ResizeRedraw = true;
      DoubleBuffered = true;
      CursorAngle = 180 / Side;
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
      g.SmoothingMode = SmoothingMode.AntiAlias;
      g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

      // Fill transform to 1280x720
      var originAnchor = MatrixAnchor(g);
      g.TranslateTransform(Center.X, Center.Y);
      float zoom = Math.Min(Center.X / 640, Center.Y/ 360);
      g.ScaleTransform(zoom, zoom);

      FillBackground(g);

      g.DrawRectangle(new Pen(new SolidBrush(Color.White)), new Rectangle(-200, -200, 100, 100));
      g.DrawString($"SUPER\n   {Side}-GON",
        new Font("Arial", 30, FontStyle.Bold),
        new SolidBrush(Color.Red), -200, -200);

      g.EndContainer(main);
    }

    delegate void TransformReset(Action<Matrix> action = null);
    static TransformReset MatrixAnchor(Graphics g)
    {
      var mat = g.Transform;
      return transformer =>
      {
        var m = mat.Clone();
        if (transformer != null) transformer(m);
        g.Transform = m;
      };
    }

    private void FillBackground(Graphics g)
    {
      var r = Math.Sqrt(Math.Pow(Center.X, 2) + Math.Pow(Center.Y, 2));
      var top = new PointF(0, -20000);
      // TODO: Initial rotation by window ratio
      var rot = top.Rotate(2 * Math.PI / Side);
      var triangle = new PointF[] { new PointF(), top, rot };

      var originAnchor = MatrixAnchor(g);

      var obsFront = new SolidBrush(new HSBColor(1 / 3f, 1, .9f));
      var briBack = new SolidBrush(new HSBColor(1 / 3f, 1, .7f));
      var drkBack = new SolidBrush(new HSBColor(1 / 3f, 1, .3f));

      void DrawHolePolygon()
      {
        g.ScaleTransform(0.004f, 0.004f);
        g.FillPolygon(new SolidBrush(Color.White), triangle);
        g.DrawPolygon(new Pen(Color.White) { LineJoin = LineJoin.Bevel }, triangle);
      }
      var degreeSide = 360f / Side;
      for (int i = Side / 2 * 2; i >= 0; i--)
      {
        originAnchor(m => m.Rotate(degreeSide * i));
        g.FillPolygon(i % 2 == 0 ? briBack : drkBack, triangle);
        DrawHolePolygon();
      }
      if (Side % 2 == 1)
      {
        originAnchor(m => m.Rotate(-360f / Side));
        var midBack = new SolidBrush(new HSBColor(1 / 3f, 1, .5f));
        g.FillPolygon(midBack, triangle);
        DrawHolePolygon();
      }

      originAnchor(m => m.Rotate(CursorAngle));
      float bot = -100, tip = 130, wid = 17.32f;
      var cursor = new PointF[] { new PointF(0, -tip), new PointF(-wid, bot), new PointF(wid, bot) };
      g.FillPolygon(new SolidBrush(Color.LawnGreen), cursor);
      originAnchor();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
      switch (keyData)
      {
        case Keys.Up:
          Side = Math.Min(100, Side + 1);
          CursorAngle = 180 / Side;
          Invalidate();
          return true;
        case Keys.Down:
          Side = Math.Max(3, Side - 1);
          CursorAngle = 180 / Side;
          Invalidate();
          return true;
        default:
          return false;
      }
    }
  }
}
