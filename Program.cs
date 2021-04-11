using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FFMediaToolkit.Decoding;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using System.IO;
using FFMediaToolkit;
using System.Threading;

namespace ExtractVideoFrames
{
  /*
   * 2 functions:
   * extract [path to folder with video] [video name with extension] 
   *   output will be in a folder called temp located in the same folder as video file
   *   
   * scale [path to the folder with the temp folder] pixelRate
   *   scales each frame with pixelRate (i.e 1920x1080 frame with pixaleRate 2 scales to 960x540)
   *   
   *   FFMediaToolkit library required
   */
  class Program
  {
    static void Main(string[] args)
    {
      if(args.Length < 3)
      {
        Console.WriteLine("Wrong number of arguments");
        return;
      }

      string framesDirectory = args[1] + @"\temp\";
      switch (args[0])
      {
        case "extract":
          //args[1] directory with video file, args[2] file name
          FFmpegLoader.FFmpegPath = @"C:\Users\mkasz\source\repos\ExtractVideoFrames\bin\ffmpeg\bin";
          var frames = extractFrames(parseMediaFile($@"{args[1]}\{args[2]}").Video);
          saveToFile(frames, framesDirectory + "frame");
          break;
        case "scale":
          //args[1] directory to frames
          scaleFrames(int.Parse(args[2]), framesDirectory);
          break;
        default:
          Console.WriteLine("Unrecognized command");
          break;
      }
      
    }

    private static void scaleFrames(int pixelRate, string path)
    {
      string[] dirs = Directory.GetFiles(path);
      string[] names = new string[dirs.Length];
      Directory.CreateDirectory(path + $@"scaled");

      if(dirs.Length < 10)
      {
        createNewFrames(pixelRate, 0, dirs.Length, path, names, dirs);
      } else
      {
        Thread[] ts = new Thread[8];
        for(int i = 0; i < 8; i++)
        {
          int firstIndex = i * dirs.Length / 8;
          int lastIndex = i == 7 ? dirs.Length : firstIndex + dirs.Length / 8;
          ts[i] = new Thread(() => createNewFrames(pixelRate, firstIndex, lastIndex, path, names, dirs));
          ts[i].Start();
        }

        for(int i = 0; i < 8; i++)
        {
          ts[i].Join();
        }
      }

      Console.WriteLine("Finished sclaing");
    }

    private static void createNewFrames(int pixelRate, int firstIndex, int lastIndex, string path, String[] names, String[] dirs)
    {
      for (int i = firstIndex; i < lastIndex; i++)
      {
        names[i] = dirs[i].Substring(path.Length);
        string[] frameLines = File.ReadAllLines(dirs[i]);
        var frame = frameLines.Select(line => line.Split(' ').Select(int.Parse).ToList()).ToList();
        var scaledFrame = new int[frame.Count / pixelRate, frame[0].Count / pixelRate];
        for (int j = 0; j < frame.Count - frame.Count % pixelRate; j += pixelRate)
        {
          for (int k = 0; k < frame[j].Count - frame[j].Count % pixelRate; k += pixelRate)
          {
            int pixelValue = 0;

            for (int l = 0; l < pixelRate; l++)
            {
              for (int m = 0; m < pixelRate; m++)
              {
                pixelValue += frame[j + l][k + m];
              }
            }

            pixelValue /= pixelRate * pixelRate;
            scaledFrame[j / pixelRate, k / pixelRate] = pixelValue;
          }
        }

        File.WriteAllText(path + $@"scaled\{names[i]}", pixelsToString(scaledFrame));
      }
    }

    private static MediaFile parseMediaFile(String path)
    {
      var file = MediaFile.Open(path);
      if (file == null)
        throw new ArgumentException("file not found");

      return file;
    }

    private static List<int[,]> extractFrames(VideoStream video)
    {
      var frames = new List<int[,]>();

      while (video.TryGetNextFrame(out var image))
      {
        var bitmap = Image.LoadPixelData<Bgr24>(image.Data, image.ImageSize.Width, image.ImageSize.Height);

        int[,] grayImg = new int[bitmap.Height, bitmap.Width];
        for(int i = 0; i < bitmap.Height; i++)
        {
          var imgRow = bitmap.GetPixelRowSpan(i).ToArray();
          for(int j = 0; j < bitmap.Width; j++)
          {
            grayImg[i, j] = (imgRow[j].B + imgRow[j].G + imgRow[j].R) / 3;
          }
        }

        frames.Add(grayImg);
        if(frames.Count % 100 == 0)
          Console.WriteLine($"{frames.Count} frames loaded.");
      }

      return frames;
    }

    private static void saveToFile(List<int[,]> frames, String path)
    {
      int i = 0;

      foreach(var frame in frames){
        i++;
        File.WriteAllText(path + $"{i}.txt", pixelsToString(frame));
        if(i % 100 == 0)
          Console.WriteLine($"{i} frames out of {frames.Count} saved");
      }
    }

    private static String pixelsToString(int[,] pixels)
    {
      var strBuilder = new StringBuilder();
      for(int i = 0; i < pixels.GetLength(0); i++)
      {
        for(int j = 0; j < pixels.GetLength(1); j++)
        {
          strBuilder.Append(pixels[i, j] + " ");
        }
        strBuilder.Remove(strBuilder.Length - 1, 1);
        strBuilder.Append("\n");
      }

      strBuilder.Remove(strBuilder.Length - 1, 1);
      return strBuilder.ToString();
    }
  }
}
