using System;
using System.Collections.Generic;
using System.Linq;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.ApplicationServices;
using GeoJSON.Net.Geometry;
using Newtonsoft.Json;
using Autodesk.Revit.UI.Selection;
using GeoJSON.Net.Feature;
using System.IO;

namespace RvtToTopoJson
{
    [TransactionAttribute(TransactionMode.Manual)]
    [RegenerationAttribute(RegenerationOption.Manual)]
    public class Command : IExternalCommand
    {
        UIApplication uiApp;
        Document doc;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            uiApp = commandData.Application;
            UIDocument uidoc = uiApp.ActiveUIDocument;
            doc = uiApp.ActiveUIDocument.Document;

            //Create a topojson object
            TopoJson.Welcome wel = new TopoJson.Welcome()
            {
                Type = "Topology",
                Transform = new TopoJson.Transform { Scale = new long[2] { 1, 1 }, Translate = new long[2] { 0, 0 } },
                Objects = new TopoJson.Objects
                {
                    Example = new TopoJson.Example
                    {
                        Type = "GeometryCollection",
                        Geometries = new List<TopoJson.Geometry>(),
                    }
                },
                Arcs = new List<List<List<double>>>()
            };

            List<TopoJson.Properties> roomProperties = new List<TopoJson.Properties>();

            //roomProperties.Add(new TopoJson.Properties() { Postal = "WA", RoomName = "Corridor", Area = 123 });
            //roomProperties.Add(new TopoJson.Properties() { Postal = "SA", RoomName = "Kitchen", Area = 456 });

            IList<Reference> roomReferences = uidoc.Selection.PickObjects(ObjectType.Element, "Select some rooms");

            double oldX = 0;
            double oldY = 0;

            foreach (Reference re in roomReferences)
            {
                Element roomElement = doc.GetElement(re);

                string roomName = roomElement.LookupParameter("Name").AsString();

                SpatialElement se = doc.GetElement(re) as SpatialElement;

                //add room properties
                roomProperties.Add(new TopoJson.Properties() { Number = se.Number, RoomName = roomName, Area = se.Area });

                for (int i = 0; i < roomProperties.Count; i++)
                {
                    wel.Objects.Example.Geometries.Add(new TopoJson.Geometry
                    {
                        Type = "Polygon",
                        Properties = roomProperties[i],
                        Arcs = new List<List<int>> { new List<int> { i } }
                    });
                }

                SpatialElementBoundaryOptions opt = new SpatialElementBoundaryOptions();

                IList<IList<BoundarySegment>> loops = se.GetBoundarySegments(opt);

                foreach (IList<BoundarySegment> loop in loops)
                {
                    List<List<double>> nestedList = new List<List<double>>();


                    ElementId segId = new ElementId(123456);


                    foreach (BoundarySegment seg in loop)
                    {

                        Line segLine = seg.GetCurve() as Line;
                        XYZ endPt = segLine.Origin;

                        if (segId == seg.ElementId)
                        {

                        }
                        else
                        {
                            //nestedList.Add(new List<double> { endPt.X - oldX, endPt.Y - oldY });
                            nestedList.Add(new List<double> { endPt.X, endPt.Y });
                        }

                        segId = seg.ElementId;

                        oldX = endPt.X;
                        oldY = endPt.Y;

                    }

                    wel.Arcs.Add(nestedList);
                }


            }




            #region GeoJson
            Transform ttr = doc.ActiveProjectLocation.GetTotalTransform().Inverse;
            Transform projectlocationTransform = GetProjectLocationTransform(doc);

            List<Polygon> multiPolygon = new List<Polygon>();

            List<IPosition> positions = new List<IPosition>();
            List<XYZ> positionsfortxt = new List<XYZ>();
            Position firstpos;
            XYZ p;


            foreach (Reference re in roomReferences)
            {
                Element roomElement = doc.GetElement(re);
                
                multiPolygon.Add(Helpers.RoomBoundary(doc, re, ttr));
            }



            #endregion

            #region File Export
            //EXPORT GEOJSON
            FileExport export = new FileExport(multiPolygon);
            export.ExportGEOJson("coordinates");

            StreamWriter writeGeojson = new StreamWriter(@"C:\Temp\Samples\export" + ".json");
            //writegoeJson.WriteLine(json);
            writeGeojson.Flush();
            writeGeojson.Close();

            #endregion

            var welcome = TopoJson.Serialize.ToJson(wel);

            StreamWriter writetext = new StreamWriter(@"C:\Temp\Samples\topoJson" + ".json");
            writetext.WriteLine(welcome);
            writetext.Flush();
            writetext.Close();


            //Activate command in revit
            #region Revit Start Command
            Transaction trans = new Transaction(doc);
            trans.Start("GeoReader");
            trans.Commit();
            TaskDialog.Show("File was built", "Success");
            #endregion
            return Result.Succeeded;
        }

        private Transform GetProjectLocationTransform(Document doc)
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

}
