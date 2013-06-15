using Microsoft.Kinect;
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
    }
}
