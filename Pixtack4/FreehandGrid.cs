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
        public List<PGeoShape> MyListOfPGeoShape { get; set; } = [];
        //public List<PPolyline> MyListOfPPolyline { get; set; } = [];
        public Polyline MyDragDrawShape { get; set; } = null!;
        //public Polyline MyPolyline { get; set; } = null!;
        private bool IsDrawing;

        public FreehandGrid()
        {
            MouseLeftButtonDown += (s, e) => { DrawBegin(e); };// クリックで描画開始
            MouseMove += (s, e) => { Drawing(e); };// ドラッグ移動、描画
            PreviewMouseLeftButtonUp += (s, e) => { DrawEnd(); };// 描画完了
            MouseLeave += (s, e) => { DrawEnd(); };// 描画完了
            MouseRightButtonDown += (s, e) => { RemoveLastDraw(); };// 右クリック時、最後に追加した図形を削除

            Background = new SolidColorBrush(Color.FromArgb(20, 0, 0, 0));


            MyDragDrawShape = MakePolyline();
            SetZIndex(MyDragDrawShape, 1);
            Children.Add(MyDragDrawShape);
            MyDragDrawShape.Visibility = Visibility.Collapsed;
        }


        // 全部削除
        public void DrawClear()
        {
            if (IsDrawing) { DrawEnd(); }
            while (MyListOfPGeoShape.Count > 0)
            {
                var temp = MyListOfPGeoShape[^1];
                Children.Remove(temp.MyGeoShape);
                _ = MyListOfPGeoShape.Remove(temp);
            }
        }


        // 最後に追加した図形を削除
        private void RemoveLastDraw()
        {
            if (!IsDrawing && MyListOfPGeoShape.Count > 0)
            {
                var temp = MyListOfPGeoShape[^1];// リストの末尾の要素を
                Children.Remove(temp.MyGeoShape);// 子要素群から削除
                _ = MyListOfPGeoShape.Remove(temp);// リストからも削除
            }
        }

        #region 描画

        // 描画開始
        private void DrawBegin(MouseButtonEventArgs e)
        {
            IsDrawing = true;
            MyDragDrawShape.Visibility = Visibility.Visible;
            MyDragDrawShape.Points.Clear();
            MyDragDrawShape.Points.Add(e.GetPosition(this));
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
                    MyDragDrawShape.Points.Add(ima);
                }
            }
        }

        // 描画完了
        private void DrawEnd()
        {
            if (IsDrawing && MyDragDrawShape.Points.Count > 0)
            {
                IsDrawing = false;
                PGeoShape pp = new();
                MyListOfPGeoShape.Add(pp);
                pp.MyOriginPoints = MyDragDrawShape.Points.Clone();

                Children.Add(pp.MyGeoShape);

                MyBind(pp);
            }

            IsDrawing = false;
            MyDragDrawShape.Visibility = Visibility.Collapsed;
            MyDragDrawShape.Points.Clear();

            void MyBind(PGeoShape pp)
            {
                var mb = new MultiBinding() { Converter = new MyConvMage() };
                mb.Bindings.Add(new Binding() { Source = pp, Path = new PropertyPath(PGeoShape.MyOriginPointsProperty) });
                mb.Bindings.Add(new Binding() { Source = this, Path = new PropertyPath(MyIntervalProperty) });
                mb.Bindings.Add(new Binding() { Source = this, Path = new PropertyPath(MyDirectionLineLengthTypeProperty) });
                mb.Bindings.Add(new Binding() { Source = this, Path = new PropertyPath(MyMageProperty) });
                BindingOperations.SetBinding(pp, PGeoShape.MyPointsProperty, mb);

                BindingOperations.SetBinding(pp.MyGeoShape, Shape.StrokeThicknessProperty, new Binding() { Source = this, Path = new PropertyPath(MyStrokeThicknessProperty) });
                BindingOperations.SetBinding(pp.MyGeoShape, Shape.StrokeProperty, new Binding() { Source = this, Path = new PropertyPath(MyStrokeProperty) });

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

        //public GeoShape MyGeoShape
        //{
        //    get { return (GeoShape)GetValue(MyGeoShapeProperty); }
        //    set { SetValue(MyGeoShapeProperty, value); }
        //}
        //public static readonly DependencyProperty MyGeoShapeProperty =
        //    DependencyProperty.Register(nameof(MyGeoShape), typeof(GeoShape), typeof(FreehandGrid), new PropertyMetadata(null));


        // ベジェ曲線の曲げの強さ加減
        public double MyMage
        {
            get { return (double)GetValue(MyMageProperty); }
            set { SetValue(MyMageProperty, value); }
        }
        public static readonly DependencyProperty MyMageProperty =
            DependencyProperty.Register(nameof(MyMage), typeof(double), typeof(FreehandGrid), new PropertyMetadata(0.3));


        // 方向線の長さType
        public DirectionLineLengthType MyDirectionLineLengthType
        {
            get { return (DirectionLineLengthType)GetValue(MyDirectionLineLengthTypeProperty); }
            set { SetValue(MyDirectionLineLengthTypeProperty, value); }
        }
        public static readonly DependencyProperty MyDirectionLineLengthTypeProperty =
            DependencyProperty.Register(nameof(MyDirectionLineLengthType), typeof(DirectionLineLengthType), typeof(FreehandGrid), new PropertyMetadata(DirectionLineLengthType.Separate別々));


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



    /// <summary>
    /// 幾何学的図形とそれに関連付けられた点をカプセル化する依存オブジェクトを表します。
    /// </summary>
    /// <remarks><see cref="PGeoShape"/> クラスは、<see cref="GeoShape"/> オブジェクトのラッパーを提供し、点のコレクションをバインドして図形の幾何学的形状を定義できるようにします。また、図形の点と原点を管理するための依存プロパティをサポートし、動的な更新とデータバインディングを可能にします。</remarks>
    ///
    public class PGeoShape : DependencyObject
    {
        public GeoShape MyGeoShape { get; set; }

        public PGeoShape()
        {

            MyGeoShape = new()
            {
                MyShapeType = ShapeType.Bezier,
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
            MyGeoShape.SetBinding(GeoShape.MyPointsProperty, new Binding() { Source = this, Path = new PropertyPath(MyPointsProperty) });
        }


        public PointCollection MyPoints
        {
            get { return (PointCollection)GetValue(MyPointsProperty); }
            set { SetValue(MyPointsProperty, value); }
        }
        public static readonly DependencyProperty MyPointsProperty =
            DependencyProperty.Register(nameof(MyPoints), typeof(PointCollection), typeof(PGeoShape), new PropertyMetadata(null));

        public PointCollection MyOriginPoints
        {
            get { return (PointCollection)GetValue(MyOriginPointsProperty); }
            set { SetValue(MyOriginPointsProperty, value); }
        }
        public static readonly DependencyProperty MyOriginPointsProperty =
            DependencyProperty.Register(nameof(MyOriginPoints), typeof(PointCollection), typeof(PGeoShape), new PropertyMetadata(null));

    }



    ///// <summary>
    ///// GeoShapeと2つのPointCollectionを持つクラス
    ///// </summary>
    //public class PPolyline : DependencyObject
    //{
    //    public Polyline MyPolyline { get; set; }

    //    public PPolyline()
    //    {

    //        MyPolyline = new()
    //        {
    //            Stroke = Brushes.DodgerBlue,
    //            StrokeThickness = 20.0,
    //            StrokeStartLineCap = PenLineCap.Round,
    //            StrokeEndLineCap = PenLineCap.Round,
    //            StrokeLineJoin = PenLineJoin.Round,
    //        };
    //        MyBind();

    //    }

    //    private void MyBind()
    //    {
    //        MyPolyline.SetBinding(Polyline.PointsProperty, new Binding() { Source = this, Path = new PropertyPath(MyPointsProperty) });
    //    }


    //    public PointCollection MyPoints
    //    {
    //        get { return (PointCollection)GetValue(MyPointsProperty); }
    //        set { SetValue(MyPointsProperty, value); }
    //    }
    //    public static readonly DependencyProperty MyPointsProperty =
    //        DependencyProperty.Register(nameof(MyPoints), typeof(PointCollection), typeof(PPolyline), new PropertyMetadata(null));

    //    public PointCollection MyOriginPoints
    //    {
    //        get { return (PointCollection)GetValue(MyOriginPointsProperty); }
    //        set { SetValue(MyOriginPointsProperty, value); }
    //    }
    //    public static readonly DependencyProperty MyOriginPointsProperty =
    //        DependencyProperty.Register(nameof(MyOriginPoints), typeof(PointCollection), typeof(PPolyline), new PropertyMetadata(null));


    //}







    public class MyConvMage : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var origin = (PointCollection)values[0];
            var interval = (int)values[1];
            var direction = (DirectionLineLengthType)values[2];
            var mage = (double)values[3];

            // 間引き
            PointCollection pc = GeoShape.MakeIntervalPointCollection(origin, interval);
            // 制御点追加
            PointCollection ppc = GeoShape.MakeControlPointCollectionFromAnchors(pc);
            // 曲げ加工
            GeoShape.SetControlPointLocate(ppc, direction, mage);
            return ppc;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }



    }



}
