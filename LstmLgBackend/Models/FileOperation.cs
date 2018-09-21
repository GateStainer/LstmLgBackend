using System;
using System.IO;

namespace LstmLgBackend.Models
{
    public class FileOperation
    {
        public static int maxMinute = 60;
        public static void DeleteTrainFiles(string[] path)
        {
            try
            {
                foreach (string filePath in path)
                {
                    if (filePath.Contains(".txt") || filePath.Contains(".train") || filePath.Contains(".test"))
                    {
                        //More than allowedTime
                        DateTime createTime = File.GetCreationTime(filePath);
                        int existMinute = (DateTime.UtcNow - createTime).Minutes;
                        if (existMinute > maxMinute)
                        {
                            File.Delete(filePath);
                        }
                    }
                }
            }
            catch
            {
                //do Nothing
            }
        }

        public static void DeleteModelFiles(string[] path)
        {
            try
            {
                foreach (string filePath in path)
                {
                    DateTime createTime = File.GetCreationTime(filePath);
                    int existMinute = (DateTime.UtcNow - createTime).Minutes;
                    if (existMinute > maxMinute)
                    {
                        File.Delete(filePath);
                    }
                }
            }
            catch
            {
                //do Nothing
            }
        }

        public static void ExecutePython(string workDir, int number)
        {
            System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo();
            psi.FileName = "python";
            psi.WorkingDirectory = workDir;
            psi.Arguments = "word_rnn.py " + number.ToString();
            var process = System.Diagnostics.Process.Start(psi);
            process.WaitForExit();
            if(process.ExitCode != 0)
            {
                throw new Exception("Training failed");
            }
        }
    }
}