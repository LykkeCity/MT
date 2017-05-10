var jsonfile = require('jsonfile');

// Read in the file to be patched
var file = process.argv[2]; // e.g. '../src/MyProject/project.json'
if (!file)
    console.log("No filename provided");
console.log("File: " + file);

// Read in the build version (this might be provided by the CI server)
var version = process.argv[3]; // e.g. '1.0.42-beta'
if (!version)
    console.log("No version provided");

jsonfile.readFile(file, function (err, project) {
    project.version = version;
    jsonfile.writeFile(file, project, {spaces: 2}, function(err) {
		if(err) {
			return console.log(err);
		}
		console.log("The file was saved!");
    });
})