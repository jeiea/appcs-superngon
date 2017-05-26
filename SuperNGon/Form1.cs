﻿using System;
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
  public partial class Form1 : Form
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
    List<SortedSet<float>> Obstacles;
    float Offset;
    Task Game;
    CancellationTokenSource Cancellation;

    class Wall
    {
      float Length;
      bool[] Exist = new bool[MaxSide];
    }

    public Form1()
    {
      ResizeRedraw = true;
      DoubleBuffered = true;
      Obstacles = new List<SortedSet<float>>();
      for (int i = 0; i < 100; i++)
        Obstacles.Add(new SortedSet<float>());
      Preparation();
      InitializeComponent();
    }

    private void Preparation()
    {
      foreach (var list in Obstacles) list.Clear();
      CursorAngle = (float)Math.Ceiling(60.0 / 360 * Side) * 360 / Side - 180 / Side;
      GetRotation = () => 180 / Side;
      GetExpansion = () => 1;
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
      var shineBrush = new SolidBrush(new HSBColor(1 / 3f, 1, .9f));
      var briBack = new SolidBrush(new HSBColor(1 / 3f, 1, .7f));
      var drkBack = new SolidBrush(new HSBColor(1 / 3f, 1, .3f));
      var whiteBrush = new SolidBrush(Color.White);
      var shinePen = new Pen(shineBrush, 3) { LineJoin = LineJoin.Round };
      var darkPen = new Pen(drkBack, 3) { LineJoin = LineJoin.Bevel };
      #endregion

      #region Draw background
      var outop = new PointF(0, -20000);
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
        var midBack = new SolidBrush(new HSBColor(1 / 3f, 1, .5f));
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
        foreach (var px in Obstacles[i].Reverse())
        {
          var wall = new PointF(pt.X * (px - Offset), pt.Y * (px - Offset));
          walls.Add(new PointF());
          walls.Add(wall);
          walls.Add(wall.Rotate(angle));
        }
      }
      if (walls.Count > 0)
        g.FillPolygon(shineBrush, walls.ToArray());
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
          Preparation();
          Invalidate();
          return true;
        case Keys.Space:
          if (Game != null) break;
          Preparation();
          Cancellation = new CancellationTokenSource();
          Start = DateTime.Now;
          Game = new Task(DoGame, Cancellation.Token);
          Game.Start();
          return true;
      }
      return base.ProcessCmdKey(ref msg, keyData);
    }

    [DllImport("USER32.dll")]
    static extern short GetKeyState(Keys nVirtKey);

    async void DoGame()
    {
      var r = new Random();
      var token = Cancellation.Token;
      var rotTime = DateTime.Now;
      while (!token.IsCancellationRequested)
      {
        // Generate walls
        for (int i = Obstacles.Sum(x => x.Count); i < 6; i++)
        {
          float beg, end;
          var col = Obstacles[r.Next(0, Side)];
          do { beg = (float)r.NextDouble() * 500 + 1000 + Offset; } while (col.Contains(beg));
          do { end = (float)r.NextDouble() * 400 + 100 + beg; } while (col.Contains(end));
          col.Add(beg);
          col.Add(end);
        }
        // Move and delete walls
        Offset += 20;
        foreach (var col in Obstacles)
        {
          col.GetViewBetween(float.MinValue, Offset).ToArray()
            .Select(x => col.Remove(x)).ToArray();
        }
        // Move cursor
        if ((GetKeyState(Keys.Left) & 0x8000) != 0)
          CursorAngle = (CursorAngle + 350) % 360;
        if ((GetKeyState(Keys.Right) & 0x8000) != 0)
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
        }
        // Update
        Invalidate();
        await Task.Delay(16);
      }
      // Game finished
      var lastRot = GetRotation();
      GetRotation = () => lastRot;
      Last = DateTime.Now - Start;
      if (Record < Last) Record = Last;
      Invalidate();
      MessageBox.Show($"기록: {Last}");
      Game = null;
      Preparation();
      Invalidate();
    }

    private bool IsColliding()
    {
      var stepping = Obstacles[(int)(CursorAngle / (360f / Side))];
      var filled = stepping.Count % 2 == 1;
      foreach (var wall in stepping)
      {
        if (wall - Offset > 130) return filled;
        filled = !filled;
      }
      return false;
    }
  }
}
