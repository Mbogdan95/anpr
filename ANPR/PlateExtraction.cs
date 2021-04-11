using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace ANPR
{
    class PlateExtraction
    {
        public static List<KeyValuePair<Mat, Mat>> DetectPlate(Mat imgGrayScale, Mat imgThresh, ref int errorCode)
        {
            // Get contours from thresholded image
            VectorOfVectorOfPoint contours = GetImageContours(imgThresh, ref errorCode);

            // Find potential plates in the contours
            List<Mat> potentialPlates = FindPotentialPlatesInContours(imgGrayScale, contours, ref errorCode);

            // First pass looping through potential plates
            KeyValuePair<Mat, Mat> licensePlateFirstPass = FindPlateInPotentialPlates(potentialPlates, "firstPass", ref errorCode);

            // Second pass looping through potential plates
            KeyValuePair<Mat, Mat> licensePlateSecondPass = FindPlateInPotentialPlates(potentialPlates, "secondPass", ref errorCode);

            // Add to a list of key value pairs plates found 
            List<KeyValuePair<Mat, Mat>> licensePlates = new List<KeyValuePair<Mat, Mat>>
            {
                licensePlateFirstPass,
                licensePlateSecondPass
            };

            // Return the list of license plates
            return licensePlates;
        }

        /// <summary>
        /// Get contours from tresholded original image
        /// </summary>
        /// <param name="imgThresh">Treshold of original image</param>
        /// <param name="errorCode">Error code</param>
        /// <returns>Vector of vector of point contours</returns>
        private static VectorOfVectorOfPoint GetImageContours(Mat imgThresh, ref int errorCode)
        {
            try
            {
                // Initiation of the image that will be used in this method
                Mat imgCanny = new Mat();

                // Initiate of vector of vector of point contours
                VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();

                // Find edges using Canny algorithm
                CvInvoke.Canny(imgThresh, imgCanny, 150, 50, 7);

                // Add all contours from canny image to contours variable
                CvInvoke.FindContours(imgCanny, contours, null, RetrType.Ccomp, ChainApproxMethod.ChainApproxNone);

                // Check if debug enabled
                if (Properties.Settings.Default.debug)
                {
                    // Show image of contours
                    CvInvoke.Imshow("Contours", imgCanny);
                }

                // Return the contours
                return contours;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                errorCode = 6;
                return new VectorOfVectorOfPoint();
            }
        }

        /// <summary>
        /// Find potential plates
        /// </summary>
        /// <param name="imgGrayScale">Grayscale input image</param>
        /// <param name="contours">Vector of vector of points containing contours</param>
        /// <param name="errorCode">Error code</param>
        /// <returns>Potential plates</returns>
        private static List<Mat> FindPotentialPlatesInContours(Mat imgGrayScale, VectorOfVectorOfPoint contours, ref int errorCode)
        {
            try
            {
                // Initiate list of possible plates
                List<Mat> possiblePlates = new List<Mat>();

                // Initiate contours image - used for debug
                Image<Bgr, byte> imgContours = new Image<Bgr, byte>(imgGrayScale.Size);

                // Loop through each contour
                for (int i = 0; i < contours.Size; i++)
                {
                    // Creates a rotated box around the contour
                    RotatedRect box = CvInvoke.MinAreaRect(contours[i]);

                    if (box.Angle < -45.0)
                    {
                        float tmp = box.Size.Width;
                        box.Size.Width = box.Size.Height;
                        box.Size.Height = tmp;
                        box.Angle += 90.0f;
                    }
                    else if (box.Angle > 45.0)
                    {
                        float tmp = box.Size.Width;
                        box.Size.Width = box.Size.Height;
                        box.Size.Height = tmp;
                        box.Angle -= 90.0f;
                    }

                    // Width/Height ratio of the rotated box
                    double whRatio = (double)box.Size.Width / box.Size.Height;

                    // Area of the rotated box
                    double area = (double)box.Size.Width * box.Size.Height;

                    // Check if the rotated box has the ratio and size of a license plate
                    if (3.0 < whRatio && whRatio < 7.0 && area > Properties.Settings.Default.area && box.Size.Width > Properties.Settings.Default.boxSizeWidthMin && box.Size.Width < Properties.Settings.Default.boxSizeWidthMax && box.Size.Height > Properties.Settings.Default.boxSizeHeightMin && box.Size.Height < Properties.Settings.Default.boxSizeHeightMax)
                    {
                        using (Mat tmp1 = new Mat())
                        using (Mat tmp2 = new Mat())
                        {
                            PointF[] srcCorners = box.GetVertices();

                            PointF[] destCorners = new PointF[] {
                                new PointF(0, box.Size.Height - 1),
                                new PointF(0, 0),
                                new PointF(box.Size.Width - 1, 0),
                                new PointF(box.Size.Width - 1, box.Size.Height - 1)
                            };

                            using (Mat rot = CvInvoke.GetAffineTransform(srcCorners, destCorners))
                            {
                                CvInvoke.WarpAffine(imgGrayScale, tmp1, rot, Size.Round(box.Size));
                            }

                            //resize the license plate such that the front is ~ 10-12. This size of front results in better accuracy from tesseract
                            Size approxSize = new Size(565, 320);
                            double scale = Math.Min(approxSize.Width / box.Size.Width, approxSize.Height / box.Size.Height);
                            Size newSize = new Size((int)Math.Round(box.Size.Width * scale), (int)Math.Round(box.Size.Height * scale));
                            CvInvoke.Resize(tmp1, tmp2, newSize, 0, 0, Inter.Cubic);

                            //removes some pixels from the edge
                            int edgePixelSize = 7;
                            Rectangle newRoi = new Rectangle(new Point(edgePixelSize, edgePixelSize),
                            tmp2.Size - new Size(2 * edgePixelSize, 2 * edgePixelSize));
                            Mat plate = new Mat(tmp2, newRoi);
                            possiblePlates.Add(plate);
                        }

                        CvInvoke.DrawContours(imgContours, contours, i, new MCvScalar(0, 0, 255));
                    }
                }

                // Check if debug enabled
                if (Properties.Settings.Default.debug)
                {
                    // Show image of possible plates
                    CvInvoke.Imshow("Possible plates", imgContours.Mat);
                }

                //Retunr possible plates
                return possiblePlates;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                errorCode = 7;
                return new List<Mat>();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="potentialPlates">List of potentials plates</param>
        /// <param name="pass">What pass is executing</param>
        /// <param name="errorCode">Error code</param>
        /// <returns>Key value pair of license plate image and its treshold</returns>
        private static KeyValuePair<Mat, Mat> FindPlateInPotentialPlates(List<Mat> potentialPlates, string pass, ref int errorCode)
        {
            // Initiate license plate variable
            KeyValuePair<Mat, Mat> licensePlate = new KeyValuePair<Mat, Mat>();

            // Initiate variable for if license plate was found
            bool licensePlateFound = false;

            try
            {
                // Check what pass
                if (pass == "firstPass")
                {
                    // Loop through all potential plates
                    foreach (var potentialPlate in potentialPlates)
                    {
                        // Initate images used
                        Mat imgBilateralFilter = new Mat();
                        Mat imgThresh = new Mat();
                        Mat imgOpen = new Mat();

                        // Initiate structuring elements used in morphological procedures
                        Mat structuringElement = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(150, 150), new Point(-1, -1));
                        Mat openElement = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(7, 7), new Point(-1, -1));
                        Mat closeElement = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(8, 15), new Point(-1, -1));
                        Mat erodeElement = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(3, 3), new Point(-1, -1));

                        // Initiate other variables
                        VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
                        List<Rectangle> boundingRectangles = new List<Rectangle>();

                        // Maximize contrast of a potential plate
                        Mat imgMaxContrast = ImageProcess.MaximizeContrast(potentialPlate, ref errorCode);

                        // Apply bilateral filter to image with contrast maximized
                        CvInvoke.BilateralFilter(imgMaxContrast, imgBilateralFilter, 5, 20, 20); // 40 20 20

                        // Do morphological operation of tophat
                        CvInvoke.MorphologyEx(imgBilateralFilter, imgOpen, MorphOp.Tophat, structuringElement, new Point(-1, -1), 2, BorderType.Default, new MCvScalar());

                        // Do adaptive treshold to tophat image
                        CvInvoke.AdaptiveThreshold(imgOpen, imgThresh, 255, AdaptiveThresholdType.GaussianC, ThresholdType.BinaryInv, 295, 19); // first pass

                        // Do morphological operation of open to thresholded image
                        CvInvoke.MorphologyEx(imgThresh, imgThresh, MorphOp.Open, openElement, new Point(-1, -1), 1, BorderType.Default, new MCvScalar());

                        // Do morphological operation of erode to open image
                        CvInvoke.MorphologyEx(imgThresh, imgThresh, MorphOp.Erode, erodeElement, new Point(-1, -1), 1, BorderType.Constant, new MCvScalar());

                        // Do morphological operation of close to thresholded image
                        CvInvoke.MorphologyEx(imgThresh, imgThresh, MorphOp.Close, closeElement, new Point(-1, -1), 1, BorderType.Constant, new MCvScalar());


                        // Find contours in erode image
                        CvInvoke.FindContours(imgThresh, contours, null, RetrType.External, ChainApproxMethod.ChainApproxSimple);

                        Image<Bgr, byte> imgContours = imgThresh.ToImage<Bgr, byte>();

                        // Loop through found contours
                        for (int i = 0; i < contours.Size; i++)
                        {
                            // Create a bounding rectangle of the contour
                            var boundingRectangle = CvInvoke.BoundingRectangle(contours[i]);

                            // Check if contour has required dimensions
                            if (boundingRectangle.Height >= Properties.Settings.Default.boundingRectangleHeightMin && boundingRectangle.Height <= Properties.Settings.Default.boundingRectangleHeightMax && boundingRectangle.Width <= Properties.Settings.Default.boundingRectangleWidthMax)
                            {
                                CvInvoke.Rectangle(imgContours, boundingRectangle, new MCvScalar(0, 0, 255));

                                // Add bounding rectangle to the list of bounding rectangles
                                boundingRectangles.Add(boundingRectangle);
                            }
                        }

                        // Check if the list of bounding rectangles has the required number of items
                        if (boundingRectangles.Count >= 5 && boundingRectangles.Count <= 9)
                        {
                            // Make a key value pair with the cropped image and its threshold
                            licensePlate = new KeyValuePair<Mat, Mat>(potentialPlate, imgThresh);

                            licensePlateFound = true;

                            // Check if debug enabled
                            if (Properties.Settings.Default.debug)
                            {
                                // Show image of plate segmented
                                CvInvoke.Imshow("Plate segmented first pass", imgContours.Mat);
                                //CvInvoke.Imshow("Plate segmented first pass", potentialPlate);

                            }

                            // Stop from searching further
                            break;
                        }

                    }
                }
                else if (pass == "secondPass")
                {
                    foreach (var potentialPlate in potentialPlates)
                    {
                        // Initate images used
                        Mat imgBilateralFilter = new Mat();
                        Mat imgThresh = new Mat();
                        Mat imgOpen = new Mat();

                        // Initiate structuring elements used in morphological procedures
                        Mat structuringElement = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(150, 150), new Point(-1, -1));
                        Mat openElement = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(7, 7), new Point(-1, -1));
                        Mat closeElement = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(5, 5), new Point(-1, -1));
                        Mat erodeElement = CvInvoke.GetStructuringElement(ElementShape.Ellipse, new Size(3, 3), new Point(-1, -1));

                        // Initiate other variables
                        VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
                        List<Rectangle> boundingRectangles = new List<Rectangle>();

                        // Maximize contrast of a potential plate
                        Mat imgMaxContrast = ImageProcess.MaximizeContrast(potentialPlate, ref errorCode);

                        // Apply bilateral filter to image with contrast maximized
                        CvInvoke.BilateralFilter(imgMaxContrast, imgBilateralFilter, 5, 10, 10); // 20 10 10


                        // Do morphological operation of tophat
                        CvInvoke.MorphologyEx(imgBilateralFilter, imgOpen, MorphOp.Tophat, structuringElement, new Point(-1, -1), 1, BorderType.Default, new MCvScalar());

                        // Do adaptive treshold to tophat image
                        CvInvoke.AdaptiveThreshold(imgOpen, imgThresh, 255, AdaptiveThresholdType.GaussianC, ThresholdType.BinaryInv, 105, 5); // second pass

                        // Do morphological operation of open to thresholded image
                        CvInvoke.MorphologyEx(imgThresh, imgThresh, MorphOp.Open, openElement, new Point(-1, -1), 1, BorderType.Constant, new MCvScalar());

                        // Do morphological operation of erode to open image
                        CvInvoke.MorphologyEx(imgThresh, imgThresh, MorphOp.Erode, erodeElement, new Point(-1, -1), 1, BorderType.Constant, new MCvScalar());

                        // Do morphological operation of close to thresholded image
                        CvInvoke.MorphologyEx(imgThresh, imgThresh, MorphOp.Close, closeElement, new Point(-1, -1), 1, BorderType.Constant, new MCvScalar());

                        // Find contours in erode image
                        CvInvoke.FindContours(imgThresh, contours, null, RetrType.External, ChainApproxMethod.ChainApproxSimple);


                        Image<Bgr, byte> imgContours = imgThresh.ToImage<Bgr, byte>();

                        // Loop through found contours
                        for (int i = 0; i < contours.Size; i++)
                        {
                            // Create a bounding rectangle of the contour
                            var boundingRectangle = CvInvoke.BoundingRectangle(contours[i]);

                            // Check if contour has required dimensions
                            if (boundingRectangle.Height >= Properties.Settings.Default.boundingRectangleHeightMin && boundingRectangle.Height <= Properties.Settings.Default.boundingRectangleHeightMax)
                            {
                                CvInvoke.Rectangle(imgContours, boundingRectangle, new MCvScalar(0, 0, 255));

                                // Add bounding rectangle to the list of bounding rectangles
                                boundingRectangles.Add(boundingRectangle);
                            }
                        }

                        // Check if the list of bounding rectangles has the required number of items
                        if (boundingRectangles.Count >= 5 && boundingRectangles.Count <= 9)
                        {
                            // Make a key value pair with the cropped image and its threshold
                            licensePlate = new KeyValuePair<Mat, Mat>(potentialPlate, imgThresh);

                            licensePlateFound = true;

                            // Check if debug enabled
                            if (Properties.Settings.Default.debug)
                            {
                                // Show image of plate segmented
                                CvInvoke.Imshow("Plate segmented second pass", imgContours.Mat);
                                //CvInvoke.Imshow("Plate segmented second pass", potentialPlate);
                            }

                            // Stop from searching further
                            break;
                        }
                    }
                }

                // Check if license plate has been found
                if (!licensePlateFound)
                {
                    // Return null
                    return new KeyValuePair<Mat, Mat>();
                }
                else
                {
                    // Return the key value pair
                    return licensePlate;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                errorCode = 8;
                return new KeyValuePair<Mat, Mat>();
            }
        }
    }
}
