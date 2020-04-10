# RvtToTopoJson

Export Rooms from Revit to GeoJson for Power BI Shape Maps.

[![Watch the video](https://img.youtube.com/vi/O-y7gwi6wtk/maxresdefault.jpg)](https://youtu.be/O-y7gwi6wtk)

- Credits: https://forums.autodesk.com/t5/revit-architecture-forum/projecting-revit-model-on-a-map-using-geojson/td-p/7973803
- GeoJson wiki: https://macwright.org/2015/03/23/geojson-second-bite.html
- https://docs.microsoft.com/en-us/power-bi/visuals/desktop-shape-map
- GeoJson to TopoJson converter: 
  - https://mapshaper.org/
  - http://jeffpaine.github.io/geojson-topojson/

The External Command code is saved in [GeoJson.cs](https://github.com/giobel/RvtToTopoJson/blob/master/RvtToTopoJson/GeoJson.cs). 
It takes a Room corner points, its name and its area and it exports them as GeoJson using the GeoJSON.Net library (credits to Daniel Ignjat for sharing his code on the Autodesk Forum).
The GeoJson file is saved in C:\Temp\export.json. I've then used mapshaper to transform it into a TopoJson. This file can be imported in Power BI using a Shape map. The TopoJson file can be expanded to access its properties but it's not the best way to do it (I think it would be better to save Room Name and Area in a csv file and use that as a table source in Power BI). 
To update all the maps at the same time, the tables must have a relationship between them. I've manually created a new Table merging the Room Names and link it to the Name's column of the other Tables. Again, this is not the best way to do it. Having the Room Names and Areas in a database and pull the data from there would be the best option.
