using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using GeoJSON.Net.Geometry;
using GeoJSON.Net.Feature;

//credits https://forums.autodesk.com/t5/revit-architecture-forum/projecting-revit-model-on-a-map-using-geojson/td-p/7973803

namespace RvtToTopoJson
{
    public class FileExport {
        private string exportString = "";

        public string ExportString { get => exportString; set => exportString = value; }

        private List<Polygon> poly = new List<Polygon>();

        public FileExport(List<Polygon> poly) {
            this.poly=poly;
        }

        public void CreateTextFile(string name, IDictionary<string, HashSet<object>> data) {
            StreamWriter writetext = new StreamWriter(name);
            foreach (var a in data.Keys)
            {
                ExportString += a + ",\n";
                foreach (var i in data[a])
                {
                    ExportString += i.ToString() + ",\n";
                }
            }
            writetext.WriteLine(ExportString);
            writetext.Flush();
            writetext.Close();

        }
        public void CreateTextFile(string name, HashSet<object> data) {
            StreamWriter writetext = new StreamWriter(name);
            foreach (var i in data)
            {
                ExportString += i.ToString() + "\n";
            }
            writetext.WriteLine(ExportString);
            writetext.Flush();
            writetext.Close();
        }
        public MultiPolygon Geometries(List<Polygon> polygons) {
            var multipolygon = new MultiPolygon(polygons);
            return multipolygon;
        }
        public void ExportGEOJson(string name) {
            var feature = new Feature(Geometries(poly));
            string json = JsonConvert.SerializeObject(feature,Formatting.Indented);
            StreamWriter writetext = new StreamWriter(name+".json");
            writetext.WriteLine(json);
            writetext.Flush();
            writetext.Close();
        }

    }
}
