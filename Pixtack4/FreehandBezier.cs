using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Pixtack4
{
    //方向線の長さの決め方の種類
    public enum DistanceType
    {
        Zero0距離,
        Average平均,
        Separate別々,
        Shorter短いほう,
        FrontBack前後間
    }
    public enum RadianType
    {
        Parallel平行,
        RightAngleOfCenter中間の直角
    }

    public class FreehandBezier : GeoShape
    {
        public PointCollection MyOriginPoints { get; private set; }// 元のPoints

        public FreehandBezier(PointCollection points)
        {
            MyShapeType = ShapeType.Bezier; 
            MyOriginPoints = points;
            //MyPoints = ChoiceAnchorPoint(MyOriginPoints, interval: 10);
            PointCollection AnchorPoints = ChoiceAnchorPoint(MyOriginPoints, interval: 10);
            var path = MakeBezierPathGeometry(AnchorPoints);

        }

        //ベジェ曲線PathからSegmentのPointsを取得
        private PointCollection GetPolyBezierSegmentPoints(Path bezierPath)
        {
            var pg = (PathGeometry)bezierPath.Data;
            PathFigure fig = pg.Figures[0];
            var seg = (PolyBezierSegment)fig.Segments[0];
            return seg.Points;
        }

        //制御点座標を決めて曲線化
        private void ToCurve(Path bezierPath, double curve, DistanceType distanceType, RadianType radianType)
        {
            PointCollection segPoints = GetPolyBezierSegmentPoints(bezierPath);
            for (int i = 1; i < AnchorPoints.Count - 1; i++)
            {
                Point beginAnchor = AnchorPoints[i - 1];
                Point currentAnchor = AnchorPoints[i];
                Point endAnchor = AnchorPoints[i + 1];
                //方向線距離
                if (radianType == RadianType.RightAngleOfCenter中間の直角)
                {
                    double beginDistance = 0, endDistance = 0;
                    switch (distanceType)
                    {
                        case DistanceType.Zero0距離:
                            break;
                        case DistanceType.Average平均:
                            (beginDistance, endDistance) = DistanceAverage(beginAnchor, currentAnchor, endAnchor);
                            break;
                        case DistanceType.Separate別々:
                            (beginDistance, endDistance) = DistanceSeparate(beginAnchor, currentAnchor, endAnchor);
                            break;
                        case DistanceType.Shorter短いほう:
                            (beginDistance, endDistance) = DistanceShorter(beginAnchor, currentAnchor, endAnchor);
                            break;
                        case DistanceType.FrontBack前後間:
                            (beginDistance, endDistance) = DistanceFrontAndBackAnchor(beginAnchor, currentAnchor, endAnchor);
                            break;
                        default:
                            break;
                    }

                    //方向線弧度取得
                    (double bRadian, double eRadian) = GetRadianDirectionLine(beginAnchor, currentAnchor, endAnchor);
                    //(double bRadian, double eRadian) = SharpTest(beginAnchor, currentAnchor, endAnchor);
                    //始点側制御点座標
                    double xDiff = Math.Cos(bRadian) * beginDistance * curve;
                    double yDiff = Math.Sin(bRadian) * beginDistance * curve;
                    segPoints[i * 3 - 2] = new Point(currentAnchor.X + xDiff, currentAnchor.Y + yDiff);
                    //終点側制御点座標
                    xDiff = Math.Cos(eRadian) * endDistance * curve;
                    yDiff = Math.Sin(eRadian) * endDistance * curve;
                    segPoints[i * 3] = new Point(currentAnchor.X + xDiff, currentAnchor.Y + yDiff);
                }
                else if (radianType == RadianType.Parallel平行)
                {
                    Point bSide = currentAnchor;
                    Point eSide = currentAnchor;
                    switch (distanceType)
                    {
                        case DistanceType.Zero0距離:
                            break;
                        case DistanceType.Average平均:
                            (bSide, eSide) = ControlPointsAverage(beginAnchor, currentAnchor, endAnchor, curve);
                            break;
                        case DistanceType.Separate別々:
                            (bSide, eSide) = ControlPointsSeparate(beginAnchor, currentAnchor, endAnchor, curve);
                            break;
                        case DistanceType.Shorter短いほう:
                            (bSide, eSide) = ControlPointsShorter(beginAnchor, currentAnchor, endAnchor, curve);
                            break;
                        case DistanceType.FrontBack前後間:
                            (bSide, eSide) = ControlPointsFrontAndBack(beginAnchor, currentAnchor, endAnchor, curve);
                            break;
                        default:
                            break;
                    }
                    segPoints[i * 3 - 2] = bSide;
                    segPoints[i * 3] = eSide;
                }
            }

        }


        //アンカー点となるPointCollectionからベジェ曲線のPathGeometryを作成
        private PathGeometry MakeBezierPathGeometry(PointCollection pc)
        {
            //PolyBezierSegment作成
            //PointCollectionをアンカー点に見立てて、その制御点を追加していく
            PolyBezierSegment seg = new PolyBezierSegment();
            seg.Points.Add(pc[0]);//始点制御点
            for (int i = 1; i < pc.Count - 1; i++)
            {
                seg.Points.Add(pc[i]);//制御点
                seg.Points.Add(pc[i]);//アンカー
                seg.Points.Add(pc[i]);//制御点
            }
            seg.Points.Add(pc[^1]);//終点制御点
            seg.Points.Add(pc[^1]);//終点アンカー

            //
            var fig = new PathFigure();
            fig.StartPoint = pc[0];
            fig.Segments.Add(seg);
            var pg = new PathGeometry();
            pg.Figures.Add(fig);
            return pg;
        }


        //指定間隔で選んだアンカー点を返す
        private PointCollection ChoiceAnchorPoint(PointCollection points, int interval)
        {
            var selectedPoints = new PointCollection();
            for (int i = 0; i < points.Count - 1; i += interval)
            {
                selectedPoints.Add(points[i]);
            }
            selectedPoints.Add(points[points.Count - 1]);//最後の一個は必ず入れる

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
