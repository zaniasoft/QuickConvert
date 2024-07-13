using System.Diagnostics;

namespace QuickConvert
{
    public class MetadataProcessor
    {
        public static void CopyXmpMetadata(string inputPngFile, string outputJpgFile)
        {
            // Generate a unique temporary file name for XMP metadata
            string tempXmpFile = Path.GetTempFileName();
            try
            {
                // Command to read XMP metadata from PNG
                string readCommand = $"exiftool -xmp -b \"{inputPngFile}\" > \"{tempXmpFile}\"";

                // Execute the read command
                ExecuteCommand(readCommand);

                // Command to write XMP metadata to JPG without creating a backup
                string writeCommand = $"exiftool -overwrite_original -tagsfromfile \"{tempXmpFile}\" -all:all \"{outputJpgFile}\"";

                // Execute the write command
                ExecuteCommand(writeCommand);
            }
            finally
            {
                // Delete the temporary XMP metadata file after processing
                if (File.Exists(tempXmpFile))
                {
                    File.Delete(tempXmpFile);
                }
            }
        }

        private static void ExecuteCommand(string command)
        {
            ProcessStartInfo psi = new("cmd.exe", $"/c {command}")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using Process process = new();
            process.StartInfo = psi;
            process.Start();

            process.WaitForExit();

            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            if (!string.IsNullOrEmpty(error))
            {
                Console.WriteLine($"Error executing command: {error}");
            }
        }
    }
}
