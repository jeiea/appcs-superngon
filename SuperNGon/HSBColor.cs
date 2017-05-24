using System;
using System.Drawing;

namespace SuperNGon
{
  ///<summary>
  ///HSBColor struct adapted for Winform
  ///</summary>
  [System.Serializable]
  public struct HSBColor
  {
    public float H;
    public float S;
    public float B;
    public float A;

    public HSBColor(float h, float s, float b, float a = 1)
    {
      H = Math.Abs(h) % 1;
      S = s;
      B = b;
      A = a;
    }

    public HSBColor(Color rgb)
    {
      A = rgb.A / 255f;

      float r = (float)rgb.R / 255;
      float g = (float)rgb.G / 255;
      float b = (float)rgb.B / 255;

      float max = Math.Max(r, Math.Max(g, b));

      if (max <= 0)
      {
        H = S = B = 0;
      }

      float min = Math.Min(r, Math.Min(g, b));
      float dif = max - min;

      if (max > min)
      {
        if (g == max)
        {
          H = (b - r) / dif * 60f + 120f;
        }
        else if (b == max)
        {
          H = (r - g) / dif * 60f + 240f;
        }
        else if (b > g)
        {
          H = (g - b) / dif * 60f + 360f;
        }
        else
        {
          H = (g - b) / dif * 60f;
        }
        if (H < 0)
        {
          H = H + 360f;
        }
      }
      else
      {
        H = 0;
      }

      H *= 1f / 360f;
      S = (dif / max) * 1f;
      B = max;
    }

    public static implicit operator HSBColor(Color rgb)
    {
      return new HSBColor(rgb);
    }

    public static HSBColor FromColor(Color rgb)
    {
      return new HSBColor(rgb);
    }

    public static Color ToColor(HSBColor hsb)
    {
      float r = hsb.B;
      float g = hsb.B;
      float b = hsb.B;
      if (hsb.S != 0)
      {
        float max = hsb.B;
        float dif = hsb.B * hsb.S;
        float min = hsb.B - dif;

        float h = hsb.H * 360f;

        if (h < 60f)
        {
          r = max;
          g = h * dif / 60f + min;
          b = min;
        }
        else if (h < 120f)
        {
          r = -(h - 120f) * dif / 60f + min;
          g = max;
          b = min;
        }
        else if (h < 180f)
        {
          r = min;
          g = max;
          b = (h - 120f) * dif / 60f + min;
        }
        else if (h < 240f)
        {
          r = min;
          g = -(h - 240f) * dif / 60f + min;
          b = max;
        }
        else if (h < 300f)
        {
          r = (h - 240f) * dif / 60f + min;
          g = min;
          b = max;
        }
        else if (h <= 360f)
        {
          r = max;
          g = min;
          b = -(h - 360f) * dif / 60 + min;
        }
        else
        {
          r = 0;
          g = 0;
          b = 0;
        }
      }

      return Color.FromArgb((int)(hsb.A * 255), Clamp255(r), Clamp255(g), Clamp255(b));
    }

    static int Clamp255(float val)
    {
      return (int)(Math.Max(0, Math.Min(1, val)) * 255);
    }

    public Color ToColor()
    {
      return ToColor(this);
    }

    public static implicit operator Color(HSBColor hsb)
    {
      return ToColor(hsb);
    }

    public override string ToString()
    {
      return "H:" + H + " S:" + S + " B:" + B;
    }

    public static HSBColor Lerp(HSBColor a, HSBColor b, float t)
    {
      float h, s;

      //check special case black (color.b==0): interpolate neither hue nor saturation!
      //check special case grey (color.s==0): don't interpolate hue!
      if (a.B == 0)
      {
        h = b.H;
        s = b.S;
      }
      else if (b.B == 0)
      {
        h = a.H;
        s = a.S;
      }
      else
      {
        if (a.S == 0)
        {
          h = b.H;
        }
        else if (b.S == 0)
        {
          h = a.H;
        }
        else
        {
          h = (a.H + b.H) % 1;
        }
        s = (a.S + b.S) / 2 * t;
      }
      return new HSBColor(h, s, (a.B + b.B) / 2 * t, (a.A + b.A) / 2 * t);
    }
  }
}
