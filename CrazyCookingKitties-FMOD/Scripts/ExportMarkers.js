/* Autumn Moulios
Last Updated 08/15/2021
ExportMarkers.js
Creates a json file containing all marker information
every time FMOD builds, or when the menu option is selected
reference:
https://qa.fmod.com/t/can-you-access-a-list-of-markers-at-runtime/11819/11
*/

// Global Variables
var projectPath = studio.project.filePath;
var outputPath = projectPath + "/../../Assets/Saves/MarkerInfo.json";
var file = studio.system.getFile(outputPath);

function ExportMarkers(success) {
    var events = studio.project.model.Event.findInstances();
    var markerInfo = {markers: []}; // data structure for writing to json

    for (var x in events) {
        for (var y in events[x].markerTracks) {
            for (var z in events[x].markerTracks[y].markers) {
                // data structure for each separate marker, nested in markerInfo
                if (events[x].markerTracks[y].markers[z].name && events[x].markerTracks[y].markers[z].position){
                    var mark = {name: events[x].markerTracks[y].markers[z].name, 
                                position: events[x].markerTracks[y].markers[z].position};
                    markerInfo.markers.push(mark);
                }
            }
        }
    }

    //sort by position
    BubbleSort(markerInfo.markers);

    //write markerInfo to json
    file.open(studio.system.openMode.WriteOnly);
    file.writeText(JSON.stringify(markerInfo));
    file.close();
}

//Copied this from the internet babey!!
function BubbleSort(arr){
    for(var i = 0; i < arr.length; i++){
        for(var j = 0; j < ( arr.length - i -1 ); j++){
            if(arr[j].position > arr[j+1].position){
                var temp = arr[j]
                arr[j] = arr[j + 1]
                arr[j+1] = temp
            }
        }
    }
}

//run ExportMarkers() every time the project is built
studio.project.buildEnded.connect(ExportMarkers);

//add a menu option under "Scripts"
studio.menu.addMenuItem({ name: "Export Markers",
        isEnabled: true,
        keySequence: "Alt+E",
        execute: ExportMarkers
});
