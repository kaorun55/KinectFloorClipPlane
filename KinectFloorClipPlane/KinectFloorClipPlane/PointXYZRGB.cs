using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectFloorClipPlane
{
    public struct PointXYZRGB
    {
        public float X;
        public float Y;
        public float Z;

        public byte R;
        public byte G;
        public byte B;

        public void Clear()
        {
            X = Y = Z = 0;
            R = G = B = 0;
        }
    }
}
