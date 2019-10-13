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

namespace doka
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


            Transform ttr = doc.ActiveProjectLocation.GetTotalTransform().Inverse;
            Transform projectlocationTransform = GetProjectLocationTransform(doc);

            IList<Reference> roomReferences = uidoc.Selection.PickObjects(ObjectType.Element, "Select some rooms");

            Polygon polygon = null;

            foreach (Reference re in roomReferences)
            {

                Element roomElement = doc.GetElement(re);

                string roomName = roomElement.LookupParameter("Name").AsString();

                SpatialElement se = doc.GetElement(re) as SpatialElement;

                //multiPolygon.Add(Helpers.RoomBoundary(doc, re, ttr));

                polygon = new Polygon(new List<LineString>
                                {
                                    new LineString(new List<IPosition>
                                    {
                                        new Position(0,0),
                                        new Position(0,1),
                                        new Position(2,1),
                                        new Position(2,0),
                                        new Position(0,0),
                                    })
                                });

                multiPolygon.Add(polygon);

            }

            var coordinates1 = new List<Position>
                {
                    new Position(0,0),
                    new Position(1,0),
                    new Position(1,2),
                    new Position(0,2),
                    new Position(0,0)

                }.ToList<IPosition>();

            var polygon1 = new Polygon(new List<LineString> { new LineString(coordinates1) });
            var featureProperties1 = new Dictionary<string, object> { { "Name", "Foo" },{ "Area", "456" } };
            var model1 = new GeoJSON.Net.Feature.Feature(polygon1, featureProperties1);

            var coordinates2 = new List<Position>
                {
                    new Position(0,2),
                    new Position(0,3),
                    new Position(1,3),
                    new Position(2,2),
                    new Position(0,2)

                }.ToList<IPosition>();

            var polygon2 = new Polygon(new List<LineString> { new LineString(coordinates2) });
            var featureProperties2 = new Dictionary<string, object> { { "Name", "Boo" },{ "Area", "123" } };
            var model2 = new GeoJSON.Net.Feature.Feature(polygon2, featureProperties2);



            var modelli = new FeatureCollection(new List<Feature> { model1, model2 });

            #region File Export
            //EXPORT GEOJSON

            FileExport export = new FileExport(multiPolygon);
            export.ExportGEOJson(@"C:\Temp\Samples\coordinates");

            var serializedData = JsonConvert.SerializeObject(modelli, Formatting.Indented);
            StreamWriter writetext = new StreamWriter(@"C:\Temp\Samples\fence.json");
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

