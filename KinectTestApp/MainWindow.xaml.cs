using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;

namespace KinectTestApp
{
    public partial class MainWindow : Window
    {
        const int SkeletonCount = 6;
        Skeleton[] _allSkeletons = new Skeleton[SkeletonCount];

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.kinectSensorChooser.KinectSensorChanged += kinectSensorChooser_KinectSensorChanged;
        }

        void kinectSensorChooser_KinectSensorChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            KinectSensor oldSensor = (KinectSensor)e.OldValue;
            if (oldSensor != null)
            {
                StopKinect(oldSensor);
            }

            KinectSensor newSensor = (KinectSensor)e.NewValue;
            if (newSensor != null)
            {
                //newSensor.DepthStream.Range = DepthRange.Near;
                newSensor.ColorStream.Enable();
                newSensor.DepthStream.Enable();
                newSensor.SkeletonStream.Enable();
                newSensor.AllFramesReady += newSensor_AllFramesReady;
                try
                {
                    newSensor.Start();
                    newSensor.ElevationAngle = 15;
                }
                catch (System.IO.IOException)
                {
                    // Another app is trying to read its data
                    this.kinectSensorChooser.AppConflictOccurred();
                }
            }
        }

        void StopKinect(KinectSensor sensor)
        {
            if (sensor != null)
            {
                sensor.Stop();
                if (sensor.AudioSource != null)
                {
                    sensor.AudioSource.Stop();
                }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            StopKinect(kinectSensorChooser.Kinect);
        }

        void newSensor_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            if (!this.kinectSensorChooser.Kinect.IsRunning)
            {
                return;
            }

            /*using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
            {
                if (depthFrame == null)
                {
                    return;
                }

                byte[] pixels = GenerateColorFromDepth(depthFrame);
                int stride = depthFrame.Width * 4;
                this.imageDisplay.Source = BitmapSource.Create(
                    depthFrame.Width,
                    depthFrame.Height,
                    96,
                    96,
                    PixelFormats.Bgr32,
                    null,
                    pixels,
                    stride
                    );
            }*/

            using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
            {
                if (colorFrame == null)
                {
                    return;
                }

                byte[] pixels = new byte[colorFrame.PixelDataLength];
                colorFrame.CopyPixelDataTo(pixels);

                // Stride - bits needed per row (BGR+empty 32)
                int stride = colorFrame.Width * 4;
                this.imageDisplay.Source = BitmapSource.Create(
                    colorFrame.Width,
                    colorFrame.Height,
                    96,
                    96,
                    PixelFormats.Bgr32,
                    null,
                    pixels,
                    stride
                    );
            }

            Skeleton skeleton = GetSkeletonFromFrame(e);
            if (skeleton == null)
            {
                return;
            }
            GetCameraPoint(skeleton, e);
        }

        byte[] GenerateColorFromDepth(DepthImageFrame depthFrame)
        {
            short[] rawDepthData = new short[depthFrame.PixelDataLength];
            depthFrame.CopyPixelDataTo(rawDepthData);

            byte[] pixels = new byte[depthFrame.Height * depthFrame.Width * 4];

            const int BlueIndex = 0;
            const int GreenIndex = 1;
            const int RedIndex = 2;

            int depthRangeMin = 800;
            int depthRangeMax = 4000;
            if (this.kinectSensorChooser.Kinect.DepthStream.Range == DepthRange.Near)
            {
                depthRangeMin = 500;
                depthRangeMax = 3000;
            }

            for (int depthIndex = 0; depthIndex < rawDepthData.Length; depthIndex++)
            {
                int colorIndex = depthIndex * 4;
                int depth = rawDepthData[depthIndex] >> DepthImageFrame.PlayerIndexBitmaskWidth;
                byte intensity = IntensityFromDepth(depth, depthRangeMin, depthRangeMax);
                pixels[colorIndex + BlueIndex] = intensity;
                pixels[colorIndex + GreenIndex] = intensity;
                pixels[colorIndex + RedIndex] = intensity;
            }

            return pixels;
        }

        static byte IntensityFromDepth(int depth, int min, int max)
        {
            float adjusted = Math.Max(depth, min);
            adjusted = Math.Min(depth, max);

            float normalized = (float)adjusted / (float)max;
            return (byte)(255 * normalized);
        }

        Skeleton GetSkeletonFromFrame(AllFramesReadyEventArgs e)
        {
            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame == null)
                {
                    return null;
                }

                skeletonFrame.CopySkeletonDataTo(_allSkeletons);

                Skeleton first = _allSkeletons.FirstOrDefault(s =>
                {
                    return s.TrackingState == SkeletonTrackingState.Tracked;
                });

                return first;
            }
        }

        void GetCameraPoint(Skeleton skeleton, AllFramesReadyEventArgs e)
        {
            using (DepthImageFrame depth = e.OpenDepthImageFrame())
            {
                if (depth == null)
                {
                    return;
                }

                DepthImagePoint headDepthPoint = depth.MapFromSkeletonPoint(
                    skeleton.Joints[JointType.Head].Position
                    );
                DepthImagePoint leftHandDepthPoint = depth.MapFromSkeletonPoint(
                    skeleton.Joints[JointType.HandLeft].Position
                    );
                DepthImagePoint rightHandDepthPoint = depth.MapFromSkeletonPoint(
                    skeleton.Joints[JointType.HandRight].Position
                    );

                ColorImagePoint headColorPoint = depth.MapToColorImagePoint(
                    headDepthPoint.X,
                    headDepthPoint.Y,
                    ColorImageFormat.RgbResolution640x480Fps30
                    );
                ColorImagePoint leftHandColorPoint = depth.MapToColorImagePoint(
                    leftHandDepthPoint.X,
                    leftHandDepthPoint.Y,
                    ColorImageFormat.RgbResolution640x480Fps30
                    );
                ColorImagePoint rightHandColorPoint = depth.MapToColorImagePoint(
                    rightHandDepthPoint.X,
                    rightHandDepthPoint.Y,
                    ColorImageFormat.RgbResolution640x480Fps30
                    );

                CameraPosition(circle1, headColorPoint, headDepthPoint);
                CameraPosition(circle2, leftHandColorPoint, leftHandDepthPoint);
                CameraPosition(circle3, rightHandColorPoint, rightHandDepthPoint);
            }
        }

        void CameraPosition(
            Ellipse circle,
            ColorImagePoint colorPoint,
            DepthImagePoint depthPoint
            )
        {
            // Prefer Canvas.SetLeft, but that doesn't work

            double left = colorPoint.X - circle.Width / 2;
            double top = colorPoint.Y - circle.Height / 2;
            circle.Margin = new Thickness(left, top, 0, 0);

            // min = 800, max = 4000
            int depth = Math.Max(depthPoint.Depth, 800);
            depth = Math.Min(depth, 4000);
            double normalized = 1.0 - ((double)depth / (4000.0 - 800.0));
            int diameter = 16 + (int)(normalized * (64 - 16));
            circle.Width = diameter;
            circle.Height = diameter;
        }
    }
}
