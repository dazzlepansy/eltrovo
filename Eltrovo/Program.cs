public class Eltrovo {
    public static void Main(string[] args) {
        var fileset = new HashingOperations(args[0]);
            fileset.FindBinaryMatches();
            fileset.FindPerceptualMatches();
            fileset.SaveGraph(args[1]);
    }
}