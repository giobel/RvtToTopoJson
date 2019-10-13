using Autodesk.Revit.DB;
using GeoJSON.Net.Geometry;
using System.Collections.Generic;

namespace RvtToTopoJson
{
    internal class Helpers
    {
        public double Area { get; set; }
        public string RoomName { get; set; }

        public static Polygon RoomBoundary(Document doc, Reference re, Transform ttr)
        {
            SpatialElement se = doc.GetElement(re) as SpatialElement;

            SpatialElementBoundaryOptions opt = new SpatialElementBoundaryOptions();

            IList<IList<BoundarySegment>> loops = se.GetBoundarySegments(opt);

            List<IPosition> positions = new List<IPosition>();


            foreach (IList<BoundarySegment> loop in loops)
            {

                ElementId segId = new ElementId(123456);

                foreach (BoundarySegment seg in loop)
                {
                    Line segLine = seg.GetCurve() as Line;

                    XYZ endPt = segLine.Origin;

                    XYZ p = ttr.OfPoint(endPt);

                    Position firstpos = new Position(p.Y, p.X, p.Z);


                    if (segId == seg.ElementId)
                    {

                    }
                    else
                    {
                        positions.Add(new Position(p.Y, p.X, p.Z));
                    }

                    positions.Add(firstpos);

                    segId = seg.ElementId;

                }
            }

            return new Polygon(new List<LineString> { new LineString(positions) });
        }

    }

}