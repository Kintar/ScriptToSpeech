using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Speech.AudioFormat;
using System.Speech.Synthesis;
using System.Text;
using System.Threading.Tasks;
using NAudio;
using NAudio.Wave;
using NAudio.Lame;

namespace TextToSpeech
{
    class Program
    {
        static int Main(string[] args)
        {

            if (args.Length < 3)
            {
                Console.WriteLine("Usage:  TextToSpeech <input text file> <output wav file> <character name>");
                return -1;
            }

            var infile = new FileInfo(args[0]);
            var outfile = new FileInfo(args[1]);

            if (!infile.Exists)
            {
                Console.WriteLine("Could not find input file '{0}'", infile.FullName);
                return -1;
            }

            if (outfile.Exists)
            {
                try
                {
                    outfile.Delete();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Could not overwrite existing output file '{0}'", outfile.FullName);
                    Console.WriteLine("Error: {0}", e.Message);
                    return -1;
                }
            }

            if (!outfile.FullName.EndsWith(".wav"))
            {
                outfile = new FileInfo(outfile.FullName + ".wav");
            }

            return new Program().Run(infile, outfile, args[2].ToLower());
        }

        private int Run(FileInfo infile, FileInfo outfile, string charName) { 

            var pb = CreatePromptBuilder(infile, charName);
            
            using (var synth = new SpeechSynthesizer())
            {
                synth.SelectVoice("Microsoft David Desktop");
                synth.SetOutputToWaveFile(outfile.FullName);
                synth.Speak(pb);
            }

            EncodeMp3(outfile);
            outfile.Delete();

            return 0;
        }

        private void EncodeMp3(FileInfo outfile)
        {
            var outMp3 = outfile.FullName.Replace(".wav", ".mp3");
            using (var fr = new AudioFileReader(outfile.FullName))
            using (var writer = new LameMP3FileWriter(outMp3, fr.WaveFormat, LAMEPreset.STANDARD))
            {
                fr.CopyTo(writer);
            }
        }

        private PromptBuilder CreatePromptBuilder(FileInfo infile, string charName)
        {
            var pb = new PromptBuilder();

            var styleCharacterIntro = new PromptStyle(PromptEmphasis.Moderate);
            var styleNotMyCharacter = new PromptStyle(PromptVolume.Default);

            using (var fs = infile.OpenRead())
            using (var reader = new StreamReader(fs))
            {
                string line;
                var myChar = false;

                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim().ToLower();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    if (line.StartsWith("#"))
                    {
                        pb.StartStyle(styleCharacterIntro);
                        line = line.Replace("#", "");
                        pb.AppendBreak();
                        pb.AppendText(line);
                        pb.AppendBreak();
                        pb.EndStyle();
                        myChar = line.Equals(charName);
                    }
                    else
                    {
                        if (!myChar)
                        {
                            pb.StartStyle(styleNotMyCharacter);
                        }
                        else
                        {
                            pb.AppendBreak();
                            pb.AppendBreak();
                        }

                        pb.AppendText(line);

                        if (!myChar)
                        {
                            pb.EndStyle();
                        }
                    }
                }

                return pb;
            }
        }
    }
}
