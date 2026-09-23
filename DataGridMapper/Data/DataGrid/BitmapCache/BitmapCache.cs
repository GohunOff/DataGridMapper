using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace MC.Data.DataGrid.BitmapCache
{
    public sealed class BitmapCache : IDisposable
    {
        private sealed class CacheEntry
        {
            public byte[] Data { get; }

            public LinkedListNode<BitmapCacheKey> Node { get; set; }

            public CacheEntry(
                byte[] data,
                LinkedListNode<BitmapCacheKey> node)
            {
                Data = data;
                Node = node;
            }
        }

        private readonly Dictionary<BitmapCacheKey, CacheEntry> _cache =
            new Dictionary<BitmapCacheKey, CacheEntry>();

        private readonly LinkedList<BitmapCacheKey> _lru =
            new LinkedList<BitmapCacheKey>();

        private bool _disposed;

        private long _currentMemoryBytes;

        public BitmapCacheOptions Options { get; }

        public long CurrentMemoryBytes
        {
            get { return _currentMemoryBytes; }
        }

        public int Count
        {
            get { return _cache.Count; }
        }

        public BitmapCache(int maxMemoryMB)
            : this(new BitmapCacheOptions
            {
                MaxMemoryMB = maxMemoryMB
            })
        {
        }

        public BitmapCache(BitmapCacheOptions options)
        {
            Options = options
                ?? throw new ArgumentNullException(nameof(options));

            if (Options.MaxMemoryMB <= 0)
                throw new ArgumentOutOfRangeException(
                    nameof(options.MaxMemoryMB));
        }

        public bool TryGet(
            BitmapCacheKey key,
            out Bitmap bitmap)
        {
            ThrowIfDisposed();

            CacheEntry entry;

            if (!_cache.TryGetValue(key, out entry))
            {
                bitmap = null;
                return false;
            }

            // Element właśnie został użyty,
            // więc przesuwamy go na początek LRU.
            Touch(key, entry);

            bitmap = Decode(entry.Data);

            return true;
        }

        public void Set(
            BitmapCacheKey key,
            Bitmap bitmap)
        {
            ThrowIfDisposed();

            if (bitmap == null)
                throw new ArgumentNullException(nameof(bitmap));

            byte[] data = Encode(bitmap);

            // Jeżeli pojedynczy obraz jest większy
            // niż cały cache, nie przechowujemy go.
            if (data.LongLength > Options.MaxMemoryBytes)
            {
                Remove(key);
                return;
            }

            Remove(key);

            var node = _lru.AddFirst(key);

            var entry = new CacheEntry(
                data,
                node);

            _cache.Add(key, entry);

            _currentMemoryBytes += data.LongLength;

            Trim();
        }

        public void Remove(BitmapCacheKey key)
        {
            CacheEntry entry;

            if (!_cache.TryGetValue(key, out entry))
                return;

            _cache.Remove(key);
            _lru.Remove(entry.Node);

            _currentMemoryBytes -= entry.Data.LongLength;
        }

        public void Clear()
        {
            _cache.Clear();
            _lru.Clear();

            _currentMemoryBytes = 0;
        }

        private void Touch(
            BitmapCacheKey key,
            CacheEntry entry)
        {
            _lru.Remove(entry.Node);

            entry.Node = _lru.AddFirst(key);
        }

        private void Trim()
        {
            while (_currentMemoryBytes > Options.MaxMemoryBytes &&
                   _lru.Last != null)
            {
                BitmapCacheKey key = _lru.Last.Value;

                Remove(key);
            }
        }

        private byte[] Encode(Bitmap bitmap)
        {
            using (var stream = new MemoryStream())
            {
                if (Options.ImageFormat == ImageFormat.Jpeg)
                {
                    SaveJpeg(
                        bitmap,
                        stream,
                        Options.JpegQuality);
                }
                else
                {
                    bitmap.Save(
                        stream,
                        ImageFormat.Png);
                }

                return stream.ToArray();
            }
        }

        private Bitmap Decode(byte[] data)
        {
            using (var stream = new MemoryStream(data))
            using (var temp = new Bitmap(stream))
            {
                // Tworzymy niezależną bitmapę.
                return new Bitmap(temp);
            }
        }

        private static void SaveJpeg(
            Bitmap bitmap,
            Stream stream,
            long quality)
        {
            quality = Math.Max(
                0,
                Math.Min(
                    100,
                    quality));

            ImageCodecInfo codec =
                ImageCodecInfo.GetImageEncoders()
                    .First(x =>
                        x.FormatID == ImageFormat.Jpeg.Guid);

            var parameters =
                new EncoderParameters(1);

            parameters.Param[0] =
                new EncoderParameter(
                    System.Drawing.Imaging.Encoder.Quality,
                    quality);

            bitmap.Save(
                stream,
                codec,
                parameters);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(
                    nameof(BitmapCache));
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            Clear();

            _disposed = true;
        }
    }


}
