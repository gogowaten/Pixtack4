using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

// 2025WPF/20250617 at main · gogowaten/2025WPF
// https://github.com/gogowaten/2025WPF/tree/main/20250617
// より改変

namespace Pixtack4
{
    public class FreehandGrid : Grid
    {
        public List<PPolyline> MyListOfPPolyline { get; set; } = [];
        public Polyline MyPolyline { get; set; } = null!;
        private bool IsDrawing;

        public FreehandGrid()
        {
            MouseLeftButtonDown += (s, e) => { DrawBegin(e); };// クリックで描画開始
            MouseMove += (s, e) => { Drawing(e); };// ドラッグ移動、描画
            PreviewMouseLeftButtonUp += (s, e) => { DrawEnd(); };// 描画完了
            MouseLeave += (s, e) => { DrawEnd(); };// 描画完了
            MouseRightButtonDown += (s, e) => { RemoveLastDraw(); };// 右クリック時、最後に追加した図形を削除

            Background = new SolidColorBrush(Color.FromArgb(20, 0, 0, 0));


            MyPolyline = MakePolyline();
            SetZIndex(MyPolyline, 1);
            Children.Add(MyPolyline);
            MyPolyline.Visibility = Visibility.Collapsed;
        }


        // 全部削除
        public void DrawClear()
        {
            if (IsDrawing) { DrawEnd(); }
            while (MyListOfPPolyline.Count > 0)
            {
                var temp = MyListOfPPolyline[^1];
                Children.Remove(temp.MyPolyline);
                _ = MyListOfPPolyline.Remove(temp);
            }
        }


        // 最後に追加した図形を削除
        private void RemoveLastDraw()
        {
            if (!IsDrawing && MyListOfPPolyline.Count > 0)
            {
                var temp = MyListOfPPolyline[^1];// リストの末尾の要素を
                Children.Remove(temp.MyPolyline);// 子要素群から削除
                _ = MyListOfPPolyline.Remove(temp);// リストからも削除
            }
        }

        #region 描画

        // 描画開始
        private void DrawBegin(MouseButtonEventArgs e)
        {
            IsDrawing = true;
            MyPolyline.Visibility = Visibility.Visible;
            MyPolyline.Points.Clear();
            MyPolyline.Points.Add(e.GetPosition(this));
        }

        // 描画
        private void Drawing(MouseEventArgs e)
        {
            if (IsDrawing)
            {
                var ima = e.GetPosition(this);
                // マイナス座標になったら描画(追加)を中止
                if (ima.X < 0 || ima.Y < 0)
                {
                    DrawEnd();
                }
                // 頂点追加
                else
                {
                    MyPolyline.Points.Add(ima);
                }
            }
        }

        // 描画完了
        private void DrawEnd()
        {
            if (IsDrawing && MyPolyline.Points.Count > 0)
            {
                IsDrawing = false;
                PPolyline pp = new();
                MyListOfPPolyline.Add(pp);
                pp.MyOriginPoints = MyPolyline.Points.Clone();

                Children.Add(pp.MyPolyline);

                MyBind(pp);
            }

            IsDrawing = false;
            MyPolyline.Visibility = Visibility.Collapsed;
            MyPolyline.Points.Clear();

            void MyBind(PPolyline pp)
            {
                var mb = new MultiBinding() { Converter = new MyConvMage() };
                mb.Bindings.Add(new Binding() { Source = pp, Path = new PropertyPath(PPolyline.MyOriginPointsProperty) });
                mb.Bindings.Add(new Binding() { Source = this, Path = new PropertyPath(MyIntervalProperty) });
                BindingOperations.SetBinding(pp, PPolyline.MyPointsProperty, mb);

                BindingOperations.SetBinding(pp.MyPolyline, Shape.StrokeThicknessProperty, new Binding() { Source = this, Path = new PropertyPath(MyStrokeThicknessProperty) });
                BindingOperations.SetBinding(pp.MyPolyline, Shape.StrokeProperty, new Binding() { Source = this, Path = new PropertyPath(MyStrokeProperty) });

            }
        }

        #endregion 描画

        private Polyline MakePolyline()
        {
            Polyline polyline = new()
            {
                Stroke = Brushes.Gray,
                StrokeLineJoin = PenLineJoin.Round,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
            };

            polyline.SetBinding(Shape.StrokeThicknessProperty, new Binding() { Source = this, Path = new PropertyPath(MyStrokeThicknessProperty) });


            return polyline;
        }






        #region 依存関係プロパティ



        // 頂点間隔
        public int MyInterval
        {
            get { return (int)GetValue(MyIntervalProperty); }
            set { SetValue(MyIntervalProperty, value); }
        }
        public static readonly DependencyProperty MyIntervalProperty =
            DependencyProperty.Register(nameof(MyInterval), typeof(int), typeof(FreehandGrid), new PropertyMetadata(1));


        public Brush MyStroke
        {
            get { return (Brush)GetValue(MyStrokeProperty); }
            set { SetValue(MyStrokeProperty, value); }
        }
        public static readonly DependencyProperty MyStrokeProperty =
            DependencyProperty.Register(nameof(MyStroke), typeof(Brush), typeof(FreehandGrid), new PropertyMetadata(Brushes.Tomato));


        public double MyStrokeThickness
        {
            get { return (double)GetValue(MyStrokeThicknessProperty); }
            set { SetValue(MyStrokeThicknessProperty, value); }
        }
        public static readonly DependencyProperty MyStrokeThicknessProperty =
            DependencyProperty.Register(nameof(MyStrokeThickness), typeof(double), typeof(FreehandGrid), new PropertyMetadata(20.0));



        #endregion 依存関係プロパティ



    }





    public class PGeoShape : DependencyObject
    {
        public GeoShape MyGeoShape { get; set; }

        public PGeoShape()
        {

            MyGeoShape = new()
            {
                Stroke = Brushes.DodgerBlue,
                StrokeThickness = 20.0,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                StrokeLineJoin = PenLineJoin.Round,
            };
            MyBind();

        }

        private void MyBind()
        {
            MyGeoShape.SetBinding(Polyline.PointsProperty, new Binding() { Source = this, Path = new PropertyPath(MyPointsProperty) });
        }


        public PointCollection MyPoints
        {
            get { return (PointCollection)GetValue(MyPointsProperty); }
            set { SetValue(MyPointsProperty, value); }
        }
        public static readonly DependencyProperty MyPointsProperty =
            DependencyProperty.Register(nameof(MyPoints), typeof(PointCollection), typeof(PPolyline), new PropertyMetadata(null));

        public PointCollection MyOriginPoints
        {
            get { return (PointCollection)GetValue(MyOriginPointsProperty); }
            set { SetValue(MyOriginPointsProperty, value); }
        }
        public static readonly DependencyProperty MyOriginPointsProperty =
            DependencyProperty.Register(nameof(MyOriginPoints), typeof(PointCollection), typeof(PPolyline), new PropertyMetadata(null));

    }



    /// <summary>
    /// GeoShapeと2つのPointCollectionを持つクラス
    /// </summary>
    public class PPolyline : DependencyObject
    {
        public Polyline MyPolyline { get; set; }

        public PPolyline()
        {

            MyPolyline = new()
            {
                Stroke = Brushes.DodgerBlue,
                StrokeThickness = 20.0,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                StrokeLineJoin = PenLineJoin.Round,
            };
            MyBind();

        }

        private void MyBind()
        {
            MyPolyline.SetBinding(Polyline.PointsProperty, new Binding() { Source = this, Path = new PropertyPath(MyPointsProperty) });
        }


        public PointCollection MyPoints
        {
            get { return (PointCollection)GetValue(MyPointsProperty); }
            set { SetValue(MyPointsProperty, value); }
        }
        public static readonly DependencyProperty MyPointsProperty =
            DependencyProperty.Register(nameof(MyPoints), typeof(PointCollection), typeof(PPolyline), new PropertyMetadata(null));

        public PointCollection MyOriginPoints
        {
            get { return (PointCollection)GetValue(MyOriginPointsProperty); }
            set { SetValue(MyOriginPointsProperty, value); }
        }
        public static readonly DependencyProperty MyOriginPointsProperty =
            DependencyProperty.Register(nameof(MyOriginPoints), typeof(PointCollection), typeof(PPolyline), new PropertyMetadata(null));


    }







    public class MyConvMage : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var origin = (PointCollection)values[0];
            var interval = (int)values[1];
            return ChoiceAnchorPoint(origin, interval);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// 元のPointCollectionから指定間隔で選んだPointCollectionを新たに作成して返す
        /// </summary>
        /// <param name="points"></param>
        /// <param name="interval"></param>
        /// <returns></returns>
        private static PointCollection ChoiceAnchorPoint(PointCollection points, int interval)
        {
            var selectedPoints = new PointCollection();
            if (points.Count == 0) { return selectedPoints; }

            if (interval < 1) { interval = 1; }//間隔は1以上
            for (int i = 0; i < points.Count - 1; i += interval)
            {
                selectedPoints.Add(points[i]);
            }
            selectedPoints.Add(points[^1]);//最後の一個は必ず入れる

            //選んだ頂点が3個以上あって、最後の頂点と最後から2番めが近いときは2番めを除去            
            if (selectedPoints.Count >= 3)
            {
                int mod = (points.Count - 2) % interval;
                if (interval / 2 > mod)
                {
                    selectedPoints.RemoveAt(selectedPoints.Count - 2);//除去
                }
            }
            return selectedPoints;
        }
    }



}
