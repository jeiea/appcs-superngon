using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SuperNGon
{
  public partial class SuperPolygonForm : Form
  {
    const int MaxSide = 100;
    int Side = 6;
    PointF Center;
    float CursorAngle;
    DateTime Start;
    TimeSpan Last;
    TimeSpan Record;
    Func<float> GetRotation;
    Func<float> GetExpansion;
    Func<float> GetHue;
    Queue<Wall> Walls = new Queue<Wall>();
    Task Game;
    CancellationTokenSource Cancellation;

    class Wall
    {
      public float Length;
      public bool[] Exist = new bool[MaxSide];
    }

    public SuperPolygonForm()
    {
      for (int i = 0; i < 50; i++)
        Walls.Enqueue(new Wall());
      SideNumPreparation();
      InitializeComponent();
      ResizeRedraw = true;
      DoubleBuffered = true;
      Text = "SuperNGon";
      ClientSize = new Size(760, 760);
    }

    private void SideNumPreparation()
    {
      foreach (var w in Walls) w.Length = 0;
      CursorAngle = (float)Math.Ceiling(60.0 / 360 * Side) * 360 / Side - 180 / Side;
      GetRotation = () => 180 / Side;
      GetExpansion = () => 1;
      GetHue = () => 1 / 3f;
    }

    protected override void OnResize(EventArgs e)
    {
      Center = new PointF(ClientSize.Width / 2, ClientSize.Height / 2);
      base.OnResize(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
      var g = e.Graphics;

      var main = g.BeginContainer();
      g.SmoothingMode = SmoothingMode.AntiAlias;
      g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

      // Transform to expand 1280x720
      var originAnchor = MatrixAnchor(g);
      g.TranslateTransform(Center.X, Center.Y);
      var transAnchor = MatrixAnchor(g);
      float zoom = Math.Min(Center.X / 640, Center.Y / 360);
      g.RotateTransform(GetRotation());
      g.ScaleTransform(zoom, zoom);

      FillBackground(g);

      #region Draw Texts
      transAnchor(m => m.Scale(zoom, zoom));

      var elapsed = Game == null ? Record : DateTime.Now - Start;
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
      var originAnchor = MatrixAnchor(g);

      #region Drawing resources
      var angle = 2 * Math.PI / Side;
      var hue = GetHue();
      var shineBrush = new SolidBrush(new HSBColor(hue, 1, .9f));
      var briBack = new SolidBrush(new HSBColor(hue, 1, .5f));
      var drkBack = new SolidBrush(new HSBColor(hue, 1, .3f));
      var shinePen = new Pen(shineBrush, 3) { LineJoin = LineJoin.Round };
      #endregion

      #region Draw background
      var outop = new PointF(0, -2000);
      var chordDots = Enumerable.Range(0, Side + 1)
        .Select(i => outop.Rotate(angle * i)).ToArray();
      PointF[] GetFan(int c)
      {
        var vs = new List<PointF>();
        for (int i = c; i < Side; i += 2)
        {
          vs.Add(new PointF());
          vs.Add(chordDots[i]);
          vs.Add(chordDots[i + 1]);
        }
        return vs.ToArray();
      }
      g.FillPolygon(briBack, GetFan(0));
      g.FillPolygon(drkBack, GetFan(1));
      if (Side % 2 == 1)
      {
        var midBack = new SolidBrush(new HSBColor(hue, 1, .4f));
        g.FillPolygon(midBack, new[] {
          new PointF(),
          chordDots[chordDots.Length - 2],
          chordDots[chordDots.Length - 1]
        });
      }
      #endregion

      #region Draw obstacles
      var walls = new List<PointF>();
      for (int i = 0; i < Side; i++)
      {
        // Charge obstacles
        var pt = new PointF(0, -1).Rotate(i * angle);
        float sum = 0;
        bool filled = false;
        foreach (var wall in Walls)
        {
          sum += wall.Length;
          if (wall.Exist[i] == filled) continue;
          var outside = new PointF(pt.X * sum, pt.Y * sum);
          walls.Add(outside.Rotate(angle));
          walls.Add(outside);
          walls.Add(new PointF());
          var prev = sum - wall.Length;
          var inside = new PointF(pt.X * prev, pt.Y * prev);
          walls.Add(inside.Rotate(angle));
          walls.Add(inside);
          walls.Add(new PointF());
          if (sum > 1500) break;
        }
      }
      if (walls.Count > 0)
      {
        walls.Reverse();
        g.FillPolygon(shineBrush, walls.ToArray());
      }
      #endregion

      #region Draw center polygon
      var intop = new PointF(0, -83 * GetExpansion());
      var center = Enumerable.Range(0, Side + 1).Select(i => intop.Rotate(angle * i)).ToArray();
      g.FillPolygon(drkBack, center);
      g.DrawLines(shinePen, center);
      #endregion

      #region Draw cursor
      originAnchor(m => m.Rotate(CursorAngle));
      float bot = -100, tip = 120, wid = 11.55f;
      var cursor = new PointF[] { new PointF(0, -tip), new PointF(-wid, bot), new PointF(wid, bot) };
      g.FillPolygon(new SolidBrush(Color.LawnGreen), cursor);
      originAnchor();
      #endregion
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
      switch (keyData)
      {
        case Keys.Escape:
          if (Game == null) break;
          Cancellation.Cancel();
          return true;
        case Keys.Up:
        case Keys.Down:
          if (Game != null) break;
          Side = keyData == Keys.Up
            ? Math.Min(MaxSide, Side + 1)
            : Math.Max(3, Side - 1);
          SideNumPreparation();
          Invalidate();
          return true;
        case Keys.Space:
          if (Game != null) break;
          Cancellation = new CancellationTokenSource();
          Start = DateTime.Now;
          Game = new Task(DoGame, Cancellation.Token);
          Game.Start();
          return true;
      }
      return base.ProcessCmdKey(ref msg, keyData);
    }

    [DllImport("USER32.dll")]
    static extern short GetAsyncKeyState(Keys vKey);

    async void DoGame()
    {
      var r = new Random();
      var token = Cancellation.Token;
      var rotTime = DateTime.Now;
      int hole = CursorIdx;

      // Insert starting term relax
      var firstWall = Walls.Dequeue();
      firstWall.Length = 900;
      firstWall.Exist = Enumerable.Repeat(false, 100).ToArray();
      Walls.Enqueue(firstWall);

      bool relax = false;
      while (!token.IsCancellationRequested)
      {
        // Move and recycle walls
        Walls.Peek().Length -= 10;
        while (Walls.Peek().Length <= 0)
        {
          relax = !relax;
          var recycle = Walls.Dequeue();
          recycle.Length = (float)r.NextDouble() * 100 + 100;
          var wall = recycle.Exist;
          for (int i = 0; i < Side; i++)
            wall[i] = relax ? false : r.Next(2) == 1;
          wall[hole] = false;
          int lmost = hole, rmost = hole;
          while (!wall[(--lmost + Side) % Side] && lmost > hole - Side) ;
          while (!wall[++rmost % Side] && rmost < hole + Side) ;
          hole = (r.Next(lmost + 1, rmost) + Side) % Side;
          Walls.Enqueue(recycle);
        }

        // Move cursor and test collision
        if ((GetAsyncKeyState(Keys.Left) & 0x8000) != 0)
          CursorAngle = (CursorAngle + 350) % 360;
        if ((GetAsyncKeyState(Keys.Right) & 0x8000) != 0)
          CursorAngle = (CursorAngle + 10) % 360;
        if (IsColliding()) break;

        // Change rotation and beat
        if (rotTime < DateTime.Now)
        {
          var flag = DateTime.Now;
          float past = GetRotation();
          float rand = (float)r.NextDouble();
          float delta = (rand * 90 + 90) * (r.Next(2) == 0 ? -1 : 1);
          GetRotation = () => past + (float)(DateTime.Now - flag).TotalSeconds * delta;
          var interval = (float)(1 - rand * 0.6);
          GetExpansion = () =>
          {
            var e = (float)(DateTime.Now - flag).TotalSeconds % interval;
            e = (interval - e) / interval * 0.2f + 0.9f;
            return e * e;
          };
          rotTime = flag + TimeSpan.FromSeconds(r.Next(5, 10));
          var pastHue = GetHue();
          var nextHue = (float)r.NextDouble();
          GetHue = () => {
            var ratio = Math.Min(1, (float)(DateTime.Now - flag).TotalSeconds / 1.5f);
            if (ratio == 1) GetHue = () => nextHue;
            return pastHue * (1 - ratio) + nextHue * ratio;
          };
        }

        // Update
        Invalidate();
        await Task.Delay(16).ConfigureAwait(false);
      }
      // Game finished
      var lastRot = GetRotation();
      GetRotation = () => lastRot;
      Last = DateTime.Now - Start;
      if (Record < Last) Record = Last;
      Invalidate();
      MessageBox.Show($"기록: {Last}");
      SideNumPreparation();
      Game = null;
      Invalidate();
    }

    int CursorIdx { get { return (int)(CursorAngle / (360f / Side)); } }

    private bool IsColliding()
    {
      float radius = 0;
      foreach (var wall in Walls)
      {
        radius += wall.Length;
        if (radius > 120) return wall.Exist[CursorIdx];
      }
      return false;
    }
  }
}
