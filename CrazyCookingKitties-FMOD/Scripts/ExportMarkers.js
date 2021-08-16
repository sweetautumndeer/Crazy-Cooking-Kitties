/* Autumn Moulios
Last Updated 08/15/2021
ExportMarkers.js
Creates a json file containing all marker information
every time FMOD builds, or when the menu option is selected
*/

// Global Variables
var projectPath = studio.project.filePath;
var outputPath = projectPath + "/../../Assets/Saves/MarkerInfo.json";
var file = studio.system.getFile(outputPath);

function ExportMarkers(success) {
    var events = studio.project.model.Event.findInstances();
    var markerInfo = {Markers: []}; // data structure for writing to json

    for (var x in events) {
        for (var y in events[x].markerTracks) {
            for (var z in events[x].markerTracks[y].markers) {
                // data structure for each separate marker, nested in markerInfo
                var mark = {name:events[x].markerTracks[y].markers[z].name, 
                            position:events[x].markerTracks[y].markers[z].position};
                markerInfo.Markers.push(mark);
            }
        }
    }

    //probably gonna quicksort the above data by position

    //write markerInfo to json
    file.open(studio.system.openMode.WriteOnly);
    file.writeText(JSON.stringify(markerInfo));
    file.close();
}

//run ExportMarkers() every time the project is built
studio.project.buildEnded.connect(ExportMarkers);

//add a menu option under "Scripts"
studio.menu.addMenuItem({ name: "Export Markers",
        isEnabled: true,
        keySequence: "Alt+E",
        execute: ExportMarkers
});
