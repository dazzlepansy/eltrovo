using CoenM.ImageHash;
using CoenM.ImageHash.HashAlgorithms;
using MimeMapping;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SSDEEP.NET;
using VDS.RDF;
using VDS.RDF.Nodes;
using VDS.RDF.Writing;

public class HashingOperations {
    // MIME types that refer to images.
    public static List<string> ImageMimeTypes = new List<string>{
        "image/bmp",
        "image/gif",
        "image/jpeg",
        "image/png",
        "image/tiff"
    };

    // MIME types that refer to documents.
    public static List<string> DocumentMimeTypes = new List<string>{
        "application/epub+zip", // epub
        "application/msword", // doc
        "application/pdf", // pdf
        "application/vnd.oasis.opendocument.text", // odt
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document", // docx
        "text/markdown", // md
        "text/plain" // txt
    };

    // This list of MIME types defines what files will be selected for comparison.
    public static List<string> TargetMimeTypes = DocumentMimeTypes
    .Concat(ImageMimeTypes)
    .ToList();

    public enum ExtentType {
        PerceptualHash,
        SsdeepHash
    };

    private Graph _graph;
    private IUriNode _ricoExtentType,
    _ricoHasExtent,
    _ricoHasExtentType,
    _ricoHasGeneticLink,
    _ricoHasIdentifier,
    _ricoHasInstantiation,
    _ricoIdentifier,
    _ricoInstantiation,
    _ricoRecord,
    _ricoResourceRecordExtent,
    _ricoTextualValue,
    _rdfType;

    private IBlankNode _extentTypeSsdeepNode, _extentTypePerceptualNode;

    private DirectoryInfo _targetDir;
    private List<FileInfo> _targetFiles;

    private PerceptualHash _perceptualHashAlgorithm;

    /// <summary>
    /// Constructor to create all the default nodes.
    /// </summary>
    /// <param name="inputPath"></param>
    public HashingOperations(string inputPath) {
        // Graph of RDF triples.
        _graph = new Graph();

        // Establish some general-use nodes in the _graph.
        // These nodes come from the RiC ontology.
        _ricoExtentType = _graph.CreateUriNode(UriFactory.Create("https://www.ica.org/standards/RiC/ontology#ExtentType"));
        _ricoHasExtent = _graph.CreateUriNode(UriFactory.Create("https://www.ica.org/standards/RiC/ontology#hasExtent"));
        _ricoHasExtentType = _graph.CreateUriNode(UriFactory.Create("https://www.ica.org/standards/RiC/ontology#hasExtentType"));
        _ricoHasGeneticLink = _graph.CreateUriNode(UriFactory.Create("https://www.ica.org/standards/RiC/ontology#hasGeneticLinkToRecordResource"));
        _ricoHasIdentifier = _graph.CreateUriNode(UriFactory.Create("https://www.ica.org/standards/RiC/ontology#hasOrHadIdentifier"));
        _ricoHasInstantiation = _graph.CreateUriNode(UriFactory.Create("https://www.ica.org/standards/RiC/ontology#hasOrHadInstantiation"));
        _ricoIdentifier = _graph.CreateUriNode(UriFactory.Create("https://www.ica.org/standards/RiC/ontology#identifier"));
        _ricoInstantiation = _graph.CreateUriNode(UriFactory.Create("https://www.ica.org/standards/RiC/ontology#Instantiation"));
        _ricoRecord = _graph.CreateUriNode(UriFactory.Create("https://www.ica.org/standards/RiC/ontology#Record"));
        _ricoResourceRecordExtent = _graph.CreateUriNode(UriFactory.Create(" https://www.ica.org/standards/RiC/ontology#RecordResourceExtent"));
        _ricoTextualValue = _graph.CreateUriNode(UriFactory.Create("https://www.ica.org/standards/RiC/ontology#textualValue"));
        // This is a generic one from RDF.
        _rdfType = _graph.CreateUriNode(UriFactory.Create("http://www.w3.org/1999/02/22-rdf-syntax-ns#type"));

        _extentTypeSsdeepNode = _graph.CreateBlankNode();
        _graph.Assert(_extentTypeSsdeepNode, _rdfType, _ricoExtentType);
        _graph.Assert(_extentTypeSsdeepNode, _ricoHasIdentifier, _graph.CreateLiteralNode(ExtentType.SsdeepHash.ToString()));
        _extentTypePerceptualNode = _graph.CreateBlankNode();
        _graph.Assert(_extentTypePerceptualNode, _rdfType, _ricoExtentType);
        _graph.Assert(_extentTypePerceptualNode, _ricoHasIdentifier, _graph.CreateLiteralNode(ExtentType.PerceptualHash.ToString()));

        _perceptualHashAlgorithm = new PerceptualHash();

        // Define the directory to be scanned. All files will be compared with all other files.
        // This will be user-selectable through the UI when the front-end is built.
        _targetDir = new DirectoryInfo(inputPath);

        // Loop through all files in a target directory if they have the desired extension and are larger than 1kb.
        // 1kb is an arbitrary number but it prevents a lot of matches between files that are too small for similarity to be meaningful.
        _targetFiles = GetNonHiddenFiles(_targetDir)
        .Where(f => TargetMimeTypes.Contains(MimeUtility.GetMimeMapping(f.FullName)) && f.Length > 1000)
        .ToList();
    }

    /// <summary>
    /// Loop through all the target files, calculate the binary hashes, and compare them to find similar files.
    /// </summary>
    public void FindBinaryMatches() {
        foreach (var file in _targetFiles) {
            var buffer = File.ReadAllBytes(file.FullName);

            var recordNode = GetOrCreateRecord(file.FullName);
            // Save the file's fuzzy hash as the record's extent.
            var binaryExtentNode = _graph.CreateBlankNode();
            _graph.Assert(binaryExtentNode, _rdfType, _ricoResourceRecordExtent);
            _graph.Assert(recordNode, _ricoHasExtent, binaryExtentNode);
            var binaryHashNode = _graph.CreateLiteralNode(Hasher.HashBuffer(buffer, buffer.Length));
            _graph.Assert(binaryExtentNode, _ricoTextualValue, binaryHashNode);
            _graph.Assert(binaryExtentNode, _ricoHasExtentType, _extentTypeSsdeepNode);
        }

        // Get all the record nodes.
        var records = _graph.GetTriplesWithPredicateObject(_rdfType, _ricoRecord)
        .Select(_ => _.Subject);

        var checkedPairs = new List<List<INode>>();

        // Compare the hashes of every file to every other file.
        foreach (var record1 in records) {
            foreach (var record2 in records) {
                if (record1 != record2  // Don't compare a record with itself.
                && !checkedPairs.Where(_ => _.Contains(record1) && _.Contains(record2)).Any()) {   // Or if we've already compared the two.
                    var similarity = GetBinarySimilarity(record1, record2);
                    
                    if (similarity == 100) {
                        // If the two are identical, delete the second record and attch its instantiation to the first record.
                        var oldTriples = _graph.GetTriplesWithSubjectPredicate(record2, _ricoHasInstantiation);
                        foreach (var oldTriple in oldTriples) {
                            // Attach the second triple's instantation to the first triple's record.
                            _graph.Assert(record2, _ricoHasInstantiation, oldTriple.Object);
                            // I'm not sure we can actually delete a node --- just retract its triples.
                            _graph.Retract(oldTriple);
                        }
                    }
                    else if (similarity > 49) {
                        // If two files are sufficiently similar, create a "genetic link" between the two records.
                        _graph.Assert(record1, _ricoHasGeneticLink, record2);
                    }

                    // Log the fact that we've cheked these two triples.
                    checkedPairs.Add(new List<INode>() { record1, record2 });
                }
            }
        }

        
    }

    /// <summary>
    /// Loop through all the target files, calculate the perceptual hashes, and compare them to find similar images.
    /// </summary>
    public void FindPerceptualMatches() {
        foreach (var file in _targetFiles) {
            // Calculate the perceptual hash for images.
            if (ImageMimeTypes.Contains(MimeUtility.GetMimeMapping(file.FullName))) {
                var recordNode = GetOrCreateRecord(file.FullName);

                // Save the file's perceptual hash as another type of extent.
                var perceptualExtentNode = _graph.CreateBlankNode();
                _graph.Assert(perceptualExtentNode, _rdfType, _ricoResourceRecordExtent);
                _graph.Assert(recordNode, _ricoHasExtent, perceptualExtentNode);
                var image = Image.Load<Rgba32>(file.FullName);
                var perceptualHashNode = _graph.CreateLiteralNode(_perceptualHashAlgorithm.Hash(image).ToString());
                _graph.Assert(perceptualExtentNode, _ricoTextualValue, perceptualHashNode);
                _graph.Assert(perceptualExtentNode, _ricoHasExtentType, _extentTypePerceptualNode);
            }
        }

        // Get all the record nodes.
        var records = _graph.GetTriplesWithPredicateObject(_rdfType, _ricoRecord)
        .Select(_ => _.Subject);

        var checkedPairs = new List<List<INode>>();

        // Compare the hashes of every file to every other file.
        foreach (var record1 in records) {
            foreach (var record2 in records) {
                if (record1 != record2  // Don't compare a record with itself.
                && !checkedPairs.Where(_ => _.Contains(record1) && _.Contains(record2)).Any()) {   // Or if we've already compared the two.
                    var similarity = GetPerceptualSimilarity(record1, record2);
                    
                    if (similarity > 70) {
                        // If two files are sufficiently similar, create a "genetic link" between the two records.
                        _graph.Assert(record1, _ricoHasGeneticLink, record2);
                    }

                    // Log the fact that we've cheked these two triples.
                    checkedPairs.Add(new List<INode>() { record1, record2 });
                }
            }
        }
    }

    /// <summary>
    /// Get the binary similarity between two records.
    /// </summary>
    /// <param name="record1"></param>
    /// <param name="record2"></param>
    /// <returns></returns>
    private int GetBinarySimilarity(INode record1, INode record2) {
        var similarity = 0;

        var hash1 = GetExtent(record1, ExtentType.SsdeepHash);
        var hash2 = GetExtent(record2, ExtentType.SsdeepHash);

        if (hash1 is not null && hash2 is not null) {
            // ssdeep can only compare files with the same block size or block size times two.
            int.TryParse(hash1.Split(":")[0], out var block_size1);
            int.TryParse(hash2.Split(":")[0], out var block_size2);
            if (block_size1 == block_size2 || block_size1 == block_size2 * 2 || block_size2 == block_size1 * 2) {
                // Compare the two hashes and save the result.
                similarity = Comparer.Compare(hash1, hash2);
            }
        }

        return similarity;
    }

    /// <summary>
    /// Get the extent of a specified type for the given record.
    /// </summary>
    /// <param name="record"></param>
    /// <param name="extentType"></param>
    /// <returns></returns>
    private string? GetExtent(INode record, ExtentType extentType) {
        var extentTypeNode = _graph.GetTriplesWithPredicateObject(_rdfType, _ricoExtentType)
        .Where(_ => _graph.GetTriplesWithSubjectPredicate(_.Subject, _ricoHasIdentifier).Select(i => i.Object.AsValuedNode().AsString()).Single() == extentType.ToString())
        .Single()
        .Subject;

        return _graph.GetTriplesWithSubjectPredicate(record, _ricoHasExtent).Select(_ => _.Object) // Select all the record's extents.
        .Where(e => _graph.Triples[(e, _ricoHasExtentType, extentTypeNode)].Any()) // Where the extent is of the requested type.
        .Select(_ => _graph.GetTriplesWithSubjectPredicate(_, _ricoTextualValue).Single()).FirstOrDefault()?.Object.AsValuedNode().AsString(); // Get the hash value.
    }

    /// <summary>
    /// Iterate through a directory recursively to get all files that are not hidden and not in hidden directories.
    /// </summary>
    /// <param name="targetDir"></param>
    /// <returns></returns>
    private static IList<FileInfo> GetNonHiddenFiles(DirectoryInfo targetDir) {
        var file_infos = new List<FileInfo>();
        file_infos.AddRange(targetDir.GetFiles("*.*", SearchOption.TopDirectoryOnly).Where(w => (w.Attributes & FileAttributes.Hidden) == 0));
        foreach (var directory in targetDir.GetDirectories("*.*", SearchOption.TopDirectoryOnly).Where(w => (w.Attributes & FileAttributes.Hidden) == 0)) {
            file_infos.AddRange(GetNonHiddenFiles(directory));
        }

        return file_infos;
    }

    /// <summary>
    /// Calculate the perceptual similarity between two records.
    /// </summary>
    /// <param name="record1"></param>
    /// <param name="record2"></param>
    /// <returns></returns>
    private int GetPerceptualSimilarity(INode record1, INode record2) {
        var similarity = 0;

        var hash1 = GetExtent(record1, ExtentType.PerceptualHash);
        var hash2 = GetExtent(record2, ExtentType.PerceptualHash);

        if (hash1 is not null && hash2 is not null) {
            similarity = (int)CompareHash.Similarity(Convert.ToUInt64(hash1), Convert.ToUInt64(hash2));
        }

        return similarity;
    }

    /// <summary>
    /// Get or create a record node for a given file path.
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    private INode GetOrCreateRecord(string filePath) {
        var recordNode = _graph.GetTriplesWithPredicateObject(_rdfType, _ricoInstantiation)
        .Where(_ => _graph.GetTriplesWithSubjectPredicate(_.Subject, _ricoHasIdentifier).Single().Object.AsValuedNode().AsString() == filePath)
        .Select(_ => _graph.GetTriplesWithPredicateObject(_ricoHasInstantiation, _.Subject).FirstOrDefault())
        .FirstOrDefault()?.Subject;

        if (recordNode is null) {
            // Create a record and instantiation node for this file.
            recordNode = _graph.CreateBlankNode();
            _graph.Assert(recordNode, _rdfType, _ricoRecord);
            var instantiationNode = _graph.CreateBlankNode();
            _graph.Assert(instantiationNode, _ricoHasIdentifier, _graph.CreateLiteralNode(filePath));
            _graph.Assert(instantiationNode, _rdfType, _ricoInstantiation);
            // Connect the record with its instantiation.
            _graph.Assert(recordNode, _ricoHasInstantiation, instantiationNode);
        }

        return recordNode;
    }

    /// <summary>
    /// Save the graph in XML format to the path specified.
    /// </summary>
    /// <param name="outputPath"></param>
    public void SaveGraph(string outputPath) {
        // Save to a file.
        RdfXmlWriter rdfxmlwriter = new RdfXmlWriter();
        rdfxmlwriter.Save(_graph, outputPath);
    }
}