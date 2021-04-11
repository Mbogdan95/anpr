using Emgu.CV;
using MadMilkman.Ini;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace ANPR
{
    class Program
    {
        static void Main(string[] args)
        {
            int errorCode = 0;

            TextRecognizer.InitTextRecognizer();

            InitParameters();

            //foreach (var file in Directory.EnumerateFiles(@"D:\Imagini ANPR auto\1", "*.jpg"))
            foreach (var file in Directory.EnumerateFiles(@"D:\Imagini ANPR auto\5", "2*.png"))
            {
                Stopwatch watch = Stopwatch.StartNew(); // time the detection process

                Mat imgOriginal = new Mat(file);
                Mat imgGrayScale = new Mat();
                Mat imgThresh = new Mat();

                List<KeyValuePair<string, bool>> licensePlateNumber = new List<KeyValuePair<string, bool>>();

                ImageProcess.Preprocess(imgOriginal, ref imgGrayScale, ref imgThresh, ref errorCode);

                if (Properties.Settings.Default.debug)
                {
                    CvInvoke.Imshow("Original image", imgOriginal);
                    CvInvoke.Imshow("Threshold", imgThresh);
                }

                List<KeyValuePair<Mat, Mat>> licensePlates = PlateExtraction.DetectPlate(imgGrayScale, imgThresh, ref errorCode);

                foreach (var licensePlate in licensePlates)
                {
                    if (licensePlate.Value != null)
                    {
                        licensePlateNumber.Add(TextRecognizer.RecognizeText(licensePlate.Value));
                    }
                }

                foreach (KeyValuePair<string, bool> pair in licensePlateNumber)
                {
                    if (pair.Value == true)
                    {
                        Console.WriteLine("License plate # " + pair.Key);
                        break;
                    }
                    else
                    {
                        Console.WriteLine("License plate # " + LicensePlateRegex.LicensePlateNumberProcess(pair.Key));
                    }
                }

                watch.Stop(); //stop the timer
                Console.WriteLine(watch.Elapsed);

                CvInvoke.WaitKey();

                CvInvoke.DestroyAllWindows();
            }
            Console.ReadLine();
        }

        private static void InitParameters()
        {
            IniOptions options = new IniOptions
            {
                KeyDuplicate = IniDuplication.Allowed,
                SectionDuplicate = IniDuplication.Allowed
            };

            IniFile ini = new IniFile(options);
            ini.Load("config.ini");

            foreach (IniSection section in ini.Sections)
            {
                foreach (IniKey key in section.Keys)
                {
                    switch (key.Name)
                    {
                        case "debug":
                            Properties.Settings.Default.debug = Convert.ToBoolean(key.Value);
                            break;
                        case "area":
                            Properties.Settings.Default.area = Convert.ToInt32(key.Value);
                            break;
                        case "boxSizeWidthMin":
                            Properties.Settings.Default.boxSizeWidthMin = Convert.ToInt32(key.Value);
                            break;
                        case "boxSizeWidthMax":
                            Properties.Settings.Default.boxSizeWidthMax = Convert.ToInt32(key.Value);
                            break;
                        case "boxSizeHeightMin":
                            Properties.Settings.Default.boxSizeHeightMin = Convert.ToInt32(key.Value);
                            break;
                        case "boxSizeHeightMax":
                            Properties.Settings.Default.boxSizeHeightMax = Convert.ToInt32(key.Value);
                            break;
                        case "boundingRectangleHeightMin":
                            Properties.Settings.Default.boundingRectangleHeightMin = Convert.ToInt32(key.Value);
                            break;
                        case "boundingRectangleHeightMax":
                            Properties.Settings.Default.boundingRectangleHeightMax = Convert.ToInt32(key.Value);
                            break;
                        case "boundingRectangleWidthMax":
                            Properties.Settings.Default.boundingRectangleWidthMax = Convert.ToInt32(key.Value);
                            break;
                    }
                }
            }

            Properties.Settings.Default.Save();
        }
        //static async Task Main(string[] args)
        //{

        //    // Initiate text recognizer
        //    TextRecognizer.InitTextRecognizer();

        //    // Initiate parameters
        //    InitParameters();

        //    // Keep looping listening on the tube
        //    while (true)
        //    {
        //        // Initate variables
        //        JObject vehicleData = new JObject();
        //        JObject trailerData = new JObject();

        //        // Read data from tube
        //        JObject dataRead = await BeanstalkdCommunication.ReadTubeAsync();

        //        // Loop through items in data from tube
        //        foreach (var item in dataRead)
        //        {
        //            // Check if item key is diffrent from "command"
        //            if (item.Key != "command")
        //            {
        //                // Initate variables
        //                string originalImagePath = item.Value.ToString();
        //                //string originalImagePath = $"/home/philro/Apps/cantar-auto/web/uploads/anpr/preproc/{item.Value.ToString()}";
        //                string plateNumber = "";
        //                string plateImagePath = "";

        //                int errorCode = 0;

        //                long processingTime = 0;

        //                int epochTime = (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;

        //                Stopwatch watch = Stopwatch.StartNew();

        //                if (File.Exists(originalImagePath))
        //                {
        //                    Mat imgOriginal = new Mat(originalImagePath);
        //                    Mat imgGrayScale = new Mat();
        //                    Mat imgThresh = new Mat();

        //                    List<KeyValuePair<string, bool>> licensePlateNumber = new List<KeyValuePair<string, bool>>();

        //                    List<Mat> croppedLicensePlateImages = new List<Mat>();

        //                    ImageProcess.Preprocess(imgOriginal, ref imgGrayScale, ref imgThresh, ref errorCode);

        //                    if (Properties.Settings.Default.debug)
        //                    {
        //                        CvInvoke.Imshow("Original image", imgOriginal);
        //                        CvInvoke.Imshow("Threshold", imgThresh);
        //                    }

        //                    List<KeyValuePair<Mat, Mat>> licensePlates = PlateExtraction.DetectPlate(imgGrayScale, imgThresh, ref errorCode);

        //                    foreach (var licensePlate in licensePlates)
        //                    {
        //                        if (licensePlate.Value != null)
        //                        {
        //                            licensePlateNumber.Add(TextRecognizer.RecognizeText(licensePlate.Value));
        //                            croppedLicensePlateImages.Add(licensePlate.Key);
        //                        }
        //                    }

        //                    int count = 0;
        //                    foreach (KeyValuePair<string, bool> pair in licensePlateNumber)
        //                    {
        //                        if (pair.Value == true)
        //                        {
        //                            Console.WriteLine("License plate # " + pair.Key);
        //                            plateNumber = pair.Key;

        //                            if (croppedLicensePlateImages[count] != null)
        //                            {
        //                                croppedLicensePlateImages[count].Save($"/home/manta/Desktop/Imagini/proc/plate_no_{epochTime}.jpg");
        //                                //croppedLicensePlateImages[count].Save($"/home/philro/Apps/cantar-auto/web/uploads/anpr/proc/{item.Key}_{epochTime}.jpg");
        //                            }

        //                            break;
        //                        }
        //                        else
        //                        {
        //                            plateNumber = LicensePlateRegex.LicensePlateNumberProcess(pair.Key);

        //                            Console.WriteLine("License plate # " + plateNumber);

        //                            if (croppedLicensePlateImages[count] != null)
        //                            {
        //                                croppedLicensePlateImages[count].Save($"/home/manta/Desktop/Imagini/proc/plate_no_{epochTime}.jpg");
        //                                //croppedLicensePlateImages[count].Save($"/home/philro/Apps/cantar-auto/web/uploads/anpr/proc/{item.Key}_{epochTime}.jpg");
        //                            }

        //                            if (LicensePlateRegex.MatchRegex(plateNumber))
        //                            {
        //                                break;
        //                            }


        //                        }
        //                        count++;
        //                    }

        //                }
        //                else
        //                {
        //                    errorCode = 1;
        //                }

        //                watch.Stop();
        //                processingTime = watch.ElapsedMilliseconds;

        //                if (plateNumber != "" && errorCode == 0)
        //                {
        //                    errorCode = 0;
        //                    plateImagePath = $"/home/philro/Apps/cantar-auto/web/uploads/anpr/proc/{item.Key}_{epochTime}.jpg";

        //                }
        //                else if (errorCode == 0)
        //                {
        //                    errorCode = 2;
        //                }

        //                if (item.Key == "vehicle")
        //                {
        //                    vehicleData = new JObject()
        //                    {
        //                        { "img_path", originalImagePath},
        //                        { "plate_no", plateNumber},
        //                        { "plate_img_path", plateImagePath},
        //                        { "proc_time", processingTime},
        //                        { "error", errorCode}
        //                    };
        //                }
        //                else if (item.Key == "trailer")
        //                {
        //                    trailerData = new JObject()
        //                    {
        //                        { "img_path", originalImagePath},
        //                        { "plate_no", plateNumber},
        //                        { "plate_img_path", plateImagePath},
        //                        { "proc_time", processingTime},
        //                        { "error", errorCode}
        //                    };
        //                }
        //            }
        //        }

        //        JObject data = new JObject() {
        //            { "command","processed"},
        //            { "vehicle", vehicleData},
        //            { "trailer", trailerData}
        //        };

        //        BeanstalkdCommunication.WriteTubeAsync(data);

        //        if (Properties.Settings.Default.debug)
        //        {
        //            CvInvoke.WaitKey();
        //        }
        //    }
        //}
    }
}
