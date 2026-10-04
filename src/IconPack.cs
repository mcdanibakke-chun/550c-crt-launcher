using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

// Format export only: retain generated PNG unchanged and package scaled PNG frames as ICO.
internal static class IconPack {
    static Bitmap Scaled(Image source,int size) {
        var bitmap=new Bitmap(size,size,PixelFormat.Format32bppArgb);
        using(var graphics=Graphics.FromImage(bitmap)) {
            graphics.Clear(Color.Transparent);
            graphics.CompositingMode=CompositingMode.SourceCopy;
            graphics.CompositingQuality=CompositingQuality.HighQuality;
            graphics.InterpolationMode=InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode=SmoothingMode.HighQuality;
            graphics.PixelOffsetMode=PixelOffsetMode.HighQuality;
            graphics.DrawImage(source,new Rectangle(0,0,size,size),0,0,source.Width,source.Height,GraphicsUnit.Pixel);
        }
        return bitmap;
    }
    static int Main(string[] args) {
        try {
            if(args.Length!=3) throw new ArgumentException("PNG, ICO output and evidence folder required.");
            if(File.Exists(args[1])) throw new IOException("Preserve existing icon output.");
            var sizes=new[]{16,24,32,48,64,96,128,256};
            var frames=new List<byte[]>();
            using(var source=Image.FromFile(args[0])) {
                Console.WriteLine("Source "+source.Width+"x"+source.Height+", alpha="+Image.IsAlphaPixelFormat(source.PixelFormat));
                if(source.Width!=source.Height) throw new ArgumentException("Square generated icon required.");
                Directory.CreateDirectory(args[2]);
                foreach(var size in sizes) using(var frame=Scaled(source,size)) using(var stream=new MemoryStream()) {
                    frame.Save(stream,ImageFormat.Png);frames.Add(stream.ToArray());
                    var preview=Path.Combine(args[2],"icon-"+size+".png");
                    if(File.Exists(preview)) throw new IOException("Preserve existing export preview.");
                    frame.Save(preview,ImageFormat.Png);
                }
                using(var sheet=new Bitmap(600,140)) using(var g=Graphics.FromImage(sheet)) {
                    g.Clear(Color.FromArgb(32,32,32));
                    int x=25;
                    foreach(var size in new[]{16,24,32,48,64}) using(var frame=Scaled(source,size)) {
                        g.DrawImageUnscaled(frame,x,30);
                        using(var font=new Font("Consolas",11)) g.DrawString(size+" px",font,Brushes.White,x,108);
                        x+=110;
                    }
                    sheet.Save(Path.Combine(args[2],"small-size-review.png"),ImageFormat.Png);
                }
            }
            using(var writer=new BinaryWriter(new FileStream(args[1],FileMode.CreateNew,FileAccess.Write))) {
                writer.Write((ushort)0);writer.Write((ushort)1);writer.Write((ushort)sizes.Length);
                uint offset=(uint)(6+16*sizes.Length);
                for(int i=0;i<sizes.Length;i++) {
                    writer.Write((byte)(sizes[i]==256?0:sizes[i]));writer.Write((byte)(sizes[i]==256?0:sizes[i]));
                    writer.Write((byte)0);writer.Write((byte)0);writer.Write((ushort)1);writer.Write((ushort)32);
                    writer.Write((uint)frames[i].Length);writer.Write(offset);offset+=(uint)frames[i].Length;
                }
                foreach(var frame in frames) writer.Write(frame);
            }
            Console.WriteLine("ICO exported: 16,24,32,48,64,96,128,256 PNG/32-bit frames.");return 0;
        } catch(Exception error) {Console.Error.WriteLine(error);return 1;}
    }
}
