using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MC.Data.DataGrid.BitmapCache
{
    public readonly struct BitmapCacheKey :
        IEquatable<BitmapCacheKey>
    {
        public int DataId { get; }

        public int Width { get; }

        public int Height { get; }

        public Guid FormatId { get; }

        public long Quality { get; }

        public BitmapCacheKey(
            int dataId,
            Size size,
            ImageFormat format,
            long quality)
        {
            DataId = dataId;

            Width = size.Width;
            Height = size.Height;

            FormatId = format.Guid;

            Quality = quality;
        }

        public bool Equals(BitmapCacheKey other)
        {
            return DataId == other.DataId &&
                   Width == other.Width &&
                   Height == other.Height &&
                   FormatId == other.FormatId &&
                   Quality == other.Quality;
        }

        public override bool Equals(object obj)
        {
            return obj is BitmapCacheKey &&
                   Equals((BitmapCacheKey)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = DataId;

                hash = hash * 397 + Width;
                hash = hash * 397 + Height;
                hash = hash * 397 + FormatId.GetHashCode();
                hash = hash * 397 + Quality.GetHashCode();

                return hash;
            }
        }
    }
}
