using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MC.Data.DataGrid.BitmapCache
{
    public sealed class BitmapCacheOptions
    {
        public int MaxMemoryMB { get; set; } = 20;

        public ImageFormat ImageFormat { get; set; }
            = ImageFormat.Png;

        public long JpegQuality { get; set; } = 90;

        public Size MaxImageSize { get; set; }
            = Size.Empty;

        public long MaxMemoryBytes
        {
            get
            {
                return (long)MaxMemoryMB * 1024 * 1024;
            }
        }
    }
}
