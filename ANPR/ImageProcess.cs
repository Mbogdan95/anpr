using Emgu.CV;                     // 
using Emgu.CV.CvEnum;              // Emgu Cv imports
using Emgu.CV.Structure;           // 
using System;
using System.Drawing;

namespace ANPR
{
    class ImageProcess
    {
        /// <summary>
        /// Preprocess original image
        /// </summary>
        /// <param name="imgOriginal">Original image</param>
        /// <param name="imgGrayscale">Imaginea in format grayscale</param>
        /// <param name="errorCode">Error code</param>
        public static void Preprocess(Mat imgOriginal, ref Mat imgGrayscale, ref Mat imgThresh, ref int errorCode)
        {
            try
            {
                // Initiation of images that will be used in this method
                Mat imgBlurred = new Mat();
                Mat imgBilateralFilter = new Mat();
                Mat imgTophat = new Mat();
                Mat imgMaxContrast = new Mat();

                // Structuring element used for morphological operation
                Mat structuringElement = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(40, 20), new Point(-1, -1));

                // Extract value channel only from original image to get imgGrayscale
                CvInvoke.CvtColor(imgOriginal, imgGrayscale, ColorConversion.Bgr2Gray);

                //imgMaxContrast = MaximizeContrast(imgGrayscale, ref errorCode);

                // Gaussian blur
                CvInvoke.GaussianBlur(imgGrayscale, imgBlurred, new Size(3, 3), 3);

                // Bilateral filter
                CvInvoke.BilateralFilter(imgBlurred, imgBilateralFilter, 10, 15, 15);

                // Morphological operation of tophap
                CvInvoke.MorphologyEx(imgBilateralFilter, imgTophat, MorphOp.Tophat, structuringElement, new Point(-1, -1), 1, BorderType.Default, new MCvScalar());

                // Adaptive treshold from the tophat image
                CvInvoke.AdaptiveThreshold(imgTophat, imgThresh, 250, AdaptiveThresholdType.GaussianC, ThresholdType.Binary, 25, -5);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                errorCode = 4;
            }

        }

        /// <summary>
        /// Maximizing image contrast
        /// </summary>
        /// <param name="imgGrayscale">Grayscale input image</param>
        /// <param name="errorCode">Error code</param>
        /// <returns>Grayscale image with maximum contrast</returns>
        public static Mat MaximizeContrast(Mat imgGrayscale, ref int errorCode)
        {
            try
            {
                // Initiation of images that will be used in this method
                Mat imgTopHat = new Mat();
                Mat imgBlackHat = new Mat();
                Mat imgGrayscalePlusTopHat = new Mat();
                Mat imgGrayscalePlusTopHatMinusBlackHat = new Mat();

                // Structuring element used for morphological operation
                Mat structuringElement = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(9, 7), new Point(-1, -1));

                // Morphological operation of tophap
                CvInvoke.MorphologyEx(imgGrayscale, imgTopHat, MorphOp.Tophat, structuringElement, new Point(-1, -1), 1, BorderType.Default, new MCvScalar());

                // Morphological operation of blackhat
                CvInvoke.MorphologyEx(imgGrayscale, imgBlackHat, MorphOp.Blackhat, structuringElement, new Point(-1, -1), 1, BorderType.Default, new MCvScalar());

                // Add grayscale image with tophat image
                CvInvoke.Add(imgGrayscale, imgTopHat, imgGrayscalePlusTopHat);

                // Substract grayscale plus tophat image from blackhat image
                CvInvoke.Subtract(imgGrayscalePlusTopHat, imgBlackHat, imgGrayscalePlusTopHatMinusBlackHat);

                // Return input image with maximum contrast
                return imgGrayscalePlusTopHatMinusBlackHat;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                errorCode = 5;
                return null;
            }
        }
    }
}
