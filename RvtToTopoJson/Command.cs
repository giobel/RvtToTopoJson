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

namespace RvtToTopoJson {
    [TransactionAttribute(TransactionMode.Manual)]
    [RegenerationAttribute(RegenerationOption.Manual)]
    public class dokaGeoReader : IExternalCommand {

        private List<Polygon> multiPolygon = new List<Polygon>();

        private List<IPosition> positions = new List<IPosition>();
        private List<XYZ> positionsfortxt = new List<XYZ>();
        Position firstpos;

        UIApplication uiApp;
        Document doc;
        private bool initiated = false;
        XYZ p;
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements) {
            uiApp = commandData.Application;
            UIDocument uidoc = uiApp.ActiveUIDocument;
            doc = uiApp.ActiveUIDocument.Document;
            //Transform ttr = doc.ActiveProjectLocation.GetTotalTransform().Inverse;
            //Transform projectlocationTransform = GetProjectLocationTransform(doc);
            //FilteredElementCollector wallcollector = new FilteredElementCollector(doc).OfClass(typeof(Wall)).WhereElementIsNotElementType();
            //bool isconcrete = true;
            //foreach (var element in wallcollector)
            //{
            //    //foreach (var mat in element.GetMaterialIds(false))
            //    //{
            //    //    if (doc.GetElement(mat).Name.Contains("Concrete")) isconcrete = true;
            //    //}
            //    if (isconcrete is false) continue;
            //    GeometryElement geo = element.get_Geometry(new Options());
            //    if (geo is null) continue;

            //    foreach (var g in geo)
            //    {
            //        Solid geosolid = g as Solid;
            //        if (geosolid is null) continue;
            //        if (geosolid.Faces.Size is 0) continue;
            //        foreach (Face f in geosolid.Faces)
            //        {
            //            Mesh mesh = f.Triangulate(1);
            //            foreach (var xyz in mesh.Vertices)
            //            {
            //                //p = projectlocationTransform.OfPoint(xyz);
            //                p = ttr.OfPoint(xyz);
            //                //p=xyz;
            //                if (mesh.Vertices.Count >= 40) continue;
            //                if (initiated == false)
            //                {
            //                    firstpos = new Position(p.Y, p.X, p.Z);
            //                    initiated = true;
            //                }
            //                positions.Add(new Position(p.Y, p.X, p.Z));
            //            }
            //        }
            //        positions.Add(firstpos);
            //        initiated = false;
            //        multiPolygon.Add(new Polygon(new List<LineString>
            //                    {
            //                        new LineString(positions)
            //                    }));
            //        positions.Clear();

            //    }


            //}


            Reference re = uidoc.Selection.PickObject(ObjectType.Element, "Select room");

            SpatialElement se = doc.GetElement(re) as SpatialElement;

            SpatialElementBoundaryOptions opt = new SpatialElementBoundaryOptions();

            IList<IList<BoundarySegment>> loops = se.GetBoundarySegments(opt);

            List<IPosition> roomCorners = new List<IPosition>();

            foreach (IList<BoundarySegment> loop in loops)
            {
                foreach (BoundarySegment seg in loop)
                {
                    Line segLine = seg.GetCurve() as Line;

                    XYZ endPt = segLine.GetEndPoint(0);

                    roomCorners.Add(new Position(endPt.X, endPt.Y));
                }
            }

            roomCorners.Add(roomCorners.First());

            var polygon = new Polygon(new List<LineString>
            {
                new LineString(roomCorners)
            });


            var properties = new MyFeatureProperty
            {
                Area = 213.5,
                RoomName = "Corridor"
            };

            Feature fe = new Feature(polygon, properties);
            FeatureCollection fc = new FeatureCollection();

            fc.Features.Add(fe);

            #region File Export
            //EXPORT GEOJSON
            //FileExport export = new FileExport(multiPolygon);
            //export.ExportGEOJson("coordinates");

            string json = JsonConvert.SerializeObject(fc, Formatting.Indented);

            //StreamWriter writetext = new StreamWriter(@"C:\Temp\Samples\export" + ".json");
            //writetext.WriteLine(json);
            //writetext.Flush();
            //writetext.Close();

            #endregion

            TopoJson.Welcome wel = new TopoJson.Welcome()
            {
                Type = "Topology",
                Transform = new TopoJson.Transform { Scale = new long[2] { 1, 1 }, Translate = new long[2] { 0, 0 } },
                Objects = new TopoJson.Objects {
                    Example = new TopoJson.Example {
                        Type = "GeometryCollection",
                        Geometries = new List<TopoJson.Geometry>(),
                    } },
                Arcs = new List<List<List<double>>>()
            };
            
            List<List<(double, double)>> arc = new List<List<(double, double)>>();

            arc.Add(new List<(double, double)> { (2, 2), (1, 0), (0, -2), (-1, 0), (0, 2) });

            List<TopoJson.Properties> roomProperties = new List<TopoJson.Properties>();

            roomProperties.Add( new TopoJson.Properties() { Postal = "WA", RoomName = "Corridor", Area = 123 } );
            roomProperties.Add( new  TopoJson.Properties() { Postal = "SA", RoomName = "Kitchen", Area = 456});

            for (int i = 0; i < 2; i++)
            {
                wel.Objects.Example.Geometries.Add(new TopoJson.Geometry
                {
                    Type = "Polygon",
                    Properties = roomProperties[i],
                    Arcs = new List<List<int>> { new List<int> { i } }
                });
            }

            wel.Arcs.Add(new List<List<double>> { new List<double> { 2, 0.4 },
                                                  new List<double> { -1, 0 },
                                                  new List<double>{0, 0.8 },
                                                  new List<double>{ 1, 0 },
                                                  new List<double>{ 0, -0.8 }                                                           
            });

            wel.Arcs.Add(new List<List<double>> { new List<double> { 2, 2 },
                                                  new List<double> { 1, 0 },
                                                  new List<double>{0, -2 },
                                                  new List<double>{ -1, 0 },
                                                  new List<double>{ 0, 2 }
            });

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
        Transform GetProjectLocationTransform(Document doc) {
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

