using SSDEEP.NET;

public class Eltrovo {
    public static void Main(string[] args) {
        // This dictionary stores the fuzzy hash of each file. The key is the file path and the value is the hash.
        var hashes = new Dictionary<string, string>();
        // This dictionary stores the similarity rating between two files. The key is a tuple of both file paths, and the value is the similarity from 0 to 100.
        var comparisons = new Dictionary<(string, string), int>();

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
        var target_dir = new DirectoryInfo("/home/user");

        // Loop through all files in a target directory if they have the desired extension and are larger than 1kb.
        // 1kb is an arbitrary number but it prevents a lot of matches between files that are too small for similarity to be meaningful.
        foreach (var file in GetNonHiddenFiles(target_dir)
        .Where(f => extensions.Contains(f.FullName.Split('.')[^1].ToLower()) && f.Length > 1000)){
            var buffer = File.ReadAllBytes(file.FullName);
            // Save all file hashes to a dictionary.
            hashes[file.FullName] = Hasher.HashBuffer(buffer, buffer.Length);
        }

        // Compare the hash of every file to every other file.
        foreach (var file1 in hashes.Keys) {
            foreach (var file2 in hashes.Keys) {
                // Build a tuple out of the two file paths in alphabetical order.
                // This prevents doing the same comparison twice, once for (file1, file2) and once for (file2, file1).
                (string, string) comparison_key;
                if (string.Compare(file1, file2) < 0) {
                    comparison_key = (file1, file2);
                }
                else {
                    comparison_key = (file2, file1);
                }

                if (file1 != file2 && !comparisons.ContainsKey(comparison_key)) {
                    // ssdeep can only compare files with the same block size or block size times two.
                    int.TryParse(hashes[file1].Split(":")[0], out var block_size1);
                    int.TryParse(hashes[file2].Split(":")[0], out var block_size2);
                    if (block_size1 == block_size2 || block_size1 == block_size2 * 2 || block_size2 == block_size1 * 2) {
                        // Compare the two hashes and save the result.
                        comparisons[comparison_key] = Comparer.Compare(hashes[file1], hashes[file2]);
                    }
                }
            }
        }

        // Print out all matches and their similarity rating out of 100.
        foreach (var key in comparisons.Keys) {
            // Only print out the matches with a rating of 50 or more. This seems to filter out a lot of false positives.
            if (comparisons[key] > 49) {
                Console.WriteLine(key + ": " + comparisons[key]);
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