using SharpGL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
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

namespace KinectFloorClipPlane
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        Kinect kinect = new Kinect();

        Matrix4x4 matrix;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded( object sender, RoutedEventArgs e )
        {
            kinect.Start();
        }

        private void OpenGLControl_OpenGLInitialized( object sender, SharpGL.SceneGraph.OpenGLEventArgs args )
        {
            var gl = args.OpenGL;

            gl.Enable( OpenGL.GL_DEPTH_TEST );
        }

        private void OpenGLControl_OpenGLDraw( object sender, SharpGL.SceneGraph.OpenGLEventArgs args )
        {
            var gl = args.OpenGL;

            gl.Clear( OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT );
            gl.ClearColor( 1, 1, 1, 1);
            gl.LoadIdentity();

            // 座標系の表示
            {
                gl.LineWidth(5);
                gl.Begin( OpenGL.GL_LINES );
                gl.Color( 1.0f, 0, 0 );
                gl.Vertex( 0, 0, 0 );
                gl.Vertex( 1, 0, 0 );
                gl.End();

                gl.Begin( OpenGL.GL_LINES );
                gl.Color( 0, 1.0f, 0 );
                gl.Vertex( 0, 0, 0 );
                gl.Vertex( 0, 1, 0 );
                gl.End();

                gl.Begin( OpenGL.GL_LINES );
                gl.Color( 0, 0, 1.0f );
                gl.Vertex( 0, 0, 0 );
                gl.Vertex( 0, 0, 1 );
                gl.End();
            }

            // 床からの回転matrixを作成
            {
                // http://gamedev.stackexchange.com/questions/80310/transform-world-space-using-kinect-floorclipplane-to-move-origin-to-floor-while
                var vector = new Vector4(
                    kinect.FloorClipPlane.X, 
                    kinect.FloorClipPlane.Y, 
                    kinect.FloorClipPlane.Z, 
                    kinect.FloorClipPlane.W );

                var yNew = new Vector3( vector.X, vector.Y, vector.Z );
                var zNew = new Vector3( 0, 1, -vector.Y / vector.Z );
                zNew = Vector3.Normalize( zNew );
                var xNew = Vector3.Cross( yNew, zNew );

                var rotation = new Matrix4x4(
                    xNew.X, yNew.X, zNew.X, 0,
                    xNew.Y, yNew.Y, zNew.Y, 0,
                    xNew.Z, yNew.Z, zNew.Z, 0,
                    0, 0, 0, 1
                    );

                var translation = Matrix4x4.CreateTranslation( 0, vector.W, 0 );

                // 回転、移動の順
                matrix = rotation * translation;
            }


            // 点群の表示
            {
                gl.Begin( OpenGL.GL_POINTS );

                foreach ( var point in kinect.PointCloud ) {
                    if ( (point.X == 0) && (point.Y == 0) && (point.Z == 0) ) {
                        continue;
                    }

                    Vector3 v = new Vector3( point.X, point.Y, point.Z );
                    if ( IsFloorClipPlane.IsChecked == true ) {
                        v = Vector3.Transform( v, matrix );
                    }

                    gl.Color( point.R, point.G, point.B );
                    gl.Vertex( v.X, v.Y, v.Z );
                }

                gl.End();
            }

            gl.Flush();
        }

        private void OpenGLControl_Resized( object sender, SharpGL.SceneGraph.OpenGLEventArgs args )
        {
            var gl = args.OpenGL;

            gl.MatrixMode( OpenGL.GL_PROJECTION );
            gl.LoadIdentity();

            gl.Perspective( 45.0f, (float)gl.RenderContextProvider.Width / (float)gl.RenderContextProvider.Height, 0.1f, 100.0f );

            // ななめ
            //gl.LookAt(
            //    1.0f, 0.1f, -1.0f,
            //    0.0f, 0.0f, 1.0f,
            //    0, 1, 0 );

            // 真横
            gl.LookAt(
                4.0f, 0.0f, 0.0f,
                0.0f, 0.0f, 0.0f,
                0, 1, 0 );

            gl.MatrixMode( OpenGL.GL_MODELVIEW );
        }
    }
}
