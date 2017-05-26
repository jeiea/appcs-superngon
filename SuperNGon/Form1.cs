using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SuperNGon
{
  public partial class Form1 : Form
  {
    int Side = 6;
    PointF Center;
    float CursorAngle;
    DateTime Start;
    TimeSpan Last;
    TimeSpan Record;
    Dictionary<Keys, bool> KeyStates = new Dictionary<Keys, bool>();
    List<LinkedList<float>> Obstacles;
    Task Game;
    CancellationTokenSource Cancellation;

    public Form1()
    {
      ResizeRedraw = true;
      DoubleBuffered = true;
      Obstacles = new List<LinkedList<float>>();
      for (int i = 0; i < Side; i++)
        Obstacles.Add(new LinkedList<float>());
      CursorAngle = (float)Math.Ceiling(60.0 / 360 * Side) * 360 / Side;
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
      var elapsed = Game == null ? Record : DateTime.Now - Start;

      var main = g.BeginContainer();
      g.SmoothingMode = SmoothingMode.AntiAlias;
      g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

      // Transform to expand 1280x720
      var originAnchor = MatrixAnchor(g);
      g.TranslateTransform(Center.X, Center.Y);
      var transAnchor = MatrixAnchor(g);
      float zoom = Math.Min(Center.X / 640, Center.Y / 360);
      if (Game != null)
      {
        g.RotateTransform(elapsed.Ticks / 200000f);
      }
      g.ScaleTransform(zoom, zoom);

      FillBackground(g);

      #region Draw Texts
      transAnchor(m => m.Scale(zoom, zoom));

      var gothic = new Font("맑은 고딕", 40, FontStyle.Bold);
      var meterBrush = new SolidBrush(
        Game == null && Record.Ticks != 0 && Record == Last ||
        elapsed > Record
        ? Color.Red
        : Color.Black);
      var rankString = string.Format("NEW RECORD: {0:f2}",
        elapsed > Record ? elapsed.TotalSeconds : Record.TotalSeconds); ;
      g.DrawString(rankString, gothic, meterBrush, 100, -370);
      var recordString = string.Format("RECORD: {0:f2}",
        Game == null ? Last.TotalSeconds : elapsed.TotalSeconds);
      g.DrawString(recordString, gothic, meterBrush, 248, -310);
      if (Game == null)
      {
        g.DrawString("PRESS SPACE TO START", gothic, new SolidBrush(Color.Black), -340, 290);
        gothic = new Font("맑은 고딕", 70, FontStyle.Bold);
        g.DrawString($"SUPER\n   {Side}-GON", gothic, new SolidBrush(Color.White), -350, -320);
      }
      #endregion

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
      var angle = 2 * Math.PI / Side;
      var top = new PointF(0, -80);
      var rot = top.Rotate(angle);
      var triangle = new PointF[] { new PointF(), top, rot };
      var spike = new PointF[] { top, new PointF(), rot };

      var originAnchor = MatrixAnchor(g);

      var obsFront = new SolidBrush(new HSBColor(1 / 3f, 1, .9f));
      var briBack = new SolidBrush(new HSBColor(1 / 3f, 1, .7f));
      var drkBack = new SolidBrush(new HSBColor(1 / 3f, 1, .3f));
      var midBack = new SolidBrush(new HSBColor(1 / 3f, 1, .5f));
      var whiteBrush = new SolidBrush(Color.White);
      var whitePen = new Pen(obsFront, 1500) { LineJoin = LineJoin.Round };
      var darkPen = new Pen(drkBack, 500) { LineJoin = LineJoin.Bevel };

      var degreeSide = 360f / Side;
      var backBrush = Side % 2 == 1 ? midBack : briBack;
      for (int i = Side - 1; i >= 0; i--)
      {
        // Rotate and draw background
        originAnchor(m => m.Rotate(degreeSide * (i + 0.5f)));
        g.FillPolygon(backBrush, new PointF[] { });

        // Draw obstacles
        var poly = new List<PointF>();
        foreach (var wallRatio in Obstacles[i].Reverse())
        {
          var wall = new PointF(0, -2000 * wallRatio);
          poly.AddRange(new[] { new PointF(), wall, wall.Rotate(angle) });
        }
        if (poly.Count > 0) g.FillPolygon(obsFront, poly.ToArray());

        // Draw center hexagon
        g.ScaleTransform(0.004f, 0.004f);
        g.FillPolygon(drkBack, triangle);
        g.DrawLines(darkPen, spike);
        //g.DrawLine(whitePen, top, rot);

        // abandon third color
        backBrush = i % 2 == 0 ? briBack : drkBack;
      }

      originAnchor();
      var inner = new PointF(0, -80);
      g.DrawLines(whitePen, Enumerable.Range(0, Side).Select(i => inner.Rotate(angle * i)).ToArray());

      // Draw cursor
      originAnchor(m => m.Rotate(CursorAngle));
      float bot = -100, tip = 130, wid = 17.32f;
      var cursor = new PointF[] { new PointF(0, -tip), new PointF(-wid, bot), new PointF(wid, bot) };
      g.FillPolygon(new SolidBrush(Color.LawnGreen), cursor);
      originAnchor();
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
      KeyStates[e.KeyCode] = false;
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
      bool handled = Game == null ? OnTitleKey(keyData) : OnGameKey(keyData);
      return handled ? true : base.ProcessCmdKey(ref msg, keyData);
    }

    private bool OnGameKey(Keys keyData)
    {
      switch (keyData)
      {
        case Keys.Left:
          KeyStates[Keys.Left] = true;
          return true;
        case Keys.Right:
          KeyStates[Keys.Right] = true;
          return true;
        case Keys.Escape:
          Cancellation.Cancel();
          Game = null;
          return true;
        default:
          return false;
      }
    }

    private bool OnTitleKey(Keys keyData)
    {
      switch (keyData)
      {
        case Keys.Up:
          Side = Math.Min(100, Side + 1);
          CursorAngle = (float)Math.Ceiling(60.0 / 360 * Side) * 360 / Side;
          Obstacles.Add(new LinkedList<float>());
          Invalidate();
          return true;
        case Keys.Down:
          Side = Math.Max(3, Side - 1);
          CursorAngle = (float)Math.Ceiling(60.0 / 360 * Side) * 360 / Side;
          Obstacles.RemoveAt(Obstacles.Count - 1);
          Invalidate();
          return true;
        case Keys.Space:
          if (Game != null) return false;
          Cancellation = new CancellationTokenSource();
          Start = DateTime.Now;
          Game = new Task(DoGame, Cancellation.Token);
          Game.Start();
          return true;
        default:
          return false;
      }
    }

    async void DoGame()
    {
      var r = new Random();
      var token = Cancellation.Token;
      while (!token.IsCancellationRequested)
      {
        for (int i = Obstacles.Sum(x => x.Count); i < 6; i++)
        {
          var col = r.Next(0, Obstacles.Count - 1);
          Obstacles[col].AddLast(1);
          Obstacles[col].AddLast((float)(1 + r.NextDouble() * 0.3));
        }
        for (int i = 0; i < Obstacles.Count; i++)
        {
          for (var node = Obstacles[i].First; node != null; node = node.Next)
          {
            node.Value -= 0.04f;
          }
        }
        foreach (var obs in Obstacles)
        {
          while (obs.Count != 0 && obs.First.Value < 0)
            obs.RemoveFirst();
        }
        if (KeyStates.TryGetValue(Keys.Left, out bool pressing) && pressing)
        {
          CursorAngle -= 5;
        }
        if (KeyStates.TryGetValue(Keys.Right, out bool pressing2) && pressing2)
        {
          CursorAngle += 5;
        }
        Invalidate();
        await Task.Delay(16);
      }
      Last = DateTime.Now - Start;
      if (Record < Last) Record = Last;
      foreach (var obs in Obstacles) obs.Clear();
      Invalidate();
    }
  }
}
