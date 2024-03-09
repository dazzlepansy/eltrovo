using SSDEEP.NET;
using VDS.RDF;
using VDS.RDF.Nodes;

public class HashingOperations {
    public static void GetSsdeepHashes(string path) {
        // Graph of RDF triples.
        var graph = new Graph();

        // Establish some general-use nodes in the graph.
        // These nodes come from the RiC ontology.
        var ricoHasGeneticLink = graph.CreateUriNode(UriFactory.Create("https://www.ica.org/standards/RiC/ontology#hasGeneticLinkToRecordResource"));
        var ricoHasInstantiation = graph.CreateUriNode(UriFactory.Create("https://www.ica.org/standards/RiC/ontology#hasOrHadInstantiation"));
        var ricoIdentifier = graph.CreateUriNode(UriFactory.Create("https://www.ica.org/standards/RiC/ontology#identifier"));
        var ricoInstantiation = graph.CreateUriNode(UriFactory.Create("https://www.ica.org/standards/RiC/ontology#Instantiation"));
        var ricoNormalizedValue = graph.CreateUriNode(UriFactory.Create("https://www.ica.org/standards/RiC/ontology#normalizedValue"));
        var ricoRecord = graph.CreateUriNode(UriFactory.Create("https://www.ica.org/standards/RiC/ontology#Record"));
        // This is a generic one from RDF.
        var rdfType = graph.CreateUriNode(UriFactory.Create("http://www.w3.org/1999/02/22-rdf-syntax-ns#type"));

        // This list of extensions defines what files will be selected for comparison.
        var extensions = new List<string>() {
            "doc",
            "docx",
            "epub",
            "md",
            "odt",
            "pdf",
            "txt",
            "jpg"
        };

        // Define the directory to be scanned. All files will be compared with all other files.
        // This will be user-selectable through the UI when the front-end is built.
        var targetDir = new DirectoryInfo(path);

        // Loop through all files in a target directory if they have the desired extension and are larger than 1kb.
        // 1kb is an arbitrary number but it prevents a lot of matches between files that are too small for similarity to be meaningful.
        foreach (var file in GetNonHiddenFiles(targetDir)
        .Where(f => extensions.Contains(f.FullName.Split('.')[^1].ToLower()) && f.Length > 1000)){
            var buffer = File.ReadAllBytes(file.FullName);

            // Create a record and instantiation node for this file.
            var recordNode = graph.CreateBlankNode();
            graph.Assert(recordNode, rdfType, ricoRecord);
            var instantiationNode = graph.CreateBlankNode(file.FullName);
            graph.Assert(instantiationNode, rdfType, ricoInstantiation);
            // Connect the record with its instantiation.
            graph.Assert(recordNode, ricoHasInstantiation, instantiationNode);
            // Save the file's fuzzy hash as the record's normalized value.
            var hashNode = graph.CreateLiteralNode(Hasher.HashBuffer(buffer, buffer.Length));
            graph.Assert(recordNode, ricoNormalizedValue, hashNode);
        }

        // Get all the record nodes.
        var triples = graph.GetTriplesWithPredicateObject(rdfType, ricoRecord);

        var checkedPairs = new List<List<INode>>();

        // Compare the hash of every file to every other file.
        foreach (var triple1 in triples) {
            var hash1 = graph.GetTriplesWithSubjectPredicate(triple1.Subject, ricoNormalizedValue).Single().Object.AsValuedNode().AsString();
            foreach (var triple2 in triples) {
                if (triple1.Subject != triple2.Subject  // Don't compare a node with itself.
                && !checkedPairs.Where(_ => _.Contains(triple1.Subject) && _.Contains(triple2.Subject)).Any()) {   // Or if we've already compared the two.
                    var hash2 = graph.GetTriplesWithSubjectPredicate(triple2.Subject, ricoNormalizedValue).Single().Object.AsValuedNode().AsString();
                    // ssdeep can only compare files with the same block size or block size times two.
                    int.TryParse(hash1.Split(":")[0], out var block_size1);
                    int.TryParse(hash2.Split(":")[0], out var block_size2);
                    if (block_size1 == block_size2 || block_size1 == block_size2 * 2 || block_size2 == block_size1 * 2) {
                        // Compare the two hashes and save the result.
                        var similarity = Comparer.Compare(hash1, hash2);

                        if (similarity == 100) {
                            // If the two are identical, delete the second record and attch its instantiation to the first record.
                            var oldTriples = graph.GetTriplesWithSubjectPredicate(triple2.Subject, ricoHasInstantiation);
                            foreach (var oldTriple in oldTriples) {
                                // Attach the second triple's instantation to the first triple's record.
                                graph.Assert(triple1.Subject, ricoHasInstantiation, oldTriple.Object);
                                // I'm not sure we can actually delete a node --- just retract its triples.
                                graph.Retract(oldTriple);
                            }
                        }
                        else if (similarity > 49) {
                            // If two files are sufficiently similar, create a "genetic link" between the two records.
                            graph.Assert(triple1.Subject, ricoHasGeneticLink, triple2.Subject);
                        }
                    }

                    // Log the fact that we've cheked these two triples.
                    checkedPairs.Add(new List<INode>() { triple1.Subject, triple2.Subject });
                }
            }
        }

        // Output all linked records/instantiations to the console.
        foreach (var triple in graph.GetTriplesWithPredicate(ricoHasGeneticLink)) {
            var subjectInstantiations = graph.GetTriplesWithSubjectPredicate(triple.Subject, ricoHasInstantiation);
            var objectInstantiations = graph.GetTriplesWithSubjectPredicate(triple.Object, ricoHasInstantiation);
            foreach (var subjectInstantiation in subjectInstantiations) {
                foreach (var objectInstantiation in objectInstantiations) {
                    Console.WriteLine(subjectInstantiation.Object.ToString() + " has genetic link to " + objectInstantiation.Object.ToString());
                }
            }
        }
    }

    /// <summary>
    /// Iterate through a directory recursively to get all files that are not hidden and not in hidden directories.
    /// </summary>
    /// <param name="target_dir"></param>
    /// <returns></returns>
    private static IList<FileInfo> GetNonHiddenFiles(DirectoryInfo target_dir) {
        var file_infos = new List<FileInfo>();
        file_infos.AddRange(target_dir.GetFiles("*.*", SearchOption.TopDirectoryOnly).Where(w => (w.Attributes & FileAttributes.Hidden) == 0));
        foreach (var directory in target_dir.GetDirectories("*.*", SearchOption.TopDirectoryOnly).Where(w => (w.Attributes & FileAttributes.Hidden) == 0)) {
            file_infos.AddRange(GetNonHiddenFiles(directory));
        }

        return file_infos;
    }
}