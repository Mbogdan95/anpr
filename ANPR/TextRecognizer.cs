using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.ML;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Xml.Serialization;

namespace ANPR
{
    class TextRecognizer
    {
        static Matrix<float> mtxClassifications = new Matrix<float>(1, 1);       // for the first time through, declare these to be 1 row by 1 column
        static Matrix<float> mtxTrainingImages = new Matrix<float>(1, 1);        // we will resize these when we know the number of rows (i.e. number of training samples)

        //static List<int> intValidChars = new List<int>(new int[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z' });

        const int MIN_CONTOUR_AREA = 200;
        const int RESIZED_IMAGE_WIDTH = 20;
        const int RESIZED_IMAGE_HEIGHT = 30;

        static KNearest kNearest = new KNearest();

        static int i = 0;

        public static void InitTextRecognizer()
        {
            XmlSerializer xmlSerializer = new XmlSerializer(mtxClassifications.GetType());          // these variables are for
            StreamReader streamReader;                                                            // reading from the XML files

            try
            {
                streamReader = new StreamReader("classifications.xml");                          // attempt to open classifications file
            }
            catch (Exception ex)
            {
                throw ex;
            }
            // read from the classifications file the 1st time, this is only to get the number of rows, not the actual data
            mtxClassifications = (Matrix<float>)xmlSerializer.Deserialize(streamReader);

            streamReader.Close();            // close the classifications XML file

            int intNumberOfTrainingSamples = mtxClassifications.Rows;         // get the number of rows, i.e. the number of training samples

            // now that we know the number of rows, reinstantiate classifications Matrix and training images Matrix with the actual number of rows
            mtxClassifications = new Matrix<float>(intNumberOfTrainingSamples, 1);
            mtxTrainingImages = new Matrix<float>(intNumberOfTrainingSamples, RESIZED_IMAGE_WIDTH * RESIZED_IMAGE_HEIGHT);

            try
            {
                streamReader = new StreamReader("classifications.xml");                      // reinitialize the stream reader, attempt to open classifications file again
            }
            catch (Exception ex)
            {
                throw ex;
            }
            // read from the classifications file again, this time we can get the actual data
            mtxClassifications = (Matrix<float>)xmlSerializer.Deserialize(streamReader);

            streamReader.Close();                // close the classifications XML file

            xmlSerializer = new XmlSerializer(mtxTrainingImages.GetType());                // reinstantiate file reading variable

            try
            {
                streamReader = new StreamReader("images.xml");                               // attempt to open classifications file
            }
            catch (Exception ex)
            {
                throw ex;
            }

            mtxTrainingImages = (Matrix<float>)xmlSerializer.Deserialize(streamReader);       // read from training images file
            streamReader.Close();                                                                        // close the training images XML file

            // train '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''


            kNearest.DefaultK = 1;
            kNearest.AlgorithmType = KNearest.Types.BruteForce;

            kNearest.Train(mtxTrainingImages, Emgu.CV.ML.MlEnum.DataLayoutType.RowSample, mtxClassifications);
        }

        public static KeyValuePair<string, bool> RecognizeText(Mat imgInput)
        {
            VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();

            List<ContoursWithData> listOfContoursWithData = new List<ContoursWithData>();          // declare a list of contours with data

            CvInvoke.FindContours(imgInput, contours, null, RetrType.External, ChainApproxMethod.ChainApproxSimple);

            //imgInput.Save(@"D:\Visual Studio Projects\KNNtrain\Imagini\4\" + i + ".jpg");
            //i++;

            // populate list of contours with data
            for (int i = 0; i <= contours.Size - 1; i++)                   // for each contour
            {
                if ((CvInvoke.ContourArea(contours[i]) > MIN_CONTOUR_AREA))
                {
                    ContoursWithData contourWithData = new ContoursWithData();                              // declare new contour with data
                    contourWithData.contour = contours[i];                                                   // populate contour member variable
                    contourWithData.boundingRectangle = CvInvoke.BoundingRectangle(contourWithData.contour);      // calculate bounding rectangle
                    contourWithData.dblArea = CvInvoke.ContourArea(contourWithData.contour);                 // calculate area

                    if (contourWithData.checkIfContourIsValid())
                    {
                        listOfContoursWithData.Add(contourWithData);// add to list of contours with data
                    }
                    else
                    {
                        if (contourWithData.boundingRectangle.Width > Properties.Settings.Default.boundingRectangleWidthMax)
                        {
                            Mat imgROItoBeCloned = new Mat(imgInput, contourWithData.boundingRectangle);

                            Mat imgROI = imgROItoBeCloned.Clone();

                            Rectangle rectangleFirst = new Rectangle(0, 0, contourWithData.boundingRectangle.Width / 2, contourWithData.boundingRectangle.Height);
                            Rectangle rectangleSecond = new Rectangle(contourWithData.boundingRectangle.Width / 2, 0, contourWithData.boundingRectangle.Width / 2, contourWithData.boundingRectangle.Height);

                            Mat firstImage = new Mat(imgROI, rectangleFirst);
                            Mat secondImage = new Mat(imgROI, rectangleSecond);

                            VectorOfVectorOfPoint contoursSplitImage = new VectorOfVectorOfPoint();

                            CvInvoke.FindContours(firstImage, contoursSplitImage, null, RetrType.External, ChainApproxMethod.ChainApproxSimple);

                            for (int j = 0; j <= contoursSplitImage.Size - 1; j++)                   // for each contour
                            {
                                if ((CvInvoke.ContourArea(contoursSplitImage[j]) > MIN_CONTOUR_AREA))
                                {
                                    ContoursWithData contourWithDataFirstImage = new ContoursWithData();                              // declare new contour with data
                                    contourWithDataFirstImage.contour = contoursSplitImage[j];                                                   // populate contour member variable
                                    contourWithDataFirstImage.boundingRectangle = CvInvoke.BoundingRectangle(contourWithDataFirstImage.contour);      // calculate bounding rectangle
                                    contourWithDataFirstImage.boundingRectangle.X = contourWithData.boundingRectangle.X;
                                    contourWithDataFirstImage.boundingRectangle.Y = contourWithData.boundingRectangle.Y;
                                    contourWithDataFirstImage.dblArea = CvInvoke.ContourArea(contourWithDataFirstImage.contour);                 // calculate area

                                    if (contourWithDataFirstImage.checkIfContourIsValid())
                                    {
                                        listOfContoursWithData.Add(contourWithDataFirstImage);// add to list of contours with data
                                    }
                                }
                            }

                            contoursSplitImage = new VectorOfVectorOfPoint();

                            CvInvoke.FindContours(secondImage, contoursSplitImage, null, RetrType.External, ChainApproxMethod.ChainApproxSimple);

                            for (int j = 0; j <= contoursSplitImage.Size - 1; j++)                   // for each contour
                            {
                                if ((CvInvoke.ContourArea(contoursSplitImage[j]) > MIN_CONTOUR_AREA))
                                {
                                    ContoursWithData contourWithDataSecondImage = new ContoursWithData();                              // declare new contour with data
                                    contourWithDataSecondImage.contour = contoursSplitImage[j];                                                   // populate contour member variable
                                    contourWithDataSecondImage.boundingRectangle = CvInvoke.BoundingRectangle(contourWithDataSecondImage.contour);      // calculate bounding rectangle
                                    contourWithDataSecondImage.boundingRectangle.X = contourWithData.boundingRectangle.X + contourWithData.boundingRectangle.Width / 2;
                                    contourWithDataSecondImage.boundingRectangle.Y = contourWithData.boundingRectangle.Y;
                                    contourWithDataSecondImage.dblArea = CvInvoke.ContourArea(contourWithDataSecondImage.contour);                 // calculate area

                                    if (contourWithDataSecondImage.checkIfContourIsValid())
                                    {
                                        listOfContoursWithData.Add(contourWithDataSecondImage);// add to list of contours with data
                                    }
                                }
                            }
                        }
                    }
                }
            }

            float averageLocationY = 0;
            float sumLocationY = 0;

            foreach (var item in listOfContoursWithData)
            {
                sumLocationY += item.boundingRectangle.Y + item.boundingRectangle.Height / 2;
            }

            averageLocationY = sumLocationY / listOfContoursWithData.Count;

            float minLocationY = averageLocationY - 0.15f * averageLocationY;
            float maxLocationY = averageLocationY + 0.15f * averageLocationY;

            listOfContoursWithData.RemoveAll(x => minLocationY > x.boundingRectangle.Y + x.boundingRectangle.Height / 2 || maxLocationY < x.boundingRectangle.Y + x.boundingRectangle.Height / 2);

            // sort contours with data from left to right
            listOfContoursWithData.Sort((oneContourWithData, otherContourWithData) => oneContourWithData.boundingRectangle.X.CompareTo(otherContourWithData.boundingRectangle.X));

            string strFinalString = "";           // declare final string, this will have the final number sequence by the end of the program

            foreach (ContoursWithData contourWithData in listOfContoursWithData)               // for each contour in list of valid contours
            {
                CvInvoke.Rectangle(imgInput, contourWithData.boundingRectangle, new MCvScalar(200, 0.0, 0.0), 2);      // draw green rect around the current char

                Mat imgROItoBeCloned = new Mat(imgInput, contourWithData.boundingRectangle);        // get ROI image of bounding rect

                Mat imgROI = imgROItoBeCloned.Clone();                // clone ROI image so we don't change original when we resize

                Mat imgROIResized = new Mat();

                // resize image, this is necessary for char recognition
                CvInvoke.Resize(imgROI, imgROIResized, new System.Drawing.Size(RESIZED_IMAGE_WIDTH, RESIZED_IMAGE_HEIGHT));

                // declare a Matrix of the same dimensions as the Image we are adding to the data structure of training images
                Matrix<float> mtxTemp = new Matrix<float>(imgROIResized.Size);

                // declare a flattened (only 1 row) matrix of the same total size
                Matrix<float> mtxTempReshaped = new Matrix<float>(1, RESIZED_IMAGE_WIDTH * RESIZED_IMAGE_HEIGHT);

                imgROIResized.ConvertTo(mtxTemp, DepthType.Cv32F);           // convert Image to a Matrix of Singles with the same dimensions

                for (int intRow = 0; intRow <= RESIZED_IMAGE_HEIGHT - 1; intRow++)       // flatten Matrix into one row by RESIZED_IMAGE_WIDTH * RESIZED_IMAGE_HEIGHT number of columns
                {
                    for (int intCol = 0; intCol <= RESIZED_IMAGE_WIDTH - 1; intCol++)
                    {
                        mtxTempReshaped[0, (intRow * RESIZED_IMAGE_WIDTH) + intCol] = mtxTemp[intRow, intCol];
                    }
                }

                float sngCurrentChar;

                sngCurrentChar = kNearest.Predict(mtxTempReshaped);              // finally we can call Predict !!!

                strFinalString = strFinalString + (char)(Convert.ToInt32(sngCurrentChar));          // append current char to full string of chars
            }

            bool licensePlateRegex = LicensePlateRegex.MatchRegex(strFinalString);

            return new KeyValuePair<string, bool>(strFinalString, licensePlateRegex);
        }
    }
}
