using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using RT.Servers;
using RT.Util.ExtensionMethods;
using RT.Util.Json;

namespace KtaneWeb
{
    public sealed partial class KtanePropellerModule
    {
        private sealed class IconSpriteInfo
        {
            public byte[] Png;
            public Dictionary<string, JsonValue> CoordsJson;
        }
        private IconSpriteInfo _iconSpriteInfo = null;

        private HttpResponse iconSpritePng(HttpRequest req)
        {
            ensureIconSpriteInfo();
            return HttpResponse.Create(_iconSpriteInfo.Png, "image/png");
        }

        private void ensureIconSpriteInfo()
        {
            if (_iconSpriteInfo == null)
                lock (this)
                    if (_iconSpriteInfo == null)
                    {
                        const int cols = 20;   // number of icons per row
                        const int w = 32;   // width of an icon in pixels
                        const int h = 32;   // height of an icon in pixels

                        var iconFiles = new DirectoryInfo(_config.ModIconDir).EnumerateFiles("*.png", SearchOption.TopDirectoryOnly).OrderBy(file => file.Name != "blank.png").ToArray();
                        var rows = (iconFiles.Length + cols - 1) / cols;
                        var coords = new Dictionary<string, JsonValue>();

                        using (var bmp = new Bitmap(w * cols, h * rows))
                        {
                            using (var g = Graphics.FromImage(bmp))
                            {
                                for (int i = 0; i < iconFiles.Length; i++)
                                {
                                    using (var icon = new Bitmap(iconFiles[i].FullName))
                                        g.DrawImage(icon, w * (i % cols), h * (i / cols));
                                    coords.Add(Path.GetFileNameWithoutExtension(iconFiles[i].Name), new JsonList { i % cols, i / cols });
                                }
                            }
                            using (var mem = new MemoryStream())
                            {
                                bmp.Save(mem, ImageFormat.Png);
                                _iconSpriteInfo = new IconSpriteInfo { Png = mem.ToArray(), CoordsJson = coords };
                            }
                        }
                    }
        }
    }
}
