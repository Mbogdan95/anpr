using Emgu.CV.Util;
using System.Drawing;

namespace ANPR
{
    public class ContoursWithData
    {
        // member variables ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''

        public VectorOfPoint contour;             // contour
        public Rectangle boundingRectangle;            // bounding rect for contour
        public double dblArea;                    // area of contour

        // ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        public bool checkIfContourIsValid()      // this is oversimplified, for a production grade program better validity checking would be necessary
        {
            if (boundingRectangle.Height >= Properties.Settings.Default.boundingRectangleHeightMin && boundingRectangle.Height <= Properties.Settings.Default.boundingRectangleHeightMax && boundingRectangle.Width <= Properties.Settings.Default.boundingRectangleWidthMax)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
