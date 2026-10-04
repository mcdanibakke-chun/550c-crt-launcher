using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
// Original terminal glyph; no GPT knot or third-party logo. Matches the supplied SVG geometry.
class PublicIcon {
 static int Main(string[] args){if(args.Length!=1||File.Exists(args[0]))return 1;
 using(var b=new Bitmap(256,256,PixelFormat.Format32bppArgb))using(var g=Graphics.FromImage(b)){
  g.Clear(Color.Transparent);g.SmoothingMode=SmoothingMode.AntiAlias;
  using(var tile=new GraphicsPath()){tile.AddArc(10,10,80,80,180,90);tile.AddArc(166,10,80,80,270,90);tile.AddArc(166,166,80,80,0,90);tile.AddArc(10,166,80,80,90,90);tile.CloseFigure();using(var fill=new SolidBrush(Color.FromArgb(8,9,9)))g.FillPath(fill,tile);using(var edge=new Pen(Color.FromArgb(56,59,53),2))g.DrawPath(edge,tile);}
  using(var frame=new Pen(Color.FromArgb(98,103,92),3)){g.DrawLines(frame,new[]{new Point(65,40),new Point(40,40),new Point(40,65)});g.DrawLines(frame,new[]{new Point(191,40),new Point(216,40),new Point(216,65)});g.DrawLines(frame,new[]{new Point(40,191),new Point(40,216),new Point(65,216)});g.DrawLines(frame,new[]{new Point(216,191),new Point(216,216),new Point(191,216)});}
  using(var glyph=new Pen(Color.FromArgb(191,193,182),13)){glyph.StartCap=glyph.EndCap=LineCap.Square;glyph.LineJoin=LineJoin.Miter;g.DrawLines(glyph,new[]{new Point(77,91),new Point(117,128),new Point(77,165)});g.DrawLine(glyph,143,165,186,165);}
  using(var scan=new Pen(Color.FromArgb(18,191,193,182),1))for(int y=66;y<=194;y+=8){int x=y<=66||y>=194?24:y<=74||y>=186?17:y<=82||y>=178?14:y<=90||y>=170?12:y<=98||y>=162?11:10;g.DrawLine(scan,x,y,256-x,y);}
  b.Save(args[0],ImageFormat.Png);
 }return 0;}
}
