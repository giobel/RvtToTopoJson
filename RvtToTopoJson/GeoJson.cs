using System.Collections.Generic;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;
using GeoJSON.Net.Geometry;
using RvtToTopoJson;
using System.IO;
using Autodesk.Revit.UI.Selection;
using GeoJSON.Net.Feature;
using Newtonsoft.Json;
using System.Linq;
using System;

namespace RvtToTopoJson
{
    [TransactionAttribute(TransactionMode.Manual)]
    [RegenerationAttribute(RegenerationOption.Manual)]
    public class CreateGeoJson : IExternalCommand
    {
        private List<Polygon> multiPolygon = new List<Polygon>();
        private List<IPosition> positions = new List<IPosition>();
        private List<XYZ> positionsfortxt = new List<XYZ>();
        Position firstpos;
        UIApplication uiApp;
        Document doc;
        private bool initiated = false;
        XYZ p;
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            uiApp = commandData.Application;
            UIDocument uidoc = uiApp.ActiveUIDocument;
            doc = uidoc.Document;

            try
            {

                Transform ttr = doc.ActiveProjectLocation.GetTotalTransform().Inverse;
                Transform projectlocationTransform = GetProjectLocationTransform(doc);

                IList<Reference> roomReferences = uidoc.Selection.PickObjects(ObjectType.Element, "Select some rooms");

                SpatialElementBoundaryOptions opt = new SpatialElementBoundaryOptions();

                //control the boundary location
                opt.SpatialElementBoundaryLocation = SpatialElementBoundaryLocation.Center;

                var features = new List<Feature>();

                foreach (Reference re in roomReferences)
                {

                    Element roomElement = doc.GetElement(re);

                    string roomName = roomElement.LookupParameter("Name").AsString();

                    SpatialElement se = doc.GetElement(re) as SpatialElement;

                    //multiPolygon.Add(Helpers.RoomBoundary(doc, re, ttr));

                    IList<IList<BoundarySegment>> loops = se.GetBoundarySegments(opt);

                    var featureProps = new Dictionary<string, object> { { "Name", roomName }, { "Area", se.Area } };

                    var coordinates = new List<Position>();

                    foreach (IList<BoundarySegment> loop in loops)
                    {
                        ElementId segId = new ElementId(123456);

                        foreach (BoundarySegment seg in loop)
                        {
                            Line segLine = seg.GetCurve() as Line;
                            XYZ endPt = segLine.GetEndPoint(0);

                            if (segId == seg.ElementId)
                            {

                            }
                            else
                            {
                                //TaskDialog.Show("re", $"{endPt.Y} {endPt.X}");
                                coordinates.Add(new Position(endPt.Y * 0.3048, endPt.X * 0.3048));
                            }

                            segId = seg.ElementId;

                        }
                    }

                    coordinates.Add(coordinates.First());

                    var polygon = new Polygon(new List<LineString> { new LineString(coordinates) });

                    features.Add(new Feature(polygon, featureProps));

                }


                var models = new FeatureCollection(features);



                #region File Export
                //EXPORT GEOJSON
                var serializedData = JsonConvert.SerializeObject(models, Formatting.Indented);
                StreamWriter writetext = new StreamWriter(@"C:\Temp\export.json");
                writetext.WriteLine(serializedData);
                writetext.Flush();
                writetext.Close();

                #endregion
                //Activate command in revit
                #region Revit Start Command
                Transaction trans = new Transaction(doc);
                trans.Start("GeoReader");
                trans.Commit();
                TaskDialog.Show("File was built", "Success");
                #endregion


                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", ex.Message);
                return Result.Failed;
            }
        }


        Transform GetProjectLocationTransform(Document doc)
        {
            // Retrieve the active project location position.

            ProjectPosition projectPosition
              = doc.ActiveProjectLocation.GetProjectPosition(XYZ.Zero);

            // Create a translation vector for the offsets

            XYZ translationVector = new XYZ(
              projectPosition.EastWest,
              projectPosition.NorthSouth,
              projectPosition.Elevation);

            Transform translationTransform
              = Transform.CreateTranslation(
                translationVector);

            // Create a rotation for the angle about true north

            Transform rotationTransform
              = Transform.CreateRotation(
                XYZ.BasisZ, projectPosition.Angle);

            // Combine the transforms 

            Transform finalTransform
              = translationTransform.Multiply(
                rotationTransform);

            return finalTransform;
        }

    }

    public class Request
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public Fence Fence { get; set; }
    }
    public class Fence
    {
        public int Type { get; set; }
        public FeatureCollection Values { get; set; }
    }
}

