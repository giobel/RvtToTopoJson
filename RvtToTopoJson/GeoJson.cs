using System.Collections.Generic;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;
using GeoJSON.Net.Geometry;
using RvtToTopoJson;
using System.IO;

namespace doka
{
    [TransactionAttribute(TransactionMode.Manual)]
    [RegenerationAttribute(RegenerationOption.Manual)]
    public class dokaGeoReader : IExternalCommand
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
            doc = uiApp.ActiveUIDocument.Document;
            Transform ttr = doc.ActiveProjectLocation.GetTotalTransform().Inverse;
            Transform projectlocationTransform = GetProjectLocationTransform(doc);



            FilteredElementCollector wallcollector = new FilteredElementCollector(doc).OfClass(typeof(Wall)).WhereElementIsNotElementType();
            bool isconcrete = true;
            foreach (var element in wallcollector)
            {
                //foreach (var mat in element.GetMaterialIds(false))
                //{
                //    if (doc.GetElement(mat).Name.Contains("Concrete")) isconcrete = true;
                //}
                if (isconcrete is false) continue;
                GeometryElement geo = element.get_Geometry(new Options());
                if (geo is null) continue;

                foreach (var g in geo)
                {
                    Solid geosolid = g as Solid;
                    if (geosolid is null) continue;
                    if (geosolid.Faces.Size is 0) continue;
                    foreach (Face f in geosolid.Faces)
                    {
                        Mesh mesh = f.Triangulate(1);
                        foreach (var xyz in mesh.Vertices)
                        {
                            //p = projectlocationTransform.OfPoint(xyz);
                            p = ttr.OfPoint(xyz);
                            //p=xyz;
                            if (mesh.Vertices.Count >= 40) continue;
                            if (initiated == false)
                            {
                                firstpos = new Position(p.Y, p.X, 0);
                                initiated = true;
                            }
                            positions.Add(new Position(p.Y, p.X, 0));
                        }
                    }
                    positions.Add(firstpos);
                    initiated = false;
                    multiPolygon.Add(new Polygon(new List<LineString>
                                {
                                    new LineString(positions)
                                }));
                    positions.Clear();

                }


            }

            #region File Export
            //EXPORT GEOJSON
            
            FileExport export = new FileExport(multiPolygon);
            export.ExportGEOJson(@"C:\Temp\Samples\coordinates");
            

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
}

