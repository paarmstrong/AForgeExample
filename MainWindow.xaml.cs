using AForge;
using AForge.Imaging;
using AForge.Imaging.Filters;
using AForge.Math.Geometry;
using AForge.Video.DirectShow;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AForgeExample
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteObject([In] IntPtr hObject);

        private FilterInfoCollection VideoCaptureDevices;
        private VideoCaptureDevice VideoDevice;

        public MainWindow()
        {
            InitializeComponent();

            VideoCaptureDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            foreach (var info in VideoCaptureDevices)
            {
                sourcesComboBox.Items.Add(info);
            }

            sourcesComboBox.SelectedIndex = 0;
        }

        private void startButton_Click(object sender, RoutedEventArgs e)
        {
            VideoDevice = new VideoCaptureDevice(VideoCaptureDevices[sourcesComboBox.SelectedIndex].MonikerString);
            VideoDevice.NewFrame += VideoDevice_NewFrame;
            VideoDevice.Start();
        }

        private void stopButton_Click(object sender, RoutedEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                videoImage.Source = null;
                normalVideoImage.Source = null;
            });

            VideoDevice.SignalToStop();
        }

        private void VideoDevice_NewFrame(object sender, AForge.Video.NewFrameEventArgs eventArgs)
        {
            var device = (VideoCaptureDevice)sender;

            if (device.IsRunning)
            {
                this.Dispatcher.Invoke(() =>
                {
                    videoImage.Source = ProcessRectangeImageSourceForBitmap((Bitmap)eventArgs.Frame.Clone());
                    normalVideoImage.Source = ImageSourceForBitmap((Bitmap)eventArgs.Frame.Clone());
                });
            }
        }

        private void snapshotButton_Click(object sender, RoutedEventArgs e)
        {
            if (VideoDevice.IsRunning)
            {
                var encoder = new PngBitmapEncoder();

                WriteableBitmap wbitmap = new WriteableBitmap((BitmapSource)normalVideoImage.Source);

                BitmapFrame frame = BitmapFrame.Create(wbitmap);
                encoder.Frames.Add(frame);

                using (var stream = File.Create(DateTime.Now.Ticks + ".png"))
                {
                    encoder.Save(stream);
                }
            }
        }

        private void calibrateButton_Click(object sender, RoutedEventArgs e)
        {
            Bitmap bmp = BitmapFromSource((BitmapSource)normalVideoImage.Source);
            int rectangleWidth = bmp.Width / 4;
            int rectangleHeight = bmp.Height / 4;

            BitmapData bitmapData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadWrite, bmp.PixelFormat);
            var filter = new Crop(new Rectangle(rectangleWidth + (rectangleWidth / 2), rectangleHeight + (rectangleHeight / 2), rectangleWidth, rectangleHeight));

            var hslBitmap = filter.Apply(bitmapData);

            var stats = new ImageStatistics(hslBitmap);

            redTextBox.Text = Convert.ToInt32(stats.Red.Median).ToString();
            greenTextBox.Text = Convert.ToInt32(stats.Green.Median).ToString();
            blueTextBox.Text = Convert.ToInt32(stats.Blue.Median).ToString();
        }

        private static Bitmap BitmapFromSource(BitmapSource bitmapsource)
        {
            Bitmap bitmap;
            using (var outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapsource));
                enc.Save(outStream);
                bitmap = new Bitmap(outStream);
            }
            return bitmap;
        }

        private ImageSource ImageSourceForBitmap(Bitmap bmp)
        {
            int rectangleWidth = bmp.Width / 4;
            int rectangleHeight = bmp.Height / 4;

            BitmapData bitmapData = bmp.LockBits(new Rectangle(rectangleWidth + (rectangleWidth / 2), rectangleHeight + (rectangleHeight / 2), rectangleWidth, rectangleHeight), ImageLockMode.ReadWrite, bmp.PixelFormat);

            AForge.Imaging.Drawing.Rectangle(bitmapData, new Rectangle(0, 0, rectangleWidth, rectangleHeight), System.Drawing.Color.Red);

            bmp.UnlockBits(bitmapData);

            var handle = bmp.GetHbitmap();
            try
            {
                return Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            finally { DeleteObject(handle); }
        }

        private ImageSource ProcessRectangeImageSourceForBitmap(Bitmap bmp)
        {
            int rectangleWidth = bmp.Width / 4;
            int rectangleHeight = bmp.Height / 4;

            BitmapData bitmapData = bmp.LockBits(new Rectangle(rectangleWidth + (rectangleWidth / 2), rectangleHeight + (rectangleHeight / 2), rectangleWidth, rectangleHeight), ImageLockMode.ReadWrite, bmp.PixelFormat);

            EuclideanColorFiltering filter = new EuclideanColorFiltering();
            filter.CenterColor = new RGB(byte.Parse(redTextBox.Text), byte.Parse(greenTextBox.Text), byte.Parse(blueTextBox.Text));
            filter.Radius = short.Parse(radiusTextBox.Text);
            filter.ApplyInPlace(bitmapData);

            bmp.UnlockBits(bitmapData);

            var handle = bmp.GetHbitmap();
            try
            {
                return Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            finally { DeleteObject(handle); }
        }

        private ImageSource ProcessImageSourceForBitmap(Bitmap bmp)
        {
            BitmapData bitmapData = bmp.LockBits(
                new Rectangle(0, 0, bmp.Width, bmp.Height),
                ImageLockMode.ReadWrite, bmp.PixelFormat);

            EuclideanColorFiltering filter = new EuclideanColorFiltering();
            filter.CenterColor = new RGB(byte.Parse(redTextBox.Text), byte.Parse(greenTextBox.Text), byte.Parse(blueTextBox.Text));
            filter.Radius = short.Parse(radiusTextBox.Text);
            filter.ApplyInPlace(bitmapData);

            BlobCounter blobCounter = new BlobCounter();

            blobCounter.FilterBlobs = true;
            blobCounter.MinHeight = 5;
            blobCounter.MinWidth = 5;
            blobCounter.MaxHeight = bitmapData.Width-1;
            blobCounter.MaxWidth = bitmapData.Width-1;

            blobCounter.ProcessImage(bitmapData);
            Blob[] blobs = blobCounter.GetObjectsInformation();
            bmp.UnlockBits(bitmapData);

            SimpleShapeChecker shapeChecker = new SimpleShapeChecker();

            Graphics g = Graphics.FromImage(bmp);
            System.Drawing.Pen redPen = new System.Drawing.Pen(System.Drawing.Color.Red, 5);
            System.Drawing.Pen yellowPen = new System.Drawing.Pen(System.Drawing.Color.Yellow, 5);
            System.Drawing.Pen greenPen = new System.Drawing.Pen(System.Drawing.Color.Green, 5);
            System.Drawing.Pen bluePen = new System.Drawing.Pen(System.Drawing.Color.Blue, 5);

            for (int i = 0, n = blobs.Length; i < n; i++)
            {
                List<IntPoint> edgePoints =
                    blobCounter.GetBlobsEdgePoints(blobs[i]);

                AForge.Point center;
                float radius;

                if (shapeChecker.IsCircle(edgePoints, out center, out radius))
                {
                    g.DrawEllipse(yellowPen,
                        (float)(center.X - radius), (float)(center.Y - radius),
                        (float)(radius * 2), (float)(radius * 2));
                }
                else
                {
                    List<IntPoint> corners;

                    if (shapeChecker.IsQuadrilateral(edgePoints, out corners))
                    {
                        if (shapeChecker.CheckPolygonSubType(corners) == PolygonSubType.Rectangle)
                        {
                            g.DrawPolygon(greenPen, ToPointsArray(corners));
                        }
                        else
                        {
                            g.DrawPolygon(bluePen, ToPointsArray(corners));
                        }
                    }
                    else
                    {
                        corners = PointsCloud.FindQuadrilateralCorners(edgePoints);
                        g.DrawPolygon(redPen, ToPointsArray(corners));
                    }
                }
            }

            redPen.Dispose();
            greenPen.Dispose();
            bluePen.Dispose();
            yellowPen.Dispose();
            g.Dispose();

            var handle = bmp.GetHbitmap();
            try
            {
                return Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            finally { DeleteObject(handle); }
        }

        private System.Drawing.Point[] ToPointsArray(List<IntPoint> points)
        {
            return points.Select(p => new System.Drawing.Point(p.X, p.Y)).ToArray();
        }
    }
}
