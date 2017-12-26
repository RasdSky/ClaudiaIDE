using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;
using ClaudiaIDE.Settings;
using System.Threading;
using System.Collections;
using ClaudiaIDE.Helpers;
using System.Drawing;
using System.Drawing.Imaging;

namespace ClaudiaIDE
{
    public class GifProvider : IImageProvider
    {
        private readonly Timer _timer;
        private Setting _setting;
        private IEnumerator<BitmapSource> _bitmapImages;

        public GifProvider(Setting setting)
        {
            _setting = setting;
            _setting.OnChanged.AddEventHandler(ReloadSettings);
            _timer = new Timer(new TimerCallback(ChangeImage));
            ReloadSettings(null, null);
        }

        ~GifProvider()
        {
            if (_setting != null)
            {
                _setting.OnChanged.RemoveEventHandler(ReloadSettings);
            }
        }

        public event EventHandler NewImageAvaliable;

        private IEnumerator<BitmapSource> GetImagesFromGif()
        {
            string path = _setting.BackgroundImageAbsolutePath;
            Image gif = Image.FromFile(path);
            FrameDimension dim = new FrameDimension(gif.FrameDimensionsList[0]);
            int frames = gif.GetFrameCount(dim);
            List<BitmapSource> imgs = new List<BitmapSource>();

            for (int i = 0; i < frames; i++)
            {
                gif.SelectActiveFrame(dim, i);

                BitmapSource bitmapSou = null;
                var bitmap = new BitmapImage();
                var fileInfo = new FileInfo(path);
                if (fileInfo.Exists)
                {
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.CreateOptions = BitmapCreateOptions.None;
                    bitmap.UriSource = new Uri(path, UriKind.RelativeOrAbsolute);
                    bitmap.EndInit();
                    bitmap.Freeze();
                    if (_setting.ImageStretch == ImageStretch.None)
                    {
                        bitmap = Utils.EnsureMaxWidthHeight(bitmap, _setting.MaxWidth, _setting.MaxHeight);
                        if (bitmap.Width != bitmap.PixelWidth || bitmap.Height != bitmap.PixelHeight)
                        {
                            bitmapSou = Utils.ConvertToDpi96(bitmap);
                        }
                    }
                }
                if (bitmapSou != null)
                {
                    imgs.Add(bitmapSou);
                }
                else
                {
                    imgs.Add(bitmap);
                }
            }
            return new BitmapImageEnumerator(imgs);
        }

        public BitmapSource GetBitmap()
        {
            return _bitmapImages.Current;
        }

        private void ReloadSettings(object sender, System.EventArgs e)
        {
            if (_setting.ImageBackgroundType == ImageBackgroundType.Single)
            {
                _timer.Change(Timeout.Infinite, Timeout.Infinite);
            }
            else
            {
                _bitmapImages = GetImagesFromGif();
                ChangeImage(null);
                _timer.Change(0, (int)_setting.UpdateImageInterval.TotalMilliseconds);
            }
        }

        private void ChangeImage(object args)
        {
            if (_bitmapImages.MoveNext())
            {
                NewImageAvaliable?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                // Reached the end of the images. Loop to beginning?
                if (_setting.LoopSlideshow)
                {
                    _bitmapImages.Reset();
                    if (_bitmapImages.MoveNext())
                    {
                        NewImageAvaliable?.Invoke(this, EventArgs.Empty);
                    }
                }
            }
        }

        public ImageBackgroundType ProviderType
        {
            get
            {
                return ImageBackgroundType.GIF;
            }
        }
    }

    public class BitmapImageEnumerator : IEnumerator<BitmapSource>
    {
        private int position;
        private List<BitmapSource> bitmapImages;
        public BitmapImageEnumerator(List<BitmapSource> bitmapImages)
        {
            this.bitmapImages = bitmapImages;
            position = -1;
        }

        public BitmapSource Current
        {
            get
            {
                return bitmapImages[position];
            }
        }

        object IEnumerator.Current
        {
            get
            {
                return Current;
            }
        }

        public void Dispose() { }

        public bool MoveNext()
        {
            position++;
            return position < bitmapImages.Count;
        }

        public void Reset()
        {
            position = -1;
        }
    }
}
