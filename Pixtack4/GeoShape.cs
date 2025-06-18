using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Security.Cryptography.Xml;


namespace Pixtack4
{
    public enum HeadType { None = 0, Arrow, }
    public enum ShapeType { Line = 0, Bezier, }
    
    // ベジェ曲線の方向線の長さの決め方の種類
    public enum DirectionLineLengthType
    {
        Zero0距離, // 0距離、曲げない
        Average平均, // 前後の方向線の長さの平均
        Separate別々, // 前後の方向線の長さを別々に設定
        Shorter短いほう, // 前後の方向線の短い方を採用
        FrontBack前後間 // 前後間を採用
    }

    public class GeoShape : Shape
    {
        public GeoShape()
        {
            MyInitializeBind();

        }

        #region 初期処理

        private void MyInitializeBind()
        {
            
            //Pointsの先頭を外したPointCollection
            _ = SetBinding(MySegmentPointsProperty, new Binding() { Source = this, Path = new PropertyPath(MyPointsProperty), Mode = BindingMode.OneWay, Converter = new MyConverterSegmentPoints() });

            ////Penのバインド、Penは図形のBoundsを計測するために必要
            //MultiBinding mb = new() { Converter = new MyConverterPen() };
            //mb.Bindings.Add(MakeOneWayBind(StrokeThicknessProperty));
            //mb.Bindings.Add(MakeOneWayBind(StrokeMiterLimitProperty));
            //mb.Bindings.Add(MakeOneWayBind(StrokeEndLineCapProperty));
            //mb.Bindings.Add(MakeOneWayBind(StrokeStartLineCapProperty));
            //mb.Bindings.Add(MakeOneWayBind(StrokeLineJoinProperty));
            //_ = SetBinding(MyPenProperty, mb);
        }

        private Binding MakeOneWayBind(DependencyProperty property)
        {
            return new Binding() { Source = this, Path = new PropertyPath(property), Mode = BindingMode.OneWay };
        }
        #endregion 初期処理


        protected override Geometry DefiningGeometry
        {
            get
            {
                if (MyPoints is null || MyPoints.Count < 2) { return Geometry.Empty; }

                if (MyHeadBeginType != HeadType.None || MyHeadEndType != HeadType.None)
                {
                    Fill = Stroke;
                }


                StreamGeometry geo = new();
                using (var context = geo.Open())
                {
                    Point begin = MyPoints[0];
                    Point end = MyPoints[^1];
                    switch (MyHeadBeginType)
                    {
                        case HeadType.None:
                            break;
                        case HeadType.Arrow:
                            begin = DrawArrow(context, begin, MyPoints[1]);
                            break;
                        default:
                            break;
                    }

                    switch (MyHeadEndType)
                    {
                        case HeadType.None:
                            break;
                        case HeadType.Arrow:
                            end = DrawArrow(context, end, MyPoints[^2]);
                            break;
                        default:
                            break;
                    }

                    switch (MyShapeType)
                    {
                        case ShapeType.Line:
                            DrawLine(context, begin, end, MyIsFill, MyIsClosed, MyIsSmoothJoin);
                            break;
                        case ShapeType.Bezier:
                            DrawBezier(context, begin, end, MyIsFill, MyIsClosed, MyIsSmoothJoin);
                            break;
                        default:
                            break;
                    }
                }
                geo.Freeze();



                ////Boundsの更新はここで行う必要がある。OnRenderではなんか違う
                ////MyBounds1 = geo.Bounds;
                ////MyBounds2 = geo.GetRenderBounds(MyPen);
                ////回転後のBounds
                //var clone = geo.Clone();
                //clone.Transform = RenderTransform;
                ////MyBounds3 = clone.Bounds;
                //MyRenderBounds = clone.GetRenderBounds(MyPen);

                return geo;
            }
        }






        #region 依存関係プロパティ

        #region 読み取り用

        ////サイズと位置
        //public Rect MyRenderBounds
        //{
        //    get { return (Rect)GetValue(MyRenderBoundsProperty); }
        //    set { SetValue(MyRenderBoundsProperty, value); }
        //}
        //public static readonly DependencyProperty MyRenderBoundsProperty =
        //    DependencyProperty.Register(nameof(MyRenderBounds), typeof(Rect), typeof(GeoShape), new PropertyMetadata(new Rect()));

        ////サイズと位置の計算に使う
        //public Pen MyPen
        //{
        //    get { return (Pen)GetValue(MyPenProperty); }
        //    set { SetValue(MyPenProperty, value); }
        //}
        //public static readonly DependencyProperty MyPenProperty =
        //    DependencyProperty.Register(nameof(MyPen), typeof(Pen), typeof(GeoShape), new PropertyMetadata(new Pen()));

        //MyPointsから作成
        public PointCollection MySegmentPoints
        {
            get { return (PointCollection)GetValue(MySegmentPointsProperty); }
            set { SetValue(MySegmentPointsProperty, value); }
        }
        public static readonly DependencyProperty MySegmentPointsProperty =
            DependencyProperty.Register(nameof(MySegmentPoints), typeof(PointCollection), typeof(GeoShape), new PropertyMetadata(null));


        #endregion 読み取り用



        #region 通常


        public bool MyIsClosed
        {
            get { return (bool)GetValue(MyIsClosedProperty); }
            set { SetValue(MyIsClosedProperty, value); }
        }
        public static readonly DependencyProperty MyIsClosedProperty =
            DependencyProperty.Register(nameof(MyIsClosed), typeof(bool), typeof(GeoShape),
                new FrameworkPropertyMetadata(false,
                    FrameworkPropertyMetadataOptions.AffectsRender |
                    FrameworkPropertyMetadataOptions.AffectsMeasure |
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));


        public bool MyIsSmoothJoin
        {
            get { return (bool)GetValue(MyIsSmoothJoinProperty); }
            set { SetValue(MyIsSmoothJoinProperty, value); }
        }
        public static readonly DependencyProperty MyIsSmoothJoinProperty =
            DependencyProperty.Register(nameof(MyIsSmoothJoin), typeof(bool), typeof(GeoShape),
                new FrameworkPropertyMetadata(false,
                    FrameworkPropertyMetadataOptions.AffectsRender |
                    FrameworkPropertyMetadataOptions.AffectsMeasure |
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));


        public bool MyIsFill
        {
            get { return (bool)GetValue(MyIsFillProperty); }
            set { SetValue(MyIsFillProperty, value); }
        }
        public static readonly DependencyProperty MyIsFillProperty =
            DependencyProperty.Register(nameof(MyIsFill), typeof(bool), typeof(GeoShape),
                new FrameworkPropertyMetadata(false,
                    FrameworkPropertyMetadataOptions.AffectsRender |
                    FrameworkPropertyMetadataOptions.AffectsMeasure |
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));


        public ShapeType MyShapeType
        {
            get { return (ShapeType)GetValue(MyShapeTypeProperty); }
            set { SetValue(MyShapeTypeProperty, value); }
        }
        public static readonly DependencyProperty MyShapeTypeProperty =
            DependencyProperty.Register(nameof(MyShapeType), typeof(ShapeType), typeof(GeoShape),
                new FrameworkPropertyMetadata(ShapeType.Line,
                    FrameworkPropertyMetadataOptions.AffectsRender |
                    FrameworkPropertyMetadataOptions.AffectsMeasure |
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        /// <summary>
        /// 終点のヘッドタイプ
        /// </summary>
        public HeadType MyHeadEndType
        {
            get { return (HeadType)GetValue(MyHeadEndTypeProperty); }
            set { SetValue(MyHeadEndTypeProperty, value); }
        }
        public static readonly DependencyProperty MyHeadEndTypeProperty =
            DependencyProperty.Register(nameof(MyHeadEndType), typeof(HeadType), typeof(GeoShape),
                new FrameworkPropertyMetadata(HeadType.None,
                    FrameworkPropertyMetadataOptions.AffectsRender |
                    FrameworkPropertyMetadataOptions.AffectsMeasure |
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));


        /// <summary>
        /// 始点のヘッドタイプ
        /// </summary>
        public HeadType MyHeadBeginType
        {
            get { return (HeadType)GetValue(MyHeadBeginTypeProperty); }
            set { SetValue(MyHeadBeginTypeProperty, value); }
        }
        public static readonly DependencyProperty MyHeadBeginTypeProperty =
            DependencyProperty.Register(nameof(MyHeadBeginType), typeof(HeadType), typeof(GeoShape),
                new FrameworkPropertyMetadata(HeadType.None,
                    FrameworkPropertyMetadataOptions.AffectsRender |
                    FrameworkPropertyMetadataOptions.AffectsMeasure |
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public double MyArrowHeadAngle
        {
            get { return (double)GetValue(MyArrowHeadAngleProperty); }
            set { SetValue(MyArrowHeadAngleProperty, value); }
        }
        public static readonly DependencyProperty MyArrowHeadAngleProperty =
            DependencyProperty.Register(nameof(MyArrowHeadAngle), typeof(double), typeof(GeoShape),
                new FrameworkPropertyMetadata(30.0,
                    FrameworkPropertyMetadataOptions.AffectsRender |
                    FrameworkPropertyMetadataOptions.AffectsMeasure |
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));



        //public PointCollection MyPoints
        //{
        //    get { return (PointCollection)GetValue(MyPointsProperty); }
        //    set { SetValue(MyPointsProperty, value); }
        //}
        //public static readonly DependencyProperty MyPointsProperty =
        //    DependencyProperty.Register(
        //        nameof(MyPoints),
        //        typeof(PointCollection),
        //        typeof(GeoShape),
        //        new FrameworkPropertyMetadata(null,
        //            FrameworkPropertyMetadataOptions.AffectsRender |
        //            FrameworkPropertyMetadataOptions.AffectsMeasure |
        //            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public PointCollection MyPoints
        {
            get { return (PointCollection)GetValue(MyPointsProperty); }
            set { SetValue(MyPointsProperty, value); }
        }
        public static readonly DependencyProperty MyPointsProperty =
            DependencyProperty.Register(
                nameof(MyPoints),
                typeof(PointCollection),
                typeof(GeoShape),
                new FrameworkPropertyMetadata(new PointCollection(),
                    FrameworkPropertyMetadataOptions.AffectsRender |
                    FrameworkPropertyMetadataOptions.AffectsMeasure |
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        #endregion 通常

        #endregion 依存関係プロパティ


        ///// <summary>
        ///// RenderBoundsの更新、
        ///// RenderBoundsはRenderTransformを変更しても更新されないので、その時用
        ///// </summary>
        //public void UpdateRenderBounds()
        //{
        //    //回転後のBounds
        //    Geometry clone = this.DefiningGeometry.Clone();
        //    clone.Transform = RenderTransform;
        //    MyRenderBounds = clone.GetRenderBounds(MyPen);
        //}


        #region メソッド

        /// <summary>
        /// ベジェ曲線部分の描画
        /// </summary>
        /// <param name="context"></param>
        /// <param name="begin">始点図形との接点</param>
        /// <param name="end">終点図形との接点</param>
        private void DrawBezier(StreamGeometryContext context, Point begin, Point end, bool isFill, bool isClose, bool isSmoothJoin)
        {
            context.BeginFigure(begin, isFill, isClose);
            var bezier = MySegmentPoints.Clone();
            //            List<Point> bezier = MyPoints.Skip(1).Take(MyPoints.Count - 2).ToList();
            bezier.Add(end);
            context.PolyBezierTo(bezier, true, isSmoothJoin);

        }





        /// <summary>
        /// 直線部分の描画
        /// </summary>
        /// <param name="context"></param>
        /// <param name="begin">始点図形との接点</param>
        /// <param name="end">終点図形との接点</param>
        private void DrawLine(StreamGeometryContext context, Point begin, Point end, bool isFill, bool isClosed, bool isSmoothJoin)
        {
            context.BeginFigure(begin, isFill, isClosed);
            context.PolyLineTo(MySegmentPoints, true, isSmoothJoin);
            //context.PolyLineTo(MyPoints.Skip(1).Take(MyPoints.Count - 2).ToList(), true, isSmoothJoin);
            context.LineTo(end, true, isSmoothJoin);
        }




        /// <summary>
        /// アローヘッド(三角形)描画
        /// </summary>
        /// <param name="context"></param>
        /// <param name="edge">端のPoint、始点ならPoints[0]、終点ならPoints[^1]</param>
        /// <param name="next">端から2番めのPoint、始点ならPoints[1]、終点ならPoints[^2]</param>
        /// <returns></returns>
        private Point DrawArrow(StreamGeometryContext context, Point edge, Point next)
        {
            //斜辺 hypotenuse ここでは二等辺三角形の底辺じゃない方の2辺
            //頂角 apex angle 先端の角
            //アローヘッドの斜辺(hypotenuse)の角度(ラジアン)を計算
            double lineRadian = Math.Atan2(next.Y - edge.Y, next.X - edge.X);
            double apexRadian = DegreeToRadian(MyArrowHeadAngle);
            double edgeSize = StrokeThickness * 2.0;
            double hypotenuseLength = edgeSize / Math.Cos(apexRadian);
            double hypotenuseRadian1 = lineRadian + apexRadian;

            //底角座標
            Point p1 = new(
                hypotenuseLength * Math.Cos(hypotenuseRadian1) + edge.X,
                hypotenuseLength * Math.Sin(hypotenuseRadian1) + edge.Y);

            double hypotenuseRadian2 = lineRadian - DegreeToRadian(MyArrowHeadAngle);
            Point p2 = new(
                hypotenuseLength * Math.Cos(hypotenuseRadian2) + edge.X,
                hypotenuseLength * Math.Sin(hypotenuseRadian2) + edge.Y);

            //アローヘッド描画、Fill(塗りつぶし)で描画
            context.BeginFigure(edge, true, false);//isFilled, isClose
            context.LineTo(p1, false, false);//isStroke, isSmoothJoin
            context.LineTo(p2, false, false);

            //アローヘッドと中間線の接点座標計算、
            //HeadSizeぴったりで計算すると僅かな隙間ができるので-1.0している、
            //-0.5でも隙間になる、-0.7で隙間なくなる
            return new Point(
                (edgeSize - 1.0) * Math.Cos(lineRadian) + edge.X,
                (edgeSize - 1.0) * Math.Sin(lineRadian) + edge.Y);
        }
        #endregion メソッド


        #region パフリックメソッド


        //        // 曲げ
        //        //       制御点の位置を決めるまでの手順
        //        // アンカー点から
        //        // A.前後方向線の長さを取得
        //        // B.方向線の弧度を取得
        //        // ABを使って制御点の位置を設定
        /// <summary>
        /// 制御点の座標を決める
        /// </summary>
        /// <param name="allPoints"></param>
        /// <param name="lengthType"></param>
        /// <param name="mage"></param>
        public static void SetControlPointLocate(PointCollection allPoints, DirectionLineLengthType lengthType, double mage)
        {
            for (int i = 0; i < allPoints.Count - 6; i += 3)
            {
                Point beginSideAnchor = allPoints[i];
                Point currentAnchor = allPoints[i + 3];
                Point endSideAnchor = allPoints[i + 6];
                // A
                (double beginSideLength, double endSideLength) = GetDirectionLineLength(lengthType, mage, beginSideAnchor, currentAnchor, endSideAnchor);
                // B
                (double beginSideRadian, double endSideRadian) = GetDirectionLineRadian(beginSideAnchor, currentAnchor, endSideAnchor);
                // AB
                // 始点側の制御点位置決定
                double x = currentAnchor.X + Math.Cos(beginSideRadian) * beginSideLength;
                double y = currentAnchor.Y + Math.Sin(beginSideRadian) * beginSideLength;
                allPoints[i + 2] = new Point(x, y);
                // 終点側の制御点位置決定
                x = currentAnchor.X + Math.Cos(endSideRadian) * endSideLength;
                y = currentAnchor.Y + Math.Sin(endSideRadian) * endSideLength;
                allPoints[i + 4] = new Point(x, y);
            }
        }


        /// <summary>
        /// 現在アンカー点とその前後のアンカー点それぞれの中間弧度に直角な弧度を計算
        /// </summary>
        /// <param name="beginAnchor">始点側アンカー点</param>
        /// <param name="currentAnchor">現在アンカー点</param>
        /// <param name="endAnchor">終点側アンカー点</param>
        /// <returns>始点側方向線弧度、終点側方向線弧度</returns>
        public static (double beginSideRadian, double endSideRadian) GetDirectionLineRadian(Point beginAnchor, Point currentAnchor, Point endAnchor)
        {
            //ラジアン(弧度)
            double beginSideRadian = GetRadian(currentAnchor, beginAnchor);//現在から始点側
            double endSideRadian = GetRadian(currentAnchor, endAnchor);//現在から終点側
            double middleRadian = (beginSideRadian + endSideRadian) / 2.0;//中間角度

            //中間角度に直角なのは90度を足した右回りと、90を引いた左回りがある
            //始点側角度＞終点側角度のときは始点側に90度を足して、終点側は90度引く
            //逆のときは足し引きも逆になる
            double bControlRadian, eControlRadian;
            if (beginSideRadian > endSideRadian)
            {
                bControlRadian = middleRadian + (Math.PI / 2.0);
                eControlRadian = middleRadian - (Math.PI / 2.0);
            }
            else
            {
                bControlRadian = middleRadian - (Math.PI / 2.0);
                eControlRadian = middleRadian + (Math.PI / 2.0);
            }

            return (bControlRadian, eControlRadian);
        }


        /// <summary>
        /// 2点間線分のラジアン(弧度)を取得
        /// </summary>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public static double GetRadian(Point begin, Point end)
        {
            return Math.Atan2(end.Y - begin.Y, end.X - begin.X);
        }




        /// <summary>
        /// 連なる3つのアンカー点の、中間のアンカー点に属する、前後の方向線の長さを返す
        /// </summary>
        /// <param name="lengthType">距離の決め方</param>
        /// <param name="mage">曲げ具合の指定、0.0から1.0を指定、0.3前後が適当</param>
        /// <param name="beginSizeAnchor">始点側のアンカー点</param>
        /// <param name="currentAnchor">中間のアンカー点</param>
        /// <param name="endSideAnchor">終点側のアンカー点</param>
        /// <returns></returns>
        public static (double, double) GetDirectionLineLength(DirectionLineLengthType lengthType, double mage, Point beginSizeAnchor, Point currentAnchor, Point endSideAnchor)
        {
            if (lengthType == DirectionLineLengthType.Zero0距離)
            {
                return (0, 0);
            }
            else if (lengthType == DirectionLineLengthType.FrontBack前後間)
            {
                double temp = GetDistance(beginSizeAnchor, endSideAnchor) * mage;
                return (temp, temp);
            }

            double distBegin = GetDistance(currentAnchor, beginSizeAnchor) * mage;
            double distEnd = GetDistance(currentAnchor, endSideAnchor) * mage;
            if (lengthType == DirectionLineLengthType.Average平均)
            {
                double average = (distBegin + distEnd) / 2.0;
                return (average, average);
            }
            else if (lengthType == DirectionLineLengthType.Separate別々)
            {
                return (distBegin, distEnd);
            }
            else if (lengthType == DirectionLineLengthType.Shorter短いほう)
            {
                double shorter = distEnd < distBegin ? distEnd : distBegin;
                return (shorter, shorter);
            }

            return (0, 0);
        }


        //2点間距離を取得
        /// <summary>
        /// Calculates the Euclidean distance between two points.
        /// </summary>
        /// <param name="p1">The first point.</param>
        /// <param name="p2">The second point.</param>
        /// <returns>The Euclidean distance between <paramref name="p1"/> and <paramref name="p2"/>.</returns>
        public static double GetDistance(Point p1, Point p2)
        {
            return Math.Sqrt(Math.Pow(p2.X - p1.X, 2.0) + Math.Pow(p2.Y - p1.Y, 2.0));
        }


        //前後のアンカー点それぞれの距離
        /// <summary>
        /// Calculates the distances between the current point and two anchor points.
        /// </summary>
        /// <remarks>This method is useful for determining the relative positioning of a point between two
        /// anchor points.</remarks>
        /// <param name="beginP">The starting anchor point.</param>
        /// <param name="currentP">The current point from which distances are calculated.</param>
        /// <param name="endP">The ending anchor point.</param>
        /// <returns>A tuple containing two distances:  <list type="bullet"> <item><description><c>begin</c>: The distance
        /// between <paramref name="currentP"/> and <paramref name="beginP"/>.</description></item>
        /// <item><description><c>end</c>: The distance between <paramref name="currentP"/> and <paramref
        /// name="endP"/>.</description></item> </list></returns>
        public static (double begin, double end) DistanceSeparate(Point beginP, Point currentP, Point endP)
        {
            double bSide = GetDistance(currentP, beginP);
            double eSide = GetDistance(currentP, endP);
            return (bSide, eSide);
        }

        //2点間線分のラジアン(弧度)を取得
        public static double GetRadianFrom2Points(Point begin, Point end)
        {
            return Math.Atan2(end.Y - begin.Y, end.X - begin.X);
        }

        //ラジアンを角度に変換
        public static double RadianToDegree(double radian)
        {
            return radian / Math.PI * 180.0;
        }


        /// <summary>
        /// 指定間隔で間引いたPointCollectionを作成 
        /// </summary>
        /// <param name="points"></param>
        /// <param name="interval"></param>
        /// <returns></returns>
        public static PointCollection MakeIntervalPointCollection(PointCollection points, int interval)
        {
            if (interval < 1) { interval = 1; }//間隔は1以上
            var selectedPoints = new PointCollection();
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

        /// <summary>
        /// アンカー点のPointCollectionから制御点のPointCollectionを作成
        /// 制御点の座標はアンカー点と同じ
        /// </summary>
        /// <param name="anchorPoints"></param>
        /// <returns></returns>
        public static PointCollection MakeControlPointCollectionFromAnchors(PointCollection anchorPoints)
        {
            PointCollection pc = [];
            pc.Add(anchorPoints[0]);// 始点
            pc.Add(anchorPoints[0]);// 始点の制御点
            for (int i = 1; i < anchorPoints.Count - 1; i++)
            {
                pc.Add(anchorPoints[i]);// 制御点
                pc.Add(anchorPoints[i]);// アンカー
                pc.Add(anchorPoints[i]);// 制御点
            }
            pc.Add(anchorPoints[^1]);// 終点の制御点
            pc.Add(anchorPoints[^1]);// 終点
            return pc;
        }

        /// <summary>
        /// 角度をラジアンに変換
        /// </summary>
        /// <param name="degree"></param>
        /// <returns></returns>
        public static double DegreeToRadian(double degree)
        {
            return degree / 360.0 * (Math.PI * 2.0);
        }

        /// <summary>
        /// 図形が収まるRectを返す
        /// </summary>
        /// <returns></returns>
        public Rect GetRenderBounds()
        {
            //自身のGeometryのクローンを使う
            //自身に適用されているRenderTransformとPenをクローンに適用して
            //クローンのGetRenderBoundsで得られる
            var geo = DefiningGeometry.Clone();
            geo.Transform = RenderTransform;
            Pen myPen = new(Brushes.Transparent, StrokeThickness)
            {
                EndLineCap = StrokeEndLineCap,
                StartLineCap = StrokeStartLineCap,
                LineJoin = StrokeLineJoin,
                MiterLimit = StrokeMiterLimit,
            };
            return geo.GetRenderBounds(myPen);
        }

        #endregion パフリックメソッド



    }




    #region コンバーター

    //Segment用のPointCollection生成
    //ソースに影響を与えないためにクローン作成して、その始点と終点要素を削除して返す
    public class MyConverterSegmentPoints : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is PointCollection pc)
            {
                if (pc.Count > 1)
                {
                    var clone = pc.Clone();
                    clone.RemoveAt(0);
                    clone.RemoveAt(clone.Count - 1);
                    return clone;
                }
                return pc;
            }
            return new PointCollection();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    #endregion コンバーター


}