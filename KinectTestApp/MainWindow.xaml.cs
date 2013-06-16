using Microsoft.Kinect;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System.Threading;
using HeadAnimation;

namespace KinectTestApp
{
    public partial class MainWindow : Window
    {
        private Stream audioStream;
        private bool reading;
        private Thread readingThread;
        private int currentFrame = 0;

        const int SkeletonCount = 6;
        Skeleton[] _allSkeletons = new Skeleton[SkeletonCount];
        Image[] _headImages = null;

        ImageSource[][] animationFrames = new ImageSource[][] {
            new ImageSource[] {
                new BitmapImage(new Uri("images/pirate4.png", UriKind.Relative)),
                new BitmapImage(new Uri("images/pirate3.png", UriKind.Relative)),
                new BitmapImage(new Uri("images/pirate2.png", UriKind.Relative)),
                new BitmapImage(new Uri("images/pirate1.png", UriKind.Relative)),
            },
            new ImageSource[] {
                new BitmapImage(new Uri("images/frankenstein4.png", UriKind.Relative)),
                new BitmapImage(new Uri("images/frankenstein3.png", UriKind.Relative)),
                new BitmapImage(new Uri("images/frankenstein2.png", UriKind.Relative)),
                new BitmapImage(new Uri("images/frankenstein1.png", UriKind.Relative)),
            }
        };

        public MainWindow()
        {
            InitializeComponent();

            _headImages = new Image[2];
            _headImages[0] = this.imageHead1;
            _headImages[1] = this.imageHead2;

            _headImages[0].Source = animationFrames[0][0];
            _headImages[1].Source = animationFrames[1][0];

            this.collisionImage1.Source = new BitmapImage(
                new Uri(".\\images\\POW.png", UriKind.Relative)
                );
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

                this.reading = true;
                this.audioStream = newSensor.AudioSource.Start();

                this.readingThread = new Thread(AudioReadingThread);
                this.readingThread.Start();
            }
        }

        private void UpdateAnimation(float energy)
        {
            ImageSource[] frames = animationFrames[0];

            int index = 0;

            if (energy <= 0.1)
            {
                index = 0;
            }
            else if (energy >= 0.9)
            {
                index = frames.Length - 1;
            }
            else
            {
                index = (int)((energy - 0.2) * (frames.Length - 1)) + 1;
            }

            if (index != currentFrame)
            {
                currentFrame = index;
                _headImages[0].Source = frames[currentFrame];
            }
        }

        private void AudioReadingThread()
        {
            try
            {
                const int AudioPollingInterval = 33;
                const int SamplesPerMillisecond = 16;
                const int BytesPerSample = 2;

                byte[] audioBuffer = new byte[AudioPollingInterval * SamplesPerMillisecond * BytesPerSample];

                LipSync lipSync = new LipSync(1, 1.0 / 30.0);
                lipSync.OnMouthChanged += delegate(float normalizedEnergy)
                {
                    Dispatcher.BeginInvoke(
                        new Action(
                            () =>
                            {
                                UpdateAnimation(normalizedEnergy);
                            }
                        )
                    );
                };

                AudioEnergyFilter audioFilter = new AudioEnergyFilter(16000, 1, 30, 0);
                audioFilter.OnAudioEnergyAvailable += delegate(double time, float normalizedEnergy)
                {
                    lipSync.ProcessEnergy(normalizedEnergy);
                };

                while (this.reading)
                {
                    int readCount = audioStream.Read(audioBuffer, 0, audioBuffer.Length);
                    audioFilter.ProcessAudio(audioBuffer, readCount);
                }
            }
            catch (Exception)
            {
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

                this.reading = false;
                if (readingThread.IsAlive)
                {
                    readingThread.Interrupt();
                    readingThread.Join();
                }

                readingThread = null;
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

            // 
            // Render captured image
            // 
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

            // 
            // Render players
            // 
            using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
            {
                if (depthFrame != null)
                {
                    using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
                    {
                        Player[] players = GetPlayers(depthFrame, skeletonFrame);
                        if (players != null)
                        {
                            foreach (Player player in players)
                            {
                                ColorImagePoint headPoint = player.GetCameraPoint(JointType.Head);
                                ColorImagePoint neckPoint = player.GetCameraPoint(JointType.ShoulderCenter);
                                CameraPositionImage(player.HeadImage, headPoint, neckPoint);
                                ColorImagePoint collision = player.GetCollisions(players).FirstOrDefault();
                                if (collision.X == 0 && collision.Y == 0)
                                {
                                    collisionImage1.Visibility = Visibility.Collapsed;
                                }
                                else
                                {
                                    collisionImage1.Visibility = Visibility.Visible;
                                    CameraPositionImage(collisionImage1, collision);
                                }
                            }
                        }
                    }
                }
            }
        }

        Player[] GetPlayers(DepthImageFrame depth, SkeletonFrame skeletonFrame)
        {
            if (depth == null || skeletonFrame == null)
            {
                return null;
            }

            skeletonFrame.CopySkeletonDataTo(_allSkeletons);
            int trackedCount = _allSkeletons.Count(s =>
            {
                return s.TrackingState == SkeletonTrackingState.Tracked;
            });

            if (trackedCount < 1)
            {
                return new Player[0];
            }

            if (_headImages.Length < trackedCount)
            {
                throw new IndexOutOfRangeException("Not enough head images for number of tracked skeletons");
            }

            Player[] players = new Player[trackedCount];
            int playerIndex = 0;
            for (int i = 0; i < _allSkeletons.Length; i++)
            {
                if (_allSkeletons[i].TrackingState == SkeletonTrackingState.Tracked)
                {
                    players[playerIndex] = new Player(_allSkeletons[i], depth, _headImages[playerIndex]);
                    playerIndex++;
                }
            }

            return players;
        }

        void CameraPositionImage(
            Image img,
            ColorImagePoint headColorPoint,
            ColorImagePoint neckColorPoint
            )
        {
            if (neckColorPoint.Y <= headColorPoint.Y)
            {
                img.Visibility = Visibility.Collapsed;
            }
            else
            {
                img.Visibility = Visibility.Visible;

                int headHeight = 2 * (neckColorPoint.Y - headColorPoint.Y);
                img.Height = headHeight;
                img.Width = headHeight;

                double left = headColorPoint.X - img.Width / 2;
                double top = headColorPoint.Y - img.Height / 2;
                img.Margin = new Thickness(left, top, 0, 0);
            }
        }

        void CameraPositionImage(
            Image img,
            ColorImagePoint headColorPoint
            )
        {
            img.Visibility = Visibility.Visible;

            double left = headColorPoint.X - img.Width / 2;
            double top = headColorPoint.Y - img.Height / 2;
            img.Margin = new Thickness(left, top, 0, 0);
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
    }
}
