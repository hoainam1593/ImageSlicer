using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

namespace ImageSlicer
{
    class MooreNeighborContourTracing
    {
        private static int s_maxBackgroudAlpha = 0;

        private Size m_size;
        private int m_nPixels;
        private int[] m_pixels;

        private int this[Point point]
        {
            get
            {
                int n = (point.Y * m_size.Width) + point.X;
                return m_pixels[n];
            }

            set
            {
                int n = (point.Y * m_size.Width) + point.X;
                m_pixels[n] = value;
            }
        }

        public MooreNeighborContourTracing(string imageFileName)
            : this((Bitmap)Image.FromFile(imageFileName, true))
        {
        }

        public MooreNeighborContourTracing(Bitmap bitmap)
        {
            // Get bitmap data.
            Rectangle rect = new Rectangle(new Point(0, 0), bitmap.Size);
            bool isReadOnly = true;
            var bitmapData = bitmap.LockBits(
                rect, 
                (isReadOnly) ? ImageLockMode.ReadOnly : ImageLockMode.ReadWrite, 
                PixelFormat.Format32bppArgb);

            // Init from bitmap data.
            m_size = new Size(bitmapData.Width, bitmapData.Height);
            m_nPixels = m_size.Width * m_size.Height;
            m_pixels = new int[m_nPixels];
            System.Runtime.InteropServices.Marshal.Copy(bitmapData.Scan0, m_pixels, 0, m_nPixels);

            // Unlock bitmap.
            bitmap.UnlockBits(bitmapData);
        }

        private bool IsContains(Point point)
        {
            return ((point.X < 0) || 
                (point.X >= m_size.Width) || 
                (point.Y < 0) || 
                (point.Y >= m_size.Height)) ? false : true;
        }

        private bool IsColor(Point point, Predicate<int> wanted)
        {
            int pixel = this[point];
            return wanted(pixel);
        }

        private bool IsBackground(Point point)
        {
            if (!IsContains(point))
            {
                return true;
            }

            // Currently, background is transparent color.
            // If you want change background e.g. black color, just change predicate.
            return IsColor(point, IsTransparent_Predicate);
        }

        private static bool IsTransparent_Predicate(int argb)
        {
            Color color = Color.FromArgb(argb);
            return (color.A <= s_maxBackgroudAlpha);
        }

        public static void SetMaxBackgroundAlpha(int maxAlpha)
        {
            s_maxBackgroudAlpha = maxAlpha;
        }

        public Size GetPictureSize()
        {
            return m_size;
        }

        // This gets all boundaries in the given pixels.
        // It assumes you're looking for boundaries between non-transparent shapes on a transparent background
        // (using the isTransparent property);
        // but you could modify this, to pass in a predicate to say what background color you're looking for (e.g. White).
        public List<List<Point>> Trace()
        {
            Size size = m_size;
            HashSet<Point> found = new HashSet<Point>();
            List<Point> list = null;
            List<List<Point>> lists = new List<List<Point>>();
            bool inside = false;

            // Defines the neighborhood offset position from current position and the neighborhood
            // position we want to check next if we find a new border at checkLocationNr.
            int width = size.Width;
            Tuple<Func<Point, Point>, int>[] neighborhood = new Tuple<Func<Point, Point>, int>[]
            {
                new Tuple<Func<Point, Point>, int>(point => new Point(point.X-1,point.Y), 7),
                new Tuple<Func<Point, Point>, int>(point => new Point(point.X-1,point.Y-1), 7),
                new Tuple<Func<Point, Point>, int>(point => new Point(point.X,point.Y-1), 1),
                new Tuple<Func<Point, Point>, int>(point => new Point(point.X+1,point.Y-1), 1),
                new Tuple<Func<Point, Point>, int>(point => new Point(point.X+1,point.Y), 3),
                new Tuple<Func<Point, Point>, int>(point => new Point(point.X+1,point.Y+1), 3),
                new Tuple<Func<Point, Point>, int>(point => new Point(point.X,point.Y+1), 5),
                new Tuple<Func<Point, Point>, int>(point => new Point(point.X-1,point.Y+1), 5)
            };

            for (int y = 0; y < size.Height; ++y)
            {
                for (int x = 0; x < size.Width; ++x)
                {
                    Point point = new Point(x, y);
                    // Scan for non-transparent pixel
                    if (found.Contains(point) && !inside)
                    {
                        // Entering an already discovered border
                        inside = true;
                        continue;
                    }
                    bool isTransparent = IsBackground(point);
                    if (!isTransparent && inside)
                    {
                        // Already discovered border point
                        continue;
                    }
                    if (isTransparent && inside)
                    {
                        // Leaving a border
                        inside = false;
                        continue;
                    }
                    if (!isTransparent && !inside)
                    {
                        lists.Add(list = new List<Point>());

                        // Undiscovered border point
                        found.Add(point); list.Add(point);   // Mark the start pixel
                        int checkLocationNr = 1;  // The neighbor number of the location we want to check for a new border point
                        Point startPos = point;      // Set start position
                        int counter = 0;       // Counter is used for the jacobi stop criterion
                        int counter2 = 0;       // Counter2 is used to determine if the point we have discovered is one single point

                        // Trace around the neighborhood
                        while (true)
                        {
                            // The corresponding absolute array address of checkLocationNr
                            Point checkPosition = neighborhood[checkLocationNr - 1].Item1(point);
                            // Variable that holds the neighborhood position we want to check if we find a new border at checkLocationNr
                            int newCheckLocationNr = neighborhood[checkLocationNr - 1].Item2;

                            // Beware that the point might be outside the bitmap.
                            // The isTransparent method contains the safety check.
                            if (!IsBackground(checkPosition))
                            {
                                // Next border point found
                                if (checkPosition == startPos)
                                {
                                    counter++;

                                    // Stopping criterion (jacob)
                                    if (newCheckLocationNr == 1 || counter >= 3)
                                    {
                                        // Close loop
                                        inside = true; // Since we are starting the search at were we first started we must set inside to true
                                        break;
                                    }
                                }

                                checkLocationNr = newCheckLocationNr; // Update which neighborhood position we should check next
                                point = checkPosition;
                                counter2 = 0;             // Reset the counter that keeps track of how many neighbors we have visited
                                found.Add(point); list.Add(point); // Set the border pixel
                            }
                            else
                            {
                                // Rotate clockwise in the neighborhood
                                checkLocationNr = 1 + (checkLocationNr % 8);
                                if (counter2 > 8)
                                {
                                    // If counter2 is above 8 we have traced around the neighborhood and
                                    // therefor the border is a single black pixel and we can exit
                                    counter2 = 0;
                                    list = null;
                                    break;
                                }
                                else
                                {
                                    counter2++;
                                }
                            }
                        }

                    }
                }
            }
            return lists;
        }
    }
}
