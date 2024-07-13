using Emgu.CV;
using Emgu.CV.CvEnum;
using QuickConvert;
using Serilog;
using System.Drawing;
using System.Drawing.Imaging;

class Program
{
    static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();

        try
        {
            string rootFolder = Directory.GetCurrentDirectory();
            string outputFolder = Path.Combine(rootFolder, "OutputJpg");
            Directory.CreateDirectory(outputFolder);

            ProcessFolder(rootFolder, outputFolder);

            Log.Information("Conversion completed.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred during conversion.");
        }
        finally
        {
            Log.CloseAndFlush();
        }

        Console.ReadLine();
    }

    static void ProcessFolder(string folderPath, string outputRootFolder)
    {
        // Combine both .png and .webp files, case-insensitive
        string[] imageFiles = Directory.GetFiles(folderPath, "*.*")
                                       .Where(file => file.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                                                      file.EndsWith(".webp", StringComparison.OrdinalIgnoreCase))
                                       .ToArray();
        // Initialize the MetadataProcessor
        MetadataProcessor processor = new();

        Parallel.ForEach(imageFiles, imageFile =>
        {
            try
            {
                Log.Information($"Processing image file: {imageFile}");

                using (Mat inputImage = new(imageFile))
                {
                    int targetWidth = 4096; // 4K width
                    double upscaleFactorWidth = (double)targetWidth / inputImage.Width;
                    int targetHeight = (int)(inputImage.Height * upscaleFactorWidth);

                    using (Mat upscaledImage = new())
                    {
                        CvInvoke.Resize(inputImage, upscaledImage, new Size(targetWidth, targetHeight), 0, 0, Inter.Cubic);

                        string relativePath = Path.GetRelativePath(Directory.GetCurrentDirectory(), imageFile);
                        string outputSubfolder = Path.Combine(outputRootFolder, Path.GetDirectoryName(relativePath) ?? throw new InvalidOperationException("Invalid folder path"));
                        Directory.CreateDirectory(outputSubfolder);
                        string outputFilePath = Path.Combine(outputSubfolder, Path.GetFileNameWithoutExtension(imageFile) + ".jpg");

                        var jpegEncoder = ImageCodecInfo.GetImageEncoders().FirstOrDefault(codec => codec.FormatID == ImageFormat.Jpeg.Guid);
                        if (jpegEncoder != null)
                        {
                            var parameters = new EncoderParameters(1);
                            parameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 100L); // Maximum quality
                            upscaledImage.ToBitmap().Save(outputFilePath, jpegEncoder, parameters);

                            // Copy metadata using ExifLib
                            //     CopyMetadata(imageFile, outputFilePath);
                            try
                            {
                                // Process to copy XMP metadata from PNG to JPG
                                MetadataProcessor.CopyXmpMetadata(imageFile, outputFilePath);

                                Console.WriteLine("XMP metadata copied successfully.");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error processing metadata: {ex.Message}");
                            }

                        }
                        else
                        {
                            Log.Warning("JPEG codec not found for image: {ImageFile}", imageFile);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while processing image file: {ImageFile}", imageFile);
            }
        });

        string[] subfolders = Directory.GetDirectories(folderPath);
        Parallel.ForEach(subfolders, subfolder =>
        {
            ProcessFolder(subfolder, outputRootFolder);
        });
    }
}