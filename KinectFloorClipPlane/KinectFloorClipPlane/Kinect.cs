using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectFloorClipPlane
{
    public class Kinect
    {
        // Kinect
        KinectSensor kinect;
        CoordinateMapper mapper;
        MultiSourceFrameReader multiReader;

        // Color
        FrameDescription colorFrameDesc;
        ColorImageFormat colorFormat = ColorImageFormat.Bgra;
        byte[] colorBuffer;

        // Depth
        FrameDescription depthFrameDesc;
        ushort[] depthBuffer;

        // FloorClipPlane
        public Vector4 FloorClipPlane
        {
            get;
            private set;
        }

        // Coodinate Mapping
        ColorSpacePoint[] depthColorPoints;
        CameraSpacePoint[] cameraPoints;

        public PointXYZRGB[] PointCloud {
            get;
            private set;
        }


        public void Start()
        {
            // Kinectの初期化
            kinect = KinectSensor.GetDefault();
            kinect.Open();

            mapper = kinect.CoordinateMapper;

            // カラー画像の情報を作成する(BGRAフォーマット)
            colorFrameDesc = kinect.ColorFrameSource.CreateFrameDescription( colorFormat );
            colorBuffer = new byte[colorFrameDesc.LengthInPixels * colorFrameDesc.BytesPerPixel];

            // Depthデータの情報を取得する
            depthFrameDesc = kinect.DepthFrameSource.FrameDescription;
            depthBuffer = new ushort[depthFrameDesc.LengthInPixels];

            // 座標変換用のバッファを生成する
            depthColorPoints = new ColorSpacePoint[depthFrameDesc.LengthInPixels];
            cameraPoints = new CameraSpacePoint[depthFrameDesc.LengthInPixels];

            PointCloud = new PointXYZRGB[depthFrameDesc.LengthInPixels];
            for ( int i = 0; i < PointCloud.Length; i++ ) {
                PointCloud[i] = new PointXYZRGB();
            }

            // フレームリーダーを開く
            multiReader = kinect.OpenMultiSourceFrameReader( FrameSourceTypes.Color | FrameSourceTypes.Depth | FrameSourceTypes.Body );

            multiReader.MultiSourceFrameArrived += multiReader_MultiSourceFrameArrived;
        }

        public void Stop()
        {
            if ( multiReader != null ) {
                multiReader.MultiSourceFrameArrived -= multiReader_MultiSourceFrameArrived;
                multiReader.Dispose();
                multiReader = null;
            }

            if ( kinect != null ) {
                kinect.Close();
                kinect = null;
            }
        }

        void multiReader_MultiSourceFrameArrived( object sender, MultiSourceFrameArrivedEventArgs e )
        {
            var multiFrame = e.FrameReference.AcquireFrame();
            if ( multiFrame == null ) {
                return;
            }

            // 各種データを取得する
            UpdateColorFrame( multiFrame );
            UpdateDepthFrame( multiFrame );
            UpdateBodyFrame( multiFrame );

            CoodinateMapping();
        }

        private void UpdateColorFrame( MultiSourceFrame multiFrame )
        {
            using ( var colorFrame = multiFrame.ColorFrameReference.AcquireFrame() ) {
                if ( colorFrame == null ) {
                    return;
                }

                // BGRAデータを取得する
                colorFrame.CopyConvertedFrameDataToArray( colorBuffer, colorFormat );
            }
        }

        private void UpdateDepthFrame( MultiSourceFrame multiFrame )
        {
            using ( var depthFrame = multiFrame.DepthFrameReference.AcquireFrame() ) {
                if ( depthFrame == null ) {
                    return;
                }

                // Depthデータを取得する
                depthFrame.CopyFrameDataToArray( depthBuffer );
            }
        }


        private void UpdateBodyFrame( MultiSourceFrame multiFrame )
        {
            using ( var bodyFrame = multiFrame.BodyFrameReference.AcquireFrame() ) {
                if ( bodyFrame == null ) {
                    return;
                }

                // 床の向きベクトルを取得する
                FloorClipPlane = bodyFrame.FloorClipPlane;
            }
        }

        private void CoodinateMapping()
        {
            // Depth座標系のカラー位置と、Depth座標系とカメラ座標系の対応を作る
            mapper.MapDepthFrameToColorSpace( depthBuffer, depthColorPoints );
            mapper.MapDepthFrameToCameraSpace( depthBuffer, cameraPoints );

            Parallel.For( 0, depthFrameDesc.LengthInPixels, i =>
            {
                PointCloud[i].Clear();

                // カラーバッファの位置
                int colorX = (int)depthColorPoints[i].X;
                int colorY = (int)depthColorPoints[i].Y;
                if ( (colorX < 0) || (colorFrameDesc.Width <= colorX) || (colorY < 0) || (colorFrameDesc.Height <= colorY) ) {
                    return;
                }

                PointCloud[i].X = cameraPoints[i].X;
                PointCloud[i].Y = cameraPoints[i].Y;
                PointCloud[i].Z = cameraPoints[i].Z;

                int colorIndex = (int)(((colorY * colorFrameDesc.Width) + colorX) * colorFrameDesc.BytesPerPixel);
                PointCloud[i].B = colorBuffer[colorIndex + 0];
                PointCloud[i].G = colorBuffer[colorIndex + 1];
                PointCloud[i].R = colorBuffer[colorIndex + 2];
            } );
        }
    }
}
