using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows;
using System.Windows.Shapes;
using System.Transactions;

namespace Pixtack4
{
    //方向線の長さの決め方の種類
    //public enum DistanceType
    //{
    //    Zero0距離,
    //    Average平均,
    //    Separate別々,
    //    Shorter短いほう,
    //    FrontBack前後間
    //}
    //public enum RadianType
    //{
    //    Parallel平行,
    //    RightAngleOfCenter中間の直角
    //}

    //public class FreehandGeoShape : GeoShape
    //{
    //    public PointCollection MyOriginPoints { get; private set; } = [];// 元のPoints

    //    public FreehandGeoShape() { }
    //    public FreehandGeoShape(PointCollection points)
    //    {
    //        MyShapeType = ShapeType.Bezier;
    //        MyOriginPoints = points;
    //        //MyPoints = ChoiceAnchorPoint(MyOriginPoints, interval: 10);
    //        PointCollection AnchorPoints = ChoiceAnchorPoint(MyOriginPoints, interval: 10);
    //        PointCollection allPoints = AddPointToAnchorPoints(AnchorPoints);


    //        //       制御点の位置を決めるまでの手順
    //        // アンカー点から
    //        // A.前後方向線の長さを取得
    //        // B.方向線の弧度を取得
    //        // ABを使って制御点の位置を設定

    //        //for (int i = 0; i < AnchorPoints.Count - 2; i++)
    //        //{
    //        //    Point beginSideAnchor = AnchorPoints[i];
    //        //    Point currentAnchor = AnchorPoints[i + 1];
    //        //    Point endSideAnchor = AnchorPoints[i + 2];
    //        //    // A
    //        //    (double beginSideLength, double endSideLength) = GetDirectionLineLength(MyDistanceType, 0.3, beginSideAnchor, currentAnchor, endSideAnchor);
    //        //    // B
    //        //    (double beginSideRadian, double endSideRadian) = GetDirectionLineRadian(beginSideAnchor, currentAnchor, endSideAnchor);
    //        //    // AB
    //        //    double x = currentAnchor.X + Math.Cos(beginSideRadian) * beginSideLength;
    //        //    double y = currentAnchor.X + Math.Sin(beginSideRadian) * beginSideLength;
    //        //    allPoints[(i * 3) - 1] = new Point(x, y);
    //        //    x = currentAnchor.X + Math.Cos(endSideRadian) * endSideLength;
    //        //    y = currentAnchor.Y + Math.Sin(endSideRadian) * endSideLength;
    //        //    allPoints[(i * 3) + 1] = new Point(x, y);
    //        //}

    //        for (int i = 0; i < allPoints.Count - 6; i += 3)
    //        {
    //            Point beginSideAnchor = AnchorPoints[i];
    //            Point currentAnchor = AnchorPoints[i + 3];
    //            Point endSideAnchor = AnchorPoints[i + 6];
    //            // A
    //            (double beginSideLength, double endSideLength) = GetDirectionLineLength(MyDistanceType, 0.3, beginSideAnchor, currentAnchor, endSideAnchor);
    //            // B
    //            (double beginSideRadian, double endSideRadian) = GetDirectionLineRadian(beginSideAnchor, currentAnchor, endSideAnchor);
    //            // AB
    //            // 始点側の制御点位置決定
    //            double x = currentAnchor.X + Math.Cos(beginSideRadian) * beginSideLength;
    //            double y = currentAnchor.X + Math.Sin(beginSideRadian) * beginSideLength;
    //            allPoints[i + 2] = new Point(x, y);
    //            // 終点側の制御点位置決定
    //            x = currentAnchor.X + Math.Cos(endSideRadian) * endSideLength;
    //            y = currentAnchor.Y + Math.Sin(endSideRadian) * endSideLength;
    //            allPoints[i + 4] = new Point(x, y);
    //        }
    //        MyPoints = allPoints;
    //    }

    //    /// <summary>
    //    /// 連なる3つのアンカー点の、中間のアンカー点に属する、前後の方向線の長さを返す
    //    /// </summary>
    //    /// <param name="distType">距離の決め方</param>
    //    /// <param name="mage">曲げ具合の指定、0.0から1.0を指定、0.3前後が適当</param>
    //    /// <param name="beginSizeAnchor">始点側のアンカー点</param>
    //    /// <param name="currentAnchor">中間のアンカー点</param>
    //    /// <param name="endSideAnchor">終点側のアンカー点</param>
    //    /// <returns></returns>
    //    private static (double, double) GetDirectionLineLength(DistanceType distType, double mage, Point beginSizeAnchor, Point currentAnchor, Point endSideAnchor)
    //    {
    //        if (distType == DistanceType.Zero0距離) { return (0, 0); }

    //        if (distType == DistanceType.Zero0距離)
    //        {
    //            return (0, 0);
    //        }
    //        else if (distType == DistanceType.FrontBack前後間)
    //        {
    //            double temp = GetDistance(beginSizeAnchor, endSideAnchor) * mage;
    //            return (temp, temp);
    //        }

    //        double distBegin = GetDistance(currentAnchor, beginSizeAnchor) * mage;
    //        double distEnd = GetDistance(currentAnchor, endSideAnchor) * mage;
    //        if (distType == DistanceType.Average平均)
    //        {
    //            double average = (distBegin + distEnd) / 2.0;
    //            return (average, average);
    //        }
    //        else if (distType == DistanceType.Separate別々)
    //        {
    //            return (distBegin, distEnd);
    //        }
    //        else if (distType == DistanceType.Shorter短いほう)
    //        {
    //            double shorter = distEnd < distBegin ? distEnd : distBegin;
    //            return (shorter, shorter);
    //        }

    //        return (0, 0);
    //    }



    //    ///// <summary>
    //    ///// 現在アンカー点とその前後のアンカー点それぞれの中間弧度に直角な弧度を計算
    //    ///// </summary>
    //    ///// <param name="beginAnchor">始点側アンカー点</param>
    //    ///// <param name="currentAnchor">現在アンカー点</param>
    //    ///// <param name="endAnchor">終点側アンカー点</param>
    //    ///// <returns>始点側方向線弧度、終点側方向線弧度</returns>
    //    //private static (double beginSideRadian, double endSideRadian) GetDirectionLineRadian(Point beginAnchor, Point currentAnchor, Point endAnchor)
    //    //{
    //    //    //ラジアン(弧度)
    //    //    double beginSideRadian = GetRadianFrom2Points(currentAnchor, beginAnchor);//現在から始点側
    //    //    double endSideRadian = GetRadianFrom2Points(currentAnchor, endAnchor);//現在から終点側
    //    //    double middleRadian = (beginSideRadian + endSideRadian) / 2.0;//中間角度

    //    //    //中間角度に直角なのは90度を足した右回りと、90を引いた左回りがある
    //    //    //始点側角度＞終点側角度のときは始点側に90度を足して、終点側は90度引く
    //    //    //逆のときは足し引きも逆になる
    //    //    double bControlRadian, eControlRadian;
    //    //    if (beginSideRadian > endSideRadian)
    //    //    {
    //    //        bControlRadian = middleRadian + (Math.PI / 2.0);
    //    //        eControlRadian = middleRadian - (Math.PI / 2.0);
    //    //    }
    //    //    else
    //    //    {
    //    //        bControlRadian = middleRadian - (Math.PI / 2.0);
    //    //        eControlRadian = middleRadian + (Math.PI / 2.0);
    //    //    }

    //    //    return (bControlRadian, eControlRadian);
    //    //}

    //    ////ベジェ曲線PathからSegmentのPointsを取得
    //    //private PointCollection GetPolyBezierSegmentPoints(Path bezierPath)
    //    //{
    //    //    var pg = (PathGeometry)bezierPath.Data;
    //    //    PathFigure fig = pg.Figures[0];
    //    //    var seg = (PolyBezierSegment)fig.Segments[0];
    //    //    return seg.Points;
    //    //}

    //    ////制御点座標を決めて曲線化
    //    //private void ToCurve(Path bezierPath, double curve, DistanceType distanceType, RadianType radianType)
    //    //{
    //    //    PointCollection segPoints = GetPolyBezierSegmentPoints(bezierPath);
    //    //    for (int i = 1; i < AnchorPoints.Count - 1; i++)
    //    //    {
    //    //        Point beginAnchor = AnchorPoints[i - 1];
    //    //        Point currentAnchor = AnchorPoints[i];
    //    //        Point endAnchor = AnchorPoints[i + 1];
    //    //        //方向線距離
    //    //        if (radianType == RadianType.RightAngleOfCenter中間の直角)
    //    //        {
    //    //            double beginDistance = 0, endDistance = 0;
    //    //            switch (distanceType)
    //    //            {
    //    //                case DistanceType.Zero0距離:
    //    //                    break;
    //    //                case DistanceType.Average平均:
    //    //                    (beginDistance, endDistance) = DistanceAverage(beginAnchor, currentAnchor, endAnchor);
    //    //                    break;
    //    //                case DistanceType.Separate別々:
    //    //                    (beginDistance, endDistance) = DistanceSeparate(beginAnchor, currentAnchor, endAnchor);
    //    //                    break;
    //    //                case DistanceType.Shorter短いほう:
    //    //                    (beginDistance, endDistance) = DistanceShorter(beginAnchor, currentAnchor, endAnchor);
    //    //                    break;
    //    //                case DistanceType.FrontBack前後間:
    //    //                    (beginDistance, endDistance) = DistanceFrontAndBackAnchor(beginAnchor, currentAnchor, endAnchor);
    //    //                    break;
    //    //                default:
    //    //                    break;
    //    //            }

    //    //            //方向線弧度取得
    //    //            (double bRadian, double eRadian) = GetDirectionLineRadian(beginAnchor, currentAnchor, endAnchor);
    //    //            //(double bRadian, double eRadian) = SharpTest(beginAnchor, currentAnchor, endAnchor);
    //    //            //始点側制御点座標
    //    //            double xDiff = Math.Cos(bRadian) * beginDistance * curve;
    //    //            double yDiff = Math.Sin(bRadian) * beginDistance * curve;
    //    //            segPoints[i * 3 - 2] = new Point(currentAnchor.X + xDiff, currentAnchor.Y + yDiff);
    //    //            //終点側制御点座標
    //    //            xDiff = Math.Cos(eRadian) * endDistance * curve;
    //    //            yDiff = Math.Sin(eRadian) * endDistance * curve;
    //    //            segPoints[i * 3] = new Point(currentAnchor.X + xDiff, currentAnchor.Y + yDiff);
    //    //        }
    //    //        else if (radianType == RadianType.Parallel平行)
    //    //        {
    //    //            Point bSide = currentAnchor;
    //    //            Point eSide = currentAnchor;
    //    //            switch (distanceType)
    //    //            {
    //    //                case DistanceType.Zero0距離:
    //    //                    break;
    //    //                case DistanceType.Average平均:
    //    //                    (bSide, eSide) = ControlPointsAverage(beginAnchor, currentAnchor, endAnchor, curve);
    //    //                    break;
    //    //                case DistanceType.Separate別々:
    //    //                    (bSide, eSide) = ControlPointsSeparate(beginAnchor, currentAnchor, endAnchor, curve);
    //    //                    break;
    //    //                case DistanceType.Shorter短いほう:
    //    //                    (bSide, eSide) = ControlPointsShorter(beginAnchor, currentAnchor, endAnchor, curve);
    //    //                    break;
    //    //                case DistanceType.FrontBack前後間:
    //    //                    (bSide, eSide) = ControlPointsFrontAndBack(beginAnchor, currentAnchor, endAnchor, curve);
    //    //                    break;
    //    //                default:
    //    //                    break;
    //    //            }
    //    //            segPoints[i * 3 - 2] = bSide;
    //    //            segPoints[i * 3] = eSide;
    //    //        }
    //    //    }

    //    //}


    //    //アンカー点となるPointCollectionからベジェ曲線のPathGeometryを作成
    //    //private PathGeometry MakeBezierPathGeometry(PointCollection pc)
    //    //{
    //    //    //PolyBezierSegment作成
    //    //    //PointCollectionをアンカー点に見立てて、その制御点を追加していく
    //    //    PolyBezierSegment seg = new PolyBezierSegment();
    //    //    seg.Points.Add(pc[0]);//始点制御点
    //    //    for (int i = 1; i < pc.Count - 1; i++)
    //    //    {
    //    //        seg.Points.Add(pc[i]);//制御点
    //    //        seg.Points.Add(pc[i]);//アンカー
    //    //        seg.Points.Add(pc[i]);//制御点
    //    //    }
    //    //    seg.Points.Add(pc[^1]);//終点制御点
    //    //    seg.Points.Add(pc[^1]);//終点アンカー

    //    //    //
    //    //    var fig = new PathFigure();
    //    //    fig.StartPoint = pc[0];
    //    //    fig.Segments.Add(seg);
    //    //    var pg = new PathGeometry();
    //    //    pg.Figures.Add(fig);
    //    //    return pg;
    //    //}

    //    // アンカーに制御点を加えたPointCollectionを作成
    //    private PointCollection AddPointToAnchorPoints(PointCollection anchorPoints)
    //    {
    //        PointCollection pc = [];
    //        pc.Add(anchorPoints[0]);// 始点
    //        pc.Add(anchorPoints[0]);// 始点の制御点
    //        for (int i = 1; i < anchorPoints.Count; i++)
    //        {
    //            pc.Add(anchorPoints[i]);// 制御点
    //            pc.Add(anchorPoints[i]);// アンカー
    //            pc.Add(anchorPoints[i]);// 制御点
    //        }
    //        pc.Add(anchorPoints[^1]);// 終点の制御点
    //        pc.Add(anchorPoints[^1]);// 終点
    //        return pc;
    //    }


    //    //指定間隔で選んだアンカー点を返す
    //    private PointCollection ChoiceAnchorPoint(PointCollection points, int interval)
    //    {
    //        var selectedPoints = new PointCollection();
    //        for (int i = 0; i < points.Count - 1; i += interval)
    //        {
    //            selectedPoints.Add(points[i]);
    //        }
    //        selectedPoints.Add(points[points.Count - 1]);//最後の一個は必ず入れる

    //        //選んだ頂点が3個以上あって、最後の頂点と最後から2番めが近いときは2番めを除去            
    //        if (selectedPoints.Count >= 3)
    //        {
    //            int mod = (points.Count - 2) % interval;
    //            if (interval / 2 > mod)
    //            {
    //                selectedPoints.RemoveAt(selectedPoints.Count - 2);//除去
    //            }
    //        }
    //        return selectedPoints;
    //    }


    //    ////前後のアンカー点の平均距離
    //    //private (double begin, double end) DistanceAverage(Point beginP, Point currentP, Point endP)
    //    //{
    //    //    double bSide = GetDistance(currentP, beginP);
    //    //    double eSide = GetDistance(currentP, endP);
    //    //    double average = (bSide + eSide) / 2.0;
    //    //    return (average, average);
    //    //}
    //    ////前後のアンカー点それぞれの距離
    //    //private (double begin, double end) DistanceSeparate(Point beginP, Point currentP, Point endP)
    //    //{
    //    //    double bSide = GetDistance(currentP, beginP);
    //    //    double eSide = GetDistance(currentP, endP);
    //    //    return (bSide, eSide);
    //    //}
    //    ////前後のアンカー点距離の短いほう
    //    //private (double begin, double end) DistanceShorter(Point beginP, Point currentP, Point endP)
    //    //{
    //    //    double bSide = GetDistance(currentP, beginP);
    //    //    double eSide = GetDistance(currentP, endP);
    //    //    double shorter = (bSide > eSide) ? eSide : bSide;
    //    //    return (shorter, shorter);
    //    //}
    //    ////前後の制御点間の距離
    //    //private (double begin, double end) DistanceFrontAndBackAnchor(Point beginP, Point currentP, Point endP)
    //    //{
    //    //    double distance = GetDistance(beginP, endP);
    //    //    return (distance, distance);
    //    //}


    //    #region 依存関係プロパティ

    //    public DistanceType MyDistanceType
    //    {
    //        get { return (DistanceType)GetValue(MyDistanceTypeProperty); }
    //        set { SetValue(MyDistanceTypeProperty, value); }
    //    }
    //    public static readonly DependencyProperty MyDistanceTypeProperty =
    //        DependencyProperty.Register(nameof(MyDistanceType), typeof(DistanceType), typeof(FreehandGeoShape), new PropertyMetadata(DistanceType.Separate別々));

    //    #endregion 依存関係プロパティ



    //    ////2点間距離を取得
    //    //private static double GetDistance(Point p1, Point p2)
    //    //{
    //    //    return Math.Sqrt(Math.Pow(p2.X - p1.X, 2.0) + Math.Pow(p2.Y - p1.Y, 2.0));
    //    //}

    //    ////2点間線分のラジアン(弧度)を取得
    //    //private static double GetRadianFrom2Points(Point begin, Point end)
    //    //{
    //    //    return Math.Atan2(end.Y - begin.Y, end.X - begin.X);
    //    //}
    //}

}
