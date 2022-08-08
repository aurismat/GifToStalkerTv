using System;
using System.IO;
using System.Diagnostics;
using System.Drawing;

namespace GifToStalkerTv
{
    internal class Program
    {
        /// <summary>
        /// Prints usage message and exits the program.
        /// </summary>
        /// <param name="msg">Optional error message to print</param>
        static void help(string msg)
        {
            Printer prnt = new Printer();
            int exitCode = 0;
            if(msg != null)
            {
                prnt.err(msg);
                exitCode = 1;
            }
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Usage:");
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.Write("./" + Process.GetCurrentProcess().ProcessName + ".exe ");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write("<gif_file.gif> ");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("[framerate, 1-60]");
            Console.ForegroundColor = ConsoleColor.White;
            Environment.Exit(exitCode);
        }

        /// <summary>
        /// Check if ffmpeg.exe is present next to the program.
        /// Exits the program if it isn't.
        /// </summary>
        static void testForFFMPEG()
        {
            Printer prnt = new Printer();
            Process p = new Process();
            p.StartInfo = new ProcessStartInfo("powershell", "gcm ffmpeg")
            {
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };
            p.Start();
            p.WaitForExit();

            bool exists = p.ExitCode == 0;
            if (!exists)
            {
                prnt.err("ffmpeg not found!");
                Environment.Exit(1);
            }
        }

        /// <summary>
        /// Check if arg is a file
        /// </summary>
        /// <param name="arg">arg string</param>
        static void testForArg1File(string arg)
        {
            bool exists = File.Exists(arg);
            if (!exists)
                help("Argument 1 is not a valid file!");
        }

        /// <summary>
        /// Check if arg is an integer, and return it if so
        /// </summary>
        /// <param name="arg">arg string</param>
        /// <param name="fps">output int</param>
        static void testForArg2Int(string arg, out int fps)
        {
            if (!int.TryParse(arg, out fps))
            {
                help("Argument 2 must be an integer");
            }
        }

        /// <summary>
        /// Uses ffmpeg to convert a gif file to a png file for each frame.
        /// </summary>
        /// <param name="gifFile">gif file dir</param>
        static void convertGifToPngList(string gifFile)
        {
            Process p = new Process();
            //start a ffmpeg process to extract all gif frames to png and scale them to 256x256 for DDS conversion later
            p.StartInfo = new ProcessStartInfo("ffmpeg", "-i " + gifFile + " -vf scale=256:256 -y -vsync 0 ./_temp/%d.png")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            p.Start();
            p.WaitForExit();
            // if for whatever reason ffmpeg fails and throws a different error code, we can catch it here and print stderr and stdout for debugging
            if (p.ExitCode != 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("ffmpeg failed with exit code " + p.ExitCode);
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("ffmpeg output is as follows:");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine(p.StandardOutput.ReadToEnd());
                Console.WriteLine(p.StandardError.ReadToEnd());
                Console.ForegroundColor = ConsoleColor.White;
                Environment.Exit(1);
            }
        }

        /// <summary>
        /// Converts .png's to a suitable .dds formatted files, suitable for STALKER.
        /// </summary>
        static void convertPngsToDdsList()
        {
            Process p = new Process();
            p.StartInfo = new ProcessStartInfo("magick",
                                               "convert -format dds -define dds:compression=DXT1" +
                                               " _temp\\*.png output\\fx_stalker_%03d.dds")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true
            };
            p.Start();
            p.WaitForExit();
            if (p.ExitCode != 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("convert failed with exit code " + p.ExitCode);
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine("convert output is as follows:");
                Console.WriteLine(p.StandardOutput.ReadToEnd());
                Console.ForegroundColor = ConsoleColor.White;
                Environment.Exit(1);
            }
        }

        /// <summary>
        /// cleans up all folders after/before using the program
        /// </summary>
        /// <param name="includeGamedata">Delete the final output folder(gamedata)?</param>
        static void cleanup(bool includeGamedata = false)
        {
            if (Directory.Exists("gamedata") && includeGamedata)
                Directory.Delete("gamedata", true);
            if (Directory.Exists("_temp"))
                Directory.Delete("_temp", true);
            if (Directory.Exists("output"))
                Directory.Delete("output", true);
        }

        /// <summary>
        /// Generates the directory structure used by the program.
        /// </summary>
        static void generateDirStructure()
        {
            Directory.CreateDirectory("_temp");
            Directory.CreateDirectory("output");
            Directory.CreateDirectory("gamedata/textures/fx");
        }

        static int getFPS(string path)
        {
            Image img = Image.FromFile(path);
            var framePropItem = img.GetPropertyItem(0x5100);
            byte[] delayRawData = framePropItem.Value;
            string delay = string.Format("{0}{1}", delayRawData[0], delayRawData[1]);
            int del = int.Parse(delay);

            return 1000 / del;
        }

        static void Main(string[] args)
        {
            Printer prnt = new Printer();
            int fps = 1;
            if (args.Length < 1 || args.Length > 2)
                help("Incorrect amount of arguments");

            //check if we have ffmpeg and magick
            testForFFMPEG();

            //validate filename arg
            testForArg1File(args[0]);

            //use FPS override if stated in args, otherwise find it yourself
            if (args.Length == 2)
            {
                testForArg2Int(args[1], out fps);
                prnt.info("FPS override arg found");
            }
            else
            {
                fps = getFPS(args[0]);
                prnt.info("FPS override not found, parsing it from .gif");
            }
            prnt.info("Framerate: " + fps);

            //cleanup and generate directory structure required
            prnt.info("Cleaning up from previous usage if needed...");
            cleanup(true);
            prnt.info("Generating directory structure required...");
            generateDirStructure();

            //convert .gif to a .png list in _temp directory
            convertGifToPngList(args[0]);
            prnt.info("Done converting .gif to a set of .png's!");
            prnt.info("Converting them to a .dds set...");

            //convert .png set to .dds set, compatible with STALKER
            convertPngsToDdsList();
            prnt.info("Done creating .dds files!");
            prnt.info("Generating .seq file...");

            //get all .dds filenames without extensions into an array
            string[] ddsArray = Directory.GetFiles("output");
            for (int i = 0; i < ddsArray.Length; i++)
                ddsArray[i] = Path.GetFileNameWithoutExtension(ddsArray[i]);

            //write fps and all .dds filename references to .seq file
            using (StreamWriter writer = new StreamWriter("output/fx_stalker.seq"))
            {
                writer.WriteLine(fps);
                foreach(string file in ddsArray)
                    writer.WriteLine("fx\\" + file);
            }

            prnt.info(".seq file generated!");
            prnt.info("Final moving of all created files...");

            //final copy to the gamedata folder
            foreach (string filePath in Directory.GetFiles("output"))
            {
                string filename = Path.GetFileName(filePath);
                File.Copy(filePath, "gamedata/textures/fx/" + filename, true);
            }

            prnt.success("Done! Put the generated \"gamedata\" folder into your STALKER directory!");

            prnt.info("Cleaning up...");
            cleanup();
        }
    }
}
