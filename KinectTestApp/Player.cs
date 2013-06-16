using Microsoft.Kinect;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace KinectTestApp
{
    class Player
    {
        Skeleton _skeleton;
        DepthImageFrame _depth;
        Image _headImage;

        public Image HeadImage { get { return _headImage; } }

        public Player(Skeleton skeleton, DepthImageFrame depth, Image headImage)
        {
            _skeleton = skeleton;
            _depth = depth;
            _headImage = headImage;
        }

        public bool AreBothHandsRaised()
        {
            var wristLeft = _skeleton.Joints[JointType.WristLeft];
            var wristRight = _skeleton.Joints[JointType.WristRight];
            var head = _skeleton.Joints[JointType.Head];

            return wristLeft.TrackingState != JointTrackingState.NotTracked &&
                wristRight.TrackingState != JointTrackingState.NotTracked &&
                head.TrackingState != JointTrackingState.NotTracked &&
                wristLeft.Position.Y > head.Position.Y &&
                wristRight.Position.Y > head.Position.Y;
        }

        public ColorImagePoint GetCameraPoint(JointType jt)
        {
            DepthImagePoint dip = _depth.MapFromSkeletonPoint(
                _skeleton.Joints[jt].Position
                );
            ColorImagePoint cip = _depth.MapToColorImagePoint(
                dip.X,
                dip.Y,
                ColorImageFormat.RgbResolution640x480Fps30
                );
            return cip;
        }

        public IEnumerable<ColorImagePoint> GetCollisions(Player[] others)
        {
            foreach (Player otherPlayer in others)
            {
                foreach (ColorImagePoint point in GetCollisions(otherPlayer))
                {
                    yield return point;
                }
            }
        }

        public IEnumerable<ColorImagePoint> GetCollisions(Player other)
        {
            if (this != other)
            {
                ColorImagePoint leftHand = GetCameraPoint(JointType.HandLeft);
                ColorImagePoint rightHand = GetCameraPoint(JointType.HandRight);

                ColorImagePoint otherHead = GetCameraPoint(JointType.Head);
                ColorImagePoint otherNeck = GetCameraPoint(JointType.ShoulderCenter);
                if (otherNeck.Y > otherHead.Y)
                {
                    int radius = otherNeck.Y - otherHead.Y;

                    int leftHandDistance = (int)Distance(leftHand, otherHead);
                    if (leftHandDistance <= radius)
                    {
                        yield return leftHand;
                    }

                    int rightHandDistance = (int)Distance(rightHand, otherHead);
                    if (rightHandDistance <= radius)
                    {
                        yield return rightHand;
                    }
                }
            }
        }

        private double Distance(ColorImagePoint pt1, ColorImagePoint pt2)
        {
            int x = pt1.X - pt2.X;
            x = x * x;

            int y = pt1.Y - pt2.Y;
            y = y * y;

            return Math.Sqrt( + y);
        }
    }
}
