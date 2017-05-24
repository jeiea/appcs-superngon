using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperNGon
{
  public static class Utility
  {
    public static PointF Rotate(this PointF pt, double rad, PointF center = new PointF())
    {
      float cos = (float)Math.Cos(rad), sin = (float)Math.Sin(rad);
      return new PointF()
      {
        X = center.X + cos * (pt.X - center.X) - sin * (pt.Y - center.Y),
        Y = center.Y + sin * (pt.X - center.X) + cos * (pt.Y - center.Y)
      };
    }
  }
}
